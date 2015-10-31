﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
        #region LSL Constants

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int TRUE = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int FALSE = 0;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const string NULL_KEY = "00000000-0000-0000-0000-000000000000";

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        static readonly Vector3 ZERO_VECTOR = Vector3.Zero;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        static readonly Quaternion ZERO_ROTATION = Quaternion.Identity;
        #endregion
    }
}
