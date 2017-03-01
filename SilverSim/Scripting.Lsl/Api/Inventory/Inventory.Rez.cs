// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scene.Types.Transfer;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilverSim.Scripting.Lsl.Api.Inventory
{
    [ServerParam("LSL.EnforceRezDistanceLimit", ParameterType = typeof(bool))]
    [ServerParam("LSL.RezDistanceMeterLimit", ParameterType = typeof(uint))]
    partial class InventoryApi
    {
        readonly RwLockedDictionary<UUID, bool> m_EnforceRezDistanceLimitParams = new RwLockedDictionary<UUID, bool>();
        readonly RwLockedDictionary<UUID, uint> m_RezDistanceMeterLimit = new RwLockedDictionary<UUID, uint>();

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

        bool IsRezDistanceLimitEnforced(UUID regionID)
        {
            bool value;
            if (m_EnforceRezDistanceLimitParams.TryGetValue(regionID, out value) ||
                m_EnforceRezDistanceLimitParams.TryGetValue(UUID.Zero, out value))
            {
                return value;
            }
            return true;
        }

        uint GetRezDistanceMeterLimit(UUID regionID)
        {
            uint value;
            if (m_RezDistanceMeterLimit.TryGetValue(regionID, out value) ||
                m_RezDistanceMeterLimit.TryGetValue(UUID.Zero, out value))
            {
                return value;
            }
            return 10;
        }

        [APILevel(APIFlags.LSL, "llRezObject")]
        [ForcedSleep(0.1)]
        public void RezObject(ScriptInstance instance, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param)
        {
            lock(instance)
            {
                List<ObjectGroup> groups;
                ObjectPart rezzingpart = instance.Part;
                SceneInterface scene = rezzingpart.ObjectGroup.Scene;
                UUID sceneid = scene.ID;
                if(IsRezDistanceLimitEnforced(sceneid) && 
                    (instance.Part.GlobalPosition - vel).Length > GetRezDistanceMeterLimit(sceneid))
                {
                    /* silent fail as per definition */
                    return;
                }
                if(TryGetObjectInventory(instance, inventory, out groups))
                {
                    Vector3 geometriccenteroffset = Vector3.Zero;
                    int primcount = 0;
                    foreach(ObjectGroup grp in groups)
                    {
                        foreach(ObjectPart part in grp.Values)
                        {
                            geometriccenteroffset += part.GlobalPosition;
                            ++primcount;
                        }
                    }
                    geometriccenteroffset /= primcount;
                    geometriccenteroffset -= groups[0].GlobalPosition;
                    pos += geometriccenteroffset;

                    RealRezObject(scene, rezzingpart, groups, pos, vel, rot, param);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llRezAtRoot")]
        [ForcedSleep(0.1)]
        public void RezAtRoot(ScriptInstance instance, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param)
        {
            lock (instance)
            {
                List<ObjectGroup> groups;
                ObjectPart rezzingpart = instance.Part;
                SceneInterface scene = rezzingpart.ObjectGroup.Scene;
                UUID sceneid = scene.ID;
                if (IsRezDistanceLimitEnforced(sceneid) &&
                    (instance.Part.GlobalPosition - vel).Length > GetRezDistanceMeterLimit(sceneid))
                {
                    /* silent fail as per definition */
                    return;
                }
                if (TryGetObjectInventory(instance, inventory, out groups))
                {
                    RealRezObject(scene, rezzingpart, groups, pos, vel, rot, param);
                }
            }
        }

        public void RealRezObject(SceneInterface scene, ObjectPart rezzingpart, List<ObjectGroup> groups, Vector3 pos, Vector3 vel, Quaternion rot, int param)
        {
            foreach (ObjectGroup sog in groups)
            {
                sog.RezzingObjectID = rezzingpart.ID;
                foreach (ObjectPart part in sog.ValuesByKey1)
                {
                    UUID oldID = part.ID;
                    part.ID = UUID.Random;
                    sog.ChangeKey(part.ID, oldID);
                    foreach (ObjectPartInventoryItem item in part.Inventory.ValuesByKey2)
                    {
                        oldID = item.ID;
                        item.ID = UUID.Random;
                        part.Inventory.ChangeKey(item.ID, oldID);
                    }
                }
            }

            foreach (ObjectGroup sog in groups)
            {
                scene.Add(sog);
                ObjectRezEvent ev = new ObjectRezEvent();
                ev.ObjectID = sog.ID;
                rezzingpart.PostEvent(ev);
            }
        }

        public bool TryGetObjectInventory(ScriptInstance instance, string name, out List<ObjectGroup> groups)
        {
            ObjectPartInventoryItem item;
            AssetData data;
            ObjectPart part = instance.Part;
            groups = null;
            if(!part.Inventory.TryGetValue(name, out item))
            {
                instance.ShoutError("Item '" + name + "' not found to rez");
                return false;
            }
            else if(item.InventoryType != InventoryType.Object || item.AssetType != AssetType.Object)
            {
                instance.ShoutError("Item '" + name + "' is not an object.");
                return false;
            }
            else if(!part.ObjectGroup.Scene.AssetService.TryGetValue(item.AssetID, out data))
            {
                instance.ShoutError("Item '" + name + "' is missing in database.");
                return false;
            }

            try
            {
                groups = ObjectXML.FromAsset(data, instance.Item.Owner);
                return true;
            }
            catch
            {
                instance.ShoutError("Item '" + name + "' has invalid content.");
                return false;
            }
        }
    }
}
