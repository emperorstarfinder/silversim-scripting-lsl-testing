﻿// SilverSim is distributed under the terms of the
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
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Hashtable
{
    [ScriptApiName("Hashtable")]
    [LSLImplementation]
    [PluginName("LSL_Hashtable")]
    [Description("ASSL Hashtable API")]
    public class HashtableApi : IPlugin, IScriptApi
    {
        [APILevel(APIFlags.ASSL)]
        [APIDisplayName("hashtable")]
        [APIAccessibleMembers("keys")]
        [APIIsVariableType]
        [APICloneOnAssignment]
        public class Hashtable : Dictionary<string, IValue>
        {
#pragma warning disable IDE1006 // Benennungsstile
            public AnArray keys
#pragma warning restore IDE1006 // Benennungsstile
            {
                get
                {
                    var res = new AnArray();
                    foreach(string k in Keys)
                    {
                        res.Add(k);
                    }
                    return res;
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.ASSL, "asHashSetString")]
        public void HashtableSet(ScriptInstance instance, Hashtable table, string key, string s)
        {
            table[key] = new AString(s);
        }

        [APILevel(APIFlags.ASSL, "asHashSetList")]
        public void HashtableSet(ScriptInstance instance, Hashtable table, string key, AnArray s)
        {
            table[key] = s;
        }

        [APILevel(APIFlags.ASSL, "asHashSetInteger")]
        public void HashtableSet(ScriptInstance instance, Hashtable table, string key, int s)
        {
            table[key] = new Integer(s);
        }

        [APILevel(APIFlags.ASSL, "asHashSetLong")]
        public void HashtableSet(ScriptInstance instance, Hashtable table, string key, long s)
        {
            table[key] = new LongInteger(s);
        }

        [APILevel(APIFlags.ASSL, "asHashSetFloat")]
        public void HashtableSet(ScriptInstance instance, Hashtable table, string key, double s)
        {
            table[key] = new Real(s);
        }

        [APILevel(APIFlags.ASSL, "asHashSetVector")]
        public void HashtableSet(ScriptInstance instance, Hashtable table, string key, Vector3 s)
        {
            table[key] = s;
        }

        [APILevel(APIFlags.ASSL, "asHashSetQuaternion")]
        public void HashtableSet(ScriptInstance instance, Hashtable table, string key, Quaternion s)
        {
            table[key] = s;
        }

        [APILevel(APIFlags.ASSL, "asHashSetKey")]
        public void HashtableSet(ScriptInstance instance, Hashtable table, string key, LSLKey s)
        {
            table[key] = s;
        }

        [APILevel(APIFlags.ASSL, "asHash2String")]
        public string Hash2String(ScriptInstance instance, Hashtable table, string key)
        {
            lock(instance)
            {
                Script script = (Script)instance;
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
        public Vector3 Hash2Vector(ScriptInstance instance, Hashtable table, string key)
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
        public Quaternion Hash2Rot(ScriptInstance instance, Hashtable table, string key)
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
        public double Hash2Float(ScriptInstance instance, Hashtable table, string key)
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
        public AnArray Hash2List(ScriptInstance instance, Hashtable table, string key)
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
        public int Hash2Int(ScriptInstance instance, Hashtable table, string key)
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
        public long Hash2Long(ScriptInstance instance, Hashtable table, string key)
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
    }
}