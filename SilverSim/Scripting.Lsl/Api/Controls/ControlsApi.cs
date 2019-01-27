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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Script;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Controls
{
    [ScriptApiName("Controls")]
    [LSLImplementation]
    [Description("LSL/OSSL Controls API")]
    public class ControlsApi : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL)]
        public const int CONTROL_FWD = 0x00000001;
        [APILevel(APIFlags.LSL)]
        public const int CONTROL_BACK = 0x00000002;
        [APILevel(APIFlags.LSL)]
        public const int CONTROL_LEFT = 0x00000004;
        [APILevel(APIFlags.LSL)]
        public const int CONTROL_RIGHT = 0x00000008;
        [APILevel(APIFlags.LSL)]
        public const int CONTROL_UP = 0x00000010;
        [APILevel(APIFlags.LSL)]
        public const int CONTROL_DOWN = 0x00000020;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_ROT_UP = 0x00000040; /* same as PitchPos */
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_ROT_DOWN = 0x00000080; /* same as PitchNeg */
        [APILevel(APIFlags.LSL)]
        public const int CONTROL_ROT_LEFT = 0x00000100; /* same as YawPos */
        [APILevel(APIFlags.LSL)]
        public const int CONTROL_ROT_RIGHT = 0x00000200; /* same as YawNeg */
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_FAST_FWD_BACK = 0x00000400;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_FAST_LEFT_RIGHT = 0x00000800;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_FAST_UP_DOWN = 0x00001000;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_FLY = 0x00002000;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_STOP = 0x00004000;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_STAND_UP = 0x00010000;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_SIT_ON_GROUND = 0x00020000;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_MOUSE_LOOK = 0x00040000;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_NUDGE_FWD = 0x00080000;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_NUDGE_BACK = 0x00100000;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_NUDGE_LEFT = 0x00200000;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_NUDGE_RIGHT = 0x00400000;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_NUDGE_UP = 0x00800000;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_NUDGE_DOWN = 0x01000000;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_TURN_LEFT = 0x02000000;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_TURN_RIGHT = 0x04000000;
        [APILevel(APIFlags.ASSL)]
        public const int CONTROL_AWAY = 0x08000000;
        [APILevel(APIFlags.LSL)]
        public const int CONTROL_LBUTTON = 0x10000000;
        [APILevel(APIFlags.LSL)]
        public const int CONTROL_ML_LBUTTON = 0x40000000;

        [APILevel(APIFlags.LSL, "control")]
        [StateEventDelegate]
        public delegate void State_control(LSLKey id, int level, int edge);

        [APILevel(APIFlags.LSL, "llTakeControls")]
        public void TakeControls(ScriptInstance instance, int controls, int accept, int pass_on)
        {
            lock (instance)
            {
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if ((grantinfo.PermsMask & ScriptPermissions.TakeControls) == 0 ||
                    grantinfo.PermsGranter == UGUI.Unknown)
                {
                    return;
                }
                IAgent agent;
                if (!instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(grantinfo.PermsGranter.ID, out agent))
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0PermissionGranterNotInRegion", "{0}: permission granter not in region", "llTakeControls"));
                    return;
                }
                agent.TakeControls(instance, controls, accept, pass_on);
            }
        }

        [APILevel(APIFlags.LSL, "llReleaseControls")]
        public void ReleaseControls(ScriptInstance instance)
        {
            lock (instance)
            {
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                grantinfo.PermsMask &= ~ScriptPermissions.TakeControls;
                if(!instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(grantinfo.PermsGranter.ID, out agent))
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0PermissionGranterNotInRegion", "{0}: permission granter not in region", "llReleaseControls"));
                    return;
                }
                agent.ReleaseControls(instance);
            }
        }

        [ExecutedOnStateChange]
        [ExecutedOnScriptRemove]
        public static void ResetControls(ScriptInstance instance)
        {
            lock (instance)
            {
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(grantinfo.PermsGranter.ID, out agent))
                {
                    agent.ReleaseControls(instance);
                }
            }
        }
    }
}
