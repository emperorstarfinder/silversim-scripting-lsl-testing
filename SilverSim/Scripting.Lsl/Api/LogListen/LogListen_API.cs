// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using System;

namespace SilverSim.Scripting.Lsl.Api.LogListen
{
    [ScriptApiName("LogListen")]
    [LSLImplementation]
    public class LogListenApi : IScriptApi, IPlugin
    {
        public LogListenApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APIExtension(APIExtension.Admin, "asLogListen")]
        public void LogListen(ScriptInstance instance, int onChannel, int enable)
        {
            throw new NotImplementedException("asLogListen(integer, integer)");
        }
    }
}
