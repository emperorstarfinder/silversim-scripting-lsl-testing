// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types.Parcel;
using SilverSim.Scripting.Common;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Parcel
{
    public partial class ParcelApi
    {
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int PARCEL_COUNT_TOTAL = 0;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int PARCEL_COUNT_OWNER = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int PARCEL_COUNT_GROUP = 2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int PARCEL_COUNT_OTHER = 3;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int PARCEL_COUNT_SELECTED = 4;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int PARCEL_COUNT_TEMP = 5;

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int PARCEL_DETAILS_NAME = 0;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int PARCEL_DETAILS_DESC = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int PARCEL_DETAILS_OWNER = 2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int PARCEL_DETAILS_GROUP = 3;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int PARCEL_DETAILS_AREA = 4;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int PARCEL_DETAILS_ID = 5;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int PARCEL_DETAILS_SEE_AVATARS = 6; // not implemented

        //osSetParcelDetails
        [APILevel(APIFlags.OSSL, APILevel.KeepCsName)]
        internal const int PARCEL_DETAILS_CLAIMDATE = 10;

        [APILevel(APIFlags.LSL, "llGetParcelDetails")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal AnArray GetParcelDetails(ScriptInstance instance, Vector3 pos, AnArray param)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int GetParcelFlags(ScriptInstance instance, Vector3 pos)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int GetParcelMaxPrims(ScriptInstance instance, Vector3 pos, int sim_wide)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetParcelMusicURL")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal string GetParcelMusicURL(ScriptInstance instance)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;

                ParcelInfo pInfo;
                if(scene.Parcels.TryGetValue(instance.Part.ObjectGroup.Position, out pInfo))
                {
                    if (pInfo.Owner.EqualsGrid(instance.Part.Owner))
                    {
                        return pInfo.MusicURI;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetParcelMusicURL")]
        [ForcedSleep(2)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void SetParcelMusicURL(ScriptInstance instance, string url)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;

                ParcelInfo pInfo;
                if(scene.Parcels.TryGetValue(instance.Part.ObjectGroup.Position, out pInfo))
                {
                    if (pInfo.Owner.EqualsGrid(instance.Part.Owner))
                    {
                        pInfo.MusicURI = new URI(url);
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llReturnObjectsByID")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int ReturnObjectsByID(ScriptInstance instance, AnArray objects)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llReturnObjectsByOwner")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int ReturnObjectsByOwner(ScriptInstance instance, LSLKey owner, int scope)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetLandOwnerAt")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal LSLKey GetLandOwnerAt(ScriptInstance instance, Vector3 pos)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                ParcelInfo pInfo;
                if(scene.Parcels.TryGetValue(pos, out pInfo))
                {
                    return pInfo.Owner.ID;
                }
                else
                {
                    return UUID.Zero;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetParcelPrimCount")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int GetParcelPrimCount(ScriptInstance instance, Vector3 pos, int category, int sim_wide)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetParcelPrimOwners")]
        [ForcedSleep(2)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal AnArray GetParcelPrimOwners(ScriptInstance instance, Vector3 pos)
        {
            throw new NotImplementedException();
        }
    }
}
