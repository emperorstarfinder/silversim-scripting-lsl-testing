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
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using JsonSerializer = SilverSim.Types.StructuredData.Json.Json;

namespace SilverSim.Scripting.Lsl.Api.Json
{
    [ScriptApiName("JSON")]
    [LSLImplementation]
    [Description("LSL/OSSL JSON API")]
    public class JsonApi : IScriptApi, IPlugin
    { 
        /* 
        excerpt from http://wiki.secondlife.com/wiki/Json_usage_in_LSL
        
        Type Conversions

        JSON has native data types that differ from LSL Types. Json Value types can be determined with llJsonValueType. 
        However, all Values retrieved from Json text will be a String and may require explicit conversion before being 
        used further (ie (float)"3.109000" and (vector)"<1.00000, 1.00000, 1.00000>").

        Number - JSON_NUMBER includes both LSL Integer and Float types (but not Inf and NaN, which must be explicitly converted 
            to String before encoding). NOTE: Float values will be converted as per the LSL string conversion rules- to 
            6 decimal place precision, with padded zeros or rounding used, except within vectors and rotations, where 
            5 decimal place precision results (ie 6.1 => 6.100000 and <1,1,1> => "<1.00000, 1.00000, 1.00000>").
        String - JSON_STRING equivalent to LSL String. NOTE: The LSL types Key, Rotation, and Vector will be converted to their 
            String representation when encoded within a Json text, either implicitly using llList2Json or explicitly before using 
            llJsonSetValue, and are always returned as a String when retrieved by llJsonGetValue. LSL strings which both begin and
            end with "\"" are interpreted literally as JSON strings, while those without are parsed when converted into JSON.
        Array - JSON_ARRAY similar to the LSL List. This is a bracket-enclosed group of Values which are separated with commas 
            ("[Value, Value, ... ]"). The Values are retrieved by use of a zero-based Index (NOTE: Negative indices are not supported!). 
            A Value may be an Array or an Object, allowing "nesting", and an empty Array ("[]") is allowed.
        Object - JSON_OBJECT similar to a Strided List with a stride of 2, this is a curly brace-enclosed group of comma-separated 
            "Key":Value pairs ("{"Key":Value, "Key":Value, ... }". The Values are retrieved by use of the "Key", which must be a String.
            A Value may be an Array or an Object, allowing "nesting", and an empty Object ("{}") is allowed.
            the Boolean constants true and false - JSON_TRUE and JSON_FALSE. NOTE: These are not to be confused with the LSL TRUE and FALSE 
            (which are overloaded integers) and they cannot be directly used in comparative testing (so, instead of if(jsonReturn), you must
            use if(jsonReturn == JSON_TRUE).
        the constant null - JSON_NULL which represents an empty, valueless placeholder and has no equivalent in LSL.
        JSON_INVALID - not a Value as such, but a possible return flag representing a failed operation within a Json text (such as trying to
            retrieve a Value from a non existent address within the text or attempting to set a Value in an Array with an out of bounds index). 
            This flag should be checked for whenever dealing with unknown Json text and when debugging your own manipulations to avoid 
            erroneous code operation.


        excerpt from http://wiki.secondlife.com/wiki/llJsonSetValue

        Returns, if successful, a new JSON text string which is json with the value indicated by the specifiers list set to value.
        
        If unsuccessful (usually because of specifying an out of range array index) it returns JSON_INVALID.

        An "out of range array index" is defined to be any Integer specifiers greater than the length of an existing array at that level
        within the Json text or greater than 0 (zero) at a level an array doesn't exist.

        A special specifiers, JSON_APPEND, is accepted which appends the value to the end of the array at the specifiers level. Care should
        be taken- if that level is not an array, the existing Value there will be overwritten and replaced with an array containing value 
        at it's first (0) index.

        Contrary to lists and strings, negative indexing of Json arrays is not supported.

        If an existing "Key" is specifiers at that level, its Value will be overwritten by value unless value is the magic value JSON_DELETE.
        If a value does not exist at specifiers, a new Key:Value pair will be formed within the Json object.

        To delete an existing value at specifiers, use JSON_DELETE as the value. Note it will not prune empty objects or arrays at higher levels.

        If value is JSON_TRUE, JSON_FALSE or JSON_NULL, the Value set will be the bare words 'true', 'false' or 'null', respectively, at the
        specifiers location within json.
        */

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        /* the following constants are the correct LSL constants, see SL wiki 
          excerpt from http://wiki.secondlife.com/wiki/JSON_FALSE et al

            JSON_INVALID    U+FDDO   
            JSON_OBJECT     U+FDD1 
            JSON_ARRAY      U+FDD2 
            JSON_NUMBER     U+FDD3 
            JSON_STRING     U+FDD4 
            JSON_NULL       U+FDD5 
            JSON_TRUE       U+FDD6 
            JSON_FALSE      U+FDD7 
            JSON_DELETE     U+FDD8 
        */
        [APILevel(APIFlags.LSL)]
        public static readonly string JSON_ARRAY = ((char)0xFDD2).ToString();
        [APILevel(APIFlags.LSL)]
        public static readonly string JSON_OBJECT = ((char)0xFDD1).ToString();
        [APILevel(APIFlags.LSL)]
        public static readonly string JSON_INVALID = ((char)0xFDD0).ToString();
        [APILevel(APIFlags.LSL)]
        public static readonly string JSON_NUMBER = ((char)0xFDD3).ToString();
        [APILevel(APIFlags.LSL)]
        public static readonly string JSON_STRING = ((char)0xFDD4).ToString();
        [APILevel(APIFlags.LSL)]
        public static readonly string JSON_TRUE = ((char)0xFDD6).ToString();
        [APILevel(APIFlags.LSL)]
        public static readonly string JSON_FALSE = ((char)0xFDD7).ToString();
        [APILevel(APIFlags.LSL)]
        public static readonly string JSON_NULL = ((char)0xFDD5).ToString();
        [APILevel(APIFlags.LSL)]
        public static readonly string JSON_DELETE = ((char)0xFDD8).ToString();

        /* constant as per http://wiki.secondlife.com/wiki/JSON_APPEND */
        [APILevel(APIFlags.LSL)]
        public const int JSON_APPEND = -1;

        #region llJsonGetValue and llJsonValueType
        IValue FollowJsonPath(IValue json, AnArray specifiers)
        {
            int pos = 0;

            for(; pos < specifiers.Count; ++pos)
            {
                string spec = specifiers[pos].ToString();
                var m = json as Map;
                if(m != null)
                {
                    ++pos;
                    if (m.ContainsKey(spec))
                    {
                        json = m[spec];
                        continue;
                    }
                    else
                    {
                        return null;
                    }
                }
                var a = json as AnArray;
                if(a != null)
                {
                    ++pos;
                    int idx;
                    if (int.TryParse(spec, out idx))
                    {
                        if (a.Count > idx)
                        {
                            json = a[idx];
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }

                /* not a valid access */
                return null;
            }

            return json;
        }

        [APILevel(APIFlags.LSL, "llJsonGetValue")]
        public string JsonGetValue(ScriptInstance instance, string json, AnArray specifiers)
        {
            using (var ms = new MemoryStream(json.ToUTF8Bytes()))
            {
                IValue val = FollowJsonPath(JsonSerializer.Deserialize(ms), specifiers);
                if(val == null)
                {
                    return JSON_INVALID;
                }
                if(val is Undef)
                {
                    return JSON_NULL;
                }
                else if(val is ABoolean)
                {
                    return val.AsBoolean ? JSON_TRUE : JSON_FALSE;
                }
                else if((val is AnArray) || (val is Map))
                {
                    return JsonSerializer.Serialize(val);
                }
                return val.ToString();
            }
        }


        [APILevel(APIFlags.LSL, "llJsonValueType")]
        public string JsonValueType(ScriptInstance instance, string json, AnArray specifiers)
        {
            using (var ms = new MemoryStream(json.ToUTF8Bytes()))
            {
                IValue val = FollowJsonPath(JsonSerializer.Deserialize(ms), specifiers);

                if(val is AnArray)
                {
                    return JSON_ARRAY;
                }
                else if(val is ABoolean)
                {
                    return val.AsBoolean ? JSON_TRUE : JSON_FALSE;
                }
                else if((val is Integer) || (val is Real))
                {
                    return JSON_NUMBER;
                }
                else if(val is Map)
                {
                    return JSON_OBJECT;
                }
                else if((val is AString) || (val is UUID))
                {
                    return JSON_STRING;
                }
                else if(val is Undef)
                {
                    return JSON_NULL;
                }
                else
                {
                    return JSON_INVALID;
                }
            }
        }
        #endregion

        #region JSON List conversion
        [APILevel(APIFlags.LSL, "llList2Json")]
        public string List2Json(ScriptInstance instance, string type, AnArray values)
        {
            if (type == JSON_ARRAY)
            {
                return JsonSerializer.Serialize(values);
            }
            else if (type == JSON_OBJECT)
            {
                var m = new Map();
                if (values.Count % 2 != 0)
                {
                    return JSON_INVALID;
                }

                for (int i = 0; i < values.Count; i += 2)
                {
                    m.Add(values[i].ToString(), values[i + 1]);
                }
                return JsonSerializer.Serialize(m);
            }
            else
            {
                return JSON_INVALID;
            }
        }

        [APILevel(APIFlags.LSL, "llJson2List")]
        public AnArray Json2List(ScriptInstance instance, string src)
        {
            IValue jsonData;
            var res = new AnArray();
            using (var ms = new MemoryStream(src.ToUTF8Bytes()))
            {
                jsonData = JsonSerializer.Deserialize(ms);
            }

            var array = jsonData as AnArray;
            if (null != array)
            {
                foreach (IValue val in array)
                {
                    res.Add(JsonSerializer.Serialize(val));
                }
                return res;
            }

            var m = jsonData as Map;
            if (null != m)
            {
                foreach (KeyValuePair<string, IValue> kvp in m)
                {
                    res.Add(kvp.Key);
                    res.Add(JsonSerializer.Serialize(kvp.Value));
                }
                return res;
            }

            if (jsonData is Undef)
            {
                res.Add(JSON_NULL);
                return res;
            }

            /* anything else */
            res.Add(jsonData);
            return res;
        }
        #endregion

        [APILevel(APIFlags.LSL, "llJsonSetValue")]
        public IValue String2Json(string value)
        {
            int i;
            double f;
            if(value == JSON_TRUE)
            {
                return new ABoolean(true);
            }
            if(value == JSON_FALSE)
            {
                return new ABoolean(false);
            }
            if(value == JSON_NULL)
            {
                return new Undef();
            }
            if(value.StartsWith("\"") && value.EndsWith("\""))
            {
                return new AString(value.Substring(1, value.Length - 2));
            }
            if(int.TryParse(value, out i))
            {
                return new Integer(i);
            }
            if(double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
            {
                return new Real(f);
            }
            return new AString(value);
        }

        interface LevelAssignment
        {
            IValue Value { get; set; }
            void Remove();
        }

        class LevelMapAssignment : LevelAssignment
        {
            readonly Map m_Map;
            readonly string m_Key;

            public LevelMapAssignment(Map m, string key)
            {
                m_Map = m;
                m_Key = key;
            }

            public IValue Value
            {
                get
                {
                    return m_Map[m_Key];
                }
                set
                {
                    m_Map[m_Key] = value;
                }
            }

            public void Remove()
            {
                m_Map.Remove(m_Key);
            }
        }

        class LevelArrayAssignment : LevelAssignment
        {
            readonly AnArray m_Array;
            readonly int m_Index;

            public LevelArrayAssignment(AnArray array, int index)
            {
                m_Array = array;
                m_Index = index;
            }

            public IValue Value
            {
                get
                {
                    return m_Array[m_Index];
                }
                set
                {
                    if (m_Array.Count <= m_Index)
                    {
                        m_Array.Add(value);
                    }
                    else
                    {
                        m_Array[m_Index] = value;
                    }
                }
            }

            public void Remove()
            {
                m_Array.RemoveAt(m_Index);
            }
        }

        public string JsonSetValue(ScriptInstance instance, string json, AnArray specifiers, string value)
        {
            IValue jsonData;
            LevelAssignment jsonLevel = null;
            IValue jsonLevelData = null;

            using (var ms = new MemoryStream(json.ToUTF8Bytes()))
            {
                jsonData = JsonSerializer.Deserialize(ms);
            }

            if(specifiers.Count == 0)
            {
                return JSON_INVALID;
            }

            for(int pos = 0; pos < specifiers.Count; ++pos)
            {
                IValue spec = specifiers[pos];
                if(spec is Integer)
                {
                    int index = spec.AsInt;
                    if(null == jsonLevel)
                    {
                        var a = new AnArray();
                        jsonData = a;
                        jsonLevel = new LevelArrayAssignment(a, 0);
                        jsonLevelData = a;
                    }
                    else
                    {
                        var a = jsonLevelData as AnArray;
                        if(a == null)
                        {
                            a = new AnArray();
                            jsonData = a;
                            jsonLevel.Value = a;
                            jsonLevelData = a;
                        }

                        jsonLevel = (index < 0 || index >= a.Count) ?
                             new LevelArrayAssignment(a, a.Count) :
                             new LevelArrayAssignment(a, index);
                    }
                }
                else if(spec is AString)
                {
                    string key = spec.ToString();
                    if(null == jsonLevel)
                    {
                        var m = new Map();
                        jsonData = m;
                        jsonLevel = new LevelMapAssignment(m, key);
                        jsonLevelData = m;
                    }
                    else
                    {
                        var m = jsonLevelData as Map;
                        if(m == null)
                        {
                            m = new Map();
                            jsonData = m;
                            jsonLevel.Value = m;
                            jsonLevelData = m;
                        }
                        jsonLevel = new LevelMapAssignment(m, key);
                    }
                }
                else
                {
                    return JSON_INVALID;
                }
            }

            if(null == jsonLevel)
            {
                return JSON_INVALID;
            }

            if(value == JSON_DELETE)
            {
                jsonLevel.Remove();
            }
            else
            {
                jsonLevel.Value = String2Json(value);
            }
            return JsonSerializer.Serialize(jsonData);
        }
    }
}
