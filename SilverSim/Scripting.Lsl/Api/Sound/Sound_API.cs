// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Sound
{
    [ScriptApiName("Sound")]
    [LSLImplementation]
    [Description("LSL Sound API")]
    public partial class SoundApi : IScriptApi, IPlugin
    {
        public SoundApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
    }
}
