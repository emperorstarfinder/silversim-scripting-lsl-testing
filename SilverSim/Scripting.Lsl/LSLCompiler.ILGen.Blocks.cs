// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        
        void ProcessBlock(
            CompileState compileState,
            Type returnType,
            List<LineInfo> functionBody,
            Dictionary<string, object> localVars,
            ref int lineIndex)
        {
            Dictionary<string, ILLabelInfo> labels;

            List<Dictionary<string, object>> localVarsStack = new List<Dictionary<string, object>>();
            List<Dictionary<string, ILLabelInfo>> labelsStack = new List<Dictionary<string, ILLabelInfo>>();
            localVarsStack.Insert(0, localVars);
            labelsStack.Insert(0, new Dictionary<string,ILLabelInfo>());
            int blockLevel = 1;

            for (; lineIndex < functionBody.Count; ++lineIndex)
            {
                localVars = localVarsStack[0];
                labels = labelsStack[0];
                LineInfo functionLine = functionBody[lineIndex];
                LocalBuilder lb;
                switch (functionLine.Line[0])
                {
                    #region Label definition
                    case "@":
                        if (functionLine.Line.Count != 3 || functionLine.Line[2] != ";")
                        {
                            throw CompilerException(functionLine, "not a valid label definition");
                        }
                        else
                        {
                            string labelName = functionLine.Line[1];
                            if (!labels.ContainsKey(labelName))
                            {
                                Label label = compileState.ILGen.DefineLabel();
                                labels[functionLine.Line[1]] = new ILLabelInfo(label, true);
                            }
                            else if (labels[labelName].IsDefined)
                            {
                                throw CompilerException(functionLine, "label already defined");
                            }
                            else
                            {
                                labels[labelName].IsDefined = true;
                            }
                            compileState.ILGen.MarkLabel(labels[labelName].Label);
                        }
                        break;
                    #endregion

                    #region Variable declarations
                    /* type named things are variable declaration */
                    case "integer":
                        if (compileState.IsImplicitControlFlow(functionLine.LineNumber))
                        {
                            throw CompilerException(functionLine,
                                string.Format("variable declaration cannot be a single statement within flow control '{0}'",
                                compileState.GetControlFlowInfo(functionLine.LineNumber)));
                        }
                        lb = compileState.ILGen.DeclareLocal(typeof(int));
                        if (compileState.EmitDebugSymbols)
                        {
                            lb.SetLocalSymInfo(functionLine.Line[1]);
                        }
                        localVars[functionLine.Line[1]] = lb;
                        if (functionLine.Line[2] != ";")
                        {
                            ProcessExpression(
                                compileState,
                                typeof(int),
                                3,
                                functionLine.Line.Count - 2,
                                functionLine,
                                localVars);
                        }
                        else
                        {
                            compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                        }
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "vector":
                        if (compileState.IsImplicitControlFlow(functionLine.LineNumber))
                        {
                            throw CompilerException(functionLine,
                                string.Format("variable declaration cannot be a single statement within flow control '{0}'",
                                compileState.GetControlFlowInfo(functionLine.LineNumber)));
                        }
                        lb = compileState.ILGen.DeclareLocal(typeof(Vector3));
                        if (compileState.EmitDebugSymbols)
                        {
                            lb.SetLocalSymInfo(functionLine.Line[1]);
                        }
                        localVars[functionLine.Line[1]] = lb;
                        if (functionLine.Line[2] != ";")
                        {
                            ProcessExpression(
                                compileState,
                                typeof(Vector3),
                                3,
                                functionLine.Line.Count - 2,
                                functionLine,
                                localVars);
                        }
                        else
                        {
                            compileState.ILGen.Emit(OpCodes.Ldsfld, typeof(Vector3).GetField("Zero"));
                        }
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "list":
                        if (compileState.IsImplicitControlFlow(functionLine.LineNumber))
                        {
                            throw CompilerException(functionLine,
                                string.Format("variable declaration cannot be a single statement within flow control '{0}'",
                                compileState.GetControlFlowInfo(functionLine.LineNumber)));
                        }
                        lb = compileState.ILGen.DeclareLocal(typeof(AnArray));
                        if (compileState.EmitDebugSymbols)
                        {
                            lb.SetLocalSymInfo(functionLine.Line[1]);
                        }
                        localVars[functionLine.Line[1]] = lb;
                        if (functionLine.Line[2] != ";")
                        {
                            ProcessExpression(
                                compileState,
                                typeof(AnArray),
                                3,
                                functionLine.Line.Count - 2,
                                functionLine,
                                localVars);
                        }
                        else
                        {
                            compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(Type.EmptyTypes));
                        }
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "float":
                        if (compileState.IsImplicitControlFlow(functionLine.LineNumber))
                        {
                            throw CompilerException(functionLine,
                                string.Format("variable declaration cannot be a single statement within flow control '{0}'",
                                compileState.GetControlFlowInfo(functionLine.LineNumber)));
                        }
                        lb = compileState.ILGen.DeclareLocal(typeof(double));
                        if (compileState.EmitDebugSymbols)
                        {
                            lb.SetLocalSymInfo(functionLine.Line[1]);
                        }
                        localVars[functionLine.Line[1]] = lb;
                        if (functionLine.Line[2] != ";")
                        {
                            ProcessExpression(
                                compileState,
                                typeof(double),
                                3,
                                functionLine.Line.Count - 2,
                                functionLine, localVars);
                        }
                        else
                        {
                            compileState.ILGen.Emit(OpCodes.Ldc_R8, 0f);
                        }
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "string":
                        if (compileState.IsImplicitControlFlow(functionLine.LineNumber))
                        {
                            throw CompilerException(functionLine,
                                string.Format("variable declaration cannot be a single statement within flow control '{0}'",
                                compileState.GetControlFlowInfo(functionLine.LineNumber)));
                        }
                        lb = compileState.ILGen.DeclareLocal(typeof(string));
                        if (compileState.EmitDebugSymbols)
                        {
                            lb.SetLocalSymInfo(functionLine.Line[1]);
                        }
                        localVars[functionLine.Line[1]] = lb;
                        if (functionLine.Line[2] != ";")
                        {
                            ProcessExpression(
                                compileState,
                                typeof(string),
                                3,
                                functionLine.Line.Count - 2,
                                functionLine,
                                localVars);
                        }
                        else
                        {
                            compileState.ILGen.Emit(OpCodes.Ldstr, string.Empty);
                        }
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "key":
                        if (compileState.IsImplicitControlFlow(functionLine.LineNumber))
                        {
                            throw CompilerException(functionLine,
                                string.Format("variable declaration cannot be a single statement within flow control '{0}'",
                                compileState.GetControlFlowInfo(functionLine.LineNumber)));
                        }
                        lb = compileState.ILGen.DeclareLocal(typeof(LSLKey));
                        if (compileState.EmitDebugSymbols)
                        {
                            lb.SetLocalSymInfo(functionLine.Line[1]);
                        }
                        localVars[functionLine.Line[1]] = lb;
                        if (functionLine.Line[2] != ";")
                        {
                            ProcessExpression(
                                compileState,
                                typeof(LSLKey),
                                3,
                                functionLine.Line.Count - 2,
                                functionLine,
                                localVars);
                        }
                        else
                        {
                            compileState.ILGen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(Type.EmptyTypes));
                        }
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "rotation":
                    case "quaternion":
                        if (compileState.IsImplicitControlFlow(functionLine.LineNumber))
                        {
                            throw CompilerException(functionLine,
                                string.Format("variable declaration cannot be a single statement within flow control '{0}'",
                                compileState.GetControlFlowInfo(functionLine.LineNumber)));
                        }
                        lb = compileState.ILGen.DeclareLocal(typeof(Quaternion));
                        if (compileState.EmitDebugSymbols)
                        {
                            lb.SetLocalSymInfo(functionLine.Line[1]);
                        }
                        localVars[functionLine.Line[1]] = lb;
                        if (functionLine.Line[2] != ";")
                        {
                            ProcessExpression(
                                compileState,
                                typeof(Quaternion),
                                3,
                                functionLine.Line.Count - 2,
                                functionLine,
                                localVars);
                        }
                        else
                        {
                            compileState.ILGen.Emit(OpCodes.Ldsfld, typeof(Quaternion).GetField("Identity"));
                        }
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        break;
                    #endregion

                    #region Control Flow (Loops)
                        /* Control Flow Statements are pre-splitted into own lines with same line number, so we do not have to care about here */
                    case "for":
                        {   /* for(a;b;c) */
                            int semicolon1, semicolon2;
                            int endoffor;
                            int countparens = 0;
                            for (endoffor = 0; endoffor <= functionLine.Line.Count; ++endoffor)
                            {
                                if (functionLine.Line[endoffor] == ")")
                                {
                                    if (--countparens == 0)
                                    {
                                        break;
                                    }
                                }
                                else if (functionLine.Line[endoffor] == "(")
                                {
                                    ++countparens;
                                }
                            }

                            if (endoffor != functionLine.Line.Count - 1 && endoffor != functionLine.Line.Count - 2)
                            {
                                throw CompilerException(functionLine, "Invalid 'for' encountered");
                            }

                            semicolon1 = functionLine.Line.IndexOf(";");
                            semicolon2 = functionLine.Line.IndexOf(";", semicolon1 + 1);
                            if (2 != semicolon1)
                            {
                                ProcessStatement(
                                    compileState,
                                    returnType,
                                    2,
                                    semicolon1 - 1,
                                    functionLine,
                                    localVars,
                                    labels);
                            }
                            Label endlabel = compileState.ILGen.DefineLabel();
                            Label looplabel = compileState.ILGen.DefineLabel();
                            compileState.m_UnnamedLabels.Add(endlabel, new KeyValuePair<int, string>(functionLine.LineNumber, "For End Label"));
                            ControlFlowElement elem = new ControlFlowElement(
                                ControlFlowType.For,
                                functionLine.Line[functionLine.Line.Count - 1] == "{",
                                looplabel,
                                endlabel,
                                compileState.IsImplicitControlFlow(functionLine.LineNumber));
                            compileState.PushControlFlow(elem);

                            compileState.ILGen.MarkLabel(looplabel);

                            if (semicolon1 + 1 != semicolon2)
                            {
                                ProcessExpression(
                                    compileState,
                                    typeof(bool),
                                    semicolon1 + 1,
                                    semicolon2 - 1,
                                    functionLine,
                                    localVars);
                                compileState.ILGen.Emit(OpCodes.Brfalse, endlabel);
                            }

                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
                                /* block */
                                compileState.ILGen.BeginScope();
                                ++blockLevel;
                                localVarsStack.Insert(0, new Dictionary<string, object>(localVars));
                                labelsStack.Insert(0, new Dictionary<string, ILLabelInfo>(labels));
                            }
                        }
                        break;

                    case "while":
                        {
                            int endofwhile;
                            int countparens = 0;
                            for (endofwhile = 0; endofwhile <= functionLine.Line.Count; ++endofwhile)
                            {
                                if (functionLine.Line[endofwhile] == ")")
                                {
                                    if (--countparens == 0)
                                    {
                                        break;
                                    }
                                }
                                else if (functionLine.Line[endofwhile] == "(")
                                {
                                    ++countparens;
                                }
                            }

                            if ((endofwhile != functionLine.Line.Count - 1 && endofwhile != functionLine.Line.Count - 2) || endofwhile == 2)
                            {
                                throw CompilerException(functionLine, "Invalid 'while' encountered");
                            }

                            Label looplabel = compileState.ILGen.DefineLabel();
                            Label endlabel = compileState.ILGen.DefineLabel();
                            compileState.m_UnnamedLabels.Add(endlabel, new KeyValuePair<int, string>(functionLine.LineNumber, "While End Label"));
                            ControlFlowElement elem = new ControlFlowElement(
                                ControlFlowType.While,
                                functionLine.Line[functionLine.Line.Count - 1] == "{",
                                looplabel,
                                endlabel,
                                compileState.IsImplicitControlFlow(functionLine.LineNumber));
                            compileState.PushControlFlow(elem);

                            compileState.ILGen.Emit(OpCodes.Br, endlabel);

                            compileState.ILGen.MarkLabel(looplabel);
                            ProcessExpression(
                                compileState,
                                typeof(bool),
                                2,
                                endofwhile - 1,
                                functionLine,
                                localVars);
                            compileState.ILGen.Emit(OpCodes.Brfalse, endlabel);

                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
                                compileState.ILGen.BeginScope();
                                ++blockLevel;
                                localVarsStack.Insert(0, new Dictionary<string, object>(localVars));
                                labelsStack.Insert(0, new Dictionary<string, ILLabelInfo>(labels));
                            }
                        }
                        break;

                    case "do":
                        {
                            Label looplabel = compileState.ILGen.DefineLabel();
                            Label endlabel = compileState.ILGen.DefineLabel();
                            compileState.m_UnnamedLabels.Add(endlabel, new KeyValuePair<int, string>(functionLine.LineNumber, "Do While End Label"));
                            ControlFlowElement elem = new ControlFlowElement(
                                ControlFlowType.DoWhile,
                                functionLine.Line[functionLine.Line.Count - 1] == "{",
                                looplabel,
                                endlabel,
                                compileState.IsImplicitControlFlow(functionLine.LineNumber));
                            compileState.PushControlFlow(elem);

                            compileState.ILGen.MarkLabel(looplabel);
                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
                                compileState.ILGen.BeginScope();
                                ++blockLevel;
                                localVarsStack.Insert(0, new Dictionary<string, object>(localVars));
                                labelsStack.Insert(0, new Dictionary<string, ILLabelInfo>(labels));
                            }
                        }
                        break;
                    #endregion

                    #region Control Flow (Conditions)
                    /* Control Flow Statements are pre-splitted into own lines with same line number, so we do not have to care about here */
                    case "if":
                        compileState.PopControlFlowImplicit(functionLine.LineNumber);
                        {
                            Label eoiflabel = compileState.ILGen.DefineLabel();
                            Label endlabel = compileState.ILGen.DefineLabel();
                            compileState.m_UnnamedLabels.Add(eoiflabel, new KeyValuePair<int, string>(functionLine.LineNumber, "IfElse End Of All Label"));
                            compileState.m_UnnamedLabels.Add(endlabel, new KeyValuePair<int, string>(functionLine.LineNumber, "IfElse End Label"));

                            int endofif;
                            int countparens = 0;
                            for (endofif = 0; endofif <= functionLine.Line.Count; ++endofif)
                            {
                                if (functionLine.Line[endofif] == ")")
                                {
                                    if (--countparens == 0)
                                    {
                                        break;
                                    }
                                }
                                else if (functionLine.Line[endofif] == "(")
                                {
                                    ++countparens;
                                }
                            }

                            ControlFlowElement elem = new ControlFlowElement(
                                ControlFlowType.If,
                                functionLine.Line[functionLine.Line.Count - 1] == "{",
                                null,
                                endlabel,
                                eoiflabel,
                                compileState.IsImplicitControlFlow(functionLine.LineNumber));
                            compileState.PushControlFlow(elem);

                            if ((endofif != functionLine.Line.Count - 1 && endofif != functionLine.Line.Count - 2) || endofif == 2)
                            {
                                throw CompilerException(functionLine, "Invalid 'if' encountered");
                            }

                            ProcessExpression(
                                compileState,
                                typeof(bool),
                                2,
                                endofif - 1,
                                functionLine,
                                localVars);
                            compileState.ILGen.Emit(OpCodes.Brfalse, endlabel);

                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
                                compileState.ILGen.BeginScope();
                                ++blockLevel;
                                localVarsStack.Insert(0, new Dictionary<string, object>(localVars));
                                labelsStack.Insert(0, new Dictionary<string, ILLabelInfo>(labels));
                            }
                        }
                        break;

                    case "else":
                        if (null == compileState.LastBlock)
                        {
                            throw CompilerException(functionLine, "No matching 'if' found for 'else'");
                        }
                        else if (functionLine.Line.Count > 1 && functionLine.Line[1] == "if")
                        { /* else if */
                            Label eoiflabel = compileState.LastBlock.EndOfIfFlowLabel.Value;
                            Label endlabel = compileState.ILGen.DefineLabel();
                            compileState.m_UnnamedLabels.Add(endlabel, new KeyValuePair<int, string>(functionLine.LineNumber, "ElseIf End Label"));

                            ControlFlowElement elem = new ControlFlowElement(
                                ControlFlowType.ElseIf,
                                functionLine.Line[functionLine.Line.Count - 1] == "{",
                                null,
                                endlabel,
                                eoiflabel,
                                compileState.IsImplicitControlFlow(functionLine.LineNumber));
                            compileState.PushControlFlow(elem);

                            int endofif;
                            int countparens = 0;
                            for (endofif = 0; endofif <= functionLine.Line.Count; ++endofif)
                            {
                                if (functionLine.Line[endofif] == ")")
                                {
                                    if (--countparens == 0)
                                    {
                                        break;
                                    }
                                }
                                else if (functionLine.Line[endofif] == "(")
                                {
                                    ++countparens;
                                }
                            }

                            if ((endofif != functionLine.Line.Count - 1 && endofif != functionLine.Line.Count - 2) || endofif == 2)
                            {
                                throw CompilerException(functionLine, "Invalid 'else if' encountered");
                            }

                            ProcessExpression(
                                compileState,
                                typeof(bool),
                                3,
                                endofif - 1,
                                functionLine,
                                localVars);
                            compileState.ILGen.Emit(OpCodes.Brfalse, endlabel);

                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
                                compileState.ILGen.BeginScope();
                                ++blockLevel;
                                localVarsStack.Insert(0, new Dictionary<string, object>(localVars));
                                labelsStack.Insert(0, new Dictionary<string, ILLabelInfo>(labels));
                            }
                        }
                        else
                        {
                            /* else */
                            Label eoiflabel = compileState.LastBlock.EndOfIfFlowLabel.Value;
                            Label endlabel = compileState.ILGen.DefineLabel();
                            compileState.m_UnnamedLabels.Add(endlabel, new KeyValuePair<int, string>(functionLine.LineNumber, "Else End Label"));

                            ControlFlowElement elem = new ControlFlowElement(
                                ControlFlowType.Else,
                                functionLine.Line[functionLine.Line.Count - 1] == "{",
                                null,
                                endlabel,
                                eoiflabel,
                                compileState.IsImplicitControlFlow(functionLine.LineNumber));
                            compileState.PushControlFlow(elem);

                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
                                compileState.ILGen.BeginScope();
                                ++blockLevel;
                                localVarsStack.Insert(0, new Dictionary<string, object>(localVars));
                                labelsStack.Insert(0, new Dictionary<string, ILLabelInfo>(labels));
                            }
                        }
                        break;
                    #endregion

                    #region New unconditional block
                    case "{": /* new unconditional block */
                        compileState.PopControlFlowImplicits(functionLine.LineNumber);
                        {
                            ControlFlowElement elem = new ControlFlowElement(ControlFlowType.UnconditionalBlock, true);
                            compileState.PushControlFlow(elem);
                            compileState.ILGen.BeginScope();
                            ++blockLevel;
                            localVarsStack.Insert(0, new Dictionary<string, object>(localVars));
                            labelsStack.Insert(0, new Dictionary<string, ILLabelInfo>(labels));
                        }
                        break;
                    #endregion

                    #region End of unconditional/conditional block
                    case "}": /* end unconditional/conditional block */
                        {
                            Dictionary<int, string> messages = new Dictionary<int, string>();
                            foreach (KeyValuePair<string, ILLabelInfo> kvp in labels)
                            {
                                if (!kvp.Value.IsDefined)
                                {
                                    foreach (int line in kvp.Value.UsedInLines)
                                    {
                                        messages[line] = string.Format("Label '{0}' not defined", kvp.Key);
                                    }
                                }
                            }
                            if (messages.Count != 0)
                            {
                                throw new CompilerException(messages);
                            }
                            ControlFlowElement elem = compileState.PopControlFlowExplicit(functionLine.LineNumber);
                            if (elem.IsExplicitBlock && elem.Type != ControlFlowType.Entry)
                            {
                                compileState.ILGen.EndScope();
                            }
                            labelsStack.RemoveAt(0);
                            localVarsStack.RemoveAt(0);
                            switch(--blockLevel)
                            {
                                case 0:
                                    return;
                                    
                                case -1:
                                    throw CompilerException(functionLine, "Unmatched '}' found");

                                default:
                                    break;
                            }
                        }
                        break;
                    #endregion

                    default:
                        ProcessStatement(
                            compileState,
                            returnType,
                            0,
                            functionLine.Line.Count - 2,
                            functionLine,
                            localVars,
                            labels);
                        compileState.PopControlFlowImplicit(functionLine.LineNumber);
                        break;
                }
            }

            if (blockLevel != 0)
            {
                throw CompilerException(functionBody[functionBody.Count - 1], "Missing '}'");
            }
        }

        void ProcessFunction(
            CompileState compileState,
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            MethodBuilder mb,
#if DEBUG
            ILGenDumpProxy ilgen,
#else
            ILGenerator ilgen,
#endif
            List<LineInfo> functionBody,
            Dictionary<string, object> localVars)
        {
            compileState.InitControlFlow();
            Type returnType = typeof(void);
            List<string> functionDeclaration = functionBody[0].Line;
            int functionStart = 2;
            compileState.ScriptTypeBuilder = scriptTypeBuilder;
            compileState.StateTypeBuilder = stateTypeBuilder;
            compileState.ILGen = ilgen;

            switch (functionDeclaration[0])
            {
                case "integer":
                    returnType = typeof(int);
                    break;

                case "vector":
                    returnType = typeof(Vector3);
                    break;

                case "list":
                    returnType = typeof(AnArray);
                    break;

                case "float":
                    returnType = typeof(double);
                    break;

                case "string":
                    returnType = typeof(string);
                    break;

                case "key":
                    returnType = typeof(LSLKey);
                    break;

                case "rotation":
                case "quaternion":
                    returnType = typeof(Quaternion);
                    break;

                case "void":
                    returnType = typeof(void);
                    break;

                default:
                    functionStart = 1;
                    break;
            }

            int paramidx = 0;
            while (functionDeclaration[++functionStart] != ")")
            {
                if (functionDeclaration[functionStart] == ",")
                {
                    ++functionStart;
                }
                Type t;
                switch (functionDeclaration[functionStart++])
                {
                    case "integer":
                        t = typeof(int);
                        break;

                    case "vector":
                        t = typeof(Vector3);
                        break;

                    case "list":
                        t = typeof(AnArray);
                        break;

                    case "float":
                        t = typeof(double);
                        break;

                    case "string":
                        t = typeof(string);
                        break;

                    case "key":
                        t = typeof(LSLKey);
                        break;

                    case "rotation":
                    case "quaternion":
                        t = typeof(Quaternion);
                        break;

                    default:
                        throw CompilerException(functionBody[0], "Internal Error");
                }
                /* parameter name and type in order */
                localVars[functionDeclaration[functionStart]] = new ILParameterInfo(t, paramidx + 1);
            }

            int lineIndex = 1;
            ProcessBlock(
                compileState,
                mb.ReturnType,
                functionBody,
                localVars,
                ref lineIndex);

            /* we have no missing return value check right now, so we simply emit default values in that case */
            if (returnType == typeof(int))
            {
                ilgen.Emit(OpCodes.Ldc_I4_0);
            }
            else if (returnType == typeof(double))
            {
                ilgen.Emit(OpCodes.Ldc_R8, 0f);
            }
            else if (returnType == typeof(string))
            {
                ilgen.Emit(OpCodes.Ldstr);
            }
            else if (returnType == typeof(AnArray))
            {
                ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(Type.EmptyTypes));
            }
            else if (returnType == typeof(Vector3))
            {
                ilgen.Emit(OpCodes.Ldsfld, typeof(Vector3).GetField("Zero"));
            }
            else if (returnType == typeof(Quaternion))
            {
                ilgen.Emit(OpCodes.Ldsfld, typeof(Quaternion).GetField("Identity"));
            }
            else if (returnType == typeof(LSLKey))
            {
                ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(Type.EmptyTypes));
            }
            ilgen.Emit(OpCodes.Ret);

            compileState.FinishControlFlowChecks();
        }
    }
}
