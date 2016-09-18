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

        void GenerateTypedEqualOperator(CompileState cs, LineInfo functionLine, Type t)
        {
            if(t == typeof(int) || t == typeof(double))
            {
                cs.ILGen.Emit(OpCodes.Ceq);
            }
            else if(t == typeof(string) ||
                t == typeof(Quaternion) ||
                t == typeof(Vector3) ||
                t == typeof(LSLKey))
            {
                cs.ILGen.Emit(OpCodes.Callvirt, t.GetMethod("Equals", new Type[] { t }));
            }
            else
            {
                throw CompilerException(functionLine, "Internal Compiler Error");
            }
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
            if(functionLine.Line[startAt] == "case" && compileState.LanguageExtensions.EnableSwitchBlock)
            {
                if(compileState.m_BreakContinueLabels.Count == 0 ||
                    compileState.m_BreakContinueLabels[0].SwitchValueLocal == null)
                {
                    throw CompilerException(functionLine, "'case' not in 'switch' block");
                }

                BreakContinueLabel bc = compileState.m_BreakContinueLabels[0];
                Label ftLabel = compileState.ILGen.DefineLabel();
                if (!bc.CaseRequired)
                {
                    compileState.ILGen.Emit(OpCodes.Br, ftLabel);
                }
                compileState.ILGen.MarkLabel(bc.NextCaseLabel);
                bc.NextCaseLabel = compileState.ILGen.DefineLabel();
                bc.CaseRequired = false;

                compileState.ILGen.Emit(OpCodes.Ldloc, bc.SwitchValueLocal);
                ProcessExpression(
                                compileState,
                                bc.SwitchValueLocal.LocalType,
                                1,
                                functionLine.Line.Count - 2,
                                functionLine,
                                localVars);
                GenerateTypedEqualOperator(compileState, functionLine, bc.SwitchValueLocal.LocalType);
                compileState.ILGen.Emit(OpCodes.Brfalse, bc.NextCaseLabel);

                Label lb = compileState.ILGen.DefineLabel();
                compileState.ILGen.MarkLabel(ftLabel);
            }
            else if(functionLine.Line[startAt] == "default" && compileState.LanguageExtensions.EnableSwitchBlock)
            {
                if (compileState.m_BreakContinueLabels.Count == 0 ||
                    compileState.m_BreakContinueLabels[0].SwitchValueLocal == null)
                {
                    throw CompilerException(functionLine, "'default' not in 'switch' block");
                }

                BreakContinueLabel bc = compileState.m_BreakContinueLabels[0];

                Label ftLabel = compileState.ILGen.DefineLabel();
                if (bc.CaseRequired)
                {
                    compileState.ILGen.Emit(OpCodes.Br, bc.NextCaseLabel);
                }
                else
                {
                    compileState.ILGen.Emit(OpCodes.Br, ftLabel);
                }

                bc.DefaultLabel = ftLabel;
                bc.HaveDefaultCase = true;
                bc.CaseRequired = false;
                compileState.ILGen.MarkLabel(ftLabel);

            }
            else if (compileState.m_BreakContinueLabels.Count != 0 &&
                compileState.m_BreakContinueLabels[0].CaseRequired)
            {
                throw CompilerException(functionLine, "missing 'case' or 'default' in 'switch' block");
            }
            else if (functionLine.Line[startAt] == "@")
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
            #region Break & Continue
            else if(functionLine.Line[startAt] == "break" &&
                (compileState.LanguageExtensions.EnableSwitchBlock || compileState.LanguageExtensions.EnableBreakContinueStatement))
            {
                if (compileState.m_BreakContinueLabels.Count == 0 || !compileState.m_BreakContinueLabels[0].HaveBreakTarget)
                {
                    if (compileState.LanguageExtensions.EnableSwitchBlock)
                    {
                        throw CompilerException(functionLine, "'continue' not in 'for'/'while'/'do while'/'switch' block");
                    }
                    else
                    {
                        throw CompilerException(functionLine, "'continue' not in 'for'/'while'/'do while' block");
                    }
                }

                compileState.ILGen.Emit(OpCodes.Br, compileState.m_BreakContinueLabels[0].BreakTargetLabel);
            }
            else if (functionLine.Line[startAt] == "continue" &&
                compileState.LanguageExtensions.EnableBreakContinueStatement)
            {
                if(compileState.m_BreakContinueLabels.Count == 0 || !compileState.m_BreakContinueLabels[0].HaveContinueTarget)
                {
                    throw CompilerException(functionLine, "'continue' not in 'for'/'while'/'do while' block");
                }

                compileState.ILGen.Emit(OpCodes.Br, compileState.m_BreakContinueLabels[0].ContinueTargetLabel);
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
                else
                {
                    ProcessExpression(
                        compileState,
                        returnType,
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
