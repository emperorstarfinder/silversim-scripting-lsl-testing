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

#pragma warning disable RCS1029

using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Globalization;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        private sealed class BinaryOperatorExpression : IExpressionStackElement
        {
            private readonly string m_Operator;
            private LocalBuilder m_LeftHandLocal;
            private LocalBuilder m_RightHandLocal;
            private readonly Tree m_LeftHand;
            private Type m_LeftHandType;
            private readonly Tree m_RightHand;
            private Type m_RightHandType;
            private readonly int m_LineNumber;
            private enum State
            {
                LeftHand,
                RightHand
            }

            private readonly List<State> m_ProcessOrder;
            private bool m_HaveBeginScope;

            private static readonly Dictionary<string, State[]> m_ProcessOrders = new Dictionary<string, State[]>();

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

            private void BeginScope(CompileState compileState)
            {
                if(m_HaveBeginScope)
                {
                    throw new CompilerException(m_LineNumber, "Internal Error! Binary operator evaluation scope error");
                }
                m_HaveBeginScope = true;
                compileState.ILGen.BeginScope();
            }

            private LocalBuilder DeclareLocal(CompileState compileState, Type localType)
            {
                if(!m_HaveBeginScope)
                {
                    compileState.ILGen.BeginScope();
                }
                m_HaveBeginScope = true;
                return compileState.ILGen.DeclareLocal(localType);
            }

            private ReturnTypeException Return(CompileState compileState, Type t)
            {
                if(m_HaveBeginScope)
                {
                    compileState.ILGen.EndScope();
                }
                return new ReturnTypeException(compileState, t, m_LineNumber);
            }

            private FieldInfo GetVectorOrQuaternionField(CompileState compileState, Type t, string member)
            {
                switch (member)
                {
                    case "x":
                        return t.GetField("X");
                    case "y":
                        return t.GetField("Y");
                    case "z":
                        return t.GetField("Z");

                    case "s":
                        if (t != typeof(Quaternion))
                        {
                            throw new CompilerException(m_LineNumber, this.GetLanguageString(
                                compileState.CurrentCulture,
                                "InvalidMemberAccessSToVector",
                                "Invalid member access 's' to vector"));
                        }
                        return t.GetField("W");

                    default:
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(
                            compileState.CurrentCulture,
                            "InvalidMemberAccess0To1",
                            "Invalid member access '{0}' to {1}"), member, compileState.MapType(t)));
                }
            }

            private Type GetMemberVarTreeType(CompileState compileState, Tree t, Dictionary<string, object> localVars)
            {
                APIAccessibleMembersAttribute membersAttr;
                var selectorStack = new List<Tree>();
                while (t.Type == Tree.EntryType.OperatorBinary)
                {
                    selectorStack.Insert(0, t);
                    t = t.SubTree[0];
                }

                if (t.Type != Tree.EntryType.Variable)
                {
                    throw new CompilerException(m_LineNumber, this.GetLanguageString(
                        compileState.CurrentCulture,
                        "LeftValueOfOperatorEqualsIsNotAVariable",
                        "L-value of operator '=' is not a variable"));
                }

                object varInfo = localVars[t.Entry];
                Type varType = GetVarType(varInfo);

                string varNameWithSelectors = t.Entry;

                foreach (Tree selector in selectorStack)
                {
                    FieldInfo fi;
                    PropertyInfo pi;
                    string member = selector.SubTree[1].Entry;
                    if(varType == typeof(Vector3) || varType == typeof(Quaternion))
                    {
                        varType = GetVectorOrQuaternionField(compileState, varType, member).FieldType;
                    }
                    else if((membersAttr = Attribute.GetCustomAttribute(varType, typeof(APIAccessibleMembersAttribute)) as APIAccessibleMembersAttribute) == null)
                    {
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(
                            compileState.CurrentCulture,
                            "OperatorDorNotSupportedFor",
                            "operator '.' not supported for {0}"), compileState.MapType(varType)));
                    }
                    else if(!membersAttr.Members.Contains(member))
                    {
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(
                            compileState.CurrentCulture,
                            "InvalidMemberAccess0To1",
                            "Invalid member access '{0}' to {1}"), member, compileState.MapType(varType)));

                    }
                    else if((fi = varType.GetField(member)) != null)
                    {
                        varType = fi.FieldType;
                    }
                    else if((pi = varType.GetMemberProperty(compileState, member)) != null)
                    {
                        if (pi.GetGetMethod() != null)
                        {
                            varType = pi.PropertyType;
                        }
                        else
                        { 
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(
                                compileState.CurrentCulture,
                                "Member1IsReadOnlyForType0AtVariable2",
                                "Member '{1}' of type '{0}' (Variable '{2}') is read only."), member, compileState.MapType(varType), varNameWithSelectors));
                        }
                    }
                    else
                    {
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(
                            compileState.CurrentCulture,
                            "InvalidMemberAccess0To1",
                            "Invalid member access '{0}' to {1}"), member, compileState.MapType(varType)));
                    }
                    varNameWithSelectors += "." + member;
                }

                return varType;
            }

            private Type GetMemberSelectorType(CompileState compileState, Type varType, string memberName)
            {
                APIAccessibleMembersAttribute membersAttr;
                if (varType == typeof(Vector3) || varType == typeof(Quaternion))
                {
                    return GetVectorOrQuaternionField(compileState, varType, memberName).FieldType;
                }
                else if ((membersAttr = Attribute.GetCustomAttribute(varType, typeof(APIAccessibleMembersAttribute)) as APIAccessibleMembersAttribute) != null)
                {
                    PropertyInfo pi;
                    FieldInfo fi;
                    if (!membersAttr.Members.Contains(memberName))
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "InvalidMemberAccess0To1", "Invalid member access '{0}' to {1}"),
                            memberName,
                            compileState.MapType(varType)));
                    }

                    if ((fi = varType.GetField(memberName)) != null)
                    {
                        return fi.FieldType;
                    }
                    else if ((pi = varType.GetMemberProperty(compileState, memberName)) != null)
                    {
                        return pi.PropertyType;
                    }
                    else
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "InvalidMemberAccess0To1", "Invalid member access '{0}' to {1}"),
                            memberName,
                            compileState.MapType(varType)));
                    }
                }
                else
                {
                    throw new CompilerException(m_LineNumber, string.Format(
                        this.GetLanguageString(compileState.CurrentCulture, "OperatorDorNotSupportedFor", "operator '.' not supported for {0}"),
                        compileState.MapType(varType)));
                }
            }

            private Type GetMemberSelectorToStack(CompileState compileState, Type varType, string memberName)
            {
                APIAccessibleMembersAttribute membersAttr;
                if (varType == typeof(Vector3) || varType == typeof(Quaternion))
                {
                    FieldInfo fi = GetVectorOrQuaternionField(compileState, varType, memberName);
                    compileState.ILGen.Emit(OpCodes.Ldfld, fi);
                    varType = fi.FieldType;
                }
                else if ((membersAttr = Attribute.GetCustomAttribute(varType, typeof(APIAccessibleMembersAttribute)) as APIAccessibleMembersAttribute) != null)
                {
                    PropertyInfo pi;
                    FieldInfo fi;
                    if (!membersAttr.Members.Contains(memberName))
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "InvalidMemberAccess0To1", "Invalid member access '{0}' to {1}"),
                            memberName,
                            compileState.MapType(varType)));
                    }

                    if ((fi = varType.GetField(memberName)) != null)
                    {
                        varType = fi.FieldType;
                        compileState.ILGen.Emit(OpCodes.Ldfld, fi);
                    }
                    else if ((pi = varType.GetMemberProperty(compileState, memberName)) != null)
                    {
                        MethodInfo mi;
                        if ((mi = pi.GetGetMethod()) == null)
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "Member1IsWriteOnlyForType0AtVariable2", "Member '{1}' of type '{0}' (Variable '{2}') is write only."), memberName, compileState.MapType(varType), string.Empty));
                        }
                        else if (mi.IsStatic)
                        {
                            compileState.ILGen.Emit(OpCodes.Pop);
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                        else if (mi.IsVirtual)
                        {
                            compileState.ILGen.Emit(OpCodes.Callvirt, mi);
                        }
                        else
                        {
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                        varType = pi.PropertyType;
                    }
                    else
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "InvalidMemberAccess0To1", "Invalid member access '{0}' to {1}"),
                            memberName,
                            compileState.MapType(varType)));
                    }
                }
                else
                {
                    throw new CompilerException(m_LineNumber, string.Format(
                        this.GetLanguageString(compileState.CurrentCulture, "OperatorDorNotSupportedFor", "operator '.' not supported for {0}"),
                        compileState.MapType(varType)));
                }

                return varType;
            }

            private Type GetMemberVarTreeToStack(CompileState compileState, Tree t, Dictionary<string, object> localVars)
            {
                var selectorStack = new List<Tree>();
                while(t.Type == Tree.EntryType.OperatorBinary)
                {
                    selectorStack.Insert(0, t);
                    t = t.SubTree[0];
                }

                if (t.Type != Tree.EntryType.Variable)
                {
                    throw new CompilerException(m_LineNumber, this.GetLanguageString(compileState.CurrentCulture, "LeftValueOfOperatorEqualsIsNotAVariable", "L-value of operator '=' is not a variable"));
                }

                object varInfo = localVars[t.Entry];
                Type varType = GetVarToStack(compileState, varInfo);

                compileState.ILGen.BeginScope();

                var locals = new Dictionary<Type, LocalBuilder>();

                foreach (Tree selector in selectorStack)
                {
                    string member = selector.SubTree[1].Entry;
                    if(varType.IsValueType)
                    {
                        LocalBuilder lb;
                        if(!locals.TryGetValue(varType, out lb))
                        {
                            lb = compileState.ILGen.DeclareLocal(varType);
                            locals.Add(varType, lb);
                        }
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        compileState.ILGen.Emit(OpCodes.Ldloca, lb);
                    }

                    varType = GetMemberSelectorToStack(compileState, varType, member);
                }

                compileState.ILGen.EndScope();

                return varType;
            }

            private Type SetMemberSelectorFromStack(CompileState compileState, Type varType, string memberName)
            {
                APIAccessibleMembersAttribute membersAttr;
                if (varType == typeof(Vector3) || varType == typeof(Quaternion))
                {
                    FieldInfo fi = GetVectorOrQuaternionField(compileState, varType, memberName);
                    compileState.ILGen.Emit(OpCodes.Stfld, fi);
                    varType = fi.FieldType;
                }
                else if ((membersAttr = Attribute.GetCustomAttribute(varType, typeof(APIAccessibleMembersAttribute)) as APIAccessibleMembersAttribute) != null)
                {
                    PropertyInfo pi;
                    FieldInfo fi;
                    MethodInfo mi;
                    if (!membersAttr.Members.Contains(memberName))
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "InvalidMemberAccess0To1", "Invalid member access '{0}' to {1}"),
                            memberName,
                            compileState.MapType(varType)));
                    }

                    if ((fi = varType.GetField(memberName)) != null)
                    {
                        varType = fi.FieldType;
                        compileState.ILGen.Emit(OpCodes.Stfld, fi);
                    }
                    else if ((pi = varType.GetMemberProperty(compileState, memberName)) != null && (mi = pi.GetSetMethod()) != null)
                    {
                        varType = pi.PropertyType;
                        if(mi.IsStatic)
                        {
                            compileState.ILGen.BeginScope();
                            LocalBuilder swapLb = compileState.ILGen.DeclareLocal(varType);
                            compileState.ILGen.Emit(OpCodes.Stloc, swapLb);
                            compileState.ILGen.Emit(OpCodes.Pop);
                            compileState.ILGen.Emit(OpCodes.Ldloc, swapLb);
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                        else if(mi.IsVirtual)
                        {
                            compileState.ILGen.Emit(OpCodes.Callvirt, mi);
                        }
                        else
                        {
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                    }
                    else
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "InvalidMemberAccess0To1", "Invalid member access '{0}' to {1}"),
                            memberName,
                            compileState.MapType(varType)));
                    }
                }
                else
                {
                    throw new CompilerException(m_LineNumber, string.Format(
                        this.GetLanguageString(compileState.CurrentCulture, "OperatorDorNotSupportedFor", "operator '.' not supported for {0}"),
                        compileState.MapType(varType)));
                }

                return varType;
            }

            public class SelectorTree
            {
                public LocalBuilder Local;
                public Type Type;
                public string Member;
                public bool RequiredToSet;
            }

            private void SetMemberVarTreeFromStack(CompileState compileState, Tree t, Dictionary<string, object> localVars)
            {
#if DEBUG
                compileState.ILGen.Writer.WriteLine("======== SetMemberVarTreeFromStack: Begin ========");
#endif

                var selectorStack = new List<Tree>();
                while (t.Type == Tree.EntryType.OperatorBinary)
                {
                    selectorStack.Insert(0, t);
                    t = t.SubTree[0];
                }

                if (t.Type != Tree.EntryType.Variable)
                {
                    throw new CompilerException(m_LineNumber, this.GetLanguageString(compileState.CurrentCulture, "LeftValueOfOperatorEqualsIsNotAVariable", "L-value of operator '=' is not a variable"));
                }

                if(compileState.ApiInfo.Constants.ContainsKey(t.Entry))
                {
                    throw new CompilerException(m_LineNumber, this.GetLanguageString(compileState.CurrentCulture, "LeftValueOfOperatorEqualsIsNotAVariable", "L-value of operator '=' is not a variable"));
                }

                object varInfo = localVars[t.Entry];

                compileState.ILGen.BeginScope();

                LocalBuilder swapLb = compileState.ILGen.DeclareLocal(m_LeftHandType);
                compileState.ILGen.Emit(OpCodes.Stloc, swapLb);
#if DEBUG
                compileState.ILGen.Writer.WriteLine("======== SetMemberVarTreeFromStack: Fetch reference ========");
#endif

                Type varType = GetVarToStack(compileState, varInfo);
                bool storeBackVar = false;

                if(Attribute.GetCustomAttribute(varType, typeof(APICloneOnAssignmentAttribute)) != null)
                {
                    storeBackVar = true;
                    compileState.ILGen.Emit(OpCodes.Newobj, varType.GetConstructor(new Type[] { varType }));
                }
                else if(varType.IsValueType)
                {
                    storeBackVar = true;
                }

                var selectorTree = new List<SelectorTree>();

                bool firstRequiredSelector = storeBackVar;
                LocalBuilder varLb = null;

                for(int i = 0; i < selectorStack.Count - 1; ++i)
                {
#if DEBUG
                    compileState.ILGen.Writer.WriteLine("-------- SetMemberVarTreeFromStack: Get: {0}: {1} --------", varType.FullName, selectorStack[i].SubTree[1].Entry);
#endif

                    Tree sel = selectorStack[i];
                    LocalBuilder lb = compileState.ILGen.DeclareLocal(varType);
                    varLb = varLb ?? lb;
                    selectorTree.Insert(0, new SelectorTree
                    {
                        Type = varType,
                        Local = lb,
                        Member = sel.SubTree[1].Entry,
                    });

                    if(varType.IsValueType)
                    {
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        compileState.ILGen.Emit(OpCodes.Ldloc, lb);
                    }
                    else if(Attribute.GetCustomAttribute(varType, typeof(APICloneOnAssignmentAttribute)) != null)
                    {
                        compileState.ILGen.Emit(OpCodes.Newobj, varType.GetConstructor(new Type[] { varType }));
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        compileState.ILGen.Emit(OpCodes.Ldloc, lb);
                    }
                    else
                    {
                        compileState.ILGen.Emit(OpCodes.Dup);
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                    }

                    varType = GetMemberSelectorToStack(compileState, varType, selectorStack[i].SubTree[1].Entry);
                    if(varType.IsValueType || Attribute.GetCustomAttribute(varType, typeof(APICloneOnAssignmentAttribute)) != null)
                    {
                        firstRequiredSelector = true;
                    }
                    selectorTree[0].RequiredToSet = firstRequiredSelector;
                }

                LocalBuilder refLb = compileState.ILGen.DeclareLocal(varType);
                varLb = varLb ?? refLb;
                if (varType.IsValueType)
                {
                    compileState.ILGen.Emit(OpCodes.Stloc, refLb);
                    compileState.ILGen.Emit(OpCodes.Ldloca, refLb);
                }
                else
                {
                    compileState.ILGen.Emit(OpCodes.Dup);
                    compileState.ILGen.Emit(OpCodes.Stloc, refLb);
                }

#if DEBUG
                compileState.ILGen.Writer.WriteLine("======== SetMemberVarTreeFromStack: Set final member ========");
#endif

                compileState.ILGen.Emit(OpCodes.Ldloc, swapLb);

                SetMemberSelectorFromStack(compileState, varType, selectorStack[selectorStack.Count - 1].SubTree[1].Entry);

#if DEBUG
                compileState.ILGen.Writer.WriteLine("======== SetMemberVarTreeFromStack: Save back necessary locals ========");
#endif

                foreach (SelectorTree sel in selectorTree)
                {
                    if(!sel.RequiredToSet)
                    {
                        break;
                    }

#if DEBUG
                    compileState.ILGen.Writer.WriteLine("-------- SetMemberVarTreeFromStack: Set: {0}: {1} --------", sel.Type.FullName, sel.Member);
#endif

                    if (sel.Type.IsValueType)
                    {
                        compileState.ILGen.Emit(OpCodes.Ldloca, sel.Local);
                    }
                    else
                    {
                        compileState.ILGen.Emit(OpCodes.Ldloc, sel.Local);
                    }
                    compileState.ILGen.Emit(OpCodes.Ldloc, refLb);
                    refLb = sel.Local;
                    SetMemberSelectorFromStack(compileState, sel.Type, sel.Member);
                }

                if (storeBackVar)
                {
                    compileState.ILGen.Emit(OpCodes.Ldloc, varLb);
                    SetVarFromStack(compileState, varInfo, m_LineNumber);
                }

                compileState.ILGen.EndScope();
#if DEBUG
                compileState.ILGen.Writer.WriteLine("======== SetMemberVarTreeFromStack: Finished ========");
#endif
            }

            public BinaryOperatorExpression(
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
                        m_LeftHandType = GetMemberVarTreeType(compileState, m_LeftHand, localVars);
                    }
                    else if (m_LeftHand.Type != Tree.EntryType.Variable)
                    {
                        throw new CompilerException(m_LineNumber, this.GetLanguageString(compileState.CurrentCulture, "LeftValueOfOperatorEqualsIsNotAVariable", "L-value of operator '=' is not a variable"));
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
                                m_LeftHandType = GetMemberVarTreeType(compileState, m_LeftHand, localVars);
                            }
                            else if (m_LeftHand.Type != Tree.EntryType.Variable)
                            {
                                throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "LeftValueOfOperatorEqualsIsNotAVariable", "L-value of operator '{0}' is not a variable"), m_Operator));
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
                if(innerExpressionReturn != null)
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
                            throw new CompilerException(m_LineNumber, this.GetLanguageString(compileState.CurrentCulture, "InternalError", "Internal Error"));
                    }
                }
                else
                {
                    switch(m_Operator)
                    {
                        case ".":
                            ProcessOperator_Member(
                                compileState);
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
                                compileState);
                            break;

                        default:
                            throw new CompilerException(m_LineNumber, string.Format("Internal Error! Unexpected operator '{0}'", m_Operator));
                    }
                    throw new CompilerException(m_LineNumber, string.Format("Internal Error! Unexpected return from operator '{0}' code generator", m_Operator));
                }
            }

            public void ProcessOperator_Member(
                CompileState compileState)
            {
                if (m_RightHand.Type != Tree.EntryType.MemberName)
                {
                    throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "0IsNotAMemberOfType1", "'{0}' is not a member of type {1}"), m_RightHand.Entry + m_RightHand.Type, compileState.MapType(m_LeftHandType)));
                }
                APIAccessibleMembersAttribute membersAttr;
                if (m_LeftHandType == typeof(Vector3) || m_LeftHandType == typeof(Quaternion))
                {
                    LocalBuilder lb = DeclareLocal(compileState, m_LeftHandType);
                    compileState.ILGen.Emit(OpCodes.Stloc, lb);
                    compileState.ILGen.Emit(OpCodes.Ldloca, lb);
                    FieldInfo fi = GetVectorOrQuaternionField(compileState, m_LeftHandType, m_RightHand.Entry);
                    compileState.ILGen.Emit(OpCodes.Ldfld, fi);
                    throw Return(compileState, fi.FieldType);
                }
                else if((membersAttr = Attribute.GetCustomAttribute(m_LeftHandType, typeof(APIAccessibleMembersAttribute)) as APIAccessibleMembersAttribute) != null)
                {
                    string memberName = m_RightHand.Entry;
                    PropertyInfo pi;
                    FieldInfo fi;
                    MethodInfo mi;
                    if (!membersAttr.Members.Contains(memberName))
                    {
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "InvalidMemberAccess0To1", "Invalid member access '{0}' to {1}"), m_RightHand.Entry, compileState.MapType(m_LeftHandType)));
                    }

                    if(m_LeftHandType.IsValueType)
                    {
                        LocalBuilder lb = DeclareLocal(compileState, m_LeftHandType);
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        compileState.ILGen.Emit(OpCodes.Ldloca, lb);
                    }

                    if ((fi = m_LeftHandType.GetField(memberName)) != null)
                    {
                        if (fi.IsStatic)
                        {
                            compileState.ILGen.Emit(OpCodes.Pop);
                            compileState.ILGen.Emit(OpCodes.Ldsfld, fi);
                        }
                        else
                        {
                            compileState.ILGen.Emit(OpCodes.Ldfld, fi);
                        }
                        throw Return(compileState, fi.FieldType);
                    }
                    else if ((pi = m_LeftHandType.GetMemberProperty(compileState, memberName)) != null && (mi = pi.GetGetMethod()) != null)
                    {
                        if(mi.IsStatic)
                        {
                            compileState.ILGen.Emit(OpCodes.Pop);
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                        if (mi.IsVirtual)
                        {
                            compileState.ILGen.Emit(OpCodes.Callvirt, mi);
                        }
                        else
                        {
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                        throw Return(compileState, pi.PropertyType);
                    }
                    else
                    {
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "InvalidMemberAccess0To1", "Invalid member access '{0}' to {1}"), m_RightHand.Entry, compileState.MapType(m_LeftHandType)));
                    }
                }
                else
                {
                    throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorDorNotSupportedFor", "operator '.' not supported for {0}"), compileState.MapType(m_LeftHandType)));
                }
            }

            public void ProcessOperator_Assignment(
                CompileState compileState,
                Dictionary<string, object> localVars)
            {
                if (m_LeftHand.Type == Tree.EntryType.OperatorBinary && m_LeftHand.Entry == ".")
                {
                    ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);
                    compileState.ILGen.Emit(OpCodes.Dup);
                    SetMemberVarTreeFromStack(compileState, m_LeftHand, localVars);
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
                object varInfo;
                bool isComponentAccess = false;
                if (m_LeftHand.Type == Tree.EntryType.OperatorBinary && m_LeftHand.Entry == ".")
                {
                    m_LeftHandType = GetMemberVarTreeType(compileState, m_LeftHand, localVars);
                    isComponentAccess = true;
                    varInfo = null;
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
                else if(m_LeftHandType == typeof(int) && m_RightHandType == typeof(double))
                {
                    /* funky LSL type calculation */
                    ProcessCasts(compileState, typeof(int), m_RightHandType, m_LineNumber);
                    m_RightHandType = typeof(int);
                }
                else if (m_LeftHandType == typeof(long) && m_RightHandType == typeof(double))
                {
                    /* funky LSL type calculation */
                    ProcessCasts(compileState, typeof(long), m_RightHandType, m_LineNumber);
                    m_RightHandType = typeof(long);
                }
                else if ((m_LeftHandType == typeof(Vector3) && m_RightHandType == typeof(int)) ||
                    (m_LeftHandType == typeof(Quaternion) && m_RightHandType == typeof(int)))
                {
                    ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                    m_RightHandType = typeof(double);
                }
                else if ((m_LeftHandType == typeof(Vector3) && m_RightHandType == typeof(long)) ||
                    (m_LeftHandType == typeof(Quaternion) && m_RightHandType == typeof(long)))
                {
                    ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                    m_RightHandType = typeof(double);
                }
                else if (m_LeftHandType == typeof(AnArray))
                {
                    /* no conversion required */
                }
                else
                {
                    ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);
                    m_RightHandType = m_LeftHandType;
                }

                MethodInfo mi;
                switch(m_Operator)
                {
                    case "+=":
                        if(typeof(int) == m_LeftHandType || typeof(double) == m_LeftHandType || typeof(long) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Add);
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
                        if (mi != null)
                        {
                            if (mi.ReturnType != m_LeftHandType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorPlusEqualsNotSupportedFor0And1", "operator '+=' not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                            }
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorPlusEqualsNotSupportedFor0And1", "operator '+=' not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                        }
                        break;

                    case "-=":
                        if(typeof(int) == m_LeftHandType || typeof(double) == m_LeftHandType || typeof(long) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Sub);
                            break;
                        }

                        mi = m_LeftHandType.GetMethod("op_Subtraction", new Type[] { m_LeftHandType, m_RightHandType });
                        if (mi != null)
                        {
                            if (mi.ReturnType != m_LeftHandType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorMinusEqualsNotSupportedFor0And1", "operator '-=' not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                            }
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorMinusEqualsNotSupportedFor0And1", "operator '-=' not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                        }
                        break;

                    case "*=":
                        if(typeof(int) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerMultiply", new Type[] { m_LeftHandType, m_RightHandType }));
                            break;
                        }
                        else if(typeof(double) == m_LeftHandType || typeof(long) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Mul);
                            break;
                        }

                        mi = m_LeftHandType.GetMethod("op_Multiply", new Type[] { m_LeftHandType, m_RightHandType });
                        if(mi != null)
                        {
                            if(mi.ReturnType != m_LeftHandType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorMultiplyEqualsNotSupportedFor0And1", "operator '*=' not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                            }
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorMultiplyEqualsNotSupportedFor0And1", "operator '*=' not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                        }
                        break;

                    case "/=":
                        if(typeof(int) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerDivision", new Type[] { m_LeftHandType, m_RightHandType }));
                            break;
                        }
                        else if(typeof(double) == m_LeftHandType || typeof(long) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Div);
                            break;
                        }

                        mi = m_LeftHandType.GetMethod("op_Division", new Type[]{m_LeftHandType, m_RightHandType});
                        if(mi != null)
                        {
                            if(mi.ReturnType != m_LeftHandType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorDivideEqualsNotSupportedFor0And1", "operator '/=' not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                            }
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorDivideEqualsNotSupportedFor0And1", "operator '/=' not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                        }
                        break;

                    case "%=":
                        if(typeof(int) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("LSL_IntegerModulus", new Type[] { m_LeftHandType, m_RightHandType }));
                            break;
                        }
                        else if(typeof(double) == m_LeftHandType || typeof(long) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Rem);
                            break;
                        }

                        mi = m_LeftHandType.GetMethod("op_Modulus", new Type[]{m_LeftHandType, m_RightHandType});
                        if(mi != null)
                        {
                            if(mi.ReturnType != m_LeftHandType)
                            {
                                throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorModuloEqualsNotSupportedFor0And1", "operator '%=' not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                            }
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorModuloEqualsNotSupportedFor0And1", "operator '%=' not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                        }
                        break;

                    default:
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "Operator0Unknown", "Operator '{0}' unknown"), m_Operator));
                }

                compileState.ILGen.Emit(OpCodes.Dup);
                if(isComponentAccess)
                {
                    SetMemberVarTreeFromStack(compileState, m_LeftHand, localVars);
                }
                else
                {
                    SetVarFromStack(compileState, varInfo, m_LineNumber);
                }
                throw Return(compileState, m_LeftHandType);
            }

            public void ProcessOperator_Return(
                CompileState compileState)
            {
                MethodInfo mi;
                switch(m_Operator)
                {
                    case "+":
                        if ((m_LeftHandType == typeof(double) || m_LeftHandType == typeof(int) || m_LeftHandType == typeof(long)) &&
                            (m_RightHandType == typeof(double) || m_RightHandType == typeof(int) || m_RightHandType == typeof(long)) &&
                            (m_LeftHandType == typeof(double) || m_RightHandType == typeof(double)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Add);
                            throw Return(compileState, typeof(double));
                        }
                        else if ((m_LeftHandType == typeof(int) || m_LeftHandType == typeof(long)) &&
                            (m_RightHandType == typeof(int) || m_RightHandType == typeof(long)) &&
                            (m_LeftHandType == typeof(long) || m_RightHandType == typeof(long)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Add);
                            throw Return(compileState, typeof(long));
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
                            if(typeof(long) == m_RightHandType)
                            {
                                compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("AddLongInt", new Type[] { m_RightHandType }));
                            }
                            else if (typeof(int) == m_RightHandType || typeof(double) == m_RightHandType || typeof(string) == m_RightHandType)
                            {
                                compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { m_RightHandType }));
                            }
                            else if(typeof(LSLKey) == m_RightHandType)
                            {
                                compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                            }
                            else if(typeof(Vector3) == m_RightHandType)
                            {
                                compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("AddVector3ToList"));
                            }
                            else if (typeof(Quaternion) == m_RightHandType)
                            {
                                compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("AddQuaternionToList"));
                            }
                            else
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", m_RightHandType.FullName));
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
                            if (mi != null)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                                compileState.ILGen.Emit(OpCodes.Call, mi);
                                if (!compileState.IsValidType(mi.ReturnType))
                                {
                                    throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                                }
                                throw Return(compileState, mi.ReturnType);
                            }
                        }
                        else if (typeof(double) != m_RightHandType && typeof(int) != m_RightHandType && typeof(string) != m_RightHandType)
                        {
                            mi = m_RightHandType.GetMethod("op_Addition", new Type[] { m_LeftHandType, m_RightHandType });
                            if (mi != null)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                                compileState.ILGen.Emit(OpCodes.Call, mi);
                                if (!compileState.IsValidType(mi.ReturnType))
                                {
                                    throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                                }
                                throw Return(compileState, mi.ReturnType);
                            }
                        }
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorPlusNotSupportedFor0And1", "operator '+' is not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));

                    case "-":
                        if((m_LeftHandType == typeof(double) || m_LeftHandType == typeof(int) || m_LeftHandType == typeof(long)) &&
                            (m_RightHandType == typeof(double) || m_RightHandType == typeof(int) || m_RightHandType == typeof(long)) &&
                            (m_LeftHandType == typeof(double) || m_RightHandType == typeof(double)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Sub);
                            throw Return(compileState, typeof(double));
                        }
                        else if (( m_LeftHandType == typeof(int) || m_LeftHandType == typeof(long)) &&
                            (m_RightHandType == typeof(int) || m_RightHandType == typeof(long)) &&
                            (m_LeftHandType == typeof(long) || m_RightHandType == typeof(long)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Sub);
                            throw Return(compileState, typeof(long));
                        }
                        else if (m_LeftHandType == typeof(int) && m_RightHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Sub);
                            throw Return(compileState, m_LeftHandType);
                        }
                        else if(typeof(double) != m_LeftHandType && typeof(int) != m_LeftHandType && typeof(string) != m_LeftHandType)
                        {
                            mi = m_LeftHandType.GetMethod("op_Subtraction", new Type[] { m_LeftHandType, m_RightHandType });
                            if (mi != null)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                                compileState.ILGen.Emit(OpCodes.Call, mi);
                                if (!compileState.IsValidType(mi.ReturnType))
                                {
                                    throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                                }
                                throw Return(compileState, mi.ReturnType);
                            }
                        }
                        if (typeof(double) != m_RightHandType && typeof(int) != m_RightHandType && typeof(string) != m_RightHandType)
                        {
                            mi = m_RightHandType.GetMethod("op_Subtraction", new Type[] { m_LeftHandType, m_RightHandType });
                            if (mi != null)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                                compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                                compileState.ILGen.Emit(OpCodes.Call, mi);
                                if (!compileState.IsValidType(mi.ReturnType))
                                {
                                    throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                                }
                                throw Return(compileState, mi.ReturnType);
                            }
                        }
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorMinusNotSupportedFor0And1", "operator '-' is not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));

                    case "*":
                        if((m_LeftHandType == typeof(double) || m_LeftHandType == typeof(int) || m_LeftHandType == typeof(long)) &&
                            (m_RightHandType == typeof(double) || m_RightHandType == typeof(int) || m_RightHandType == typeof(long)) &&
                            (m_LeftHandType == typeof(double) || m_RightHandType == typeof(double)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Mul);
                            throw Return(compileState, typeof(double));
                        }
                        else if ((m_LeftHandType == typeof(int) || m_LeftHandType == typeof(long)) &&
                            (m_RightHandType == typeof(int) || m_RightHandType == typeof(long)) &&
                            (m_LeftHandType == typeof(long) || m_RightHandType == typeof(long)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Mul);
                            throw Return(compileState, typeof(long));
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
                            if (m_RightHandType == typeof(Vector3) || m_RightHandType == typeof(Quaternion))
                            {
                                mi = m_RightHandType.GetMethod("op_Multiply", new Type[] { m_LeftHandType, m_RightHandType });
                                if (mi != null)
                                {
                                    compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                                    compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                                    compileState.ILGen.Emit(OpCodes.Call, mi);
                                    if (!compileState.IsValidType(mi.ReturnType))
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
                        else if(m_LeftHandType == typeof(Quaternion) && m_RightHandType == typeof(Quaternion))
                        {
                            mi = m_LeftHandType.GetMethod("op_Multiply", new Type[] { m_LeftHandType, m_RightHandType });
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                            if (!compileState.IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(compileState, mi.ReturnType);
                        }

                        mi = m_LeftHandType.GetMethod("op_Multiply", new Type[] { m_LeftHandType, m_RightHandType });
                        if (mi != null)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                            if (!compileState.IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(compileState, mi.ReturnType);
                        }
                        mi = m_RightHandType.GetMethod("op_Multiply", new Type[] { m_LeftHandType, m_RightHandType });
                        if (mi != null)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                            if (!compileState.IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(compileState, mi.ReturnType);
                        }
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorMultiplyNotSupportedFor0And1", "operator '*' is not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));

                    case "/":
                        if((m_LeftHandType == typeof(double) || m_LeftHandType == typeof(int) || m_LeftHandType == typeof(long)) &&
                            (m_RightHandType == typeof(double) || m_RightHandType == typeof(int) || m_RightHandType == typeof(long)) &&
                            (m_LeftHandType == typeof(double) || m_RightHandType == typeof(double)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Div);
                            throw Return(compileState, typeof(double));
                        }
                        else if ((m_LeftHandType == typeof(long) || m_LeftHandType == typeof(int)) &&
                            (m_RightHandType == typeof(long) || m_RightHandType == typeof(int)) &&
                            (m_LeftHandType == typeof(long) || m_RightHandType == typeof(long)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Div);
                            throw Return(compileState, typeof(long));
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
                        else if(m_LeftHandType == typeof(Quaternion) && m_RightHandType == typeof(Quaternion))
                        {
                            mi = typeof(LSLCompiler).GetMethod("LSLQuaternionDivision");
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                            if (!compileState.IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(compileState, mi.ReturnType);
                        }

                        mi = m_LeftHandType.GetMethod("op_Division", new Type[] { m_LeftHandType, m_RightHandType });
                        if (mi != null)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                            if (!compileState.IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(compileState, mi.ReturnType);
                        }
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorDivideNotSupportedFor0And1", "operator '/' is not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));

                    case "%":
                        if((m_LeftHandType == typeof(double) || m_LeftHandType == typeof(int) || m_LeftHandType == typeof(long)) &&
                            (m_RightHandType == typeof(double) || m_RightHandType == typeof(int) || m_RightHandType == typeof(long)) &&
                            (m_LeftHandType == typeof(double) || m_RightHandType == typeof(double)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Rem);
                            throw Return(compileState, typeof(double));
                        }
                        else if ((m_LeftHandType == typeof(long) || m_LeftHandType == typeof(int)) &&
                            (m_RightHandType == typeof(long) || m_RightHandType == typeof(int)) &&
                            (m_LeftHandType == typeof(long) || m_RightHandType == typeof(double)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Rem);
                            throw Return(compileState, typeof(long));
                        }
                        else if (m_LeftHandType == typeof(int) && m_RightHandType == typeof(int))
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
                        if (mi != null)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Call, mi);
                            if(!compileState.IsValidType(mi.ReturnType))
                            {
                                throw new CompilerException(m_LineNumber, string.Format("Internal Error! Type {0} is not a LSL compatible type", mi.ReturnType.FullName));
                            }
                            throw Return(compileState, mi.ReturnType);
                        }
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorModuloNotSupportedFor0And1", "operator '%' is not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));

                    case "<<":
                        if (m_LeftHandType == typeof(int) && m_RightHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Shl);
                        }
                        else if ((m_LeftHandType == typeof(long) || m_LeftHandType == typeof(int)) &&
                            (m_RightHandType == typeof(long) || m_RightHandType == typeof(int)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessCasts(compileState, typeof(int), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Shl);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorShiftLeftNotSupportedFor0And1", "operator '<<' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                        }
                        throw Return(compileState, m_LeftHandType);

                    case ">>":
                        if (m_LeftHandType == typeof(int) && m_RightHandType == typeof(int))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Shr);
                        }
                        else if ((m_LeftHandType == typeof(long) || m_LeftHandType == typeof(int)) &&
                            (m_RightHandType == typeof(long) || m_RightHandType == typeof(int)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessCasts(compileState, typeof(int), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Shr);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorShiftRightNotSupportedFor0And1", "operator '>>' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                        }
                        throw Return(compileState, m_LeftHandType);

                    case "==":
                        if(m_LeftHandType == typeof(double) || m_RightHandType == typeof(double))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            if (compileState.UsesSinglePrecision)
                            {
                                compileState.ILGen.Emit(OpCodes.Conv_R4);
                            }
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                            if (compileState.UsesSinglePrecision)
                            {
                                compileState.ILGen.Emit(OpCodes.Conv_R4);
                            }

                            compileState.ILGen.Emit(OpCodes.Ceq);
                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(long) || m_RightHandType == typeof(long))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_RightHandType, m_LineNumber);

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
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorEqualsEqualsNotSupportedFor0And1", "operator '==' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));

                    case "!=":
                        if(m_LeftHandType == typeof(double) || m_RightHandType == typeof(double))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            if (compileState.UsesSinglePrecision)
                            {
                                compileState.ILGen.Emit(OpCodes.Conv_R4);
                            }
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            if (compileState.UsesSinglePrecision)
                            {
                                compileState.ILGen.Emit(OpCodes.Conv_R4);
                            }

                            compileState.ILGen.Emit(OpCodes.Ceq);
                            compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                            compileState.ILGen.Emit(OpCodes.Ceq);
                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(long) || m_RightHandType == typeof(long))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);

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
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorUnequalsNotSupportedFor0And1", "operator '!=' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));

                    case "<=":
                        if(m_LeftHandType == typeof(double) || m_RightHandType == typeof(double))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            if (compileState.UsesSinglePrecision)
                            {
                                compileState.ILGen.Emit(OpCodes.Conv_R4);
                            }
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                            if (compileState.UsesSinglePrecision)
                            {
                                compileState.ILGen.Emit(OpCodes.Conv_R4);
                            }

                            compileState.ILGen.Emit(OpCodes.Cgt);
                            compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                            compileState.ILGen.Emit(OpCodes.Ceq);
                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(long) || m_RightHandType == typeof(long))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_RightHandType, m_LineNumber);

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
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorLessEqualsNotSupportedFor0And1", "operator '<=' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                        }

                    case "<":
                        if(m_LeftHandType == typeof(double) || m_RightHandType == typeof(double))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            if (compileState.UsesSinglePrecision)
                            {
                                compileState.ILGen.Emit(OpCodes.Conv_R4);
                            }
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                            if (compileState.UsesSinglePrecision)
                            {
                                compileState.ILGen.Emit(OpCodes.Conv_R4);
                            }

                            compileState.ILGen.Emit(OpCodes.Clt);

                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(long) || m_RightHandType == typeof(long))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_RightHandType, m_LineNumber);

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
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorLessNotSupportedFor0And1", "operator '<' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_LeftHandType)));

                    case ">":
                        if(m_LeftHandType == typeof(double) || m_RightHandType == typeof(double))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            if (compileState.UsesSinglePrecision)
                            {
                                compileState.ILGen.Emit(OpCodes.Conv_R4);
                            }
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                            if (compileState.UsesSinglePrecision)
                            {
                                compileState.ILGen.Emit(OpCodes.Conv_R4);
                            }

                            compileState.ILGen.Emit(OpCodes.Cgt);

                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(long) || m_RightHandType == typeof(long))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_RightHandType, m_LineNumber);

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
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorGreaterNotSupportedFor0And1", "operator '>' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_LeftHandType)));

                    case ">=":
                        if(m_LeftHandType == typeof(double) || m_RightHandType == typeof(double))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_LeftHandType, m_LineNumber);
                            if (compileState.UsesSinglePrecision)
                            {
                                compileState.ILGen.Emit(OpCodes.Conv_R4);
                            }
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(double), m_RightHandType, m_LineNumber);
                            if (compileState.UsesSinglePrecision)
                            {
                                compileState.ILGen.Emit(OpCodes.Conv_R4);
                            }

                            compileState.ILGen.Emit(OpCodes.Clt);
                            compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                            compileState.ILGen.Emit(OpCodes.Ceq);

                            throw Return(compileState, typeof(int));
                        }
                        else if (m_LeftHandType == typeof(long) || m_RightHandType == typeof(long))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_RightHandType, m_LineNumber);

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
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorGreaterEqualsNotSupportedFor0And1", "operator '>=' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));

                    case "&&":
                        /* DeMorgan helps here a lot to convert the operations nicely */
                        if ((typeof(int) == m_LeftHandType || typeof(long) == m_LeftHandType) &&
                            (typeof(int) == m_RightHandType || typeof(long) == m_RightHandType))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(bool), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(bool), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.And);
                            throw Return(compileState, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorAndAndNotSupportedFor0And1", "operator '&&' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));

                    case "&":
                        if (typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.And);
                            throw Return(compileState, typeof(int));
                        }
                        else if (typeof(long) == m_LeftHandType || typeof(long) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.And);
                            throw Return(compileState, typeof(long));
                        }
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorAndNotSupportedFor0And1", "operator '&' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));

                    case "|":
                        if (typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Or);
                            throw Return(compileState, typeof(int));
                        }
                        else if (typeof(long) == m_LeftHandType || typeof(long) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            if(typeof(int) == m_LeftHandType)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldc_I8, 0xFFFFFFFFL);
                                compileState.ILGen.Emit(OpCodes.And);
                            }
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_RightHandType, m_LineNumber);
                            if (typeof(int) == m_RightHandType)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldc_I8, 0xFFFFFFFFL);
                                compileState.ILGen.Emit(OpCodes.And);
                            }
                            compileState.ILGen.Emit(OpCodes.Or);
                            throw Return(compileState, typeof(long));
                        }
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorOrNotSupportedFor0And1", "operator '|' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));

                    case "^":
                        if (typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Xor);
                            throw Return(compileState, typeof(int));
                        }
                        else if (typeof(long) == m_LeftHandType || typeof(long) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_LeftHandType, m_LineNumber);
                            if (typeof(int) == m_LeftHandType)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldc_I8, 0xFFFFFFFFL);
                                compileState.ILGen.Emit(OpCodes.And);
                            }
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(long), m_RightHandType, m_LineNumber);
                            if (typeof(int) == m_RightHandType)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldc_I8, 0xFFFFFFFFL);
                                compileState.ILGen.Emit(OpCodes.And);
                            }
                            compileState.ILGen.Emit(OpCodes.Xor);
                            throw Return(compileState, typeof(long));
                        }
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorXorNotSupportedFor0And1", "operator '^' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));

                    case "||":
                        if ((typeof(int) == m_LeftHandType || typeof(long) == m_LeftHandType) &&
                            (typeof(int) == m_RightHandType || typeof(long) == m_RightHandType))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(bool), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(bool), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Or);
                            throw Return(compileState, typeof(int));
                        }
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorOrOrNotSupportedFor0And1", "operator '||' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));

                    default:
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "UnknownOperator0For1And2", "unknown operator '{0}' for {1} and {2}"), m_Operator, compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                }
            }
        }
    }
}
