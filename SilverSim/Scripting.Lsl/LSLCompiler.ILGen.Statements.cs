﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

#pragma warning disable RCS1029

using SilverSim.Scene.Types.Script;
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

        private void GenerateTypedEqualOperator(CompileState cs, LineInfo functionLine, Type t)
        {
            if(t == typeof(int) || t == typeof(double) || t == typeof(long) || t == typeof(char))
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
                throw new CompilerException(functionLine.FirstTokenLineNumber, "Internal Compiler Error");
            }
        }

        private void ProcessStatement(
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
                    throw new CompilerException(functionLine.Line[startAt].LineNumber, this.GetLanguageString(compileState.CurrentCulture, "CaseNotInSwitchBlock", "'case' not in 'switch' block"));
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

                compileState.ILGen.MarkLabel(ftLabel);
            }
            else if(functionLine.Line[startAt] == "default" && compileState.LanguageExtensions.EnableSwitchBlock)
            {
                if (compileState.m_BreakContinueLabels.Count == 0 ||
                    compileState.m_BreakContinueLabels[0].SwitchValueLocal == null)
                {
                    throw new CompilerException(functionLine.Line[startAt].LineNumber, this.GetLanguageString(compileState.CurrentCulture, "DefaultNotInSwitchBlock", "'default' not in 'switch' block"));
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
                throw new CompilerException(functionLine.Line[startAt].LineNumber, this.GetLanguageString(compileState.CurrentCulture, "MissingCaseOrDefaultInSwitchBlock", "missing 'case' or 'default' in 'switch' block"));
            }
            else if (functionLine.Line[startAt] == "@")
            {
                throw new CompilerException(functionLine.Line[startAt].LineNumber, this.GetLanguageString(compileState.CurrentCulture, "InvalidLabelDeclaration", "Invalid label declaration"));
            }
            #region Jump to label
            else if (functionLine.Line[startAt] == "jump")
            {
                if (functionLine.Line.Count != startAt + 3)
                {
                    throw new CompilerException(functionLine.Line[startAt].LineNumber, this.GetLanguageString(compileState.CurrentCulture, "InvalidJumpStatement", "Invalid jump statement"));
                }
                if (!labels.ContainsKey(functionLine.Line[1]))
                {
                    Label label = compileState.ILGen.DefineLabel();
                    labels[functionLine.Line[1]] = new ILLabelInfo(label, false);
                }
                labels[functionLine.Line[1]].UsedInLines.Add(functionLine.Line[1].LineNumber);

                compileState.ILGen.Emit(OpCodes.Br, labels[functionLine.Line[1]].Label);
                return;
            }
            #endregion
            #region Break & Continue
            else if(functionLine.Line[startAt] == "break" &&
                (compileState.LanguageExtensions.EnableSwitchBlock || compileState.LanguageExtensions.EnableBreakContinueStatement))
            {
                uint n = 1;
                if(functionLine.Line[startAt + 1] != ";")
                {
                    n = uint.Parse(functionLine.Line[startAt + 1]);
                }

                int labelidx;
                for(labelidx = 0; labelidx < compileState.m_BreakContinueLabels.Count; ++labelidx)
                {
                    if (!compileState.m_BreakContinueLabels[labelidx].HaveBreakTarget)
                    {
                        continue;
                    }
                    if(--n == 0)
                    {
                        break;
                    }
                }

                if (labelidx == compileState.m_BreakContinueLabels.Count)
                {
                    if (compileState.LanguageExtensions.EnableSwitchBlock)
                    {
                        throw new CompilerException(functionLine.Line[startAt].LineNumber, this.GetLanguageString(compileState.CurrentCulture, "BreakNotInForWhileDoWhileSwitchBlock", "'break' not in 'for'/'while'/'do while'/'switch' block"));
                    }
                    else
                    {
                        throw new CompilerException(functionLine.Line[startAt].LineNumber, this.GetLanguageString(compileState.CurrentCulture, "BreakNotInForWhileDoWhileBlock", "'break' not in 'for'/'while'/'do while' block"));
                    }
                }

                compileState.ILGen.Emit(OpCodes.Br, compileState.m_BreakContinueLabels[labelidx].BreakTargetLabel);
            }
            else if (functionLine.Line[startAt] == "continue" &&
                compileState.LanguageExtensions.EnableBreakContinueStatement)
            {
                uint n = 1;
                if (functionLine.Line[startAt + 1] != ";")
                {
                    n = uint.Parse(functionLine.Line[startAt + 1]);
                }

                int labelidx;
                for (labelidx = 0; labelidx < compileState.m_BreakContinueLabels.Count; ++labelidx)
                {
                    if (!compileState.m_BreakContinueLabels[labelidx].HaveContinueTarget)
                    {
                        continue;
                    }
                    if (--n == 0)
                    {
                        break;
                    }
                }

                if (labelidx == compileState.m_BreakContinueLabels.Count)
                {
                    throw new CompilerException(functionLine.Line[startAt].LineNumber, this.GetLanguageString(compileState.CurrentCulture, "ContinueNotInForWhileDoWhileBlock", "'continue' not in 'for'/'while'/'do while' block"));
                }

                compileState.ILGen.Emit(OpCodes.Br, compileState.m_BreakContinueLabels[labelidx].ContinueTargetLabel);
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
                else if(functionLine.Line.Count - 2 < 1)
                {
                    throw new CompilerException(functionLine.Line[startAt].LineNumber, this.GetLanguageString(compileState.CurrentCulture, "ReturnIsMissingReturnValue", "'return' is missing a return value."));
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
                string stateName = functionLine.Line[startAt + 1];
                /* when same state, the state instruction compiles to nop according to wiki */
                if(!compileState.m_States.ContainsKey(stateName))
                {
                    throw new CompilerException(functionLine.Line[startAt + 1].LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "StateName0IsNotValid", "State name '{0}' is not valid."), stateName));
                }
                compileState.ILGen.Emit(OpCodes.Ldstr, stateName);
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
