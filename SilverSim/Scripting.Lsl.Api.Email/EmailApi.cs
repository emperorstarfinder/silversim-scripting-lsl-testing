// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using System;

namespace SilverSim.Scripting.Lsl.Api.Email
{
    [ScriptApiName("Email")]
    [LSLImplementation]
    public class EmailApi : IScriptApi, IPlugin
    {
        public EmailApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL, "email")]
        [StateEventDelegate]
        public delegate void State_email(string time, string address, string subject, string message, int num_left);

        [APILevel(APIFlags.LSL, "llEmail")]
        public void Email(ScriptInstance instance, string address, string subject, string message)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetNextEmail")]
        public void GetNextEmail(ScriptInstance instance, string address, string subject)
        {
            throw new NotImplementedException();
        }
    }
}
