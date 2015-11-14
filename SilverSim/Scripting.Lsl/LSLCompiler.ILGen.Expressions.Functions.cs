// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Scripting.Lsl.Expression;
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
                public readonly string ParameterName;
                public readonly Type ParameterType;
                public readonly Tree FunctionArgument;
                public readonly int Position;
                public FunctionParameterInfo(string name, Type t, Tree functionarg, int position)
                {
                    ParameterName = name;
                    ParameterType = t;
                    FunctionArgument = functionarg;
                    Position = position;
                }
            }
            readonly List<FunctionParameterInfo> m_Parameters = new List<FunctionParameterInfo>();

            readonly string m_FunctionName;
            readonly Type m_FunctionReturnType;
            readonly int m_LineNumber;
            readonly MethodInfo m_MethodInfo;

            public FunctionExpression(
                LSLCompiler lslCompiler,
                CompileState compileState,
                Tree functionTree,
                int lineNumber,
                Dictionary<string, object> localVars)
            {
                MethodBuilder mb;
                m_LineNumber = lineNumber;
                List<ApiMethodInfo> methods;
                if (compileState.m_FunctionInfo.TryGetValue(functionTree.Entry, out mb))
                {
                    KeyValuePair<Type, KeyValuePair<string, Type>[]> signatureInfo = compileState.m_FunctionSignature[functionTree.Entry];
                    KeyValuePair<string, Type>[] pi = signatureInfo.Value;

                    if (null == compileState.StateTypeBuilder)
                    {
                        compileState.ILGen.Emit(OpCodes.Ldarg_0);
                    }
                    else
                    {
                        compileState.ILGen.Emit(OpCodes.Ldarg_0);
                        compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                    }

                    m_FunctionName = functionTree.Entry;

                    for (int i = 0; i < functionTree.SubTree.Count; ++i)
                    {
                        m_Parameters.Add(new FunctionParameterInfo(pi[i].Key, pi[i].Value, functionTree.SubTree[i], i));
                    }
                    m_MethodInfo = mb;
                    m_FunctionReturnType = signatureInfo.Key;
                }
                else if (compileState.ApiInfo.Methods.TryGetValue(functionTree.Entry, out methods))
                {
                    foreach (ApiMethodInfo method in methods)
                    {
                        ParameterInfo[] pi = method.Method.GetParameters();
                        if (pi.Length - 1 == functionTree.SubTree.Count)
                        {
                            ScriptApiName apiAttr = (ScriptApiName)System.Attribute.GetCustomAttribute(method.Api.GetType(), typeof(ScriptApiName));

                            if (!IsValidType(method.Method.ReturnType))
                            {
                                throw new CompilerException(lineNumber, string.Format("Internal Error! Return Value (type {1}) of function {0} is not LSL compatible", method.Method.Name, method.Method.ReturnType.Name));
                            }

                            compileState.ILGen.Emit(OpCodes.Ldsfld, compileState.m_ApiFieldInfo[apiAttr.Name]);

                            if (null == compileState.StateTypeBuilder)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldarg_0);
                            }
                            else
                            {
                                compileState.ILGen.Emit(OpCodes.Ldarg_0);
                                compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                            }

                            for (int i = 0; i < functionTree.SubTree.Count; ++i)
                            {
                                if (!IsValidType(pi[i + 1].ParameterType))
                                {
                                    throw new CompilerException(lineNumber, string.Format("Internal Error! Parameter {0} (type {1}) of function {2} is not LSL compatible",
                                        pi[i + 1].Name, pi[i + 1].ParameterType.FullName, functionTree.Entry));
                                }

                                m_Parameters.Add(new FunctionParameterInfo(pi[i + 1].Name, pi[i + 1].ParameterType, functionTree.SubTree[i], i));
                            }

                            m_MethodInfo = method.Method;
                            m_FunctionReturnType = method.Method.ReturnType;
                            return;
                        }
                    }
                    throw new CompilerException(lineNumber, string.Format("Parameter mismatch at function {0}", functionTree.Entry));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("No function {0} defined", functionTree.Entry));
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
                    try
                    {
                        ProcessImplicitCasts(compileState.ILGen, m_Parameters[0].ParameterType, innerExpressionReturn, m_LineNumber);
                    }
                    catch
                    {
                        throw new CompilerException(m_LineNumber,
                            string.Format("No implicit cast from {0} to {1} possible for parameter '{2}' of function '{3}'",
                                MapType(innerExpressionReturn),
                                MapType(m_Parameters[0].ParameterType),
                                m_Parameters[0].ParameterName,
                                m_FunctionName));
                    }

                    m_Parameters.RemoveAt(0);
                }

                if(m_Parameters.Count == 0)
                {
                    compileState.ILGen.Emit(OpCodes.Call, m_MethodInfo);
                    throw new ReturnTypeException(m_FunctionReturnType, m_LineNumber);
                }
                else
                {
                    return m_Parameters[0].FunctionArgument;
                }
            }
        }
    }
}
