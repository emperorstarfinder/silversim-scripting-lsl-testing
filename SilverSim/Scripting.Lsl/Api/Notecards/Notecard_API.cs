// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Notecards
{
    [ScriptApiName("Notecard")]
    [LSLImplementation]
    [Description("LSL/OSSL Notecard API")]
    public partial class NotecardApi : IScriptApi, IPlugin
    {
        public NotecardApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
    }
}
