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

#pragma warning disable IDE0018
#pragma warning disable RCS1029
#pragma warning disable RCS1163

using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl.Api.ByteString;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.ASSL, "variant")]
        [APIDisplayName("variant")]
        [ImplementsCustomTypecasts]
        [APIAccessibleMembers("Type")]
        public struct Variant
        {
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
                if(byteArray != null)
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
                if(t == typeof(AString))
                {
                    return "string";
                }
                else if(t == typeof(Real))
                {
                    return "float";
                }
                else if(t == typeof(LSLKey))
                {
                    return "key";
                }
                else if(t == typeof(Quaternion))
                {
                    return "rotation";
                }
                else if(t == typeof(Vector3))
                {
                    return "vector";
                }
                else if(t == typeof(LongInteger))
                {
                    return "long";
                }
                else if(t == typeof(Integer))
                {
                    return "int";
                }
                else if(t == typeof(ByteArrayApi.ByteArray))
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

            public static Variant operator+(Variant v1, Variant v2)
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
                    if(t1 == typeof(Integer))
                    {
                        return v1.Value.AsInt + v2.Value.AsInt;
                    }
                    else if(t1 == typeof(LongInteger))
                    {
                        return v1.Value.AsLong + v2.Value.AsLong;
                    }
                    else if(t1 == typeof(Real))
                    {
                        return (double)v1.Value.AsReal + v2.Value.AsReal;
                    }
                    else if(t1 == typeof(Vector3))
                    {
                        return v1.Value.AsVector3 + v2.Value.AsVector3;
                    }
                    else if (t1 == typeof(Quaternion))
                    {
                        return v1.Value.AsQuaternion + v2.Value.AsQuaternion;
                    }
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
                else if(t1 == typeof(LongInteger) && t2 == typeof(Integer))
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
                        return LSLCompiler.LSL_IntegerMultiply(v1.Value.AsInt,  v2.Value.AsInt);
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

        [APILevel(APIFlags.LSL, "llDeleteSubList")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "DeleteSubList")]
        [Description("Returns a list that is a copy of src but with the slice from start to end removed.")]
        [Pure]
        public AnArray DeleteSubList(
            [Description("source")]
            AnArray src,
            [Description("start index")]
            int start,
            [Description("end index")]
            int end)
        {
            if (start < 0)
            {
                start = src.Count + start;
            }
            if (end < 0)
            {
                end = src.Count + end;
            }

            if (start < 0)
            {
                start = 0;
            }
            else if (start > src.Count)
            {
                start = src.Count;
            }

            if (end < 0)
            {
                end = 0;
            }
            else if (end > src.Count)
            {
                end = src.Count;
            }

            if (start > end)
            {
                var res = new AnArray();
                for (int i = start; i <= end; ++i)
                {
                    res.Add(src[i]);
                }

                return res;
            }
            else
            {
                var res = new AnArray();

                for (int i = 0; i < start + 1; ++i)
                {
                    res.Add(src[i]);
                }

                for (int i = end; i < src.Count; ++i)
                {
                    res.Add(src[i]);
                }

                return res;
            }
        }

        [APILevel(APIFlags.LSL, "llList2ListStrided")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetStride")]
        [Description("Returns a list of all the entries in the strided list whose index is a multiple of stride in the range start to end.")]
        [Pure]
        public AnArray List2ListStrided(
            AnArray src,
            [Description("start index")]
            int start,
            [Description("end index")]
            int end,
            [Description("number of entries per stride, if less than 1 it is assumed to be 1")]
            int stride)
        {
            var result = new AnArray();
            var si = new int[2];
            var ei = new int[2];
            bool twopass = false;

            /*
             * First step is always to deal with negative indices
             */

            if (start < 0)
            {
                start = src.Count + start;
            }
            if (end < 0)
            {
                end = src.Count + end;
            }

            /*
             * Out of bounds indices are OK, just trim them accordingly
             */

            if (start > src.Count)
            {
                start = src.Count;
            }

            if (end > src.Count)
            {
                end = src.Count;
            }

            if (stride == 0)
            {
                stride = 1;
            }

            /*
             * There may be one or two ranges to be considered
             */

            if (start != end)
            {
                if (start <= end)
                {
                    si[0] = start;
                    ei[0] = end;
                }
                else
                {
                    si[1] = start;
                    ei[1] = src.Count;
                    si[0] = 0;
                    ei[0] = end;
                    twopass = true;
                }

                /*
                 * The scan always starts from the beginning of the
                 * source list, but members are only selected if they
                 * fall within the specified sub-range. The specified
                 * range values are inclusive.
                 * A negative stride reverses the direction of the
                 * scan producing an inverted list as a result.
                 */

                if (stride > 0)
                {
                    for (int i = 0; i < src.Count; i += stride)
                    {
                        if (i <= ei[0] && i >= si[0])
                        {
                            result.Add(src[i]);
                        }
                        if (twopass && i >= si[1] && i <= ei[1])
                        {
                            result.Add(src[i]);
                        }
                    }
                }
                else if (stride < 0)
                {
                    for (int i = src.Count - 1; i >= 0; i += stride)
                    {
                        if (i <= ei[0] && i >= si[0])
                        {
                            result.Add(src[i]);
                        }
                        if (twopass && i >= si[1] && i <= ei[1])
                        {
                            result.Add(src[i]);
                        }
                    }
                }
            }
            else
            {
                if (start % stride == 0)
                {
                    result.Add(src[start]);
                }
            }

            return result;
        }

        [APILevel(APIFlags.LSL, "llList2List")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Sublist")]
        [Pure]
        public AnArray List2List(AnArray src, int start, int end)
        {
            if (start < 0)
            {
                start = src.Count + start;
            }
            if (end < 0)
            {
                end = src.Count + end;
            }

            if (start < 0)
            {
                start = 0;
            }
            else if (start > src.Count)
            {
                start = src.Count;
            }

            if (end < 0)
            {
                end = 0;
            }
            else if (end > src.Count)
            {
                end = src.Count;
            }

            if (start <= end)
            {
                var res = new AnArray();
                for (int i = start; i <= end; ++i )
                {
                    res.Add(src[i]);
                }

                return res;
            }
            else
            {
                var res = new AnArray();

                for (int i = 0; i < end + 1; ++i)
                {
                    res.Add(src[i]);
                }

                for (int i = start; i < src.Count; ++i)
                {
                    res.Add(src[i]);
                }

                return res;
            }
        }

        [APILevel(APIFlags.LSL, "llList2Float")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetFloatAt")]
        [Pure]
        public double List2Float(AnArray src, int index)
        {
            if(index < 0)
            {
                index = src.Count - index;
            }

            if(index < 0 ||index >=src.Count)
            {
                return 0;
            }

            return src[index].LslConvertToFloat();
        }

        [APILevel(APIFlags.LSL, "llListInsertList")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Insert")]
        [Pure]
        public AnArray ListInsertList(AnArray dest, AnArray src, int index)
        {
            AnArray pref;
            AnArray suff;

            if (index < 0)
            {
                index += dest.Count;
                if (index < 0)
                {
                    index = 0;
                }
            }

            if (index != 0)
            {
                pref = List2List(dest, 0, index - 1);
                if (index < dest.Count)
                {
                    suff = List2List(dest, index, -1);
                    return pref + src + suff;
                }
                else
                {
                    return pref + src;
                }
            }
            else
            {
                if (index < dest.Count)
                {
                    suff = List2List(dest, index, -1);
                    return src + suff;
                }
                else
                {
                    return src;
                }
            }
        }

        private bool CompareListElement(IValue a, IValue b)
        {
            return a.GetType() == b.GetType() && a.Equals(b);
        }

        [APILevel(APIFlags.LSL, "llListFindList")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Find")]
        [Description("Returns the integer index of the first instance of test in src.")]
        [Pure]
        public int ListFindList(
            [Description("what to search in (haystack)")]
            AnArray src,
            [Description("what to search for (needle)")]
            AnArray test)
        {
            int index = -1;
            int length = src.Count - test.Count + 1;

            /* If either list is empty, do not match */
            if (src.Count != 0 && test.Count != 0)
            {
                for (int i = 0; i < length; i++)
                {
                    if (CompareListElement(src[i], test[0]))
                    {
                        int j;
                        for (j = 1; j < test.Count; j++)
                        {
                            if (!CompareListElement(src[i + j], test[j]))
                            {
                                break;
                            }
                        }

                        if (j == test.Count)
                        {
                            index = i;
                            break;
                        }
                    }
                }
            }

            return index;
        }

        [APIExtension(APIExtension.LongInteger, "llList2Long")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetLongAt")]
        [Description("Returns an integer that is at index in src")]
        [Pure]
        public long List2Long(
            [Description("List containing the element of interest")]
            AnArray src,
            [Description("Index of the element of interest.")]
            int index)
        {
            if (index < 0)
            {
                index = src.Count - index;
            }

            if (index < 0 || index >= src.Count)
            {
                return 0;
            }

            return src[index].LslConvertToLong();
        }

        [APILevel(APIFlags.LSL, "llList2Integer")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetIntegerAt")]
        [Description("Returns an integer that is at index in src")]
        [Pure]
        public int List2Integer(
            [Description("List containing the element of interest")]
            AnArray src,
            [Description("Index of the element of interest.")]
            int index)
        {
            if (index < 0)
            {
                index = src.Count - index;
            }

            if (index < 0 || index >= src.Count)
            {
                return 0;
            }

            return src[index].LslConvertToInt();
        }

        [APILevel(APIFlags.LSL, "llList2Key")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetKeyAt")]
        [Description("Returns a key that is at index in src")]
        [Pure]
        public LSLKey List2Key(ScriptInstance instance,
            [Description("List containing the element of interest")]
            AnArray src,
            [Description("Index of the element of interest.")]
            int index)
        {
            var script = (Script)instance;
            if (index < 0)
            {
                index = src.Count - index;
            }

            if (index < 0 || index >= src.Count)
            {
                return UUID.Zero;
            }

            return src[index].LslConvertToKey(script.UsesSinglePrecision);
        }

        [APILevel(APIFlags.LSL, "llList2Rot")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetRotAt")]
        [Description("Returns a rotation that is at index in src")]
        [Pure]
        public Quaternion List2Rot(
            [Description("List containing the element of interest")]
            AnArray src,
            [Description("Index of the element of interest.")]
            int index)
        {
            if (index < 0)
            {
                index = src.Count - index;
            }

            if (index < 0 || index >= src.Count)
            {
                return Quaternion.Identity;
            }

            return src[index].LslConvertToRot();
        }

        [APILevel(APIFlags.LSL, "llList2String")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetStringAt")]
        [Description("Returns a string that is at index in src")]
        [Pure]
        public string List2String(ScriptInstance instance,
            [Description("List containing the element of interest")]
            AnArray src,
            [Description("Index of the element of interest.")]
            int index)
        {
            var script = (Script)instance;
            if (index < 0)
            {
                index = src.Count - index;
            }

            if (index < 0 || index >= src.Count)
            {
                return string.Empty;
            }

            return src[index].LslConvertToString(script.UsesSinglePrecision);
        }

        [APILevel(APIFlags.LSL, "llList2Vector")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetVectorAt")]
        [Description("Returns a vector that is at index in src")]
        [Pure]
        public Vector3 List2Vector(
            [Description("List containing the element of interest")]
            AnArray src,
            [Description("Index of the element of interest.")]
            int index)
        {
            if (index < 0)
            {
                index = src.Count - index;
            }

            if (index < 0 || index >= src.Count)
            {
                return Vector3.Zero;
            }

            return src[index].LslConvertToVector();
        }

        [APILevel(APIFlags.LSL, "llDumpList2String")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "ToString")]
        [Description("Returns a string that is the list src converted to a string with separator between the entries.")]
        [Pure]
        public string DumpList2String(ScriptInstance instance, AnArray src, string separator)
        {
            var script = (Script)instance;
            var sb = new StringBuilder();

            foreach(IValue val in src)
            {
                if(sb.Length != 0)
                {
                    sb.Append(separator);
                }
                Type t = val.GetType();
                if (t == typeof(Real))
                {
                    sb.Append(script.UsesSinglePrecision ?
                        LSLCompiler.SinglePrecision.TypecastFloatToString(val.AsReal) :
                        LSLCompiler.TypecastDoubleToString(val.AsReal));
                }
                else if (t == typeof(Vector3))
                {
                    sb.Append(script.UsesSinglePrecision ?
                        LSLCompiler.SinglePrecision.TypecastVectorToString6Places((Vector3)val) :
                        LSLCompiler.TypecastVectorToString6Places((Vector3)val));
                }
                else if (t == typeof(Quaternion))
                {
                    sb.Append(script.UsesSinglePrecision ?
                        LSLCompiler.SinglePrecision.TypecastRotationToString6Places((Quaternion)val) :
                        LSLCompiler.TypecastRotationToString6Places((Quaternion)val));
                }
                else
                {
                    sb.Append(val.ToString());
                }
            }
            return sb.ToString();
        }

        [APILevel(APIFlags.LSL, "llList2CSV")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "ToCSV")]
        [Description("Returns a string of comma separated values taken in order from src.")]
        [Pure]
        public string List2CSV(ScriptInstance instance, AnArray src) => DumpList2String(instance, src, ", ");

        [APILevel(APIFlags.LSL)]
        public const int TYPE_INTEGER = 1;
        [APILevel(APIFlags.LSL)]
        public const int TYPE_FLOAT = 2;
        [APILevel(APIFlags.LSL)]
        public const int TYPE_STRING = 3;
        [APILevel(APIFlags.LSL)]
        public const int TYPE_KEY = 4;
        [APILevel(APIFlags.LSL)]
        public const int TYPE_VECTOR = 5;
        [APILevel(APIFlags.LSL)]
        public const int TYPE_ROTATION = 6;
        [APIExtension(APIExtension.LongInteger)]
        public const int TYPE_LONGINTEGER = 7;
        [APILevel(APIFlags.ASSL)]
        public const int TYPE_LIST = 8;
        [APILevel(APIFlags.LSL)]
        public const int TYPE_INVALID = 0;

        [APILevel(APIFlags.LSL, "llGetListEntryType")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetEntryTypeAt")]
        [Description("Returns the type (an integer) of the entry at index in src.")]
        [Pure]
        public int GetListEntryType(
            [Description("List containing the element of interest")]
            AnArray src,
            [Description("Index of the element of interest")]
            int index)
        {
            if (index < 0)
            {
                index = src.Count - index;
            }

            if (index < 0 || index >= src.Count)
            {
                return TYPE_INVALID;
            }

            return (int)src[index].LSL_Type;
        }

        [APILevel(APIFlags.LSL, "llGetListLength")]
        [Description("Returns an integer that is the number of elements in the list src")]
        public int GetListLength(AnArray src) => src.Count;

        private AnArray ParseString2List(string src, AnArray separators_raw, AnArray spacers_raw, bool keepNulls)
        {
            var spacers = new List<string>(from spacer in spacers_raw where spacer.LSL_Type == LSLValueType.String && spacer.ToString().Length != 0 select spacer.ToString());
            var separators = new List<string>(from separator in separators_raw where separator.LSL_Type == LSLValueType.String && separator.ToString().Length != 0 select separator.ToString());
            var res = new AnArray();

            int position = 0;
            string[] spacers_array = spacers.ToArray();
            string[] separators_array = separators.ToArray();
            while(position < src.Length)
            {
                if (separators_array == null)
                {
                    separators_array = separators_array.ToArray();
                }

                if(spacers_array == null)
                {
                    spacers_array = spacers.ToArray();
                }

                int lowestDelimIndex = src.Length;
                int lowestSpacerIndex = src.Length;
                int selectedDelimLength = 0;
                int selectedSpacerLength = 0;
                if (separators_array.Length != 0)
                {
                    foreach (string separator in separators_array)
                    {
                        int index = src.IndexOf(separator, position);
                        if (index < 0)
                        {
                            separators.Remove(separator);
                            separators_array = null;
                        }
                        else
                        {
                            lowestDelimIndex = Math.Min(index, lowestDelimIndex);
                            selectedDelimLength = separator.Length;
                        }
                    }
                }

                string spc = null;
                if (spacers_array.Length != 0)
                {
                    foreach (string spacer in spacers.ToArray())
                    {
                        int index = src.IndexOf(spacer, position);
                        if (index < 0)
                        {
                            spacers.Remove(spacer);
                            spacers_array = null;
                        }
                        else
                        {
                            lowestSpacerIndex = Math.Min(index, lowestSpacerIndex);
                            spc = spacer;
                            selectedSpacerLength = spacer.Length;
                        }
                    }
                }

                int lowestIndex;
                int selectedLength;

                if(lowestSpacerIndex < lowestDelimIndex)
                {
                    lowestIndex = lowestSpacerIndex;
                    selectedLength = selectedSpacerLength;
                }
                else
                {
                    lowestIndex = lowestDelimIndex;
                    selectedLength = selectedDelimLength;
                    spc = null;
                }

                if (keepNulls || lowestIndex > 0)
                {
                    string val = src.Substring(position, lowestDelimIndex - position);
                    res.Add(val);
                }

                position = lowestIndex + selectedLength;

                if (spc != null)
                {
                    res.Add(spc);
                }
            }

            return res;
        }

        [APILevel(APIFlags.LSL, "llParseString2List")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "ParseToList")]
        [Description("Returns a list that is src broken into a list of strings, discarding separators, keeping spacers, discards any null (empty string) values generated.")]
        [Pure]
        public AnArray ParseString2List(
            [Description("source string")]
            string src,
            [Description("separators to be discarded")]
            AnArray separators,
            [Description("spacers to be kept")]
            AnArray spacers) => ParseString2List(src, separators, spacers, false);

        [APILevel(APIFlags.LSL, "llParseStringKeepNulls")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "ParseToListKeepNulls")]
        [Description("Returns a list that is src broken into a list, discarding separators, keeping spacers, keeping any null values generated.")]
        [Pure]
        public AnArray ParseStringKeepNulls(
            [Description("source string")]
            string src,
            [Description("separators to be discarded")]
            AnArray separators,
            [Description("spacers to be kept")]
            AnArray spacers) => ParseString2List(src, separators, spacers, true);

        [APILevel(APIFlags.LSL, "llCSV2List")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "CSV2List")]
        [Description("This function takes a string of values separated by commas, and turns it into a list.")]
        [Pure]
        public AnArray CSV2List(string src)
        {
            bool wsconsume = true;
            bool inbracket = false;
            var ret = new AnArray();
            var value = new StringBuilder();

            foreach(char c in src)
            {
                switch(c)
                {
                    case ' ': case '\t':
                        if(wsconsume)
                        {
                            break;
                        }
                        value.Append(c);
                        break;

                    case '<':
                        inbracket = true;
                        value.Append(c);
                        break;

                    case '>':
                        inbracket = false;
                        value.Append(c);
                        break;

                    case ',':
                        if(inbracket)
                        {
                            value.Append(c);
                            break;
                        }

                        ret.Add(value.ToString());
                        value.Clear();
                        wsconsume = true;
                        break;

                    default:
                        wsconsume = false;
                        value.Append(c);
                        break;
                }
            }
            if (value.Length != 0)
            {
                ret.Add(value.ToString());
            }
            return ret;
        }

        [APILevel(APIFlags.LSL, "llListReplaceList")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Replace")]
        [Description("This function takes a string of values separated by commas, and turns it into a list.")]
        [Pure]
        public AnArray ListReplaceList(AnArray dest, AnArray src, int start, int end)
        {
            AnArray pref;

            // Note that although we have normalized, both
            // indices could still be negative.
            if (start < 0)
            {
                start += dest.Count;
            }

            if (end < 0)
            {
                end += dest.Count;
            }
            if (start <= end)
            {
                if (start > 0)
                {
                    pref = List2List(dest, 0, start - 1);

                    return (end + 1 < dest.Count) ?
                        (pref + src + List2List(dest, end + 1, -1)) :
                        (pref + src);
                }
                else if (start == 0)
                {
                    return (end + 1 < dest.Count) ?
                        (src + List2List(dest, end + 1, -1)) :
                        src;
                }
                else
                {
                    return (end + 1 < dest.Count) ?
                        List2List(dest, end + 1, -1) :
                        new AnArray();
                }
            }
            else
            {
                return List2List(dest, end + 1, start - 1) + src;
            }
        }

        private static int ElementCompare(IValue left, IValue right)
        {
            Type leftType = left.GetType();
            if (left.GetType() != right.GetType())
            {
                /* as per LSL behaviour, unequal types are considered equal */
                return 0;
            }

            if(leftType == typeof(LSLKey) || leftType == typeof(AString))
            {
                return string.CompareOrdinal(left.ToString(), right.ToString());
            }
            else if(leftType == typeof(Integer))
            {
                return Math.Sign(left.AsInt - right.AsInt);
            }
            else if(leftType == typeof(Real))
            {
                return Math.Sign(left.AsReal - right.AsReal);
            }
            else if(leftType == typeof(Vector3))
            {
                return Math.Sign(left.AsVector3.Length - right.AsVector3.Length);
            }
            else if(leftType == typeof(Quaternion))
            {
                return Math.Sign(left.AsQuaternion.Length - right.AsQuaternion.Length);
            }
            else
            {
                return 0;
            }
        }

        private static int ElementCompareDescending(IValue left, IValue right) => 0 - ElementCompare(left, right);

        [APILevel(APIFlags.LSL, "llListSort")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Sort")]
        [Description("Returns a list that is src sorted by stride.")]
        [Pure]
        public AnArray ListSort(
            [Description("List to be sorted")]
            AnArray src,
            [Description("number of entries per stride. If it is less than 1, it is assumed to be 1.")]
            int stride,
            [Description("If TRUE, the sort order is ascending. Otherwise, the order is descending.")]
            int ascending)
        {
            AnArray res;

            if (0 == src.Count)
            {
                return new AnArray();
            }

            Comparison<IValue> compare = (ascending == 1) ?
                ElementCompare :
                (Comparison<IValue>)ElementCompareDescending;

            IValue[] ret = src.ToArray();
            if(stride < 2)
            {
                bool homogenousTypeEntries = true;
                int index;
                Type firstEntryType = ret[0].GetType();
                for (index = 1; index < ret.Length; index++)
                {
                    if (firstEntryType != ret[index].GetType())
                    {
                        homogenousTypeEntries = false;
                        break;
                    }
                }

                if (homogenousTypeEntries)
                {
                    Array.Sort(ret, compare);
                    res = new AnArray();
                    res.AddRange(ret);
                    return res;
                }
            }

            int i;
            int j;
            int k;
            int n = ret.Length;

            /* a slow bubble-sort in unoptimized style see LSL wiki for the llSortList description */
            for (i = 0; i < (n - stride); i += stride)
            {
                for (j = i + stride; j < n; j += stride)
                {
                    if (compare(ret[i], ret[j]) > 0)
                    {
                        for (k = 0; k < stride; k++)
                        {
                            IValue tmp = ret[i + k];
                            ret[i + k] = ret[j + k];
                            ret[j + k] = tmp;
                        }
                    }
                }
            }

            res = new AnArray();
            res.AddRange(ret);
            return res;
        }

        [APILevel(APIFlags.LSL, "llListRandomize")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Randomize")]
        [Pure]
        public AnArray ListRandomize(AnArray src, int stride)
        {
            /* From LSL wiki:
             * When you want to randomize the position of every list element, specify a stride of 1. This is perhaps the setting most used.
             * If the stride is not a factor of the list length, the src list is returned. In other words, llGetListLength(src) % stride must be 0.
             * Conceptually, the algorithm selects llGetListLength(src) / stride buckets, and then for each bucket swaps in the contents with another b
             */
            AnArray result;
            var rand = new Random();

            int chunkcount;
            int[] chunks;

            if (stride <= 0)
            {
                stride = 1;
            }

            if (src.Count != stride && src.Count % stride == 0)
            {
                chunkcount = src.Count / stride;

                chunks = new int[chunkcount];

                for (int i = 0; i < chunkcount; i++)
                {
                    chunks[i] = i;
                }

                /* Knuth shuffle the chunk index */
                for (int i = chunkcount - 1; i >= 1; i--)
                {
                    /* Elect an unrandomized chunk to swap */
                    int index = rand.Next(i + 1);

                    /* and swap position with first unrandomized chunk */
                    int tmp = chunks[i];
                    chunks[i] = chunks[index];
                    chunks[index] = tmp;
                }

                result = new AnArray();

                /* Construct the randomized list */
                for (int i = 0; i < chunkcount; i++)
                {
                    result.AddRange(src.GetRange(chunks[i] * stride, stride));
                }
            }
            else
            {
                result = new AnArray(src);
            }

            return result;
        }

        [APILevel(APIFlags.LSL)]
        [Description("Calculates the range.")]
        public const int LIST_STAT_RANGE = 0;
        [APILevel(APIFlags.LSL)]
        [Description("Calculates the smallest number.")]
        public const int LIST_STAT_MIN = 1;
        [APILevel(APIFlags.LSL)]
        [Description("Calculates the largest number.")]
        public const int LIST_STAT_MAX = 2;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_MEAN = 3;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_MEDIAN = 4;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_STD_DEV = 5;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_SUM = 6;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_SUM_SQUARES = 7;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_NUM_COUNT = 8;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_GEOMETRIC_MEAN = 9;
        [APILevel(APIFlags.LSL)]
        public const int LIST_STAT_HARMONIC_MEAN = 100;

        [APILevel(APIFlags.LSL, "llListStatistics")]
        [Pure]
        public double ListStatistics(int operation, AnArray src)
        {
            switch(operation)
            {
                case LIST_STAT_RANGE: return ListRange(src);
                case LIST_STAT_MIN: return ListMin(src);
                case LIST_STAT_MAX: return ListMax(src);
                case LIST_STAT_MEAN: return ListMean(src);
                case LIST_STAT_MEDIAN: return ListMedian(src);
                case LIST_STAT_NUM_COUNT: return ListNumericLength(src);
                case LIST_STAT_STD_DEV: return ListStdDev(src);
                case LIST_STAT_SUM: return ListSum(src);
                case LIST_STAT_SUM_SQUARES: return ListSumSquares(src);
                case LIST_STAT_GEOMETRIC_MEAN: return ListGeometricMean(src);
                case LIST_STAT_HARMONIC_MEAN: return ListHarmonicMean(src);
                default: return 0;
            }
        }

        [APIExtension(APIExtension.MemberFunctions, "Index")]
        [Pure]
        public int Index(AnArray array, string value)
        {
            for(int idx = 0; idx < array.Count; ++idx)
            {
                IValue iv = array[idx];
                if(iv.Type == Types.ValueType.String && iv.ToString() == value)
                {
                    return idx;
                }
            }
            return -1;
        }

        [APIExtension(APIExtension.MemberFunctions, "Contains")]
        [Pure]
        public int Contains(AnArray array, string value) => (Index(array, value) >= 0).ToLSLBoolean();

        [APIExtension(APIExtension.MemberFunctions, "Index")]
        [Pure]
        public int Index(AnArray array, int value)
        {
            for (int idx = 0; idx < array.Count; ++idx)
            {
                IValue iv = array[idx];
                if (iv.Type == Types.ValueType.Integer && iv.AsInt == value)
                {
                    return idx;
                }
            }
            return -1;
        }

        [APIExtension(APIExtension.MemberFunctions, "Contains")]
        [Pure]
        public int Contains(AnArray array, int value) => (Index(array, value) >= 0).ToLSLBoolean();

        [APIExtension(APIExtension.MemberFunctions, "Index")]
        [Pure]
        public int Index(AnArray array, long value)
        {
            for (int idx = 0; idx < array.Count; ++idx)
            {
                IValue iv = array[idx];
                if (iv.Type == Types.ValueType.LongInteger && iv.AsInt == value)
                {
                    return idx;
                }
            }
            return -1;
        }

        [APIExtension(APIExtension.MemberFunctions, "Contains")]
        [Pure]
        public int Contains(AnArray array, long value) => (Index(array, value) >= 0).ToLSLBoolean();

        [APIExtension(APIExtension.MemberFunctions, "IndexOf")]
        [Pure]
        public int Index(AnArray array, double value)
        {
            for (int idx = 0; idx < array.Count; ++idx)
            {
                IValue iv = array[idx];
                if (iv.Type == Types.ValueType.Integer && iv.AsReal == value)
                {
                    return idx;
                }
            }
            return -1;
        }

        [APIExtension(APIExtension.MemberFunctions, "Contains")]
        [Pure]
        public int Contains(AnArray array, double value) => (Index(array, value) >= 0).ToLSLBoolean();

        [APIExtension(APIExtension.MemberFunctions, "IndexOf")]
        [Pure]
        public int Index(AnArray array, Vector3 value)
        {
            for (int idx = 0; idx < array.Count; ++idx)
            {
                IValue iv = array[idx];
                if (iv.Type == Types.ValueType.Vector && iv.AsVector3 == value)
                {
                    return idx;
                }
            }
            return -1;
        }

        [APIExtension(APIExtension.MemberFunctions, "Contains")]
        [Pure]
        public int Contains(AnArray array, Vector3 value) => (Index(array, value) >= 0).ToLSLBoolean();

        [APIExtension(APIExtension.MemberFunctions, "IndexOf")]
        [Pure]
        public int Index(AnArray array, Quaternion value)
        {
            for (int idx = 0; idx < array.Count; ++idx)
            {
                IValue iv = array[idx];
                if (iv.Type == Types.ValueType.Rotation && iv.AsQuaternion == value)
                {
                    return idx;
                }
            }
            return -1;
        }

        [APIExtension(APIExtension.MemberFunctions, "Contains")]
        [Pure]
        public int Contains(AnArray array, Quaternion value) => (Index(array, value) >= 0).ToLSLBoolean();

        [APIExtension(APIExtension.MemberFunctions, "IndexOf")]
        [Pure]
        public int Index(AnArray array, LSLKey key)
        {
            for (int idx = 0; idx < array.Count; ++idx)
            {
                IValue iv = array[idx];
                if (iv.Type == Types.ValueType.UUID && iv.ToString() == key.ToString())
                {
                    return idx;
                }
            }
            return -1;
        }

        [APIExtension(APIExtension.MemberFunctions, "Contains")]
        [Pure]
        public int Contains(AnArray array, LSLKey key) => (Index(array, key) >= 0).ToLSLBoolean();

        #region llListStatistics function implementation
        private bool IsValue(IValue iv, out double v)
        {
            switch (iv.LSL_Type)
            {
                case LSLValueType.Integer:
                case LSLValueType.Float:
                    v = iv.AsReal;
                    return true;

                case LSLValueType.String:
                    return double.TryParse(iv.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out v);

                default:
                    v = 0;
                    return false;
            }
        }

        private double ListMin(AnArray src)
        {
            double minimum = double.PositiveInfinity;
            double entry;

            for (int i = 0; i < src.Count; i++)
            {
                if(IsValue(src[i], out entry) &&
                    entry < minimum)
                {
                    minimum = entry;
                }
            }

            return minimum;
        }

        private double ListMax(AnArray src)
        {
            double maximum = double.NegativeInfinity;
            double entry;

            for (int i = 0; i < src.Count; i++)
            {
                if(IsValue(src[i], out entry) &&
                    entry > maximum)
                {
                    maximum = entry;
                }
            }

            return maximum;
        }

        private double ListRange(AnArray src)
        {
            double maximum = double.NegativeInfinity;
            double minimum = double.PositiveInfinity;
            double entry;

            for (int i = 0; i < src.Count; i++)
            {
                if (IsValue(src[i], out entry))
                {
                    if (entry > maximum)
                    {
                        maximum = entry;
                    }
                    if (entry < minimum)
                    {
                        minimum = entry;
                    }
                }
            }

            return maximum / minimum;
        }

        private int ListNumericLength(AnArray src)
        {
            int count = 0;
            double entry;

            for (int i = 0; i < src.Count; i++)
            {
                if (IsValue(src[i], out entry))
                {
                    count++;
                }
            }

            return count;
        }

        private double ListSum(AnArray src)
        {
            double sum = 0;
            double entry;

            for (int i = 0; i < src.Count; i++)
            {
                if (IsValue(src[i], out entry))
                {
                    sum += entry;
                }
            }

            return sum;
        }

        private double ListSumSquares(AnArray src)
        {
            double sum = 0;
            double entry;
            for (int i = 0; i < src.Count; i++)
            {
                if (IsValue(src[i], out entry))
                {
                    sum += entry * entry;
                }
            }
            return sum;
        }

        private double ListMean(AnArray src)
        {
            double sum = 0;
            double entry;
            int count = 0;

            for (int i = 0; i < src.Count; i++)
            {
                if (IsValue(src[i], out entry))
                {
                    sum += entry;
                    ++count;
                }
            }

            return sum / count;
        }

        private double ListMedian(AnArray src) => ListQi(src, 0.5);

        private double ListGeometricMean(AnArray src)
        {
            double ret = 1.0;
            int count = 0;
            double entry;

            for (int i = 0; i < src.Count; i++)
            {
                if (IsValue(src[i], out entry))
                {
                    ret *= entry;
                    ++count;
                }
            }
            return Math.Exp(Math.Log(ret) / count);
        }

        private double ListHarmonicMean(AnArray src)
        {
            double ret = 0.0;
            double entry;
            int count = 0;

            for (int i = 0; i < src.Count; i++)
            {
                if (IsValue(src[i], out entry))
                {
                    ret += 1.0 / entry;
                    ++count;
                }
            }

            return count / ret;
        }

        private double ListVariance(AnArray src)
        {
            double s = 0;
            int count = 0;
            double entry;
            double sum = 0;

            for (int i = 0; i < src.Count; i++)
            {
                if (IsValue(src[i], out entry))
                {
                    s += Math.Pow(entry, 2);
                    sum += entry;
                    ++count;
                }
            }
            return (s - (count * Math.Pow(sum / count, 2))) / (count - 1);
        }

        private double ListStdDev(AnArray src)
        {
            return Math.Sqrt(ListVariance(src));
        }

        private double[] NumericSort(AnArray src)
        {
            var resList = new List<double>();
            double entry;

            for (int i = 0; i < src.Count; i++)
            {
                if (IsValue(src[i], out entry))
                {
                    resList.Add(entry);
                }
            }

            double[] resArray = resList.ToArray();
            Array.Sort(resArray);
            return resArray;
        }

        private double ListQi(AnArray src, double i)
        {
            double[] j = NumericSort(src);

            return Math.Abs(Math.Ceiling(j.Length * i) - (j.Length * i)) < Double.Epsilon ?
                (j[(int)((j.Length * i) - 1)] + j[(int)(j.Length * i)]) / 2 :
                j[(int)(Math.Ceiling(j.Length * i)) - 1];
        }
        #endregion
    }
}
