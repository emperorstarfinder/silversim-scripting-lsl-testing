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
using System;
using System.Collections.Generic;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        private static CompilerException CompilerException(LineInfo p, string message) =>
            new CompilerException(p.LineNumber, message);

        public enum ResultIsModifiedEnum
        {
            No,
            Yes
        }

        private ResultIsModifiedEnum ProcessExpression(
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
            Tree expressionTree = LineToExpressionTree(compileState, expressionLine, localVars.Keys, functionLine.LineNumber, compileState.CurrentCulture);

            return ProcessExpression(
                compileState,
                expectedType,
                expressionTree,
                functionLine.LineNumber,
                localVars);
        }

        private ResultIsModifiedEnum ProcessExpression(
            CompileState compileState,
            Type expectedType,
            Tree functionTree,
            int lineNumber,
            Dictionary<string, object> localVars)
        {
            var isModified = ResultIsModifiedEnum.No;

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

            if(functionTree.SubTree[0].Type == Tree.EntryType.Variable ||
                functionTree.SubTree[0].Type == Tree.EntryType.ThisOperator)
            {
                /* variables are unmodified */
            }
            else if(functionTree.SubTree[0].Type != Tree.EntryType.OperatorBinary ||
                    !(functionTree.SubTree[0].Entry == "+=" ||
                    functionTree.SubTree[0].Entry == "-=" ||
                    functionTree.SubTree[0].Entry == "*=" ||
                    functionTree.SubTree[0].Entry == "/=" ||
                    functionTree.SubTree[0].Entry == "%=" ||
                    functionTree.SubTree[0].Entry == "."))
            {
                isModified = ResultIsModifiedEnum.Yes;
            }
            if(functionTree.SubTree[0].Value != null)
            {
                isModified = ResultIsModifiedEnum.Yes;
            }

            if (expectedType != retType)
            {
                isModified = ResultIsModifiedEnum.Yes;
            }
            return isModified;
        }

        private Type ProcessExpressionToAnyType(
            CompileState compileState,
            int startAt,
            int endAt,
            LineInfo functionLine,
            Dictionary<string, object> localVars)
        {
            if (startAt > endAt)
            {
                throw new NotSupportedException();
            }

            List<string> expressionLine = functionLine.Line.GetRange(startAt, endAt - startAt + 1);
            Tree expressionTree = LineToExpressionTree(compileState, expressionLine, localVars.Keys, functionLine.LineNumber, compileState.CurrentCulture);

            return ProcessExpressionPart(
                compileState,
                expressionTree,
                functionLine.LineNumber,
                localVars);
        }
    }
}
