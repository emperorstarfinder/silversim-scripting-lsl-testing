// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;

namespace SilverSim.Scripting.Lsl.Api.Controls
{
    [ScriptApiName("Controls")]
    [LSLImplementation]
    public partial class ControlsApi : IScriptApi, IPlugin
    {
        public ControlsApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CONTROL_FWD = 0x00000001;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CONTROL_BACK = 0x00000002;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CONTROL_LEFT = 0x00000004;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CONTROL_RIGHT = 0x00000008;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CONTROL_ROT_LEFT = 0x00000100;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CONTROL_ROT_RIGHT = 0x00000200;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CONTROL_UP = 0x00000010;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CONTROL_DOWN = 0x00000020;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int CONTROL_LBUTTON = 0x10000000;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
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
                    grantinfo.PermsGranter == UUI.Unknown)
                {
                    return;
                }
                IAgent agent;
                if (!instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(grantinfo.PermsGranter.ID, out agent))
                {
                    instance.ShoutError("llTakeControls: permission granter not in region");
                    return;
                }
                throw new NotImplementedException();
            }
        }

        [APILevel(APIFlags.LSL, "llReleaseControls")]
        public void ReleaseControls(ScriptInstance instance)
        {
            lock (instance)
            {
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                grantinfo.PermsMask &= (~ScriptPermissions.TakeControls);
                if(!instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(grantinfo.PermsGranter.ID, out agent))
                {
                    instance.ShoutError("llReleaseControls: permission granter not in region");
                    return;
                }
                throw new NotImplementedException();
            }
        }
    }
}
