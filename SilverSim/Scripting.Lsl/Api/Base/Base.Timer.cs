// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using System;
using System.Collections.Generic;
using System.Globalization;

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
                script.SetTimerEvent(sec);
            }
        }

        [ExecutedOnDeserialization("timer")]
        public void Deserialize(ScriptInstance instance, List<object> param)
        {
            if(param.Count < 2)
            {
                return;
            }
            Script script = (Script)instance;
            lock(script)
            {
                double interval = (double)param[0];
                double elapsed = (double)param[1];
                elapsed %= interval;
                script.SetTimerEvent(interval, elapsed);
            }
        }

        [ExecutedOnSerialization("timer")]
        public void Serialize(ScriptInstance instance, List<object> res)
        {
            Script script = (Script)instance;
            lock(script)
            {
                if (script.Timer.Enabled)
                {
                    res.Add("timer");
                    res.Add(2);
                    double interval = script.CurrentTimerInterval;
                    res.Add(interval);
                    int timeElapsed = Environment.TickCount - script.LastTimerEventTick;
                    double timeToElapse = interval - timeElapsed / 1000f;
                    res.Add(timeToElapse);
                }
            }
        }
    }
}
