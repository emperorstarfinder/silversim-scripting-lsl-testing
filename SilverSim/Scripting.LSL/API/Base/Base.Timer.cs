// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
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
