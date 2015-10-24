// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_PITCH = 0;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_FOCUS_OFFSET = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_FOCUS_OFFSET_X = 2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_FOCUS_OFFSET_Y = 3;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_FOCUS_OFFSET_Z = 4;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_POSITION_LAG = 5;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_FOCUS_LAG = 6;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_DISTANCE = 7;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_BEHINDNESS_ANGLE = 8;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_BEHINDNESS_LAG = 9;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_POSITION_THRESHOLD = 10;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_FOCUS_THRESHOLD = 11;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_ACTIVE = 12;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_POSITION = 13;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_POSITION_X = 14;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_POSITION_Y = 15;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_POSITION_Z = 16;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_FOCUS = 17;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_FOCUS_X = 18;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_FOCUS_Y = 19;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_FOCUS_Z = 20;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_POSITION_LOCKED = 21;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CAMERA_FOCUS_LOCKED = 22;

        [APILevel(APIFlags.LSL, "llSetCameraAtOffset")]
        public void SetCameraAtOffset(ScriptInstance instance, Vector3 offset)
        {
            throw new NotImplementedException("llSetCameraAtOffset(vector)");
        }

        [APILevel(APIFlags.LSL, "llSetLinkCamera")]
        public void SetLinkCamera(ScriptInstance instance, int link, Vector3 eye, Vector3 at)
        {
            throw new NotImplementedException("llSetLinkCamera(int, vector, vector)");
        }

        [APILevel(APIFlags.LSL, "llSetCameraOffset")]
        public void SetCameraOffset(ScriptInstance instance, Vector3 offset)
        {
            throw new NotImplementedException("llSetCameraOffset(vector)");
        }

        [APILevel(APIFlags.LSL, "llClearCameraParams")]
        public void ClearCameraParams(ScriptInstance instance)
        {
            lock (instance)
            {
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if (grantinfo.PermsGranter != UUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.ControlCamera) != 0)
                {
                    throw new NotImplementedException("llClearCameraParams()");
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetCameraParams")]
        public void SetCameraParams(ScriptInstance instance, AnArray rules)
        {
            lock (instance)
            {
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if (grantinfo.PermsGranter != UUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.ControlCamera) != 0)
                {
                    throw new NotImplementedException("llSetCameraParams(list)");
                }
            }

        }

        [APILevel(APIFlags.LSL, "llGetCameraPos")]
        public Vector3 GetCameraPos(ScriptInstance instance)
        {
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
            if (grantinfo.PermsGranter != UUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.TrackCamera) != 0)
            {
                throw new NotImplementedException("llGetCameraPos()");
            }
            return Vector3.Zero;
        }

        [APILevel(APIFlags.LSL, "llGetCameraRot")]
        public Quaternion GetCameraRot(ScriptInstance instance)
        {
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
            if (grantinfo.PermsGranter != UUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.TrackCamera) != 0)
            {
                throw new NotImplementedException("llGetCameraRot()");
            }
            return Quaternion.Identity;
        }
    }
}
