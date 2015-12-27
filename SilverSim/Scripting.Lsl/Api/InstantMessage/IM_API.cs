// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.IM
{
    [ScriptApiName("InstantMessage")]
    [LSLImplementation]
    [Description("LSL/OSSL InstantMessage API")]
    public partial class InstantMessageApi : IScriptApi, IPlugin
    {
        public InstantMessageApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
    }
}
