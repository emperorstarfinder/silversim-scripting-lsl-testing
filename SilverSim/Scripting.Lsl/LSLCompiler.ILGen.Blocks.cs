// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        void ProcessBlock(
            CompileState compileState,
            Type returnType,
            Dictionary<string, object> localVars,
            Dictionary<string, ILLabelInfo> labels,
            bool isImplicit = false)
        {
            Label? eoif_label = null;
            do
            {
            processnext:
                LineInfo functionLine = compileState.GetLine();
                LocalBuilder lb;
                switch (functionLine.Line[0])
                {
                    #region Label definition
                    case "@":
                        if(eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
                        }

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
                                throw CompilerException(functionLine, string.Format("label '{0}' already defined", labelName));
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
                        if (isImplicit)
                        {
                            throw CompilerException(functionLine, "variable declaration not allowed within conditional statement without block");
                        }

                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
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
                        if (isImplicit)
                        {
                            throw CompilerException(functionLine, "variable declaration not allowed within conditional statement without block");
                        }

                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
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
                        if (isImplicit)
                        {
                            throw CompilerException(functionLine, "variable declaration not allowed within conditional statement without block");
                        }

                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
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
                        if (isImplicit)
                        {
                            throw CompilerException(functionLine, "variable declaration not allowed within conditional statement without block");
                        }

                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
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
                            compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)0);
                        }
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        break;

                    case "string":
                        if (isImplicit)
                        {
                            throw CompilerException(functionLine, "variable declaration not allowed within conditional statement without block");
                        }

                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
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
                        if (isImplicit)
                        {
                            throw CompilerException(functionLine, "variable declaration not allowed within conditional statement without block");
                        }

                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
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
                        if (isImplicit)
                        {
                            throw CompilerException(functionLine, "variable declaration not allowed within conditional statement without block");
                        }

                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
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
                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
                        }

                        {   /* for(a;b;c) */
                            int semicolon1;
                            int semicolon2;
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
                                    typeof(void),
                                    2,
                                    semicolon1 - 1,
                                    functionLine,
                                    localVars,
                                    labels);
                            }
                            Label endlabel = compileState.ILGen.DefineLabel();
                            Label looplabel = compileState.ILGen.DefineLabel();

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
                                ProcessBlock(
                                    compileState,
                                    returnType,
                                    new Dictionary<string, object>(localVars),
                                    labels);
                            }
                            else
                            {
                                ProcessBlock(
                                    compileState,
                                    returnType,
                                    localVars,
                                    labels,
                                    true);
                            }

                            if (semicolon2 + 1 != endoffor)
                            {
                                ProcessExpression(
                                    compileState,
                                    typeof(void),
                                    semicolon2 + 1,
                                    endoffor - 1,
                                    functionLine,
                                    localVars);
                            }


                            compileState.ILGen.Emit(OpCodes.Br, looplabel);
                            compileState.ILGen.MarkLabel(endlabel);
                        }
                        break;

                    case "while":
                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
                        }

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
                                ProcessBlock(
                                    compileState,
                                    returnType,
                                    new Dictionary<string, object>(localVars),
                                    labels);
                            }
                            else
                            {
                                ProcessBlock(
                                    compileState,
                                    returnType,
                                    localVars,
                                    labels,
                                    true);
                            }

                            compileState.ILGen.Emit(OpCodes.Br, looplabel);
                            compileState.ILGen.MarkLabel(endlabel);
                        }
                        break;

                    case "do":
                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
                        }

                        {
                            Label looplabel = compileState.ILGen.DefineLabel();

                            compileState.ILGen.MarkLabel(looplabel);
                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
                                ProcessBlock(
                                    compileState,
                                    returnType,
                                    new Dictionary<string, object>(localVars),
                                    labels);
                            }
                            else
                            {
                                ProcessBlock(
                                    compileState,
                                    returnType,
                                    localVars,
                                    labels,
                                    true);
                            }

                            functionLine = compileState.GetLine("Missing 'while' for 'do'");
                            if(functionLine.Line[0] != "while")
                            {
                                throw CompilerException(functionLine, "Missing 'while' for 'do'");
                            }

                            if (compileState.GetLine("Invalid 'while' for 'do'").Line[0] != ";")
                            {
                                throw CompilerException(functionLine, "Invalid 'while' for 'do'");
                            }

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

                            ProcessExpression(
                                compileState,
                                typeof(bool),
                                2,
                                endofwhile - 1,
                                functionLine,
                                localVars);
                            compileState.ILGen.Emit(OpCodes.Brtrue, looplabel);
                        }
                        break;

                    #endregion

                    #region Control Flow (Conditions)
                    /* Control Flow Statements are pre-splitted into own lines with same line number, so we do not have to care about here */
                    case "if":
                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
                        }

                        {
                            eoif_label = compileState.ILGen.DefineLabel();
                            Label endlabel = compileState.ILGen.DefineLabel();

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
                                ProcessBlock(
                                    compileState,
                                    returnType,
                                    new Dictionary<string, object>(localVars),
                                    labels);

                                compileState.ILGen.Emit(OpCodes.Br, eoif_label.Value);
                                compileState.ILGen.MarkLabel(endlabel);
                            }
                            else
                            {
                                ProcessBlock(
                                    compileState,
                                    returnType,
                                    localVars,
                                    labels,
                                    true);

                                compileState.ILGen.Emit(OpCodes.Br, eoif_label.Value);
                                compileState.ILGen.MarkLabel(endlabel);

                                LineInfo li = compileState.PeekLine();
                                if (li.Line[0] == "else")
                                {
                                    goto processnext;
                                }
                            }

                        }
                        break;

                    case "else":
                        if(!eoif_label.HasValue)
                        {
                            throw CompilerException(functionLine, "No matching 'if' found for 'else'");
                        }
                        else if (functionLine.Line.Count > 1 && functionLine.Line[1] == "if")
                        { /* else if */
                            int endofif;
                            int countparens = 0;
                            Label endlabel = compileState.ILGen.DefineLabel();

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
                                ProcessBlock(
                                    compileState,
                                    returnType,
                                    new Dictionary<string, object>(localVars),
                                    labels);

                                compileState.ILGen.Emit(OpCodes.Br, eoif_label.Value);
                                compileState.ILGen.MarkLabel(endlabel);
                            }
                            else
                            {
                                ProcessBlock(
                                    compileState,
                                    returnType,
                                    localVars,
                                    labels,
                                    true);

                                compileState.ILGen.Emit(OpCodes.Br, eoif_label.Value);
                                compileState.ILGen.MarkLabel(endlabel);

                                LineInfo li = compileState.PeekLine();
                                if (li.Line[0] == "else")
                                {
                                    goto processnext;
                                }
                            }
                        }
                        else
                        {
                            /* else */
                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
                                ProcessBlock(
                                    compileState,
                                    returnType,
                                    new Dictionary<string, object>(localVars),
                                    labels);
                            }
                            else
                            {
                                ProcessBlock(
                                    compileState,
                                    returnType,
                                    localVars,
                                    labels,
                                    true);
                            }
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
                        }
                        break;
                    #endregion

                    #region New unconditional block
                    case "{": /* new unconditional block */
                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
                        }

                        ProcessBlock(
                            compileState,
                            returnType,
                            new Dictionary<string, object>(localVars),
                            labels);
                        break;
                    #endregion

                    #region End of unconditional/conditional block
                    case "}": /* end unconditional/conditional block or do while */
                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
                        }

                        return;

                    #endregion

                    default:
                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
                        }

                        ProcessStatement(
                            compileState,
                            returnType,
                            0,
                            functionLine.Line.Count - 2,
                            functionLine,
                            localVars,
                            labels);
                        break;
                }
            } while (!isImplicit);
            if (eoif_label.HasValue)
            {
                compileState.ILGen.MarkLabel(eoif_label.Value);
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

            compileState.FunctionBody = functionBody;
            compileState.FunctionLineIndex = 1;
            Dictionary<string, ILLabelInfo> labels = new Dictionary<string, ILLabelInfo>();
            ProcessBlock(
                compileState,
                mb.ReturnType,
                localVars,
                labels);

            /* we have no missing return value check right now, so we simply emit default values in that case */
            if (returnType == typeof(int))
            {
                ilgen.Emit(OpCodes.Ldc_I4_0);
            }
            else if (returnType == typeof(double))
            {
                ilgen.Emit(OpCodes.Ldc_R8, (double)0);
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

            Dictionary<int, string> labelsUndefined = new Dictionary<int, string>();
            foreach (KeyValuePair<string, ILLabelInfo> kvp in labels)
            {
                if(!kvp.Value.IsDefined)
                {
                    foreach (int i in kvp.Value.UsedInLines)
                    {
                        labelsUndefined.Add(i, string.Format("Undefined label '{0}' used", kvp.Key));
                    }
                }
            }
            if(labelsUndefined.Count != 0)
            {
                throw new CompilerException(labelsUndefined);
            }

            if(compileState.HaveMoreLines)
            {
                throw CompilerException(compileState.FunctionBody[compileState.FunctionBody.Count - 1], "Unexpected more lines following");
            }
        }
    }
}
