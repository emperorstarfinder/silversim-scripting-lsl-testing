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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Groups;
using SilverSim.Types.Parcel;
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Scripting.Lsl.Api.Parcel
{
    public partial class ParcelApi
    {
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_COUNT_TOTAL = 0;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_COUNT_OWNER = 1;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_COUNT_GROUP = 2;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_COUNT_OTHER = 3;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_COUNT_SELECTED = 4;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_COUNT_TEMP = 5;

        [APILevel(APIFlags.LSL)]
        public const int PARCEL_DETAILS_NAME = 0;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_DETAILS_DESC = 1;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_DETAILS_OWNER = 2;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_DETAILS_GROUP = 3;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_DETAILS_AREA = 4;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_DETAILS_ID = 5;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_DETAILS_SEE_AVATARS = 6;

        //osSetParcelDetails
        [APILevel(APIFlags.OSSL)]
        public const int PARCEL_DETAILS_CLAIMDATE = 10;

        [APILevel(APIFlags.LSL, "llOverMyLand")]
        public int OverMyLand(ScriptInstance instance, LSLKey id)
        {
            lock(instance)
            {
                IObject obj;
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if(scene.Objects.TryGetValue(id, out obj))
                {
                    ParcelInfo pinfo;
                    if(scene.Parcels.TryGetValue(obj.GlobalPosition, out pinfo))
                    {
                        return pinfo.Owner.EqualsGrid(instance.Item.Owner) ? 1 : 0;
                    }
                }
                return 0;
            }
        }

        [APILevel(APIFlags.LSL, "llGetParcelDetails")]
        public AnArray GetParcelDetails(ScriptInstance instance, Vector3 pos, AnArray param)
        {
            var res = new AnArray();
            lock (instance)
            {
                ParcelInfo pinfo;
                if (instance.Part.ObjectGroup.Scene.Parcels.TryGetValue(pos, out pinfo))
                {
                    foreach(IValue val in param)
                    {
                        switch(val.AsInt)
                        {
                            case PARCEL_DETAILS_NAME:
                                res.Add(pinfo.Name);
                                break;

                            case PARCEL_DETAILS_DESC:
                                res.Add(pinfo.Description);
                                break;

                            case PARCEL_DETAILS_OWNER:
                                res.Add(pinfo.Owner.ID);
                                break;

                            case PARCEL_DETAILS_GROUP:
                                res.Add(pinfo.Group.ID);
                                break;

                            case PARCEL_DETAILS_AREA:
                                res.Add(pinfo.Area);
                                break;

                            case PARCEL_DETAILS_ID:
                                res.Add(pinfo.ID);
                                break;

                            case PARCEL_DETAILS_SEE_AVATARS:
                                res.Add(pinfo.SeeAvatars.ToLSLBoolean());
                                break;

                            default:
                                res.Add(string.Empty);
                                break;
                        }
                    }
                }
            }
            return res;
        }

        [APILevel(APIFlags.LSL, "llGetParcelFlags")]
        public int GetParcelFlags(ScriptInstance instance, Vector3 pos)
        {
            lock(instance)
            {
                ParcelInfo pinfo;
                if(instance.Part.ObjectGroup.Scene.Parcels.TryGetValue(pos, out pinfo))
                {
                    return (int)pinfo.Flags;
                }
                return 0;
            }
        }

        [APILevel(APIFlags.LSL, "llScriptDanger")]
        public int ScriptDanger(ScriptInstance instance, Vector3 pos)
        {
            throw new NotImplementedException("llScriptDanger(vector)");
        }

        [APILevel(APIFlags.LSL, "llGetParcelMaxPrims")]
        public int GetParcelMaxPrims(ScriptInstance instance, Vector3 pos, int sim_wide)
        {
            var parcels = new List<ParcelInfo>();
            ParcelInfo locatedparcel;

            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if(scene.Parcels.TryGetValue(pos, out locatedparcel))
                {
                    parcels.Add(locatedparcel);
                }
                if(sim_wide != 0)
                {
                    parcels.AddRange(from p in scene.Parcels where p.Owner.EqualsGrid(locatedparcel.Owner) select p);
                }

                int maxPrims = 0;
                foreach(ParcelInfo p in parcels)
                {
                    maxPrims += scene.CalcMaxTotalParcelPrims(p);
                }

                return maxPrims;
            }
        }

        [APILevel(APIFlags.LSL, "llGetParcelMusicURL")]
        public string GetParcelMusicURL(ScriptInstance instance)
        {
            lock (instance)
            {
                ObjectPart thisPart = instance.Part;
                SceneInterface scene = thisPart.ObjectGroup.Scene;

                ParcelInfo pInfo;
                return (scene.Parcels.TryGetValue(thisPart.ObjectGroup.Position, out pInfo) &&
                    pInfo.MusicURI != null && (pInfo.Owner.EqualsGrid(thisPart.Owner) || !pInfo.ObscureMusic)) ?
                    pInfo.MusicURI :
                    string.Empty;
            }
        }

        [APILevel(APIFlags.LSL, "llSetParcelMusicURL")]
        [ForcedSleep(2)]
        public void SetParcelMusicURL(ScriptInstance instance, string url)
        {
            lock (instance)
            {
                ObjectPart thisPart = instance.Part;
                SceneInterface scene = thisPart.ObjectGroup.Scene;

                ParcelInfo pInfo;
                if (scene.Parcels.TryGetValue(thisPart.ObjectGroup.Position, out pInfo) &&
                    pInfo.Owner.EqualsGrid(thisPart.Owner))
                {
                    try
                    {
                        pInfo.MusicURI = new URI(url);
                    }
                    catch
                    {
                        pInfo.MusicURI = null;
                    }
                    scene.TriggerParcelUpdate(pInfo);
                }
            }
        }

        private int ReturnObjects(SceneInterface scene, UGUI returningAgent, List<UUID> returnids)
        {
            foreach (UUID objectid in returnids)
            {
                ObjectGroup o;
                if (!scene.ObjectGroups.TryGetValue(objectid, out o))
                {
                    continue;
                }
                if (o.IsAttached)
                {
                    continue;
                }
                if (!scene.CanReturn(returningAgent, o, o.GlobalPosition))
                {
                    return ERR_PARCEL_PERMISSIONS;
                }
            }

            return scene.ReturnObjects(returningAgent, returnids).Count;
        }

        [APILevel(APIFlags.LSL, "llReturnObjectsByID")]
        public int ReturnObjectsByID(ScriptInstance instance, AnArray objects)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                ObjectPartInventoryItem item = instance.Item;
                ObjectPartInventoryItem.PermsGranterInfo permsgranter = item.PermsGranter;

                if (!permsgranter.PermsGranter.EqualsGrid(instance.Item.Owner) ||
                    0 == (permsgranter.PermsMask & ScriptPermissions.ReturnObjects))
                {
                    return ERR_RUNTIME_PERMISSIONS;
                }

                var returnids = new List<UUID>();

                foreach(IValue iv in objects)
                {
                    UUID objectid;
                    if(!UUID.TryParse(iv.ToString(), out objectid))
                    {
                        return ERR_MALFORMED_PARAMS;
                    }
                    if(returnids.Contains(objectid))
                    {
                        return ERR_MALFORMED_PARAMS;
                    }
                    returnids.Add(objectid);
                }

                return ReturnObjects(scene, item.Owner, returnids);
            }
        }

        [APILevel(APIFlags.LSL, "llReturnObjectsByOwner")]
        public int ReturnObjectsByOwner(ScriptInstance instance, LSLKey owner, int scope)
        {
            lock (instance)
            {
                ParcelInfo parcel;
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.Parcels.TryGetValue(instance.Part.ObjectGroup.GlobalPosition, out parcel))
                {
                    /* a lot faster to go by ObjectGroups then to go by prims here */
                    var returnids = new List<UUID>();
                    foreach (ObjectGroup o in instance.Part.ObjectGroup.Scene.ObjectGroups)
                    {
                        if (o.IsAttached)
                        {
                            continue;
                        }
                        if (!parcel.LandBitmap.ContainsLocation(o.GlobalPosition))
                        {
                            continue;
                        }
                        if(o.Owner.ID.Equals(owner.AsUUID))
                        {
                            returnids.Add(o.ID);
                        }
                    }
                    return ReturnObjects(scene, instance.Item.Owner, returnids);
                }
                return ERR_GENERIC;
            }
        }

        [APILevel(APIFlags.LSL, "llGetLandOwnerAt")]
        public LSLKey GetLandOwnerAt(ScriptInstance instance, Vector3 pos)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                ParcelInfo pInfo;
                return (scene.Parcels.TryGetValue(pos, out pInfo)) ?
                    pInfo.Owner.ID :
                    UUID.Zero;
            }
        }

        [APILevel(APIFlags.LSL, "llGetParcelPrimCount")]
        public int GetParcelPrimCount(ScriptInstance instance, Vector3 pos, int category, int sim_wide)
        {
            lock (instance)
            {
                int primCount = 0;
                ParcelInfo parcel;
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.Parcels.TryGetValue(pos, out parcel))
                {
                    /* a lot faster to go by ObjectGroups then to go by prims here */
                    foreach (ObjectGroup o in instance.Part.ObjectGroup.Scene.ObjectGroups)
                    {
                        if (o.IsAttached)
                        {
                            continue;
                        }
                        if (!parcel.LandBitmap.ContainsLocation(o.GlobalPosition))
                        {
                            continue;
                        }

                        primCount += o.Count;
                    }
                }
                return primCount;
            }
        }

        [APILevel(APIFlags.LSL, "llGetParcelPrimOwners")]
        [ForcedSleep(2)]
        public AnArray GetParcelPrimOwners(ScriptInstance instance, Vector3 pos)
        {
            lock (instance)
            {
                var res = new AnArray();
                ParcelInfo parcel;
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.Parcels.TryGetValue(pos, out parcel))
                {
                    var owners = new List<UUID>();
                    foreach (ObjectGroup o in instance.Part.ObjectGroup.Scene.ObjectGroups)
                    {
                        if(o.IsAttached)
                        {
                            continue;
                        }
                        if(!parcel.LandBitmap.ContainsLocation(o.GlobalPosition))
                        {
                            continue;
                        }

                        UUID ownerId = o.Owner.ID;
                        if (!owners.Contains(ownerId))
                        {
                            owners.Add(ownerId);
                        }
                    }
                    foreach (UUID owner in owners)
                    {
                        res.Add(new LSLKey(owner));
                    }
                }
                return res;
            }
        }

        [APILevel(APIFlags.LSL, "llEjectFromLand")]
        public void EjectFromLand(ScriptInstance instance, LSLKey avatar)
        {
            lock(instance)
            {
                ObjectPart part = instance.Part;
                ObjectGroup grp = part.ObjectGroup;
                ObjectPartInventoryItem item = instance.Item;
                SceneInterface scene = part.ObjectGroup.Scene;
                ParcelInfo pinfo;

                if(scene.Parcels.TryGetValue(grp.GlobalPosition, out pinfo) && 
                    (pinfo.Owner.EqualsGrid(item.Owner) ||
                    (pinfo.GroupOwned && grp.IsGroupOwned && pinfo.Group == grp.Group &&
                    scene.HasGroupPower(grp.Owner, grp.Group, GroupPowers.LandEjectAndFreeze))))
                {
                    scene.EjectFromParcel(avatar.AsUUID, pinfo.ID);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llAddToLandBanList")]
        [ForcedSleep(0.1)]
        public void AddToLandBanList(ScriptInstance instance, LSLKey avatar, double hours)
        {
            lock (instance)
            {
                ObjectPart part = instance.Part;
                ObjectGroup grp = part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                ParcelInfo pInfo;
                var entry = new ParcelAccessEntry();
                if (!scene.AvatarNameService.TryGetValue(avatar.AsUUID, out entry.Accessor))
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "FailedToFindAgentForUUID0", "Failed to find agent for UUID {0}", avatar.AsUUID));
                }
                else if (scene.Parcels.TryGetValue(grp.Position, out pInfo))
                {
                    entry.RegionID = scene.ID;
                    entry.ParcelID = pInfo.ID;
                    entry.ExpiresAt = (hours < double.Epsilon) ?
                        null :
                        Date.UnixTimeToDateTime((ulong)(hours * 3600) + Date.GetUnixTime());

                    if (pInfo.Owner.EqualsGrid(part.Owner) ||
                        (pInfo.Group.ID != UUID.Zero && scene.HasGroupPower(part.Owner, pInfo.Group, GroupPowers.LandManageBanned)))
                    {
                        scene.Parcels.BlackList.Store(entry);
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llAddToLandPassList")]
        [ForcedSleep(0.1)]
        public void AddToLandPassList(ScriptInstance instance, LSLKey avatar, double hours)
        {
            lock (instance)
            {
                ObjectPart part = instance.Part;
                ObjectGroup grp = part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                ParcelInfo pInfo;
                var entry = new ParcelAccessEntry();
                if(!scene.AvatarNameService.TryGetValue(avatar.AsUUID, out entry.Accessor))
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "FailedToFindAgentForUUID0", "Failed to find agent for UUID {0}", avatar.AsUUID));
                }
                else if (scene.Parcels.TryGetValue(grp.Position, out pInfo))
                {
                    entry.RegionID = scene.ID;
                    entry.ParcelID = pInfo.ID;
                    entry.ExpiresAt = (hours < double.Epsilon) ?
                        null :
                        Date.UnixTimeToDateTime((ulong)(hours * 3600) + Date.GetUnixTime());

                    if (pInfo.Owner.EqualsGrid(part.Owner) ||
                        (pInfo.Group.ID != UUID.Zero && scene.HasGroupPower(part.Owner, pInfo.Group, GroupPowers.LandManageAllowed)))
                    {
                        scene.Parcels.WhiteList.Store(entry);
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llRemoveFromLandBanList")]
        [ForcedSleep(0.1)]
        public void RemoveFromLandBanList(ScriptInstance instance, LSLKey avatar)
        {
            lock (instance)
            {
                ObjectPart part = instance.Part;
                ObjectGroup grp = part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                ParcelInfo pInfo;
                UGUI accessor;
                if (!scene.AvatarNameService.TryGetValue(avatar.AsUUID, out accessor))
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "FailedToFindAgentForUUID0", "Failed to find agent for UUID {0}", avatar.AsUUID));
                }
                else if (scene.Parcels.TryGetValue(grp.Position, out pInfo) &&
                    (pInfo.Owner.EqualsGrid(part.Owner) ||
                    (pInfo.Group.ID != UUID.Zero && scene.HasGroupPower(part.Owner, pInfo.Group, GroupPowers.LandManageBanned))))
                {
                    scene.Parcels.BlackList.Remove(scene.ID, pInfo.ID, accessor);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llRemoveFromLandPassList")]
        [ForcedSleep(0.1)]
        public void RemoveFromLandPassList(ScriptInstance instance, LSLKey avatar)
        {
            lock (instance)
            {
                ObjectPart part = instance.Part;
                ObjectGroup grp = part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                ParcelInfo pInfo;
                UGUI accessor;
                if (!scene.AvatarNameService.TryGetValue(avatar.AsUUID, out accessor))
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "FailedToFindAgentForUUID0", "Failed to find agent for UUID {0}", avatar.AsUUID));
                }
                else if (scene.Parcels.TryGetValue(grp.Position, out pInfo) &&
                    (pInfo.Owner.EqualsGrid(part.Owner) ||
                    (pInfo.Group.ID != UUID.Zero && scene.HasGroupPower(part.Owner, pInfo.Group, GroupPowers.LandManageAllowed))))
                {
                    scene.Parcels.WhiteList.Remove(scene.ID, pInfo.ID, accessor);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llResetLandBanList")]
        [ForcedSleep(0.1)]
        public void ResetLandBanList(ScriptInstance instance)
        {
            lock (instance)
            {
                ObjectPart part = instance.Part;
                ObjectGroup grp = part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                ParcelInfo pInfo;
                if (scene.Parcels.TryGetValue(grp.Position, out pInfo) &&
                    (pInfo.Owner.EqualsGrid(part.Owner) ||
                    (pInfo.Group.ID != UUID.Zero && scene.HasGroupPower(part.Owner, pInfo.Group, Types.Groups.GroupPowers.LandManageBanned))))
                {
                    scene.Parcels.BlackList.Remove(scene.ID, pInfo.ID);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llResetLandPassList")]
        [ForcedSleep(0.1)]
        public void ResetLandPassList(ScriptInstance instance)
        {
            lock(instance)
            {
                ObjectPart part = instance.Part;
                ObjectGroup grp = part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                ParcelInfo pInfo;
                if(scene.Parcels.TryGetValue(grp.Position, out pInfo) &&
                    (pInfo.Owner.EqualsGrid(part.Owner) ||
                    (pInfo.Group.ID != UUID.Zero && scene.HasGroupPower(part.Owner, pInfo.Group, GroupPowers.LandManageAllowed))))
                {
                    scene.Parcels.WhiteList.Remove(scene.ID, pInfo.ID);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osSetParcelDetails")]
        [ThreatLevelRequired(ThreatLevel.High)]
        public void SetParcelDetails(ScriptInstance instance, Vector3 pos, AnArray rules)
        {
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                ParcelInfo pInfo;

                if (scene.Parcels.TryGetValue(pos, out pInfo))
                {
                    int idx = 0;
                    bool parcelDataChanged = false;
                    while (idx + 1 < rules.Count)
                    {
                        UGUI agentId;
                        UGI groupId;
                        int type = rules[idx++].AsInt;

                        switch (type)
                        {
                            case PARCEL_DETAILS_NAME:
                                parcelDataChanged = true;
                                pInfo.Name = rules[idx++].ToString();
                                break;

                            case PARCEL_DETAILS_DESC:
                                parcelDataChanged = true;
                                pInfo.Description = rules[idx++].ToString();
                                break;

                            case PARCEL_DETAILS_OWNER:
                                if(!scene.AvatarNameService.TryGetValue(rules[idx++].AsUUID, out agentId))
                                {
                                    throw new LocalizedScriptErrorException(this, "Parameter0Parameter1DoesNotResolveToAnAgent", "{0} parameter '{1}' does not resolve to an agent", "PARCEL_DETAILS_OWNER", rules[idx++].ToString());
                                }
                                pInfo.Owner = agentId;
                                break;

                            case PARCEL_DETAILS_GROUP:
                                UUID grpId = rules[idx++].AsUUID;
                                groupId = UGI.Unknown;
                                if(grpId != UUID.Zero && !scene.GroupsNameService.TryGetValue(grpId, out groupId))
                                {
                                    throw new LocalizedScriptErrorException(this, "Parameter0Parameter1DoesNotResolveToAnAgent", "{0} parameter '{1}' does not resolve to an agent", "PARCEL_DETAILS_GROUP", rules[idx++].ToString());
                                }
                                pInfo.Group = groupId;
                                break;

                            case PARCEL_DETAILS_CLAIMDATE:
                                pInfo.ClaimDate = Date.UnixTimeToDateTime(rules[idx++].AsULong);
                                break;

                            case PARCEL_DETAILS_SEE_AVATARS:
                                pInfo.SeeAvatars = rules[idx++].AsBoolean;
                                break;

                            default:
                                throw new LocalizedScriptErrorException(this, "OsSetParcelDetailsUnknownParameterType0", "osSetParcelDetails: Unknown parameter type {0}", type);
                        }
                    }
                    if(parcelDataChanged)
                    {
                        scene.TriggerParcelUpdate(pInfo);
                    }
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osParcelJoin")]
        public void ParcelJoin(ScriptInstance instance, Vector3 pos1, Vector3 pos2)
        {
            Vector3 min = pos1.ComponentMin(pos2);
            Vector3 max = pos1.ComponentMax(pos2);

            lock (instance)
            {
                instance.Part.ObjectGroup.Scene.JoinParcels(instance.Part.Owner, (int)min.X, (int)min.Y, (int)max.X, (int)max.Y);
            }
        }

        [APILevel(APIFlags.OSSL, "osParcelSubdivide")]
        public void ParcelSubdivide(ScriptInstance instance, Vector3 pos1, Vector3 pos2)
        {
            Vector3 min = pos1.ComponentMin(pos2);
            Vector3 max = pos1.ComponentMax(pos2);

            lock (instance)
            {
                instance.Part.ObjectGroup.Scene.DivideParcel(instance.Part.Owner, (int)min.X, (int)min.Y, (int)max.X, (int)max.Y);
            }
        }
    }
}
