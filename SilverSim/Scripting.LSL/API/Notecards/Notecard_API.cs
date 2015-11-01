// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using System;

namespace SilverSim.Scripting.LSL.Api.Notecards
{
    [ScriptApiName("Notecard")]
    [LSLImplementation]
    public partial class NotecardApi : IScriptApi, IPlugin
    {
        public NotecardApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }
    }
}
