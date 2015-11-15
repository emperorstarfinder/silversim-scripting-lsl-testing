﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scripting.Lsl.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        sealed class RotationExpression : IExpressionStackElement
        {
            readonly List<Tree> m_ListElements = new List<Tree>();
            readonly int m_LineNumber;

            public RotationExpression(
                LSLCompiler lslCompiler,
                CompileState compileState,
                Tree functionTree,
                int lineNumber,
                Dictionary<string, object> localVars)
            {
                m_LineNumber = lineNumber;
                for (int i = 0; i < 4; ++i)
                {
                    if (functionTree.SubTree[i].Type == Tree.EntryType.DeclarationArgument)
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
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(Quaternion).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) }));
                    throw new ReturnTypeException(typeof(Quaternion), m_LineNumber);
                }
                else
                {
                    return m_ListElements[0];
                }
            }
        }
    }
}
