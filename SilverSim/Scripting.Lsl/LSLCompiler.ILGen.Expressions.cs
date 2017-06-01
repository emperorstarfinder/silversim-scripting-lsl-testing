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

#pragma warning disable RCS1029, IDE0020

using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        [Serializable]
        public class ReturnTypeException : Exception
        {
            public Type ReturnType;
            internal ReturnTypeException(CompileState compileState, Type t, int lineNumber)
            {
                ReturnType = t;
                if(t == null)
                {
                    throw new CompilerException(lineNumber, "Internal Error! returnType is not set");
                }
                else if (!compileState.IsValidType(t))
                {
                    throw new CompilerException(lineNumber, string.Format("Internal Error! '{0}' is not a LSL compatible type", t.FullName));
                }
            }
        }

        private interface IExpressionStackElement
        {
            Tree ProcessNextStep(
                LSLCompiler lslCompiler,
                CompileState compileState,
                Dictionary<string, object> localVars,
                Type innerExpressionReturn);
        }

        private Type ProcessExpressionPart(
            CompileState compileState,
            Tree functionTree,
            int lineNumber,
            Dictionary<string, object> localVars)
        {
            var expressionStack = new List<IExpressionStackElement>();
            Type innerExpressionReturn = null;
            bool first = true;

            for (; ;)
            {
                if (expressionStack.Count != 0)
                {
                    try
                    {
                        functionTree = expressionStack[0].ProcessNextStep(
                            this,
                            compileState,
                            localVars,
                            innerExpressionReturn);
                    }
                    catch (ReturnTypeException e)
                    {
                        innerExpressionReturn = e.ReturnType;
                        expressionStack.RemoveAt(0);
                        if (expressionStack.Count == 0)
                        {
                            if (!compileState.IsValidType(innerExpressionReturn))
                            {
                                throw new CompilerException(lineNumber, "Internal Error! Return type is not set to LSL compatible type. (" + innerExpressionReturn.FullName + ")");
                            }
                            return innerExpressionReturn;
                        }
                        continue;
                    }
                }
                else if(!first)
                {
                    if(!compileState.IsValidType(innerExpressionReturn))
                    {
                        throw new CompilerException(lineNumber, "Internal Error! Return type is not set to LSL compatible type. (" + innerExpressionReturn.FullName + ")");
                    }
                    return innerExpressionReturn;
                }
                first = false;

                /* dive into */
                while (functionTree.Type == Tree.EntryType.FunctionArgument ||
                    functionTree.Type == Tree.EntryType.ExpressionTree ||
                    (functionTree.Type == Tree.EntryType.Level && functionTree.Entry != "["))
                {
                    functionTree = functionTree.SubTree[0];
                }

                if (functionTree.Value != null)
                {
                    if (functionTree.Value is Tree.ConstantValueFloat)
                    {
                        compileState.ILGen.Emit(OpCodes.Ldc_R8, ((Tree.ConstantValueFloat)functionTree.Value).Value);
                        innerExpressionReturn = typeof(double);
                    }
                    else if (functionTree.Value is Tree.ConstantValueInt)
                    {
                        compileState.ILGen.Emit(OpCodes.Ldc_I4, ((Tree.ConstantValueInt)functionTree.Value).Value);
                        innerExpressionReturn = typeof(int);
                    }
                    else if (functionTree.Value is Tree.ConstantValueLong)
                    {
                        compileState.ILGen.Emit(OpCodes.Ldc_I8, ((Tree.ConstantValueLong)functionTree.Value).Value);
                        innerExpressionReturn = typeof(long);
                    }
                    else if (functionTree.Value is Tree.ConstantValueString)
                    {
                        compileState.ILGen.Emit(OpCodes.Ldstr, ((Tree.ConstantValueString)functionTree.Value).Value);
                        innerExpressionReturn = typeof(string);
                    }
                    else if (functionTree.Value is ConstantValueRotation)
                    {
                        var val = (ConstantValueRotation)functionTree.Value;
                        compileState.ILGen.Emit(OpCodes.Ldc_R8, val.Value.X);
                        compileState.ILGen.Emit(OpCodes.Ldc_R8, val.Value.Y);
                        compileState.ILGen.Emit(OpCodes.Ldc_R8, val.Value.Z);
                        compileState.ILGen.Emit(OpCodes.Ldc_R8, val.Value.W);
                        compileState.ILGen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) }));
                        innerExpressionReturn = typeof(Quaternion);
                    }
                    else if (functionTree.Value is ConstantValueVector)
                    {
                        var val = (ConstantValueVector)functionTree.Value;
                        compileState.ILGen.Emit(OpCodes.Ldc_R8, val.Value.X);
                        compileState.ILGen.Emit(OpCodes.Ldc_R8, val.Value.Y);
                        compileState.ILGen.Emit(OpCodes.Ldc_R8, val.Value.Z);
                        compileState.ILGen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double) }));
                        innerExpressionReturn = typeof(Vector3);
                    }
                    else
                    {
                        throw new CompilerException(lineNumber, "Internal Error");
                    }
                }
                else
                {
                    switch (functionTree.Type)
                    {
                        case Tree.EntryType.Function:
                            expressionStack.Insert(0, new FunctionExpression(
                                compileState,
                                functionTree,
                                lineNumber));
                            innerExpressionReturn = null;
                            break;

                        case Tree.EntryType.OperatorBinary:
                            expressionStack.Insert(0, new BinaryOperatorExpression(
                                this,
                                compileState,
                                functionTree,
                                lineNumber,
                                localVars));
                            innerExpressionReturn = null;
                            break;

                        case Tree.EntryType.ThisOperator:
                            expressionStack.Insert(0, new ThisOperatorExpression(
                                functionTree,
                                lineNumber));
                            innerExpressionReturn = null;
                            break;

                        case Tree.EntryType.OperatorLeftUnary:
                            switch (functionTree.Entry)
                            {
                                case "++":
                                    if (functionTree.SubTree[0].Type == Tree.EntryType.Variable || functionTree.SubTree[0].Type == Tree.EntryType.Unknown)
                                    {
                                        object v = localVars[functionTree.SubTree[0].Entry];
                                        innerExpressionReturn = GetVarToStack(compileState, v);
                                        if (innerExpressionReturn == typeof(int))
                                        {
                                            compileState.ILGen.Emit(OpCodes.Ldc_I4_1);
                                            compileState.ILGen.Emit(OpCodes.Add);
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else if (innerExpressionReturn == typeof(long))
                                        {
                                            compileState.ILGen.Emit(OpCodes.Ldc_I8, 1L);
                                            compileState.ILGen.Emit(OpCodes.Add);
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else if (innerExpressionReturn == typeof(double))
                                        {
                                            compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)1);
                                            compileState.ILGen.Emit(OpCodes.Add);
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else
                                        {
                                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorPlusPlusNotSupportedFor0", "operator '++' not supported for {0}"), compileState.MapType(innerExpressionReturn)));
                                        }
                                    }
                                    else if (functionTree.SubTree[0].Type == Tree.EntryType.OperatorBinary && functionTree.SubTree[0].Entry == ".")
                                    {
                                        compileState.ILGen.BeginScope();
                                        object v = localVars[functionTree.SubTree[0].SubTree[0].Entry];
                                        innerExpressionReturn = GetVarToStack(compileState, v);
                                        if (innerExpressionReturn == typeof(Vector3))
                                        {
                                            LocalBuilder structLb = compileState.ILGen.DeclareLocal(innerExpressionReturn);
                                            compileState.ILGen.Emit(OpCodes.Stloc, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            FieldInfo fi;
                                            switch (functionTree.SubTree[0].SubTree[1].Entry)
                                            {
                                                case "x":
                                                    fi = typeof(Vector3).GetField("X");
                                                    break;

                                                case "y":
                                                    fi = typeof(Vector3).GetField("Y");
                                                    break;

                                                case "z":
                                                    fi = typeof(Vector3).GetField("Z");
                                                    break;

                                                default:
                                                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ComponentAccessFor0AtVectorIsNotDefined", "Component access for '{0}' at vector is not defined"), functionTree.SubTree[0].SubTree[1].Entry));
                                            }
                                            compileState.ILGen.Emit(OpCodes.Ldfld, fi);
                                            innerExpressionReturn = typeof(double);
                                            LocalBuilder copyLb = compileState.ILGen.DeclareLocal(typeof(double));
                                            compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)1);
                                            compileState.ILGen.Emit(OpCodes.Add);
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            compileState.ILGen.Emit(OpCodes.Stloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Stfld, fi);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, structLb);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else if (innerExpressionReturn == typeof(Quaternion))
                                        {
                                            LocalBuilder structLb = compileState.ILGen.DeclareLocal(innerExpressionReturn);
                                            compileState.ILGen.Emit(OpCodes.Stloc, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            FieldInfo fi;
                                            switch (functionTree.SubTree[0].SubTree[1].Entry)
                                            {
                                                case "x":
                                                    fi = typeof(Quaternion).GetField("X");
                                                    break;

                                                case "y":
                                                    fi = typeof(Quaternion).GetField("Y");
                                                    break;

                                                case "z":
                                                    fi = typeof(Quaternion).GetField("Z");
                                                    break;

                                                case "s":
                                                    fi = typeof(Quaternion).GetField("S");
                                                    break;

                                                default:
                                                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ComponentAccessFor0AtRotationIsNotDefined", "Component access for '{0}' at rotation is not defined"), functionTree.SubTree[0].SubTree[1].Entry));
                                            }
                                            compileState.ILGen.Emit(OpCodes.Ldfld, fi);
                                            innerExpressionReturn = typeof(double);
                                            LocalBuilder copyLb = compileState.ILGen.DeclareLocal(typeof(double));
                                            compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)1);
                                            compileState.ILGen.Emit(OpCodes.Add);
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            compileState.ILGen.Emit(OpCodes.Stloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Stfld, fi);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, structLb);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else
                                        {
                                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorDotNotSupportedFor0", "Operator '.' not supported for '{0}'"), compileState.MapType(innerExpressionReturn)));
                                        }

                                        compileState.ILGen.EndScope();
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorPlusPlusNotSupportedFor0", "Operator '++' not supported for '{0}'"), functionTree.SubTree[0].Entry));
                                    }
                                    break;

                                case "--":
                                    if (functionTree.SubTree[0].Type == Tree.EntryType.Variable || functionTree.SubTree[0].Type == Tree.EntryType.Unknown)
                                    {
                                        object v = localVars[functionTree.SubTree[0].Entry];
                                        innerExpressionReturn = GetVarToStack(compileState, v);
                                        if (innerExpressionReturn == typeof(int))
                                        {
                                            compileState.ILGen.Emit(OpCodes.Ldc_I4_1);
                                            compileState.ILGen.Emit(OpCodes.Sub);
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else if (innerExpressionReturn == typeof(long))
                                        {
                                            compileState.ILGen.Emit(OpCodes.Ldc_I8, 1L);
                                            compileState.ILGen.Emit(OpCodes.Sub);
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else if (innerExpressionReturn == typeof(double))
                                        {
                                            compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)1);
                                            compileState.ILGen.Emit(OpCodes.Sub);
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else
                                        {
                                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorMinusMinusNotSupportedFor0", "operator '--' not supported for {0}"), compileState.MapType(innerExpressionReturn)));
                                        }
                                    }
                                    else if (functionTree.SubTree[0].Type == Tree.EntryType.OperatorBinary && functionTree.SubTree[0].Entry == ".")
                                    {
                                        compileState.ILGen.BeginScope();
                                        object v = localVars[functionTree.SubTree[0].SubTree[0].Entry];
                                        innerExpressionReturn = GetVarToStack(compileState, v);
                                        if (innerExpressionReturn == typeof(Vector3))
                                        {
                                            LocalBuilder structLb = compileState.ILGen.DeclareLocal(innerExpressionReturn);
                                            compileState.ILGen.Emit(OpCodes.Stloc, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            FieldInfo fi;
                                            switch (functionTree.SubTree[0].SubTree[1].Entry)
                                            {
                                                case "x":
                                                    fi = typeof(Vector3).GetField("X");
                                                    break;

                                                case "y":
                                                    fi = typeof(Vector3).GetField("Y");
                                                    break;

                                                case "z":
                                                    fi = typeof(Vector3).GetField("Z");
                                                    break;

                                                default:
                                                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ComponentAccessFor0AtVectorIsNotDefined", "Component access for '{0}' at vector is not defined"), functionTree.SubTree[0].SubTree[1].Entry));
                                            }
                                            compileState.ILGen.Emit(OpCodes.Ldfld, fi);
                                            innerExpressionReturn = typeof(double);
                                            LocalBuilder copyLb = compileState.ILGen.DeclareLocal(typeof(double));
                                            compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)1);
                                            compileState.ILGen.Emit(OpCodes.Sub);
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            compileState.ILGen.Emit(OpCodes.Stloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Stfld, fi);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, structLb);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else if (innerExpressionReturn == typeof(Quaternion))
                                        {
                                            LocalBuilder structLb = compileState.ILGen.DeclareLocal(innerExpressionReturn);
                                            compileState.ILGen.Emit(OpCodes.Stloc, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            FieldInfo fi;
                                            switch (functionTree.SubTree[0].SubTree[1].Entry)
                                            {
                                                case "x":
                                                    fi = typeof(Quaternion).GetField("X");
                                                    break;

                                                case "y":
                                                    fi = typeof(Quaternion).GetField("Y");
                                                    break;

                                                case "z":
                                                    fi = typeof(Quaternion).GetField("Z");
                                                    break;

                                                case "s":
                                                    fi = typeof(Quaternion).GetField("S");
                                                    break;

                                                default:
                                                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ComponentAccessFor0AtRotationIsNotDefined", "Component access for '{0}' at rotation is not defined"), functionTree.SubTree[0].SubTree[1].Entry));
                                            }
                                            compileState.ILGen.Emit(OpCodes.Ldfld, fi);
                                            innerExpressionReturn = typeof(double);
                                            LocalBuilder copyLb = compileState.ILGen.DeclareLocal(typeof(double));
                                            compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)1);
                                            compileState.ILGen.Emit(OpCodes.Sub);
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            compileState.ILGen.Emit(OpCodes.Stloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Stfld, fi);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, structLb);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else
                                        {
                                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorDotNotSupportedFor0", "Operator '.' not supported for '{0}'"), compileState.MapType(innerExpressionReturn)));
                                        }

                                        compileState.ILGen.EndScope();
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorMinusMinusNotSupportedFor0", "operator '--' not supported for '{0}'"), functionTree.SubTree[0].Entry));
                                    }
                                    break;

                                default:
                                    if (functionTree.Entry.StartsWith("(") &&
                                        functionTree.Entry.EndsWith(")") &&
                                        compileState.ContainsValidVarType(functionTree.Entry.Substring(1, functionTree.Entry.Length - 2)))
                                    {
                                        expressionStack.Insert(0, new TypecastExpression(
                                            functionTree,
                                            lineNumber));
                                        innerExpressionReturn = null;
                                    }
                                    else
                                    {
                                        expressionStack.Insert(0, new LeftUnaryOperators(
                                            functionTree,
                                            lineNumber));
                                        innerExpressionReturn = null;
                                    }
                                    break;
                            }
                            break;

                        case Tree.EntryType.OperatorRightUnary:
                            switch (functionTree.Entry)
                            {
                                case "++":
                                    if (functionTree.SubTree[0].Type == Tree.EntryType.Variable)
                                    {
                                        object v = localVars[functionTree.SubTree[0].Entry];
                                        innerExpressionReturn = GetVarToStack(compileState, v);
                                        if (innerExpressionReturn == typeof(int))
                                        {
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            compileState.ILGen.Emit(OpCodes.Ldc_I4_1);
                                            compileState.ILGen.Emit(OpCodes.Add);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else if (innerExpressionReturn == typeof(long))
                                        {
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            compileState.ILGen.Emit(OpCodes.Ldc_I8, 1L);
                                            compileState.ILGen.Emit(OpCodes.Add);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else if (innerExpressionReturn == typeof(double))
                                        {
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)1);
                                            compileState.ILGen.Emit(OpCodes.Add);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else
                                        {
                                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorPlusPlusNotSupportedFor0", "operator '++' not supported for {0}"), compileState.MapType(innerExpressionReturn)));
                                        }
                                    }
                                    else if(functionTree.SubTree[0].Type == Tree.EntryType.OperatorBinary && functionTree.SubTree[0].Entry == ".")
                                    {
                                        compileState.ILGen.BeginScope();
                                        object v = localVars[functionTree.SubTree[0].SubTree[0].Entry];
                                        innerExpressionReturn = GetVarToStack(compileState, v);
                                        if (innerExpressionReturn == typeof(Vector3))
                                        {
                                            LocalBuilder structLb = compileState.ILGen.DeclareLocal(innerExpressionReturn);
                                            compileState.ILGen.Emit(OpCodes.Stloc, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            FieldInfo fi;
                                            switch(functionTree.SubTree[0].SubTree[1].Entry)
                                            {
                                                case "x":
                                                    fi = typeof(Vector3).GetField("X");
                                                    break;

                                                case "y":
                                                    fi = typeof(Vector3).GetField("Y");
                                                    break;

                                                case "z":
                                                    fi = typeof(Vector3).GetField("Z");
                                                    break;

                                                default:
                                                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ComponentAccessFor0AtVectorIsNotDefined", "Component access for '{0}' at vector is not defined"), functionTree.SubTree[0].SubTree[1].Entry));
                                            }
                                            compileState.ILGen.Emit(OpCodes.Ldfld, fi);
                                            innerExpressionReturn = typeof(double);
                                            LocalBuilder copyLb = compileState.ILGen.DeclareLocal(typeof(double));
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            compileState.ILGen.Emit(OpCodes.Stloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)1);
                                            compileState.ILGen.Emit(OpCodes.Add);
                                            compileState.ILGen.Emit(OpCodes.Stfld, fi);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, structLb);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else if(innerExpressionReturn == typeof(Quaternion))
                                        {
                                            LocalBuilder structLb = compileState.ILGen.DeclareLocal(innerExpressionReturn);
                                            compileState.ILGen.Emit(OpCodes.Stloc, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            FieldInfo fi;
                                            switch (functionTree.SubTree[0].SubTree[1].Entry)
                                            {
                                                case "x":
                                                    fi = typeof(Quaternion).GetField("X");
                                                    break;

                                                case "y":
                                                    fi = typeof(Quaternion).GetField("Y");
                                                    break;

                                                case "z":
                                                    fi = typeof(Quaternion).GetField("Z");
                                                    break;

                                                case "s":
                                                    fi = typeof(Quaternion).GetField("S");
                                                    break;

                                                default:
                                                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ComponentAccessFor0AtRotationIsNotDefined", "Component access for '{0}' at rotation is not defined"), functionTree.SubTree[0].SubTree[1].Entry));
                                            }
                                            compileState.ILGen.Emit(OpCodes.Ldfld, fi);
                                            innerExpressionReturn = typeof(double);
                                            LocalBuilder copyLb = compileState.ILGen.DeclareLocal(typeof(double));
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            compileState.ILGen.Emit(OpCodes.Stloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)1);
                                            compileState.ILGen.Emit(OpCodes.Add);
                                            compileState.ILGen.Emit(OpCodes.Stfld, fi);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, structLb);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else
                                        {
                                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorDotNotSupportedFor0", "operator '.' not supported for '{0}'"), compileState.MapType(innerExpressionReturn)));
                                        }

                                        compileState.ILGen.EndScope();
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorPlusPlusNotSupportedFor0", "operator '++' not supported for '{0}'"), functionTree.SubTree[0].Entry));
                                    }
                                    break;

                                case "--":
                                    if (functionTree.SubTree[0].Type == Tree.EntryType.Variable)
                                    {
                                        object v = localVars[functionTree.SubTree[0].Entry];
                                        innerExpressionReturn = GetVarToStack(compileState, v);
                                        if (innerExpressionReturn == typeof(int))
                                        {
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            compileState.ILGen.Emit(OpCodes.Ldc_I4_1);
                                            compileState.ILGen.Emit(OpCodes.Sub);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else if (innerExpressionReturn == typeof(long))
                                        {
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            compileState.ILGen.Emit(OpCodes.Ldc_I8, 1L);
                                            compileState.ILGen.Emit(OpCodes.Sub);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else if (innerExpressionReturn == typeof(double))
                                        {
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)1);
                                            compileState.ILGen.Emit(OpCodes.Sub);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else
                                        {
                                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorMinusMinusNotSupportedFor0", "operator '--' not supported for {0}"), compileState.MapType(innerExpressionReturn)));
                                        }
                                    }
                                    else if (functionTree.SubTree[0].Type == Tree.EntryType.OperatorBinary && functionTree.SubTree[0].Entry == ".")
                                    {
                                        compileState.ILGen.BeginScope();
                                        object v = localVars[functionTree.SubTree[0].SubTree[0].Entry];
                                        innerExpressionReturn = GetVarToStack(compileState, v);
                                        if (innerExpressionReturn == typeof(Vector3))
                                        {
                                            LocalBuilder structLb = compileState.ILGen.DeclareLocal(innerExpressionReturn);
                                            compileState.ILGen.Emit(OpCodes.Stloc, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            FieldInfo fi;
                                            switch (functionTree.SubTree[0].SubTree[1].Entry)
                                            {
                                                case "x":
                                                    fi = typeof(Vector3).GetField("X");
                                                    break;

                                                case "y":
                                                    fi = typeof(Vector3).GetField("Y");
                                                    break;

                                                case "z":
                                                    fi = typeof(Vector3).GetField("Z");
                                                    break;

                                                default:
                                                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ComponentAccessFor0AtVectorIsNotDefined", "Component access for '{0}' at vector is not defined"), functionTree.SubTree[0].SubTree[1].Entry));
                                            }
                                            compileState.ILGen.Emit(OpCodes.Ldfld, fi);
                                            innerExpressionReturn = typeof(double);
                                            LocalBuilder copyLb = compileState.ILGen.DeclareLocal(typeof(double));
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            compileState.ILGen.Emit(OpCodes.Stloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)1);
                                            compileState.ILGen.Emit(OpCodes.Sub);
                                            compileState.ILGen.Emit(OpCodes.Stfld, fi);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, structLb);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else if (innerExpressionReturn == typeof(Quaternion))
                                        {
                                            LocalBuilder structLb = compileState.ILGen.DeclareLocal(innerExpressionReturn);
                                            compileState.ILGen.Emit(OpCodes.Stloc, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            FieldInfo fi;
                                            switch (functionTree.SubTree[0].SubTree[1].Entry)
                                            {
                                                case "x":
                                                    fi = typeof(Quaternion).GetField("X");
                                                    break;

                                                case "y":
                                                    fi = typeof(Quaternion).GetField("Y");
                                                    break;

                                                case "z":
                                                    fi = typeof(Quaternion).GetField("Z");
                                                    break;

                                                case "s":
                                                    fi = typeof(Quaternion).GetField("S");
                                                    break;

                                                default:
                                                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "ComponentAccessFor0AtRotationIsNotDefined", "Component access for '{0}' at rotation is not defined"), functionTree.SubTree[0].SubTree[1].Entry));
                                            }
                                            compileState.ILGen.Emit(OpCodes.Ldfld, fi);
                                            innerExpressionReturn = typeof(double);
                                            LocalBuilder copyLb = compileState.ILGen.DeclareLocal(typeof(double));
                                            compileState.ILGen.Emit(OpCodes.Dup);
                                            compileState.ILGen.Emit(OpCodes.Stloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloca, structLb);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, copyLb);
                                            compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)1);
                                            compileState.ILGen.Emit(OpCodes.Sub);
                                            compileState.ILGen.Emit(OpCodes.Stfld, fi);
                                            compileState.ILGen.Emit(OpCodes.Ldloc, structLb);
                                            SetVarFromStack(compileState, v, lineNumber);
                                        }
                                        else
                                        {
                                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorDotNotSupportedFor0", "Operator '.' not supported for '{0}'"), compileState.MapType(innerExpressionReturn)));
                                        }

                                        compileState.ILGen.EndScope();
                                    }
                                    else
                                    {
                                        throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "OperatorMinusMinusNotSupportedFor0", "Operator '--' not supported for '{0}'"), functionTree.SubTree[0].Entry));
                                    }
                                    break;

                                default:
                                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "RightUnaryOperator0NotSupported", "Right unary operator '{0}' not supported"), functionTree.Entry));
                            }
                            break;

                        case Tree.EntryType.ReservedWord:
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "0IsAReservedWord", "'{0}' is a reserved word"), functionTree.Entry));

                        #region Constants and complex types
                        case Tree.EntryType.StringValue:
                            /* string value */
                            {
                                Tree.ConstantValueString val = (Tree.ConstantValueString)functionTree.Value;
                                compileState.ILGen.Emit(OpCodes.Ldstr, val.Value);
                                innerExpressionReturn = typeof(string);
                            }
                            break;

                        case Tree.EntryType.Rotation:
                            /* rotation */
                            if (functionTree.Value != null)
                            {
                                /* constants */
                                var val = (ConstantValueRotation)functionTree.Value;
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, val.Value.X);
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, val.Value.Y);
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, val.Value.Z);
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, val.Value.W);
                                compileState.ILGen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) }));
                                innerExpressionReturn = typeof(Quaternion);
                            }
                            else
                            {
                                expressionStack.Insert(0, new RotationExpression(
                                    functionTree,
                                    lineNumber));
                                innerExpressionReturn = null;
                            }
                            break;

                        case Tree.EntryType.Value:
                            /* value */
                            if (functionTree.Value is ConstantValueRotation)
                            {
                                var v = (ConstantValueRotation)functionTree.Value;
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, v.Value.X);
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, v.Value.Y);
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, v.Value.Z);
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, v.Value.W);
                                compileState.ILGen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) }));
                                innerExpressionReturn = typeof(Quaternion);
                            }
                            else if (functionTree.Value is ConstantValueVector)
                            {
                                var v = (ConstantValueVector)functionTree.Value;
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, v.Value.X);
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, v.Value.Y);
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, v.Value.Z);
                                compileState.ILGen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double) }));
                                innerExpressionReturn = typeof(Vector3);
                            }
                            else if (functionTree.Value is Tree.ConstantValueFloat)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, ((Tree.ConstantValueFloat)functionTree.Value).Value);
                                innerExpressionReturn = typeof(double);
                            }
                            else if (functionTree.Value is Tree.ConstantValueInt)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldc_I4, ((Tree.ConstantValueInt)functionTree.Value).Value);
                                innerExpressionReturn = typeof(int);
                            }
                            else if (functionTree.Value is Tree.ConstantValueLong)
                            {
                                compileState.ILGen.Emit(OpCodes.Ldc_I8, ((Tree.ConstantValueLong)functionTree.Value).Value);
                                innerExpressionReturn = typeof(long);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, this.GetLanguageString(compileState.CurrentCulture, "InvalidValue", "invalid value"));
                            }
                            break;

                        case Tree.EntryType.Vector:
                            /* three components */
                            if (functionTree.Value != null)
                            {
                                /* constants */
                                var val = (ConstantValueVector)functionTree.Value;
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, val.Value.X);
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, val.Value.Y);
                                compileState.ILGen.Emit(OpCodes.Ldc_R8, val.Value.Z);
                                compileState.ILGen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double) }));
                                innerExpressionReturn = typeof(Vector3);
                            }
                            else
                            {
                                expressionStack.Insert(0, new VectorExpression(
                                    functionTree,
                                    lineNumber));
                                innerExpressionReturn = null;
                            }
                            break;
                        #endregion

                        case Tree.EntryType.Variable:
                            /* variable */
                            try
                            {
                                object v = localVars[functionTree.Entry];
                                innerExpressionReturn = GetVarToStack(compileState, v);
                            }
                            catch
#if DEBUG
                                (Exception e)
#endif
                            {
#if DEBUG
                                m_Log.DebugFormat("Exception {0} at {1}", e.Message, e.StackTrace);
#endif
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "Variable0NotDefined", "Variable '{0}' not defined"), functionTree.Entry));
                            }
                            break;

                        case Tree.EntryType.Level:
                            switch (functionTree.Entry)
                            {
                                case "[":
                                    /* we got a list */
                                    expressionStack.Insert(0, new ListExpression(
                                        compileState,
                                        functionTree,
                                        lineNumber));
                                    innerExpressionReturn = null;
                                    break;

                                default:
                                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "UnexpectedLevelEntry0", "unexpected level entry '{0}'"), functionTree.Entry));
                            }
                            break;

                        default:
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(compileState.CurrentCulture, "Unknown0", "unknown '{0}'"), functionTree.Entry));
                    }
                }
            }
        }
    }
}
