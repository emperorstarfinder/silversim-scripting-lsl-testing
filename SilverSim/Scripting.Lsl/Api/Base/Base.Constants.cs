// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        #region LSL Constants

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int TRUE = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int FALSE = 0;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const string NULL_KEY = "00000000-0000-0000-0000-000000000000";

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public static readonly Vector3 ZERO_VECTOR = Vector3.Zero;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public static readonly Quaternion ZERO_ROTATION = Quaternion.Identity;

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CHANGED_INVENTORY = 0x00000001;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CHANGED_COLOR = 0x00000002;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CHANGED_SHAPE = 0x00000004;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CHANGED_SCALE = 0x00000008;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CHANGED_TEXTURE = 0x00000010;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CHANGED_LINK = 0x00000020;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CHANGED_ALLOWED_DROP = 0x00000040;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CHANGED_OWNER = 0x00000080;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CHANGED_REGION = 0x00000100;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CHANGED_TELEPORT = 0x00000200;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CHANGED_REGION_START = 0x00000400;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CHANGED_MEDIA = 0x00000800;
        #endregion
    }
}
