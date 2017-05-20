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
        private sealed class FunctionExpression : IExpressionStackElement
        {
            private sealed class FunctionParameterInfo
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

            private readonly List<FunctionParameterInfo> m_Parameters = new List<FunctionParameterInfo>();
            private int m_ParameterPos;
            private readonly List<object> m_SelectedFunctions = new List<object>();

            private readonly string m_FunctionName;
            private readonly int m_LineNumber;

            private void GenIncCallDepthCount(CompileState compileState)
            {
                /* load script instance reference */
                if (compileState.StateTypeBuilder == null)
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

            private void GenDecCallDepthCount(CompileState compileState)
            {
                /* load script instance reference */
                if (compileState.StateTypeBuilder == null)
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
                CompileState compileState,
                Tree functionTree,
                int lineNumber)
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
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ParameterMismatchAtFunction0Parameter", "Parameter mismatch at function {0}: no function variant takes {1} parameter"), functionTree.Entry, functionTree.SubTree.Count));
                        }
                        else
                        {
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ParameterMismatchAtFunction0Parameters", "Parameter mismatch at function {0}: no function variant takes {1} parameters"), functionTree.Entry, functionTree.SubTree.Count));
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
                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "NoFunction0Defined", "No function {0} defined"), functionTree.Entry));
                }
                else if (m_SelectedFunctions.Count == 0)
                {
                    if (functionTree.SubTree.Count == 1)
                    {
                        throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ParameterMismatchAtFunction0Parameter", "Parameter mismatch at function {0}: no function variant takes {1} parameter"), functionTree.Entry, functionTree.SubTree.Count));
                    }
                    else
                    {
                        throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ParameterMismatchAtFunction0Parameters", "Parameter mismatch at function {0}: no function variant takes {1} parameters"), functionTree.Entry, functionTree.SubTree.Count));
                    }
                }
            }

            public Tree ProcessNextStep(
                LSLCompiler lslCompiler,
                CompileState compileState,
                Dictionary<string, object> localVars,
                Type innerExpressionReturn)
            {
                if (innerExpressionReturn != null)
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

            private bool IsFunctionIdenticalMatch(object o)
            {
                Type t = o.GetType();
                if(t == typeof(ApiMethodInfo))
                {
                    var methodInfo = (ApiMethodInfo)o;
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
                    var methodInfo = (FunctionInfo)o;
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

            private bool IsImplicitCastedMatch(object o, out int matchedCount)
            {
                matchedCount = 0;
                Type t = o.GetType();
                if (t == typeof(ApiMethodInfo))
                {
                    var methodInfo = (ApiMethodInfo)o;
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
                    var methodInfo = (FunctionInfo)o;
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

            private Type GenerateFunctionCall(CompileState compileState)
            {
                compileState.ILGen.BeginScope();
                var lbs = new LocalBuilder[m_Parameters.Count];
                for (int i = 0; i < lbs.Length; ++i)
                {
                    lbs[i] = compileState.ILGen.DeclareLocal(m_Parameters[i].ParameterType);
                }

                /* store all parameters to locals */
                for (int i = lbs.Length; i-- > 0;)
                {
                    compileState.ILGen.Emit(OpCodes.Stloc, lbs[i]);
                }

                object o = SelectFunctionCall(compileState);
                Type ot = o.GetType();
                if (ot == typeof(FunctionInfo))
                {
                    var funcInfo = o as FunctionInfo;
                    /* load script instance reference */
                    if (compileState.StateTypeBuilder == null)
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
                    var apiMethod = (ApiMethodInfo)o;
                    MethodInfo methodInfo = apiMethod.Method;
                    var apiAttr = (ScriptApiNameAttribute)Attribute.GetCustomAttribute(apiMethod.Api.GetType(), typeof(ScriptApiNameAttribute));
                    var threatLevelAttr = (ThreatLevelRequiredAttribute)Attribute.GetCustomAttribute(methodInfo, typeof(ThreatLevelRequiredAttribute));

                    if (threatLevelAttr != null)
                    {
                        /* load ScriptInstance reference */
                        compileState.ILGen.Emit(OpCodes.Ldarg_0);
                        if (compileState.StateTypeBuilder != null)
                        {
                            compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                        }
                        compileState.ILGen.Emit(OpCodes.Castclass, typeof(Script));
                        if (string.IsNullOrEmpty(threatLevelAttr.FunctionName))
                        {
                            compileState.ILGen.Emit(OpCodes.Ldstr, m_FunctionName);
                        }
                        else
                        {
                            compileState.ILGen.Emit(OpCodes.Ldstr, threatLevelAttr.FunctionName);
                        }
                        compileState.ILGen.Emit(OpCodes.Ldc_I4, (int)threatLevelAttr.ThreatLevel);
                        MethodInfo checkMethod = typeof(Script).GetMethod("CheckThreatLevel", new Type[] { typeof(string), typeof(ThreatLevel) });
                        compileState.ILGen.Emit(OpCodes.Call, checkMethod);
                    }

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

                    var forcedSleep = (ForcedSleepAttribute)Attribute.GetCustomAttribute(methodInfo, typeof(ForcedSleepAttribute));
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
                    throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ParameterMismatchAtFunction0Parameter", "Parameter mismatch at function {0}: no function variant takes {1} parameter"), m_FunctionName, m_Parameters.Count));
                }
                else
                {
                    throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ParameterMismatchAtFunction0Parameters", "Parameter mismatch at function {0}: no function variant takes {1} parameters"), m_FunctionName, m_Parameters.Count));
                }
            }

            private object SelectFunctionCall(CompileState compileState)
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

                if(closeMatch == null)
                {
                    if (m_Parameters.Count == 1)
                    {
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ParameterMismatchAtFunction0Parameter", "Parameter mismatch at function {0}: no function variant takes {1} parameter"), m_FunctionName, m_Parameters.Count));
                    }
                    else
                    {
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ParameterMismatchAtFunction0Parameters", "Parameter mismatch at function {0}: no function variant takes {1} parameters"), m_FunctionName, m_Parameters.Count));
                    }
                }

                return closeMatch;
            }
        }
    }
}
