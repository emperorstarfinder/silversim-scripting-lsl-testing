// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "timer")]
        [StateEventDelegate]
        public delegate void State_timer();

        [APILevel(APIFlags.LSL, "llSetTimerEvent")]
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
