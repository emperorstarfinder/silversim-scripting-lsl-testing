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

using SilverSim.Scripting.Lsl.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        private object CallConstantMethodFunction(CompileState compileState, ApiMethodInfo apiMethod, List<Type> paramTypes, List<object> paramValues)
        {
            int idx;
            ParameterInfo[] pi = apiMethod.Method.GetParameters();
            for(idx = 0; idx < pi.Length; ++idx)
            {
                object p = paramValues[idx];
                if(!TryConstantCastTo(compileState, ref p, pi[idx].ParameterType))
                {
                    return null;
                }
                paramValues[idx] = p;
            }
            return apiMethod.Method.Invoke(apiMethod.Api, paramValues.ToArray());
        }

        private object CallConstantInlineFunction(CompileState compileState, InlineApiMethodInfo inlineApiMethod, List<Type> paramTypes, List<object> paramValues)
        {
            if (inlineApiMethod.CompiledDynamicMethod == null)
            {
                List<Type> targetParameters = new List<Type>();
                foreach(InlineApiMethodInfo.ParameterInfo ipi in inlineApiMethod.Parameters)
                {
                    targetParameters.Add(ipi.ParameterType);
                }
                DynamicMethod dynMethod = new DynamicMethod(
                    "dyn_pure_" + inlineApiMethod.FunctionName,
                    inlineApiMethod.ReturnType,
                    targetParameters.ToArray());
                ILGenDumpProxy ilgen = new ILGenDumpProxy(dynMethod.GetILGenerator(), null, null);
                for (int i = 0; i < paramValues.Count; ++i)
                {
                    ilgen.Emit(OpCodes.Ldarg, i);
                }
                inlineApiMethod.Generate(ilgen);
                ilgen.Emit(OpCodes.Ret);
                inlineApiMethod.CompiledDynamicMethod = dynMethod;
                List<Type> genArgs = new List<Type>(targetParameters)
                {
                    inlineApiMethod.ReturnType
                };
                Type delegateType = null;
                switch (paramValues.Count)
                {
                    case 0:
                        delegateType = typeof(Func<>).GetGenericTypeDefinition().MakeGenericType(genArgs.ToArray());
                        break;
                    case 1:
                        delegateType = typeof(Func<,>).GetGenericTypeDefinition().MakeGenericType(genArgs.ToArray());
                        break;
                    case 2:
                        delegateType = typeof(Func<,,>).GetGenericTypeDefinition().MakeGenericType(genArgs.ToArray());
                        break;
                    case 3:
                        delegateType = typeof(Func<,,,>).GetGenericTypeDefinition().MakeGenericType(genArgs.ToArray());
                        break;
                    case 4:
                        delegateType = typeof(Func<,,,,>).GetGenericTypeDefinition().MakeGenericType(genArgs.ToArray());
                        break;
                    case 5:
                        delegateType = typeof(Func<,,,,,>).GetGenericTypeDefinition().MakeGenericType(genArgs.ToArray());
                        break;
                }
                if (delegateType == null)
                {
                    return null;
                }
                inlineApiMethod.CompiledDynamicDelegate = dynMethod.CreateDelegate(delegateType);
            }

            InlineApiMethodInfo.ParameterInfo[] pi = inlineApiMethod.Parameters;
            int idx;
            for (idx = 0; idx < pi.Length; ++idx)
            {
                object p = paramValues[idx];
                if (!TryConstantCastTo(compileState, ref p, pi[idx].ParameterType))
                {
                    return null;
                }
                paramValues[idx] = p;
            }

            return inlineApiMethod.CompiledDynamicDelegate.DynamicInvoke(paramValues.ToArray());
        }

        private void SolveFunctionConstantOperations(CompileState cs, Tree st, Dictionary<string, List<ApiMethodInfo>> methods, Dictionary<string, List<InlineApiMethodInfo>> inlineMethods, bool firstMustMatch)
        {
            bool areAllArgumentsConstant = true;
            List<Type> paramTypes = new List<Type>();
            List<object> paramValues = new List<object>();
            foreach (Tree ot in st.SubTree)
            {
                Type oType = ot.Value.GetType();
                if (oType == typeof(ConstantValueRotation))
                {
                    paramTypes.Add(typeof(Quaternion));
                    paramValues.Add(((ConstantValueRotation)ot.Value).Value);
                }
                else if (oType == typeof(ConstantValueVector))
                {
                    paramTypes.Add(typeof(Vector3));
                    paramValues.Add(((ConstantValueVector)ot.Value).Value);
                }
                else if (oType == typeof(Tree.ConstantValueChar))
                {
                    paramTypes.Add(typeof(char));
                    paramValues.Add(((Tree.ConstantValueChar)ot.Value).Value);
                }
                else if (oType == typeof(Tree.ConstantValueFloat))
                {
                    paramTypes.Add(typeof(double));
                    paramValues.Add(((Tree.ConstantValueFloat)ot.Value).Value);
                }
                else if (oType == typeof(Tree.ConstantValueInt))
                {
                    paramTypes.Add(typeof(int));
                    paramValues.Add(((Tree.ConstantValueInt)ot.Value).Value);
                }
                else if (oType == typeof(Tree.ConstantValueLong))
                {
                    paramTypes.Add(typeof(long));
                    paramValues.Add(((Tree.ConstantValueLong)ot.Value).Value);
                }
                else if (oType == typeof(Tree.ConstantValueString))
                {
                    paramTypes.Add(typeof(string));
                    paramValues.Add(((Tree.ConstantValueString)ot.Value).Value);
                }
                else
                {
                    areAllArgumentsConstant = false;
                }
            }

            if (areAllArgumentsConstant)
            {
                List<InlineApiMethodInfo> inlineMethodInfos;
                List<object> selectedFunctions = new List<object>();
                if (inlineMethods.TryGetValue(st.Entry, out inlineMethodInfos))
                {
                    foreach (InlineApiMethodInfo ami in inlineMethodInfos)
                    {
                        if (ami.ReturnType == typeof(void))
                        {
                            continue;
                        }
                        InlineApiMethodInfo.ParameterInfo[] pi = ami.Parameters;
                        if (pi.Length == paramTypes.Count && ami.IsPure)
                        {
                            selectedFunctions.Add(ami);
                        }
                    }
                }

                List<ApiMethodInfo> methodInfos;
                if (methods.TryGetValue(st.Entry, out methodInfos))
                {
                    foreach (ApiMethodInfo ami in methodInfos)
                    {
                        if (ami.Method.ReturnType == typeof(void))
                        {
                            continue;
                        }
                        ParameterInfo[] pi = ami.Method.GetParameters();
                        if (pi.Length == paramTypes.Count && Attribute.GetCustomAttribute(ami.Method, typeof(IsPureAttribute)) != null)
                        {
                            selectedFunctions.Add(ami);
                        }
                    }
                }


                object o = SelectConstantFunctionCall(cs, selectedFunctions, paramTypes, firstMustMatch);
                if (o != null)
                {
                    object resValue;
                    try
                    {
                        if (o is InlineApiMethodInfo)
                        {
                            resValue = CallConstantInlineFunction(cs, (InlineApiMethodInfo)o, paramTypes, paramValues);
                        }
                        else
                        {
                            resValue = CallConstantMethodFunction(cs, (ApiMethodInfo)o, paramTypes, paramValues);
                        }
                        AssignResult(st, resValue);
                    }
                    catch
                    {
                        /* ignore exceptions here */
                    }
                }
            }
        }

        private bool IsConstantFunctionIdenticalMatch(object o, List<Type> parameters)
        {
            Type t = o.GetType();
            if (t == typeof(ApiMethodInfo))
            {
                var methodInfo = (ApiMethodInfo)o;
                ParameterInfo[] pi = methodInfo.Method.GetParameters();
                for (int i = 0; i < parameters.Count; ++i)
                {
                    Type sourceType = parameters[i];
                    Type destType = pi[i].ParameterType;
                    if (sourceType != destType)
                    {
                        return false;
                    }
                }
            }
            else if (t == typeof(InlineApiMethodInfo))
            {
                var methodInfo = (InlineApiMethodInfo)o;
                InlineApiMethodInfo.ParameterInfo[] pi = methodInfo.Parameters;
                for (int i = 0; i < parameters.Count; ++i)
                {
                    Type sourceType = parameters[i];
                    Type destType = pi[i].ParameterType;
                    if (sourceType != destType)
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private bool TryConstantCastTo(CompileState compileState, ref object o, Type toType)
        {
            Type fromType = o.GetType();
            if (fromType == toType)
            {
                /* identical type is castable */
                return true;
            }
            else if(toType == typeof(string))
            {
                if(fromType == typeof(LSLKey))
                {
                    o = ((LSLKey)o).ToString();
                    return true;
                }
                else if(fromType == typeof(char))
                {
                    o = ((char)o).ToString();
                    return true;
                }
                else if(fromType == typeof(int))
                {
                    o = ((int)o).ToString();
                    return true;
                }
                else if(fromType == typeof(long))
                {
                    o = ((long)o).ToString();
                    return true;
                }
                else if(fromType == typeof(Vector3))
                {
                    o = compileState.UsesSinglePrecision ?
                                        SinglePrecision.TypecastVectorToString5Places((Vector3)o) :
                                        TypecastVectorToString5Places((Vector3)o);
                    return true;
                }
                else if (fromType == typeof(Quaternion))
                {
                    o = compileState.UsesSinglePrecision?
                                        SinglePrecision.TypecastRotationToString5Places((Quaternion)o) :
                                        TypecastRotationToString5Places((Quaternion)o);
                    return true;
                }
                else if (fromType == typeof(double))
                {
                    o = compileState.UsesSinglePrecision?
                                        SinglePrecision.TypecastFloatToString((double)o) :
                                        TypecastDoubleToString((double)o);
                    return true;
                }
            }
            else if(fromType == typeof(string) && toType == typeof(LSLKey))
            {
                o = new LSLKey((string)o);
                return true;
            }
            else if(fromType == typeof(LSLKey) && toType == typeof(string))
            {
                o = ((LSLKey)o).ToString();
                return true;
            }
            else if(fromType == typeof(int) && toType == typeof(double))
            {
                o = (double)(int)o;
                return true;
            }
            else if(fromType == typeof(long) && toType == typeof(double))
            {
                o = (double)(long)o;
                return true;
            }
            else if(fromType == typeof(int) && toType == typeof(long))
            {
                o = (long)(int)o;
                return true;
            }
            else if(fromType == typeof(char) && toType == typeof(string))
            {
                o = ((char)o).ToString();
                return true;
            }
            else if(toType == typeof(AnArray))
            {
                AnArray array;
                if(fromType == typeof(string))
                {
                    array = new AnArray { new AString((string)o) };
                }
                else if(fromType == typeof(char))
                {
                    array = new AnArray { new AString(((char)o).ToString()) };
                }
                else if(fromType == typeof(int))
                {
                    array = new AnArray { new Integer((int)o) };
                }
                else if (fromType == typeof(long))
                {
                    array = new AnArray { new LongInteger((long)o) };
                }
                else if (fromType == typeof(double))
                {
                    array = new AnArray { new Real((double)o) };
                }
                else if(fromType == typeof(Quaternion))
                {
                    array = new AnArray { (Quaternion)o };
                }
                else if(fromType == typeof(Vector3))
                {
                    array = new AnArray { (Vector3)o };
                }
                else if(fromType == typeof(LSLKey))
                {
                    array = new AnArray { new LSLKey((LSLKey)o) };
                }
                else
                {
                    return false;
                }
                return true;
            }

            Dictionary<Type, MethodInfo> toDict;
            MethodInfo mi;
            if(compileState.ApiInfo.Typecasts.TryGetValue(fromType, out toDict) &&
                toDict.TryGetValue(toType, out mi) &&
                mi.Name == "op_Implicit" &&
                Attribute.GetCustomAttribute(mi, typeof(IsPureAttribute)) != null)
            {
                o = mi.Invoke(null, new object[] { o });
                return true;
            }
            return false;
        }

        private bool IsConstantImplicitCastedMatch(CompileState compileState, object o, List<Type> parameters, bool firstMustMatch, out int matchedCount)
        {
            matchedCount = 0;
            Type t = o.GetType();
            if (t == typeof(ApiMethodInfo))
            {
                var methodInfo = (ApiMethodInfo)o;
                var attr = Attribute.GetCustomAttribute(methodInfo.Method, typeof(AllowExplicitTypecastsBeImplicitToStringAttribute)) as AllowExplicitTypecastsBeImplicitToStringAttribute;
                ParameterInfo[] pi = methodInfo.Method.GetParameters();
                for (int i = 0; i < parameters.Count; ++i)
                {
                    Type sourceType = parameters[i];
                    Type destType = pi[i].ParameterType;
                    if (sourceType != destType)
                    {
                        if(i == 0 && firstMustMatch)
                        {
                            return false;
                        }
                        else if (compileState.LanguageExtensions.EnableAllowImplicitCastToString && attr != null && attr.ParameterNumbers.Contains(i + 1) && IsExplicitlyCastableToString(compileState, sourceType))
                        {
                            /* is castable by attribute */
                        }
                        else if (!IsImplicitlyCastable(compileState, destType, sourceType))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        ++matchedCount;
                    }
                }
            }
            else if (t == typeof(InlineApiMethodInfo))
            {
                var methodInfo = (InlineApiMethodInfo)o;
                InlineApiMethodInfo.ParameterInfo[] pi = methodInfo.Parameters;
                for (int i = 0; i < parameters.Count; ++i)
                {
                    Type sourceType = parameters[i];
                    Type destType = pi[i].ParameterType;
                    if (sourceType != destType)
                    {
                        if (i == 0 && firstMustMatch)
                        {
                            return false;
                        }
                        else if (!IsImplicitlyCastable(compileState, destType, sourceType))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        ++matchedCount;
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private object SelectConstantFunctionCall(CompileState compileState, List<object> selectedFunctions, List<Type> parameterTypes, bool firstMustMatch)
        {
            /* search the identical match or closest match */
            object closeMatch = null;
            int closeMatchCountHighest = -1;
            foreach (object o in selectedFunctions)
            {
                if (IsConstantFunctionIdenticalMatch(o, parameterTypes))
                {
                    return o;
                }
                int closeMatchCount;
                if (IsConstantImplicitCastedMatch(compileState, o, parameterTypes, firstMustMatch, out closeMatchCount) && closeMatchCount > closeMatchCountHighest)
                {
                    closeMatch = o;
                    closeMatchCountHighest = closeMatchCount;
                }
            }

            return closeMatch;
        }
    }
}
