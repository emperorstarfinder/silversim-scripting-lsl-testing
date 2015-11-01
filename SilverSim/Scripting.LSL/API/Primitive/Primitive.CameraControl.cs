// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_PITCH = 0;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_FOCUS_OFFSET = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_FOCUS_OFFSET_X = 2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_FOCUS_OFFSET_Y = 3;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_FOCUS_OFFSET_Z = 4;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_POSITION_LAG = 5;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_FOCUS_LAG = 6;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_DISTANCE = 7;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_BEHINDNESS_ANGLE = 8;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_BEHINDNESS_LAG = 9;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_POSITION_THRESHOLD = 10;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_FOCUS_THRESHOLD = 11;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_ACTIVE = 12;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_POSITION = 13;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_POSITION_X = 14;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_POSITION_Y = 15;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_POSITION_Z = 16;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_FOCUS = 17;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_FOCUS_X = 18;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_FOCUS_Y = 19;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_FOCUS_Z = 20;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_POSITION_LOCKED = 21;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int CAMERA_FOCUS_LOCKED = 22;

        [APILevel(APIFlags.LSL, "llSetCameraAtOffset")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetCameraAtOffset(ScriptInstance instance, Vector3 offset)
        {
            lock(instance)
            {
                instance.Part.CameraAtOffset = offset;
            }
        }

        [APILevel(APIFlags.LSL, "llSetLinkCamera")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetLinkCamera(ScriptInstance instance, int link, Vector3 eye, Vector3 at)
        {
            lock (instance)
            {
                ObjectPart part = instance.Part.ObjectGroup[link];
                part.CameraAtOffset = at;
                part.CameraEyeOffset = eye;
            }
        }

        [APILevel(APIFlags.LSL, "llSetCameraEyeOffset")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetCameraEyeOffset(ScriptInstance instance, Vector3 offset)
        {
            lock (instance)
            {
                instance.Part.CameraEyeOffset = offset;
            }
        }

        [APILevel(APIFlags.LSL, "llClearCameraParams")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void ClearCameraParams(ScriptInstance instance)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetCameraParams(ScriptInstance instance, AnArray rules)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        Vector3 GetCameraPos(ScriptInstance instance)
        {
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
            if (grantinfo.PermsGranter != UUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.TrackCamera) != 0)
            {
                lock(this)
                {
                    IAgent agent;
                    if(instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(grantinfo.PermsGranter.ID, out agent))
                    {
                        return agent.CameraPosition;
                    }
                }
            }
            return Vector3.Zero;
        }

        [APILevel(APIFlags.LSL, "llGetCameraRot")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        Quaternion GetCameraRot(ScriptInstance instance)
        {
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
            if (grantinfo.PermsGranter != UUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.TrackCamera) != 0)
            {
                lock(this)
                {
                    IAgent agent;
                    if(instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(grantinfo.PermsGranter.ID, out agent))
                    {
                        return agent.CameraRotation;
                    }
                }
            }
            return Quaternion.Identity;
        }
    }
}
