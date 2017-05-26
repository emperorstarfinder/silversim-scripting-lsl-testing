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
using SilverSim.Scripting.Lsl.Expression;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        #region LSL specific constant types
        private sealed class ConstantValueVector : Tree.ConstantValue
        {
            public Vector3 Value;

            public ConstantValueVector(Vector3 v)
            {
                Value = v;
            }

            public override string ToString() => TypecastVectorToString5Places(Value);

            public override Tree.ValueBase Negate() => new ConstantValueVector(-Value);
        }

        private sealed class ConstantValueRotation : Tree.ConstantValue
        {
            public Quaternion Value;

            public ConstantValueRotation(Quaternion v)
            {
                Value = v;
            }

            public override string ToString() => TypecastRotationToString6Places(Value);

            public override Tree.ValueBase Negate() => new ConstantValueRotation(-Value);
        }
        #endregion

        private void SolveConstantOperations(CompileState cs, Tree tree, int lineNumber, CultureInfo currentCulture)
        {
            var processNodes = new List<Tree>();
            var enumeratorStack = new List<ListTreeEnumState>();
            enumeratorStack.Insert(0, new ListTreeEnumState(tree));
            processNodes.Add(tree);
            while (enumeratorStack.Count != 0)
            {
                if (!enumeratorStack[0].MoveNext())
                {
                    enumeratorStack.RemoveAt(0);
                }
                else
                {
                    tree = enumeratorStack[0].Current;
                    processNodes.Insert(0, tree);
                    enumeratorStack.Add(new ListTreeEnumState(tree));
                }
            }

            foreach (Tree st in processNodes)
            {
                if (st.Entry != "<")
                {
                    /* intentionally left empty */
                }
                else if (st.Type == Tree.EntryType.Vector)
                {
                    if (st.SubTree[0].SubTree[0].Value != null &&
                        st.SubTree[1].SubTree[0].Value != null &&
                        st.SubTree[2].SubTree[0].Value != null)
                    {
                        var v = new double[3];
                        for (int idx = 0; idx < 3; ++idx)
                        {
                            Type t = st.SubTree[idx].SubTree[0].ValueType;
                            if (t == typeof(Tree.ConstantValueFloat))
                            {
                                v[idx] = ((Tree.ConstantValueFloat)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else if (t == typeof(Tree.ConstantValueInt))
                            {
                                v[idx] = ((Tree.ConstantValueInt)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, this.GetLanguageString(currentCulture, "ConstantVectorCannotContainOtherValuesThanFloatOrInt", "constant vector cannot contain other values than float or int"));
                            }
                        }

                        st.Value = new ConstantValueVector(new Vector3(v[0], v[1], v[2]));
                    }
                }
                else if (st.Type == Tree.EntryType.Rotation)
                {
                    if (st.SubTree[0].SubTree[0].Value != null &&
                        st.SubTree[1].SubTree[0].Value != null &&
                        st.SubTree[2].SubTree[0].Value != null &&
                        st.SubTree[3].SubTree[0].Value != null)
                    {
                        double[] v = new double[4];
                        for (int idx = 0; idx < 4; ++idx)
                        {
                            Type t = st.SubTree[idx].SubTree[0].ValueType;
                            if (t == typeof(Tree.ConstantValueFloat))
                            {
                                v[idx] = ((Tree.ConstantValueFloat)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else if (t == typeof(Tree.ConstantValueInt))
                            {
                                v[idx] = ((Tree.ConstantValueInt)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, "constant rotation cannot contain other values than float or int");
                            }
                        }

                        st.Value = new ConstantValueRotation(new Quaternion(v[0], v[1], v[2], v[3]));
                    }
                }

                #region Binary operators
                if (st.Type == Tree.EntryType.OperatorBinary)
                {
                    foreach (Tree ot in st.SubTree)
                    {
                        if (ot.Type == Tree.EntryType.Value && ot.Value == null)
                        {
                            ot.Process(cs, lineNumber);
                        }
                    }
                }

                if (st.Type == Tree.EntryType.OperatorBinary && st.SubTree[0].Value != null && st.SubTree[1].Value != null)
                {
                    Type leftType = st.SubTree[0].ValueType;
                    Type rightType;
                    switch (st.Entry)
                    {
                        case ".":
                            if (leftType == typeof(ConstantValueRotation))
                            {
                                switch (st.SubTree[1].Entry)
                                {
                                    case "x":
                                        st.Value = new Tree.ConstantValueFloat(((ConstantValueRotation)st.SubTree[0].Value).Value.X);
                                        break;
                                    case "y":
                                        st.Value = new Tree.ConstantValueFloat(((ConstantValueRotation)st.SubTree[0].Value).Value.Y);
                                        break;
                                    case "z":
                                        st.Value = new Tree.ConstantValueFloat(((ConstantValueRotation)st.SubTree[0].Value).Value.Z);
                                        break;
                                    case "s":
                                        st.Value = new Tree.ConstantValueFloat(((ConstantValueRotation)st.SubTree[0].Value).Value.W);
                                        break;

                                    default:
                                        break;
                                }
                            }
                            else if (leftType == typeof(ConstantValueVector))
                            {
                                switch (st.SubTree[1].Entry)
                                {
                                    case "x":
                                        st.Value = new Tree.ConstantValueFloat(((ConstantValueVector)st.SubTree[0].Value).Value.X);
                                        break;
                                    case "y":
                                        st.Value = new Tree.ConstantValueFloat(((ConstantValueVector)st.SubTree[0].Value).Value.Y);
                                        break;
                                    case "z":
                                        st.Value = new Tree.ConstantValueFloat(((ConstantValueVector)st.SubTree[0].Value).Value.Z);
                                        break;

                                    default:
                                        break;
                                }
                            }
                            break;

                        case "+":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(ConstantValueVector) && rightType == typeof(ConstantValueVector))
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value +
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueString) && rightType == typeof(Tree.ConstantValueString))
                            {
                                st.Value = new Tree.ConstantValueString(
                                    ((Tree.ConstantValueString)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueString)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(ConstantValueRotation) && rightType == typeof(ConstantValueRotation))
                            {
                                st.Value = new ConstantValueRotation(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value +
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case "-":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(ConstantValueVector) && rightType == typeof(ConstantValueVector))
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value -
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(ConstantValueRotation) && rightType == typeof(ConstantValueRotation))
                            {
                                st.Value = new ConstantValueRotation(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value -
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case "*":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(LSLCompiler.LSL_IntegerMultiply(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value,
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value));
                            }
                            else if (leftType == typeof(ConstantValueVector) && rightType == typeof(ConstantValueVector))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value.Dot(
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value));
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(ConstantValueVector))
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value *
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value);
                            }
                            else if (leftType == typeof(ConstantValueVector) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(ConstantValueVector))
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value);
                            }
                            else if (leftType == typeof(ConstantValueVector) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(ConstantValueRotation) && rightType == typeof(ConstantValueRotation))
                            {
                                st.Value = new ConstantValueRotation(
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value *
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value);
                            }
                            else if (leftType == typeof(ConstantValueVector) && rightType == typeof(ConstantValueRotation))
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value *
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case "/":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(LSLCompiler.LSL_IntegerDivision(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value,
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value));
                            }
                            else if (leftType == typeof(ConstantValueVector) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(ConstantValueVector) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(ConstantValueRotation) && rightType == typeof(ConstantValueRotation))
                            {
                                st.Value = new ConstantValueRotation(
                                    LSLQuaternionDivision(((ConstantValueRotation)(st.SubTree[0].Value)).Value,
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value));
                            }
                            else if (leftType == typeof(ConstantValueVector) && rightType == typeof(ConstantValueRotation))
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value /
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case "%":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(LSL_IntegerModulus(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value,
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value));
                            }
                            else if (leftType == typeof(ConstantValueVector) && rightType == typeof(ConstantValueVector))
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value %
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case "^":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value ^
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value ^
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                var a = (ulong)((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value;
                                var b = (ulong)((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value;
                                st.Value = new Tree.ConstantValueLong((long)(a ^ b));
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                var a = (ulong)((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value;
                                var b = (ulong)((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value;
                                st.Value = new Tree.ConstantValueLong((long)(a ^ b));
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case "<<":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <<
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value <<
                                    (int)((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    (long)((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <<
                                    (int)((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value <<
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case ">>":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >>
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value >>
                                    (int)((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    (long)((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >>
                                    (int)((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value >>
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case ">":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                if (cs.UsesSinglePrecision)
                                {
                                    var l = (float)((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value;
                                    var r = (float)((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value;
                                    st.Value = new Tree.ConstantValueInt(
                                        l > r ? 1 : 0);
                                }
                                else
                                {
                                    st.Value = new Tree.ConstantValueInt(
                                        ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >
                                        ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                                }
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case "<":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                if (cs.UsesSinglePrecision)
                                {
                                    var l = (float)((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value;
                                    var r = (float)((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value;
                                    st.Value = new Tree.ConstantValueInt(
                                        l <
                                        r ? 1 : 0);
                                }
                                else
                                {
                                    st.Value = new Tree.ConstantValueInt(
                                        ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <
                                        ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                                }
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case ">=":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                if (cs.UsesSinglePrecision)
                                {
                                    var l = (float)((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value;
                                    var r = (float)((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value;
                                    st.Value = new Tree.ConstantValueInt(
                                        l >=
                                        r ? 1 : 0);
                                }
                                else
                                {
                                    st.Value = new Tree.ConstantValueInt(
                                        ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >=
                                        ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                                }
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case "<=":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                if (cs.UsesSinglePrecision)
                                {
                                    var l = (float)((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value;
                                    var r = (float)((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value;
                                    st.Value = new Tree.ConstantValueInt(
                                        l <=
                                        r ? 1 : 0);
                                }
                                else
                                {
                                    st.Value = new Tree.ConstantValueInt(
                                        ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <=
                                        ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                                }
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case "!=":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                if (cs.UsesSinglePrecision)
                                {
                                    var l = (float)((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value;
                                    var r = (float)((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value;
                                    st.Value = new Tree.ConstantValueInt(
                                        l !=
                                        r ? 1 : 0);
                                }
                                else
                                {
                                    st.Value = new Tree.ConstantValueInt(
                                        ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value !=
                                        ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                                }
                            }
                            else if (leftType == typeof(ConstantValueVector) && rightType == typeof(ConstantValueVector))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value !=
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(ConstantValueRotation) && rightType == typeof(ConstantValueRotation))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value !=
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case "==":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueFloat) && rightType == typeof(Tree.ConstantValueFloat))
                            {
                                if (cs.UsesSinglePrecision)
                                {
                                    var l = (float)((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value;
                                    var r = (float)((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value;
                                    st.Value = new Tree.ConstantValueInt(
                                        l ==
                                        r ? 1 : 0);
                                }
                                else
                                {
                                    st.Value = new Tree.ConstantValueInt(
                                        ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value ==
                                        ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                                }
                            }
                            else if (leftType == typeof(ConstantValueVector) && rightType == typeof(ConstantValueVector))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value ==
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(ConstantValueRotation) && rightType == typeof(ConstantValueRotation))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value ==
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case "&":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value &
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value &
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value &
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value &
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case "|":
                            rightType = st.SubTree[1].ValueType;
                            if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value |
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                st.Value = new Tree.ConstantValueLong(
                                    ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value |
                                    ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueLong) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                var a = (ulong)((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value;
                                var b = (ulong)((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value;
                                st.Value = new Tree.ConstantValueLong((long)(a | b));
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueLong))
                            {
                                var a = (ulong)((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value;
                                var b = (ulong)((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value;
                                st.Value = new Tree.ConstantValueLong((long)(a | b));
                            }
                            else
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                            }
                            break;

                        case "&&":
                            {
                                bool isLeftTrue = false;
                                bool isRightTrue = false;
                                rightType = st.SubTree[1].ValueType;

                                if (leftType == typeof(Tree.ConstantValueInt))
                                {
                                    isLeftTrue = ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value != 0;
                                }
                                else if (leftType == typeof(Tree.ConstantValueLong))
                                {
                                    isLeftTrue = ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value != 0;
                                }
                                else if (leftType == typeof(Tree.ConstantValueFloat))
                                {
                                    isLeftTrue = Math.Abs(((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value) > Double.Epsilon;
                                }
                                else if (leftType == typeof(Tree.ConstantValueString))
                                {
                                    isLeftTrue = ((Tree.ConstantValueString)(st.SubTree[0].Value)).Value.Length != 0;
                                }
                                else if (leftType == typeof(ConstantValueVector))
                                {
                                    isLeftTrue = ((ConstantValueVector)(st.SubTree[0].Value)).Value.Length > Double.Epsilon;
                                }
                                else if (leftType == typeof(ConstantValueRotation))
                                {
                                    isLeftTrue = ((ConstantValueRotation)(st.SubTree[0].Value)).Value.IsLSLTrue;
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                                }

                                if (rightType == typeof(Tree.ConstantValueInt))
                                {
                                    isRightTrue = ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value != 0;
                                }
                                else if (rightType == typeof(Tree.ConstantValueLong))
                                {
                                    isRightTrue = ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value != 0;
                                }
                                else if (rightType == typeof(Tree.ConstantValueFloat))
                                {
                                    isRightTrue = Math.Abs(((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value) > Double.Epsilon;
                                }
                                else if (rightType == typeof(Tree.ConstantValueString))
                                {
                                    isRightTrue = ((Tree.ConstantValueString)(st.SubTree[1].Value)).Value.Length != 0;
                                }
                                else if (rightType == typeof(ConstantValueVector))
                                {
                                    isRightTrue = ((ConstantValueVector)(st.SubTree[1].Value)).Value.Length > Double.Epsilon;
                                }
                                else if (rightType == typeof(ConstantValueRotation))
                                {
                                    isRightTrue = ((ConstantValueRotation)(st.SubTree[1].Value)).Value.IsLSLTrue;
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                                }

                                st.Value = new Tree.ConstantValueInt((isLeftTrue && isRightTrue).ToLSLBoolean());
                            }
                            break;

                        case "||":
                            {
                                bool isLeftTrue = false;
                                bool isRightTrue = false;
                                rightType = st.SubTree[1].ValueType;

                                if (leftType == typeof(Tree.ConstantValueInt))
                                {
                                    isLeftTrue = ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value != 0;
                                }
                                else if (leftType == typeof(Tree.ConstantValueLong))
                                {
                                    isLeftTrue = ((Tree.ConstantValueLong)(st.SubTree[0].Value)).Value != 0;
                                }
                                else if (leftType == typeof(Tree.ConstantValueFloat))
                                {
                                    isLeftTrue = Math.Abs(((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value) > Double.Epsilon;
                                }
                                else if (leftType == typeof(Tree.ConstantValueString))
                                {
                                    isLeftTrue = ((Tree.ConstantValueString)(st.SubTree[0].Value)).Value.Length != 0;
                                }
                                else if (leftType == typeof(ConstantValueVector))
                                {
                                    isLeftTrue = ((ConstantValueVector)(st.SubTree[0].Value)).Value.Length > Double.Epsilon;
                                }
                                else if (leftType == typeof(ConstantValueRotation))
                                {
                                    isLeftTrue = ((ConstantValueRotation)(st.SubTree[0].Value)).Value.IsLSLTrue;
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                                }

                                if (rightType == typeof(Tree.ConstantValueInt))
                                {
                                    isRightTrue = ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value != 0;
                                }
                                else if (rightType == typeof(Tree.ConstantValueLong))
                                {
                                    isRightTrue = ((Tree.ConstantValueLong)(st.SubTree[1].Value)).Value != 0;
                                }
                                else if (rightType == typeof(Tree.ConstantValueFloat))
                                {
                                    isRightTrue = Math.Abs(((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value) > Double.Epsilon;
                                }
                                else if (rightType == typeof(Tree.ConstantValueString))
                                {
                                    isRightTrue = ((Tree.ConstantValueString)(st.SubTree[1].Value)).Value.Length != 0;
                                }
                                else if (rightType == typeof(ConstantValueVector))
                                {
                                    isRightTrue = ((ConstantValueVector)(st.SubTree[1].Value)).Value.Length > Double.Epsilon;
                                }
                                else if (rightType == typeof(ConstantValueRotation))
                                {
                                    isRightTrue = ((ConstantValueRotation)(st.SubTree[1].Value)).Value.IsLSLTrue;
                                }
                                else
                                {
                                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                                }

                                st.Value = new Tree.ConstantValueInt((isLeftTrue || isRightTrue).ToLSLBoolean());
                            }
                            break;

                        default:
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OperatorIsUnknown", "Cannot process '{0}': operator is unknown"), st.Entry));
                    }
                }
                #endregion
                #region Left unary operators
                else if (st.Type == Tree.EntryType.OperatorLeftUnary && (st.SubTree[0].Value != null || st.SubTree[0].Type == Tree.EntryType.Value))
                {
                    if (st.Entry != "-" && st.SubTree[0].Type == Tree.EntryType.Value)
                    {
                        st.Process(cs, lineNumber);
                    }
                    if (st.Entry == "+")
                    {
                        st.Value = st.SubTree[0].Value;
                    }
                    else if (st.Entry == "-")
                    {
                        if (st.SubTree[0].Value == null)
                        {
                            st.SubTree[0].Process(cs, lineNumber);
                        }
                        if (st.Value == null)
                        {
                            st.Value = st.SubTree[0].Value.Negate();
                        }
                    }
                    else if (st.Entry == "~")
                    {
                        Type type = st.ValueType;
                        if (type == typeof(Tree.ConstantValueFloat))
                        {
                            throw new CompilerException(lineNumber, this.GetLanguageString(currentCulture, "FloatCannotBeBinaryNegated", "float cannot be binary-negated"));
                        }
                        else if (type == typeof(Tree.ConstantValueInt))
                        {
                            st.Value = new Tree.ConstantValueInt(~((Tree.ConstantValueInt)(st.Value)).Value);
                        }
                        else if (type == typeof(Tree.ConstantValueLong))
                        {
                            st.Value = new Tree.ConstantValueLong(~((Tree.ConstantValueLong)(st.Value)).Value);
                        }
                        else if (st.Value is Tree.ConstantValueString)
                        {
                            throw new CompilerException(lineNumber, this.GetLanguageString(currentCulture, "StringCannotBeBinaryNegated", "string cannot be binary negated"));
                        }
                    }
                    else if (st.Entry == "!")
                    {
                        Type type = st.ValueType;
                        if (type == typeof(Tree.ConstantValueFloat))
                        {
                            throw new CompilerException(lineNumber, this.GetLanguageString(currentCulture, "FloatCannotBeLogicallyNegated", "float cannot be logically negated"));
                        }
                        else if (type == typeof(Tree.ConstantValueInt))
                        {
                            st.Value = new Tree.ConstantValueInt(((Tree.ConstantValueInt)(st.Value)).Value == 0 ? 1 : 0);
                        }
                        else if (type == typeof(Tree.ConstantValueLong))
                        {
                            st.Value = new Tree.ConstantValueInt(~((Tree.ConstantValueLong)(st.Value)).Value == 0 ? 1 : 0);
                        }
                        else if (type == typeof(Tree.ConstantValueString))
                        {
                            throw new CompilerException(lineNumber, this.GetLanguageString(currentCulture, "StringCannotBeLogicallyNegated", "string cannot be logically negated"));
                        }
                    }
                    else if (st.SubTree[0].Value != null)
                    {
                        Type type = st.SubTree[0].ValueType;
                        switch (st.Entry)
                        {
                            case "(string)":
                                if (type == typeof(ConstantValueRotation))
                                {
                                    st.Value = new Tree.ConstantValueString(
                                        cs.UsesSinglePrecision ?
                                        SinglePrecision.TypecastRotationToString5Places(((ConstantValueRotation)st.SubTree[0].Value).Value) :
                                        TypecastRotationToString5Places(((ConstantValueRotation)st.SubTree[0].Value).Value));
                                }
                                else if (type == typeof(ConstantValueVector))
                                {
                                    st.Value = new Tree.ConstantValueString(
                                        cs.UsesSinglePrecision ?
                                        SinglePrecision.TypecastVectorToString5Places(((ConstantValueVector)st.SubTree[0].Value).Value) :
                                        TypecastVectorToString5Places(((ConstantValueVector)st.SubTree[0].Value).Value));
                                }
                                else if (type == typeof(Tree.ConstantValueFloat))
                                {
                                    st.Value = new Tree.ConstantValueString(
                                        cs.UsesSinglePrecision ?
                                        SinglePrecision.TypecastFloatToString(((Tree.ConstantValueFloat)st.SubTree[0].Value).Value) :
                                        TypecastDoubleToString(((Tree.ConstantValueFloat)st.SubTree[0].Value).Value));
                                }
                                else if (type == typeof(Tree.ConstantValueInt))
                                {
                                    st.Value = new Tree.ConstantValueString(((Tree.ConstantValueInt)st.SubTree[0].Value).ToString());
                                }
                                else if (type == typeof(Tree.ConstantValueLong))
                                {
                                    st.Value = new Tree.ConstantValueString(((Tree.ConstantValueLong)st.SubTree[0].Value).ToString());
                                }
                                else if (type == typeof(Tree.ConstantValueString))
                                {
                                    st.Value = st.SubTree[0].Value;
                                }
                                else if (st.SubTree[0].Type == Tree.EntryType.Level && st.SubTree[0].Entry == "[")
                                {
                                    /* check if all parts are constants */
                                    bool isConstant = true;
                                    foreach (Tree sst in st.SubTree[0].SubTree)
                                    {
                                        if (sst.Value == null)
                                        {
                                            isConstant = false;
                                        }
                                    }

                                    if (isConstant)
                                    {
                                        var o = new StringBuilder();
                                        foreach (Tree sst in st.SubTree[0].SubTree)
                                        {
                                            o.Append(sst.Value.ToString());
                                        }
                                        st.Value = new Tree.ConstantValueString(o.ToString());
                                    }
                                }
                                break;

                            case "(rotation)":
                            case "(quaternion)":
                                if (type == typeof(ConstantValueRotation))
                                {
                                    st.Value = st.SubTree[0].Value;
                                }
                                else if (type == typeof(Tree.ConstantValueString))
                                {
                                    st.Value = new ConstantValueRotation(ParseStringToQuaternion(((Tree.ConstantValueString)st.SubTree[0].Value).Value));
                                }
                                break;

                            case "(vector)":
                                if (type == typeof(ConstantValueVector))
                                {
                                    st.Value = st.SubTree[0].Value;
                                }
                                else if (type == typeof(Tree.ConstantValueString))
                                {
                                    st.Value = new ConstantValueVector(ParseStringToVector(((Tree.ConstantValueString)st.SubTree[0].Value).Value));
                                }
                                break;

                            case "(long)":
                                if (type == typeof(Tree.ConstantValueInt))
                                {
                                    st.Value = new Tree.ConstantValueLong(((Tree.ConstantValueInt)st.SubTree[0].Value).Value);
                                }
                                else if (type == typeof(Tree.ConstantValueLong))
                                {
                                    st.Value = st.SubTree[0].Value;
                                }
                                else if (type == typeof(Tree.ConstantValueFloat))
                                {
                                    st.Value = new Tree.ConstantValueLong(ConvToLong(((Tree.ConstantValueFloat)st.SubTree[0].Value).Value));
                                }
                                else if (type == typeof(Tree.ConstantValueString))
                                {
                                    try
                                    {
                                        st.Value = new Tree.ConstantValueLong(ConvToLong(((Tree.ConstantValueString)st.SubTree[0].Value).Value));
                                    }
                                    catch
                                    {
                                        st.Value = new Tree.ConstantValueInt(0);
                                    }
                                }
                                break;

                            case "(integer)":
                                if (type == typeof(Tree.ConstantValueInt))
                                {
                                    st.Value = st.SubTree[0].Value;
                                }
                                else if (type == typeof(Tree.ConstantValueLong))
                                {
                                    st.Value = new Tree.ConstantValueLong(ConvToInt(((Tree.ConstantValueLong)st.SubTree[0].Value).Value));
                                }
                                else if (type == typeof(Tree.ConstantValueFloat))
                                {
                                    st.Value = new Tree.ConstantValueInt(ConvToInt(((Tree.ConstantValueFloat)st.SubTree[0].Value).Value));
                                }
                                else if (type == typeof(Tree.ConstantValueString))
                                {
                                    try
                                    {
                                        st.Value = new Tree.ConstantValueInt(ConvToInt(((Tree.ConstantValueString)st.SubTree[0].Value).Value));
                                    }
                                    catch
                                    {
                                        st.Value = new Tree.ConstantValueInt(0);
                                    }
                                }
                                break;

                            case "(float)":
                                if (type == typeof(Tree.ConstantValueFloat))
                                {
                                    st.Value = st.SubTree[0].Value;
                                }
                                else if (type == typeof(Tree.ConstantValueInt))
                                {
                                    st.Value = new Tree.ConstantValueFloat(((Tree.ConstantValueInt)st.SubTree[0].Value).Value);
                                }
                                else if (type == typeof(Tree.ConstantValueLong))
                                {
                                    st.Value = new Tree.ConstantValueFloat(((Tree.ConstantValueLong)st.SubTree[0].Value).Value);
                                }
                                else if (type == typeof(Tree.ConstantValueString))
                                {
                                    try
                                    {
                                        st.Value = new Tree.ConstantValueFloat(ParseStringToDouble(((Tree.ConstantValueString)st.SubTree[0].Value).Value));
                                    }
                                    catch
                                    {
                                        st.Value = new Tree.ConstantValueFloat(0);
                                    }
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
                #endregion
                #region Parenthesis
                else if (st.Type == Tree.EntryType.Level && st.Entry == "(" && st.SubTree.Count == 1)
                {
                    st.Value = st.SubTree[0].Value;
                }
                #endregion
            }
        }

        private sealed class ListTreeEnumState
        {
            public int Position = -1;
            public Tree Tree;

            public ListTreeEnumState(Tree tree)
            {
                Tree = tree;
            }

            public bool MoveNext()
            {
                if (Position >= Tree.SubTree.Count)
                {
                    return false;
                }
                return ++Position < Tree.SubTree.Count;
            }

            public Tree Current => Tree.SubTree[Position];
        }

        private sealed class ListTreeEnumReverseState
        {
            public int Position;
            public Tree Tree;

            public ListTreeEnumReverseState(Tree tree)
            {
                Tree = tree;
                Position = tree.SubTree.Count;
            }

            public bool MoveNext()
            {
                if (Position < 0)
                {
                    return false;
                }
                return --Position >= 0;
            }

            public Tree Current => Tree.SubTree[Position];
        }

        private void SolveMaxNegValues(Tree resolvetree)
        {
            var enumeratorStack = new List<ListTreeEnumState>();
            enumeratorStack.Insert(0, new ListTreeEnumState(resolvetree));
            while (enumeratorStack.Count != 0)
            {
                if (!enumeratorStack[0].MoveNext())
                {
                    enumeratorStack.RemoveAt(0);
                }
                else
                {
                    resolvetree = enumeratorStack[0].Current;
                    if (resolvetree.Type == Tree.EntryType.OperatorLeftUnary && resolvetree.Entry == "-" &&
                        resolvetree.SubTree.Count == 1 && resolvetree.SubTree[0].Entry == "2147483648" && resolvetree.SubTree[0].Type == Tree.EntryType.Value)
                    {
                        resolvetree.Value = new Tree.ConstantValueInt(-2147483648);
                    }
                    else if (resolvetree.Entry == "2147483648" && resolvetree.Type == Tree.EntryType.Value)
                    {
                        resolvetree.Value = new Tree.ConstantValueFloat(2147483648f);
                    }
                    else
                    {
                        enumeratorStack.Insert(0, new ListTreeEnumState(resolvetree));
                    }
                }
            }
        }

        #region Order Tree according to definitions
        private void OrderOperators_ElementSelector(Tree tree, int lineNumber, CultureInfo currentCulture)
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
                        throw new CompilerException(lineNumber, this.GetLanguageString(currentCulture, "ElementSelectorNeedsAVectorOrRotationToSelectSomething", "element selector needs a vector or rotation to select something"));
                    }
                    if (pos + 1 >= tree.SubTree.Count)
                    {
                        throw new CompilerException(lineNumber, this.GetLanguageString(currentCulture, "ElementSelectorNeedsASelector", "element selector needs a selector"));
                    }

                    switch (tree.SubTree[pos - 1].Type)
                    {
                        case Tree.EntryType.Variable:
                        case Tree.EntryType.Declaration:
                        case Tree.EntryType.Function:
                        case Tree.EntryType.Vector:
                        case Tree.EntryType.Rotation:
                        case Tree.EntryType.ThisOperator:
                            break;

                        default:
                            throw new CompilerException(lineNumber, this.GetLanguageString(currentCulture, "ElementSelectorRequiresVariableDeclarationOrAFunctionReturnValue", "element selector requires variable, declaration or a function with a return value"));
                    }

                    /*
                    switch (tree.SubTree[pos + 1].Entry)
                    {
                        case "x":
                        case "y":
                        case "z":
                        case "s":
                            break;

                        default:
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidElementSelector0", "invalid element selector '{0}'"), tree.SubTree[pos + 1].Entry));
                    }*/

                    enumeratorStack.Add(tree.SubTree[pos - 1]);
                    elem.SubTree.Add(tree.SubTree[pos - 1]);
                    elem.SubTree.Add(tree.SubTree[pos + 1]);
                    elem.Type = Tree.EntryType.OperatorBinary;
                    tree.SubTree.RemoveAt(pos + 1);
                    tree.SubTree.RemoveAt(--pos);
                }
            }
        }

        private void OrderOperators_IncsDecs(Tree tree, int lineNumber, CultureInfo currentCulture)
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
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "0NeedsVariableBeforeDot", "'{0}' needs a variable before '.'."), elem.Entry));
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
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "0NeedsVariableBeforeDot", "'{0}' needs a variable before '.'."), elem.Entry));
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

        private void OrderOperators_Common(Tree tree, List<string> operators, int lineNumber, CultureInfo currentCulture)
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
                        throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "MissingLValueTo0", "missing l-value to '{0}'"), ent));
                    }
                    else if(pos + 1 >= tree.SubTree.Count)
                    {
                        throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "MissingRValueTo0", "missing r-value to '{0}'"), ent));
                    }

                    switch(tree.SubTree[pos - 1].Type)
                    {
                        case Tree.EntryType.Variable:
                        case Tree.EntryType.Value:
                        case Tree.EntryType.StringValue:
                        case Tree.EntryType.Function:
                        case Tree.EntryType.Declaration:
                        case Tree.EntryType.Level:
                        case Tree.EntryType.OperatorBinary:
                        case Tree.EntryType.OperatorLeftUnary:
                        case Tree.EntryType.OperatorRightUnary:
                        case Tree.EntryType.Vector:
                        case Tree.EntryType.Rotation:
                            break;

                        default:
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidLValueTo0", "invalid l-value to '{0}'"), ent));
                    }

                    switch (tree.SubTree[pos + 1].Type)
                    {
                        case Tree.EntryType.Variable:
                        case Tree.EntryType.Value:
                        case Tree.EntryType.StringValue:
                        case Tree.EntryType.Function:
                        case Tree.EntryType.Declaration:
                        case Tree.EntryType.Level:
                        case Tree.EntryType.OperatorBinary:
                        case Tree.EntryType.OperatorLeftUnary:
                        case Tree.EntryType.OperatorRightUnary:
                        case Tree.EntryType.Vector:
                        case Tree.EntryType.Rotation:
                            break;

                        default:
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidRValueTo0", "invalid r-value to '{0}'"), ent));
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

        private readonly List<string> m_AssignmentOps = new List<string>(new string[] { "=", "+=", "-=", "/=", "%=", "*=" });
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

            if(tree.Type == Tree.EntryType.Variable ||
                (tree.Type == Tree.EntryType.OperatorBinary && tree.Entry == "."))
            {
                variable = tree;
                return true;
            }
            else
            {
                return false;
            }
        }

        private void OrderOperators_Assignments(Tree tree, int lineNumber, CultureInfo currentCulture)
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
                        throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "MissingLValueTo0", "missing l-value to '{0}'"), ent));
                    }
                    else if (pos + 1 >= tree.SubTree.Count)
                    {
                        throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "MissingRValueTo0", "missing r-value to '{0}'"), ent));
                    }

                    switch (tree.SubTree[pos - 1].Type)
                    {
                        case Tree.EntryType.Variable:
                            break;

                        case Tree.EntryType.OperatorBinary:
                            if(tree.SubTree[pos - 1].Entry != "." ||
                                tree.SubTree[pos - 1].SubTree[0].Type != Tree.EntryType.Variable)
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidLValueTo0", "invalid l-value to '{0}'"), ent));
                            }
                            break;

                        case Tree.EntryType.OperatorLeftUnary:
                            if(!((tree.SubTree[pos - 1].Entry == "~" || tree.SubTree[pos - 1].Entry == "!") &&
                                Assignments_ExtractLeftUnaryOnLValue(tree.SubTree[pos - 1], out startlvalueunarytree, out endlvalueunarytree, out variable)))
                            {
                                throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidLValueTo0", "invalid l-value to '{0}'"), ent));
                            }
                            break;

                        default:
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidLValueTo0", "invalid l-value to '{0}'"), ent));
                    }

                    switch (tree.SubTree[pos + 1].Type)
                    {
                        case Tree.EntryType.Variable:
                        case Tree.EntryType.Value:
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
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidRValueTo0", "invalid r-value to '{0}'"), ent));
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

        private void OrderOperators_UnaryLefts(CompileState cs, Tree tree, int lineNumber, CultureInfo currentCulture)
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
                                enumeratorStack.Add(elem);
                                break;

                            default:
                                break;
                        }
                        continue;
                    }

                    if (ent == "!" || ent == "~" || m_TypecastOperators.Contains(ent) ||
                        (cs.LanguageExtensions.EnableLongIntegers && ent == "(long)"))
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
                                case Tree.EntryType.StringValue:
                                case Tree.EntryType.Vector:
                                case Tree.EntryType.Rotation:
                                    break;

                                default:
                                    throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidRightHandParameterTo0", "invalid right hand parameter to '{0}'"), ent));
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
                                case Tree.EntryType.StringValue:
                                case Tree.EntryType.Value:
                                case Tree.EntryType.Variable:
                                case Tree.EntryType.Vector:
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
                                case Tree.EntryType.StringValue:
                                case Tree.EntryType.Vector:
                                case Tree.EntryType.Rotation:
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
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidRightHandParameterTo0", "invalid right hand parameter to '{0}'"), ent));
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

        private void OrderOperators(CompileState cs, Tree tree, int lineNumber, CultureInfo currentCulture)
        {
            OrderOperators_ElementSelector(tree, lineNumber, currentCulture);
            OrderOperators_IncsDecs(tree, lineNumber, currentCulture);
            OrderOperators_UnaryLefts(cs, tree, lineNumber, currentCulture);
            OrderOperators_Common(tree, m_MulDivOps, lineNumber, currentCulture);
            OrderOperators_Common(tree, m_AddSubOps, lineNumber, currentCulture);
            OrderOperators_Common(tree, m_BitwiseShiftOps, lineNumber, currentCulture);
            OrderOperators_Common(tree, m_CompareOps, lineNumber, currentCulture);
            OrderOperators_Common(tree, m_CompareEqualityOps, lineNumber, currentCulture);
            OrderOperators_Common(tree, m_BitwiseAndOps, lineNumber, currentCulture);
            OrderOperators_Common(tree, m_BitwiseXorOps, lineNumber, currentCulture);
            OrderOperators_Common(tree, m_BitwiseOrOps, lineNumber, currentCulture);
            OrderOperators_Common(tree, m_LogicalOps, lineNumber, currentCulture);
            OrderOperators_Assignments(tree, lineNumber, currentCulture);
        }

        private void OrderBrackets_SeparateArguments(Tree resolvetree, string elemname, Tree.EntryType type, int lineNumber, CultureInfo currentCulture)
        {
            var argBegin = new List<int>();
            var argEnd = new List<int>();
            int i;
            int n = resolvetree.SubTree.Count;
            bool paraLast = false;
            for(i = 0; i < n; ++i)
            {
                Tree st = resolvetree.SubTree[i];
                if(st.Type == Tree.EntryType.Separator)
                {
                    if(!paraLast)
                    {
                        throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "Missing0BeforeComma", "Missing {0} before ','"), elemname));
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
                var st = new Tree()
                {
                    Type = type
                };
                st.SubTree.AddRange(resolvetree.SubTree.GetRange(argBegin[i], argEnd[i] - argBegin[i]));
                arguments.Add(st);
            }

            resolvetree.SubTree = arguments;
        }

        private void OrderBrackets(CompileState cs, Tree resolvetree, int lineNumber, CultureInfo currentCulture)
        {
#if DEBUG
            cs.ILGen.Writer.WriteLine(string.Format("  //** Tree Flat Begin (Line {0})", lineNumber));
            foreach (Tree st in resolvetree.SubTree)
            {
                cs.ILGen.Writer.WriteLine(string.Format("  //** {0}: {1}", st.Entry, st.Type.ToString()));
            }
            cs.ILGen.Writer.WriteLine("  //** Tree Flat End");
#endif
            var parenStack = new List<KeyValuePair<string, int>>();
            int i = 0;

            while(i < resolvetree.SubTree.Count)
            {
                string v = resolvetree.SubTree[i].Entry;
                if(resolvetree.SubTree[i].Type == Tree.EntryType.StringValue)
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
                            throw new CompilerException(lineNumber, this.GetLanguageString(currentCulture, "ClosingParenthesisDoesNotHavePreceedingOpeningParenthesis", "')' does not have preceeding '('"));
                        }
                        else if(parenStack[0].Key != "(")
                        {
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "ClosingParenthesisDoesMatchPreceeding0", "')' does not match preceeding '{0}'"), parenStack[0].Key));
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
                                    lineNumber,
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
                            throw new CompilerException(lineNumber, this.GetLanguageString(currentCulture, "ClosingGtDoesNotHavePreceedingLt", "'>' does not have preceeding '<'"));
                        }
                        else if (parenStack[0].Key != "<")
                        {
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "ClosingGtDoesMatchPreceeding0", "'>' does not match preceeding '{0}'"), parenStack[0].Key));
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
                                lineNumber,
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
                                    throw new CompilerException(lineNumber, this.GetLanguageString(currentCulture, "InvalidNumberOfElementsInVectorRotationDeclaration", "invalid number of elements in vector/rotation declaration"));
                            }

                            i = startPos + 1;
                        }
                        break;

                    case "]":
                        if (parenStack.Count == 0)
                        {
                            throw new CompilerException(lineNumber, this.GetLanguageString(currentCulture, "ClosingBracketDoesNotHavePreceedingOpeningBracket", "']' does not have preceeding '['"));
                        }
                        else if(parenStack[0].Key != "[")
                        {
                            throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "ClosingBracketDoesMatchPreceeding0", "']' does not match preceeding '{0}'"), parenStack[0].Key));
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
                                var thisOpTree = new Tree();
                                Tree varTree = resolvetree.SubTree[startPos - 1];
                                thisOpTree.Type = Tree.EntryType.ThisOperator;
                                resolvetree.SubTree[startPos - 1] = thisOpTree;
                                resolvetree.SubTree[startPos - 1].SubTree = resolvetree.SubTree[startPos].SubTree;
                                resolvetree.SubTree.RemoveRange(startPos, i - startPos + 1);
                                OrderBrackets_SeparateArguments(
                                    resolvetree.SubTree[startPos - 1],
                                    "parameter",
                                    Tree.EntryType.FunctionArgument,
                                    lineNumber,
                                    currentCulture);
                                thisOpTree.SubTree.Insert(0, varTree);

                                i = startPos;
                            }
                            else
                            {
                                resolvetree.SubTree.RemoveRange(startPos + 1, i - startPos);

                                OrderBrackets_SeparateArguments(
                                    resolvetree.SubTree[startPos],
                                    this.GetLanguageString(currentCulture, "ListElement", "list element"),
                                    Tree.EntryType.DeclarationArgument,
                                    lineNumber,
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

        private void SolveTree(CompileState cs, Tree resolvetree, int lineNumber, CultureInfo currentCulture)
        {
            SolveMaxNegValues(resolvetree);
            SolveConstantOperations(cs, resolvetree, lineNumber, currentCulture);
        }

        #region Pre-Tree identifiers
        private void IdentifyDeclarations(Tree resolvetree)
        {
            int n = resolvetree.SubTree.Count;
            for (int i = 0; i < n; ++i)
            {
                Tree st = resolvetree.SubTree[i];
                string ent = st.Entry;
                if (st.Type != Tree.EntryType.OperatorUnknown)
                {
                    continue;
                }

                switch (ent)
                {
                    case "<":
                        if (0 == i ||
                            (resolvetree.SubTree[i - 1].Type == Tree.EntryType.OperatorUnknown &&
                            resolvetree.SubTree[i - 1].Entry != "++" &&
                            resolvetree.SubTree[i - 1].Entry != "--") ||
                            resolvetree.SubTree[i - 1].Type == Tree.EntryType.Separator)
                        {
                            st.Type = Tree.EntryType.Declaration;
                        }
                        else
                        {
                            switch (resolvetree.SubTree[i - 1].Entry)
                            {
                                case "[":
                                case "(":
                                    if (resolvetree.SubTree[i - 1].Type != Tree.EntryType.StringValue)
                                    {
                                        st.Type = Tree.EntryType.Declaration;
                                    }
                                    break;

                                default:
                                    break;
                            }
                        }
                        break;
                    case ">":
                        if (i + 1 == n ||
                            resolvetree.SubTree[i + 1].Type == Tree.EntryType.Separator)
                        {
                            st.Type = Tree.EntryType.Declaration;
                        }
                        else if(resolvetree.SubTree[i + 1].Type == Tree.EntryType.OperatorUnknown)
                        {
                            switch(resolvetree.SubTree[i + 1].Entry)
                            {
                                case "-":
                                    if(i + 2 < n)
                                    {
                                        switch(resolvetree.SubTree[i + 2].Type)
                                        {
                                            case Tree.EntryType.Variable:
                                            case Tree.EntryType.Value:
                                            case Tree.EntryType.Function:
                                                break;

                                            default:
                                                st.Type = Tree.EntryType.Declaration;
                                                break;
                                        }
                                    }
                                    break;

                                case "+":
                                case "*":
                                case "/":
                                case "%":
                                case "==":
                                case "!=":
                                case "||":
                                case "&&":
                                    st.Type = Tree.EntryType.Declaration;
                                    break;

                                default:
                                    break;
                            }
                        }
                        else
                        {
                            switch(resolvetree.SubTree[i + 1].Entry)
                            {
                                case "]":
                                case ")":
                                    if (resolvetree.SubTree[i + 1].Type != Tree.EntryType.StringValue)
                                    {
                                        st.Type = Tree.EntryType.Declaration;
                                    }
                                    break;

                                default:
                                    break;
                            }
                        }
                        break;

                    default:
                        break;
                }
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
                    (cs.ApiInfo.Types.ContainsKey(ent)) ||
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

        private void IdentifyFunctions(CompileState cs, Tree resolvetree, int lineNumber, CultureInfo currentCulture)
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
                    cs.ApiInfo.Methods.ContainsKey(ent))
                {
                    if(i + 1 >= n)
                    {
                        throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "Function0IsNotFollowedByOpeningParenthesis", "Function '{0}' is not followed by '('"), ent));
                    }
                    else if(resolvetree.SubTree[i + 1].Entry != "(")
                    {
                        throw new CompilerException(lineNumber, string.Format(this.GetLanguageString(currentCulture, "InvalidCallToFunction0", "Invalid call to function '{0}'"), ent));
                    }
                    else
                    {
                        st.Type = Tree.EntryType.Function;
                    }
                }
            }
        }
        #endregion

        private Tree LineToExpressionTree(CompileState cs, List<string> expressionLine, ICollection<string> localVarNames, int lineNumber, CultureInfo currentCulture)
        {
            PreprocessLine(cs, expressionLine);
            var expressionTree = new Tree(expressionLine);
            IdentifyReservedWords(cs, expressionTree);
            IdentifyVariables(expressionTree, localVarNames);
            IdentifyFunctions(cs, expressionTree, lineNumber, currentCulture);
            IdentifyOperators(cs, expressionTree);
            IdentifyNumericValues(expressionTree);
            IdentifyDeclarations(expressionTree);

            int n = expressionTree.SubTree.Count;
            var msg = new StringBuilder();
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
                        if(msg.Length != 0)
                        {
                            msg.Append("\n");
                        }
                        msg.AppendFormat(this.GetLanguageString(currentCulture, "NoFunction0Defined", "no function '{0}' defined"), entry);
                    }
                    else if(i > 0 &&
                        expressionTree.SubTree[i - 1].Type == Tree.EntryType.OperatorUnknown &&
                        expressionTree.SubTree[i - 1].Entry == ".")
                    {
                        /* element selector */
                    }
                    else
                    {
                        if (msg.Length != 0)
                        {
                            msg.Append("\n");
                        }
                        msg.AppendFormat(this.GetLanguageString(currentCulture, "Variable0NotDefined", "no variable '{0}' defined"), entry);
                    }
                }
                st.Process(cs, lineNumber);
            }

            if(msg.Length != 0)
            {
                throw new CompilerException(lineNumber, msg.ToString());
            }

            /* After OrderBrackets only deep-scanners can be used */
            OrderBrackets(cs, expressionTree, lineNumber, currentCulture);

            OrderOperators(cs, expressionTree, lineNumber, currentCulture);

            SolveTree(cs, expressionTree, lineNumber, currentCulture);
            if (expressionTree.SubTree.Count != 1)
            {
                throw new CompilerException(lineNumber, "Internal Error! Expression tree not solved");
            }
            return expressionTree;
        }
    }
}
