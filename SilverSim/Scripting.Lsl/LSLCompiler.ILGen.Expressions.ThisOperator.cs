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

#pragma warning disable IDE0018

using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        private sealed class ThisOperatorExpression : IExpressionStackElement
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

            public ThisOperatorExpression(
                Tree functionTree,
                int lineNumber)
            {
                m_LineNumber = lineNumber;

                m_FunctionName = functionTree.Entry;

                for (int i = 0; i < functionTree.SubTree.Count; ++i)
                {
                    m_Parameters.Add(new FunctionParameterInfo(functionTree.SubTree[i], i));
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
                if (m_Parameters.Count > m_ParameterPos)
                {
                    return m_Parameters[m_ParameterPos++].FunctionArgument;
                }
                else
                {
                    Type returnType = GenerateFunctionCall(compileState);
                    throw new ReturnTypeException(compileState, returnType, m_LineNumber);
                }
            }

            private bool IsImplicitCastedMatch(CompileState compileState, MethodInfo mi, out int matchedCount)
            {
                matchedCount = 0;
                ParameterInfo[] pi = mi.GetParameters();
                for (int i = 0; i < m_Parameters.Count; ++i)
                {
                    Type sourceType = m_Parameters[i].ParameterType;
                    Type destType = pi[i + 1].ParameterType;
                    if (sourceType != destType)
                    {
                        if (!IsImplicitlyCastable(compileState, destType, sourceType))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        ++matchedCount;
                    }
                }

                return true;
            }

            private Type GenerateFunctionCall(CompileState compileState)
            {
                List<Type> exactTypes = new List<Type>();
                for(int i = 1; i < m_Parameters.Count; ++i)
                {
                    exactTypes.Add(m_Parameters[i].ParameterType);
                }

                PropertyInfo pInfo = m_Parameters[0].ParameterType.GetProperty("Item", exactTypes.ToArray());
                MethodInfo mi;
                if(pInfo != null && (mi = pInfo.GetGetMethod()) != null)
                {
                    if(mi.IsVirtual)
                    {
                        compileState.ILGen.Emit(OpCodes.Callvirt, mi);
                    }
                    else
                    {
                        compileState.ILGen.Emit(OpCodes.Call, mi);
                    }
                    return mi.ReturnType;
                }

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

                mi = SelectFunctionCall(compileState);
                /* load actual parameters */
                ParameterInfo[] parameters = mi.GetParameters();
                for (int i = 0; i < lbs.Length; ++i)
                {
                    compileState.ILGen.Emit(OpCodes.Ldloc, lbs[i]);
                    ProcessImplicitCasts(compileState, parameters[i + 1].ParameterType, m_Parameters[i].ParameterType, m_LineNumber);
                }

                GenIncCallDepthCount(compileState);
                compileState.ILGen.Emit(OpCodes.Call, mi);
                GenDecCallDepthCount(compileState);

                compileState.ILGen.EndScope();
                return mi.ReturnType;
            }

            private List<MethodInfo> SelectProperties(Type varType)
            {
                var methods = new List<MethodInfo>();
                foreach(PropertyInfo prop in varType.GetProperties())
                {
                    MethodInfo mi = prop.GetGetMethod();
                    if(prop.IsSpecialName && prop.Name == "Item" && mi != null)
                    {
                        methods.Add(mi);
                    }
                }
                return methods;
            }

            private MethodInfo SelectFunctionCall(CompileState compileState)
            {
                /* search the identical match or closest match */
                MethodInfo closeMatch = null;
                int closeMatchCountHighest = -1;
                List<MethodInfo> thisOperators = SelectProperties(m_Parameters[0].ParameterType);
                foreach (MethodInfo o in thisOperators)
                {
                    int closeMatchCount;
                    if (IsImplicitCastedMatch(compileState, o, out closeMatchCount) && closeMatchCount > closeMatchCountHighest)
                    {
                        closeMatch = o;
                        closeMatchCountHighest = closeMatchCount;
                    }
                }

                if (closeMatch == null)
                {
                    if(thisOperators.Count == 0)
                    {
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ThisOperatorNotSupportedForType0", "This operator not supported for type {0}"), compileState.MapType(m_Parameters[0].ParameterType)));
                    }
                    else if (m_Parameters.Count == 1)
                    {
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ParameterMismatchAtThisOperator0Parameter", "Parameter mismatch at this operator {0}: no this variant takes {1} parameter"), m_FunctionName, m_Parameters.Count));
                    }
                    else
                    {
                        throw new CompilerException(m_LineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ParameterMismatchAtThisOperator0Parameters", "Parameter mismatch at this operator {0}: no this variant takes {1} parameters"), m_FunctionName, m_Parameters.Count));
                    }
                }

                return closeMatch;
            }
        }
    }
}
