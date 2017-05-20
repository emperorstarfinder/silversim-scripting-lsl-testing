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

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        private sealed class TypecastExpression : IExpressionStackElement
        {
            private readonly Tree m_TypecastTree;
            private readonly Type m_TargetType;
            private readonly int m_LineNumber;

            public TypecastExpression(
                Tree functionTree,
                int lineNumber)
            {
                m_LineNumber = lineNumber;
                m_TypecastTree = functionTree.SubTree[0];
                switch (functionTree.Entry)
                {
                    case "(long)":
                        m_TargetType = typeof(long);
                        break;

                    case "(integer)":
                        m_TargetType = typeof(int);
                        break;

                    case "(float)":
                        m_TargetType = typeof(double);
                        break;

                    case "(string)":
                        m_TargetType = typeof(string);
                        break;

                    case "(key)":
                        m_TargetType = typeof(LSLKey);
                        break;

                    case "(list)":
                        m_TargetType = typeof(AnArray);
                        break;

                    case "(vector)":
                        m_TargetType = typeof(Vector3);
                        break;

                    case "(rotation)":
                    case "(quaternion)":
                        m_TargetType = typeof(Quaternion);
                        break;

                    default:
                        throw new CompilerException(lineNumber, string.Format("Internal Error! {0} is not a valid typecast", functionTree.Entry));
                }
            }

            public Tree ProcessNextStep(
                LSLCompiler lslCompiler,
                CompileState compileState,
                Dictionary<string, object> localVars,
                Type innerExpressionReturn)
            {
                if(innerExpressionReturn == null)
                {
                    return m_TypecastTree;
                }
                else
                {
                    ProcessCasts(compileState, m_TargetType, innerExpressionReturn, m_LineNumber);
                    throw new ReturnTypeException(m_TargetType, m_LineNumber);
                }
            }
        }
    }
}
