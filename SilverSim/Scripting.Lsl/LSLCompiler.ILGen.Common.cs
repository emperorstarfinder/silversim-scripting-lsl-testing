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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {

        public sealed class LSLScriptAssembly : IScriptAssembly
        {
            public readonly Assembly Assembly;
            readonly Type m_ScriptType;
            readonly Dictionary<string, Type> m_StateTypes;
            readonly bool m_ForcedSleep;
            readonly List<Action<ScriptInstance>> m_ScriptRemoveDelegates;
            readonly List<Action<ScriptInstance, List<object>>> m_ScriptSerializeDelegates;
            readonly Dictionary<string, Action<ScriptInstance, List<object>>> m_ScriptDeserializeDelegates;

            public LSLScriptAssembly(
                Assembly assembly, 
                Type script, 
                Dictionary<string, Type> stateTypes, 
                bool forcedSleep, 
                List<Action<ScriptInstance>> scriptRemoveDelegates,
                List<Action<ScriptInstance, List<object>>> scriptSerializeDelegates,
                Dictionary<string, Action<ScriptInstance, List<object>>> scriptDeserializeDelegates)
            {
                Assembly = assembly;
                m_ScriptType = script;
                m_StateTypes = stateTypes;
                m_ForcedSleep = forcedSleep;
                m_ScriptRemoveDelegates = scriptRemoveDelegates;
                m_ScriptSerializeDelegates = scriptSerializeDelegates;
                m_ScriptDeserializeDelegates = scriptDeserializeDelegates;
            }

            public ScriptInstance Instantiate(ObjectPart objpart, ObjectPartInventoryItem item, byte[] serializedState = null)
            {
                ConstructorInfo scriptconstructor = m_ScriptType.
                    GetConstructor(new Type[3] { typeof(ObjectPart), typeof(ObjectPartInventoryItem), typeof(bool) });
                Script m_Script = (Script)scriptconstructor.Invoke(new object[3] { objpart, item, m_ForcedSleep });

                foreach (KeyValuePair<string, Type> t in m_StateTypes)
                {
                    ConstructorInfo info = t.Value.GetConstructor(new Type[1] { m_ScriptType });
                    object[] param = new object[1];
                    param[0] = m_Script;
                    m_Script.AddState(t.Key, (ILSLState)info.Invoke(param));
                }

                m_Script.ScriptRemoveDelegates = m_ScriptRemoveDelegates;
                m_Script.SerializationDelegates = m_ScriptSerializeDelegates;
                m_Script.DeserializationDelegates = m_ScriptDeserializeDelegates;
                if (null != serializedState)
                {
                    try
                    {
                        using (MemoryStream ms = new MemoryStream(serializedState))
                        {
                            using (XmlTextReader reader = new XmlTextReader(ms))
                            {
                                m_Script.LoadScriptState(Script.SavedScriptState.FromXML(reader, item));
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        m_Log.WarnFormat("Failed to restore script state for {0} ({1}): {2} ({3}): {4}: {5}\n{6}", objpart.Name, objpart.ID, item.Name, item.ID,
                            e.GetType().FullName, e.Message, e.StackTrace);
                        m_Script.IsResetRequired = true;
                    }
                }
                return m_Script;
            }
        }

        internal sealed class ILParameterInfo
        {
            public int Position;
            public Type ParameterType;

            public ILParameterInfo(Type type, int position)
            {
                ParameterType = type;
                Position = position;
            }
        }

        internal sealed class ILLabelInfo
        {
            public Label Label;
            public bool IsDefined;
            public List<int> UsedInLines = new List<int>();

            public ILLabelInfo(Label label, bool isDefined)
            {
                Label = label;
                IsDefined = isDefined;
            }
        }

        #region LSL Integer Overflow
        /* special functions for converts
         * 
         * Integer Overflow
         * The compiler treats integers outside the range -2147483648 to 2147483647 somewhat strangely. No compile time warning or error is generated. (If the following explanation, doesn't make sense to you don't worry -- just know to avoid using numbers outside the valid range in your script.)

         * - For an integer outside the range -2147483648 to 2147483647, the absolute value of the number is reduced to fall in the range 0 to 4294967295 (0xFFFFFFFF).
         * - This number is then parsed as an unsigned 32 bit integer and cast to the corresponding signed integer.
         * - If the value in the script had a negative sign, the sign of the internal representation is switched.
         * - The net effect is that very large positive numbers get mapped to -1 and very large negative numbers get mapped to 1.
         */

        public static int ConvToInt(double v)
        {
            try
            {
                return (int)v;
            }
            catch
            {
                if (v > 0)
                {
                    try
                    {
                        return (int)((uint)v);
                    }
                    catch
                    {
                        return -1;
                    }
                }
                else
                {
                    try
                    {
                        return (int)-((uint)v);
                    }
                    catch
                    {
                        return 1;
                    }
                }
            }
        }

        public static AnArray AddKeyToList(AnArray src, LSLKey key)
        {
            AnArray res = new AnArray();
            res.AddRange(src);
            res.Add((IValue)key);
            return res;
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public static int ConvToInt(string v)
        {
            if (v.ToLower().StartsWith("0x"))
            {
                try
                {
                    return (int)uint.Parse(v.Substring(2), NumberStyles.HexNumber);
                }
                catch
                {
                    return -1;
                }
            }
            else
            {
                try
                {
                    return int.Parse(v);
                }
                catch
                {
                    string t;
                    uint i;
                    int pos = 0;
                    while (pos < v.Length)
                    {
                        char c = v[pos];
                        if (!char.IsDigit(c) && c != '-')
                        {
                            break;
                        }
                        ++pos;
                    }
                    t = v.Substring(0, pos);
                    if(t.Length == 0)
                    {
                        return 0;
                    }
                    else if (t.StartsWith("-"))
                    {
                        uint m = int.MaxValue;
                        m += 1;
                        return (pos == 1 || !uint.TryParse(t.Substring(1), out i) || i > m) ? 1 : -(int)i;
                    }
                    else
                    {
                        return (pos == 0 || !uint.TryParse(t, out i) || i > int.MaxValue) ? -1 : (int)i;
                    }
                }
            }
        }

        public static int LSL_IntegerMultiply(int a, int b)
        {
            long c = (long)a * b;
            if(c >= UInt32.MaxValue)
            {
                c = -1;
            }
            else if(c <= -(long)UInt32.MaxValue)
            {
                c = 1;
            }
            else if(c > Int32.MaxValue && c < UInt32.MaxValue)
            {
                c = (int)(uint)c;
            }
            else if(c < Int32.MinValue && c < -(long)UInt32.MaxValue)
            {
                c = -(int)(uint)c;
            }
            return (int)c;
        }

        public static int LSL_IntegerDivision(int a, int b)
        {
            return (a == -2147483648 && b == -1) ?
                -2147483648 :
                a / b;
        }

        public static int LSL_IntegerModulus(int a, int b)
        {
            return (a == -2147483648 && b == -1) ?
                0 :
                a % b;
        }
        #endregion

        #region Preprocessor for concatenated string constants
        void CollapseStringConstants(List<string> args)
        {
            int pos = 0;
            while(++pos < args.Count - 2)
            {
                if (args[pos] == "+" && args[pos - 1].StartsWith("\"") && args[pos + 1].StartsWith("\""))
                {
                    string larg = args[pos - 1];
                    args[pos - 1] = larg.Substring(0, larg.Length - 1) + args[pos + 1].Substring(1);
                    args.RemoveAt(pos);
                    args.RemoveAt(pos);
                    --pos;
                }
            }
        }
        #endregion

        void CombineTypecastArguments(List<string> args)
        {
            for (int pos = 0; pos < args.Count - 2; ++pos)
            {
                if (args[pos] == "(" && m_Typecasts.Contains(args[pos + 1]) && args[pos + 2] == ")")
                {
                    args[pos] = "(" + args[pos + 1] + ")";
                    args.RemoveAt(pos + 1);
                    args.RemoveAt(pos + 1);
                }
            }
        }

        /*  Process important things before solving operators */
        void PreprocessLine(List<string> args)
        {
            CombineTypecastArguments(args);
            CollapseStringConstants(args);
        }

        #region Type validation and string representation
        internal static bool IsValidType(Type t)
        {
            if (t == typeof(string))
            {
                return true;
            }
            if (t == typeof(int))
            {
                return true;
            }
            if (t == typeof(double))
            {
                return true;
            }
            if (t == typeof(LSLKey))
            {
                return true;
            }
            if (t == typeof(Quaternion))
            {
                return true;
            }
            if (t == typeof(Vector3))
            {
                return true;
            }
            if (t == typeof(AnArray))
            {
                return true;
            }
            if (t == typeof(void))
            {
                return true;
            }
            return false;
        }
        internal static string MapType(Type t)
        {
            if (t == typeof(string))
            {
                return "string";
            }
            if (t == typeof(int))
            {
                return "integer";
            }
            if (t == typeof(double))
            {
                return "float";
            }
            if (t == typeof(LSLKey))
            {
                return "key";
            }
            if (t == typeof(Quaternion))
            {
                return "rotation";
            }
            if (t == typeof(Vector3))
            {
                return "vector";
            }
            if (t == typeof(AnArray))
            {
                return "list";
            }
            if (t == typeof(void))
            {
                return "void";
            }
            return "???";
        }
        #endregion

        #region Typecasting IL Generator
        internal static void ProcessImplicitCasts(
            CompileState compileState,
            Type toType, 
            Type fromType, 
            int lineNumber)
        {
            if(IsImplicitlyCastable(toType, fromType))
            {
                ProcessCasts(compileState, toType, fromType, lineNumber);
            }
            else if(null == fromType)
            {
                throw new CompilerException(lineNumber, "Internal Error! fromType is not set");
            }
            else if (null == toType)
            {
                throw new CompilerException(lineNumber, "Internal Error! toType is not set");
            }
            else if (!IsValidType(fromType))
            {
                throw new CompilerException(lineNumber, string.Format("Internal Error! {0} is not a LSL compatible type", fromType.FullName));
            }
            else if (!IsValidType(toType))
            {
                throw new CompilerException(lineNumber, string.Format("Internal Error! {0} is not a LSL compatible type", toType.FullName));
            }
            else
            {
                throw new CompilerException(lineNumber, string.Format(compileState.GetLanguageString(compileState.CurrentCulture, "UnsupportedImplicitTypecastFrom0To1", "Unsupported implicit typecast from {0} to {1}"), MapType(fromType), MapType(toType)));
            }
        }

        public static double ParseStringToDouble(string input)
        {
            double v = 0;
            string inp = input;
            int strLen = inp.Length;
            bool isneg = strLen > 0 && input[0] == '-';
            while (strLen > 0)
            {
                if (!double.TryParse(inp, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                {
                    v = 0;
                    inp = inp.Substring(0, --strLen);
                }
                else
                {
                    if(isneg)
                    {
                        if (0 == BitConverter.DoubleToInt64Bits(v))
                        {
                            v *= -1.0;
                        }
                    }
                    break;
                }
            }
            return v;
        }

        public static bool TryParseStringToDouble(string input, out double v)
        {
            bool isneg = input.StartsWith("-");
            v = 0;
            bool parsed = double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
            if(parsed && isneg)
            {
                if(0 == BitConverter.DoubleToInt64Bits(v))
                {
                    v *= -1.0;
                }
            }
            return parsed;
        }

        public static Vector3 ParseStringToVector(string val)
        {
            char[] splitChar = { ',' };
            if(!val.StartsWith("<") && !val.EndsWith(">"))
            {
                return Vector3.Zero;
            }
            string[] split = val.Replace("<", string.Empty).Replace(">", string.Empty).Split(splitChar);
            if (split.Length != 3)
            {
                return Vector3.Zero;
            }

            double x;
            double y;
            double z;

            if (!TryParseStringToDouble(split[0], out x) ||
                !TryParseStringToDouble(split[1], out y) ||
                !TryParseStringToDouble(split[2], out z))
            {
                return Vector3.Zero;
            }
            return new Vector3(x, y, z);
        }

        public static Quaternion ParseStringToQuaternion(string input)
        {
            char[] splitChar = { ',' };
            if (!input.StartsWith("<") && !input.EndsWith(">"))
            {
                return Quaternion.Identity;
            }
            string[] split = input.Replace("<", string.Empty).Replace(">", string.Empty).Split(splitChar);
            int splitLength = split.Length;
            if (splitLength < 3 || splitLength > 4)
            {
                return Quaternion.Identity;
            }
            double x;
            double y;
            double z;
            double w;
            if (!TryParseStringToDouble(split[0], out x) ||
               !TryParseStringToDouble(split[1], out y) ||
                !TryParseStringToDouble(split[2], out z))
            {
                return Quaternion.Identity;
            }

            if (splitLength == 3)
            {
                return new Quaternion(x, y, z);
            }
            else
            {
                if (!TryParseStringToDouble(split[3], out w))
                {
                    return Quaternion.Identity;
                }
                return new Quaternion(x, y, z, w);
            }
        }

        public static Quaternion LSLQuaternionDivision(Quaternion a, Quaternion b)
        {
            return b.Conjugate() * a;
        }

        public static class SinglePrecision
        {
            static string TypecastFloatToString(double v, int placesAfter)
            {
                string val;
                if (BitConverter.DoubleToInt64Bits(v) == BitConverter.DoubleToInt64Bits(NegativeZero))
                {
                    val = "-0";
                }
                else
                {
                    val = ((float)v).ToString(CultureInfo.InvariantCulture);
                }

                if (!val.Contains("E") && !val.Contains("e"))
                {
                    int pos = val.IndexOf('.');
                    if (pos < 0)
                    {
                        val += ".0".PadRight(placesAfter + 1, '0');
                    }
                    else
                    {
                        val = val.Substring(0, pos + 1) + val.Substring(pos + 1).PadRight(placesAfter, '0');
                    }
                }
                return val;
            }

            public static string TypecastFloatToString(double v)
            {
                return TypecastFloatToString(v, 6);
            }

            public static string TypecastVectorToString5Places(Vector3 v)
            {
                return string.Format("<{0}, {1}, {2}>",
                    TypecastFloatToString(v.X, 5),
                    TypecastFloatToString(v.Y, 5),
                    TypecastFloatToString(v.Z, 5));
            }

            public static string TypecastVectorToString6Places(Vector3 v)
            {
                return string.Format("<{0}, {1}, {2}>",
                    TypecastFloatToString(v.X, 6),
                    TypecastFloatToString(v.Y, 6),
                    TypecastFloatToString(v.Z, 6));
            }

            public static string TypecastRotationToString5Places(Quaternion v)
            {
                return string.Format("<{0}, {1}, {2}, {3}>",
                    TypecastFloatToString(v.X, 5),
                    TypecastFloatToString(v.Y, 5),
                    TypecastFloatToString(v.Z, 5),
                    TypecastFloatToString(v.W, 5));
            }

            public static string TypecastRotationToString6Places(Quaternion v)
            {
                return string.Format("<{0}, {1}, {2}, {3}>",
                    TypecastFloatToString(v.X, 6),
                    TypecastFloatToString(v.Y, 6),
                    TypecastFloatToString(v.Z, 6),
                    TypecastFloatToString(v.W, 6));
            }

            public static string TypecastListToString(AnArray array)
            {
                StringBuilder sb = new StringBuilder();
                foreach (IValue iv in array)
                {
                    Type t = iv.GetType();
                    if (t == typeof(Real))
                    {
                        sb.Append(TypecastFloatToString(iv.AsReal));
                    }
                    else if (t == typeof(Vector3))
                    {
                        sb.Append(TypecastVectorToString6Places((Vector3)iv));
                    }
                    else if (t == typeof(Quaternion))
                    {
                        sb.Append(TypecastRotationToString6Places((Quaternion)iv));
                    }
                    else
                    {
                        sb.Append(iv.ToString());
                    }
                }
                return sb.ToString();
            }


        }

        static string TypecastDoubleToString(double v, int placesAfter)
        {
            string val;
            if(BitConverter.DoubleToInt64Bits(v) == BitConverter.DoubleToInt64Bits(NegativeZero))
            {
                val = "-0";
            }
            else
            {
                val = v.ToString(CultureInfo.InvariantCulture);
            }

            if(!val.Contains("E") && !val.Contains("e"))
            {
                int pos = val.IndexOf('.');
                if(pos < 0)
                {
                    val += ".0".PadRight(placesAfter + 1, '0');
                }
                else
                {
                    val = val.Substring(0, pos + 1) + val.Substring(pos + 1).PadRight(placesAfter, '0');
                }
            }
            return val;
        }

        public static string TypecastDoubleToString(double v)
        {
            return TypecastDoubleToString(v, 6);
        }

        public static string TypecastVectorToString5Places(Vector3 v)
        {
            return string.Format("<{0}, {1}, {2}>",
                TypecastDoubleToString(v.X, 5),
                TypecastDoubleToString(v.Y, 5),
                TypecastDoubleToString(v.Z, 5));
        }

        public static string TypecastVectorToString6Places(Vector3 v)
        {
            return string.Format("<{0}, {1}, {2}>",
                TypecastDoubleToString(v.X, 6),
                TypecastDoubleToString(v.Y, 6),
                TypecastDoubleToString(v.Z, 6));
        }

        public static string TypecastRotationToString5Places(Quaternion v)
        {
            return string.Format("<{0}, {1}, {2}, {3}>",
                TypecastDoubleToString(v.X, 5),
                TypecastDoubleToString(v.Y, 5),
                TypecastDoubleToString(v.Z, 5),
                TypecastDoubleToString(v.W, 5));
        }

        public static string TypecastRotationToString6Places(Quaternion v)
        {
            return string.Format("<{0}, {1}, {2}, {3}>",
                TypecastDoubleToString(v.X, 6),
                TypecastDoubleToString(v.Y, 6),
                TypecastDoubleToString(v.Z, 6),
                TypecastDoubleToString(v.W, 6));
        }

        public static string TypecastListToString(AnArray array)
        {
            StringBuilder sb = new StringBuilder();
            foreach (IValue iv in array)
            {
                Type t = iv.GetType();
                if (t == typeof(Real))
                {
                    sb.Append(TypecastDoubleToString(iv.AsReal));
                }
                else if (t == typeof(Vector3))
                {
                    sb.Append(TypecastVectorToString6Places((Vector3)iv));
                }
                else if (t == typeof(Quaternion))
                {
                    sb.Append(TypecastRotationToString6Places((Quaternion)iv));
                }
                else
                {
                    sb.Append(iv.ToString());
                }
            }
            return sb.ToString();
        }

        internal static bool IsImplicitlyCastable(Type toType, Type fromType)
        {
            return (fromType == toType ||
                toType == typeof(void) ||
                (fromType == typeof(string) && toType == typeof(LSLKey)) ||
                (fromType == typeof(LSLKey) && toType == typeof(string)) ||
                (fromType == typeof(int) && toType == typeof(double)) ||
                toType == typeof(AnArray) ||
                toType == typeof(bool)) ;
        }

        internal static void ProcessCasts(
            CompileState compileState,
            Type toType,
            Type fromType,
            int lineNumber)
        {
            /* value is on stack before */
            if (toType == fromType)
            {
                /* same type does not need any code to be generated */
            }
            else if (toType == typeof(void))
            {
                compileState.ILGen.Emit(OpCodes.Pop);
            }
            else if (fromType == typeof(void))
            {
                throw new CompilerException(lineNumber, compileState.GetLanguageString(compileState.CurrentCulture, "FunctionDoesNotReturnAnything", "function does not return anything"));
            }
            else if (toType == typeof(LSLKey))
            {
                if (fromType == typeof(string))
                {
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(new Type[] { fromType }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format(compileState.GetLanguageString(compileState.CurrentCulture, "UnsupportedTypecastFrom0To1", "unsupported typecast from {0} to {1}"), MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(string))
            {
                if (fromType == typeof(int))
                {
                    LocalBuilder lb = compileState.ILGen.DeclareLocal(fromType);
                    compileState.ILGen.Emit(OpCodes.Stloc, lb);
                    compileState.ILGen.Emit(OpCodes.Ldloca, lb);
                    compileState.ILGen.Emit(OpCodes.Callvirt, typeof(int).GetMethod("ToString", Type.EmptyTypes));
                }
                else if(fromType == typeof(Vector3))
                {
                    compileState.ILGen.Emit(OpCodes.Call, 
                        (compileState.UsesSinglePrecision ? typeof(SinglePrecision) : typeof(LSLCompiler)).GetMethod("TypecastVectorToString5Places"));
                }
                else if (fromType == typeof(Quaternion))
                {
                    compileState.ILGen.Emit(OpCodes.Call, 
                        (compileState.UsesSinglePrecision ? typeof(SinglePrecision) : typeof(LSLCompiler)).GetMethod("TypecastRotationToString5Places"));
                }
                else if (fromType == typeof(double))
                {
                    compileState.ILGen.Emit(OpCodes.Call, 
                        compileState.UsesSinglePrecision ? typeof(SinglePrecision).GetMethod("TypecastFloatToString") : typeof(LSLCompiler).GetMethod("TypecastDoubleToString"));
                }
                else if(fromType == typeof(AnArray))
                {
                    compileState.ILGen.Emit(OpCodes.Call,
                        (compileState.UsesSinglePrecision ? typeof(SinglePrecision) : typeof(LSLCompiler)).GetMethod("TypecastListToString"));
                }
                else if (fromType == typeof(LSLKey))
                {
                    compileState.ILGen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", Type.EmptyTypes));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format(compileState.GetLanguageString(compileState.CurrentCulture, "UnsupportedTypecastFrom0To1", "unsupported typecast from {0} to {1}"), MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(int))
            {
                /* yes, we need special handling for conversion of string to integer or float to integer. (see section about Integer Overflow) */
                if (fromType == typeof(string) || fromType == typeof(double))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ConvToInt", new Type[] { fromType }));
                }
                else if(fromType == typeof(LSLKey))
                {
                    /* extension to LSL explicit typecasting rules */
                    compileState.ILGen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ConvToInt", new Type[] { typeof(string) }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format(compileState.GetLanguageString(compileState.CurrentCulture, "UnsupportedTypecastFrom0To1", "unsupported typecast from {0} to {1}"), MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(bool))
            {
                if (fromType == typeof(string))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(string).GetProperty("Length").GetGetMethod());
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(int))
                {
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(LSLKey))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLKey).GetProperty("IsLSLTrue").GetGetMethod());
                }
                else if (fromType == typeof(double))
                {
                    compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(AnArray))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetProperty("Count").GetGetMethod());
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(Quaternion))
                {
                    LocalBuilder lb = compileState.ILGen.DeclareLocal(fromType);
                    compileState.ILGen.Emit(OpCodes.Stloc, lb);
                    compileState.ILGen.Emit(OpCodes.Ldloca, lb);
                    compileState.ILGen.Emit(OpCodes.Call, typeof(Quaternion).GetProperty("IsLSLTrue").GetGetMethod());
                }
                else if (fromType == typeof(Vector3))
                {
                    LocalBuilder lb = compileState.ILGen.DeclareLocal(fromType);
                    compileState.ILGen.Emit(OpCodes.Stloc, lb);
                    compileState.ILGen.Emit(OpCodes.Ldloca, lb);
                    compileState.ILGen.Emit(OpCodes.Call, typeof(Vector3).GetProperty("Length").GetGetMethod());
                    compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                    compileState.ILGen.Emit(OpCodes.Ldc_I4_0);
                    compileState.ILGen.Emit(OpCodes.Ceq);
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format(compileState.GetLanguageString(compileState.CurrentCulture, "UnsupportedTypecastFrom0To1", "unsupported typecast from {0} to {1}"), MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(double))
            {
                if (fromType == typeof(string))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ParseStringToDouble", new Type[] { typeof(string) }));
                }
                else if (fromType == typeof(int))
                {
                    compileState.ILGen.Emit(OpCodes.Conv_R8);
                }
                else if (fromType == typeof(LSLKey) && compileState.LanguageExtensions.EnableExtendedTypecasts)
                {
                    /* extension to LSL explicit typecasting rules */
                    compileState.ILGen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ParseStringToDouble", new Type[] { typeof(string) }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format(compileState.GetLanguageString(compileState.CurrentCulture, "UnsupportedTypecastFrom0To1", "unsupported typecast from {0} to {1}"), MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(Vector3))
            {
                if (fromType == typeof(string))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ParseStringToVector", new Type[] { typeof(string) }));
                }
                else if (fromType == typeof(LSLKey) && compileState.LanguageExtensions.EnableExtendedTypecasts)
                {
                    /* extension to LSL explicit typecasting rules */
                    compileState.ILGen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ParseStringToVector", new Type[] { typeof(string) }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format(compileState.GetLanguageString(compileState.CurrentCulture, "UnsupportedTypecastFrom0To1", "unsupported typecast from {0} to {1}"), MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(Quaternion))
            {
                if (fromType == typeof(string))
                {
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ParseStringToQuaternion", new Type[] { typeof(string) }));
                }
                else if (fromType == typeof(LSLKey) && compileState.LanguageExtensions.EnableExtendedTypecasts)
                {
                    /* extension to LSL explicit typecasting rules */
                    compileState.ILGen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ParseStringToQuaternion", new Type[] { typeof(string) }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format(compileState.GetLanguageString(compileState.CurrentCulture, "UnsupportedTypecastFrom0To1", "unsupported typecast from {0} to {1}"), MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(AnArray))
            {
                if (fromType == typeof(string) || fromType == typeof(int) || fromType == typeof(double))
                {
                    compileState.ILGen.BeginScope();
                    LocalBuilder lb = compileState.ILGen.DeclareLocal(fromType);
                    compileState.ILGen.Emit(OpCodes.Stloc, lb);
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Dup);
                    compileState.ILGen.Emit(OpCodes.Ldloc, lb);
                    compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { fromType }));
                    compileState.ILGen.EndScope();
                }
                else if(fromType == typeof(LSLKey))
                {
                    compileState.ILGen.BeginScope();
                    LocalBuilder lb = compileState.ILGen.DeclareLocal(fromType);
                    compileState.ILGen.Emit(OpCodes.Stloc, lb);
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Dup);
                    compileState.ILGen.Emit(OpCodes.Ldloc, lb);
                    compileState.ILGen.Emit(OpCodes.Call, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                    compileState.ILGen.EndScope();
                }
                else if (fromType == typeof(Vector3))
                {
                    compileState.ILGen.BeginScope();
                    LocalBuilder lb = compileState.ILGen.DeclareLocal(typeof(Vector3));
                    compileState.ILGen.Emit(OpCodes.Stloc, lb);
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Dup);
                    compileState.ILGen.Emit(OpCodes.Ldloc, lb);
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("AddVector3ToList"));
                    compileState.ILGen.EndScope();
                }
                else if (fromType == typeof(Quaternion))
                {
                    compileState.ILGen.BeginScope();
                    LocalBuilder lb = compileState.ILGen.DeclareLocal(typeof(Quaternion));
                    compileState.ILGen.Emit(OpCodes.Stloc, lb);
                    compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(Type.EmptyTypes));
                    compileState.ILGen.Emit(OpCodes.Dup);
                    compileState.ILGen.Emit(OpCodes.Ldloc, lb);
                    compileState.ILGen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("AddQuaternionToList"));
                    compileState.ILGen.EndScope();
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format(compileState.GetLanguageString(compileState.CurrentCulture, "UnsupportedTypecastFrom0To1", "unsupported typecast from {0} to {1}"), MapType(fromType), MapType(toType)));
                }
            }
            else
            {
                throw new CompilerException(lineNumber, string.Format(compileState.GetLanguageString(compileState.CurrentCulture, "UnsupportedTypecastFrom0To1", "unsupported typecast from {0} to {1}"), MapType(fromType), MapType(toType)));
            }
        }
        #endregion

        #region Variable Access IL Generator
        internal static Type GetVarType(
            object v)
        {
            ILParameterInfo ilpi;
            LocalBuilder lb;
            FieldBuilder fb;
            FieldInfo fi;

            ilpi = v as ILParameterInfo;
            if (null != ilpi)
            {
                return ilpi.ParameterType;
            }

            lb = v as LocalBuilder;
            if (null != lb)
            {
                return lb.LocalType;
            }

            fb = v as FieldBuilder;
            if (null != fb)
            {
                return fb.FieldType;
            }

            fi = v as FieldInfo;
            if (null != fi)
            {
                return fi.FieldType;
            }

            throw new NotSupportedException();
        }

        static Type EmitFieldRead(
            CompileState compileState,
            FieldInfo fi)
        {
            if ((fi.Attributes & FieldAttributes.Literal) != 0)
            {
                if (fi.FieldType == typeof(int))
                {
                    compileState.ILGen.Emit(OpCodes.Ldc_I4, (int)fi.GetValue(null));
                }
                else if (fi.FieldType == typeof(double))
                {
                    compileState.ILGen.Emit(OpCodes.Ldc_R8, (double)fi.GetValue(null));
                }
                else if(fi.FieldType == typeof(string))
                {
                    compileState.ILGen.Emit(OpCodes.Ldstr, (string)fi.GetValue(null));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if ((fi.Attributes & FieldAttributes.Static) != 0)
            {
                compileState.ILGen.Emit(OpCodes.Ldsfld, fi);
            }
            else
            {
                compileState.ILGen.Emit(OpCodes.Ldarg_0);
                if (null != compileState.StateTypeBuilder)
                {
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                }
                compileState.ILGen.Emit(OpCodes.Ldfld, fi);
            }
            return fi.FieldType;
        }

        internal static Type GetVarToStack(
            CompileState compileState,
            object v)
        {
            Type retType;
            ILParameterInfo ilpi;
            LocalBuilder lb;
            FieldBuilder fb;
            FieldInfo fi;

            if (null != (ilpi = v as ILParameterInfo))
            {
                compileState.ILGen.Emit(OpCodes.Ldarg, ilpi.Position);
                retType = ilpi.ParameterType;
            }
            else if (null != (lb = v as LocalBuilder))
            {
                compileState.ILGen.Emit(OpCodes.Ldloc, lb);
                retType = lb.LocalType;
            }
            else if (null != (fb = v as FieldBuilder))
            {
                retType = EmitFieldRead(
                    compileState,
                    fb);
            }
            else if (null != (fi = v as FieldInfo))
            {
                retType = EmitFieldRead(
                    compileState,
                    fi);
            }
            else
            {
                throw new NotSupportedException();
            }
            if (retType == typeof(AnArray))
            {
                /* list has deep copying */
                compileState.ILGen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[] { retType }));
            }
            return retType;
        }

        internal static void SetVarFromStack(
            CompileState compileState,
            object v,
            int lineNumber)
        {
            ILParameterInfo ilpi;
            LocalBuilder lb;
            FieldBuilder fb;
            FieldInfo fi;

            ilpi = v as ILParameterInfo;
            if (null != ilpi)
            {
                compileState.ILGen.Emit(OpCodes.Starg, ilpi.Position);
                return;
            }

            lb = v as LocalBuilder;
            if (null != lb)
            {
                compileState.ILGen.Emit(OpCodes.Stloc, lb);
                return;
            }

            fb = v as FieldBuilder;
            if (null != fb)
            {
                if ((fb.Attributes & FieldAttributes.Static) != 0)
                {
                    throw new CompilerException(lineNumber, compileState.GetLanguageString(compileState.CurrentCulture, "SettingConstantsIsNotAllowed", "Setting constants is not allowed"));
                }
                compileState.ILGen.BeginScope();
                LocalBuilder swapLb = compileState.ILGen.DeclareLocal(fb.FieldType);
                compileState.ILGen.Emit(OpCodes.Stloc, swapLb);
                compileState.ILGen.Emit(OpCodes.Ldarg_0);
                if (null != compileState.StateTypeBuilder)
                {
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                }
                compileState.ILGen.Emit(OpCodes.Ldloc, swapLb);

                compileState.ILGen.Emit(OpCodes.Stfld, fb);
                compileState.ILGen.EndScope();
                return;
            }

            fi = v as FieldInfo;
            if (null != fi)
            {
                if ((fi.Attributes & FieldAttributes.Static) != 0)
                {
                    throw new CompilerException(lineNumber, compileState.GetLanguageString(compileState.CurrentCulture, "SettingConstantsIsNotAllowed", "Setting constants is not allowed"));
                }
                compileState.ILGen.BeginScope();
                LocalBuilder swapLb = compileState.ILGen.DeclareLocal(fi.FieldType);
                compileState.ILGen.Emit(OpCodes.Stloc, swapLb);
                compileState.ILGen.Emit(OpCodes.Ldarg_0);
                if (null != compileState.StateTypeBuilder)
                {
                    compileState.ILGen.Emit(OpCodes.Ldfld, compileState.InstanceField);
                }
                compileState.ILGen.Emit(OpCodes.Ldloc, swapLb);

                compileState.ILGen.Emit(OpCodes.Stfld, fi);
                compileState.ILGen.EndScope();
                return;
            }

            throw new NotSupportedException();
        }
        #endregion

        #region Constants collector for IL Generator
        Dictionary<string, object> AddConstants(CompileState compileState, TypeBuilder typeBuilder, ILGenerator ilgen)
        {
            Dictionary<string, object> localVars = new Dictionary<string, object>();
            foreach(KeyValuePair<string, FieldInfo> kvp in compileState.ApiInfo.Constants)
            {
                FieldInfo f = kvp.Value;
                if ((f.Attributes & FieldAttributes.Static) != 0)
                {
                    if ((f.Attributes & FieldAttributes.InitOnly) != 0 || (f.Attributes & FieldAttributes.Literal) != 0)
                    {
                        localVars[kvp.Key] = f;
                    }
                    else
                    {
                        m_Log.DebugFormat(compileState.GetLanguageString(compileState.CurrentCulture, "Field0HasUnsupportedAttributeFlags1", "Field {0} has unsupported attribute flags {1}"), kvp.Key, f.Attributes.ToString());
                    }
                }

            }
            return localVars;
        }
        #endregion
    }
}
