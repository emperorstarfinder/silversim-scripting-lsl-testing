// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        public static void RequestStateChange(string newState)
        {
            throw new ChangeStateException(newState);
        }

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
                return;
            }
            #endregion
            #region Empty Statement
            else if(functionLine.Line[startAt] == ";")
            {
                return;
            }
            #endregion
            #region State Change
            else if (functionLine.Line[startAt] == "state")
            {
                /* when same state, the state instruction compiles to nop according to wiki */
                compileState.ILGen.Emit(OpCodes.Ldstr, functionLine.Line[startAt + 1]);
                MethodInfo mi = typeof(LSLCompiler).GetMethod("RequestStateChange", BindingFlags.Public | BindingFlags.Static);
                compileState.ILGen.Emit(OpCodes.Call, mi);
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
