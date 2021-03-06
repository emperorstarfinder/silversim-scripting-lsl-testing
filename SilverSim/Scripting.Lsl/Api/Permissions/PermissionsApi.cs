﻿// SilverSim is distributed under the terms of the
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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Script;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Permissions
{
    [ScriptApiName("Permissions")]
    [LSLImplementation]
    [Description("LSL Permissions API")]
    public class PermissionsApi : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL)]
        [Description("permission to take money from agent's account")]
        public const int PERMISSION_DEBIT = 0x2;
        [APILevel(APIFlags.LSL)]
        [Description("permission to take agent's controls")]
        public const int PERMISSION_TAKE_CONTROLS = 0x4;
        [APILevel(APIFlags.LSL)]
        [Description("permission to start or stop animations on agent")]
        public const int PERMISSION_TRIGGER_ANIMATION = 0x10;
        [APILevel(APIFlags.LSL)]
        [Description("permission to attach/detach from agent")]
        public const int PERMISSION_ATTACH = 0x20;
        [APILevel(APIFlags.LSL)]
        [Description("permission to change links")]
        public const int PERMISSION_CHANGE_LINKS = 0x80;
        [APILevel(APIFlags.LSL)]
        [Description("permission to track agent's camera position and rotation")]
        public const int PERMISSION_TRACK_CAMERA = 0x400;
        [APILevel(APIFlags.LSL)]
        [Description("permission to control the agent's camera\n(must be sat on or attached; automatically revoked on stand or detach)")]
        public const int PERMISSION_CONTROL_CAMERA = 0x800;
        [APILevel(APIFlags.LSL)]
        [Description("permission to teleport the agent")]
        public const int PERMISSION_TELEPORT = 0x1000;
        [APILevel(APIFlags.LSL)]
        [Description("permission to manage estate access without notifying the owner of changes")]
        public const int PERMISSION_SILENT_ESTATE_MANAGEMENT = 0x4000;
        [APILevel(APIFlags.LSL)]
        [Description("permission to configure overriding of default animations")]
        public const int PERMISSION_OVERRIDE_ANIMATIONS = 0x8000;
        [APILevel(APIFlags.LSL)]
        [Description("permission to return object from parcels by llReturnObjectsByOwner and llReturnObjectsByID")]
        public const int PERMISSION_RETURN_OBJECTS = 0x10000;

        [APILevel(APIFlags.LSL, "run_time_permissions")]
        [StateEventDelegate]
        public delegate void State_run_time_permissions(int perm);

        [APILevel(APIFlags.LSL, "llGetPermissions")]
        public int GetPermissions(ScriptInstance instance)
        {
            lock (instance)
            {
                return (int)instance.Item.PermsGranter.PermsMask;
            }
        }

        [APILevel(APIFlags.LSL, "llGetPermissionsKey")]
        public LSLKey GetPermissionsKey(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Item.PermsGranter.PermsGranter.ID;
            }
        }

        [APILevel(APIFlags.LSL, "llRequestPermissions")]
        public void RequestPermissions(ScriptInstance instance, LSLKey agentID, int permissions)
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
                        instance.PostEvent(new RuntimePermissionsEvent
                        {
                            Permissions = perms,
                            PermissionsKey = a.Owner
                        });
                    }
                }
            }
        }

        [ExecutedOnScriptReset]
        public static void ResetPermissions(ScriptInstance instance)
        {
            lock (instance)
            {
                instance.Item.PermsGranter = null;
            }
        }
    }
}
