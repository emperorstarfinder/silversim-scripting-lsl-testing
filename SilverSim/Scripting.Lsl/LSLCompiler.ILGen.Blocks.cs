// SilverSim is distributed under the terms of the
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

#pragma warning disable RCS1029, IDE0018

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
        private void ProcessBlock(
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
                compileState.ILGen.MarkSequencePoint(functionLine.LineNumber, functionLine.LineNumber, 1, 1);
                switch (functionLine.Line[0])
                {
                    #region Label definition
                    case "@":
                        if(compileState.m_BreakContinueLabels.Count != 0 &&
                            compileState.m_BreakContinueLabels[0].CaseRequired)
                        {
                            throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "MissingCaseOrDefaultInSwitchBlock", "missing 'case' or 'default' in 'switch' block"));
                        }
                        if(eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
                        }

                        if (functionLine.Line.Count != 3 || functionLine.Line[2] != ";")
                        {
                            throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "NotAValidLabelDefinition", "not a valid label definition"));
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
                                throw CompilerException(functionLine, string.Format(this.GetLanguageString(compileState.CurrentCulture, "Label0AlreadyDefined", "label '{0}' already defined"), labelName));
                            }
                            else
                            {
                                labels[labelName].IsDefined = true;
                            }
                            compileState.ILGen.MarkLabel(labels[labelName].Label);
                        }
                        break;
                    #endregion

                    #region Control Flow (Loops)
                    /* Control Flow Statements are pre-splitted into own lines with same line number, so we do not have to care about here */
                    case "for":
                        if (compileState.m_BreakContinueLabels.Count != 0 &&
                            compileState.m_BreakContinueLabels[0].CaseRequired)
                        {
                            throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "MissingCaseOrDefaultInSwitchBlock", "missing 'case' or 'default' in 'switch' block"));
                        }
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
                                throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "InvalidForEncountered", "Invalid 'for' encountered"));
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

                            compileState.m_BreakContinueLabels.Insert(0, new BreakContinueLabel
                            {
                                BreakTargetLabel = endlabel,
                                ContinueTargetLabel = looplabel,
                                HaveBreakTarget = true,
                                HaveContinueTarget = true
                            });

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
                            compileState.m_BreakContinueLabels.RemoveAt(0);
                        }
                        break;

                    case "while":
                        if (compileState.m_BreakContinueLabels.Count != 0 &&
                            compileState.m_BreakContinueLabels[0].CaseRequired)
                        {
                            throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "MissingCaseOrDefaultInSwitchBlock", "missing 'case' or 'default' in 'switch' block"));
                        }
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
                                throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "InvalidWhileEncountered", "Invalid 'while' encountered"));
                            }

                            Label looplabel = compileState.ILGen.DefineLabel();
                            Label endlabel = compileState.ILGen.DefineLabel();

                            compileState.m_BreakContinueLabels.Insert(0, new BreakContinueLabel
                            {
                                BreakTargetLabel = endlabel,
                                ContinueTargetLabel = looplabel,
                                HaveBreakTarget = true,
                                HaveContinueTarget = true
                            });

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
                            compileState.m_BreakContinueLabels.RemoveAt(0);
                        }
                        break;

                    case "do":
                        if (compileState.m_BreakContinueLabels.Count != 0 &&
                            compileState.m_BreakContinueLabels[0].CaseRequired)
                        {
                            throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "MissingCaseOrDefaultInSwitchBlock", "missing 'case' or 'default' in 'switch' block"));
                        }
                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
                        }

                        {
                            Label looplabel = compileState.ILGen.DefineLabel();
                            var bcLabel = new BreakContinueLabel
                            {
                                ContinueTargetLabel = looplabel,
                                BreakTargetLabel = compileState.ILGen.DefineLabel(),
                                HaveContinueTarget = true,
                                HaveBreakTarget = true
                            };
                            compileState.m_BreakContinueLabels.Insert(0, bcLabel);

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

                            functionLine = compileState.GetLine(this.GetLanguageString(compileState.CurrentCulture, "MissingWhileForDo", "Missing 'while' for 'do'"));
                            if(functionLine.Line[0] != "while")
                            {
                                throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "MissingWhileForDo", "Missing 'while' for 'do'"));
                            }

                            if (compileState.GetLine(this.GetLanguageString(compileState.CurrentCulture, "InvalidWhileForDo", "Invalid 'while' for 'do'")).Line[0] != ";")
                            {
                                throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "InvalidWhileForDo", "Invalid 'while' for 'do'"));
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
                                throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "InvalidWhileEncountered", "Invalid 'while' encountered"));
                            }

                            ProcessExpression(
                                compileState,
                                typeof(bool),
                                2,
                                endofwhile - 1,
                                functionLine,
                                localVars);
                            compileState.ILGen.Emit(OpCodes.Brtrue, looplabel);

                            compileState.ILGen.MarkLabel(bcLabel.BreakTargetLabel);
                            compileState.m_BreakContinueLabels.RemoveAt(0);
                        }
                        break;

                    #endregion

                    #region Control Flow (Switch)
                    case "switch":
                        if (compileState.m_BreakContinueLabels.Count != 0 &&
                            compileState.m_BreakContinueLabels[0].CaseRequired)
                        {
                            throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "MissingCaseOrDefaultInSwitchBlock", "missing 'case' or 'default' in 'switch' block"));
                        }
                        if (!compileState.LanguageExtensions.EnableSwitchBlock)
                        {
                            goto default;
                        }

                        compileState.ILGen.BeginScope();
                        var switchBcLabel = new BreakContinueLabel
                        {
                            CaseRequired = true,
                            BreakTargetLabel = compileState.ILGen.DefineLabel(),
                            NextCaseLabel = compileState.ILGen.DefineLabel(),
                            HaveBreakTarget = true
                        };
                        compileState.m_BreakContinueLabels.Insert(0, switchBcLabel);

                        Type switchVarType = ProcessExpressionToAnyType(
                                compileState,
                                2,
                                functionLine.Line.Count - 3,
                                functionLine,
                                localVars);
                        if(switchVarType == typeof(AnArray))
                        {
                            throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "ListValueNotSupportedForSwitchBlock", "List value not supported for 'switch' block"));
                        }
                        LocalBuilder switchLb = compileState.ILGen.DeclareLocal(switchVarType);
                        compileState.ILGen.Emit(OpCodes.Stloc, switchLb);
                        switchBcLabel.SwitchValueLocal = switchLb;

                        ProcessBlock(
                            compileState,
                            returnType,
                            new Dictionary<string, object>(localVars),
                            labels);
                        compileState.ILGen.Emit(OpCodes.Br, switchBcLabel.BreakTargetLabel);
                        compileState.ILGen.MarkLabel(switchBcLabel.NextCaseLabel);
                        if (switchBcLabel.HaveDefaultCase)
                        {
                            compileState.ILGen.Emit(OpCodes.Br, switchBcLabel.DefaultLabel);
                        }
                        else
                        {
                            compileState.ILGen.Emit(OpCodes.Nop);
                        }
                        compileState.ILGen.MarkLabel(switchBcLabel.BreakTargetLabel);
                        compileState.m_BreakContinueLabels.RemoveAt(0);
                        compileState.ILGen.EndScope();
                        break;

                    #endregion

                    #region Control Flow (Conditions)
                    /* Control Flow Statements are pre-splitted into own lines with same line number, so we do not have to care about here */
                    case "if":
                        if (compileState.m_BreakContinueLabels.Count != 0 &&
                            compileState.m_BreakContinueLabels[0].CaseRequired)
                        {
                            throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "MissingCaseOrDefaultInSwitchBlock", "missing 'case' or 'default' in 'switch' block"));
                        }
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
                                throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "InvalidIfEncountered", "Invalid 'if' encountered"));
                            }

                            ProcessExpression(
                                compileState,
                                typeof(bool),
                                2,
                                endofif - 1,
                                functionLine,
                                localVars);
                            compileState.ILGen.Emit(OpCodes.Brfalse, endlabel);
                            if (compileState.m_BreakContinueLabels.Count == 0)
                            {
                                compileState.m_BreakContinueLabels.Add(new BreakContinueLabel());
                            }
                            else
                            {
                                compileState.m_BreakContinueLabels.Insert(0, new BreakContinueLabel(compileState.m_BreakContinueLabels[0]));
                            }
                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
                                ProcessBlock(
                                    compileState,
                                    returnType,
                                    new Dictionary<string, object>(localVars),
                                    labels);
                                compileState.m_BreakContinueLabels.RemoveAt(0);

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
                                compileState.m_BreakContinueLabels.RemoveAt(0);

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
                        if (compileState.m_BreakContinueLabels.Count != 0 &&
                            compileState.m_BreakContinueLabels[0].CaseRequired)
                        {
                            throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "MissingCaseOrDefaultInSwitchBlock", "missing 'case' or 'default' in 'switch' block"));
                        }
                        if (!eoif_label.HasValue)
                        {
                            throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "NoMatchingIfFoundForElse", "No matching 'if' found for 'else'"));
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
                                throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "InvalidElseIfEncountered", "Invalid 'else if' encountered"));
                            }

                            ProcessExpression(
                                compileState,
                                typeof(bool),
                                3,
                                endofif - 1,
                                functionLine,
                                localVars);
                            compileState.ILGen.Emit(OpCodes.Brfalse, endlabel);

                            if (compileState.m_BreakContinueLabels.Count == 0)
                            {
                                compileState.m_BreakContinueLabels.Add(new BreakContinueLabel());
                            }
                            else
                            {
                                compileState.m_BreakContinueLabels.Insert(0, new BreakContinueLabel(compileState.m_BreakContinueLabels[0]));
                            }

                            if (functionLine.Line[functionLine.Line.Count - 1] == "{")
                            {
                                ProcessBlock(
                                    compileState,
                                    returnType,
                                    new Dictionary<string, object>(localVars),
                                    labels);

                                compileState.m_BreakContinueLabels.RemoveAt(0);

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
                                compileState.m_BreakContinueLabels.RemoveAt(0);

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
                        if (compileState.m_BreakContinueLabels.Count != 0 &&
                            compileState.m_BreakContinueLabels[0].CaseRequired)
                        {
                            throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "MissingCaseOrDefaultInSwitchBlock", "missing 'case' or 'default' in 'switch' block"));
                        }
                        if (eoif_label.HasValue)
                        {
                            compileState.ILGen.MarkLabel(eoif_label.Value);
                            eoif_label = null;
                        }

                        if (compileState.m_BreakContinueLabels.Count == 0)
                        {
                            compileState.m_BreakContinueLabels.Add(new BreakContinueLabel());
                        }
                        else
                        {
                            compileState.m_BreakContinueLabels.Insert(0, new BreakContinueLabel(compileState.m_BreakContinueLabels[0]));
                        }

                        ProcessBlock(
                            compileState,
                            returnType,
                            new Dictionary<string, object>(localVars),
                            labels);
                        compileState.m_BreakContinueLabels.RemoveAt(0);
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
                        Type targetType;
                        if (compileState.TryGetValidVarType(functionLine.Line[0], out targetType))
                        {
                            if (isImplicit)
                            {
                                throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "VariableDeclarationNotAllowedWithinConditionalStatementWithoutBlock", "variable declaration not allowed within conditional statement without block"));
                            }
                            if (compileState.m_BreakContinueLabels.Count != 0 &&
                                compileState.m_BreakContinueLabels[0].CaseRequired)
                            {
                                throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "MissingCaseOrDefaultInSwitchBlock", "missing 'case' or 'default' in 'switch' block"));
                            }

                            if (eoif_label.HasValue)
                            {
                                compileState.ILGen.MarkLabel(eoif_label.Value);
                                eoif_label = null;
                            }

                            lb = compileState.ILGen.DeclareLocal(targetType);
                            if (compileState.EmitDebugSymbols)
                            {
                                lb.SetLocalSymInfo(functionLine.Line[1]);
                            }
                            localVars[functionLine.Line[1]] = lb;
                            if (functionLine.Line[2] != ";")
                            {
                                ResultIsModifiedEnum modified = ProcessExpression(
                                    compileState,
                                    targetType,
                                    3,
                                    functionLine.Line.Count - 2,
                                    functionLine,
                                    localVars);
                                if(modified == ResultIsModifiedEnum.Yes)
                                {
                                    /* skip operation as it is modified */
                                }
                                else if(targetType == typeof(AnArray) || Attribute.GetCustomAttribute(targetType, typeof(APICloneOnAssignmentAttribute)) != null)
                                {
                                    /* keep LSL semantics valid */
                                    compileState.ILGen.Emit(OpCodes.Newobj, targetType.GetConstructor(new Type[] { targetType }));
                                }
                            }
                            else if(targetType == typeof(int))
                            {
                                compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                            }
                            else if (targetType == typeof(long))
                            {
                                compileState.ILGen.Emit(OpCodes.Ldc_I8, (long)0);
                            }
                            else if (targetType == typeof(double))
                            {
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)0);
                            }
                            else if (targetType == typeof(string))
                            {
                                compileState.ILGen.Emit(OpCodes.Ldstr, string.Empty);
                            }
                            else if (targetType == typeof(Quaternion))
                            {
                                compileState.ILGen.Emit(OpCodes.Ldsfld, typeof(Quaternion).GetField("Identity"));
                            }
                            else if(targetType.IsValueType)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldloca, lb);
                                compileState.ILGen.Emit(OpCodes.Initobj, targetType);
                                break;
                            }
                            else
                            {
                                ConstructorInfo cInfo = targetType.GetConstructor(Type.EmptyTypes);
                                if(cInfo == null)
                                {
                                    throw CompilerException(functionLine, this.GetLanguageString(compileState.CurrentCulture, "InternalError", "Internal Error"));
                                }
                                compileState.ILGen.Emit(OpCodes.Newobj, cInfo);
                            }
                            compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        }
                        else
                        {
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
                        }
                        break;
                }
            } while (!isImplicit);
            if (eoif_label.HasValue)
            {
                compileState.ILGen.MarkLabel(eoif_label.Value);
            }
        }

        private void ProcessFunction(
            CompileState compileState,
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            MethodBuilder mb,
            ILGenDumpProxy ilgen,
            List<LineInfo> functionBody,
            Dictionary<string, object> localVars)
        {
            Type returnType;
            List<string> functionDeclaration = functionBody[0].Line;
            int functionStart = 2;
            compileState.m_BreakContinueLabels.Clear();
            compileState.ScriptTypeBuilder = scriptTypeBuilder;
            compileState.StateTypeBuilder = stateTypeBuilder;
            compileState.ILGen = ilgen;

            if(!compileState.ApiInfo.Types.TryGetValue(functionDeclaration[0], out returnType))
            {
                functionStart = 1;
                returnType = typeof(void);
            }

            if(functionDeclaration[functionStart + 1] == "this")
            {
                /* special keyword to declare custom member function */
            }

            int paramidx = 0;
            while (functionDeclaration[++functionStart] != ")")
            {
                if (functionDeclaration[functionStart] == ",")
                {
                    ++functionStart;
                }
                Type t;
                if (!compileState.TryGetValidVarType(functionDeclaration[functionStart++], out t))
                {
                    throw CompilerException(functionBody[0], this.GetLanguageString(compileState.CurrentCulture, "InternalError", "Internal Error"));
                }
                /* parameter name and type in order */
                localVars[functionDeclaration[functionStart]] = new ILParameterInfo(t, ++paramidx);
            }

            compileState.FunctionBody = functionBody;
            compileState.FunctionLineIndex = 1;
            var labels = new Dictionary<string, ILLabelInfo>();
            ProcessBlock(
                compileState,
                mb.ReturnType,
                localVars,
                labels);

            if (!ilgen.GeneratedRetAtLast)
            {
                /* we have no missing return value check right now, so we simply emit default values in that case */
                if (returnType == typeof(int))
                {
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                }
                else if(returnType == typeof(long))
                {
                    ilgen.Emit(OpCodes.Ldc_I8, (long)0);
                }
                else if (returnType == typeof(double))
                {
                    ilgen.Emit(OpCodes.Ldc_R8, (double)0);
                }
                else if (returnType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Ldstr, string.Empty);
                }
                else if (returnType == typeof(Quaternion))
                {
                    ilgen.Emit(OpCodes.Ldsfld, typeof(Quaternion).GetField("Identity"));
                }
                else if(returnType == typeof(void))
                {
                    /* no return value */
                }
                else if(returnType.IsValueType)
                {
                    LocalBuilder lb = compileState.ILGen.DeclareLocal(returnType);
                    compileState.ILGen.Emit(OpCodes.Ldloca, lb);
                    compileState.ILGen.Emit(OpCodes.Initobj, returnType);
                    compileState.ILGen.Emit(OpCodes.Ldloc, lb);
                }
                else
                {
                    ConstructorInfo cInfo = returnType.GetConstructor(Type.EmptyTypes);
                    if(cInfo == null)
                    {
                        throw CompilerException(functionBody[0], this.GetLanguageString(compileState.CurrentCulture, "InternalError", "Internal Error"));
                    }
                    ilgen.Emit(OpCodes.Newobj, cInfo);
                }
                ilgen.Emit(OpCodes.Ret);
            }

            var labelsUndefined = new Dictionary<int, string>();
            foreach (KeyValuePair<string, ILLabelInfo> kvp in labels)
            {
                if(!kvp.Value.IsDefined)
                {
                    foreach (int i in kvp.Value.UsedInLines)
                    {
                        labelsUndefined.Add(i, string.Format(this.GetLanguageString(compileState.CurrentCulture, "UndefinedLabel0Used", "Undefined label '{0}' used"), kvp.Key));
                    }
                }
            }
            if(labelsUndefined.Count != 0)
            {
                throw new CompilerException(labelsUndefined);
            }

            if(compileState.HaveMoreLines)
            {
                throw CompilerException(compileState.FunctionBody[compileState.FunctionBody.Count - 1], this.GetLanguageString(compileState.CurrentCulture, "UnexpectedMoreLinesFollowing", "Unexpected more lines following"));
            }
        }
    }
}
