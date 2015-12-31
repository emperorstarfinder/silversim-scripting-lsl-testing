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
        sealed class ListExpression : IExpressionStackElement
        {
            readonly LocalBuilder m_NewList;
            readonly List<Tree> m_ListElements = new List<Tree>();
            readonly int m_LineNumber;

            public ListExpression(
                LSLCompiler lslCompiler,
                CompileState compileState,
                Tree functionTree,
                int lineNumber,
                Dictionary<string, object> localVars)
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
                        throw new CompilerException(lineNumber, "Wrong list declaration");
                    }
                    if(st.Type != Tree.EntryType.DeclarationArgument)
                    {
                        throw new CompilerException(lineNumber, "Wrong list declaration");
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
                if(null != innerExpressionReturn)
                {
                    if (innerExpressionReturn == typeof(void))
                    {
                        throw new CompilerException(m_LineNumber, "Function has no return value");
                    }
                    else if (innerExpressionReturn == typeof(int) || innerExpressionReturn == typeof(double) || innerExpressionReturn == typeof(string))
                    {
                        compileState.ILGen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { innerExpressionReturn }));
                    }
                    else if (innerExpressionReturn == typeof(LSLKey) || innerExpressionReturn == typeof(Vector3) || innerExpressionReturn == typeof(Quaternion))
                    {
                        compileState.ILGen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                    }
                    else if (innerExpressionReturn == typeof(AnArray))
                    {
                        throw new CompilerException(m_LineNumber, "Lists cannot be put into lists");
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
                    throw new ReturnTypeException(typeof(AnArray), m_LineNumber);
                }
                else
                {
                    return m_ListElements[0];
                }
            }
        }
    }
}
