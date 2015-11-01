// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.LSL.Api.Permissions
{
    [ScriptApiName("Permissions")]
    [LSLImplementation]
    public partial class PermissionsApi : IScriptApi, IPlugin
    {
        public PermissionsApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        [LSLTooltip("permission to take money from agent's account")]
        const int PERMISSION_DEBIT = 0x2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        [LSLTooltip("permission to take agent's controls")]
        const int PERMISSION_TAKE_CONTROLS = 0x4;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        [LSLTooltip("permission to start or stop animations on agent")]
        const int PERMISSION_TRIGGER_ANIMATION = 0x10;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        [LSLTooltip("permission to attach/detach from agent")]
        const int PERMISSION_ATTACH = 0x20;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        [LSLTooltip("permission to change links")]
        const int PERMISSION_CHANGE_LINKS = 0x80;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        [LSLTooltip("permission to track agent's camera position and rotation")]
        const int PERMISSION_TRACK_CAMERA = 0x400;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        [LSLTooltip("permission to control the agent's camera\n(must be sat on or attached; automatically revoked on stand or detach)")]
        const int PERMISSION_CONTROL_CAMERA = 0x800;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        [LSLTooltip("permission to teleport the agent")]
        const int PERMISSION_TELEPORT = 0x1000;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        [LSLTooltip("permission to manage estate access without notifying the owner of changes")]
        const int PERMISSION_SILENT_ESTATE_MANAGEMENT = 0x4000;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        [LSLTooltip("permission to configure overriding of default animations")]
        const int PERMISSION_OVERRIDE_ANIMATIONS = 0x8000;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        [LSLTooltip("permission to return object from parcels by llReturnObjectsByOwner and llReturnObjectsByID")]
        const int PERMISSION_RETURN_OBJECTS = 0x10000;



        [APILevel(APIFlags.LSL, "run_time_permissions")]
        [StateEventDelegate]
        delegate void State_run_time_permissions(int perm);

        [APILevel(APIFlags.LSL, "llGetPermissions")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        int GetPermissions(ScriptInstance instance)
        {
            lock (instance)
            {
                return (int)instance.Item.PermsGranter.PermsMask;
            }
        }

        [APILevel(APIFlags.LSL, "llGetPermissionsKey")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        LSLKey GetPermissionsKey(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Item.PermsGranter.PermsGranter.ID;
            }
        }

        [APILevel(APIFlags.LSL, "llRequestPermissions")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void RequestPermissions(ScriptInstance instance, LSLKey agentID, int permissions)
        {
            lock(instance)
            {
                if (agentID == UUID.Zero || permissions == 0)
                {
                    instance.RevokePermissions(agentID, (ScriptPermissions)permissions);
                }
                else
                {
                    IAgent a;
                    try
                    {
                        a = instance.Part.ObjectGroup.Scene.Agents[agentID];
                    }
                    catch
                    {
                        instance.Item.PermsGranter = null;
                        return;
                    }
                    ScriptPermissions perms = a.RequestPermissions(instance.Part, instance.Item.ID, (ScriptPermissions)permissions);
                    if (perms != ScriptPermissions.None)
                    {
                        RuntimePermissionsEvent e = new RuntimePermissionsEvent();
                        e.Permissions = perms;
                        e.PermissionsKey = a.Owner;
                        instance.PostEvent(e);
                    }
                }
            }
        }

        [ExecutedOnScriptReset]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        static void ResetPermissions(ScriptInstance instance)
        {
            lock (instance)
            {
                instance.Item.PermsGranter = null;
            }
        }
    }
}
