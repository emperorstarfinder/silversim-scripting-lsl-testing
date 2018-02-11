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
using SilverSim.Scripting.Lsl.Api.Base;
using SilverSim.Scripting.Lsl.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        public static string SetStringElement(string s, int index, char c)
        {
            int origIndex = index;
            if (index < 0)
            {
                index = s.Length + index;
            }

            if(index == 0)
            {
                return c.ToString() + s.Substring(1);
            }
            else if(index == s.Length - 1)
            {
                return s.Substring(0, s.Length - 1) + c.ToString();
            }
            else if (index > 0 && index < s.Length)
            {
                return s.Substring(0, index) + c.ToString() + s.Substring(index + 1);
            }
            else
            {
                throw new LocalizedScriptErrorException(new BaseApi.Variant(), "ListIndex0IsOutOfBounds", "'list' index '{0}' is out of bounds.", origIndex);
            }
        }

        public static void SetArrayElement(AnArray array, int index, BaseApi.Variant v)
        {
            int origIndex = index;
            if (index < 0)
            {
                index = array.Count + index;
            }

            if(index >= 0 && index < array.Count)
            {
                array[index] = v.Value;
            }
            else
            {
                throw new LocalizedScriptErrorException(new BaseApi.Variant(), "ListIndex0IsOutOfBounds", "'list' index '{0}' is out of bounds.", origIndex);
            }
        }

        public static char GetStringElement(string s, int index)
        {
            int origIndex = index;
            if (index < 0)
            {
                index = s.Length + index;
            }

            return (index >= 0 && index < s.Length) ? s[index] : char.MinValue;
        }

        public static BaseApi.Variant GetArrayElement(AnArray array, int index)
        {
            int origIndex = index;
            if(index < 0)
            {
                index = array.Count + index;
            }

            IValue v;
            if(array.TryGetValue(index, out v))
            {
                Type t = v.GetType();
                if(t == typeof(AString))
                {
                    return v.ToString();
                }
                else if(t == typeof(LSLKey))
                {
                    return (LSLKey)v;
                }
                else if (t == typeof(Quaternion))
                {
                    return (Quaternion)v;
                }
                else if (t == typeof(Vector3))
                {
                    return (Vector3)v;
                }
                else if (t == typeof(Integer))
                {
                    return v.AsInt;
                }
                else if (t == typeof(LongInteger))
                {
                    return v.AsLong;
                }
                else if (t == typeof(Real))
                {
                    return (double)(Real)v;
                }
            }
            else
            {
                throw new LocalizedScriptErrorException(new BaseApi.Variant(), "ListIndex0IsOutOfBounds", "'list' index '{0}' is out of bounds.", origIndex);
            }

            return string.Empty;
        }

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
                m_ProcessOrders.Add("+=", new State[] { State.RightHand });
                m_ProcessOrders.Add("-=", new State[] { State.RightHand });
                m_ProcessOrders.Add("*=", new State[] { State.RightHand });
                m_ProcessOrders.Add("/=", new State[] { State.RightHand });
                m_ProcessOrders.Add("%=", new State[] { State.RightHand });
                m_ProcessOrders.Add("|=", new State[] { State.RightHand });
                m_ProcessOrders.Add("&=", new State[] { State.RightHand });
                m_ProcessOrders.Add("^=", new State[] { State.RightHand });
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

            private Type GetMemberVarTreeType(LSLCompiler compiler, CompileState compileState, Tree t, Dictionary<string, object> localVars)
            {
                APIAccessibleMembersAttribute membersAttr;
                var selectorStack = new List<Tree>();
                while (t.Type == Tree.EntryType.OperatorBinary || t.Type == Tree.EntryType.ThisOperator)
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

                foreach (Tree selector in selectorStack)
                {
                    if (selector.Type == Tree.EntryType.ThisOperator)
                    {
                        varType = GetThisOperatorType(compiler, compileState, varType, selector, localVars);
                    }
                    else
                    {
                        FieldInfo fi;
                        PropertyInfo pi;
                        string member = selector.SubTree[1].Entry;
                        if (varType == typeof(Vector3) || varType == typeof(Quaternion))
                        {
                            varType = GetVectorOrQuaternionField(compileState, varType, member).FieldType;
                        }
                        else if((varType == typeof(string) || varType == typeof(AnArray)) && compileState.LanguageExtensions.EnableProperties)
                        {
                            if(member == "Length")
                            {
                                varType = typeof(int);
                            }
                            else
                            {
                                throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(
                                    compileState.CurrentCulture,
                                    "InvalidMemberAccess0To1",
                                    "Invalid member access '{0}' to {1}"), member, compileState.MapType(varType)));
                            }
                        }
                        else if ((membersAttr = compileState.GetAccessibleAPIMembersAttribute(varType)) == null)
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(
                                compileState.CurrentCulture,
                                "OperatorDorNotSupportedFor",
                                "operator '.' not supported for {0}"), compileState.MapType(varType)));
                        }
                        else if (membersAttr.Members.Length != 0 && !membersAttr.Members.Contains(member))
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(
                                compileState.CurrentCulture,
                                "InvalidMemberAccess0To1",
                                "Invalid member access '{0}' to {1}"), member, compileState.MapType(varType)));
                        }
                        else if ((fi = compileState.GetField(varType, member)) != null)
                        {
                            varType = fi.FieldType;
                        }
                        else if ((pi = varType.GetMemberProperty(compileState, member)) != null)
                        {
                            if (pi.GetGetMethod() != null)
                            {
                                varType = pi.PropertyType;
                            }
                            else
                            {
                                throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(
                                    compileState.CurrentCulture,
                                    "Member1IsReadOnlyForType0",
                                    "Member '{1}' of type '{0}' is read only."), compileState.MapType(varType), member));
                            }
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(
                                compileState.CurrentCulture,
                                "InvalidMemberAccess0To1",
                                "Invalid member access '{0}' to {1}"), member, compileState.MapType(varType)));
                        }
                    }
                }

                return varType;
            }

            private readonly Dictionary<Tree, LocalBuilder> m_CachedLeftSideValues = new Dictionary<Tree, LocalBuilder>();

            private Type[] GetParametersTypes(LSLCompiler compiler, CompileState compileState, Tree arguments, Dictionary<string, object> localVars)
            {
                var paramTypes = new List<Type>();
                for(int i = 1; i < arguments.SubTree.Count; ++i)
                {
                    Tree s = arguments.SubTree[i];
                    LocalBuilder lb;
                    Type retType;
                    if (m_CachedLeftSideValues.TryGetValue(s, out lb))
                    {
                        retType = lb.LocalType;
                    }
                    else
                    {
                        if (compileState.ILGen.HaveDebugOut)
                        {
                            compileState.ILGen.WriteLine("++++ Processing this-Operator parameter {0} / {1} ++++", i - 1, s.GetHashCode());
                        }
                        retType = compiler.ProcessExpressionPart(
                            compileState,
                            s,
                            localVars);
                        if (retType == typeof(void))
                        {
                            throw new CompilerException(m_LineNumber, this.GetLanguageString(
                                compileState.CurrentCulture,
                                "ExpressionForThisOperatorHasNoReturnValue",
                                "Expression for this operator has no return value"));
                        }
                        lb = DeclareLocal(compileState, retType);
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        m_CachedLeftSideValues.Add(s, lb);
                        if (compileState.ILGen.HaveDebugOut)
                        {
                            compileState.ILGen.WriteLine("++++ Processed this-Operator parameter {0} / {1} ++++", i - 1, s.GetHashCode());
                        }
                    }

                    paramTypes.Add(retType);
                }
                return paramTypes.ToArray();
            }

            private void GetParametersToStack(LSLCompiler compiler, CompileState compileState, Tree arguments, Dictionary<string, object> localVars)
            {
                for (int i = 1; i < arguments.SubTree.Count; ++i)
                {
                    Tree s = arguments.SubTree[i];
                    LocalBuilder lb;
                    Type retType;
                    if (m_CachedLeftSideValues.TryGetValue(s, out lb))
                    {
                        if (compileState.ILGen.HaveDebugOut)
                        {
                            compileState.ILGen.WriteLine("++++ Getting cached this-Operator parameter {0} / {1} ++++", i - 1, s.GetHashCode());
                        }
                        retType = lb.LocalType;
                        compileState.ILGen.Emit(OpCodes.Ldloc, lb);
                    }
                    else
                    {
                        if (compileState.ILGen.HaveDebugOut)
                        {
                            compileState.ILGen.WriteLine("++++ Processing this-Operator parameter {0} / {1} ++++", i - 1, s.GetHashCode());
                        }
                        retType = compiler.ProcessExpressionPart(
                            compileState,
                            s,
                            localVars);
                        if (retType == typeof(void))
                        {
                            throw new CompilerException(m_LineNumber, this.GetLanguageString(
                                compileState.CurrentCulture,
                                "ExpressionForThisOperatorHasNoReturnValue",
                                "Expression for this operator has no return value"));
                        }
                        lb = DeclareLocal(compileState, retType);
                        compileState.ILGen.Emit(OpCodes.Dup);
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        m_CachedLeftSideValues.Add(s, lb);
                        if (compileState.ILGen.HaveDebugOut)
                        {
                            compileState.ILGen.WriteLine("++++ Processed this-Operator parameter {0} / {1} ++++", i - 1, s.GetHashCode());
                        }
                    }
                }
            }

            private string GetThisParameterTypeString(CompileState compileState, Type[] paramTypes)
            {
                var res = new StringBuilder();
                foreach(Type paramType in paramTypes)
                {
                    if(res.Length != 0)
                    {
                        res.Append(", ");
                    }
                    res.Append(compileState.MapType(paramType));
                }
                return res.ToString();
            }

            private Type GetThisOperatorType(LSLCompiler compiler, CompileState compileState, Type varType, Tree arguments, Dictionary<string, object> localVars)
            {
                Type[] paramTypes = GetParametersTypes(compiler, compileState, arguments, localVars);
                if(varType == typeof(AnArray))
                {
                    if (!compileState.LanguageExtensions.EnableArrayThisOperator)
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsNotSupportedForType0", "This[{1}] operator is not supported for type '{0}'."),
                            compileState.MapType(varType),
                            GetThisParameterTypeString(compileState, paramTypes)));
                    }

                    var staticParamTypes = new List<Type> { typeof(AnArray) };
                    staticParamTypes.AddRange(paramTypes);
                    MethodInfo mi = typeof(LSLCompiler).GetMethod("GetArrayElement", BindingFlags.Static | BindingFlags.Public, null, staticParamTypes.ToArray(), null);
                    if (mi == null || mi.ReturnType != typeof(BaseApi.Variant))
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsNotSupportedForType0", "This[{1}] operator is not supported for type '{0}'."),
                            compileState.MapType(varType),
                            GetThisParameterTypeString(compileState, paramTypes)));
                    }
                    return typeof(BaseApi.Variant);
                }
                else if(varType == typeof(string))
                {
                    if (!compileState.LanguageExtensions.EnableCharacterType)
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsNotSupportedForType0", "This[{1}] operator is not supported for type '{0}'."),
                            compileState.MapType(varType),
                            GetThisParameterTypeString(compileState, paramTypes)));
                    }

                    var staticParamTypes = new List<Type> { typeof(string) };
                    staticParamTypes.AddRange(paramTypes);
                    MethodInfo mi = typeof(LSLCompiler).GetMethod("GetStringElement", BindingFlags.Static | BindingFlags.Public, null, staticParamTypes.ToArray(), null);
                    if (mi == null)
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsNotSupportedForType0", "This[{1}] operator is not supported for type '{0}'."),
                            compileState.MapType(varType),
                            GetThisParameterTypeString(compileState, paramTypes)));
                    }
                    return typeof(char);
                }

                PropertyInfo pInfo = varType.GetProperty("Item", paramTypes.ToArray());
                if(pInfo == null)
                {
                    throw new CompilerException(m_LineNumber, string.Format(
                        this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsNotSupportedForType0", "This[{1}] operator is not supported for type '{0}'."),
                        compileState.MapType(varType),
                        GetThisParameterTypeString(compileState, paramTypes)));
                }

                return pInfo.PropertyType;
            }

            private Type GetThisOperatorToStack(LSLCompiler compiler, CompileState compileState, Type varType, Tree arguments, Dictionary<string, object> localVars)
            {
                MethodInfo mi;
                Type[] paramTypes = GetParametersTypes(compiler, compileState, arguments, localVars);
                if (varType == typeof(AnArray))
                {
                    if (!compileState.LanguageExtensions.EnableArrayThisOperator)
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsNotSupportedForType0", "This[{1}] operator is not supported for type '{0}'."),
                            compileState.MapType(varType),
                            GetThisParameterTypeString(compileState, paramTypes)));
                    }

                    var staticParamTypes = new List<Type> { typeof(AnArray) };
                    staticParamTypes.AddRange(paramTypes);
                    mi = typeof(LSLCompiler).GetMethod("GetArrayElement", BindingFlags.Static | BindingFlags.Public, null, staticParamTypes.ToArray(), null);
                    if (mi == null || mi.ReturnType != typeof(BaseApi.Variant))
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsNotSupportedForType0", "This[{1}] operator is not supported for type '{0}'."),
                            compileState.MapType(varType),
                            GetThisParameterTypeString(compileState, paramTypes)));
                    }

                    GetParametersToStack(compiler, compileState, arguments, localVars);
                    compileState.ILGen.Emit(OpCodes.Call, mi);

                    return typeof(BaseApi.Variant);
                }
                else if (varType == typeof(string))
                {
                    if (!compileState.LanguageExtensions.EnableCharacterType)
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsNotSupportedForType0", "This[{1}] operator is not supported for type '{0}'."),
                            compileState.MapType(varType),
                            GetThisParameterTypeString(compileState, paramTypes)));
                    }

                    var staticParamTypes = new List<Type> { typeof(string) };
                    staticParamTypes.AddRange(paramTypes);
                    mi = typeof(LSLCompiler).GetMethod("GetStringElement", BindingFlags.Static | BindingFlags.Public, null, staticParamTypes.ToArray(), null);
                    if (mi == null || mi.ReturnType != typeof(BaseApi.Variant))
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsNotSupportedForType0", "This[{1}] operator is not supported for type '{0}'."),
                            compileState.MapType(varType),
                            GetThisParameterTypeString(compileState, paramTypes)));
                    }

                    GetParametersToStack(compiler, compileState, arguments, localVars);
                    compileState.ILGen.Emit(OpCodes.Call, mi);

                    return typeof(char);
                }

                PropertyInfo pInfo = varType.GetProperty("Item", paramTypes.ToArray());
                if (pInfo == null)
                {
                    throw new CompilerException(m_LineNumber, string.Format(
                        this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsNotSupportedForType0", "This[{1}] operator is not supported for type '{0}'."),
                        compileState.MapType(varType),
                        GetThisParameterTypeString(compileState, paramTypes)));
                }
                mi = pInfo.GetGetMethod();
                if (mi == null)
                {
                    throw new CompilerException(m_LineNumber, string.Format(
                        this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsWriteOnlyForType0", "This[{1}] operator is write only for type '{0}'."),
                        compileState.MapType(varType),
                        GetThisParameterTypeString(compileState, paramTypes)));
                }

                if(mi.IsStatic)
                {
                    compileState.ILGen.Emit(OpCodes.Pop);
                }

                GetParametersToStack(compiler, compileState, arguments, localVars);

                if (mi.IsVirtual)
                {
                    compileState.ILGen.Emit(OpCodes.Callvirt, mi);
                }
                else
                {
                    compileState.ILGen.Emit(OpCodes.Call, mi);
                }

                return pInfo.PropertyType;
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
                else if(varType == typeof(string) && compileState.LanguageExtensions.EnableProperties)
                {
                    if(memberName != "Length")
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "InvalidMemberAccess0To1", "Invalid member access '{0}' to {1}"),
                            memberName,
                            compileState.MapType(varType)));
                    }
                    PropertyInfo pi = typeof(string).GetProperty("Length");
                    compileState.ILGen.Emit(OpCodes.Call, pi.GetGetMethod());
                    varType = pi.PropertyType;
                }
                else if (varType == typeof(AnArray) && compileState.LanguageExtensions.EnableProperties)
                {
                    if (memberName != "Length")
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "InvalidMemberAccess0To1", "Invalid member access '{0}' to {1}"),
                            memberName,
                            compileState.MapType(varType)));
                    }
                    PropertyInfo pi = typeof(AnArray).GetProperty("Count");
                    compileState.ILGen.Emit(OpCodes.Call, pi.GetGetMethod());
                    varType = pi.PropertyType;
                }
                else if ((membersAttr = compileState.GetAccessibleAPIMembersAttribute(varType)) != null)
                {
                    PropertyInfo pi;
                    FieldInfo fi;
                    if (membersAttr.Members.Length != 0 && !membersAttr.Members.Contains(memberName))
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "InvalidMemberAccess0To1", "Invalid member access '{0}' to {1}"),
                            memberName,
                            compileState.MapType(varType)));
                    }

                    if ((fi = compileState.GetField(varType, memberName)) != null)
                    {
                        varType = fi.FieldType;
                        compileState.ILGen.Emit(OpCodes.Ldfld, fi);
                    }
                    else if ((pi = varType.GetMemberProperty(compileState, memberName)) != null)
                    {
                        MethodInfo mi;
                        if ((mi = pi.GetGetMethod()) == null)
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "Member1IsWriteOnlyForType0", "Member '{1}' of type '{0}' is write only."), compileState.MapType(varType), memberName));
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

            private Type GetMemberVarTreeToStack(LSLCompiler compiler, CompileState compileState, Tree t, Dictionary<string, object> localVars)
            {
                var selectorStack = new List<Tree>();
                while(t.Type == Tree.EntryType.OperatorBinary || t.Type == Tree.EntryType.ThisOperator)
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

                var locals = new Dictionary<Type, LocalBuilder>();

                foreach (Tree selector in selectorStack)
                {
                    switch (selector.Type)
                    {
                        case Tree.EntryType.OperatorBinary:
                            string member = selector.SubTree[1].Entry;
                            if (varType.IsValueType)
                            {
                                LocalBuilder lb;
                                if (!locals.TryGetValue(varType, out lb))
                                {
                                    lb = DeclareLocal(compileState, varType);
                                    locals.Add(varType, lb);
                                }
                                compileState.ILGen.Emit(OpCodes.Stloc, lb);
                                compileState.ILGen.Emit(OpCodes.Ldloca, lb);
                            }

                            varType = GetMemberSelectorToStack(compileState, varType, member);
                            break;

                        case Tree.EntryType.ThisOperator:
                            varType = GetThisOperatorToStack(compiler, compileState, varType, selector, localVars);
                            break;
                    }
                }

                return varType;
            }

            private Type SetThisOperatorFromStack(LSLCompiler compiler, CompileState compileState, Type varType, Type valueArgument, Tree arguments, Dictionary<string, object> localVars)
            {
                Type[] paramTypes = GetParametersTypes(compiler, compileState, arguments, localVars);
                LocalBuilder swapLb = DeclareLocal(compileState, valueArgument);
                compileState.ILGen.Emit(OpCodes.Stloc, swapLb);

                MethodInfo mi;
                if (varType == typeof(AnArray))
                {
                    if (!compileState.LanguageExtensions.EnableArrayThisOperator)
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsNotSupportedForType0", "This[{1}] operator is not supported for type '{0}'."),
                            compileState.MapType(varType),
                            GetThisParameterTypeString(compileState, paramTypes)));
                    }

                    var staticParamTypes = new List<Type> { typeof(AnArray) };
                    staticParamTypes.AddRange(paramTypes);
                    staticParamTypes.Add(typeof(BaseApi.Variant));
                    mi = typeof(LSLCompiler).GetMethod("SetArrayElement", BindingFlags.Static | BindingFlags.Public, null, staticParamTypes.ToArray(), null);
                    if (mi == null)
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsNotSupportedForType0", "This[{1}] operator is not supported for type '{0}'."),
                            compileState.MapType(varType),
                            GetThisParameterTypeString(compileState, paramTypes)));
                    }

                    GetParametersToStack(compiler, compileState, arguments, localVars);
                    compileState.ILGen.Emit(OpCodes.Ldloc, swapLb);
                    ProcessImplicitCasts(compileState, typeof(BaseApi.Variant), valueArgument, m_LineNumber);
                    compileState.ILGen.Emit(OpCodes.Call, mi);

                    return typeof(BaseApi.Variant);
                }
                else if (varType == typeof(string))
                {
                    if (!compileState.LanguageExtensions.EnableCharacterType)
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsNotSupportedForType0", "This[{1}] operator is not supported for type '{0}'."),
                            compileState.MapType(varType),
                            GetThisParameterTypeString(compileState, paramTypes)));
                    }

                    var staticParamTypes = new List<Type> { typeof(string) };
                    staticParamTypes.AddRange(paramTypes);
                    staticParamTypes.Add(typeof(char));
                    mi = typeof(LSLCompiler).GetMethod("SetStringElement", BindingFlags.Static | BindingFlags.Public, null, staticParamTypes.ToArray(), null);
                    if (mi == null)
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsNotSupportedForType0", "This[{1}] operator is not supported for type '{0}'."),
                            compileState.MapType(varType),
                            GetThisParameterTypeString(compileState, paramTypes)));
                    }

                    GetParametersToStack(compiler, compileState, arguments, localVars);
                    compileState.ILGen.Emit(OpCodes.Ldloc, swapLb);
                    ProcessImplicitCasts(compileState, typeof(char), valueArgument, m_LineNumber);
                    compileState.ILGen.Emit(OpCodes.Call, mi);
                    /* special handling here to pass the string back to caller */
                    return typeof(char);
                }

                PropertyInfo pInfo = varType.GetProperty("Item", paramTypes.ToArray());
                if (pInfo == null)
                {
                    throw new CompilerException(m_LineNumber, string.Format(
                        this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsNotSupportedForType0", ""),
                        "This[{1}] operator is not supported for type '{0}'.",
                        compileState.MapType(varType),
                        GetThisParameterTypeString(compileState, paramTypes)));
                }
                mi = pInfo.GetSetMethod();
                if (mi == null)
                {
                    throw new CompilerException(m_LineNumber, string.Format(
                        this.GetLanguageString(compileState.CurrentCulture, "ThisOperator1IsReadOnlyForType0", ""),
                        "This[{1}] operator is read only for type '{0}'.",
                        compileState.MapType(varType),
                        GetThisParameterTypeString(compileState, paramTypes)));
                }

                if (mi.IsStatic)
                {
                    compileState.ILGen.Emit(OpCodes.Pop);
                }

                GetParametersToStack(compiler, compileState, arguments, localVars);
                compileState.ILGen.Emit(OpCodes.Ldloc, swapLb);

                if (mi.IsVirtual)
                {
                    compileState.ILGen.Emit(OpCodes.Callvirt, mi);
                }
                else
                {
                    compileState.ILGen.Emit(OpCodes.Call, mi);
                }

                return pInfo.PropertyType;
            }

            private void SetMemberSelectorFromStack(CompileState compileState, Type varType, string memberName)
            {
                APIAccessibleMembersAttribute membersAttr;
                if (varType == typeof(Vector3) || varType == typeof(Quaternion))
                {
                    FieldInfo fi = GetVectorOrQuaternionField(compileState, varType, memberName);
                    compileState.ILGen.Emit(OpCodes.Stfld, fi);
                }
                else if ((varType == typeof(string) || varType == typeof(AnArray)) && compileState.LanguageExtensions.EnableProperties)
                {
                    throw new CompilerException(m_LineNumber, string.Format(
                        this.GetLanguageString(compileState.CurrentCulture, "InvalidMemberAccess0To1", "Invalid member access '{0}' to {1}"),
                        memberName,
                        compileState.MapType(varType)));
                }
                else if ((membersAttr = compileState.GetAccessibleAPIMembersAttribute(varType)) != null)
                {
                    PropertyInfo pi;
                    FieldInfo fi;
                    MethodInfo mi;
                    if (membersAttr.Members.Length != 0 && !membersAttr.Members.Contains(memberName))
                    {
                        throw new CompilerException(m_LineNumber, string.Format(
                            this.GetLanguageString(compileState.CurrentCulture, "InvalidMemberAccess0To1", "Invalid member access '{0}' to {1}"),
                            memberName,
                            compileState.MapType(varType)));
                    }

                    if ((fi = compileState.GetField(varType, memberName)) != null)
                    {
                        if(fi.IsStatic || fi.IsInitOnly)
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "Member1IsReadOnlyForType0", "Member '{1}' of type '{0}' is read only."), compileState.MapType(varType), memberName));
                        }
                        compileState.ILGen.Emit(OpCodes.Stfld, fi);
                    }
                    else if ((pi = varType.GetMemberProperty(compileState, memberName)) != null)
                    {
                        mi = pi.GetSetMethod();
                        if(mi == null)
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "Member1IsReadOnlyForType0", "Member '{1}' of type '{0}' is read only."), compileState.MapType(varType), memberName));
                        }

                        if(mi.IsStatic)
                        {
                            compileState.ILGen.BeginScope();
                            LocalBuilder swapLb = compileState.ILGen.DeclareLocal(pi.PropertyType);
                            compileState.ILGen.Emit(OpCodes.Stloc, swapLb);
                            compileState.ILGen.Emit(OpCodes.Pop);
                            compileState.ILGen.Emit(OpCodes.Ldloc, swapLb);
                            compileState.ILGen.EndScope();
                        }

                        if (mi.IsVirtual)
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
            }

            public class SelectorTree
            {
                public Tree.EntryType Operator;
                public LocalBuilder Local;
                public Type Type;
                public string Member;
                public bool RequiredToSet;
                public Tree Arguments;
            }

            private void SetMemberVarTreeFromStack(LSLCompiler lslCompiler, CompileState compileState, Tree t, Dictionary<string, object> localVars)
            {
                if (compileState.ILGen.HaveDebugOut)
                {
                    compileState.ILGen.WriteLine("======== SetMemberVarTreeFromStack: Begin ========");
                }

                var selectorStack = new List<Tree>();
                while (t.Type == Tree.EntryType.OperatorBinary || t.Type == Tree.EntryType.ThisOperator)
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

                LocalBuilder swapLb = DeclareLocal(compileState, m_LeftHandType);
                compileState.ILGen.Emit(OpCodes.Stloc, swapLb);
                if (compileState.ILGen.HaveDebugOut)
                {
                    compileState.ILGen.WriteLine("======== SetMemberVarTreeFromStack: Fetch reference ========");
                }

                Type varType = GetVarToStack(compileState, varInfo);
                bool storeBackVar = false;

                if(compileState.IsCloneOnAssignment(varType))
                {
                    storeBackVar = true;
                    compileState.ILGen.Emit(OpCodes.Newobj, compileState.GetCopyConstructor(varType));
                }
                else if(varType.IsValueType || varType == typeof(string))
                {
                    storeBackVar = true;
                }

                var selectorTree = new List<SelectorTree>();

                bool firstRequiredSelector = storeBackVar;
                LocalBuilder varLb = null;

                for(int i = 0; i < selectorStack.Count - 1; ++i)
                {
                    if (compileState.ILGen.HaveDebugOut)
                    {
                        compileState.ILGen.WriteLine("-------- SetMemberVarTreeFromStack: Get: {0}: {1} --------", varType.FullName, selectorStack[i].SubTree[1].Entry);
                    }

                    Tree sel = selectorStack[i];
                    LocalBuilder lb = DeclareLocal(compileState, varType);
                    varLb = varLb ?? lb;
                    selectorTree.Insert(0, new SelectorTree
                    {
                        Operator = sel.Type,
                        Type = varType,
                        Local = lb,
                        Member = sel.SubTree.Count > 1 ? sel.SubTree[1].Entry : string.Empty,
                        Arguments = sel
                    });

                    if(varType.IsValueType)
                    {
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        compileState.ILGen.Emit(OpCodes.Ldloca, lb);
                    }
                    else if(compileState.IsCloneOnAssignment(varType))
                    {
                        compileState.ILGen.Emit(OpCodes.Newobj, compileState.GetCopyConstructor(varType));
                        compileState.ILGen.Emit(OpCodes.Dup);
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                    }
                    else
                    {
                        compileState.ILGen.Emit(OpCodes.Dup);
                        compileState.ILGen.Emit(OpCodes.Stloc, lb);
                    }

                    if (sel.Type == Tree.EntryType.ThisOperator)
                    {
                        Type vType = varType;
                        varType = GetThisOperatorToStack(lslCompiler, compileState, varType, sel, localVars);
                        if(vType == typeof(string))
                        {
                            compileState.ILGen.Emit(OpCodes.Stloc, lb);
                        }
                    }
                    else
                    {
                        varType = GetMemberSelectorToStack(compileState, varType, sel.SubTree[1].Entry);
                    }
                    if(varType.IsValueType || varType == typeof(string) || compileState.IsCloneOnAssignment(varType))
                    {
                        firstRequiredSelector = true;
                    }
                    selectorTree[0].RequiredToSet = firstRequiredSelector;
                }

                LocalBuilder refLb = DeclareLocal(compileState, varType);
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

                if (compileState.ILGen.HaveDebugOut)
                {
                    compileState.ILGen.WriteLine("======== SetMemberVarTreeFromStack: Set final member ========");
                }

                compileState.ILGen.Emit(OpCodes.Ldloc, swapLb);

                Tree finalSel = selectorStack[selectorStack.Count - 1];
                if (finalSel.Type == Tree.EntryType.ThisOperator)
                {
                    SetThisOperatorFromStack(lslCompiler, compileState, varType, swapLb.LocalType, finalSel, localVars);
                    if(varType == typeof(string))
                    {
                        compileState.ILGen.Emit(OpCodes.Stloc, refLb);
                    }
                }
                else
                {
                    SetMemberSelectorFromStack(compileState, varType, finalSel.SubTree[1].Entry);
                }

                if (compileState.ILGen.HaveDebugOut)
                {
                    compileState.ILGen.WriteLine("======== SetMemberVarTreeFromStack: Save back necessary locals ========");
                }

                foreach (SelectorTree sel in selectorTree)
                {
                    if(!sel.RequiredToSet)
                    {
                        break;
                    }

                    if (compileState.ILGen.HaveDebugOut)
                    {
                        compileState.ILGen.WriteLine("-------- SetMemberVarTreeFromStack: Set: {0}: {1} --------", sel.Type.FullName, sel.Member);
                    }

                    if (sel.Type.IsValueType)
                    {
                        compileState.ILGen.Emit(OpCodes.Ldloca, sel.Local);
                    }
                    else
                    {
                        compileState.ILGen.Emit(OpCodes.Ldloc, sel.Local);
                    }
                    compileState.ILGen.Emit(OpCodes.Ldloc, refLb);
                    if (sel.Operator == Tree.EntryType.ThisOperator)
                    {
                        SetThisOperatorFromStack(lslCompiler, compileState, sel.Type, refLb.LocalType, sel.Arguments, localVars);
                        if(sel.Type == typeof(string))
                        {
                            compileState.ILGen.Emit(OpCodes.Stloc, sel.Local);
                        }
                    }
                    else
                    {
                        SetMemberSelectorFromStack(compileState, sel.Type, sel.Member);
                    }
                    refLb = sel.Local;
                }

                if (storeBackVar)
                {
                    compileState.ILGen.Emit(OpCodes.Ldloc, varLb);
                    SetVarFromStack(compileState, varInfo, m_LineNumber);
                }

                if (compileState.ILGen.HaveDebugOut)
                {
                    compileState.ILGen.WriteLine("======== SetMemberVarTreeFromStack: Finished ========");
                }
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
                    if ((m_LeftHand.Type == Tree.EntryType.OperatorBinary && m_LeftHand.Entry == ".") ||
                        m_LeftHand.Type == Tree.EntryType.ThisOperator)
                    {
                        m_LeftHandType = GetMemberVarTreeType(lslCompiler, compileState, m_LeftHand, localVars);
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
                        case "&=":
                        case "|=":
                        case "^=":
                            if(compileState.LanguageExtensions.EnableLogicalModifyAssignments)
                            {
                                goto case "+=";
                            }
                            break;

                        case "+=":
                        case "-=":
                        case "*=":
                        case "/=":
                        case "%=":
                            if ((m_LeftHand.Type == Tree.EntryType.OperatorBinary && m_LeftHand.Entry == ".") ||
                                m_LeftHand.Type == Tree.EntryType.ThisOperator)
                            {
                                m_LeftHandType = GetMemberVarTreeType(lslCompiler, compileState, m_LeftHand, localVars);
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
                                lslCompiler,
                                compileState,
                                localVars);
                            break;

                        case "|=":
                        case "&=":
                        case "^=":
                            if(compileState.LanguageExtensions.EnableLogicalModifyAssignments)
                            {
                                goto case "+=";
                            }
                            throw new CompilerException(m_LineNumber, string.Format("Internal Error! Unexpected operator '{0}'", m_Operator));

                        case "+=":
                        case "-=":
                        case "*=":
                        case "/=":
                        case "%=":
                            ProcessOperator_ModifyAssignment(
                                lslCompiler,
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
                    throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "0IsNotAMemberOfType1", "'{0}' is not a member of type {1}"), m_RightHand.Type.ToString() + " " + m_RightHand.Entry, compileState.MapType(m_LeftHandType)));
                }
                Type t = GetMemberSelectorToStack(compileState, m_LeftHandType, m_RightHand.Entry);
                throw Return(compileState, t);
            }

            public void ProcessOperator_Assignment(
                LSLCompiler lslCompiler,
                CompileState compileState,
                Dictionary<string, object> localVars)
            {
                if ((m_LeftHand.Type == Tree.EntryType.OperatorBinary && m_LeftHand.Entry == ".") ||
                    m_LeftHand.Type == Tree.EntryType.ThisOperator)
                {
                    if (null != m_RightHandLocal)
                    {
                        compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                    }
                    ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);
                    if(m_LeftHandType == m_RightHandType && m_RightHandType == typeof(AnArray))
                    {
                        /* duplicate array to adhere to LSL language features */
                        compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[] { typeof(AnArray) }));
                    }
                    compileState.ILGen.Emit(OpCodes.Dup);
                    SetMemberVarTreeFromStack(lslCompiler, compileState, m_LeftHand, localVars);
                    throw Return(compileState, m_LeftHandType);
                }
                else
                {
                    object varInfo = localVars[m_LeftHand.Entry];
                    m_LeftHandType = GetVarType(varInfo);
                    ProcessImplicitCasts(compileState, m_LeftHandType, m_RightHandType, m_LineNumber);
                    if (m_LeftHandType == m_RightHandType && m_RightHandType == typeof(AnArray))
                    {
                        /* duplicate array to adhere to LSL language features */
                        compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[] { typeof(AnArray) }));
                    }
                    compileState.ILGen.Emit(OpCodes.Dup);
                    SetVarFromStack(
                        compileState,
                        varInfo,
                        m_LineNumber);
                    throw Return(compileState, m_LeftHandType);
                }
            }

            public void ProcessOperator_ModifyAssignment(
                LSLCompiler lslCompiler,
                CompileState compileState,
                Dictionary<string, object> localVars)
            {
                object varInfo;
                bool isComponentAccess = false;
                if ((m_LeftHand.Type == Tree.EntryType.OperatorBinary && m_LeftHand.Entry == ".") ||
                    m_LeftHand.Type == Tree.EntryType.ThisOperator)
                {
                    m_LeftHandType = GetMemberVarTreeToStack(lslCompiler, compileState, m_LeftHand, localVars);
                    isComponentAccess = true;
                    varInfo = null;
                }
                else
                {
                    varInfo = localVars[m_LeftHand.Entry];
                    GetVarToStack(compileState, varInfo);
                    if(m_Operator == "+=" && m_LeftHandType == typeof(AnArray) && typeof(AnArray).GetMethod("Add", new Type[] { m_RightHandType }) != null)
                    {
                        compileState.ILGen.Emit(OpCodes.Dup);
                    }
                }

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
                        if(typeof(int) == m_LeftHandType || typeof(double) == m_LeftHandType || typeof(long) == m_LeftHandType || typeof(char) == m_LeftHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Add);
                            break;
                        }
                        if(typeof(string) == m_LeftHandType && typeof(string) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Call, typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }));
                            break;
                        }
                        else if(typeof(string) == m_LeftHandType && compileState.LanguageExtensions.EnableImplicitTypecastToStringOnAddOperator)
                        {
                            ProcessCasts(compileState, typeof(string), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Call, typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }));
                            break;
                        }
                        else if(typeof(AnArray) == m_LeftHandType)
                        {
                            if (typeof(AnArray) == m_RightHandType)
                            {
                                mi = typeof(AnArray).GetMethod("AddRange", new Type[] { m_RightHandType });
                            }
                            else if(typeof(char) == m_RightHandType)
                            {
                                mi = typeof(LSLCompiler).GetMethod("AddCharToArray", BindingFlags.Static, null, new Type[] { typeof(AnArray), typeof(char) }, null);
                            }
                            else
                            {
                                mi = typeof(AnArray).GetMethod("Add", new Type[] { m_RightHandType });
                            }
                            if (mi != null)
                            {
                                if(m_RightHandType.IsValueType && mi.GetParameters()[0].ParameterType == typeof(IValue))
                                {
                                    compileState.ILGen.Emit(OpCodes.Box, m_RightHandType);
                                }
                                if (mi.IsVirtual)
                                {
                                    compileState.ILGen.Emit(OpCodes.Callvirt, mi);
                                }
                                else
                                {
                                    compileState.ILGen.Emit(OpCodes.Call, mi);
                                }
                                break;
                            }
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
                        if(typeof(int) == m_LeftHandType || typeof(double) == m_LeftHandType || typeof(long) == m_LeftHandType || typeof(char) == m_LeftHandType)
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

                    case "&=":
                        if((typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType) ||
                            (typeof(long) == m_LeftHandType && typeof(long) == m_RightHandType))
                        {
                            compileState.ILGen.Emit(OpCodes.And);
                        }
                        else if(typeof(long) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Conv_I8);
                            compileState.ILGen.Emit(OpCodes.And);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorAndEqualsNotSupportedFor0And1", "operator '&=' not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                        }
                        break;

                    case "|=":
                        if ((typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType) ||
                            (typeof(long) == m_LeftHandType && typeof(long) == m_RightHandType))
                        {
                            compileState.ILGen.Emit(OpCodes.Or);
                        }
                        else if (typeof(long) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Conv_I8);
                            compileState.ILGen.Emit(OpCodes.Or);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorOrEqualsNotSupportedFor0And1", "operator '|=' not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                        }
                        break;

                    case "^=":
                        if ((typeof(int) == m_LeftHandType && typeof(int) == m_RightHandType) ||
                            (typeof(long) == m_LeftHandType && typeof(long) == m_RightHandType))
                        {
                            compileState.ILGen.Emit(OpCodes.Xor);
                        }
                        else if (typeof(long) == m_LeftHandType && typeof(int) == m_RightHandType)
                        {
                            compileState.ILGen.Emit(OpCodes.Conv_I8);
                            compileState.ILGen.Emit(OpCodes.Xor);
                        }
                        else
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorXorEqualsNotSupportedFor0And1", "operator '^=' not supported for '{0}' and '{1}'"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                        }
                        break;

                    default:
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "Operator0Unknown", "Operator '{0}' unknown"), m_Operator));
                }

                compileState.ILGen.Emit(OpCodes.Dup);
                if(isComponentAccess)
                {
                    SetMemberVarTreeFromStack(lslCompiler, compileState, m_LeftHand, localVars);
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
                        else if (m_LeftHandType == typeof(char) && m_RightHandType == typeof(char))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Add);
                            throw Return(compileState, typeof(char));
                        }
                        else if ((m_LeftHandType == typeof(int) || m_LeftHandType == typeof(char)) &&
                            (m_RightHandType == typeof(int) || m_RightHandType == typeof(char)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Add);
                            throw Return(compileState, typeof(int));
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
                        else if(m_LeftHandType == typeof(AnArray))
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
                        else if(m_LeftHandType == typeof(string) || m_LeftHandType == typeof(LSLKey))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            if (m_LeftHandType == typeof(LSLKey))
                            {
                                compileState.ILGen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", Type.EmptyTypes));
                            }
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            if (compileState.LanguageExtensions.EnableImplicitTypecastToStringOnAddOperator)
                            {
                                ProcessCasts(compileState, typeof(string), m_RightHandType, m_LineNumber);
                            }
                            else
                            {
                                ProcessImplicitCasts(compileState, typeof(string), m_RightHandType, m_LineNumber);
                            }
                            compileState.ILGen.Emit(OpCodes.Call, typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }));
                            throw Return(compileState, typeof(string));
                        }
                        else if((m_RightHandType == typeof(string) || m_RightHandType == typeof(LSLKey)) && compileState.LanguageExtensions.EnableImplicitTypecastToStringOnAddOperator)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessCasts(compileState, typeof(string), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            if (m_RightHandType == typeof(LSLKey))
                            {
                                compileState.ILGen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", Type.EmptyTypes));
                            }
                            compileState.ILGen.Emit(OpCodes.Call, typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }));
                            throw Return(compileState, typeof(string));
                        }

                        if (typeof(double) != m_LeftHandType && typeof(int) != m_LeftHandType && typeof(string) != m_LeftHandType)
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
                        else if (m_LeftHandType == typeof(char) && m_RightHandType == typeof(char))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Sub);
                            throw Return(compileState, typeof(char));
                        }
                        else if ((m_LeftHandType == typeof(int) || m_LeftHandType == typeof(char)) &&
                            (m_RightHandType == typeof(int) || m_RightHandType == typeof(char)))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            compileState.ILGen.Emit(OpCodes.Sub);
                            throw Return(compileState, typeof(int));
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
                        else if (m_LeftHandType == typeof(int) || m_LeftHandType == typeof(char))
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
                        else if (m_LeftHandType == typeof(int) || m_LeftHandType == typeof(char))
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
                        else if (m_LeftHandType == typeof(int) || m_LeftHandType == typeof(char))
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
                        else if (m_LeftHandType == typeof(int) || m_LeftHandType == typeof(char))
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
                        else if (m_LeftHandType == typeof(int) || m_LeftHandType == typeof(char))
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
                            MethodInfo operatorMethodInfo = m_LeftHandType.GetMethod("op_GreaterThan", new Type[] { m_LeftHandType, m_LeftHandType });
                            if (operatorMethodInfo != null)
                            {
                                compileState.ILGen.Emit(OpCodes.Call, operatorMethodInfo);

                                throw Return(compileState, typeof(int));
                            }
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
                        else if (m_LeftHandType == typeof(int) || m_LeftHandType == typeof(char))
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

                            MethodInfo operatorMethodInfo = m_LeftHandType.GetMethod("op_GreaterThanOrEqual", new Type[] { m_LeftHandType, m_LeftHandType });
                            if (operatorMethodInfo != null)
                            {
                                compileState.ILGen.Emit(OpCodes.Call, operatorMethodInfo);

                                throw Return(compileState, typeof(int));
                            }
                        }
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorGreaterEqualsNotSupportedFor0And1", "operator '>=' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));

                    case "&&":
                        /* DeMorgan helps here a lot to convert the operations nicely */
                        compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                        try
                        {
                            ProcessImplicitCasts(compileState, typeof(bool), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(bool), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.And);
                        }
                        catch(CompilerException)
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorAndAndNotSupportedFor0And1", "operator '>=' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                        }
                        throw Return(compileState, typeof(int));

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
                        try
                        {
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_LeftHandLocal);
                            ProcessImplicitCasts(compileState, typeof(bool), m_LeftHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Ldloc, m_RightHandLocal);
                            ProcessImplicitCasts(compileState, typeof(bool), m_RightHandType, m_LineNumber);
                            compileState.ILGen.Emit(OpCodes.Or);
                        }
                        catch (CompilerException)
                        {
                            throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorOrOrNotSupportedFor0And1", "operator '>=' not supported for {0} and {1}"), compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                        }
                        throw Return(compileState, typeof(int));

                    default:
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "UnknownOperator0For1And2", "unknown operator '{0}' for {1} and {2}"), m_Operator, compileState.MapType(m_LeftHandType), compileState.MapType(m_RightHandType)));
                }
            }
        }
    }
}
