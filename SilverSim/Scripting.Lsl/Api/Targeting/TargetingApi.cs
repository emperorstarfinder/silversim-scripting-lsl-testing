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

using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;

namespace SilverSim.Scripting.Lsl.Api.Targeting
{
    [ScriptApiName("Targeting")]
    [LSLImplementation]
    [Description("LSL Targeting API")]
    [ServerParam("LSL.MaxTargetsPerScript", ParameterType = typeof(uint), DefaultValue = 8)]
    [ServerParam("LSL.MaxRotTargetsPerScript", ParameterType = typeof(uint), DefaultValue = 8)]
    public class TargetingApi : IScriptApi, IPlugin, IPluginShutdown
    {
        public class AtTargetInfo
        {
            public Date Created;
            public UUID SceneID;
            public ObjectGroup ObjectGroup;
            public UUID PartID;
            public UUID ItemID;
            public Vector3 TargetPosition;
            public double Epsilon;

            public AtTargetInfo()
            {
                Created = Date.Now;
            }
        }

        public class RotTargetInfo
        {
            public Date Created;
            public UUID SceneID;
            public ObjectGroup ObjectGroup;
            public UUID PartID;
            public UUID ItemID;
            public Quaternion TargetRotation;
            public double Epsilon;

            public RotTargetInfo()
            {
                Created = Date.Now;
            }
        }

        public RwLockedDictionaryAutoAdd<UUID, RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<int, AtTargetInfo>>> m_AtTargets = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<int, AtTargetInfo>>>(() =>
            new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<int, AtTargetInfo>>(() => new RwLockedDictionary<int, AtTargetInfo>()));

        public RwLockedDictionaryAutoAdd<UUID, RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<int, RotTargetInfo>>> m_RotTargets = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<int, RotTargetInfo>>>(() =>
            new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<int, RotTargetInfo>>(() => new RwLockedDictionary<int, RotTargetInfo>()));

        private SceneList m_Scenes;
        public RwLockedDictionary<UUID, Timer> m_SceneTimers = new RwLockedDictionary<UUID, Timer>();

        private int m_NextAtTargetId;
        private int m_NextRotTargetId;
        private readonly object m_NextIdLock = new object();

        private int NextAtTargetId
        {
            get
            {
                lock(m_NextIdLock)
                {
                    if(++m_NextAtTargetId < 1)
                    {
                        m_NextAtTargetId = 1;
                    }
                    return m_NextAtTargetId;
                }
            }
        }

        private int NextRotTargetId
        {
            get
            {
                lock (m_NextIdLock)
                {
                    if (++m_NextRotTargetId < 1)
                    {
                        m_NextRotTargetId = 1;
                    }
                    return m_NextRotTargetId;
                }
            }
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_Scenes.OnRegionRemove += OnSceneRemove;
        }

        public void Shutdown()
        {
            if (m_Scenes != null)
            {
                m_Scenes.OnRegionRemove -= OnSceneRemove;
            }
        }

        private void OnSceneAdd(SceneInterface scene)
        {
            var t = new Timer(0.1); /* let's do 10Hz here */
            m_SceneTimers.Add(scene.ID, t);
            UUID sceneId = scene.ID;
            t.Elapsed += (object sender, ElapsedEventArgs args) => OnTimer(sender, sceneId);
            t.Start();
        }

        private void OnTimer(object sender, UUID sceneID)
        {
            SceneInterface scene;
            var removeList = new List<int>();
            if (m_Scenes.TryGetValue(sceneID, out scene))
            {
                var removeItems = new Dictionary<UUID, bool>();

                #region AtTargets
                RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<int, AtTargetInfo>> atTargets = m_AtTargets[sceneID];
                foreach(KeyValuePair<UUID, RwLockedDictionary<int, AtTargetInfo>> kvpOuter in atTargets)
                {
                    removeList.Clear();
                    foreach (KeyValuePair<int, AtTargetInfo> kvpInner in kvpOuter.Value)
                    {
                        try
                        {
                            AtTargetInfo info = kvpInner.Value;
                            Vector3 pos = info.ObjectGroup.GlobalPosition;
                            ObjectPart part;
                            ObjectPartInventoryItem item;

                            if(!scene.Primitives.TryGetValue(info.PartID, out part) ||
                                !part.Inventory.TryGetValue(info.ItemID, out item) ||
                                item.ScriptInstance == null)
                            {
                                removeList.Add(kvpInner.Key);
                                removeItems[info.ItemID] = true;
                            }
                            else if (pos.ApproxEquals(info.TargetPosition, info.Epsilon))
                            {
                                item.ScriptInstance.PostEvent(new AtTargetEvent
                                {
                                    Handle = kvpInner.Key,
                                    OurPosition = pos,
                                    TargetPosition = info.TargetPosition
                                });
                            }
                            else
                            {
                                item.ScriptInstance.PostEvent(new NotAtTargetEvent());
                            }
                        }
                        catch
                        {
                            removeList.Add(kvpInner.Key);
                        }
                    }
                    foreach(int k in removeList)
                    {
                        kvpOuter.Value.Remove(k);
                    }
                }
                #endregion

                #region RotTargets
                RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<int, RotTargetInfo>> rotTargets = m_RotTargets[sceneID];
                foreach (KeyValuePair<UUID, RwLockedDictionary<int, RotTargetInfo>> kvpOuter in rotTargets)
                {
                    removeList.Clear();
                    foreach (KeyValuePair<int, RotTargetInfo> kvpInner in kvpOuter.Value)
                    {
                        try
                        {
                            RotTargetInfo info = kvpInner.Value;
                            Quaternion rot = info.ObjectGroup.GlobalRotation;
                            ObjectPart part;
                            ObjectPartInventoryItem item;

                            if (!scene.Primitives.TryGetValue(info.PartID, out part) ||
                                !part.Inventory.TryGetValue(info.ItemID, out item) ||
                                item.ScriptInstance == null)
                            {
                                removeList.Add(kvpInner.Key);
                                removeItems[info.ItemID] = true;
                            }
                            else if (rot.ApproxEquals(info.TargetRotation, info.Epsilon))
                            {
                                item.ScriptInstance.PostEvent(new AtRotTargetEvent
                                {
                                    Handle = kvpInner.Key,
                                    OurRotation = rot,
                                    TargetRotation = info.TargetRotation
                                });
                            }
                            else
                            {
                                item.ScriptInstance.PostEvent(new NotAtRotTargetEvent());
                            }
                        }
                        catch
                        {
                            removeList.Add(kvpInner.Key);
                        }
                    }
                    foreach (int k in removeList)
                    {
                        kvpOuter.Value.Remove(k);
                    }
                }
                #endregion

                foreach (UUID id in removeItems.Keys)
                {
                    atTargets.Remove(id);
                    rotTargets.Remove(id);
                }
            }
            else
            {
                var t = sender as Timer;
                t?.Stop();
            }
        }

        private void OnSceneRemove(SceneInterface scene)
        {
            m_SceneTimers.RemoveIf(scene.ID, (Timer t) =>
            {
                t.Stop();
                t.Dispose();
                return true;
            });
            m_AtTargets.Remove(scene.ID);
            m_RotTargets.Remove(scene.ID);
        }

        private int GetMaxPosTargets(UUID regionID)
        {
            int value;
            if (m_MaxPosTargetHandleParams.TryGetValue(regionID, out value) ||
                m_MaxPosTargetHandleParams.TryGetValue(UUID.Zero, out value))
            {
                return value;
            }
            return 8;
        }

        private int GetMaxRotTargets(UUID regionID)
        {
            int value;
            if (m_MaxRotTargetHandleParams.TryGetValue(regionID, out value) ||
                m_MaxRotTargetHandleParams.TryGetValue(UUID.Zero, out value))
            {
                return value;
            }
            return 8;
        }

        private readonly RwLockedDictionary<UUID, int> m_MaxPosTargetHandleParams = new RwLockedDictionary<UUID, int>();
        private readonly RwLockedDictionary<UUID, int> m_MaxRotTargetHandleParams = new RwLockedDictionary<UUID, int>();

        [ServerParam("LSL.MaxTargetsPerScript")]
        public void MaxTargetsPerScriptUpdated(UUID regionID, string value)
        {
            int intval;
            if (value.Length == 0)
            {
                m_MaxPosTargetHandleParams.Remove(regionID);
            }
            else if (int.TryParse(value, out intval))
            {
                m_MaxPosTargetHandleParams[regionID] = intval;
            }
        }

        [ServerParam("LSL.MaxRotTargetsPerScript")]
        public void MaxRotTargetsPerScriptUpdated(UUID regionID, string value)
        {
            int intval;
            if (value.Length == 0)
            {
                m_MaxRotTargetHandleParams.Remove(regionID);
            }
            else if (int.TryParse(value, out intval))
            {
                m_MaxRotTargetHandleParams[regionID] = intval;
            }
        }

        [APILevel(APIFlags.LSL, "at_rot_target")]
        [StateEventDelegate]
        public delegate void State_at_rot_target(int handle, Quaternion targetrot, Quaternion ourrot);

        [APILevel(APIFlags.LSL, "at_target")]
        [StateEventDelegate]
        public delegate void State_at_target(int tnum, Vector3 targetpos, Vector3 ourpos);

        [APILevel(APIFlags.LSL, "not_at_rot_target")]
        [StateEventDelegate]
        public delegate void State_not_at_rot_target();

        [APILevel(APIFlags.LSL, "not_at_target")]
        [StateEventDelegate]
        public delegate void State_not_at_target();

        [APILevel(APIFlags.LSL, "llTarget")]
        public int Target(ScriptInstance instance, Vector3 position, double range)
        {
            lock(instance)
            {
                ObjectPartInventoryItem item = instance.Item;
                ObjectPart part = instance.Part;
                ObjectGroup grp = part.ObjectGroup;
                SceneInterface scene = grp.Scene;

                var info = new AtTargetInfo
                {
                    ObjectGroup = grp,
                    SceneID = scene.ID,
                    PartID = part.ID,
                    ItemID = item.ID,
                    TargetPosition = position,
                    Epsilon = range
                };
                RwLockedDictionary<int, AtTargetInfo> targetList = m_AtTargets[info.SceneID][info.ItemID];
                while(targetList.Count >= GetMaxPosTargets(info.SceneID))
                {
                    int oldest = -1;
                    Date oldestDate = Date.Now;
                    foreach(KeyValuePair<int, AtTargetInfo> kvp in targetList)
                    {
                        if(oldestDate.AsULong > kvp.Value.Created.AsULong)
                        {
                            oldest = kvp.Key;
                        }
                    }
                    if(!targetList.Remove(oldest))
                    {
                        break;
                    }
                }
                int newHandle = NextAtTargetId;
                while(targetList.ContainsKey(newHandle))
                {
                    newHandle = NextAtTargetId;
                }

                targetList.Add(newHandle, info);
                return newHandle;
            }
        }

        [APILevel(APIFlags.LSL, "llTargetRemove")]
        public void TargetRemove(ScriptInstance instance, int handle)
        {
            lock(instance)
            {
                ObjectPartInventoryItem item = instance.Item;
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                m_AtTargets[scene.ID][item.ID].Remove(handle);
                m_AtTargets[scene.ID].RemoveIf(item.ID, (RwLockedDictionary<int, AtTargetInfo> k) => k.Count == 0);
            }
        }

        [APILevel(APIFlags.LSL, "llRotTarget")]
        public int RotTarget(ScriptInstance instance, Quaternion rot, double error)
        {
            lock (instance)
            {
                ObjectPartInventoryItem item = instance.Item;
                ObjectPart part = instance.Part;
                ObjectGroup grp = part.ObjectGroup;
                SceneInterface scene = grp.Scene;

                var info = new RotTargetInfo
                {
                    ObjectGroup = grp,
                    SceneID = scene.ID,
                    PartID = part.ID,
                    ItemID = item.ID,
                    TargetRotation = rot,
                    Epsilon = error
                };
                RwLockedDictionary<int, RotTargetInfo> targetList = m_RotTargets[info.SceneID][info.ItemID];
                while (targetList.Count >= GetMaxRotTargets(info.SceneID))
                {
                    int oldest = -1;
                    Date oldestDate = Date.Now;
                    foreach (KeyValuePair<int, RotTargetInfo> kvp in targetList)
                    {
                        if (oldestDate.AsULong > kvp.Value.Created.AsULong)
                        {
                            oldest = kvp.Key;
                        }
                    }
                    if(!targetList.Remove(oldest))
                    {
                        break;
                    }
                }
                int newHandle = NextAtTargetId;
                while (targetList.ContainsKey(newHandle))
                {
                    newHandle = NextAtTargetId;
                }

                targetList.Add(newHandle, info);
                return newHandle;
            }
        }

        [APILevel(APIFlags.LSL, "llRotTargetRemove")]
        public void RotTargetRemove(ScriptInstance instance, int handle)
        {
            lock (instance)
            {
                ObjectPartInventoryItem item = instance.Item;
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                m_RotTargets[scene.ID][item.ID].Remove(handle);
                m_RotTargets[scene.ID].RemoveIf(item.ID, (RwLockedDictionary<int, RotTargetInfo> k) => k.Count == 0);
            }
        }

        [ExecutedOnScriptReset]
        [ExecutedOnScriptRemove]
        public void ScriptResetOrRemove(ScriptInstance instance)
        {
            UUID itemid;
            lock (instance)
            {
                itemid = instance.Item.ID;
                m_RotTargets.Remove(itemid);
                m_AtTargets.Remove(itemid);
            }
        }

        /* yes, original LSL serializes targets and it makes sense to have it that way */
        [ExecutedOnDeserialization("target")]
        public void DeserializeAtTarget(ScriptInstance instance, List<object> args)
        {
            if (args.Count < 4)
            {
                return;
            }
            var script = (Script)instance;
            lock (script)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<int, AtTargetInfo>> atTargets;
                if (m_AtTargets.TryGetValue(scene.ID, out atTargets))
                {
                    var handle = (int)args[0];
                    var info = new AtTargetInfo
                    {
                        SceneID = scene.ID,
                        PartID = instance.Part.ID,
                        ObjectGroup = grp,
                        ItemID = instance.Item.ID,
                        Created = Date.UnixTimeToDateTime((ulong)args[1]),
                        TargetPosition = (Vector3)args[2],
                        Epsilon = (double)args[3]
                    };
                    atTargets[info.ItemID][handle] = info;
                }
            }
        }

        [ExecutedOnSerialization("target")]
        public void SerializeAtTarget(ScriptInstance instance, List<object> res)
        {
            var script = (Script)instance;
            lock (script)
            {
                SceneInterface scene;
                try
                {
                    scene = instance.Part.ObjectGroup.Scene;
                }
                catch
                {
                    /* do not try serialization on remove */
                    return;
                }
                RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<int, AtTargetInfo>> atTargets;
                if (m_AtTargets.TryGetValue(scene.ID, out atTargets))
                {
                    RwLockedDictionary<int, AtTargetInfo> infos;
                    if (atTargets.TryGetValue(instance.Item.ID, out infos))
                    {
                        foreach (KeyValuePair<int, AtTargetInfo> kvp in infos)
                        {
                            res.Add("target");
                            res.Add(4);

                            res.Add(kvp.Key);
                            res.Add(kvp.Value.Created.AsULong);
                            res.Add(kvp.Value.TargetPosition);
                            res.Add(kvp.Value.Epsilon);
                        }
                    }
                }
            }
        }

        /* yes, original LSL serializes rotation targets and it makes sense to have it that way */
        [ExecutedOnDeserialization("rot_target")]
        public void DeserializeRotTarget(ScriptInstance instance, List<object> args)
        {
            if (args.Count < 4)
            {
                return;
            }
            var script = (Script)instance;
            lock (script)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<int, RotTargetInfo>> rotTargets;
                if (m_RotTargets.TryGetValue(scene.ID, out rotTargets))
                {
                    var handle = (int)args[0];
                    var info = new RotTargetInfo
                    {
                        SceneID = scene.ID,
                        PartID = instance.Part.ID,
                        ObjectGroup = grp,
                        ItemID = instance.Item.ID,
                        Created = Date.UnixTimeToDateTime((ulong)args[1]),
                        TargetRotation = (Quaternion)args[2],
                        Epsilon = (double)args[3]
                    };
                    rotTargets[info.ItemID][handle] = info;
                }
            }
        }

        [ExecutedOnSerialization("rot_target")]
        public void SerializeRotTarget(ScriptInstance instance, List<object> res)
        {
            var script = (Script)instance;
            lock (script)
            {
                SceneInterface scene;
                try
                {
                    scene = instance.Part.ObjectGroup.Scene;
                }
                catch
                {
                    /* do not try serialization on remove */
                    return;
                }
                RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<int, RotTargetInfo>> rotTargets;
                if (m_RotTargets.TryGetValue(scene.ID, out rotTargets))
                {
                    RwLockedDictionary<int, RotTargetInfo> infos;
                    if (rotTargets.TryGetValue(instance.Item.ID, out infos))
                    {
                        foreach (KeyValuePair<int, RotTargetInfo> kvp in infos)
                        {
                            res.Add("rot_target");
                            res.Add(4);

                            res.Add(kvp.Key);
                            res.Add(kvp.Value.Created.AsULong);
                            res.Add(kvp.Value.TargetRotation);
                            res.Add(kvp.Value.Epsilon);
                        }
                    }
                }
            }
        }
    }
}
