// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llResetTime")]
        public void ResetTime(ScriptInstance instance)
        {
            lock(instance)
            {
                instance.ExecutionTime = 0;
            }
        }

        [APILevel(APIFlags.LSL, "llGetTime")]
        public double GetTime(ScriptInstance instance)
        {
            double v;
            lock (instance)
            {
                v = instance.ExecutionTime;
            }
            return v;
        }

        [APILevel(APIFlags.LSL, "llGetAndResetTime")]
        public double GetAndResetTime(ScriptInstance instance)
        {
            double old;
            lock(instance)
            {
                old = instance.ExecutionTime;
                instance.ExecutionTime = 0;
            }
            return old;
        }
    }
}
