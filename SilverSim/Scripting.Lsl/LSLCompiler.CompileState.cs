// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Reflection.Emit;

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

            public override string ToString()
            {
                return LineNumber.ToString() + ": " + string.Join(" ", Line);
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

            public TypeBuilder ScriptTypeBuilder;
            public TypeBuilder StateTypeBuilder;
            public FieldBuilder InstanceField;
#if DEBUG
            public ILGenDumpProxy ILGen;
#else
            public ILGenerator ILGen;
#endif

            public List<LineInfo> FunctionBody;
            public int FunctionLineIndex;

            public CompileState()
            {

            }

            #region Function Body access
            public LineInfo GetLine(string message = "Premature end of function body")
            {
                int lineIndex = FunctionLineIndex++;
                List<LineInfo> functionBody = FunctionBody;
                if (lineIndex >= functionBody.Count)
                {
                    throw CompilerException(functionBody[functionBody.Count - 1], message);
                }
                return functionBody[lineIndex];
            }

            public LineInfo PeekLine(string message = "Premature end of function body")
            {
                int lineIndex = FunctionLineIndex;
                List<LineInfo> functionBody = FunctionBody;
                if (lineIndex >= functionBody.Count)
                {
                    throw CompilerException(functionBody[functionBody.Count - 1], message);
                }
                return functionBody[lineIndex];
            }

            public bool HaveMoreLines
            {
                get
                {
                    return FunctionLineIndex < FunctionBody.Count;
                }
            }
            #endregion
        }
    }
}
