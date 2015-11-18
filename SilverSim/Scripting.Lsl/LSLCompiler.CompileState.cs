// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        sealed internal class LineInfo
        {
            public readonly List<string> Line;
            public readonly int LineNumber;

            public LineInfo(List<string> line, int lineNo)
            {
                Line = line;
                LineNumber = lineNo;
            }
        }

        internal enum ControlFlowType
        {
            Entry,
            UnconditionalBlock,
            If,
            Else,
            ElseIf,
            For,
            DoWhile,
            While
        }

        sealed internal class ControlFlowElement
        {
            public bool IsExplicitBlock;
            public bool PopNextImplicit;
            public ControlFlowType Type;
            public Label? LoopLabel;
            public Label? EndOfControlFlowLabel;
            public Label? EndOfIfFlowLabel;
            //public bool EndOfIfLabelDefined;

            public ControlFlowElement(ControlFlowType type, bool isExplicit, Label looplabel, Label eofclabel)
            {
                IsExplicitBlock = isExplicit;
                Type = type;
                LoopLabel = looplabel;
                EndOfControlFlowLabel = eofclabel;
            }
            public ControlFlowElement(ControlFlowType type, bool isExplicit, Label looplabel, Label eofclabel, bool popOneImplicit)
            {
                IsExplicitBlock = isExplicit;
                Type = type;
                LoopLabel = looplabel;
                EndOfControlFlowLabel = eofclabel;
                PopNextImplicit = popOneImplicit;
            }
            public ControlFlowElement(ControlFlowType type, bool isExplicit, Label looplabel, Label eofclabel, Label eoiflabel)
            {
                IsExplicitBlock = isExplicit;
                Type = type;
                LoopLabel = looplabel;
                EndOfControlFlowLabel = eofclabel;
                EndOfIfFlowLabel = eoiflabel;
            }
            public ControlFlowElement(ControlFlowType type, bool isExplicit, Label? looplabel, Label eofclabel, Label eoiflabel, bool popOneImplicit)
            {
                IsExplicitBlock = isExplicit;
                Type = type;
                LoopLabel = looplabel;
                EndOfControlFlowLabel = eofclabel;
                EndOfIfFlowLabel = eoiflabel;
                PopNextImplicit = popOneImplicit;
            }
            public ControlFlowElement(ControlFlowType type, bool isExplicit)
            {
                IsExplicitBlock = isExplicit;
                Type = type;
            }
        }

        sealed internal class CompileState
        {
            public ApiInfo ApiInfo = new ApiInfo();
            public bool ForcedSleepDefault;
            public bool EmitDebugSymbols;
            public Dictionary<string, MethodBuilder> m_FunctionInfo = new Dictionary<string, MethodBuilder>();
            public Dictionary<string, KeyValuePair<Type, KeyValuePair<string, Type>[]>> m_FunctionSignature = new Dictionary<string, KeyValuePair<Type, KeyValuePair<string, Type>[]>>();
            public Dictionary<string, Type> m_VariableDeclarations = new Dictionary<string, Type>();
            public Dictionary<string, FieldBuilder> m_VariableFieldInfo = new Dictionary<string, FieldBuilder>();
            public Dictionary<string, LineInfo> m_VariableInitValues = new Dictionary<string, LineInfo>();
            public List<List<string>> m_LocalVariables = new List<List<string>>();
            public Dictionary<string, List<LineInfo>> m_Functions = new Dictionary<string, List<LineInfo>>();
            public Dictionary<string, Dictionary<string, List<LineInfo>>> m_States = new Dictionary<string, Dictionary<string, List<LineInfo>>>();
            public Dictionary<string, FieldBuilder> m_ApiFieldInfo = new Dictionary<string, FieldBuilder>();
            readonly List<ControlFlowElement> m_ControlFlowStack = new List<ControlFlowElement>();
            public Dictionary<Label, KeyValuePair<int, string>> m_UnnamedLabels = new Dictionary<Label, KeyValuePair<int, string>>();
            public ControlFlowElement LastBlock;

            public TypeBuilder ScriptTypeBuilder;
            public TypeBuilder StateTypeBuilder;
            public FieldBuilder InstanceField;
#if DEBUG
            public ILGenDumpProxy ILGen;
#else
            public ILGenerator ILGen;
#endif

            public void InitControlFlow()
            {
                m_ControlFlowStack.Clear();
                m_UnnamedLabels.Clear();
                LastBlock = null;
                PushControlFlow(new ControlFlowElement(ControlFlowType.Entry, true));
            }

            public void FinishControlFlowChecks()
            {
                if (LastBlock != null && (LastBlock.Type == ControlFlowType.If || LastBlock.Type == ControlFlowType.ElseIf) && null != LastBlock.EndOfIfFlowLabel)
                {
                    m_UnnamedLabels.Remove(LastBlock.EndOfIfFlowLabel.Value);
                    ILGen.MarkLabel(LastBlock.EndOfIfFlowLabel.Value);
                    LastBlock = null;
                }

                Dictionary<int, string> messages = new Dictionary<int, string>();
                foreach (KeyValuePair<int, string> kvp in m_UnnamedLabels.Values)
                {
                    messages[kvp.Key] = string.Format("Internal Error! Undefined local label for {0}", kvp.Value);
                }
                if (messages.Count != 0)
                {
                    throw new CompilerException(messages);
                }
            }

            public void PushControlFlow(ControlFlowElement e)
            {
                m_ControlFlowStack.Insert(0, e);
                LastBlock = null;
            }

            public string GetControlFlowInfo(int lineNumber)
            {
                if (m_ControlFlowStack.Count == 0)
                {
                    throw new CompilerException(lineNumber, "Mismatched '}'");
                }
                switch (m_ControlFlowStack[0].Type)
                {
                    case ControlFlowType.Entry: return "function entry";
                    case ControlFlowType.If: return "if";
                    case ControlFlowType.Else: return "else";
                    case ControlFlowType.ElseIf: return "else if";
                    case ControlFlowType.For: return "for";
                    case ControlFlowType.DoWhile: return "do ... while";
                    case ControlFlowType.While: return "while";
                    default: throw new ArgumentException(m_ControlFlowStack[0].Type.ToString());
                }
            }

            public bool IsImplicitControlFlow(int lineNumber)
            {
                if (m_ControlFlowStack.Count == 0)
                {
                    throw new CompilerException(lineNumber, "Mismatched '}'");
                }
                return !m_ControlFlowStack[0].IsExplicitBlock;
            }

            public void CloseOpenIfBlocks(int lineNumber)
            {
                if (LastBlock != null && (LastBlock.Type == ControlFlowType.If || LastBlock.Type == ControlFlowType.ElseIf) && null != LastBlock.EndOfIfFlowLabel)
                {
                    m_UnnamedLabels.Remove(LastBlock.EndOfIfFlowLabel.Value);
                    ILGen.MarkLabel(LastBlock.EndOfIfFlowLabel.Value);
                    LastBlock = null;
                }
            }

            public void PopControlFlowImplicit(int lineNumber)
            {
                if (LastBlock != null && (LastBlock.Type == ControlFlowType.If || LastBlock.Type == ControlFlowType.ElseIf) && null != LastBlock.EndOfIfFlowLabel)
                {
                    m_UnnamedLabels.Remove(LastBlock.EndOfIfFlowLabel.Value);
                    ILGen.MarkLabel(LastBlock.EndOfIfFlowLabel.Value);
                    LastBlock = null;
                }

                if (m_ControlFlowStack.Count == 0)
                {
                    throw new CompilerException(lineNumber, "Mismatched '}'");
                }
                else if (!m_ControlFlowStack[0].IsExplicitBlock)
                {
                    ControlFlowElement elem = m_ControlFlowStack[0];
                    m_ControlFlowStack.RemoveAt(0);
                    if (elem.Type == ControlFlowType.If || elem.Type == ControlFlowType.ElseIf)
                    {
                        LastBlock = elem;
                    }
                    else if (null != elem.EndOfIfFlowLabel) /* if we are putting one to LastBlock, we do not close the label */
                    {
                        if (elem.Type == ControlFlowType.Else)
                        {
                            if (!m_UnnamedLabels.Remove(elem.EndOfIfFlowLabel.Value))
                            {
                                throw new CompilerException(lineNumber, "Internal Error! Duplicate End Of If Label");
                            }
                            ILGen.MarkLabel(elem.EndOfIfFlowLabel.Value);
                        }
                        else
                        {
                            ILGen.Emit(OpCodes.Br, elem.EndOfIfFlowLabel.Value);
                        }
                    }
                    if (null != elem.EndOfControlFlowLabel)
                    {
                        if (!m_UnnamedLabels.Remove(elem.EndOfControlFlowLabel.Value))
                        {
                            throw new CompilerException(lineNumber, string.Format("Internal Error! Duplicate End Of Flow ('{0}') Label", elem.Type.ToString()));
                        }
                        ILGen.MarkLabel(elem.EndOfControlFlowLabel.Value);
                    }
                }
            }

            public void PopControlFlowImplicits(int lineNumber)
            {
                if (LastBlock != null && (LastBlock.Type == ControlFlowType.If || LastBlock.Type == ControlFlowType.ElseIf) && null != LastBlock.EndOfIfFlowLabel)
                {
                    if (!m_UnnamedLabels.Remove(LastBlock.EndOfIfFlowLabel.Value))
                    {
                        throw new CompilerException(lineNumber, "Internal Error! Duplicate End Of If Label");
                    }
                    ILGen.MarkLabel(LastBlock.EndOfIfFlowLabel.Value);
                    LastBlock = null;
                }

                if (m_ControlFlowStack.Count == 0)
                {
                    throw new CompilerException(lineNumber, "Mismatched '}'");
                }
                else while (!m_ControlFlowStack[0].IsExplicitBlock)
                    {
                        ControlFlowElement elem = m_ControlFlowStack[0];
                        m_ControlFlowStack.RemoveAt(0);
                        if (elem.Type == ControlFlowType.If || elem.Type == ControlFlowType.ElseIf)
                        {
                            LastBlock = elem;
                        }
                        else if (null != elem.EndOfIfFlowLabel) /* if we are putting one to LastBlock, we do not close the label */
                        {
                            if (elem.Type == ControlFlowType.Else)
                            {
                                if (!m_UnnamedLabels.Remove(elem.EndOfIfFlowLabel.Value))
                                {
                                    throw new CompilerException(lineNumber, "Internal Error! Duplicate End Of If Label");
                                }
                                ILGen.MarkLabel(elem.EndOfIfFlowLabel.Value);
                            }
                            else
                            {
                                ILGen.Emit(OpCodes.Br, elem.EndOfIfFlowLabel.Value);
                            }
                        }
                        if (null != elem.EndOfControlFlowLabel)
                        {
                            if (!m_UnnamedLabels.Remove(elem.EndOfControlFlowLabel.Value))
                            {
                                throw new CompilerException(lineNumber, string.Format("Internal Error! Duplicate End Of Flow ('{0}') Label", elem.Type.ToString()));
                            }
                            ILGen.MarkLabel(elem.EndOfControlFlowLabel.Value);
                        }
                    }
            }

            public ControlFlowElement PopControlFlowExplicit(int lineNumber)
            {
                if (LastBlock != null && (LastBlock.Type == ControlFlowType.If || LastBlock.Type == ControlFlowType.ElseIf) && null != LastBlock.EndOfIfFlowLabel)
                {
                    m_UnnamedLabels.Remove(LastBlock.EndOfIfFlowLabel.Value);
                    ILGen.MarkLabel(LastBlock.EndOfIfFlowLabel.Value);
                    LastBlock = null;
                }

                PopControlFlowImplicits(lineNumber);

                while (m_ControlFlowStack.Count != 0 && !m_ControlFlowStack[0].IsExplicitBlock)
                {
                    ControlFlowElement elem = m_ControlFlowStack[0];
                    m_ControlFlowStack.RemoveAt(0);
                    if (elem.Type == ControlFlowType.If || elem.Type == ControlFlowType.ElseIf)
                    {
                        LastBlock = elem;
                    }
                    else if (null != elem.EndOfIfFlowLabel) /* if we are putting one to LastBlock, we do not close the label */
                    {
                        if (!m_UnnamedLabels.Remove(elem.EndOfIfFlowLabel.Value))
                        {
                            throw new CompilerException(lineNumber, "Internal Error! Duplicate End Of If Label");
                        }
                        ILGen.MarkLabel(elem.EndOfIfFlowLabel.Value);
                    }
                    if (null != elem.EndOfControlFlowLabel)
                    {
                        if (!m_UnnamedLabels.Remove(elem.EndOfControlFlowLabel.Value))
                        {
                            throw new CompilerException(lineNumber, string.Format("Internal Error! Duplicate End Of Flow ('{0}') Label", elem.Type.ToString()));
                        }
                        ILGen.MarkLabel(elem.EndOfControlFlowLabel.Value);
                    }
                }

                if (m_ControlFlowStack.Count == 0)
                {
                    throw new CompilerException(lineNumber, "Mismatched '}'");
                }
                else
                {
                    ControlFlowElement elem = m_ControlFlowStack[0];
                    m_ControlFlowStack.RemoveAt(0);
                    if (elem.Type == ControlFlowType.If || elem.Type == ControlFlowType.ElseIf)
                    {
                        LastBlock = elem;
                    }
                    else if (null != elem.EndOfIfFlowLabel) /* if we are putting one to LastBlock, we do not close the label */
                    {
                        if (elem.Type == ControlFlowType.Else)
                        {
                            if (!m_UnnamedLabels.Remove(elem.EndOfIfFlowLabel.Value))
                            {
                                throw new CompilerException(lineNumber, "Internal Error! Duplicate End Of If Label");
                            }
                            ILGen.MarkLabel(elem.EndOfIfFlowLabel.Value);
                        }
                        else
                        {
                            ILGen.Emit(OpCodes.Br, elem.EndOfIfFlowLabel.Value);
                        }
                    }
                    if (null != elem.EndOfControlFlowLabel)
                    {
                        if (!m_UnnamedLabels.Remove(elem.EndOfControlFlowLabel.Value))
                        {
                            throw new CompilerException(lineNumber, string.Format("Internal Error! Duplicate End Of Flow ('{0}') Label", elem.Type.ToString()));
                        }
                        ILGen.MarkLabel(elem.EndOfControlFlowLabel.Value);
                        elem.EndOfControlFlowLabel = null;
                    }
                    return elem;
                }
            }

            public CompileState()
            {

            }
        }
    }
}
