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

using SilverSim.Scripting.Lsl.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        sealed class VectorExpression : IExpressionStackElement
        {
            readonly List<Tree> m_ListElements = new List<Tree>();
            readonly int m_LineNumber;

            public VectorExpression(
                LSLCompiler lslCompiler,
                CompileState compileState,
                Tree functionTree,
                int lineNumber,
                Dictionary<string, object> localVars)
            {
                m_LineNumber = lineNumber;
                for (int i = 0; i < 3; ++i)
                {
                    if(functionTree.SubTree[i].Type == Tree.EntryType.DeclarationArgument)
                    {
                        m_ListElements.Add(functionTree.SubTree[i].SubTree[0]);
                    }
                    else
                    {
                        m_ListElements.Add(functionTree.SubTree[i]);
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
                    ProcessImplicitCasts(compileState, typeof(double), innerExpressionReturn, m_LineNumber);
                    m_ListElements.RemoveAt(0);
                }

                if (m_ListElements.Count == 0)
                {
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(Vector3).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double) }));
                    throw new ReturnTypeException(typeof(Vector3), m_LineNumber);
                }
                else
                {
                    return m_ListElements[0];
                }
            }
        }
    }
}
