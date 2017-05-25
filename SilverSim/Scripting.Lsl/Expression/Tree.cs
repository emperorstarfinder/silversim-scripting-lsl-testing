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

#pragma warning disable IDE0018, RCS1029

using SilverSim.Scene.Types.Script;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SilverSim.Scripting.Lsl.Expression
{
    public class Tree
    {
        public enum EntryType
        {
            Unknown,
            StringValue,
            Value,
            OperatorUnknown,
            OperatorLeftUnary, /* e.g. ++x */
            OperatorRightUnary, /* e.g. x++ */
            OperatorBinary,
            ReservedWord,
            Invalid,
            Function,
            FunctionArgument,
            Declaration,
            DeclarationArgument,
            Vector,
            Rotation,
            Separator,
            LevelBegin, /* intermediate step */
            LevelEnd, /* intermediate step */
            Level,
            ExpressionTree,
            Variable
        }

        public bool ProcessedOpSort;

        public List<Tree> SubTree = new List<Tree>();
        public EntryType Type /* = EntryType.Unknown */;
        public string Entry = string.Empty;

        public abstract class ValueBase
        {
            public abstract ValueBase Negate();
        }

        public abstract class ConstantValue : ValueBase
        {
        }

        public class ConstantValueInt : ConstantValue
        {
            public int Value;

            public ConstantValueInt(int value)
            {
                Value = value;
            }

            public ConstantValueInt(string str)
            {
                Value = LSLCompiler.ConvToInt(str);
            }

            public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

            public override ValueBase Negate() => new ConstantValueInt(-Value);
        }

        public class ConstantValueLong : ConstantValue
        {
            public long Value;

            public ConstantValueLong(long value)
            {
                Value = value;
            }

            public ConstantValueLong(string str)
            {
                Value = LSLCompiler.ConvToLong(str);
            }

            public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

            public override ValueBase Negate() => new ConstantValueLong(-Value);
        }

        public class ConstantValueFloat : ConstantValue
        {
            public double Value;
            public ConstantValueFloat(double value)
            {
                Value = value;
            }

            public override string ToString() => LSLCompiler.TypecastDoubleToString(Value);

            public override ValueBase Negate() => new ConstantValueFloat(-Value);
        }

        public class ConstantValueString : ConstantValue
        {
            public string Value;
            public ConstantValueString(string value)
            {
                Value = value;
            }

            public override string ToString() => Value;

            public override ValueBase Negate()
            {
                throw new NotSupportedException("strings cannot be negated");
            }
        }

        public ValueBase Value;

        public Type ValueType => Value?.GetType();

        public Tree()
        {
            /* intentionally left empty */
        }

        /* pre-initializes an expression tree */
        public Tree(List<string> args)
        {
            Type = EntryType.ExpressionTree;
            Tree nt;
            foreach(string arg in args)
            {
                if(arg.StartsWith("\""))
                {
                    nt = new Tree()
                    {
                        Type = EntryType.StringValue,
                        Entry = arg.Substring(1, arg.Length - 2)
                    };
                    SubTree.Add(nt);
                }
                else
                {
                    nt = new Tree()
                    {
                        Type = EntryType.Unknown,
                        Entry = arg
                    };
                    SubTree.Add(nt);
                }
            }
        }

        private static string ProcessCSlashes(string v)
        {
            int idx = 0;
            int slen = v.Length;
            var o = new StringBuilder();
            while(idx < v.Length)
            {
                char c = v[idx++];
                switch(c)
                {
                    case '\\':
                        if (idx < v.Length)
                        {
                            c = v[idx++];
                            switch (c)
                            {
                                case 't':
                                    o.Append("    ");
                                    break;

                                case 'n':
                                    o.Append((char)10);
                                    break;

                                default:
                                    o.Append(c);
                                    break;
                            }
                        }
                        break;

                    default:
                        o.Append(c);
                        break;
                }
            }
            return o.ToString();
        }

        internal void Process(LSLCompiler.CompileState cs, int lineNumber)
        {
            if(Type == EntryType.StringValue)
            {
                Value = new ConstantValueString(ProcessCSlashes(Entry));
            }
            else if(Type == EntryType.Value)
            {
                int val;
                double fval;
                if(int.TryParse(Entry, out val) || Entry.StartsWith("0x") || Entry.StartsWith("0X"))
                {
                    Value = new ConstantValueInt(Entry);
                }
                else if(cs.LanguageExtensions.EnableLongIntegers && (Entry.EndsWith("l") || Entry.EndsWith("L")))
                {
                    Value = new ConstantValueLong(Entry.Substring(0, Entry.Length - 1));
                }
                else if(double.TryParse(Entry, NumberStyles.Float, CultureInfo.InvariantCulture, out fval))
                {
                    if (Entry.StartsWith("-") && BitConverter.DoubleToInt64Bits(fval) == 0)
                    {
                        fval = LSLCompiler.NegativeZero;
                    }
                    Value = new ConstantValueFloat(fval);
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("'{0}' is not a value", Entry));
                }
            }
        }
    }
}
