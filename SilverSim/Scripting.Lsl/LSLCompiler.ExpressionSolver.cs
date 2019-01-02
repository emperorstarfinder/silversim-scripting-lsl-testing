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

#pragma warning disable RCS1029

using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Scripting.Lsl.Expression;
using SilverSim.Types;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        #region Order Tree according to definitions
        private int FindBeginOfElementSelectors(Tree tree, int startPos, CultureInfo currentCulture)
        {
            while(startPos >= 0)
            {
                switch (tree.SubTree[startPos].Type)
                {
                    case Tree.EntryType.Variable:
                    case Tree.EntryType.Declaration:
                    case Tree.EntryType.Function:
                    case Tree.EntryType.Vector:
                    case Tree.EntryType.Rotation:
                    case Tree.EntryType.ThisOperator:
                    case Tree.EntryType.StringValue:
                    case Tree.EntryType.CharValue:
                    case Tree.EntryType.Value:
                        return startPos;

                    case Tree.EntryType.OperatorBinary:
                        if(tree.SubTree[startPos].Entry == ".")
                        {
                            return startPos;
                        }
                        goto default;

                    case Tree.EntryType.MemberName:
                        if(startPos < 2)
                        {
                            throw new CompilerException(tree.SubTree[startPos].LineNumber, this.GetLanguageString(currentCulture, "ElementSelectorRequiresVariableDeclarationOrAFunctionReturnValue", "element selector requires variable, declaration or a function with a return value"));
                        }
                        startPos -= 2;
                        break;

                    default:
                        throw new CompilerException(tree.SubTree[startPos].LineNumber, this.GetLanguageString(currentCulture, "ElementSelectorRequiresVariableDeclarationOrAFunctionReturnValue", "element selector requires variable, declaration or a function with a return value"));
                }
            }

            return -1;
        }

        private void OrderOperators_ElementSelector(Tree tree, CultureInfo currentCulture)
        {
            var enumeratorStack = new List<Tree>();
            enumeratorStack.Insert(0, tree);
            while (enumeratorStack.Count != 0)
            {
                tree = enumeratorStack[0];
                enumeratorStack.RemoveAt(0);
                int pos = tree.SubTree.Count;
                while (pos-- > 0)
                {
                    Tree elem = tree.SubTree[pos];
                    if (elem.Entry != "." ||
                        elem.Type != Tree.EntryType.OperatorUnknown)
                    {
                        switch (elem.Type)
                        {
                            case Tree.EntryType.Level:
                            case Tree.EntryType.FunctionArgument:
                            case Tree.EntryType.Function:
                            case Tree.EntryType.Declaration:
                            case Tree.EntryType.DeclarationArgument:
                            case Tree.EntryType.Vector:
                            case Tree.EntryType.Rotation:
                            case Tree.EntryType.ThisOperator:
                                enumeratorStack.Add(elem);
                                break;

                            default:
                                break;
                        }
                        continue;
                    }
                    if (pos == 0)
                    {
                        throw new CompilerException(tree.LineNumber, this.GetLanguageString(currentCulture, "ElementSelectorNeedsAVectorOrRotationToSelectSomething", "element selector needs a vector or rotation to select something"));
                    }
                    if (pos + 1 >= tree.SubTree.Count)
                    {
                        throw new CompilerException(tree.LineNumber, this.GetLanguageString(currentCulture, "ElementSelectorNeedsASelector", "element selector needs a selector"));
                    }

                    int startPos = FindBeginOfElementSelectors(tree, pos - 1, currentCulture);
                    if(startPos < 0)
                    {
                        throw new CompilerException(tree.LineNumber, this.GetLanguageString(currentCulture, "ElementSelectorRequiresVariableDeclarationOrAFunctionReturnValue", "element selector requires variable, declaration or a function with a return value"));
                    }

                    if(tree.SubTree[pos + 1].Type != Tree.EntryType.MemberName &&
                        tree.SubTree[pos + 1].Type != Tree.EntryType.Function)
                    {
                        throw new CompilerException(tree.LineNumber, this.GetLanguageString(currentCulture, "ElementSelectorNeedsASelector", "element selector needs a selector"));
                    }

                    enumeratorStack.Add(tree.SubTree[startPos]);
                    int elemPos = startPos + 1;
                    while (elemPos <= pos)
                    {
                        elem = tree.SubTree[elemPos];
                        elem.SubTree.Add(tree.SubTree[elemPos - 1]);
                        elem.SubTree.Add(tree.SubTree[elemPos + 1]);
                        elem.Type = Tree.EntryType.OperatorBinary;
                        tree.SubTree.RemoveAt(elemPos + 1);
                        tree.SubTree.RemoveAt(elemPos - 1);
                        pos -= 2;
                    }
                    pos = startPos;
                }
            }
        }

        private void OrderOperators_IncsDecs(Tree tree, CultureInfo currentCulture)
        {
            var enumeratorStack = new List<Tree>();
            enumeratorStack.Insert(0, tree);
            while (enumeratorStack.Count != 0)
            {
                tree = enumeratorStack[0];
                enumeratorStack.RemoveAt(0);
                int pos = tree.SubTree.Count;
                while (pos-- > 0)
                {
                    Tree elem = tree.SubTree[pos];
                    if ((elem.Entry != "++" && elem.Entry != "--") ||
                        elem.Type != Tree.EntryType.OperatorUnknown)
                    {
                        switch (elem.Type)
                        {
                            case Tree.EntryType.Level:
                            case Tree.EntryType.FunctionArgument:
                            case Tree.EntryType.Function:
                            case Tree.EntryType.Declaration:
                            case Tree.EntryType.DeclarationArgument:
                            case Tree.EntryType.Vector:
                            case Tree.EntryType.Rotation:
                                enumeratorStack.Add(elem);
                                break;

                            default:
                                break;
                        }
                        continue;
                    }

                    if (pos > 0 &&
                        (tree.SubTree[pos - 1].Type == Tree.EntryType.Variable ||
                        (tree.SubTree[pos - 1].Type == Tree.EntryType.OperatorBinary &&
                        tree.SubTree[pos - 1].Entry == ".")))
                    {
                        /* either variable or element selector */
                        if (tree.SubTree[pos - 1].Type == Tree.EntryType.OperatorBinary &&
                            tree.SubTree[pos - 1].SubTree[0].Type != Tree.EntryType.Variable)
                        {
                            throw new CompilerException(tree.SubTree[pos - 1].LineNumber, string.Format(this.GetLanguageString(currentCulture, "0NeedsVariableBeforeDot", "'{0}' needs a variable before '.'."), elem.Entry));
                        }

                        /* right unary */
                        elem.Type = Tree.EntryType.OperatorRightUnary;
                        elem.SubTree.Add(tree.SubTree[pos - 1]);
                        tree.SubTree.RemoveAt(--pos);
                    }
                    else if (pos + 1 < tree.SubTree.Count &&
                        (tree.SubTree[pos + 1].Type == Tree.EntryType.Variable ||
                        (tree.SubTree[pos + 1].Type == Tree.EntryType.OperatorBinary &&
                        tree.SubTree[pos + 1].Entry == ".")))
                    {
                        /* either variable or element selector */
                        if (tree.SubTree[pos + 1].Type == Tree.EntryType.OperatorBinary &&
                            tree.SubTree[pos + 1].SubTree[0].Type != Tree.EntryType.Variable)
                        {
                            throw new CompilerException(tree.SubTree[pos + 1].LineNumber, string.Format(this.GetLanguageString(currentCulture, "0NeedsVariableBeforeDot", "'{0}' needs a variable before '.'."), elem.Entry));
                        }

                        /* left unary */
                        elem.Type = Tree.EntryType.OperatorLeftUnary;
                        elem.SubTree.Add(tree.SubTree[pos + 1]);
                        tree.SubTree.RemoveAt(pos + 1);
                    }
                    else if (pos + 1 < tree.SubTree.Count &&
                             (tree.SubTree[pos + 1].Type == Tree.EntryType.OperatorRightUnary &&
                             (tree.SubTree[pos + 1].Entry == "++" || tree.SubTree[pos + 1].Entry == "--")))
                    {
                        /* left unary */
                        elem.Type = Tree.EntryType.OperatorLeftUnary;
                        elem.SubTree.Add(tree.SubTree[pos + 1]);
                        tree.SubTree.RemoveAt(pos + 1);
                    }
                }
            }
        }

        private void OrderOperators_Common_LtoR(Tree tree, List<string> operators, CultureInfo currentCulture)
        {
            var enumeratorStack = new List<Tree>();
            enumeratorStack.Insert(0, tree);
            while (enumeratorStack.Count != 0)
            {
                tree = enumeratorStack[0];
                enumeratorStack.RemoveAt(0);
                for (int pos = 0; pos < tree.SubTree.Count; ++pos)
                {
                    Tree elem = tree.SubTree[pos];
                    string ent = elem.Entry;
                    if (!operators.Contains(ent) ||
                        elem.Type != Tree.EntryType.OperatorUnknown)
                    {
                        switch (elem.Type)
                        {
                            case Tree.EntryType.Level:
                            case Tree.EntryType.FunctionArgument:
                            case Tree.EntryType.Function:
                            case Tree.EntryType.Declaration:
                            case Tree.EntryType.DeclarationArgument:
                            case Tree.EntryType.Vector:
                            case Tree.EntryType.Rotation:
                            case Tree.EntryType.OperatorBinary:
                            case Tree.EntryType.OperatorLeftUnary:
                            case Tree.EntryType.OperatorRightUnary:
                            case Tree.EntryType.ThisOperator:
                                enumeratorStack.Add(elem);
                                break;

                            default:
                                break;
                        }
                        continue;
                    }

                    if (pos == 0)
                    {
                        throw new CompilerException(elem.LineNumber, string.Format(this.GetLanguageString(currentCulture, "MissingLValueTo0", "missing l-value to '{0}'"), ent));
                    }
                    else if (pos + 1 >= tree.SubTree.Count)
                    {
                        throw new CompilerException(elem.LineNumber, string.Format(this.GetLanguageString(currentCulture, "MissingRValueTo0", "missing r-value to '{0}'"), ent));
                    }

                    switch (tree.SubTree[pos - 1].Type)
                    {
                        case Tree.EntryType.Variable:
                        case Tree.EntryType.Value:
                        case Tree.EntryType.CharValue:
                        case Tree.EntryType.StringValue:
                        case Tree.EntryType.Function:
                        case Tree.EntryType.Declaration:
                        case Tree.EntryType.Level:
                        case Tree.EntryType.OperatorBinary:
                        case Tree.EntryType.OperatorLeftUnary:
                        case Tree.EntryType.OperatorRightUnary:
                        case Tree.EntryType.Vector:
                        case Tree.EntryType.Rotation:
                        case Tree.EntryType.ThisOperator:
                            break;

                        default:
                            throw new CompilerException(tree.SubTree[pos - 1].LineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidLValueTo0", "invalid l-value to '{0}'"), ent));
                    }

                    switch (tree.SubTree[pos + 1].Type)
                    {
                        case Tree.EntryType.Variable:
                        case Tree.EntryType.Value:
                        case Tree.EntryType.CharValue:
                        case Tree.EntryType.StringValue:
                        case Tree.EntryType.Function:
                        case Tree.EntryType.Declaration:
                        case Tree.EntryType.Level:
                        case Tree.EntryType.OperatorBinary:
                        case Tree.EntryType.OperatorLeftUnary:
                        case Tree.EntryType.OperatorRightUnary:
                        case Tree.EntryType.Vector:
                        case Tree.EntryType.Rotation:
                        case Tree.EntryType.ThisOperator:
                            break;

                        default:
                            throw new CompilerException(tree.SubTree[pos + 1].LineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidRValueTo0", "invalid r-value to '{0}'"), ent));
                    }

                    enumeratorStack.Add(tree.SubTree[pos - 1]);
                    enumeratorStack.Add(tree.SubTree[pos + 1]);
                    elem.SubTree.Add(tree.SubTree[pos - 1]);
                    elem.SubTree.Add(tree.SubTree[pos + 1]);
                    elem.Type = Tree.EntryType.OperatorBinary;
                    tree.SubTree.RemoveAt(pos + 1);
                    tree.SubTree.RemoveAt(--pos);
                }
            }
        }

        private void OrderOperators_Common(Tree tree, List<string> operators, CultureInfo currentCulture)
        {
            var enumeratorStack = new List<Tree>();
            enumeratorStack.Insert(0, tree);
            while (enumeratorStack.Count != 0)
            {
                tree = enumeratorStack[0];
                enumeratorStack.RemoveAt(0);
                int pos = tree.SubTree.Count;
                while (pos-- > 0)
                {
                    Tree elem = tree.SubTree[pos];
                    string ent = elem.Entry;
                    if (!operators.Contains(ent) ||
                        elem.Type != Tree.EntryType.OperatorUnknown)
                    {
                        switch (elem.Type)
                        {
                            case Tree.EntryType.Level:
                            case Tree.EntryType.FunctionArgument:
                            case Tree.EntryType.Function:
                            case Tree.EntryType.Declaration:
                            case Tree.EntryType.DeclarationArgument:
                            case Tree.EntryType.Vector:
                            case Tree.EntryType.Rotation:
                            case Tree.EntryType.OperatorBinary:
                            case Tree.EntryType.OperatorLeftUnary:
                            case Tree.EntryType.OperatorRightUnary:
                            case Tree.EntryType.ThisOperator:
                                enumeratorStack.Add(elem);
                                break;

                            default:
                                break;
                        }
                        continue;
                    }

                    if(pos == 0)
                    {
                        throw new CompilerException(elem.LineNumber, string.Format(this.GetLanguageString(currentCulture, "MissingLValueTo0", "missing l-value to '{0}'"), ent));
                    }
                    else if(pos + 1 >= tree.SubTree.Count)
                    {
                        throw new CompilerException(elem.LineNumber, string.Format(this.GetLanguageString(currentCulture, "MissingRValueTo0", "missing r-value to '{0}'"), ent));
                    }

                    switch(tree.SubTree[pos - 1].Type)
                    {
                        case Tree.EntryType.Variable:
                        case Tree.EntryType.Value:
                        case Tree.EntryType.CharValue:
                        case Tree.EntryType.StringValue:
                        case Tree.EntryType.Function:
                        case Tree.EntryType.Declaration:
                        case Tree.EntryType.Level:
                        case Tree.EntryType.OperatorBinary:
                        case Tree.EntryType.OperatorLeftUnary:
                        case Tree.EntryType.OperatorRightUnary:
                        case Tree.EntryType.Vector:
                        case Tree.EntryType.Rotation:
                        case Tree.EntryType.ThisOperator:
                            break;

                        default:
                            throw new CompilerException(tree.SubTree[pos - 1].LineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidLValueTo0", "invalid l-value to '{0}'"), ent));
                    }

                    switch (tree.SubTree[pos + 1].Type)
                    {
                        case Tree.EntryType.Variable:
                        case Tree.EntryType.Value:
                        case Tree.EntryType.CharValue:
                        case Tree.EntryType.StringValue:
                        case Tree.EntryType.Function:
                        case Tree.EntryType.Declaration:
                        case Tree.EntryType.Level:
                        case Tree.EntryType.OperatorBinary:
                        case Tree.EntryType.OperatorLeftUnary:
                        case Tree.EntryType.OperatorRightUnary:
                        case Tree.EntryType.Vector:
                        case Tree.EntryType.Rotation:
                        case Tree.EntryType.ThisOperator:
                            break;

                        default:
                            throw new CompilerException(tree.SubTree[pos + 1].LineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidRValueTo0", "invalid r-value to '{0}'"), ent));
                    }

                    enumeratorStack.Add(tree.SubTree[pos - 1]);
                    enumeratorStack.Add(tree.SubTree[pos + 1]);
                    elem.SubTree.Add(tree.SubTree[pos - 1]);
                    elem.SubTree.Add(tree.SubTree[pos + 1]);
                    elem.Type = Tree.EntryType.OperatorBinary;
                    tree.SubTree.RemoveAt(pos + 1);
                    tree.SubTree.RemoveAt(--pos);
                }
            }
        }

        private readonly List<string> m_AssignmentOps = new List<string>(new string[] { "=", "+=", "-=", "/=", "%=", "*=", "&=", "|=", "^=" });
        private bool Assignments_ExtractLeftUnaryOnLValue(Tree tree, out Tree start, out Tree end, out Tree variable)
        {
            variable = null;
            end = null;
            start = tree;
            while(tree.Type == Tree.EntryType.OperatorLeftUnary &&
                (tree.Entry == "~" || tree.Entry == "!"))
            {
                end = tree;
                if(tree.SubTree.Count != 1)
                {
                    return false;
                }
                tree = tree.SubTree[0];
            }

            while(tree.Type == Tree.EntryType.OperatorBinary && tree.Entry == ".")
            {
                tree = tree.SubTree[0];
            }

            if (tree.Type == Tree.EntryType.Variable)
            {
                variable = tree;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool Assignments_HasVariableTree(Tree tree)
        {
            while(tree.Entry == "." && tree.Type == Tree.EntryType.OperatorBinary)
            {
                if(tree.SubTree.Count < 1)
                {
                    return false;
                }
                tree = tree.SubTree[0];
            }
            return tree.Type == Tree.EntryType.Variable;
        }

        private void OrderOperators_Assignments(Tree tree, CultureInfo currentCulture)
        {
            var enumeratorStack = new List<Tree>();
            enumeratorStack.Insert(0, tree);
            while (enumeratorStack.Count != 0)
            {
                tree = enumeratorStack[0];
                enumeratorStack.RemoveAt(0);
                int pos = tree.SubTree.Count;
                while (pos-- > 0)
                {
                    Tree variable = null;
                    Tree endlvalueunarytree = null;
                    Tree startlvalueunarytree = null;
                    Tree elem = tree.SubTree[pos];
                    string ent = elem.Entry;
                    if (!m_AssignmentOps.Contains(ent) ||
                        elem.Type != Tree.EntryType.OperatorUnknown)
                    {
                        switch (elem.Type)
                        {
                            case Tree.EntryType.Level:
                            case Tree.EntryType.FunctionArgument:
                            case Tree.EntryType.Function:
                            case Tree.EntryType.Declaration:
                            case Tree.EntryType.DeclarationArgument:
                            case Tree.EntryType.Vector:
                            case Tree.EntryType.Rotation:
                            case Tree.EntryType.OperatorBinary:
                            case Tree.EntryType.OperatorLeftUnary:
                            case Tree.EntryType.OperatorRightUnary:
                                enumeratorStack.Add(elem);
                                break;

                            default:
                                break;
                        }
                        continue;
                    }

                    if (pos == 0)
                    {
                        throw new CompilerException(elem.LineNumber, string.Format(this.GetLanguageString(currentCulture, "MissingLValueTo0", "missing l-value to '{0}'"), ent));
                    }
                    else if (pos + 1 >= tree.SubTree.Count)
                    {
                        throw new CompilerException(elem.LineNumber, string.Format(this.GetLanguageString(currentCulture, "MissingRValueTo0", "missing r-value to '{0}'"), ent));
                    }

                    switch (tree.SubTree[pos - 1].Type)
                    {
                        case Tree.EntryType.Variable:
                        case Tree.EntryType.ThisOperator:
                            break;

                        case Tree.EntryType.OperatorBinary:
                            if(tree.SubTree[pos - 1].Entry != "." ||
                                Assignments_HasVariableTree(tree.SubTree[1]))
                            {
                                throw new CompilerException(tree.SubTree[pos - 1].LineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidLValueTo0", "invalid l-value to '{0}'"), ent));
                            }
                            break;

                        case Tree.EntryType.OperatorLeftUnary:
                            if(!((tree.SubTree[pos - 1].Entry == "~" || tree.SubTree[pos - 1].Entry == "!") &&
                                Assignments_ExtractLeftUnaryOnLValue(tree.SubTree[pos - 1], out startlvalueunarytree, out endlvalueunarytree, out variable)))
                            {
                                throw new CompilerException(tree.SubTree[pos - 1].LineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidLValueTo0", "invalid l-value to '{0}'"), ent));
                            }
                            break;

                        default:
                            throw new CompilerException(tree.SubTree[pos - 1].LineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidLValueTo0", "invalid l-value to '{0}'"), ent));
                    }

                    switch (tree.SubTree[pos + 1].Type)
                    {
                        case Tree.EntryType.Variable:
                        case Tree.EntryType.Value:
                        case Tree.EntryType.CharValue:
                        case Tree.EntryType.StringValue:
                        case Tree.EntryType.Function:
                        case Tree.EntryType.Declaration:
                        case Tree.EntryType.Level:
                        case Tree.EntryType.OperatorBinary:
                        case Tree.EntryType.OperatorLeftUnary:
                        case Tree.EntryType.OperatorRightUnary:
                        case Tree.EntryType.Vector:
                        case Tree.EntryType.Rotation:
                        case Tree.EntryType.ThisOperator:
                            break;

                        default:
                            throw new CompilerException(tree.SubTree[pos + 1].LineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidRValueTo0", "invalid r-value to '{0}'"), ent));
                    }

                    if (startlvalueunarytree != null)
                    {
                        enumeratorStack.Add(tree.SubTree[pos + 1]);
                        elem.SubTree.Add(variable);
                        elem.SubTree.Add(tree.SubTree[pos + 1]);
                        elem.Type = Tree.EntryType.OperatorBinary;
                        tree.SubTree.RemoveAt(pos + 1);
                        tree.SubTree.RemoveAt(--pos);

                        endlvalueunarytree.SubTree.Clear();
                        endlvalueunarytree.SubTree.Add(elem);
                        tree.SubTree[pos] = startlvalueunarytree;
                    }
                    else
                    {
                        enumeratorStack.Add(tree.SubTree[pos + 1]);
                        elem.SubTree.Add(tree.SubTree[pos - 1]);
                        elem.SubTree.Add(tree.SubTree[pos + 1]);
                        elem.Type = Tree.EntryType.OperatorBinary;
                        tree.SubTree.RemoveAt(pos + 1);
                        tree.SubTree.RemoveAt(--pos);
                    }
                }
            }
        }

        private readonly List<string> m_TypecastOperators = new List<string>(new string[] {
            "(string)",
            "(list)",
            "(float)",
            "(vector)",
            "(rotation)",
            "(quaternion)",
            "(integer)",
            "(key)"
            });

        private void OrderOperators_UnaryLefts(CompileState cs, Tree tree, CultureInfo currentCulture)
        {
            var enumeratorStack = new List<Tree>();
            enumeratorStack.Insert(0, tree);
            while (enumeratorStack.Count != 0)
            {
                tree = enumeratorStack[0];
                enumeratorStack.RemoveAt(0);
                int pos = tree.SubTree.Count;
                while (pos-- > 0)
                {
                    Tree elem = tree.SubTree[pos];
                    string ent = elem.Entry;
                    if (elem.Type != Tree.EntryType.OperatorUnknown)
                    {
                        switch (elem.Type)
                        {
                            case Tree.EntryType.Level:
                            case Tree.EntryType.FunctionArgument:
                            case Tree.EntryType.Function:
                            case Tree.EntryType.Declaration:
                            case Tree.EntryType.DeclarationArgument:
                            case Tree.EntryType.Vector:
                            case Tree.EntryType.Rotation:
                            case Tree.EntryType.ThisOperator:
                                enumeratorStack.Add(elem);
                                break;

                            default:
                                break;
                        }
                        continue;
                    }

                    if (ent == "!" || ent == "~" || m_TypecastOperators.Contains(ent) ||
                        (cs.LanguageExtensions.EnableLongIntegers && ent == "(long)") ||
                        (cs.LanguageExtensions.EnableCharacterType && ent == "(char)") ||
                        cs.ContainsValidVarType(ent))
                    {
                        if (pos + 1 < tree.SubTree.Count ||
                            (pos == 0 ||
                            tree.SubTree[pos - 1].Type == Tree.EntryType.OperatorUnknown))
                        {
                            switch (tree.SubTree[pos + 1].Type)
                            {
                                case Tree.EntryType.OperatorLeftUnary:
                                case Tree.EntryType.OperatorRightUnary:
                                case Tree.EntryType.OperatorBinary:
                                case Tree.EntryType.Function:
                                case Tree.EntryType.Declaration:
                                case Tree.EntryType.Level:
                                case Tree.EntryType.Value:
                                case Tree.EntryType.Variable:
                                case Tree.EntryType.CharValue:
                                case Tree.EntryType.StringValue:
                                case Tree.EntryType.Vector:
                                case Tree.EntryType.Rotation:
                                case Tree.EntryType.ThisOperator:
                                    break;

                                default:
                                    throw new CompilerException(tree.SubTree[pos - 1].LineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidRightHandParameterTo0", "invalid right hand parameter to '{0}'"), ent));
                            }

                            /* left unary */
                            elem.Type = Tree.EntryType.OperatorLeftUnary;
                            elem.SubTree.Add(tree.SubTree[pos + 1]);
                            tree.SubTree.RemoveAt(pos + 1);
                        }
                    }
                    else if(ent == "+" || ent == "-")
                    {
                        bool hasValidLeftHand = false;
                        bool hasValidRightHand = false;

                        if (pos != 0)
                        {
                            switch (tree.SubTree[pos - 1].Type)
                            {
                                case Tree.EntryType.OperatorBinary:
                                    if (tree.SubTree[pos - 1].Entry == ".")
                                    {
                                        hasValidLeftHand = true;
                                    }
                                    break;

                                case Tree.EntryType.Declaration:
                                case Tree.EntryType.Function:
                                case Tree.EntryType.Level:
                                case Tree.EntryType.OperatorLeftUnary:
                                case Tree.EntryType.OperatorRightUnary:
                                case Tree.EntryType.Rotation:
                                case Tree.EntryType.CharValue:
                                case Tree.EntryType.StringValue:
                                case Tree.EntryType.Value:
                                case Tree.EntryType.Variable:
                                case Tree.EntryType.Vector:
                                case Tree.EntryType.ThisOperator:
                                    hasValidLeftHand = true;
                                    break;

                                default:
                                    break;
                            }
                        }

                        if (pos + 1 != tree.SubTree.Count)
                        {
                            switch (tree.SubTree[pos + 1].Type)
                            {
                                case Tree.EntryType.OperatorBinary:
                                    if (tree.SubTree[pos + 1].Entry == ".")
                                    {
                                        hasValidRightHand = true;
                                    }
                                    break;

                                case Tree.EntryType.OperatorLeftUnary:
                                case Tree.EntryType.OperatorRightUnary:
                                case Tree.EntryType.Function:
                                case Tree.EntryType.Declaration:
                                case Tree.EntryType.Level:
                                case Tree.EntryType.Value:
                                case Tree.EntryType.Variable:
                                case Tree.EntryType.CharValue:
                                case Tree.EntryType.StringValue:
                                case Tree.EntryType.Vector:
                                case Tree.EntryType.Rotation:
                                case Tree.EntryType.ThisOperator:
                                    hasValidRightHand = true;
                                    break;

                                default:
                                    break;
                            }
                        }

                        if (hasValidRightHand && hasValidLeftHand)
                        {
                            /* ignore */
                        }
                        else if (hasValidRightHand)
                        {
                            /* left unary */
                            elem.Type = Tree.EntryType.OperatorLeftUnary;
                            elem.SubTree.Add(tree.SubTree[pos + 1]);
                            tree.SubTree.RemoveAt(pos + 1);
                        }
                        else
                        {
                            int errorPos = pos + 1;
                            if(errorPos >= tree.SubTree.Count)
                            {
                                errorPos = pos;
                            }
                            throw new CompilerException(tree.SubTree[errorPos].LineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidRightHandParameterTo0", "invalid right hand parameter to '{0}'"), ent));
                        }
                    }
                }
            }
        }

        private readonly List<string> m_MulDivOps = new List<string>(new string[] { "*", "/", "%" });
        private readonly List<string> m_AddSubOps = new List<string>(new string[] { "+", "-" });
        private readonly List<string> m_BitwiseShiftOps = new List<string>(new string[] { "<<", ">>" });
        private readonly List<string> m_CompareOps = new List<string>(new string[] { "<", ">", "<=", ">=" });
        private readonly List<string> m_CompareEqualityOps = new List<string>(new string[] { "==", "!=" });
        private readonly List<string> m_BitwiseAndOps = new List<string>(new string[] { "&" });
        private readonly List<string> m_BitwiseXorOps = new List<string>(new string[] { "^" });
        private readonly List<string> m_BitwiseOrOps = new List<string>(new string[] { "|" });
        private readonly List<string> m_LogicalOps = new List<string>(new string[] { "&&", "||" });

        private void OrderOperators(CompileState cs, Tree tree, CultureInfo currentCulture)
        {
            OrderOperators_ElementSelector(tree, currentCulture);
            OrderOperators_IncsDecs(tree, currentCulture);
            OrderOperators_UnaryLefts(cs, tree, currentCulture);
            OrderOperators_Common_LtoR(tree, m_MulDivOps, currentCulture);
            OrderOperators_Common(tree, m_AddSubOps, currentCulture);
            OrderOperators_Common(tree, m_BitwiseShiftOps, currentCulture);
            OrderOperators_Common(tree, m_CompareOps, currentCulture);
            OrderOperators_Common(tree, m_CompareEqualityOps, currentCulture);
            OrderOperators_Common(tree, m_BitwiseAndOps, currentCulture);
            OrderOperators_Common(tree, m_BitwiseXorOps, currentCulture);
            OrderOperators_Common(tree, m_BitwiseOrOps, currentCulture);
            OrderOperators_Common(tree, m_LogicalOps, currentCulture);
            OrderOperators_Assignments(tree, currentCulture);
        }

        private void OrderBrackets_SeparateArguments(Tree resolvetree, string elemname, Tree.EntryType type, CultureInfo currentCulture)
        {
            var argBegin = new List<int>();
            var argEnd = new List<int>();
            int i;
            int n = resolvetree.SubTree.Count;
            bool paraLast = false;
            int lineNumber = 0;
            for(i = 0; i < n; ++i)
            {
                Tree st = resolvetree.SubTree[i];
                lineNumber = st.LineNumber;
                if(st.Type == Tree.EntryType.Separator)
                {
                    if(!paraLast)
                    {
                        throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "Missing0BeforeComma", "Missing {0} before ','"), elemname));
                    }
                    else
                    {
                        argEnd.Add(i);
                    }
                    paraLast = false;
                }
                else
                {
                    if(!paraLast)
                    {
                        argBegin.Add(i);
                    }
                    paraLast = true;
                }
            }
            if(!paraLast && n != 0)
            {
                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "Missing0AfterComma", "Missing {0} after ','"), elemname));
            }

            if (paraLast)
            {
                argEnd.Add(n);
            }

            if (argBegin.Count != argEnd.Count)
            {
                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "0Invalid", "{ 0} invalid"), elemname));
            }

            var arguments = new List<Tree>();
            n = argBegin.Count;
            for(i = 0; i < n; ++i)
            {
                var st = new Tree(resolvetree.SubTree[argBegin[i]].LineNumber)
                {
                    Type = type
                };
                st.SubTree.AddRange(resolvetree.SubTree.GetRange(argBegin[i], argEnd[i] - argBegin[i]));
                arguments.Add(st);
            }

            resolvetree.SubTree = arguments;
        }

        private void OrderBrackets(CompileState cs, Tree resolvetree, CultureInfo currentCulture)
        {
            if (cs.ILGen.HaveDebugOut)
            {
                cs.ILGen.WriteLine(string.Format("  //** Tree Flat Begin (Line {0})", resolvetree.LineNumber));
                foreach (Tree st in resolvetree.SubTree)
                {
                    cs.ILGen.WriteLine(string.Format("  //** {0}: {1}", st.Entry, st.Type.ToString()));
                }
                cs.ILGen.WriteLine("  //** Tree Flat End");
            }

            var parenStack = new List<KeyValuePair<string, int>>();
            int i = 0;

            while(i < resolvetree.SubTree.Count)
            {
                string v = resolvetree.SubTree[i].Entry;
                if(resolvetree.SubTree[i].Type == Tree.EntryType.StringValue ||
                    resolvetree.SubTree[i].Type == Tree.EntryType.CharValue)
                {
                    v = string.Empty; /* place holder to skip strings */
                }
                switch(v)
                {
                    case "(":
                    case "[":
                        parenStack.Insert(0, new KeyValuePair<string, int>(v, i));
                        resolvetree.SubTree[i].Type = Tree.EntryType.Level;
                        ++i;
                        break;

                    case "<":
                        if(resolvetree.SubTree[i].Type == Tree.EntryType.Declaration)
                        {
                            parenStack.Insert(0, new KeyValuePair<string, int>(v, i));
                        }
                        ++i;
                        break;

                    case ")":
                        if(parenStack.Count == 0)
                        {
                            throw new CompilerException(resolvetree.SubTree[i].LineNumber, this.GetLanguageString(currentCulture, "ClosingParenthesisDoesNotHavePreceedingOpeningParenthesis", "')' does not have preceeding '('"));
                        }
                        else if(parenStack[0].Key != "(")
                        {
                            throw new CompilerException(resolvetree.SubTree[i].LineNumber, string.Format(this.GetLanguageString(currentCulture, "ClosingParenthesisDoesMatchPreceeding0", "')' does not match preceeding '{0}'"), parenStack[0].Key));
                        }
                        else
                        {
                            int startPos = parenStack[0].Value;
                            parenStack.RemoveAt(0);
                            if(startPos + 1 != i)
                            {
                                resolvetree.SubTree[startPos].SubTree.AddRange(resolvetree.SubTree.GetRange(startPos + 1, i - startPos - 1));
                            }

                            if (startPos > 0 && resolvetree.SubTree[startPos - 1].Type == Tree.EntryType.Function)
                            {
                                resolvetree.SubTree[startPos - 1].SubTree = resolvetree.SubTree[startPos].SubTree;
                                resolvetree.SubTree.RemoveRange(startPos, i - startPos + 1);
                                OrderBrackets_SeparateArguments(
                                    resolvetree.SubTree[startPos - 1],
                                    "parameter",
                                    Tree.EntryType.FunctionArgument,
                                    currentCulture);
                                i = startPos;
                            }
                            else
                            {
                                resolvetree.SubTree.RemoveRange(startPos + 1, i - startPos);
                                i = startPos + 1;
                            }
                        }
                        break;

                    case ">":
                        if(resolvetree.SubTree[i].Type != Tree.EntryType.Declaration)
                        {
                            ++i;
                        }
                        else if (parenStack.Count == 0)
                        {
                            throw new CompilerException(resolvetree.SubTree[i].LineNumber, this.GetLanguageString(currentCulture, "ClosingGtDoesNotHavePreceedingLt", "'>' does not have preceeding '<'"));
                        }
                        else if (parenStack[0].Key != "<")
                        {
                            throw new CompilerException(resolvetree.SubTree[i].LineNumber, string.Format(this.GetLanguageString(currentCulture, "ClosingGtDoesNotMatchPreceeding0", "'>' does not match preceeding '{0}'"), parenStack[0].Key));
                        }
                        else
                        {
                            int startPos = parenStack[0].Value;
                            parenStack.RemoveAt(0);
                            if (startPos + 1 != i)
                            {
                                resolvetree.SubTree[startPos].SubTree.AddRange(resolvetree.SubTree.GetRange(startPos + 1, i - startPos - 1));
                            }
                            resolvetree.SubTree.RemoveRange(startPos + 1, i - startPos);

                            OrderBrackets_SeparateArguments(
                                resolvetree.SubTree[startPos],
                                this.GetLanguageString(currentCulture, "VectorRotationElement", "vector/rotation element"),
                                Tree.EntryType.DeclarationArgument,
                                currentCulture);
                            switch(resolvetree.SubTree[startPos].SubTree.Count)
                            {
                                case 3:
                                    resolvetree.SubTree[startPos].Type = Tree.EntryType.Vector;
                                    break;

                                case 4:
                                    resolvetree.SubTree[startPos].Type = Tree.EntryType.Rotation;
                                    break;

                                default:
                                    throw new CompilerException(resolvetree.SubTree[startPos].LineNumber, this.GetLanguageString(currentCulture, "InvalidNumberOfElementsInVectorRotationDeclaration", "invalid number of elements in vector/rotation declaration"));
                            }

                            i = startPos + 1;
                        }
                        break;

                    case "]":
                        if (parenStack.Count == 0)
                        {
                            throw new CompilerException(resolvetree.SubTree[i].LineNumber, this.GetLanguageString(currentCulture, "ClosingBracketDoesNotHavePreceedingOpeningBracket", "']' does not have preceeding '['"));
                        }
                        else if(parenStack[0].Key != "[")
                        {
                            throw new CompilerException(resolvetree.SubTree[i].LineNumber, string.Format(this.GetLanguageString(currentCulture, "ClosingBracketDoesMatchPreceeding0", "']' does not match preceeding '{0}'"), parenStack[0].Key));
                        }
                        else
                        {
                            int startPos = parenStack[0].Value;
                            parenStack.RemoveAt(0);
                            if (startPos + 1 != i)
                            {
                                resolvetree.SubTree[startPos].SubTree.AddRange(resolvetree.SubTree.GetRange(startPos + 1, i - startPos - 1));
                            }

                            if (startPos > 0 && resolvetree.SubTree[startPos - 1].Type == Tree.EntryType.Variable)
                            {
                                var thisOpTree = new Tree(resolvetree.SubTree[startPos -1].LineNumber);
                                Tree varTree = resolvetree.SubTree[startPos - 1];
                                thisOpTree.Type = Tree.EntryType.ThisOperator;
                                resolvetree.SubTree[startPos - 1] = thisOpTree;
                                resolvetree.SubTree[startPos - 1].SubTree = resolvetree.SubTree[startPos].SubTree;
                                resolvetree.SubTree.RemoveRange(startPos, i - startPos + 1);
                                OrderBrackets_SeparateArguments(
                                    thisOpTree,
                                    "parameter",
                                    Tree.EntryType.FunctionArgument,
                                    currentCulture);
                                thisOpTree.SubTree.Insert(0, varTree);

                                i = startPos;
                            }
                            else if(startPos > 0 && resolvetree.SubTree[startPos - 1].Type == Tree.EntryType.MemberName)
                            {
                                var thisOpTree = resolvetree.SubTree[startPos];
                                thisOpTree.Type = Tree.EntryType.ThisOperator;
                                resolvetree.SubTree.RemoveRange(startPos, i - startPos + 1);
                                OrderBrackets_SeparateArguments(
                                    thisOpTree,
                                    "parameter",
                                    Tree.EntryType.FunctionArgument,
                                    currentCulture);

                                int endPos = startPos - 1;
                                startPos = FindBeginOfElementSelectors(resolvetree, endPos, currentCulture);

                                int elemPos = startPos + 1;
                                while (elemPos <= endPos)
                                {
                                    Tree elem = resolvetree.SubTree[elemPos];
                                    elem.SubTree.Add(resolvetree.SubTree[elemPos - 1]);
                                    elem.SubTree.Add(resolvetree.SubTree[elemPos + 1]);
                                    elem.Type = Tree.EntryType.OperatorBinary;
                                    resolvetree.SubTree.RemoveAt(elemPos + 1);
                                    resolvetree.SubTree.RemoveAt(elemPos - 1);
                                    endPos -= 2;
                                }
                                thisOpTree.SubTree.Insert(0, resolvetree.SubTree[startPos]);
                                resolvetree.SubTree[startPos] = thisOpTree;
                                i = startPos + 1;
                            }
                            else
                            {
                                resolvetree.SubTree.RemoveRange(startPos + 1, i - startPos);

                                OrderBrackets_SeparateArguments(
                                    resolvetree.SubTree[startPos],
                                    this.GetLanguageString(currentCulture, "ListElement", "list element"),
                                    Tree.EntryType.DeclarationArgument,
                                    currentCulture);

                                i = startPos + 1;
                            }
                        }
                        break;

                    default:
                        ++i;
                        break;
                }
            }
        }

        #endregion

        private void SolveTree(CompileState cs, Tree resolvetree, CultureInfo currentCulture)
        {
            SolveMaxNegValues(resolvetree);
            SolveConstantOperations(cs, resolvetree, currentCulture, !cs.LanguageExtensions.EnableMemberFunctions);
        }

        #region Pre-Tree identifiers
        private void IdentifyParenLevel(CompileState cs, Tree resolvetree)
        {
            int parenLevel = 0;
            int n = resolvetree.SubTree.Count;
            var parenStack = new List<string>();
            for(int i = 0; i < n; ++i)
            {
                Tree st = resolvetree.SubTree[i];

                switch(st.Type)
                {
                    case Tree.EntryType.StringValue:
                    case Tree.EntryType.CharValue:
                    case Tree.EntryType.Value:
                        st.ParenLevel = parenLevel;
                        continue;
                }

                string ent = st.Entry;
                switch (ent)
                {
                    case "(":
                    case "[":
                        parenStack.Insert(0, ent);
                        st.ParenLevel = parenLevel++;
                        break;

                    case ")":
                        if (parenStack.Count == 0)
                        {
                            throw new CompilerException(st.LineNumber, this.GetLanguageString(cs.CurrentCulture, "ClosingParenthesisDoesNotHavePreceedingOpeningParenthesis", "')' does not have preceeding '('"));
                        }
                        else if(parenStack[0] != "(")
                        {
                            throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(cs.CurrentCulture, "ClosingParenthesisDoesNotMatchPreceeding0", "')' does not match preceeding '{0}'"), parenStack[0]));
                        }
                        parenStack.RemoveAt(0);
                        st.ParenLevel = --parenLevel;
                        break;

                    case "]":
                        if(parenStack.Count == 0)
                        {
                            throw new CompilerException(st.LineNumber, this.GetLanguageString(cs.CurrentCulture, "ClosingBracketDoesNotHavePreceedingOpeningBracket", "']' does not have preceeding '['"));
                        }
                        else if (parenStack[0] != "[")
                        {
                            throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(cs.CurrentCulture, "ClosingParenthesisDoesNotMatchPreceeding0", "']' does not match preceeding '{0}'"), parenStack[0]));
                        }
                        parenStack.RemoveAt(0);
                        st.ParenLevel = --parenLevel;
                        break;

                    default:
                        st.ParenLevel = parenLevel;
                        break;
                }
            }
        }

        private int IdentifyFindEndOfDeclaration(CompileState cs, Tree resolvetree, int startfrom)
        {
            int parenlevel = resolvetree.SubTree[startfrom].ParenLevel;
            int n = resolvetree.SubTree.Count;
            int endof = -1;
            bool haveSeparator = false;
            int lineNumber = 0;
            for (int i = startfrom; i < n; ++i)
            {
                int nparenlevel = resolvetree.SubTree[i].ParenLevel;
                lineNumber = resolvetree.SubTree[i].LineNumber;
                if (parenlevel > nparenlevel)
                {
                    /* stop at leaving level */
                    break;
                }
                else if (parenlevel != nparenlevel)
                {
                    continue;
                }
                if(resolvetree.SubTree[i].Entry == ">")
                {
                    endof = i;
                    break;
                }
                else if(resolvetree.SubTree[i].Entry == ",")
                {
                    haveSeparator = true;
                }
            }
            if(!haveSeparator)
            {
                return -1;
            }
            if(endof < 0)
            {
                throw new CompilerException(lineNumber, this.GetLanguageString(cs.CurrentCulture, "OpeningLtDoesNotHaveFollowingGt", "'<' does not have following '>'"));
            }
            return endof;
        }

        private void IdentifyDeclarations(CompileState cs, Tree resolvetree)
        {
            int n = resolvetree.SubTree.Count;
            int eod;
            int i = 0;
            while(i < n)
            {
                Tree st = resolvetree.SubTree[i];
                string ent = st.Entry;
                if (st.Type == Tree.EntryType.OperatorUnknown &&
                    st.Entry == "<")
                {
                    eod = IdentifyFindEndOfDeclaration(cs, resolvetree, i);
                    if (eod >= 0)
                    {
                        st.Type = Tree.EntryType.Declaration;
                        resolvetree.SubTree[eod].Type = Tree.EntryType.Declaration;
                        i = eod;
                    }
                }

                ++i;
            }
        }

        private void IdentifyNumericValues(Tree resolvetree)
        {
            int i = 0;
            while(i < resolvetree.SubTree.Count)
            {
                Tree st = resolvetree.SubTree[i];
                string ent = st.Entry;
                if(st.Type != Tree.EntryType.Unknown)
                {
                    ++i;
                    continue;
                }

                char c = ent[0];
                if (char.IsDigit(c) || (c == '.' && ent != "."))
                {
                    st.Type = Tree.EntryType.Value;
                }

                if(st.Type == Tree.EntryType.Value)
                {
                    /* let us find negation signs beforehand */
                    if (i == 1 &&
                        resolvetree.SubTree[i - 1].Entry == "-" &&
                        resolvetree.SubTree[i - 1].Type == Tree.EntryType.OperatorUnknown)
                    {
                        st.Entry = "-" + st.Entry;
                        resolvetree.SubTree.RemoveAt(--i);
                    }
                    else if(i > 1 &&
                        resolvetree.SubTree[i - 1].Entry == "-" &&
                        resolvetree.SubTree[i - 1].Type == Tree.EntryType.OperatorUnknown)
                    {
                        switch(resolvetree.SubTree[i - 2].Type)
                        {
                            case Tree.EntryType.Declaration:
                            case Tree.EntryType.Level:
                            case Tree.EntryType.OperatorBinary:
                            case Tree.EntryType.OperatorUnknown:
                                st.Entry = "-" + st.Entry;
                                resolvetree.SubTree.RemoveAt(i - 1);
                                --i;
                                break;

                            default:
                                break;
                        }
                    }
                    ++i;
                }
                else
                {
                    ++i;
                }
            }
        }

        private void IdentifyReservedWords(CompileState cs, Tree resolvetree)
        {
            int n = resolvetree.SubTree.Count;
            for(int i = 0; i < n; ++i)
            {
                Tree st = resolvetree.SubTree[i];
                string ent = st.Entry;
                if (st.Type != Tree.EntryType.Unknown)
                {
                    continue;
                }

                if(m_ReservedWords.Contains(ent) ||
                    (cs.ApiInfo.VariableTypes.Contains(ent)) ||
                    ((ent == "switch" || ent == "case" || ent == "break") && cs.LanguageExtensions.EnableSwitchBlock) ||
                    ((ent == "break" || ent == "continue") && cs.LanguageExtensions.EnableBreakContinueStatement))
                {
                    st.Type = Tree.EntryType.ReservedWord;
                }
            }
        }

        private void IdentifyOperators(CompileState cs, Tree resolvetree)
        {
            int n = resolvetree.SubTree.Count;
            for (int i = 0; i < n; ++i)
            {
                Tree st = resolvetree.SubTree[i];
                string ent = st.Entry;
                if (st.Type != Tree.EntryType.Unknown)
                {
                    continue;
                }

                char c = ent[0];
                if(c == '.' && ent != ".")
                {
                    /* ignore this */
                }
                else if(m_OpChars.Contains(c))
                {
                    st.Type = c == ',' ? Tree.EntryType.Separator : Tree.EntryType.OperatorUnknown;
                }
                else
                {
                    switch (ent)
                    {
                        case "(char)":
                            if (!cs.LanguageExtensions.EnableCharacterType)
                            {
                                goto default;
                            }
                            st.Type = Tree.EntryType.OperatorUnknown;
                            break;

                        case "(long)":
                            if(!cs.LanguageExtensions.EnableLongIntegers)
                            {
                                goto default;
                            }
                            st.Type = Tree.EntryType.OperatorUnknown;
                            break;

                        case "(list)":
                        case "(string)":
                        case "(key)":
                        case "(vector)":
                        case "(rotation)":
                        case "(quaternion)":
                        case "(integer)":
                        case "(float)":
                            st.Type = Tree.EntryType.OperatorUnknown;
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        private void IdentifyVariables(Tree resolvetree, ICollection<string> localVarNames)
        {
            int n = resolvetree.SubTree.Count;
            for (int i = 0; i < n; ++i)
            {
                Tree st = resolvetree.SubTree[i];
                string ent = st.Entry;
                if(st.Type != Tree.EntryType.Unknown)
                {
                    continue;
                }

                if(localVarNames.Contains(ent) &&
                   (0 == i || resolvetree.SubTree[i - 1].Entry != "."))
                {
                    st.Type = Tree.EntryType.Variable;
                }
            }
        }

        private void IdentifyMemberAccessors(Tree resolvetree)
        {
            int n = resolvetree.SubTree.Count;
            for (int i = 0; i < n; ++i)
            {
                Tree st = resolvetree.SubTree[i];
                string ent = st.Entry;
                if(st.Type != Tree.EntryType.Unknown)
                {
                    continue;
                }

                if(ent == "." && i + 1 < n)
                {
                    if (i + 2 < n && resolvetree.SubTree[i + 2].Entry == "(")
                    {
                        /* Member Function detected */
                    }
                    else
                    {
                        resolvetree.SubTree[i + 1].Type = Tree.EntryType.MemberName;
                    }
                }
            }
        }

        private void IdentifyFunctions(CompileState cs, Tree resolvetree, CultureInfo currentCulture)
        {
            int n = resolvetree.SubTree.Count;
            for (int i = 0; i < n; ++i)
            {
                Tree st = resolvetree.SubTree[i];
                string ent = st.Entry;
                if (st.Type != Tree.EntryType.Unknown)
                {
                    continue;
                }

                if (cs.m_Functions.ContainsKey(ent) ||
                    cs.ApiInfo.Methods.ContainsKey(ent) ||
                    cs.ApiInfo.InlineMethods.ContainsKey(ent) ||
                    ((cs.ApiInfo.MemberMethods.ContainsKey(ent) || cs.ApiInfo.InlineMemberMethods.ContainsKey(ent)) && resolvetree.SubTree[i + 1].Entry == "("))
                {
                    if(i + 1 >= n)
                    {
                        throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "Function0IsNotFollowedByOpeningParenthesis", "Function '{0}' is not followed by '('"), ent));
                    }
                    else if(resolvetree.SubTree[i + 1].Entry != "(")
                    {
                        throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidCallToFunction0", "Invalid call to function '{0}'"), ent));
                    }
                    else
                    {
                        st.Type = Tree.EntryType.Function;
                    }
                }
            }
        }
        #endregion

        private Tree LineToExpressionTree(CompileState cs, List<TokenInfo> expressionLine, ICollection<string> localVarNames, CultureInfo currentCulture, bool enableCommaSeparatedExpressions = false)
        {
            PreprocessLine(cs, expressionLine);
            var expressionTree = new Tree(expressionLine);
            IdentifyReservedWords(cs, expressionTree);
            IdentifyMemberAccessors(expressionTree);
            IdentifyVariables(expressionTree, localVarNames);
            IdentifyFunctions(cs, expressionTree, currentCulture);
            IdentifyOperators(cs, expressionTree);
            IdentifyNumericValues(expressionTree);
            IdentifyParenLevel(cs, expressionTree);
            IdentifyDeclarations(cs, expressionTree);

            int n = expressionTree.SubTree.Count;
            var groupedMsgs = new Dictionary<int, StringBuilder>();
            var groupedMsgBuilder = new StringBuilder();
            for(int i = 0; i < n; ++i)
            {
                Tree st = expressionTree.SubTree[i];
                if(st.Type == Tree.EntryType.Unknown &&
                    st.Entry != "(" && st.Entry != "[" &&
                    st.Entry != ")" && st.Entry != "]")
                {
                    string entry = st.Entry;
                    /* there should be no unknowns anymore */
                    if(i + 1 < n &&
                        expressionTree.SubTree[i + 1].Type == Tree.EntryType.Unknown &&
                        expressionTree.SubTree[i + 1].Entry == "(")
                    {
                        if(!groupedMsgs.TryGetValue(expressionTree.SubTree[i + 1].LineNumber, out groupedMsgBuilder))
                        {
                            groupedMsgBuilder = new StringBuilder();
                            groupedMsgs[expressionTree.SubTree[i + 1].LineNumber] = groupedMsgBuilder;
                        }
                        else
                        {
                            groupedMsgBuilder.Append("\n");
                        }
                        groupedMsgBuilder.AppendFormat(this.GetLanguageString(currentCulture, "NoFunction0Defined", "no function '{0}' defined"), entry);
                    }
                    else if(i > 0 &&
                        expressionTree.SubTree[i - 1].Type == Tree.EntryType.OperatorUnknown &&
                        expressionTree.SubTree[i - 1].Entry == ".")
                    {
                        /* element selector */
                    }
                    else
                    {
                        if (!groupedMsgs.TryGetValue(expressionTree.SubTree[i].LineNumber, out groupedMsgBuilder))
                        {
                            groupedMsgBuilder = new StringBuilder();
                            groupedMsgs[expressionTree.SubTree[i].LineNumber] = groupedMsgBuilder;
                        }
                        else
                        {
                            groupedMsgBuilder.Append("\n");
                        }
                        if (cs.ContainsValidVarType(entry))
                        {
                            groupedMsgBuilder.AppendFormat(this.GetLanguageString(currentCulture, "UsedType0InPlaceOfAVariable", "Used type '{0}' in place of a variable."), entry);
                        }
                        else if(!IsValidVarName(entry) || cs.IsReservedWord(entry))
                        {
                            groupedMsgBuilder.AppendFormat(this.GetLanguageString(currentCulture, "Used0InPlaceOfAVariable", "Used '{0}' in place of a variable."), entry);
                        }
                        else
                        {
                            groupedMsgBuilder.AppendFormat(this.GetLanguageString(currentCulture, "Variable0NotDefined", "no variable '{0}' defined"), entry);
                        }
                    }
                }
                st.Process(cs);
            }

            if(groupedMsgs.Count != 0)
            {
                var actdict = new Dictionary<int, string>();
                foreach(KeyValuePair<int, StringBuilder> kvp in groupedMsgs)
                {
                    actdict.Add(kvp.Key, kvp.Value.ToString());
                }
                throw new CompilerException(actdict);
            }

            /* After OrderBrackets only deep-scanners can be used */
            OrderBrackets(cs, expressionTree, currentCulture);

            OrderOperators(cs, expressionTree, currentCulture);

            SolveTree(cs, expressionTree, currentCulture);

            if(enableCommaSeparatedExpressions && expressionTree.SubTree.Count > 1 && expressionTree.SubTree.Count % 2 == 1)
            {
                for(int i = 0; i < expressionTree.SubTree.Count; ++i)
                {
                    string t = expressionTree.SubTree[i].Entry;
                    if (i % 2 == 1)
                    {
                        if(t != ",")
                        {
                            throw new CompilerException(expressionTree.SubTree[i].LineNumber, this.GetLanguageString(cs.CurrentCulture, "SyntaxError", "Syntax Error"));
                        }
                    }
                    else if(t == ",")
                    {
                        throw new CompilerException(expressionTree.SubTree[i].LineNumber, this.GetLanguageString(cs.CurrentCulture, "SyntaxError", "Syntax Error"));
                    }
                }

                for(int i = expressionTree.SubTree.Count - 2; i > 0; i -= 2)
                {
                    expressionTree.SubTree.RemoveAt(i);
                }
            }
            else if (expressionTree.SubTree.Count != 1)
            {
                if (expressionTree.SubTree.Count > 1)
                {
                    throw new CompilerException(expressionTree.SubTree[1].LineNumber, this.GetLanguageString(cs.CurrentCulture, "SyntaxError", "Syntax Error"));
                }
                else
                {
                    throw new CompilerException(expressionTree.LineNumber, this.GetLanguageString(cs.CurrentCulture, "SyntaxError", "Syntax Error"));

                }
            }
            IdentifyMemberFunctions(expressionTree);
            if (cs.LanguageExtensions.EnableMemberFunctions)
            {
                SolveConstantOperations(cs, expressionTree, currentCulture, true);
            }
            return expressionTree;
        }

        private void IdentifyMemberFunctions(Tree tree)
        {
            var enumeratorStack = new List<Tree>();
            enumeratorStack.Insert(0, tree);
            while (enumeratorStack.Count != 0)
            {
                tree = enumeratorStack[0];
                enumeratorStack.RemoveAt(0);

                int cnt = tree.SubTree.Count;
                for(int pos = 0; pos < cnt; ++pos)
                {
                    Tree subtree = tree.SubTree[pos];
                    if (subtree.Type == Tree.EntryType.OperatorBinary && subtree.Entry == "." && subtree.SubTree[1].Type == Tree.EntryType.Function)
                    {
                        Tree function = subtree.SubTree[1];
                        var funcArg = new Tree(function.LineNumber)
                        {
                            Type = Tree.EntryType.FunctionArgument,
                        };
                        funcArg.SubTree.Add(subtree.SubTree[0]);
                        funcArg.Value = subtree.SubTree[0].Value;
                        function.SubTree.Insert(0, funcArg);
                        tree.SubTree[pos] = function;
                        function.Type = Tree.EntryType.MemberFunction;
                        subtree = function;
                    }

                    if (subtree.Value != null)
                    {
                        /* skip solved parts */
                        continue;
                    }
                    switch(subtree.Type)
                    {
                        case Tree.EntryType.Declaration:
                        case Tree.EntryType.DeclarationArgument:
                        case Tree.EntryType.Function:
                        case Tree.EntryType.MemberFunction:
                        case Tree.EntryType.FunctionArgument:
                        case Tree.EntryType.Level:
                        case Tree.EntryType.LevelBegin:
                        case Tree.EntryType.OperatorBinary:
                        case Tree.EntryType.OperatorLeftUnary:
                        case Tree.EntryType.OperatorRightUnary:
                        case Tree.EntryType.ThisOperator:
                        case Tree.EntryType.Vector:
                            enumeratorStack.Add(subtree);
                            break;
                    }
                }
            }
        }
    }
}
