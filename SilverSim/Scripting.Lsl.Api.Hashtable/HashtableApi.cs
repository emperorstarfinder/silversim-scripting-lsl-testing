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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl.Api.ByteString;
using SilverSim.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;

namespace SilverSim.Scripting.Lsl.Api.Hashtable
{

    [ScriptApiName("Hashtable")]
    [LSLImplementation]
    [PluginName("LSL_Hashtable")]
    [Description("ASSL Hashtable API")]
    public class HashtableApi : IPlugin, IScriptApi
    {
        [APILevel(APIFlags.ASSL, "variant2")]
        [APIDisplayName("variant2")]
        [ImplementsCustomTypecasts]
        [APIAccessibleMembers("Type")]
        public struct Variant
        {
            internal Variant(IValue iv)
            {
                if(iv == null)
                {
                    throw new ArgumentNullException(nameof(iv));
                }
                Value = iv;
            }

            public IValue Value { get; private set; }

            [APILevel(APIFlags.ASSL)]
            public static implicit operator Variant(int v) => new Variant { Value = new Integer(v) };
            [APILevel(APIFlags.ASSL)]
            public static implicit operator Variant(long v) => new Variant { Value = new LongInteger(v) };
            [APILevel(APIFlags.ASSL)]
            public static implicit operator Variant(double v) => new Variant { Value = new Real(v) };
            [APILevel(APIFlags.ASSL)]
            public static implicit operator Variant(string v) => new Variant { Value = new AString(v) };
            [APILevel(APIFlags.ASSL)]
            public static implicit operator Variant(LSLKey v) => new Variant { Value = new LSLKey(v) };
            [APILevel(APIFlags.ASSL)]
            public static implicit operator Variant(Vector3 v) => new Variant { Value = v };
            [APILevel(APIFlags.ASSL)]
            public static implicit operator Variant(Quaternion v) => new Variant { Value = v };
            [APILevel(APIFlags.ASSL)]
            public static implicit operator Variant(ByteArrayApi.ByteArray v) => new Variant { Value = new ByteArrayApi.ByteArray(v) };
            [APILevel(APIFlags.ASSL)]
            public static implicit operator bool(Variant v) => v.Value.AsBoolean;
            [APILevel(APIFlags.ASSL)]
            public static implicit operator int(Variant v) => v.Value.LslConvertToInt();
            [APILevel(APIFlags.ASSL)]
            public static implicit operator long(Variant v) => v.Value.LslConvertToLong();
            [APILevel(APIFlags.ASSL)]
            public static implicit operator double(Variant v) => v.Value.LslConvertToFloat();
            [APILevel(APIFlags.ASSL)]
            public static implicit operator string(Variant v) => v.Value.LslConvertToString();
            [APILevel(APIFlags.ASSL)]
            public static implicit operator LSLKey(Variant v) => v.Value.LslConvertToKey();
            [APILevel(APIFlags.ASSL)]
            public static implicit operator Vector3(Variant v) => v.Value.LslConvertToVector();
            [APILevel(APIFlags.ASSL)]
            public static implicit operator Quaternion(Variant v) => v.Value.LslConvertToRot();
            [APIExtension(APIExtension.ByteArray)]
            public static implicit operator ByteArrayApi.ByteArray(Variant v)
            {
                var byteArray = (ByteArrayApi.ByteArray)v.Value;
                if (byteArray != null)
                {
                    return (ByteArrayApi.ByteArray)v.Value;
                }
                else
                {
                    return new ByteArrayApi.ByteArray();
                }
            }

            public int Type => (int)Value.LSL_Type;
            private static string MapVariantType(Type t)
            {
                if(t == typeof(AnArray))
                {
                    return "list";
                }
                else if (t == typeof(AString))
                {
                    return "string";
                }
                else if (t == typeof(Real))
                {
                    return "float";
                }
                else if (t == typeof(LSLKey))
                {
                    return "key";
                }
                else if (t == typeof(Quaternion))
                {
                    return "rotation";
                }
                else if (t == typeof(Vector3))
                {
                    return "vector";
                }
                else if (t == typeof(LongInteger))
                {
                    return "long";
                }
                else if (t == typeof(Integer))
                {
                    return "int";
                }
                else if (t == typeof(ByteArrayApi.ByteArray))
                {
                    return "bytearray";
                }
                else
                {
                    return "???";
                }
            }

            public static AnArray operator +(AnArray v1, Variant v2) => new AnArray(v1) { v2.Value };

            public static AnArray operator +(Variant v1, AnArray v2)
            {
                var n = new AnArray { v1.Value };
                n.AddRange(v2);
                return n;
            }

            [APILevel(APIFlags.ASSL)]
            public static implicit operator AnArray(Variant v) => new AnArray { v.Value };

            public static Variant operator +(Variant v1, Variant v2)
            {
                Type t1 = v1.Value.GetType();
                Type t2 = v2.Value.GetType();
                if (t1 == typeof(AString) || t2 == typeof(AString) ||
                    t1 == typeof(LSLKey) || t2 == typeof(LSLKey))
                {
                    return (string)v1 + (string)v2;
                }
                else if (t1 == t2)
                {
                    if (t1 == typeof(Integer))
                    {
                        return v1.Value.AsInt + v2.Value.AsInt;
                    }
                    else if (t1 == typeof(LongInteger))
                    {
                        return v1.Value.AsLong + v2.Value.AsLong;
                    }
                    else if (t1 == typeof(Real))
                    {
                        return (double)v1.Value.AsReal + v2.Value.AsReal;
                    }
                    else if (t1 == typeof(Vector3))
                    {
                        return v1.Value.AsVector3 + v2.Value.AsVector3;
                    }
                    else if (t1 == typeof(Quaternion))
                    {
                        return v1.Value.AsQuaternion + v2.Value.AsQuaternion;
                    }
                    else if(t1 == typeof(AnArray))
                    {
                        return new Variant { Value = ((AnArray)v1.Value) + ((AnArray)v2.Value) };
                    }
                }
                else if(t1 == typeof(AnArray))
                {
                    return new Variant
                    {
                        Value = new AnArray((AnArray)v1.Value)
                        {
                            v2.Value
                        }
                    };
                }
                else if(t2 == typeof(AnArray))
                {
                    return new Variant { Value = v1 + (AnArray)v2 };
                }
                else if (t1 == typeof(Real) && t2 == typeof(Integer))
                {
                    return v1.Value.AsReal + v2.Value.AsInt;
                }
                else if (t1 == typeof(Real) && t2 == typeof(LongInteger))
                {
                    return v1.Value.AsReal + v2.Value.AsLong;
                }
                else if (t1 == typeof(Integer) && t2 == typeof(Real))
                {
                    return v1.Value.AsInt + v2.Value.AsReal;
                }
                else if (t1 == typeof(LongInteger) && t2 == typeof(Real))
                {
                    return v1.Value.AsLong + v2.Value.AsReal;
                }
                else if (t1 == typeof(LongInteger) && t2 == typeof(Integer))
                {
                    return v1.Value.AsLong + v2.Value.AsInt;
                }
                else if (t1 == typeof(Integer) && t2 == typeof(LongInteger))
                {
                    return v1.Value.AsInt + v2.Value.AsLong;
                }
                throw new LocalizedScriptErrorException(
                    new Variant(),
                    "IncompatibleTypes1And2ForOperator0",
                    "Incompatible types '{1}' and '{2}' at operator '{0}' at this-operator for type 'list'.",
                    "+=", MapVariantType(t1), MapVariantType(t2));
            }

            public static Variant operator -(Variant v1, Variant v2)
            {
                Type t1 = v1.Value.GetType();
                Type t2 = v2.Value.GetType();
                if (t1 == typeof(AString) || t2 == typeof(AString) ||
                    t1 == typeof(LSLKey) || t2 == typeof(LSLKey))
                {
                    /* intentionally left empty */
                }
                else if (t1 == t2)
                {
                    if (t1 == typeof(Integer))
                    {
                        return v1.Value.AsInt - v2.Value.AsInt;
                    }
                    else if (t1 == typeof(LongInteger))
                    {
                        return v1.Value.AsLong - v2.Value.AsLong;
                    }
                    else if (t1 == typeof(Real))
                    {
                        return (double)v1.Value.AsReal - v2.Value.AsReal;
                    }
                    else if (t1 == typeof(Vector3))
                    {
                        return v1.Value.AsVector3 - v2.Value.AsVector3;
                    }
                    else if (t1 == typeof(Quaternion))
                    {
                        return v1.Value.AsQuaternion - v2.Value.AsQuaternion;
                    }
                }
                else if (t1 == typeof(Real) && t2 == typeof(Integer))
                {
                    return v1.Value.AsReal - v2.Value.AsInt;
                }
                else if (t1 == typeof(Real) && t2 == typeof(LongInteger))
                {
                    return v1.Value.AsReal - v2.Value.AsLong;
                }
                else if (t1 == typeof(Integer) && t2 == typeof(Real))
                {
                    return v1.Value.AsInt - v2.Value.AsReal;
                }
                else if (t1 == typeof(LongInteger) && t2 == typeof(Real))
                {
                    return v1.Value.AsLong - v2.Value.AsReal;
                }
                else if (t1 == typeof(LongInteger) && t2 == typeof(Integer))
                {
                    return v1.Value.AsLong - v2.Value.AsInt;
                }
                else if (t1 == typeof(Integer) && t2 == typeof(LongInteger))
                {
                    return v1.Value.AsInt - v2.Value.AsLong;
                }
                throw new LocalizedScriptErrorException(
                    new Variant(),
                    "IncompatibleTypes1And2ForOperator0",
                    "Incompatible types '{1}' and '{2}' at operator '{0}' at this-operator for type 'list'.",
                    "-=", MapVariantType(t1), MapVariantType(t2));
            }

            public static Variant operator *(Variant v1, Variant v2)
            {
                Type t1 = v1.Value.GetType();
                Type t2 = v2.Value.GetType();
                if (t1 == typeof(AString) || t2 == typeof(AString) ||
                    t1 == typeof(LSLKey) || t2 == typeof(LSLKey))
                {
                    /* intentionally left empty */
                }
                else if (t1 == t2)
                {
                    if (t1 == typeof(Integer))
                    {
                        return LSLCompiler.LSL_IntegerMultiply(v1.Value.AsInt, v2.Value.AsInt);
                    }
                    else if (t1 == typeof(LongInteger))
                    {
                        return v1.Value.AsLong * v2.Value.AsLong;
                    }
                    else if (t1 == typeof(Real))
                    {
                        return (double)v1.Value.AsReal * v2.Value.AsReal;
                    }
                    else if (t1 == typeof(Vector3))
                    {
                        return v1.Value.AsVector3 * v2.Value.AsVector3;
                    }
                    else if (t1 == typeof(Quaternion))
                    {
                        return v1.Value.AsQuaternion * v2.Value.AsQuaternion;
                    }
                }
                else if (t1 == typeof(Real) && t2 == typeof(Integer))
                {
                    return v1.Value.AsReal * v2.Value.AsInt;
                }
                else if (t1 == typeof(Real) && t2 == typeof(LongInteger))
                {
                    return v1.Value.AsReal * v2.Value.AsLong;
                }
                else if (t1 == typeof(Integer) && t2 == typeof(Real))
                {
                    return v1.Value.AsInt * v2.Value.AsReal;
                }
                else if (t1 == typeof(LongInteger) && t2 == typeof(Real))
                {
                    return v1.Value.AsLong * v2.Value.AsReal;
                }
                else if (t1 == typeof(LongInteger) && t2 == typeof(Integer))
                {
                    return v1.Value.AsLong * v2.Value.AsInt;
                }
                else if (t1 == typeof(Integer) && t2 == typeof(LongInteger))
                {
                    return v1.Value.AsInt * v2.Value.AsLong;
                }
                else if (t1 == typeof(Vector3) && t2 == typeof(Quaternion))
                {
                    return v2.Value.AsVector3 * v1.Value.AsQuaternion;
                }
                else if (t1 == typeof(Vector3) && t2 == typeof(Integer))
                {
                    return v2.Value.AsVector3 * v1.Value.AsInt;
                }
                else if (t1 == typeof(Vector3) && t2 == typeof(LongInteger))
                {
                    return v2.Value.AsVector3 * v1.Value.AsLong;
                }
                else if (t1 == typeof(Vector3) && t2 == typeof(Real))
                {
                    return v2.Value.AsVector3 * v1.Value.AsReal;
                }
                else if (t1 == typeof(Quaternion) && t2 == typeof(Integer))
                {
                    return v2.Value.AsQuaternion * v1.Value.AsInt;
                }
                else if (t1 == typeof(Quaternion) && t2 == typeof(LongInteger))
                {
                    return v2.Value.AsQuaternion * v1.Value.AsLong;
                }
                else if (t1 == typeof(Quaternion) && t2 == typeof(Real))
                {
                    return v2.Value.AsQuaternion * v1.Value.AsReal;
                }
                throw new LocalizedScriptErrorException(
                    new Variant(),
                    "IncompatibleTypes1And2ForOperator0",
                    "Incompatible types '{1}' and '{2}' at operator '{0}' at this-operator for type 'list'.",
                    "*=", MapVariantType(t1), MapVariantType(t2));
            }

            public static Variant operator /(Variant v1, Variant v2)
            {
                Type t1 = v1.Value.GetType();
                Type t2 = v2.Value.GetType();
                if (t1 == typeof(AString) || t2 == typeof(AString) ||
                    t1 == typeof(LSLKey) || t2 == typeof(LSLKey))
                {
                    /* intentionally left empty */
                }
                else if (t1 == t2)
                {
                    if (t1 == typeof(Integer))
                    {
                        return LSLCompiler.LSL_IntegerDivision(v1.Value.AsInt, v2.Value.AsInt);
                    }
                    else if (t1 == typeof(LongInteger))
                    {
                        return v1.Value.AsLong / v2.Value.AsLong;
                    }
                    else if (t1 == typeof(Real))
                    {
                        return (double)v1.Value.AsReal / v2.Value.AsReal;
                    }
                    else if (t1 == typeof(Quaternion))
                    {
                        return LSLCompiler.LSLQuaternionDivision(v1.Value.AsQuaternion, v2.Value.AsQuaternion);
                    }
                }
                else if (t1 == typeof(Real) && t2 == typeof(Integer))
                {
                    return v1.Value.AsReal / v2.Value.AsInt;
                }
                else if (t1 == typeof(Real) && t2 == typeof(LongInteger))
                {
                    return v1.Value.AsReal / v2.Value.AsLong;
                }
                else if (t1 == typeof(Integer) && t2 == typeof(Real))
                {
                    return v1.Value.AsInt / v2.Value.AsReal;
                }
                else if (t1 == typeof(LongInteger) && t2 == typeof(Real))
                {
                    return v1.Value.AsLong / v2.Value.AsReal;
                }
                else if (t1 == typeof(LongInteger) && t2 == typeof(Integer))
                {
                    return v1.Value.AsLong / v2.Value.AsInt;
                }
                else if (t1 == typeof(Integer) && t2 == typeof(LongInteger))
                {
                    return v1.Value.AsInt / v2.Value.AsLong;
                }
                else if (t1 == typeof(Vector3) && t2 == typeof(Quaternion))
                {
                    return v2.Value.AsVector3 / v1.Value.AsQuaternion;
                }
                else if (t1 == typeof(Vector3) && t2 == typeof(Integer))
                {
                    return v2.Value.AsVector3 / v1.Value.AsInt;
                }
                else if (t1 == typeof(Vector3) && t2 == typeof(LongInteger))
                {
                    return v2.Value.AsVector3 / v1.Value.AsLong;
                }
                else if (t1 == typeof(Vector3) && t2 == typeof(Real))
                {
                    return v2.Value.AsVector3 / v1.Value.AsReal;
                }
                throw new LocalizedScriptErrorException(
                    new Variant(),
                    "IncompatibleTypes1And2ForOperator0",
                    "Incompatible types '{1}' and '{2}' at operator '{0}' at this-operator for type 'list'.",
                    "/=", MapVariantType(t1), MapVariantType(t2));
            }

            public static Variant operator %(Variant v1, Variant v2)
            {
                Type t1 = v1.Value.GetType();
                Type t2 = v2.Value.GetType();
                if (t1 == typeof(AString) || t2 == typeof(AString) ||
                    t1 == typeof(LSLKey) || t2 == typeof(LSLKey))
                {
                    /* intentionally left empty */
                }
                else if (t1 == t2)
                {
                    if (t1 == typeof(Integer))
                    {
                        return LSLCompiler.LSL_IntegerModulus(v1.Value.AsInt, v2.Value.AsInt);
                    }
                    else if (t1 == typeof(LongInteger))
                    {
                        return v1.Value.AsLong % v2.Value.AsLong;
                    }
                    else if (t1 == typeof(Real))
                    {
                        return (double)v1.Value.AsReal % v2.Value.AsReal;
                    }
                }
                else if (t1 == typeof(Real) && t2 == typeof(Integer))
                {
                    return v1.Value.AsReal % v2.Value.AsInt;
                }
                else if (t1 == typeof(Real) && t2 == typeof(LongInteger))
                {
                    return v1.Value.AsReal % v2.Value.AsLong;
                }
                else if (t1 == typeof(Integer) && t2 == typeof(Real))
                {
                    return v1.Value.AsInt % v2.Value.AsReal;
                }
                else if (t1 == typeof(LongInteger) && t2 == typeof(Real))
                {
                    return v1.Value.AsLong % v2.Value.AsReal;
                }
                else if (t1 == typeof(LongInteger) && t2 == typeof(Integer))
                {
                    return v1.Value.AsLong % v2.Value.AsInt;
                }
                else if (t1 == typeof(Integer) && t2 == typeof(LongInteger))
                {
                    return v1.Value.AsInt % v2.Value.AsLong;
                }
                throw new LocalizedScriptErrorException(
                    new Variant(),
                    "IncompatibleTypes1And2ForOperator0",
                    "Incompatible types '{1}' and '{2}' at operator '{0}' at this-operator for type 'list'.",
                    "%=", MapVariantType(t1), MapVariantType(t2));
            }
        }

        public sealed class HashtableEnumerator : IEnumerator<KeyValuePair<string, Variant>>
        {
            private readonly Hashtable Src;
            private readonly string[] Keys;
            private int Position = -1;

            public HashtableEnumerator(Hashtable src)
            {
                Src = src;
                Keys = src.KeysBase;
            }

            public KeyValuePair<string, Variant> Current
            {
                get
                {
                    string name = Keys[Position];
                    return new KeyValuePair<string, Variant>(name, Src[name]);
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext() => ++Position < Keys.Length;

            public void Reset() => Position = -1;
        }

        [APILevel(APIFlags.ASSL)]
        [APIDisplayName("hashtable")]
        [APIAccessibleMembers("Keys")]
        [APIIsVariableType]
        [APICloneOnAssignment]
        public sealed class Hashtable : Dictionary<string, IValue>
        {
            public Hashtable()
            {
            }

            public Hashtable(Hashtable src)
            {
                foreach(KeyValuePair<string, IValue> kvp in src)
                {
                    Type t = kvp.Value.GetType();
                    if(Attribute.GetCustomAttribute(t, typeof(APICloneOnAssignmentAttribute)) != null)
                    {
                        base[kvp.Key] = (IValue)Activator.CreateInstance(t, kvp.Value);
                    }
                    else
                    {
                        base[kvp.Key] = kvp.Value;
                    }
                }
            }

            public string[] KeysBase => base.Keys.ToArray();

            public new AnArray Keys
            {
                get
                {
                    var res = new AnArray();
                    foreach(string k in base.Keys)
                    {
                        res.Add(k);
                    }
                    return res;
                }
            }

            public new Variant this[string name]
            {
                get
                {
                    IValue val;
                    if(!TryGetValue(name, out val))
                    {
                        return new Variant(new Undef());
                    }
                    return new Variant(val);
                }

                set
                {
                    base[name] = value.Value;
                }
            }

            /* bypass override */
            public void SetEntry(string name, IValue iv)
            {
                base[name] = iv;
            }

            public HashtableEnumerator GetLslForeachEnumerator() => new HashtableEnumerator(this);
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.ASSL, "asHashSetString")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Set")]
        public void HashtableSet(Hashtable table, string key, string s)
        {
            table.SetEntry(key, new AString(s));
        }

        [APILevel(APIFlags.ASSL, "asHashSetList")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Set")]
        public void HashtableSet(Hashtable table, string key, AnArray s)
        {
            table.SetEntry(key, new AnArray(s));
        }

        [APILevel(APIFlags.ASSL, "asHashSetInteger")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Set")]
        public void HashtableSet(Hashtable table, string key, int s)
        {
            table.SetEntry(key, new Integer(s));
        }

        [APILevel(APIFlags.ASSL, "asHashSetLong")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Set")]
        public void HashtableSet(Hashtable table, string key, long s)
        {
            table.SetEntry(key, new LongInteger(s));
        }

        [APILevel(APIFlags.ASSL, "asHashSetFloat")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Set")]
        public void HashtableSet(Hashtable table, string key, double s)
        {
            table.SetEntry(key, new Real(s));
        }

        [APILevel(APIFlags.ASSL, "asHashSetVector")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Set")]
        public void HashtableSet(Hashtable table, string key, Vector3 s)
        {
            table.SetEntry(key, s);
        }

        [APILevel(APIFlags.ASSL, "asHashSetQuaternion")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Set")]
        public void HashtableSet(Hashtable table, string key, Quaternion s)
        {
            table.SetEntry(key, s);
        }

        [APILevel(APIFlags.ASSL, "asHashSetKey")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Set")]
        public void HashtableSet(Hashtable table, string key, LSLKey s)
        {
            table.SetEntry(key, new LSLKey(s));
        }

        [APILevel(APIFlags.ASSL, "asHashSetByteArray")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Set")]
        public void HashtableSet(Hashtable table, string key, ByteArrayApi.ByteArray s)
        {
            table.SetEntry(key, new ByteArrayApi.ByteArray(s));
        }

        [APILevel(APIFlags.ASSL, "asHash2String")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetString")]
        [Pure]
        public string Hash2String(ScriptInstance instance, Hashtable table, string key)
        {
            lock(instance)
            {
                var script = (Script)instance;
                IValue val;
                if(!table.TryGetValue(key, out val))
                {
                    return string.Empty;
                }

                Type t = val.GetType();
                if (t == typeof(Real))
                {
                    return script.UsesSinglePrecision ?
                        LSLCompiler.SinglePrecision.TypecastFloatToString(val.AsReal) :
                        LSLCompiler.TypecastDoubleToString(val.AsReal);
                }
                else if (t == typeof(Vector3))
                {
                    return script.UsesSinglePrecision ?
                        LSLCompiler.SinglePrecision.TypecastVectorToString6Places((Vector3)val) :
                        LSLCompiler.TypecastVectorToString6Places((Vector3)val);
                }
                else if (t == typeof(Quaternion))
                {
                    return script.UsesSinglePrecision ?
                        LSLCompiler.SinglePrecision.TypecastRotationToString6Places((Quaternion)val) :
                        LSLCompiler.TypecastRotationToString6Places((Quaternion)val);
                }
                else
                {
                    return val.ToString();
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asHash2Vector")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetVector")]
        [Pure]
        public Vector3 Hash2Vector(Hashtable table, string key)
        {
            IValue v;
            if(!table.TryGetValue(key, out v))
            {
                return Vector3.Zero;
            }

            try
            {
                return v.AsVector3;
            }
            catch
            {
                return Vector3.Zero;
            }
        }

        [APILevel(APIFlags.ASSL, "asHash2Rot")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetRot")]
        [Pure]
        public Quaternion Hash2Rot(Hashtable table, string key)
        {
            IValue v;
            if (!table.TryGetValue(key, out v))
            {
                return Quaternion.Identity;
            }

            try
            {
                return v.AsQuaternion;
            }
            catch
            {
                return Quaternion.Identity;
            }
        }

        [APILevel(APIFlags.ASSL, "asHash2Float")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetFloat")]
        [Pure]
        public double Hash2Float(Hashtable table, string key)
        {
            IValue v;
            if(!table.TryGetValue(key, out v))
            {
                return 0;
            }

            try
            {
                return v.AsReal;
            }
            catch
            {
                return 0;
            }
        }

        [APILevel(APIFlags.ASSL, "asHash2List")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetList")]
        [Pure]
        public AnArray Hash2List(Hashtable table, string key)
        {
            IValue v;
            if (!table.TryGetValue(key, out v))
            {
                return new AnArray();
            }

            if(v is AnArray)
            {
                return (AnArray)v;
            }
            else
            {
                return new AnArray
                {
                    v
                };
            }
        }

        [APILevel(APIFlags.ASSL, "asHash2Integer")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetInteger")]
        [Pure]
        public int Hash2Int(Hashtable table, string key)
        {
            IValue v;
            if(!table.TryGetValue(key, out v))
            {
                return 0;
            }
            if (v is Real)
            {
                return LSLCompiler.ConvToInt((Real)v);
            }
            else if (v is AString)
            {
                return LSLCompiler.ConvToInt(v.ToString());
            }
            else
            {
                try
                {
                    return v.AsInteger;
                }
                catch
                {
                    return 0;
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asHash2Long")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetLong")]
        [Pure]
        public long Hash2Long(Hashtable table, string key)
        {
            IValue v;
            if (!table.TryGetValue(key, out v))
            {
                return 0;
            }
            if (v is Real)
            {
                return LSLCompiler.ConvToLong((Real)v);
            }
            else if (v is AString)
            {
                return LSLCompiler.ConvToLong(v.ToString());
            }
            else
            {
                try
                {
                    return v.AsLong;
                }
                catch
                {
                    return 0;
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asHash2Key")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetKey")]
        [Pure]
        public LSLKey Hash2Key(ScriptInstance instance, Hashtable table, string key)
        {
            lock (instance)
            {
                Script script = (Script)instance;
                IValue val;
                if (!table.TryGetValue(key, out val))
                {
                    return new LSLKey();
                }

                Type t = val.GetType();
                if (t == typeof(Real))
                {
                    return script.UsesSinglePrecision ?
                        LSLCompiler.SinglePrecision.TypecastFloatToString(val.AsReal) :
                        LSLCompiler.TypecastDoubleToString(val.AsReal);
                }
                else if (t == typeof(Vector3))
                {
                    return script.UsesSinglePrecision ?
                        LSLCompiler.SinglePrecision.TypecastVectorToString6Places((Vector3)val) :
                        LSLCompiler.TypecastVectorToString6Places((Vector3)val);
                }
                else if (t == typeof(Quaternion))
                {
                    return script.UsesSinglePrecision ?
                        LSLCompiler.SinglePrecision.TypecastRotationToString6Places((Quaternion)val) :
                        LSLCompiler.TypecastRotationToString6Places((Quaternion)val);
                }
                else
                {
                    return val.ToString();
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asHash2GetByteArray")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetByteArray")]
        [Pure]
        public ByteArrayApi.ByteArray Hash2ByteArray(ScriptInstance instance, Hashtable table, string key)
        {
            lock (instance)
            {
                Script script = (Script)instance;
                IValue val;
                if (!table.TryGetValue(key, out val))
                {
                    return new ByteArrayApi.ByteArray();
                }

                Type t = val.GetType();
                if (t == typeof(ByteArrayApi.ByteArray))
                {
                    return (ByteArrayApi.ByteArray)val;
                }
                else
                {
                    return new ByteArrayApi.ByteArray();
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asHashContainsKey")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "ContainsKey")]
        [Pure]
        public int HashContainsKey(Hashtable h, string key) => h.ContainsKey(key).ToLSLBoolean();

        [APILevel(APIFlags.ASSL, "asHashRemove")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Remove")]
        public int HashRemoveKey(Hashtable h, string key) => h.Remove(key).ToLSLBoolean();
    }
}
