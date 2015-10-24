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

namespace SilverSim.Scripting.LSL.API.Parcel
{
    public partial class Parcel_API
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
        public const int PARCEL_DETAILS_CLAIMDATE = 10;

        [APILevel(APIFlags.LSL, "llGetParcelDetails")]
        public AnArray GetParcelDetails(ScriptInstance instance, Vector3 pos, AnArray param)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetParcelFlags")]
        public int GetParcelFlags(ScriptInstance instance, Vector3 pos)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetParcelMaxPrims")]
        public int GetParcelMaxPrims(ScriptInstance instance, Vector3 pos, int sim_wide)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetParcelMusicURL")]
        public string GetParcelMusicURL(ScriptInstance instance)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                try
                {
                    ParcelInfo pInfo = scene.Parcels[instance.Part.ObjectGroup.Position];
                    if (pInfo.Owner.EqualsGrid(instance.Part.Owner))
                    {
                        return pInfo.MusicURI;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetParcelMusicURL")]
        [ForcedSleep(2)]
        public void SetParcelMusicURL(ScriptInstance instance, string url)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llReturnObjectsByID")]
        public int ReturnObjectsByID(ScriptInstance instance, AnArray objects)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llReturnObjectsByOwner")]
        public int ReturnObjectsByOwner(ScriptInstance instance, LSLKey owner, int scope)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetLandOwnerAt")]
        public LSLKey GetLandOwnerAt(ScriptInstance instance, Vector3 pos)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                try
                {
                    ParcelInfo pInfo = scene.Parcels[pos];
                    return pInfo.Owner.ID;
                }
                catch
                {
                    return UUID.Zero;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetParcelPrimCount")]
        public int GetParcelPrimCount(ScriptInstance instance, Vector3 pos, int category, int sim_wide)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetParcelPrimOwners")]
        [ForcedSleep(2)]
        public AnArray GetParcelPrimOwners(ScriptInstance instance, Vector3 pos)
        {
            throw new NotImplementedException();
        }
    }
}
