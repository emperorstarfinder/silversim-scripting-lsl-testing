// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        sealed class BinaryOperatorExpression : IExpressionStackElement
        {
            readonly string m_Operator;
            LocalBuilder m_LeftHandLocal;
            LocalBuilder m_RightHandLocal;
            readonly Tree m_LeftHand;
            Type m_LeftHandType;
            readonly Tree m_RightHand;
            Type m_RightHandType;
            readonly int m_LineNumber;
            enum State
            {
                LeftHand,
                RightHand
            }

            readonly List<State> m_ProcessOrder;
            bool m_HaveBeginScope;

            static readonly Dictionary<string, State[]> m_ProcessOrders = new Dictionary<string, State[]>();

            static BinaryOperatorExpression()
            {
                m_ProcessOrders.Add("+", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("-", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("*", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("/", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("%", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("<<", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add(">>", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("&", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("|", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("&&", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("||", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("^", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("<", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("<=", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("==", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("!=", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add(">", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add(">=", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add(".", new State[] { State.LeftHand });

                m_ProcessOrders.Add("=", new State[] { State.RightHand });
                m_ProcessOrders.Add("+=", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("-=", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("*=", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("/=", new State[] { State.RightHand, State.LeftHand });
                m_ProcessOrders.Add("%=", new State[] { State.RightHand, State.LeftHand });
            }

            void BeginScope(CompileState compileState)
            {
                if(m_HaveBeginScope)
                {
                    throw new CompilerException(m_LineNumber, "Internal Error! Binary operator evaluation scope error");
                }
                m_HaveBeginScope = true;
                compileState.ILGen.BeginScope();
            }

            LocalBuilder DeclareLocal(CompileState compileState, Type localType)
            {
                if(!m_HaveBeginScope)
                {
                    compileState.ILGen.BeginScope();
                }
                m_HaveBeginScope = true;
                return compileState.ILGen.DeclareLocal(localType);
            }

            ReturnTypeException Return(CompileState compileState, Type t)
            {
                if(m_HaveBeginScope)
                {
                    compileState.ILGen.EndScope();
                }
                return new ReturnTypeException(t, m_LineNumber);
            }

            public BinaryOperatorExpression(
                LSLCompiler lslCompiler,
                CompileState compileState,
                Tree functionTree,
                int lineNumber,
                Dictionary<string, object> localVars)
            {
                m_LineNumber = lineNumber;
                m_LeftHand = functionTree.SubTree[0];
                m_RightHand = functionTree.SubTree[1];
                m_Operator = functionTree.Entry;
                m_ProcessOrder = new List<State>(m_ProcessOrders[m_Operator]);
                if(m_Operator == "=")
                {
                    if (m_LeftHand.Type == Tree.EntryType.OperatorBinary && m_LeftHand.Entry == ".")
                    {
                        if (m_LeftHand.SubTree[0].Type != Tree.EntryType.Variable)
                        {
                            throw new CompilerException(m_LineNumber, "l-value of operator '=' is not a variable");
                        }
                        object varInfo = localVars[m_LeftHand.SubTree[0].Entry];
                        m_LeftHandType = GetVarType(varInfo);
                        if(m_LeftHandType != typeof(Vector3) && m_LeftHandType != typeof(Quaternion))
                        {
                            throw new CompilerException(m_LineNumber, "operator '.' not supported for " + MapType(m_LeftHandType));
                        }
                        switch(m_LeftHand.SubTree[1].Entry)
                        {
                            case "x":
                            case "y":
                            case "z":
                                break;

                            case "s":
                                if(m_LeftHandType != typeof(Quaternion))
                                {
                                    throw new CompilerException(m_LineNumber, "invalid member access 's' to vector");
                                }
                                break;

                            default:
                                throw new CompilerException(m_LineNumber, string.Format("invalid member access '{0}' to {1}", m_LeftHand.SubTree[1].Entry, MapType(m_LeftHandType)));
                        }
                        m_LeftHandType = typeof(double);
                    }
                    else if (m_LeftHand.Type != Tree.EntryType.Variable)
                    {
                        throw new CompilerException(m_LineNumber, "l-value of operator '=' is not a variable");
                    }
                    else
                    {
                        object varInfo = localVars[m_LeftHand.Entry];
                        m_LeftHandType = GetVarType(varInfo);
                    }
                }
                else if(m_Operator != "=" && m_Operator != ".")
                {
                    /* evaluation is reversed, so we have to sort them */
                    BeginScope(compileState);
                    switch(m_Operator)
                    {
                        case "+=":
                        case "-=":
                        case "*=":
                        case "/=":
                        case "%=":
                            if (m_LeftHand.Type == Tree.EntryType.OperatorBinary && m_LeftHand.Entry == ".")
                            {
                                if (m_LeftHand.SubTree[0].Type != Tree.EntryType.Variable)
                                {
                                    throw new CompilerException(m_LineNumber, "l-value of operator '=' is not a variable");
                                }
                                object varInfo = localVars[m_LeftHand.SubTree[0].Entry];
                                m_LeftHandType = GetVarType(varInfo);
                                if (m_LeftHandType != typeof(Vector3) && m_LeftHandType != typeof(Quaternion))
                                {
                                    throw new CompilerException(m_LineNumber, "operator '.' not supported for " + MapType(m_LeftHandType));
                                }
                                switch (m_LeftHand.SubTree[1].Entry)
                                {
                                    case "x":
                                    case "y":
                                    case "z":
                                        break;

                                    case "s":
                                        if (m_LeftHandType != typeof(Quaternion))
                                        {
                                            throw new CompilerException(m_LineNumber, "invalid member access 's' to vector");
                                        }
                                        break;

                                    default:
                                        throw new CompilerException(m_LineNumber, string.Format("invalid member access '{0}' to {1}", m_LeftHand.SubTree[1].Entry, MapType(m_LeftHandType)));
                                }
                                m_LeftHandType = typeof(double);
                            }
                            else if (m_LeftHand.Type != Tree.EntryType.Variable)
                            {
                                throw new CompilerException(m_LineNumber, string.Format("l-value of operator '{0}' is not a variable", m_Operator));
                            }
                            else
                            {
                                object varInfo = localVars[m_LeftHand.Entry];
                                m_LeftHandType = GetVarType(varInfo);
                            }
                            break;

                        default:
                            break;
                    }
                }
            }


            public Tree ProcessNextStep(
                LSLCompiler lslCompiler, 
                CompileState compileState, 
                Dictionary<string, object> localVars,
                Type innerExpressionReturn)
            {
                if(null != innerExpressionReturn)
                {
                    switch(m_ProcessOrder[0])
                    {
                        case State.RightHand:
                            if (m_HaveBeginScope)
                            {
                                m_RightHandLocal = DeclareLocal(compileState, innerExpressionReturn);
                                compileState.ILGen.Emit(OpCodes.Stloc, m_RightHandLocal);
                            }
                            m_RightHandType = innerExpressionReturn;
                            break;

                        case State.LeftHand:
                            if (m_HaveBeginScope)
                            {
                                m_LeftHandLocal = DeclareLocal(compileState, innerExpressionReturn);
                                compileState.ILGen.Emit(OpCodes.Stloc, m_LeftHandLocal);
                            }
                            m_LeftHandType = innerExpressionReturn;
                            break;

                        default:
                            break;

                    }
                    m_ProcessOrder.RemoveAt(0);
                }
                
                if(m_ProcessOrder.Count != 0)
                {
                    switch(m_ProcessOrder[0])
                    {
                        case State.RightHand:
                            return m_RightHand;

                        case State.LeftHand:
                            return m_LeftHand;

                        default:
                            throw new CompilerException(m_LineNumber, "Internal Error");
                    }
                }
                else
                {
                    switch(m_Operator)
                    {
                        case ".": 
                            ProcessOperator_Member(
                                compileState,
                                localVars);
                            break;

                        case "=":
                            ProcessOperator_Assignment(
                                compileState,
                                localVars);
                            break;

                        case "+=":
                        case "-=":
                        case "*=":
                        case "/=":
                        case "%=":
                            ProcessOperator_ModifyAssignment(
                                compileState,
                                localVars);
                            break;

                        case "+":
                        case "-":
                        case "*":
                        case "/":
                        case "%":
                        case "^":
                        case "&":
                        case "&&":
                        case "|":
                        case "||":
                        case "!=":
                        case "==":
                        case "<=":
                        case ">=":
                        case ">":
                        case "<":
                        case "<<":
                        case ">>":
                            ProcessOperator_Return(
                                compileState,
                                localVars);
                            break;

                        default:
                            throw new CompilerException(m_LineNumber, string.Format("Internal Error! Unexpected operator '{0}'", m_Operator));
                    }
                    throw new CompilerException(m_LineNumber, string.Format("Internal Error! Unexpected return from operator '{0}' code generator", m_Operator));
                }
            }

            public void ProcessOperator_Member(
                CompileState compileState,
                Dictionary<string, object> localVars)
            {
                if (m_RightHand.Type != Tree.EntryType.Unknown &&
                    m_RightHand.Type != Tree.EntryType.Variable)
                {
                    throw new CompilerException(m_LineNumber, string.Format("'{0}' is not a member of type {1}", m_RightHand.Entry, MapType(m_LeftHandType)));
                }
                if (m_LeftHandType == typeof(Vector3))
                {
                    LocalBuilder lb = DeclareLocal(compileState, m_LeftHandType);
                    compileState.ILGen.Emit(OpCodes.Stloc, lb);
                    compileState.ILGen.Emit(OpCodes.Ldloca, lb);
                    switch (m_RightHand.Entry)
                    {
                        case "x":
                            compileState.ILGen.Emit(OpCodes.Ldfld, typeof(Vector3).GetField("X"));
                            break;

                        case "y":
                            compileState.ILGen.Emit(OpCodes.Ldfld, typeof(Vector3).GetField("Y"));
                            break;

                        case "z":
                            compileState.ILGen.Emit(OpCodes.Ldfld, typeof(Vector3).GetField("Z"));
                            break;

                        default:
                            throw new CompilerException(m_LineNumber, string.Format("'{0}' is not a member of type vector", m_RightHand.Entry));
                    }
                    throw Return(compileState, typeof(double));
                }
                else if (m_LeftHandType == typeof(Quaternion))
                {
                    LocalBuilder lb = DeclareLocal(compileState, m_LeftHandType);
                    compileState.ILGen.Emit(OpCodes.Stloc, lb);
                    compileState.ILGen.Emit(OpCodes.Ldloca, lb);
                    switch (m_RightHand.Entry)
                    {
                        case "x":
                            compileState.ILGen.Emit(OpCodes.Ldfld, typeof(Quaternion).GetField("X"));
                            break;

                        case "y":
                            compileState.ILGen.Emit(OpCodes.Ldfld, typeof(Quaternion).GetField("Y"));
                            break;

                        case "z":
                            compileState.ILGen.Emit(OpCodes.Ldfld, typeof(Quaternion).GetField("Z"));
                            break;

                        case "s":
                            compileState.ILGen.Emit(OpCodes.Ldfld, typeof(Quaternion).GetField("W"));
                            break;

                        default:
                            throw new CompilerException(m_LineNumber, string.Format("'{0}' is not a member of type rotation", m_RightHand.Entry));
                    }
                    throw Return(compileState, typeof(double));
                }
                else
                {
                    throw new CompilerException(m_LineNumber, "operator '.' can only be used on type vector or rotation");
                }
            }

            public void ProcessOperator_Assignment(
                CompileState compileState,
                Dictionary<string, object> localVars)
            {
                if (m_LeftHand.Type == Tree.EntryType.OperatorBinary && m_LeftHand.Entry == ".")
                {
                    object varInfo = localVars[m_LeftHand.SubTree[0].Entry];
                    Type varType = GetVarType(varInfo);
                    ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                    compileState.ILGen.Emit(OpCodes.Dup);
                    compileState.ILGen.BeginScope();
                    LocalBuilder structLb = compileState.ILGen.DeclareLocal(varType);
                    LocalBuilder copyLb = compileState.ILGen.DeclareLocal(typeof(double));
                    compileState.ILGen.Emit(OpCodes.Stloc, copyLb);
                    GetVarToStack(compileState, varInfo);
                    compileState.ILGen.Emit(OpCodes.Stloc, structLb);
                    compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                    compileState.ILGen.Emit(OpCodes.Ldloc, copyLb);
                    FieldInfo fi;
                    switch(m_LeftHand.SubTree[1].Entry)
                    {
                        case "x":
                            fi = varType.GetField("X");
                            break;

                        case "y":
                            fi = varType.GetField("Y");
                            break;

                        case "z":
                            fi = varType.GetField("Z");
                            break;

                        case "s":
                            fi = varType.GetField("W");
                            break;

                        default:
                            fi = null;
                            break;
                    }
                    compileState.ILGen.Emit(OpCodes.Stfld, fi);
                    compileState.ILGen.Emit(OpCodes.Ldloc, structLb);
                    SetVarFromStack(
                        compileState,
                        varInfo,
                        m_LineNumber);
                    compileState.ILGen.EndScope();
                    throw Return(compileState, m_LeftHandType);
                }
                else
                {
                    object varInfo = localVars[m_LeftHand.Entry];
                    m_LeftHandType = GetVarType(varInfo);
                    ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);
                    compileState.ILGen.Emit(OpCodes.Dup);
                    SetVarFromStack(
                        compileState,
                        varInfo,
                        m_LineNumber);
                    throw Return(compileState, m_LeftHandType);
                }
            }

            public void ProcessOperator_ModifyAssignment(
                CompileState compileState,
                Dictionary<string, object> localVars)
            {
                LocalBuilder componentLocal = null;
                object varInfo;
                bool isComponentAccess = false;
                if (m_LeftHand.Type == Tree.EntryType.OperatorBinary && m_LeftHand.Entry == ".")
                {
                    varInfo = localVars[m_LeftHand.SubTree[0].Entry];
                    m_LeftHandType = typeof(double);
                    Type varType = GetVarToStack(compileState, varInfo);
                    componentLocal = DeclareLocal(compileState, varType);
                    compileState.ILGen.Emit(OpCodes.Stloc, componentLocal);
                    isComponentAccess = true;
                }
                else
                {
                    varInfo = localVars[m_LeftHand.Entry];
                }

                compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);

                if((m_LeftHandType == typeof(Vector3) && m_RightHandType == typeof(double)) ||
                    (m_LeftHandType == typeof(Vector3) && m_RightHandType == typeof(Quaternion)) ||
                    (m_LeftHandType == typeof(Quaternion) && m_RightHandType == typeof(double)))
                {
                    /* three combined cases */
                }
                else if((m_LeftHandType == typeof(Vector3) && m_RightHandType == typeof(int)) ||
                    (m_LeftHandType == typeof(Quaternion) && m_RightHandType == typeof(int)))
                {
                    ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                    m_RightHandType = typeof(double);
                }
                else
                {
                    ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);
                }

                MethodInfo mi;
                switch(m_Operator)
                {
                    case "+=":
                        if(typeof(int) == m_LeftHandType || typeof(double) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Sub);
                            break;
                        }
                        if(typeof(string) == m_LeftHandType && typeof(string) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Call, typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }));
                            break;
                        }
                        if(typeof(AnArray) == m_LeftHandType && typeof(LSLKey) == m_RightHandType)
                        {
                            mi = typeof(LSLCompiler).GetMethod("AddKeyToList", new Type[] { m_LeftHandType, m_RightHandType });
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                            break;
                        }

                        mi = m_LeftHandType.GetMethod("op_Addition", new Type[]{m_LeftHandType, m_RightHandType});
                        if (null != mi)
                        {
                            if (mi.ReturnType != m_LeftHandType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '+=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                            }
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '+=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }
                        break;

                    case "-=":
                        if(typeof(int) == m_LeftHandType || typeof(double) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Sub);
                            break;
                        }

                        mi = m_LeftHandType.GetMethod("op_Subtraction", new Type[] { m_LeftHandType, m_RightHandType });
                        if (null != mi)
                        {
                            if (mi.ReturnType != m_LeftHandType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '-=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                            }
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '-=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }
                        break;

                    case "*=":
                        if(typeof(int) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerMultiply", new Type[] { m_LeftHandType, m_RightHandType }));
                            break;
                        }
                        else if(typeof(double) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Mul);
                            break;
                        }

                        mi = m_LeftHandType.GetMethod("op_Multiply", new Type[] { m_LeftHandType, m_RightHandType });
                        if(null != mi)
                        {
                            if(mi.ReturnType != m_LeftHandType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '*=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                            }
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '*=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }
                        break;

                    case "/=":
                        if(typeof(int) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerDivision", new Type[] { m_LeftHandType, m_RightHandType }));
                            break;
                        }
                        else if(typeof(double) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Mul);
                            break;
                        }

                        mi = m_LeftHandType.GetMethod("op_Division", new Type[]{m_LeftHandType, m_RightHandType});
                        if(null != mi)
                        {
                            if(mi.ReturnType != m_LeftHandType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '/=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                            }
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '*=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }
                        break;

                    case "%=":
                        if(typeof(int) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerModulus", new Type[] { m_LeftHandType, m_RightHandType }));
                            break;
                        }
                        else if(typeof(double) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Div);
                            break;
                        }

                        mi = m_LeftHandType.GetMethod("op_Modulus", new Type[]{m_LeftHandType, m_RightHandType});
                        if(null != mi)
                        {
                            if(mi.ReturnType != m_LeftHandType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '%=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                            }
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '%=' not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }
                        break;

                    default:
                        throw new CompilerException(m_LineNumber, string.Format("operator '{0}' unknown", m_Operator));
                }

                compileState.ILGen.Emit(OpCodes.Dup);
                if(isComponentAccess)
                {
                    Type varType = GetVarType(varInfo);
                    LocalBuilder resultLocal = DeclareLocal(compileState, m_LeftHandType);
                    compileState.ILGen.Emit(OpCodes.Stloc, resultLocal);
                    compileState.ILGen.Emit(OpCodes.Ldloca, componentLocal);
                    compileState.ILGen.Emit(OpCodes.Ldloc, resultLocal);
                    string fieldName;
                    switch(m_LeftHand.SubTree[1].Entry)
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
                            if(typeof(Quaternion) != varType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format("'vector' does not have a '{0}' member", m_LeftHand.SubTree[1].Entry));
                            }
                            fieldName = "W";
                            break;

                        default:
                            throw new CompilerException(m_LineNumber, string.Format("'{0}' does not have a '{1}' member", MapType(varType), m_LeftHand.SubTree[1].Entry));
                    }

                    compileState.ILGen.Emit(OpCodes.Stfld, varType.GetField(fieldName));
                    compileState.ILGen.Emit(OpCodes.Ldloc, componentLocal);
                    SetVarFromStack(compileState, varInfo, m_LineNumber);
                    compileState.ILGen.Emit(OpCodes.Ldloc, resultLocal);
                    throw Return(compileState, typeof(double));
                }
                else
                {
                    SetVarFromStack(compileState, varInfo, m_LineNumber);
                    throw Return(compileState, m_LeftHandType);
                }
            }

            [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidComplexMethodsRule")]
            public void ProcessOperator_Return(
                CompileState compileState,
                Dictionary<string, object> localVars)
            {
                MethodInfo mi;
                switch(m_Operator)
                {
                    case "+":
                        if ((m_LeftHandType == typeof(double) || m_LeftHandType == typeof(int)) &&
                            (m_RightHandType == typeof(double) || m_RightHandType == typeof(int)) &&
                            (m_LeftHandType == typeof(double) || m_RightHandType == typeof(double)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Add);
                            throw Return(compileState, typeof(double));
                        }
                        else if (m_LeftHandType == typeof(AnArray) && m_RightHandType == typeof(AnArray))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[] { typeof(AnArray) }));
                            compileState.ILGen.Emit(OpCodes.Dup);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("AddRange", new Type[] { typeof(AnArray) }));
                            throw Return(compileState, typeof(AnArray));
                        }
                        if(m_LeftHandType == typeof(AnArray))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[] { typeof(AnArray) }));
                            compileState.ILGen.Emit(OpCodes.Dup);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            if (typeof(int) == m_RightHandType || typeof(double) == m_RightHandType || typeof(string) == m_RightHandType)
                            {
                                compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { m_RightHandType }));
                            }
                            else if(typeof(LSLKey) == m_RightHandType)
                            {
                                compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                            }
                            else
                            {
                                compileState.ILGen.Emit(OpCodes.Box, m_RightHandType);
                                compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                            }
                            throw Return(compileState, typeof(AnArray));
                        }
                        else if(m_RightHandType == typeof(AnArray))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessCasts(compileState, typeof(AnArray), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Dup);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("AddRange", new Type[] { m_RightHandType }));
                            throw Return(compileState, typeof(AnArray));
                        }
                        else if (m_LeftHandType == typeof(int) && m_RightHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Add);
                            throw Return(compileState, m_LeftHandType);
                        }
                        else if(m_LeftHandType == typeof(string))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("Concat", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(compileState, typeof(string));
                        }
                        else if(m_LeftHandType == typeof(LSLKey))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", Type.EmptyTypes));
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(string), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Call, typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }));
                            throw Return(compileState, typeof(string));
                        }
                        else if(typeof(string) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("Concat", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(compileState, m_LeftHandType);
                        }

                        if(typeof(double) != m_LeftHandType && typeof(int) != m_LeftHandType && typeof(string) != m_LeftHandType)
                        {
                            mi = m_LeftHandType.GetMethod("op_Addition", new Type[] { m_LeftHandType, m_RightHandType });
                            if (null != mi)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                                compileState.ILGen.Emit(OpCodes.Call, mi);
                                if (!IsValidType(mi.ReturnType))
                                {
                                    throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                                }
                                throw Return(compileState, mi.ReturnType);
                            }
                        }
                        else if (typeof(double) != m_RightHandType && typeof(int) != m_RightHandType && typeof(string) != m_RightHandType)
                        {
                            mi = m_RightHandType.GetMethod("op_Addition", new Type[] { m_LeftHandType, m_RightHandType });
                            if (null != mi)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                                compileState.ILGen.Emit(OpCodes.Call, mi);
                                if (!IsValidType(mi.ReturnType))
                                {
                                    throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                                }
                                throw Return(compileState, mi.ReturnType);
                            }
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '+' is not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "-":
                        if((m_LeftHandType == typeof(double) || m_LeftHandType == typeof(int)) &&
                            (m_RightHandType == typeof(double) || m_RightHandType == typeof(int)) &&
                            (m_LeftHandType == typeof(double) || m_RightHandType == typeof(double)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Sub);
                            throw Return(compileState, typeof(double));
                        }
                        else if(m_LeftHandType == typeof(int) && m_RightHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Sub);
                            throw Return(compileState, m_LeftHandType);
                        }
                        else if(typeof(double) != m_LeftHandType && typeof(int) != m_LeftHandType && typeof(string) != m_LeftHandType)
                        {
                            mi = m_LeftHandType.GetMethod("op_Subtraction", new Type[] { m_LeftHandType, m_RightHandType });
                            if (null != mi)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                                compileState.ILGen.Emit(OpCodes.Call, mi);
                                if (!IsValidType(mi.ReturnType))
                                {
                                    throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                                }
                                throw Return(compileState, mi.ReturnType);
                            }
                        }
                        if (typeof(double) != m_RightHandType && typeof(int) != m_RightHandType && typeof(string) != m_RightHandType)
                        {
                            mi = m_RightHandType.GetMethod("op_Subtraction", new Type[] { m_LeftHandType, m_RightHandType });
                            if (null != mi)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                                compileState.ILGen.Emit(OpCodes.Call, mi);
                                if (!IsValidType(mi.ReturnType))
                                {
                                    throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                                }
                                throw Return(compileState, mi.ReturnType);
                            }
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '-' is not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        
                    case "*":
                        if((m_LeftHandType == typeof(double) || m_LeftHandType == typeof(int)) &&
                            (m_RightHandType == typeof(double) || m_RightHandType == typeof(int)) &&
                            (m_LeftHandType == typeof(double) || m_RightHandType == typeof(double)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Mul);
                            throw Return(compileState, typeof(double));
                        }
                        else if (m_LeftHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerMultiply", new Type[] { typeof(int), typeof(int) }));
                            throw Return(compileState, m_LeftHandType);
                        }
                        else if (m_LeftHandType == typeof(double))
                        {
                            if(m_RightHandType == typeof(Vector3) || m_RightHandType == typeof(Quaternion))
                            {
                                mi = m_RightHandType.GetMethod("op_Multiply", new Type[] { m_LeftHandType, m_RightHandType });
                                if (null != mi)
                                {
                                    compileState.ILGen.Emit(OpCodes.Call, mi);
                                    if (!IsValidType(mi.ReturnType))
                                    {
                                        throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                                    }
                                    throw Return(compileState, mi.ReturnType);
                                }
                            }
                        }
                        else if(m_LeftHandType == typeof(string))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_Multiply", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(compileState, typeof(string));
                        }
                        
                        mi = m_LeftHandType.GetMethod("op_Multiply", new Type[] { m_LeftHandType, m_RightHandType });
                        if (null != mi)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                            if (!IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(compileState, mi.ReturnType);
                        }
                        mi = m_RightHandType.GetMethod("op_Multiply", new Type[] { m_LeftHandType, m_RightHandType });
                        if (null != mi)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                            if (!IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(compileState, mi.ReturnType);
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '*' is not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "/":
                        if((m_LeftHandType == typeof(double) || m_LeftHandType == typeof(int)) &&
                            (m_RightHandType == typeof(double) || m_RightHandType == typeof(int)) &&
                            (m_LeftHandType == typeof(double) || m_RightHandType == typeof(double)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Div);
                            throw Return(compileState, typeof(double));
                        }
                        else if (m_LeftHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(int), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerDivision", new Type[] { typeof(int), typeof(int) }));
                            throw Return(compileState, m_LeftHandType);
                        }
                        else if (m_LeftHandType == typeof(string))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_Division", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(compileState, typeof(string));
                        }

                        mi = m_LeftHandType.GetMethod("op_Division", new Type[] { m_LeftHandType, m_RightHandType });
                        if (null != mi)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                            if (!IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(compileState, mi.ReturnType);
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '/' is not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "%":
                        if((m_LeftHandType == typeof(double) || m_LeftHandType == typeof(int)) &&
                            (m_RightHandType == typeof(double) || m_RightHandType == typeof(int)) &&
                            (m_LeftHandType == typeof(double) || m_RightHandType == typeof(double)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Rem);
                            throw Return(compileState, typeof(double));
                        }
                        else if(m_LeftHandType == typeof(int) && m_RightHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(int), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerModulus", new Type[] { typeof(int), typeof(int) }));
                            throw Return(compileState, m_LeftHandType);
                        }
                        else if (m_LeftHandType == typeof(string))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_Modulus", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(compileState, typeof(string));
                        }

                        mi = m_RightHandType.GetMethod("op_Modulus", new Type[] { m_LeftHandType, m_RightHandType });
                        if (null != mi)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                            if(!IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(compileState, mi.ReturnType);
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '%' is not supported for '{0}' and '{1}'", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "<<":
                        if (m_LeftHandType == typeof(int) && m_RightHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Shl);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '<<' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }
                        throw Return(compileState, m_LeftHandType);

                    case ">>":
                        if (m_LeftHandType == typeof(int) && m_RightHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Shr);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '>>' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }
                        throw Return(compileState, m_LeftHandType);

                    case "==":
                        if(m_LeftHandType == typeof(double) || m_RightHandType == typeof(double))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Ceq);
                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Ceq);
                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(LSLKey))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Callvirt, m_LeftHandType.GetMethod("Equals", new Type[] { m_LeftHandType }));
                            throw Return(compileState, typeof(int));
                        }
                        else if (m_RightHandType == typeof(LSLKey))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, m_RightHandType, m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Callvirt, m_RightHandType.GetMethod("Equals", new Type[] { m_LeftHandType }));
                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(Vector3) || m_LeftHandType == typeof(Quaternion) || m_LeftHandType == typeof(string))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_Equality", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(AnArray) && m_RightHandType == typeof(AnArray))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Callvirt, m_LeftHandType.GetProperty("Count").GetGetMethod());
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Callvirt, m_LeftHandType.GetProperty("Count").GetGetMethod());
                            compileState.ILGen.Emit(OpCodes.Ceq);
                            throw Return(compileState, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '==' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "!=":
                        if(m_LeftHandType == typeof(double) || m_RightHandType == typeof(double))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Ceq);
                            compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                            compileState.ILGen.Emit(OpCodes.Ceq);
                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(int), m_RightHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Ceq);
                            compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                            compileState.ILGen.Emit(OpCodes.Ceq);
                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(LSLKey))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Callvirt, m_LeftHandType.GetMethod("Equals", new Type[] { m_LeftHandType }));
                            compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                            compileState.ILGen.Emit(OpCodes.Ceq);
                            throw Return(compileState, typeof(int));
                        }
                        else if (m_RightHandType == typeof(LSLKey))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, m_RightHandType, m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Callvirt, m_RightHandType.GetMethod("Equals", new Type[] { m_LeftHandType }));
                            compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                            compileState.ILGen.Emit(OpCodes.Ceq);
                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(Vector3) || m_LeftHandType == typeof(Quaternion) || m_LeftHandType == typeof(string))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_Inequality", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(AnArray) && m_RightHandType == typeof(AnArray))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Callvirt, m_LeftHandType.GetProperty("Count").GetGetMethod());
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Callvirt, m_RightHandType.GetProperty("Count").GetGetMethod());
                            /* LSL is really about subtraction with that operator */
                            compileState.ILGen.Emit(OpCodes.Sub);
                            throw Return(compileState, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '!=' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "<=":
                        if(m_LeftHandType == typeof(double) || m_RightHandType == typeof(double))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Cgt);
                            compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                            compileState.ILGen.Emit(OpCodes.Ceq);
                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(int), m_RightHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Cgt);
                            compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                            compileState.ILGen.Emit(OpCodes.Ceq);
                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(Vector3) || m_LeftHandType == typeof(Quaternion))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_LessThanOrEqual", new Type[] { m_LeftHandType, m_LeftHandType }));
                            throw Return(compileState, typeof(int));
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format("operator '<=' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));
                        }

                    case "<":
                        if(m_LeftHandType == typeof(double) || m_RightHandType == typeof(double))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Clt);

                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(int), m_RightHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Clt);

                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(Vector3) || m_LeftHandType == typeof(Quaternion))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_LessThan", new Type[] { m_LeftHandType, m_LeftHandType }));

                            throw Return(compileState, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '<' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_LeftHandType)));

                    case ">":
                        if(m_LeftHandType == typeof(double) || m_RightHandType == typeof(double))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Cgt);

                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(int), m_RightHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Cgt);

                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(Vector3) || m_LeftHandType == typeof(Quaternion))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_GreaterThan", new Type[] { m_LeftHandType, m_LeftHandType }));

                            throw Return(compileState, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '>' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_LeftHandType)));

                    case ">=":
                        if(m_LeftHandType == typeof(double) || m_RightHandType == typeof(double))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Clt);
                            compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                            compileState.ILGen.Emit(OpCodes.Ceq);

                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(int), m_RightHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Clt);
                            compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                            compileState.ILGen.Emit(OpCodes.Ceq);

                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(Vector3) || m_LeftHandType == typeof(Quaternion))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);

                            compileState.ILGen.Emit(OpCodes.Call, m_LeftHandType.GetMethod("op_GreaterThanOrEqual", new Type[] { m_LeftHandType, m_LeftHandType }));

                            throw Return(compileState, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '>=' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "&&":
                        if (typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.And);
                            compileState.ILGen.Emit(OpCodes.Ldc_I4_1);
                            compileState.ILGen.Emit(OpCodes.And);
                            throw Return(compileState, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '&&' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "&":
                        if (typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.And);
                            throw Return(compileState, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '&' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "|":
                        if (typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Or);
                            throw Return(compileState, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '|' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "^":
                        if (typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Xor);
                            throw Return(compileState, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '^' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    case "||":
                        if (typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Or);
                            compileState.ILGen.Emit(OpCodes.Ldc_I4_1);
                            compileState.ILGen.Emit(OpCodes.And);
                            throw Return(compileState, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format("operator '||' not supported for {0} and {1}", MapType(m_LeftHandType), MapType(m_RightHandType)));

                    default:
                        throw new CompilerException(m_LineNumber, string.Format("unknown operator '{0}' for {1} and {2}", m_Operator, MapType(m_LeftHandType), MapType(m_RightHandType)));
                }
            }
        }
    }
}
