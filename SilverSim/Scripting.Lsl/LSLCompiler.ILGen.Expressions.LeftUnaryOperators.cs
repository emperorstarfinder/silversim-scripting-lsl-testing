// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        sealed class LeftUnaryOperators : IExpressionStackElement
        {
            readonly Tree m_ExpressionTree;
            readonly string m_Operator;
            readonly int m_LineNumber;

            public LeftUnaryOperators(
                LSLCompiler lslCompiler,
                CompileState compileState,
                Tree functionTree,
                int lineNumber,
                Dictionary<string, object> localVars)
            {
                m_Operator = functionTree.Entry;
                m_ExpressionTree = functionTree.SubTree[0];
                m_LineNumber = lineNumber;
            }

            public Tree ProcessNextStep(
                LSLCompiler lslCompiler,
                CompileState compileState,
                Dictionary<string, object> localVars,
                Type innerExpressionReturn)
            {
                if (null != innerExpressionReturn)
                {
                    switch (m_Operator)
                    {
                        case "!":
                            if (innerExpressionReturn == typeof(int))
                            {
                                compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                                compileState.ILGen.Emit(OpCodes.Ceq);
                            }
                            else if(innerExpressionReturn == typeof(LSLKey) ||
                                innerExpressionReturn == typeof(Quaternion))
                            {
                                compileState.ILGen.Emit(OpCodes.Call, innerExpressionReturn.GetProperty("IsLSLTrue").GetGetMethod());
                                compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                                compileState.ILGen.Emit(OpCodes.Ceq);
                            }
                            else if (innerExpressionReturn == typeof(string) ||
                                innerExpressionReturn == typeof(AnArray))
                            {
                                compileState.ILGen.Emit(OpCodes.Call, innerExpressionReturn.GetProperty("Length").GetGetMethod());
                                compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                                compileState.ILGen.Emit(OpCodes.Ceq);
                            }
                            else if (innerExpressionReturn == typeof(Vector3))
                            {
                                compileState.ILGen.Emit(OpCodes.Call, innerExpressionReturn.GetProperty("Length").GetGetMethod());
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, 0f);
                                compileState.ILGen.Emit(OpCodes.Ceq);
                            }
                            else
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '!' not supported for {0}", MapType(innerExpressionReturn)));
                            }
                            throw new ReturnTypeException(typeof(int), m_LineNumber);

                        case "-":
                            if (innerExpressionReturn == typeof(int) || innerExpressionReturn == typeof(double))
                            {
                                compileState.ILGen.Emit(OpCodes.Neg);
                            }
                            else if (innerExpressionReturn == typeof(Vector3))
                            {
                                compileState.ILGen.Emit(OpCodes.Call, typeof(Vector3).GetMethod("op_UnaryNegation"));
                            }
                            else if (innerExpressionReturn == typeof(Quaternion))
                            {
                                compileState.ILGen.Emit(OpCodes.Call, typeof(Quaternion).GetMethod("op_UnaryNegation"));
                            }
                            else
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '-' not supported for {0}", MapType(innerExpressionReturn)));
                            }
                            throw new ReturnTypeException(innerExpressionReturn, m_LineNumber);

                        case "~":
                            if (innerExpressionReturn == typeof(int))
                            {
                                compileState.ILGen.Emit(OpCodes.Neg);
                            }
                            else
                            {
                                throw new CompilerException(m_LineNumber, string.Format("operator '~' not supported for {0}", MapType(innerExpressionReturn)));
                            }
                            throw new ReturnTypeException(innerExpressionReturn, m_LineNumber);

                        default:
                            throw new CompilerException(m_LineNumber, string.Format("left unary operator '{0}' not supported", m_Operator));
                    }
                }
                else
                {
                    return m_ExpressionTree;
                }
            }
        }
    }
}
