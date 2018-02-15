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

using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Experience;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scripting.Lsl.Api.Experience
{
    public sealed partial class ExperienceApi
    {
        [APILevel(APIFlags.LSL, "llCreateKeyValue")]
        public LSLKey CreateKeyValue(ScriptInstance instance, string k, string v)
        {
            lock(instance)
            {
                ExperienceServiceInterface experienceService = instance.Part.ObjectGroup.Scene.ExperienceService;
                UUID experienceId = instance.Item.ExperienceID;
                UUID queryid = CheckExperienceStatus(instance, experienceService, experienceId);
                if(queryid != UUID.Zero)
                {
                    return queryid;
                }

                try
                {
                    experienceService.KeyValueStore.Add(experienceId, k, v);
                }
                catch
                {
                    return SendExperienceError(instance, XP_ERROR_STORAGE_EXCEPTION);
                }
                var e = new DataserverEvent
                {
                    QueryID = UUID.Random,
                    Data = string.Format("1,{0}", v)
                };
                instance.PostEvent(e);
                return e.QueryID;
            }
        }

        [APILevel(APIFlags.LSL, "llDataSizeKeyValue")]
        public LSLKey DataSizeKeyValue(ScriptInstance instance)
        {
            lock(instance)
            {
                ExperienceServiceInterface experienceService = instance.Part.ObjectGroup.Scene.ExperienceService;
                UUID experienceId = instance.Item.ExperienceID;
                UUID queryid = CheckExperienceStatus(instance, experienceService, experienceId);
                if (queryid != UUID.Zero)
                {
                    return queryid;
                }

                int used;
                int quota;
                bool s;
                try
                {
                    s = experienceService.KeyValueStore.GetDatasize(experienceId, out used, out quota);
                    if(!s)
                    {
                        return SendExperienceError(instance, XP_ERROR_STORAGE_EXCEPTION);
                    }
                }
                catch
                {
                    return SendExperienceError(instance, XP_ERROR_STORAGE_EXCEPTION);
                }

                var e = new DataserverEvent
                {
                    QueryID = UUID.Random,
                    Data = string.Format("1,{0},{1}", used, quota)
                };
                instance.PostEvent(e);
                return e.QueryID;
            }
        }

        [APILevel(APIFlags.LSL, "llDeleteKeyValue")]
        public LSLKey DeleteKeyValue(ScriptInstance instance, string k)
        {
            lock (instance)
            {
                ExperienceServiceInterface experienceService = instance.Part.ObjectGroup.Scene.ExperienceService;
                UUID experienceId = instance.Item.ExperienceID;
                UUID queryid = CheckExperienceStatus(instance, experienceService, experienceId);
                if (queryid != UUID.Zero)
                {
                    return queryid;
                }

                bool s;
                try
                {
                    s = experienceService.KeyValueStore.Remove(experienceId, k);
                }
                catch
                {
                    return SendExperienceError(instance, XP_ERROR_STORAGE_EXCEPTION);
                }

                if (!s)
                {
                    return SendExperienceError(instance, XP_ERROR_STORAGE_EXCEPTION);
                }

                var e = new DataserverEvent
                {
                    QueryID = UUID.Random,
                    Data = "1"
                };
                instance.PostEvent(e);
                return e.QueryID;
            }
        }

        [APILevel(APIFlags.LSL, "llKeyCountKeyValue")]
        public LSLKey KeyCountKeyValue(ScriptInstance instance)
        {
            lock (instance)
            {
                ExperienceServiceInterface experienceService = instance.Part.ObjectGroup.Scene.ExperienceService;
                UUID experienceId = instance.Item.ExperienceID;
                UUID queryid = CheckExperienceStatus(instance, experienceService, experienceId);
                if (queryid != UUID.Zero)
                {
                    return queryid;
                }

                List<string> keys;

                try
                {
                    keys = experienceService.KeyValueStore.GetKeys(experienceId);
                }
                catch
                {
                    return SendExperienceError(instance, XP_ERROR_STORAGE_EXCEPTION);
                }

                var e = new DataserverEvent
                {
                    QueryID = UUID.Random,
                    Data = string.Format("1,{0}", keys.Count)
                };
                instance.PostEvent(e);
                return e.QueryID;
            }
        }

        [APILevel(APIFlags.LSL, "llKeysKeyValue")]
        public LSLKey KeysKeyValue(ScriptInstance instance, int first, int count)
        {
            lock(instance)
            {
                ExperienceServiceInterface experienceService = instance.Part.ObjectGroup.Scene.ExperienceService;
                UUID experienceId = instance.Item.ExperienceID;
                UUID queryid = CheckExperienceStatus(instance, experienceService, experienceId);
                if (queryid != UUID.Zero)
                {
                    return queryid;
                }

                List<string> keys;

                try
                {
                    keys = experienceService.KeyValueStore.GetKeys(experienceId);
                }
                catch
                {
                    return SendExperienceError(instance, XP_ERROR_STORAGE_EXCEPTION);
                }

                if (first >= keys.Count)
                {
                    return SendExperienceError(instance, XP_ERROR_KEY_NOT_FOUND);
                }

                if(count < 0)
                {
                    count = 0;
                }
                else if(count + first > keys.Count)
                {
                    count = keys.Count - first;
                }

                var e = new DataserverEvent
                {
                    QueryID = UUID.Random,
                    Data = string.Format("1,{0}", string.Join(",", keys.GetRange(first, count)))
                };
                instance.PostEvent(e);
                return e.QueryID;
            }
        }

        [APILevel(APIFlags.LSL, "llReadKeyValue")]
        public LSLKey ReadKeyValue(ScriptInstance instance, string k)
        {
            lock(instance)
            {
                ExperienceServiceInterface experienceService = instance.Part.ObjectGroup.Scene.ExperienceService;
                UUID experienceId = instance.Item.ExperienceID;
                UUID queryid = CheckExperienceStatus(instance, experienceService, experienceId);
                if (queryid != UUID.Zero)
                {
                    return queryid;
                }

                string val;
                bool s;

                try
                {
                    s = experienceService.KeyValueStore.TryGetValue(experienceId, k, out val);
                }
                catch
                {
                    return SendExperienceError(instance, XP_ERROR_STORAGE_EXCEPTION);
                }

                if (s)
                {
                    var e = new DataserverEvent
                    {
                        QueryID = UUID.Random,
                        Data = string.Format("1,{0}", val)
                    };
                    instance.PostEvent(e);
                    return e.QueryID;
                }
                else
                {
                    return SendExperienceError(instance, XP_ERROR_KEY_NOT_FOUND);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llUpdateKeyValue")]
        public LSLKey UpdateKeyValue(ScriptInstance instance, string k, string v, int checked_orig, string original_value)
        {
            lock (instance)
            {
                ExperienceServiceInterface experienceService = instance.Part.ObjectGroup.Scene.ExperienceService;
                UUID experienceId = instance.Item.ExperienceID;
                UUID queryid = CheckExperienceStatus(instance, experienceService, experienceId);
                if (queryid != UUID.Zero)
                {
                    return queryid;
                }

                bool changed;
                try
                {
                    if (checked_orig != 0)
                    {
                        changed = experienceService.KeyValueStore.StoreOnlyIfEqualOrig(experienceId, k, v, original_value);
                        if(!changed)
                        {
                            return SendExperienceError(instance, XP_ERROR_RETRY_UPDATE);
                        }
                    }
                    else
                    {
                        experienceService.KeyValueStore.Store(experienceId, k, v);
                    }
                }
                catch
                {
                    return SendExperienceError(instance, XP_ERROR_STORAGE_EXCEPTION);
                }

                var e = new DataserverEvent
                {
                    QueryID = UUID.Random,
                    Data = string.Format("1,{0}", v)
                };
                instance.PostEvent(e);
                return e.QueryID;
            }
        }
    }
}
