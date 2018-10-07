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
using System.Reflection.Emit;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        public static void AddVector3ToList(AnArray array, Vector3 v)
        {
            array.Add(v);
        }

        public static void AddQuaternionToList(AnArray array, Quaternion q)
        {
            array.Add(q);
        }

        private sealed class ListExpression : IExpressionStackElement
        {
            private readonly LocalBuilder m_NewList;
            private readonly List<Tree> m_ListElements = new List<Tree>();
            private readonly int m_LineNumber;

            public ListExpression(
                CompileState compileState,
                Tree functionTree,
                int lineNumber)
            {
                m_LineNumber = lineNumber;
                compileState.ILGen.BeginScope();
                m_NewList = compileState.ILGen.DeclareLocal(typeof(AnArray));
                compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(Type.EmptyTypes));
                compileState.ILGen.Emit(OpCodes.Stloc, m_NewList);
                for (int i = 0; i < functionTree.SubTree.Count; ++i)
                {
                    Tree st = functionTree.SubTree[i];
                    if(st.SubTree.Count != 1)
                    {
                        throw new CompilerException(lineNumber, this.GetLanguageString(compileState.CurrentCulture, "WrongListDeclaration", "Wrong list declaration"));
                    }
                    if(st.Type != Tree.EntryType.DeclarationArgument)
                    {
                        throw new CompilerException(lineNumber, this.GetLanguageString(compileState.CurrentCulture, "WrongListDeclaration", "Wrong list declaration"));
                    }
                    m_ListElements.Add(st.SubTree[0]);
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
                    if (innerExpressionReturn == typeof(void))
                    {
                        throw new CompilerException(m_LineNumber, this.GetLanguageString(compileState.CurrentCulture, "FunctionHasNoReturnValue", "Function has no return value"));
                    }
                    else if (innerExpressionReturn == typeof(int) || innerExpressionReturn == typeof(double) || innerExpressionReturn == typeof(string))
                    {
                        compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { innerExpressionReturn }));
                    }
                    else if (innerExpressionReturn == typeof(char))
                    {
                        compileState.ILGen.Emit(OpCodes.Callvirt, typeof(char).GetMethod("ToString", Type.EmptyTypes));
                        compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { typeof(string) }));
                    }
                    else if (innerExpressionReturn == typeof(long))
                    {
                        compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { typeof(long) }));
                    }
                    else if (innerExpressionReturn == typeof(LSLKey))
                    {
                        compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                    }
                    else if (innerExpressionReturn == typeof(Vector3))
                    {
                        compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("AddVector3ToList"));
                    }
                    else if (innerExpressionReturn == typeof(Quaternion))
                    {
                        compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("AddQuaternionToList"));
                    }
                    else if (innerExpressionReturn == typeof(AnArray))
                    {
                        throw new CompilerException(m_LineNumber, this.GetLanguageString(compileState.CurrentCulture, "ListsCannotBePutIntoList", "Lists cannot be put into lists"));
                    }
                    else
                    {
                        throw new CompilerException(m_LineNumber, "Internal error");
                    }
                    m_ListElements.RemoveAt(0);
                }

                compileState.ILGen.Emit(OpCodes.Ldloc, m_NewList);
                if (m_ListElements.Count == 0)
                {
                    compileState.ILGen.EndScope();
                    throw new ReturnTypeException(compileState, typeof(AnArray), m_LineNumber);
                }
                else
                {
                    return m_ListElements[0];
                }
            }
        }
    }
}
