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
using System.Reflection;
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

        private sealed class ConstantValueKey : Tree.ConstantValue
        {
            public LSLKey Value;

            public ConstantValueKey(LSLKey v)
            {
                Value = v;
            }

            public override string ToString() => Value.ToString();

            public override Tree.ValueBase Negate()
            {
                throw new NotSupportedException("key cannot be negated");
            }
        }
        #endregion


        private void AssignResult(Tree st, object resValue)
        {
            Type resType = resValue.GetType();
            if (resType == typeof(Quaternion))
            {
                st.Value = new ConstantValueRotation((Quaternion)resValue);
            }
            else if (resType == typeof(Vector3))
            {
                st.Value = new ConstantValueVector((Vector3)resValue);
            }
            else if (resType == typeof(char))
            {
                st.Value = new Tree.ConstantValueChar((char)resValue);
            }
            else if (resType == typeof(double))
            {
                st.Value = new Tree.ConstantValueFloat((double)resValue);
            }
            else if (resType == typeof(int))
            {
                st.Value = new Tree.ConstantValueInt((int)resValue);
            }
            else if (resType == typeof(long))
            {
                st.Value = new Tree.ConstantValueLong((long)resValue);
            }
            else if (resType == typeof(string))
            {
                st.Value = new Tree.ConstantValueString((string)resValue);
            }
            else if(resType == typeof(LSLKey))
            {
                st.Value = new ConstantValueKey((LSLKey)resValue);
            }
        }


        private void SolveConstantOperations(CompileState cs, Tree tree, CultureInfo currentCulture, bool solveMemberFunctions)
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
                                throw new CompilerException(st.SubTree[idx].SubTree[0].LineNumber, this.GetLanguageString(currentCulture, "ConstantVectorCannotContainOtherValuesThanFloatOrInt", "constant vector cannot contain other values than float or int"));
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
                        var v = new double[4];
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
                                throw new CompilerException(st.SubTree[idx].SubTree[0].LineNumber, "constant rotation cannot contain other values than float or int");
                            }
                        }

                        st.Value = new ConstantValueRotation(new Quaternion(v[0], v[1], v[2], v[3]));
                    }
                }

                if (st.Type == Tree.EntryType.Variable)
                {
                    FieldInfo fi;
                    if(cs.ApiInfo.Constants.TryGetValue(st.Entry, out fi))
                    {
                        object o = fi.GetValue(null);
                        AssignResult(st, o);
                    }
                }

                #region Function operators
                if(!solveMemberFunctions)
                {
                    /* ignore functions when not set */
                }
                else if(st.Type == Tree.EntryType.MemberFunction)
                {
                    bool areAllArgumentsConstant = true;
                    foreach (Tree ot in st.SubTree)
                    {
                        if (ot.Type == Tree.EntryType.FunctionArgument && ot.Value == null)
                        {
                            ot.SubTree[0].Process(cs);
                            ot.Value = ot.SubTree[0].Value;
                            if (ot.Value == null)
                            {
                                areAllArgumentsConstant = false;
                            }
                        }
                    }

                    if (areAllArgumentsConstant && cs.LanguageExtensions.EnableFunctionConstantSolver)
                    {
                        SolveFunctionConstantOperations(cs, st, cs.ApiInfo.MemberMethods, cs.ApiInfo.InlineMemberMethods, true);
                    }
                }
                else if (st.Type == Tree.EntryType.Function)
                {
                    bool areAllArgumentsConstant = true;
                    foreach (Tree ot in st.SubTree)
                    {
                        if (ot.Type == Tree.EntryType.FunctionArgument && ot.Value == null)
                        {
                            ot.SubTree[0].Process(cs);
                            ot.Value = ot.SubTree[0].Value;
                            if (ot.Value == null)
                            {
                                areAllArgumentsConstant = false;
                            }
                        }
                    }

                    if (areAllArgumentsConstant && cs.LanguageExtensions.EnableFunctionConstantSolver)
                    {
                        SolveFunctionConstantOperations(cs, st, cs.ApiInfo.Methods, cs.ApiInfo.InlineMethods, false);
                    }
                }
                #endregion

                #region Binary operators
                if (st.Type == Tree.EntryType.OperatorBinary)
                {
                    foreach (Tree ot in st.SubTree)
                    {
                        if (ot.Type == Tree.EntryType.Value && ot.Value == null)
                        {
                            ot.Process(cs);
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
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value);
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
                            else if (leftType == typeof(Tree.ConstantValueString) && rightType == typeof(ConstantValueKey) &&
                                cs.LanguageExtensions.EnableKeyAndStringConcat)
                            {
                                st.Value = new Tree.ConstantValueString(
                                    ((Tree.ConstantValueString)(st.SubTree[0].Value)).Value +
                                    ((ConstantValueKey)(st.SubTree[1].Value)).ToString());
                            }
                            else if (leftType == typeof(ConstantValueKey) && rightType == typeof(Tree.ConstantValueString) &&
                                cs.LanguageExtensions.EnableKeyAndStringConcat)
                            {
                                st.Value = new Tree.ConstantValueString(
                                    ((ConstantValueKey)(st.SubTree[0].Value)).ToString() +
                                    ((Tree.ConstantValueString)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(ConstantValueKey) && rightType == typeof(ConstantValueKey) &&
                                cs.LanguageExtensions.EnableKeyAndStringConcat)
                            {
                                st.Value = new Tree.ConstantValueString(
                                    ((ConstantValueKey)(st.SubTree[0].Value)).ToString() +
                                    ((ConstantValueKey)(st.SubTree[1].Value)).ToString());
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value);
                            }
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value);
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value ? 1 : 0);
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value ? 1 : 0);
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value ? 1 : 0);
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value ? 1 : 0);
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value ? 1 : 0);
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueInt))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueInt) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (leftType == typeof(Tree.ConstantValueChar) && rightType == typeof(Tree.ConstantValueChar))
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value ? 1 : 0);
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                                throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                                else if (leftType == typeof(Tree.ConstantValueChar))
                                {
                                    isLeftTrue = ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value != 0;
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
                                    throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                                }

                                if (rightType == typeof(Tree.ConstantValueInt))
                                {
                                    isRightTrue = ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value != 0;
                                }
                                else if (rightType == typeof(Tree.ConstantValueChar))
                                {
                                    isRightTrue = ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value != 0;
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
                                    throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
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
                                else if (leftType == typeof(Tree.ConstantValueChar))
                                {
                                    isLeftTrue = ((Tree.ConstantValueChar)(st.SubTree[0].Value)).Value != 0;
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
                                    throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                                }

                                if (rightType == typeof(Tree.ConstantValueInt))
                                {
                                    isRightTrue = ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value != 0;
                                }
                                else if (rightType == typeof(Tree.ConstantValueChar))
                                {
                                    isRightTrue = ((Tree.ConstantValueChar)(st.SubTree[1].Value)).Value != 0;
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
                                    throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OnParametersOfMismatchingType", "Cannot process '{0}' on parameters of mismatching type"), st.Entry));
                                }

                                st.Value = new Tree.ConstantValueInt((isLeftTrue || isRightTrue).ToLSLBoolean());
                            }
                            break;

                        default:
                            throw new CompilerException(st.LineNumber, string.Format(this.GetLanguageString(currentCulture, "CannotProcess0OperatorIsUnknown", "Cannot process '{0}': operator is unknown"), st.Entry));
                    }
                }
                #endregion
                #region Left unary operators
                else if (st.Type == Tree.EntryType.OperatorLeftUnary && (st.SubTree[0].Value != null || st.SubTree[0].Type == Tree.EntryType.Value))
                {
                    if (st.Entry != "-" && st.SubTree[0].Type == Tree.EntryType.Value)
                    {
                        st.Process(cs);
                    }
                    if (st.Entry == "+")
                    {
                        st.Value = st.SubTree[0].Value;
                    }
                    else if (st.Entry == "-")
                    {
                        if (st.SubTree[0].Value == null)
                        {
                            st.SubTree[0].Process(cs);
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
                            throw new CompilerException(st.LineNumber, this.GetLanguageString(currentCulture, "FloatCannotBeBinaryNegated", "float cannot be binary-negated"));
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
                            throw new CompilerException(st.LineNumber, this.GetLanguageString(currentCulture, "StringCannotBeBinaryNegated", "string cannot be binary negated"));
                        }
                    }
                    else if (st.Entry == "!")
                    {
                        Type type = st.ValueType;
                        if (type == typeof(Tree.ConstantValueFloat))
                        {
                            throw new CompilerException(st.LineNumber, this.GetLanguageString(currentCulture, "FloatCannotBeLogicallyNegated", "float cannot be logically negated"));
                        }
                        else if (type == typeof(Tree.ConstantValueInt))
                        {
                            st.Value = new Tree.ConstantValueInt(((Tree.ConstantValueInt)(st.Value)).Value == 0 ? 1 : 0);
                        }
                        else if (type == typeof(Tree.ConstantValueChar))
                        {
                            st.Value = new Tree.ConstantValueInt(((Tree.ConstantValueChar)(st.Value)).Value == 0 ? 1 : 0);
                        }
                        else if (type == typeof(Tree.ConstantValueLong))
                        {
                            st.Value = new Tree.ConstantValueInt(~((Tree.ConstantValueLong)(st.Value)).Value == 0 ? 1 : 0);
                        }
                        else if (type == typeof(Tree.ConstantValueString))
                        {
                            throw new CompilerException(st.LineNumber, this.GetLanguageString(currentCulture, "StringCannotBeLogicallyNegated", "string cannot be logically negated"));
                        }
                    }
                    else if (st.SubTree[0].Value != null)
                    {
                        Type type = st.SubTree[0].ValueType;
                        switch (st.Entry)
                        {
                            case "(key)":
                                if(type == typeof(Tree.ConstantValueString))
                                {
                                    st.Value = new ConstantValueKey(new LSLKey(((Tree.ConstantValueString)st.SubTree[0].Value).Value));
                                }
                                break;

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
                                else if(type == typeof(ConstantValueKey))
                                {
                                    st.Value = new Tree.ConstantValueString(st.SubTree[0].ToString());
                                }
                                else if (type == typeof(Tree.ConstantValueString))
                                {
                                    st.Value = new Tree.ConstantValueString(((Tree.ConstantValueChar)st.SubTree[0].Value).ToString());
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

                            case "(char)":
                                if (type == typeof(Tree.ConstantValueInt))
                                {
                                    st.Value = new Tree.ConstantValueChar((char)((Tree.ConstantValueInt)st.SubTree[0].Value).Value);
                                }
                                else if (type == typeof(Tree.ConstantValueString))
                                {
                                    var cvs = (Tree.ConstantValueString)st.SubTree[0].Value;
                                    st.Value = new Tree.ConstantValueChar(cvs.Value.Length != 0 ? cvs.Value[0] : char.MinValue);
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
                                    st.Value = new Tree.ConstantValueInt(ConvToInt(((Tree.ConstantValueLong)st.SubTree[0].Value).Value));
                                }
                                else if (type == typeof(Tree.ConstantValueChar))
                                {
                                    st.Value = new Tree.ConstantValueInt(((Tree.ConstantValueChar)st.SubTree[0].Value).Value);
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

    }
}
