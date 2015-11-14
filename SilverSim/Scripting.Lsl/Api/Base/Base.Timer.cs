// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llSetTimerEvent")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void SetTimerEvent(ScriptInstance instance, double sec)
        {
            Script script = (Script)instance;
            lock (script)
            {
                script.Timer.Enabled = false;
                script.Timer.Interval = sec;
                script.Timer.Enabled = sec > 0.01;
            }
        }
    }
}
