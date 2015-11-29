// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl.Expression;
using System;
using System.Collections.Generic;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        static CompilerException CompilerException(LineInfo p, string message)
        {
            return new CompilerException(p.LineNumber, message);
        }

        void ProcessExpression(
            CompileState compileState,
            Type expectedType,
            int startAt,
            int endAt,
            LineInfo functionLine,
            Dictionary<string, object> localVars)
        {
            if(startAt > endAt)
            {
                throw new NotSupportedException();
            }

            List<string> expressionLine = functionLine.Line.GetRange(startAt, endAt - startAt + 1);
            Tree expressionTree = LineToExpressionTree(compileState, expressionLine, localVars.Keys, functionLine.LineNumber);

            ProcessExpression(
                compileState, 
                expectedType, 
                expressionTree,
                functionLine.LineNumber,
                localVars);
        }

        void ProcessExpression(
            CompileState compileState,
            Type expectedType,
            Tree functionTree,
            int lineNumber,
            Dictionary<string, object> localVars)
        {
            Type retType = ProcessExpressionPart(
                compileState,
                functionTree,
                lineNumber,
                localVars);
            ProcessImplicitCasts(
                compileState,
                expectedType,
                retType,
                lineNumber);
        }
    }
}
