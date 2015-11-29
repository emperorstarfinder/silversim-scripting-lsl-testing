// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.Lsl.Expression
{
    [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
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
            public ValueBase()
            {
                /* intentionally left empty */
            }

            public abstract ValueBase Negate();
        }

        public abstract class ConstantValue : ValueBase
        {
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
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

            public override string ToString()
            {
                return Value.ToString(CultureInfo.InvariantCulture);
            }

            public override ValueBase Negate()
            {
                return new ConstantValueInt(-Value);
            }
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public class ConstantValueFloat : ConstantValue
        {
            public double Value;
            public ConstantValueFloat(double value)
            {
                Value = value;
            }

            public override string ToString()
            {
                return Value.ToString(CultureInfo.InvariantCulture);
            }

            public override ValueBase Negate()
            {
                return new ConstantValueFloat(-Value);
            }
        }

        public class ConstantValueString : ConstantValue
        {
            public string Value;
            public ConstantValueString(string value)
            {
                Value = value;
            }

            public override string ToString()
            {
                return Value;
            }

            public override ValueBase Negate()
            {
                throw new NotSupportedException("strings cannot be negated");
            }
        }

        public ValueBase Value;


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
                    nt = new Tree();
                    nt.Type = EntryType.StringValue;
                    nt.Entry = arg.Substring(1, arg.Length - 2);
                    SubTree.Add(nt);
                }
                else
                {
                    nt = new Tree();
                    nt.Type = EntryType.Unknown;
                    nt.Entry = arg;
                    SubTree.Add(nt);
                }
            }
        }

        public void Process(int lineNumber)
        {
            if(Type == EntryType.StringValue)
            {
                Value = new ConstantValueString(Entry);
            }
            else if(Type == EntryType.Value)
            {
                int val;
                float fval;
                if(int.TryParse(Entry, out val) || Entry.StartsWith("0x"))
                {
                    Value = new ConstantValueInt(Entry);
                }
                else if(float.TryParse(Entry, NumberStyles.Float, CultureInfo.InvariantCulture, out fval))
                {
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
