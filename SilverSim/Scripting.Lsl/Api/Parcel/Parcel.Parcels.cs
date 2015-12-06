// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using System;

namespace SilverSim.Scripting.Lsl.Api.Parcel
{
    public partial class ParcelApi
    {
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PARCEL_COUNT_TOTAL = 0;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PARCEL_COUNT_OWNER = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PARCEL_COUNT_GROUP = 2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PARCEL_COUNT_OTHER = 3;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PARCEL_COUNT_SELECTED = 4;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PARCEL_COUNT_TEMP = 5;

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PARCEL_DETAILS_NAME = 0;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PARCEL_DETAILS_DESC = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PARCEL_DETAILS_OWNER = 2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PARCEL_DETAILS_GROUP = 3;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PARCEL_DETAILS_AREA = 4;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PARCEL_DETAILS_ID = 5;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PARCEL_DETAILS_SEE_AVATARS = 6; // not implemented

        //osSetParcelDetails
        [APILevel(APIFlags.OSSL, APILevel.KeepCsName)]
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
            AnArray res = new AnArray();
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
                                throw new NotImplementedException("PARCEL_DETAILS_SEE_AVATARS in llGetParcelDetails");

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

        [APILevel(APIFlags.LSL, "llGetParcelMaxPrims")]
        public int GetParcelMaxPrims(ScriptInstance instance, Vector3 pos, int sim_wide)
        {
            throw new NotImplementedException("llGetParcelMaxPrims(vector, integer)");
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
                    pInfo.Owner.EqualsGrid(thisPart.Owner)) ?
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
                    pInfo.MusicURI = new URI(url);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llReturnObjectsByID")]
        public int ReturnObjectsByID(ScriptInstance instance, AnArray objects)
        {
            throw new NotImplementedException("llReturnObjectsByID(list)");
        }

        [APILevel(APIFlags.LSL, "llReturnObjectsByOwner")]
        public int ReturnObjectsByOwner(ScriptInstance instance, LSLKey owner, int scope)
        {
            throw new NotImplementedException("llReturnObjectsByOwner(key, integer)");
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
            throw new NotImplementedException("llGetParcelPrimCount(vector, integer, integer)");
        }

        [APILevel(APIFlags.LSL, "llGetParcelPrimOwners")]
        [ForcedSleep(2)]
        public AnArray GetParcelPrimOwners(ScriptInstance instance, Vector3 pos)
        {
            throw new NotImplementedException("llGetParcelPrimOwners(vector)");
        }

        [APILevel(APIFlags.LSL, "llEjectFromLand")]
        public void EjectFromLand(ScriptInstance instance, LSLKey avatar)
        {
            throw new NotImplementedException("llEjectFromLand(key)");
        }

        [APILevel(APIFlags.LSL, "llAddToLandBanList")]
        [ForcedSleep(0.1)]
        public void AddToLandBanList(ScriptInstance instance, LSLKey avatar, double hours)
        {
            throw new NotImplementedException("llAddToLandBanList(key, float)");
        }

        [APILevel(APIFlags.LSL, "llAddToLandPassList")]
        [ForcedSleep(0.1)]
        public void AddToLandPassList(ScriptInstance instance, LSLKey avatar, double hours)
        {
            throw new NotImplementedException("llAddToLandPassList(key, float)");
        }

        [APILevel(APIFlags.LSL, "llRemoveFromLandBanList")]
        [ForcedSleep(0.1)]
        public void RemoveFromLandBanList(ScriptInstance instance, LSLKey avatar)
        {
            throw new NotImplementedException("llRemoveFromLandBanList(key)");
        }

        [APILevel(APIFlags.LSL, "llRemoveFromLandPassList")]
        [ForcedSleep(0.1)]
        public void RemoveFromLandPassList(ScriptInstance instance, LSLKey avatar)
        {
            throw new NotImplementedException("llRemoveFromLandPassList(key)");
        }

        [APILevel(APIFlags.LSL, "llResetLandBanList")]
        [ForcedSleep(0.1)]
        public void ResetLandBanList(ScriptInstance instance)
        {
            throw new NotImplementedException("llResetLandBanList()");
        }

        [APILevel(APIFlags.LSL, "llResetLandPassList")]
        [ForcedSleep(0.1)]
        public void ResetLandPassList(ScriptInstance instance)
        {
            throw new NotImplementedException("llResetLandPassList()");
        }
    }
}
