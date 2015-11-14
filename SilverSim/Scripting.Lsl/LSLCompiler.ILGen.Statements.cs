// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        void ProcessStatement(
            CompileState compileState,
            Type returnType,
            int startAt,
            int endAt,
            LineInfo functionLine,
            Dictionary<string, object> localVars,
            Dictionary<string, ILLabelInfo> labels)
        {
            if (functionLine.Line[startAt] == "@")
            {
                throw CompilerException(functionLine, "Invalid label declaration");
            }
            #region Jump to label
            else if (functionLine.Line[startAt] == "jump")
            {
                if (functionLine.Line.Count <= startAt + 2)
                {
                    throw CompilerException(functionLine, "Invalid jump statement");
                }
                if (!labels.ContainsKey(functionLine.Line[1]))
                {
                    Label label = compileState.ILGen.DefineLabel();
                    labels[functionLine.Line[1]] = new ILLabelInfo(label, false);
                }
                labels[functionLine.Line[1]].UsedInLines.Add(functionLine.LineNumber);

                compileState.ILGen.Emit(OpCodes.Br, labels[functionLine.Line[1]].Label);
                compileState.PopControlFlowImplicit(functionLine.LineNumber);
                return;
            }
            #endregion
            #region Return from function
            else if (functionLine.Line[startAt] == "return")
            {
                if (returnType == typeof(void))
                {
                    if (functionLine.Line[1] != ";")
                    {
                        ProcessExpression(
                            compileState,
                            typeof(void),
                            1,
                            functionLine.Line.Count - 2,
                            functionLine,
                            localVars);
                    }
                }
                else if (returnType == typeof(int))
                {
                    ProcessExpression(
                        compileState,
                        typeof(int),
                        1,
                        functionLine.Line.Count - 2,
                        functionLine,
                        localVars);
                }
                else if (returnType == typeof(string))
                {
                    ProcessExpression(
                        compileState,
                        typeof(string),
                        1,
                        functionLine.Line.Count - 2,
                        functionLine,
                        localVars);
                }
                else if (returnType == typeof(double))
                {
                    ProcessExpression(
                        compileState,
                        typeof(double),
                        1,
                        functionLine.Line.Count - 2,
                        functionLine,
                        localVars);
                }
                else if (returnType == typeof(AnArray))
                {
                    ProcessExpression(
                        compileState,
                        typeof(AnArray),
                        1,
                        functionLine.Line.Count - 2,
                        functionLine,
                        localVars);
                }
                else if (returnType == typeof(Vector3))
                {
                    ProcessExpression(
                        compileState,
                        typeof(Vector3),
                        1,
                        functionLine.Line.Count - 2,
                        functionLine,
                        localVars);
                }
                else if (returnType == typeof(Quaternion))
                {
                    ProcessExpression(
                        compileState,
                        typeof(Quaternion),
                        1,
                        functionLine.Line.Count - 2,
                        functionLine,
                        localVars);
                }
                else if (returnType == typeof(LSLKey))
                {
                    ProcessExpression(
                        compileState,
                        typeof(LSLKey),
                        1,
                        functionLine.Line.Count - 2,
                        functionLine,
                        localVars);
                }
                compileState.ILGen.Emit(OpCodes.Ret);
                compileState.PopControlFlowImplicit(functionLine.LineNumber);
                return;
            }
            #endregion
            #region State Change
            else if (functionLine.Line[startAt] == "state")
            {
                /* when same state, the state instruction compiles to nop according to wiki */
                compileState.ILGen.Emit(OpCodes.Ldstr, functionLine.Line[1]);
                compileState.ILGen.Emit(OpCodes.Newobj, typeof(ChangeStateException).GetConstructor(new Type[1] { typeof(string) }));
                compileState.ILGen.Emit(OpCodes.Throw);
                compileState.PopControlFlowImplicit(functionLine.LineNumber);
                return;
            }
            #endregion
            #region Assignment =
            else if (functionLine.Line[startAt + 1] == "=")
            {
                string varName = functionLine.Line[startAt];
                /* variable assignment */
                object v = localVars[varName];
                ProcessExpression(
                    compileState,
                    GetVarType(v),
                    startAt + 2,
                    endAt,
                    functionLine,
                    localVars);
                SetVarFromStack(compileState, v, functionLine.LineNumber);
            }
            #endregion
            #region Component Access
            else if (functionLine.Line[startAt + 1] == ".")
            {
                /* component access */
                if (startAt != 0)
                {
                    throw CompilerException(functionLine, "Invalid assignment");
                }
                else
                {
                    string varName = functionLine.Line[startAt];
                    object o = localVars[varName];
                    Type varType = GetVarType(o);
                    compileState.ILGen.BeginScope();
                    LocalBuilder lb_struct = compileState.ILGen.DeclareLocal(varType);
                    GetVarToStack(compileState, o);
                    compileState.ILGen.Emit(OpCodes.Stloc, lb_struct);
                    string fieldName;
                    if (varType == typeof(Vector3))
                    {
                        switch (functionLine.Line[startAt + 2])
                        {
                            case "x":
                                fieldName = "X";
                                break;

                            case "y":
                                fieldName = "Y";
                                break;

                            case "z":
                                fieldName = "Z";
                                break;

                            default:
                                throw CompilerException(functionLine, "vector does not have member " + functionLine.Line[startAt + 2]);
                        }

                    }
                    else if (varType == typeof(Quaternion))
                    {
                        switch (functionLine.Line[startAt + 2])
                        {
                            case "x":
                                fieldName = "X";
                                break;

                            case "y":
                                fieldName = "Y";
                                break;

                            case "z":
                                fieldName = "Z";
                                break;

                            case "s":
                                fieldName = "W";
                                break;

                            default:
                                throw CompilerException(functionLine, "quaternion does not have member " + functionLine.Line[startAt + 2]);
                        }
                    }
                    else
                    {
                        throw CompilerException(functionLine, "Type " + MapType(varType) + " does not have accessible components");
                    }

                    compileState.ILGen.Emit(OpCodes.Ldloca, lb_struct);
                    if (functionLine.Line[startAt + 3] != "=")
                    {
                        compileState.ILGen.Emit(OpCodes.Dup, lb_struct);
                        compileState.ILGen.Emit(OpCodes.Ldfld, varType.GetField(fieldName));
                    }
                    ProcessExpression(
                        compileState,
                        typeof(double),
                        startAt + 4,
                        endAt,
                        functionLine,
                        localVars);

                    switch (functionLine.Line[startAt + 3])
                    {
                        case "=":
                            compileState.ILGen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                            break;

                        case "+=":
                            compileState.ILGen.Emit(OpCodes.Add);
                            compileState.ILGen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                            break;

                        case "-=":
                            compileState.ILGen.Emit(OpCodes.Sub);
                            compileState.ILGen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                            break;

                        case "*=":
                            compileState.ILGen.Emit(OpCodes.Mul);
                            compileState.ILGen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                            break;

                        case "/=":
                            compileState.ILGen.Emit(OpCodes.Div);
                            compileState.ILGen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                            break;

                        case "%=":
                            compileState.ILGen.Emit(OpCodes.Rem);
                            compileState.ILGen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                            break;

                        default:
                            throw CompilerException(functionLine, string.Format("invalid assignment operator '{0}'", functionLine.Line[startAt + 3]));
                    }
                    compileState.ILGen.Emit(OpCodes.Ldloc, lb_struct);
                    SetVarFromStack(compileState, o, functionLine.LineNumber);
                    compileState.ILGen.EndScope();
                }
            }
            #endregion
            #region Assignment Operators += -= *= /= %=
            else if (functionLine.Line[startAt + 1] == "+=")
            {
                if (startAt != 0)
                {
                    throw CompilerException(functionLine, "Invalid assignment");
                }
                else
                {
                    string varName = functionLine.Line[startAt];
                    object v = localVars[varName];
                    Type ret = GetVarToStack(compileState, v);
                    ProcessExpression(
                        compileState,
                        GetVarType(v),
                        startAt + 2,
                        endAt,
                        functionLine,
                        localVars);
                    if (ret == typeof(int) || ret == typeof(double) || ret == typeof(string))
                    {
                        compileState.ILGen.Emit(OpCodes.Add);
                    }
                    else if (ret == typeof(LSLKey) || ret == typeof(AnArray) || ret == typeof(Vector3) || ret == typeof(Quaternion))
                    {
                        compileState.ILGen.Emit(OpCodes.Callvirt, ret.GetMethod("op_Addition", new Type[] { ret, ret }));
                    }
                    else
                    {
                        throw CompilerException(functionLine, string.Format("operator '+=' is not supported for {0}", MapType(ret)));
                    }
                    SetVarFromStack(compileState, v, functionLine.LineNumber);
                }
            }
            else if (functionLine.Line[startAt + 1] == "-=")
            {
                if (startAt != 0)
                {
                    throw CompilerException(functionLine, "Invalid assignment");
                }
                else
                {
                    string varName = functionLine.Line[startAt];
                    object v = localVars[varName];
                    Type ret = GetVarToStack(compileState, v);
                    ProcessExpression(
                        compileState,
                        GetVarType(v),
                        startAt + 2,
                        endAt,
                        functionLine,
                        localVars);
                    if (ret == typeof(int) || ret == typeof(double))
                    {
                        compileState.ILGen.Emit(OpCodes.Sub);
                    }
                    else if (ret == typeof(Vector3) || ret == typeof(Quaternion))
                    {
                        compileState.ILGen.Emit(OpCodes.Callvirt, ret.GetMethod("op_Subtraction", new Type[] { ret, ret }));
                    }
                    else
                    {
                        throw CompilerException(functionLine, string.Format("operator '-=' is not supported for {0}", MapType(ret)));
                    }
                    SetVarFromStack(compileState, v, functionLine.LineNumber);
                }
            }
            else if (functionLine.Line[startAt + 1] == "*=")
            {
                if (startAt != 0)
                {
                    throw CompilerException(functionLine, "Invalid assignment");
                }
                else
                {
                    string varName = functionLine.Line[startAt];
                    object v = localVars[varName];
                    Type ret = GetVarToStack(compileState, v);
                    ProcessExpression(
                        compileState,
                        GetVarType(v),
                        startAt + 2,
                        endAt,
                        functionLine,
                        localVars);
                    if (ret == typeof(int) || ret == typeof(double))
                    {
                        compileState.ILGen.Emit(OpCodes.Mul);
                    }
                    else if (ret == typeof(Vector3) || ret == typeof(Quaternion))
                    {
                        compileState.ILGen.Emit(OpCodes.Callvirt, ret.GetMethod("op_Multiply", new Type[] { ret, ret }));
                    }
                    else
                    {
                        throw CompilerException(functionLine, string.Format("operator '*=' is not supported for {0}", MapType(ret)));
                    }
                    SetVarFromStack(compileState, v, functionLine.LineNumber);
                }
            }
            else if (functionLine.Line[startAt + 1] == "/=")
            {
                if (startAt != 0)
                {
                    throw CompilerException(functionLine, "Invalid assignment");
                }
                else
                {
                    string varName = functionLine.Line[startAt];
                    object v = localVars[varName];
                    Type ret = GetVarToStack(compileState, v);
                    ProcessExpression(
                        compileState,
                        GetVarType(v),
                        startAt + 2,
                        endAt,
                        functionLine,
                        localVars);
                    if (ret == typeof(int) || ret == typeof(double))
                    {
                        compileState.ILGen.Emit(OpCodes.Div);
                    }
                    else if (ret == typeof(Vector3) || ret == typeof(Quaternion))
                    {
                        compileState.ILGen.Emit(OpCodes.Callvirt, ret.GetMethod("op_Division", new Type[] { ret, ret }));
                    }
                    else
                    {
                        throw CompilerException(functionLine, string.Format("operator '/=' is not supported for {0}", MapType(ret)));
                    }
                    SetVarFromStack(compileState, v, functionLine.LineNumber);
                }
            }
            else if (functionLine.Line[startAt + 1] == "%=")
            {
                if (startAt != 0)
                {
                    throw CompilerException(functionLine, "Invalid assignment");
                }
                else
                {
                    string varName = functionLine.Line[startAt];
                    object v = localVars[varName];
                    Type ret = GetVarToStack(compileState, v);
                    ProcessExpression(
                        compileState,
                        GetVarType(v),
                        startAt + 2,
                        endAt,
                        functionLine,
                        localVars);
                    if (ret == typeof(int) || ret == typeof(double))
                    {
                        compileState.ILGen.Emit(OpCodes.Rem);
                    }
                    else
                    {
                        throw CompilerException(functionLine, string.Format("operator '%=' is not supported for {0}", MapType(ret)));
                    }
                    SetVarFromStack(compileState, v, functionLine.LineNumber);
                }
            }
            #endregion
            else
            {
                /* function call no return */
                ProcessExpression(
                    compileState,
                    typeof(void),
                    startAt,
                    endAt,
                    functionLine,
                    localVars);
            }
        }
    }
}
