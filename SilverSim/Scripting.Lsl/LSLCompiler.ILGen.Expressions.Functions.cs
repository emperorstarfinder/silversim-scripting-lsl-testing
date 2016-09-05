// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
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
        sealed class FunctionExpression : IExpressionStackElement
        {
            sealed class FunctionParameterInfo
            {
                public readonly string ParameterName = string.Empty;

                public readonly Tree FunctionArgument;
                public readonly int Position;

                public Type ParameterType; /* will be resolved later */
                public FunctionParameterInfo(Tree functionarg, int position)
                {
                    FunctionArgument = functionarg;
                    Position = position;
                }
            }
            readonly List<FunctionParameterInfo> m_Parameters = new List<FunctionParameterInfo>();
            int m_ParameterPos;
            readonly List<object> m_SelectedFunctions = new List<object>();

            readonly string m_FunctionName;
            readonly int m_LineNumber;

            void GenIncCallDepthCount(CompileState compileState)
            {
                /* load script instance reference */
                if (null == compileState.StateTypeBuilder)
                {
                    compileState.ILGen.Emit(OpCodes.Ldarg_0);
                }
                else
                {
                    compileState.ILGen.Emit(OpCodes.Ldarg_0);
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                }
                compileState.ILGen.Emit(OpCodes.Call, typeof(Script).GetMethod("IncCallDepthCount", Type.EmptyTypes));
            }

            void GenDecCallDepthCount(CompileState compileState)
            {
                /* load script instance reference */
                if (null == compileState.StateTypeBuilder)
                {
                    compileState.ILGen.Emit(OpCodes.Ldarg_0);
                }
                else
                {
                    compileState.ILGen.Emit(OpCodes.Ldarg_0);
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                }
                compileState.ILGen.Emit(OpCodes.Call, typeof(Script).GetMethod("DecCallDepthCount", Type.EmptyTypes));
            }

            public FunctionExpression(
                LSLCompiler lslCompiler,
                CompileState compileState,
                Tree functionTree,
                int lineNumber,
                Dictionary<string, object> localVars)
            {
                List<FunctionInfo> funcInfos;
                List<ApiMethodInfo> methods;
                bool functionNameValid = false;

                m_LineNumber = lineNumber;

                m_FunctionName = functionTree.Entry;

                for (int i = 0; i < functionTree.SubTree.Count; ++i)
                {
                    m_Parameters.Add(new FunctionParameterInfo(functionTree.SubTree[i], i));
                }

                if (compileState.m_Functions.TryGetValue(functionTree.Entry, out funcInfos))
                {
                    functionNameValid = true;
                    /*
                    */

                    foreach (FunctionInfo funcInfo in funcInfos)
                    {
                        if (funcInfo.Parameters.Length == m_Parameters.Count)
                        {
                            m_SelectedFunctions.Add(funcInfo);
                        }
                    }
                    if (m_SelectedFunctions.Count == 0)
                    {
                        if (functionTree.SubTree.Count == 1)
                        {
                            throw new CompilerException(lineNumber, string.Format("Parameter mismatch at function {0}: no function variant takes {1} parameter", functionTree.Entry, functionTree.SubTree.Count));
                        }
                        else
                        {
                            throw new CompilerException(lineNumber, string.Format("Parameter mismatch at function {0}: no function variant takes {1} parameters", functionTree.Entry, functionTree.SubTree.Count));
                        }
                    }
                }

                if (compileState.ApiInfo.Methods.TryGetValue(functionTree.Entry, out methods))
                {
                    functionNameValid = true;
                    foreach (ApiMethodInfo method in methods)
                    {
                        ParameterInfo[] pi = method.Method.GetParameters();
                        if (pi.Length - 1 == functionTree.SubTree.Count)
                        {
                            bool methodValid = true;

                            if (!IsValidType(method.Method.ReturnType))
                            {
                                methodValid = false;
                                m_Log.ErrorFormat("Internal Error! Return Value (type {1}) of function {0} is not LSL compatible", method.Method.Name, method.Method.ReturnType.Name);
                            }

                            for (int i = 0; i < functionTree.SubTree.Count; ++i)
                            {
                                if (!IsValidType(pi[i + 1].ParameterType))
                                {
                                    m_Log.ErrorFormat("Internal Error! Parameter {0} (type {1}) of function {2} is not LSL compatible",
                                        pi[i + 1].Name, pi[i + 1].ParameterType.FullName, functionTree.Entry);
                                    methodValid = false;
                                }
                            }

                            if (methodValid)
                            {
                                m_SelectedFunctions.Add(method);
                            }
                        }
                    }
                }

                if (!functionNameValid)
                {
                    throw new CompilerException(lineNumber, string.Format("No function {0} defined", functionTree.Entry));
                }
                else if (m_SelectedFunctions.Count == 0)
                {
                    if (functionTree.SubTree.Count == 1)
                    {
                        throw new CompilerException(lineNumber, string.Format("Parameter mismatch at function {0}: no function variant takes {1} parameter", functionTree.Entry, functionTree.SubTree.Count));
                    }
                    else
                    {
                        throw new CompilerException(lineNumber, string.Format("Parameter mismatch at function {0}: no function variant takes {1} parameters", functionTree.Entry, functionTree.SubTree.Count));
                    }
                }
            }

            public Tree ProcessNextStep(
                LSLCompiler lslCompiler,
                CompileState compileState,
                Dictionary<string, object> localVars,
                Type innerExpressionReturn)
            {
                if (null != innerExpressionReturn)
                {
                    m_Parameters[m_ParameterPos - 1].ParameterType = innerExpressionReturn;
                }
                if(m_Parameters.Count > m_ParameterPos)
                {
                    return m_Parameters[m_ParameterPos++].FunctionArgument;
                }
                else
                {
                    Type returnType = GenerateFunctionCall(compileState);
                    throw new ReturnTypeException(returnType, m_LineNumber);
                }
            }

            bool IsFunctionIdenticalMatch(object o)
            {
                Type t = o.GetType();
                if(t == typeof(ApiMethodInfo))
                {
                    ApiMethodInfo methodInfo = (ApiMethodInfo)o;
                    ParameterInfo[] pi = methodInfo.Method.GetParameters();
                    for (int i = 0; i < m_Parameters.Count; ++i)
                    {
                        Type sourceType = m_Parameters[i].ParameterType;
                        Type destType = pi[i + 1].ParameterType;
                        if(sourceType != destType)
                        {
                            return false;
                        }
                    }
                }
                else if(t == typeof(FunctionInfo))
                {
                    FunctionInfo methodInfo = (FunctionInfo)o;
                    KeyValuePair<string, Type>[] pi = methodInfo.Parameters;
                    for (int i = 0; i < m_Parameters.Count; ++i)
                    {
                        Type sourceType = m_Parameters[i].ParameterType;
                        Type destType = pi[i].Value;
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

            bool IsImplicitCastedMatch(object o, out int matchedCount)
            {
                matchedCount = 0;
                Type t = o.GetType();
                if (t == typeof(ApiMethodInfo))
                {
                    ApiMethodInfo methodInfo = (ApiMethodInfo)o;
                    ParameterInfo[] pi = methodInfo.Method.GetParameters();
                    for (int i = 0; i < m_Parameters.Count; ++i)
                    {
                        Type sourceType = m_Parameters[i].ParameterType;
                        Type destType = pi[i + 1].ParameterType;
                        if (sourceType != destType)
                        {
                            if(!IsImplicitlyCastable(destType, sourceType))
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
                else if (t == typeof(FunctionInfo))
                {
                    FunctionInfo methodInfo = (FunctionInfo)o;
                    KeyValuePair<string, Type>[] pi = methodInfo.Parameters;
                    for (int i = 0; i < m_Parameters.Count; ++i)
                    {
                        Type sourceType = m_Parameters[i].ParameterType;
                        Type destType = pi[i].Value;
                        if (sourceType != destType)
                        {
                            if (!IsImplicitlyCastable(destType, sourceType))
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

            Type GenerateFunctionCall(CompileState compileState)
            {
                compileState.ILGen.BeginScope();
                LocalBuilder[] lbs = new LocalBuilder[m_Parameters.Count];
                for (int i = 0; i < lbs.Length; ++i)
                {
                    lbs[i] = compileState.ILGen.DeclareLocal(m_Parameters[i].ParameterType);
                }

                /* store all parameters to locals */
                for (int i = lbs.Length; i-- > 0;)
                {
                    compileState.ILGen.Emit(OpCodes.Stloc, lbs[i]);
                }

                object o = SelectFunctionCall(compileState, lbs);
                Type ot = o.GetType();
                if (ot == typeof(FunctionInfo))
                {
                    FunctionInfo funcInfo = o as FunctionInfo;
                    /* load script instance reference */
                    if (null == compileState.StateTypeBuilder)
                    {
                        compileState.ILGen.Emit(OpCodes.Ldarg_0);
                    }
                    else
                    {
                        compileState.ILGen.Emit(OpCodes.Ldarg_0);
                        compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                    }

                    /* load actual parameters */
                    KeyValuePair<string, Type>[] parameters = funcInfo.Parameters;
                    for (int i = 0; i < lbs.Length; ++i)
                    {
                        compileState.ILGen.Emit(OpCodes.Ldloc, lbs[i]);
                        ProcessImplicitCasts(compileState, parameters[i].Value, m_Parameters[i].ParameterType, m_LineNumber);
                    }

                    GenIncCallDepthCount(compileState);
                    compileState.ILGen.Emit(OpCodes.Call, funcInfo.Method);
                    GenDecCallDepthCount(compileState);

                    compileState.ILGen.EndScope();
                    return funcInfo.Method.ReturnType;
                }
                else if (ot == typeof(ApiMethodInfo))
                {
                    ApiMethodInfo apiMethod = (ApiMethodInfo)o;
                    MethodInfo methodInfo = apiMethod.Method;
                    ScriptApiNameAttribute apiAttr = (ScriptApiNameAttribute)Attribute.GetCustomAttribute(apiMethod.Api.GetType(), typeof(ScriptApiNameAttribute));

                    /* load ScriptApi reference */
                    compileState.ILGen.Emit(OpCodes.Ldsfld, compileState.m_ApiFieldInfo[apiAttr.Name]);

                    /* load ScriptInstance reference */
                    compileState.ILGen.Emit(OpCodes.Ldarg_0);
                    if (compileState.StateTypeBuilder != null)
                    {
                        compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                    }

                    /* load actual parameters */
                    ParameterInfo[] parameters = methodInfo.GetParameters();
                    for (int i = 0; i < lbs.Length; ++i)
                    {
                        compileState.ILGen.Emit(OpCodes.Ldloc, lbs[i]);
                        ProcessImplicitCasts(compileState, parameters[i + 1].ParameterType, m_Parameters[i].ParameterType, m_LineNumber);
                    }

                    GenIncCallDepthCount(compileState);
                    compileState.ILGen.Emit(OpCodes.Call, apiMethod.Method);
                    GenDecCallDepthCount(compileState);

                    ForcedSleepAttribute forcedSleep = (ForcedSleepAttribute)Attribute.GetCustomAttribute(methodInfo, typeof(ForcedSleepAttribute));
                    if (forcedSleep != null)
                    {
                        compileState.ILGen.Emit(OpCodes.Ldarg_0);
                        if (compileState.StateTypeBuilder != null)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                        }
                        compileState.ILGen.Emit(OpCodes.Ldc_I4, (int)(forcedSleep.Seconds * 1000));
                        compileState.ILGen.Emit(OpCodes.Call, typeof(Script).GetMethod("ForcedSleep", new Type[] { typeof(int) }));
                    }

                    compileState.ILGen.EndScope();
                    return methodInfo.ReturnType;
                }
                else if (m_Parameters.Count == 1)
                {
                    throw new CompilerException(m_LineNumber, string.Format("Parameter mismatch at function {0}: no function variant takes {1} parameter", m_FunctionName, m_Parameters.Count));
                }
                else
                {
                    throw new CompilerException(m_LineNumber, string.Format("Parameter mismatch at function {0}: no function variant takes {1} parameters", m_FunctionName, m_Parameters.Count));
                }
            }


            object SelectFunctionCall(CompileState compileState, LocalBuilder[] lbs)
            {
                /* search the identical match or closest match */
                object closeMatch = null;
                int closeMatchCountHighest = -1;
                foreach(object o in m_SelectedFunctions)
                {
                    if(IsFunctionIdenticalMatch(o))
                    {
                        return o;
                    }
                    int closeMatchCount;
                    if (IsImplicitCastedMatch(o, out closeMatchCount) && closeMatchCount > closeMatchCountHighest)
                    {
                        closeMatch = o;
                        closeMatchCountHighest = closeMatchCount;
                    }
                }

                if(null == closeMatch)
                {
                    if (m_Parameters.Count == 1)
                    {
                        throw new CompilerException(m_LineNumber, string.Format("Parameter mismatch at function {0}: no function variant takes {1} parameter", m_FunctionName, m_Parameters.Count));
                    }
                    else
                    {
                        throw new CompilerException(m_LineNumber, string.Format("Parameter mismatch at function {0}: no function variant takes {1} parameters", m_FunctionName, m_Parameters.Count));
                    }

                }

                return closeMatch;
            }
        }
    }
}
