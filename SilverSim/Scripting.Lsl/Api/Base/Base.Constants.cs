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

using SilverSim.Types;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        #region LSL Constants

        [APILevel(APIFlags.LSL)]
        public const int TRUE = 1;
        [APILevel(APIFlags.LSL)]
        public const int FALSE = 0;
        [APILevel(APIFlags.LSL)]
        public const string NULL_KEY = "00000000-0000-0000-0000-000000000000";

        [APILevel(APIFlags.LSL)]
        public static readonly Vector3 ZERO_VECTOR = Vector3.Zero;
        [APILevel(APIFlags.LSL)]
        public static readonly Quaternion ZERO_ROTATION = Quaternion.Identity;

        [APILevel(APIFlags.LSL)]
        public const int CHANGED_INVENTORY = 0x00000001;
        [APILevel(APIFlags.LSL)]
        public const int CHANGED_COLOR = 0x00000002;
        [APILevel(APIFlags.LSL)]
        public const int CHANGED_SHAPE = 0x00000004;
        [APILevel(APIFlags.LSL)]
        public const int CHANGED_SCALE = 0x00000008;
        [APILevel(APIFlags.LSL)]
        public const int CHANGED_TEXTURE = 0x00000010;
        [APILevel(APIFlags.LSL)]
        public const int CHANGED_LINK = 0x00000020;
        [APILevel(APIFlags.LSL)]
        public const int CHANGED_ALLOWED_DROP = 0x00000040;
        [APILevel(APIFlags.LSL)]
        public const int CHANGED_OWNER = 0x00000080;
        [APILevel(APIFlags.LSL)]
        public const int CHANGED_REGION = 0x00000100;
        [APILevel(APIFlags.LSL)]
        public const int CHANGED_TELEPORT = 0x00000200;
        [APILevel(APIFlags.LSL)]
        public const int CHANGED_REGION_START = 0x00000400;
        [APILevel(APIFlags.LSL)]
        public const int CHANGED_MEDIA = 0x00000800;

        [APILevel(APIFlags.LSL)]
        public const int ATTACH_CHEST = 1;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_HEAD = 2;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_LSHOULDER = 3;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_RSHOULDER = 4;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_LHAND = 5;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_RHAND = 6;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_LFOOT = 7;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_RFOOT = 8;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_BACK = 9;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_PELVIS = 10;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_MOUTH = 11;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_CHIN = 12;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_LEAR = 13;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_REAR = 14;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_LEYE = 15;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_REYE = 16;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_NOSE = 17;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_RUARM = 18;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_RLARM = 19;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_LUARM = 20;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_LLARM = 21;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_RHIP = 22;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_RULEG = 23;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_RLLEG = 24;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_LHIP = 25;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_LULEG = 26;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_LLLEG = 27;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_BELLY = 28;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_LEFT_PEC = 29;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_RIGHT_PEC = 30;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_HUD_CENTER_2 = 31;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_HUD_TOP_RIGHT = 32;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_HUD_TOP_CENTER = 33;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_HUD_TOP_LEFT = 34;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_HUD_CENTER_1 = 35;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_HUD_BOTTOM_LEFT = 36;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_HUD_BOTTOM = 37;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_HUD_BOTTOM_RIGHT = 38;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_NECK = 39;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_AVATAR_CENTER = 40;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_LHAND_RING1 = 41;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_RHAND_RING1 = 42;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_TAIL_BASE = 43;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_TAIL_TIP = 44;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_LWING = 45;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_RWING = 46;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_FACE_JAW = 47;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_FACE_LEAR = 48;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_FACE_REAR = 49;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_FACE_LEYE = 50;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_FACE_REYE = 51;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_FACE_TONGUE = 52;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_GROIN = 53;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_HIND_LFOOT = 54;
        [APILevel(APIFlags.LSL)]
        public const int ATTACH_HIND_RFOOT = 55;
        #endregion
    }
}
