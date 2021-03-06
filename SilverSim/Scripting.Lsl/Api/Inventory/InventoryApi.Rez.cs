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

#pragma warning disable IDE0018, IDE0019
#pragma warning disable RCS1029

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Scripting.Lsl.Api.Inventory
{
    [ServerParam("LSL.EnforceRezDistanceLimit", ParameterType = typeof(bool), DefaultValue = true)]
    [ServerParam("LSL.RezDistanceMeterLimit", ParameterType = typeof(uint), DefaultValue = 10)]
    public partial class InventoryApi
    {
        private readonly RwLockedDictionary<UUID, bool> m_EnforceRezDistanceLimitParams = new RwLockedDictionary<UUID, bool>();
        private readonly RwLockedDictionary<UUID, uint> m_RezDistanceMeterLimit = new RwLockedDictionary<UUID, uint>();

        [ServerParam("LSL.EnforceRezDistanceLimit")]
        public void EnforceRezDistanceLimitUpdated(UUID regionID, string value)
        {
            bool boolval;
            if (value.Length == 0)
            {
                m_EnforceRezDistanceLimitParams.Remove(regionID);
            }
            else if (bool.TryParse(value, out boolval))
            {
                m_EnforceRezDistanceLimitParams[regionID] = boolval;
            }
            else
            {
                m_EnforceRezDistanceLimitParams[regionID] = true;
            }
        }

        [ServerParam("LSL.RezDistanceMeterLimit")]
        public void RezDistanceMeterLimitUpdated(UUID regionID, string value)
        {
            uint uintval;
            if (value.Length == 0)
            {
                m_RezDistanceMeterLimit.Remove(regionID);
            }
            else if (uint.TryParse(value, out uintval))
            {
                m_RezDistanceMeterLimit[regionID] = uintval;
            }
        }

        private bool IsRezDistanceLimitEnforced(UUID regionID)
        {
            bool value;
            if (m_EnforceRezDistanceLimitParams.TryGetValue(regionID, out value) ||
                m_EnforceRezDistanceLimitParams.TryGetValue(UUID.Zero, out value))
            {
                return value;
            }
            return true;
        }

        private uint GetRezDistanceMeterLimit(UUID regionID)
        {
            uint value;
            if (m_RezDistanceMeterLimit.TryGetValue(regionID, out value) ||
                m_RezDistanceMeterLimit.TryGetValue(UUID.Zero, out value))
            {
                return value;
            }
            return 10;
        }

        private Vector3 CalculateGeometricCenter(List<ObjectGroup> groups)
        {
            if(groups.Count == 0)
            {
                return Vector3.Zero;
            }
            Vector3 aabbMin = groups[0].Position;
            Vector3 aabbMax = groups[0].Position;
            foreach (ObjectGroup grp in groups)
            {
                foreach (ObjectPart part in grp.Values)
                {
                    aabbMin = aabbMin.ComponentMin(part.GlobalPosition - part.Size / 2);
                    aabbMax = aabbMax.ComponentMax(part.GlobalPosition + part.Size / 2);
                }
            }
            return (aabbMax - aabbMin) / 2;
        }

        [APIExtension(APIExtension.RezAsync, "asLinkRezObjectAsync")]
        [CheckFunctionPermission("asRezObjectAsync")]
        public void LinkRezObjectAsynchronous(ScriptInstance instance, int link, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param) =>
            LinkRezObject(instance, link, inventory, pos, vel, rot, param, true);

        [APILevel(APIFlags.ASSL, "asLinkRezObject")]
        public void LinkRezObject(ScriptInstance instance, int link, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param) =>
            LinkRezObject(instance, link, inventory, pos, vel, rot, param, false);

        private void LinkRezObject(ScriptInstance instance, int link, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param, bool useAsync)
        {
            lock (instance)
            {
                List<ObjectGroup> groups;
                ObjectPart invpart;
                ObjectPart rezzingpart = instance.Part;
                SceneInterface scene = rezzingpart.ObjectGroup.Scene;
                UUID sceneid = scene.ID;
                bool removeinventory;
                ObjectPartInventoryItem item;
                if (IsRezDistanceLimitEnforced(sceneid) &&
                    (instance.Part.GlobalPosition - pos).Length > GetRezDistanceMeterLimit(sceneid))
                {
                    /* silent fail as per definition */
                    return;
                }
                if (instance.TryGetLink(link, out invpart) &&
                    instance.TryGetObjectInventory(invpart, inventory, out groups, out removeinventory, out item))
                {
                    pos += CalculateGeometricCenter(groups);

                    if (useAsync)
                    {
                        ThreadPool.QueueUserWorkItem((o) =>
                        {
                            RealRezObject(scene, instance.Item.AssetID, instance.Item.Owner, rezzingpart, groups, pos, vel, rot, param, item);
                        });
                        if (removeinventory)
                        {
                            rezzingpart.Inventory.Remove(item.ID);
                        }
                    }
                    else if (RealRezObject(scene, instance.Item.AssetID, instance.Item.Owner, rezzingpart, groups, pos, vel, rot, param, item) &&
                            removeinventory)
                    {
                        rezzingpart.Inventory.Remove(item.ID);
                    }
                }
            }
        }

        [APIExtension(APIExtension.RezAsync, "asRezObjectAsync")]
        [CheckFunctionPermission("asRezObjectAsync")]
        public void RezObjectAsynchronous(ScriptInstance instance, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param) =>
            RezObject(instance, inventory, pos, vel, rot, param, true);

        [APILevel(APIFlags.LSL, "llRezObject")]
        [ForcedSleep(0.1)]
        public void RezObject(ScriptInstance instance, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param) =>
            RezObject(instance, inventory, pos, vel, rot, param, false);

        private void RezObject(ScriptInstance instance, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param, bool useAsync)
        {
            lock(instance)
            {
                List<ObjectGroup> groups;
                ObjectPart rezzingpart = instance.Part;
                SceneInterface scene = rezzingpart.ObjectGroup.Scene;
                UUID sceneid = scene.ID;
                bool removeinventory;
                ObjectPartInventoryItem item;
                if(instance.TryGetObjectInventory(inventory, out groups, out removeinventory, out item))
                {
                    pos += CalculateGeometricCenter(groups);

                    if (IsRezDistanceLimitEnforced(sceneid) &&
                        (instance.Part.GlobalPosition - pos).Length > GetRezDistanceMeterLimit(sceneid))
                    {
                        /* silent fail as per definition */
                        return;
                    }

                    if(useAsync)
                    {
                        ThreadPool.QueueUserWorkItem((o) =>
                        {
                            if(!RealRezObject(scene, instance.Item.AssetID, instance.Item.Owner, rezzingpart, groups, pos, vel, rot, param, item))
                            {
                                m_Log.WarnFormat("Object {0} [{1}] at {2} tried to rez inventory {3} owned by {4}", rezzingpart.Name, rezzingpart.ID, rezzingpart.GlobalPosition, inventory, rezzingpart.Owner);
                            }
                        });
                        if (removeinventory)
                        {
                            rezzingpart.Inventory.Remove(item.ID);
                        }
                    }
                    else if (!RealRezObject(scene, instance.Item.AssetID, instance.Item.Owner, rezzingpart, groups, pos, vel, rot, param, item))
                    {
                        m_Log.WarnFormat("Object {0} [{1}] at {2} tried to rez inventory {3} owned by {4}", rezzingpart.Name, rezzingpart.ID, rezzingpart.GlobalPosition, inventory, rezzingpart.Owner);
                    }
                    else if(removeinventory)
                    {
                        rezzingpart.Inventory.Remove(item.ID);
                    }
                }
            }
        }

        [APIExtension(APIExtension.RezAsync, "asLinkRezAtRootAsync")]
        [CheckFunctionPermission("asRezObjectAsync")]
        public void LinkRezAtRootAsynchronous(ScriptInstance instance, int link, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param) =>
            LinkRezAtRoot(instance, link, inventory, pos, vel, rot, param, true);

        [APILevel(APIFlags.ASSL, "asLinkRezAtRoot")]
        public void LinkRezAtRoot(ScriptInstance instance, int link, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param) =>
            LinkRezAtRoot(instance, link, inventory, pos, vel, rot, param, false);

        private void LinkRezAtRoot(ScriptInstance instance, int link, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param, bool useAsync)
        {
            lock (instance)
            {
                List<ObjectGroup> groups;
                ObjectPart rezzingpart = instance.Part;
                ObjectPart invpart;
                ObjectPartInventoryItem item;
                SceneInterface scene = rezzingpart.ObjectGroup.Scene;
                UUID sceneid = scene.ID;
                bool removeinventory;
                if (IsRezDistanceLimitEnforced(sceneid) &&
                    (instance.Part.GlobalPosition - pos).Length > GetRezDistanceMeterLimit(sceneid))
                {
                    /* silent fail as per definition */
                    return;
                }

                if (instance.TryGetLink(link, out invpart) &&
                    instance.TryGetObjectInventory(invpart, inventory, out groups, out removeinventory, out item))
                {
                    if(useAsync)
                    {
                        ThreadPool.QueueUserWorkItem((o) =>
                            RealRezObject(scene, instance.Item.AssetID, instance.Item.Owner, rezzingpart, groups, pos, vel, rot, param, item));
                        if(removeinventory)
                        {
                            invpart.Inventory.Remove(item.ID);
                        }
                    }
                    else if (RealRezObject(scene, instance.Item.AssetID, instance.Item.Owner, rezzingpart, groups, pos, vel, rot, param, item) &&
                        removeinventory)
                    {
                        invpart.Inventory.Remove(item.ID);
                    }
                }
            }
        }

        [APIExtension(APIExtension.RezAsync, "asRezAtRootAsync")]
        [CheckFunctionPermission("asRezObjectAsync")]
        public void RezAtRootAsynchronous(ScriptInstance instance, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param) =>
            RezAtRoot(instance, inventory, pos, vel, rot, param, true);

        [APILevel(APIFlags.LSL, "llRezAtRoot")]
        [ForcedSleep(0.1)]
        public void RezAtRoot(ScriptInstance instance, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param) =>
            RezAtRoot(instance, inventory, pos, vel, rot, param, false);

        private void RezAtRoot(ScriptInstance instance, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param, bool useAsync)
        {
            lock (instance)
            {
                List<ObjectGroup> groups;
                ObjectPart rezzingpart = instance.Part;
                ObjectPartInventoryItem item;
                SceneInterface scene = rezzingpart.ObjectGroup.Scene;
                UUID sceneid = scene.ID;
                bool removeinventory;
                if (IsRezDistanceLimitEnforced(sceneid) &&
                    (instance.Part.GlobalPosition - pos).Length > GetRezDistanceMeterLimit(sceneid))
                {
                    /* silent fail as per definition */
                    return;
                }

                if (instance.TryGetObjectInventory(inventory, out groups, out removeinventory, out item))
                {
                    if(useAsync)
                    {
                        ThreadPool.QueueUserWorkItem((o) =>
                            RealRezObject(scene, instance.Item.AssetID, instance.Item.Owner, rezzingpart, groups, pos, vel, rot, param, item));
                        if(removeinventory)
                        {
                            rezzingpart.Inventory.Remove(item.ID);
                        }
                    }
                    else if (RealRezObject(scene, instance.Item.AssetID, instance.Item.Owner, rezzingpart, groups, pos, vel, rot, param, item) &&
                        removeinventory)
                    {
                        rezzingpart.Inventory.Remove(item.ID);
                    }
                }
            }
        }

        public bool RealRezObject(SceneInterface scene, UUID rezzingscriptassetid, UGUI rezzingowner, ObjectPart rezzingpart, List<ObjectGroup> groups, Vector3 pos, Vector3 vel, Quaternion rot, int param, ObjectPartInventoryItem sourceItem)
        {
            Quaternion rotOff = rot / groups[0].GlobalRotation;
            Vector3 coalesced = groups[0].CoalescedRestoreOffset;
            foreach (ObjectGroup sog in groups)
            {
                sog.RezzingObjectID = rezzingpart.ID;
                sog.GlobalRotation *= rotOff;
                sog.GlobalPosition = pos;
                sog.GlobalPosition += (sog.CoalescedRestoreOffset - coalesced) * rotOff;
                sog.Owner = rezzingowner;
                if(!scene.CanRez(rezzingpart.ID, rezzingowner, sog.GlobalPosition, sog.OriginalAssetID, rezzingscriptassetid))
                {
                    return false;
                }
            }

            foreach (ObjectGroup sog in groups)
            {
                sog.RezzingObjectID = rezzingpart.ID;
                foreach (ObjectPart part in sog.ValuesByKey1)
                {
                    part.RezDate = Date.Now;
                    foreach (ObjectPartInventoryItem item in part.Inventory.ValuesByKey2)
                    {
                        if(item.AssetType == AssetType.LSLText)
                        {
                            var savedScriptState = item.ScriptState as ScriptStates.ScriptState;
                            if (savedScriptState != null)
                            {
                                savedScriptState.StartParameter = param;
                            }
                            else
                            {
                                savedScriptState = new ScriptStates.ScriptState
                                {
                                    StartParameter = param
                                };
                                item.ScriptState = savedScriptState;
                            }
                        }
                    }
                }
                sog.Velocity = vel;
            }

            foreach (ObjectGroup sog in groups)
            {
                sog.IsDieAtEdge = true; /* as per definition of llRezObject and llRezAtRoot */
                scene.RezObject(sog, rezzingowner, param, rezzingpart.ID, sourceItem);
                rezzingpart.PostEvent(new ObjectRezEvent
                {
                    ObjectID = sog.ID
                });
            }

            return true;
        }
    }
}
