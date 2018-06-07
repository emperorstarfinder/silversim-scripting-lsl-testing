// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System.Collections.Generic;
using System.Threading;

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
            var script = (Script)instance;
            lock (script)
            {
                script.SetTimerEvent(sec);
            }
        }

        [APILevel(APIFlags.ASSL, "asSetTimerEvent")]
        public void SetTimerEvent(ScriptInstance instance, string name, double sec)
        {
            var script = (Script)instance;
            lock(script)
            {
                if (string.IsNullOrEmpty(name))
                {
                    script.SetTimerEvent(sec);
                }
                else
                {
                    script.SetTimerEvent(name, sec);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asSetTimerEvent")]
        public void SetTimerEvent(ScriptInstance instance, string name, double sec, int autostop)
        {
            var script = (Script)instance;
            lock (script)
            {
                if (string.IsNullOrEmpty(name))
                {
                    script.SetTimerEvent(sec, 0, autostop != 0);
                }
                else
                {
                    script.SetTimerEvent(name, sec, 0, autostop != 0);
                }
            }
        }

        [ExecutedOnDeserialization("timer")]
        public void Deserialize(ScriptInstance instance, List<object> param)
        {
            if(param.Count < 2)
            {
                return;
            }
            var script = (Script)instance;
            lock(script)
            {
                var interval = (long)param[0] / 10000000.0;
                var elapsed = (long)param[1] / 10000000.0;
                elapsed %= interval;
                script.SetTimerEvent(interval, elapsed, param.Count > 2 && (bool)param[2]);
            }
        }

        [ExecutedOnSerialization("timer")]
        public void Serialize(ScriptInstance instance, List<object> res)
        {
            var script = (Script)instance;
            lock(script)
            {
                if (script.IsTimerEnabled)
                {
                    bool isTimerAutoStop = script.IsTimerOneshot;
                    res.Add("timer");
                    res.Add(isTimerAutoStop ? 3 : 2);
                    var interval = (long)(script.CurrentTimerInterval * Script.TimeSource.Frequency);
                    res.Add(interval);
                    long timeElapsed = Script.TimeSource.TicksElapsed(Script.TimeSource.TickCount, Interlocked.Read(ref script.LastTimerEventTick));
                    long timeToElapse = (interval - timeElapsed) * 100000000 / Script.TimeSource.Frequency;
                    res.Add(timeToElapse);
                    if(isTimerAutoStop)
                    {
                        res.Add(true);
                    }
                }
            }
        }

        [ExecutedOnDeserialization("namedtimer")]
        public void DeserializeNamed(ScriptInstance instance, List<object> param)
        {
            var script = (Script)instance;
            lock (script)
            {
                for (int paramPos = 0; paramPos + 3 < param.Count; paramPos += 4)
                {
                    var interval = (long)param[paramPos + 1] / 10000000.0;
                    var elapsed = (long)param[paramPos + 2] / 10000000.0;
                    elapsed %= interval;
                    script.SetTimerEvent(param[paramPos + 0].ToString(), interval, elapsed, (bool)param[paramPos + 3]);
                }
            }
        }

        [ExecutedOnSerialization("namedtimer")]
        public void SerializeNamed(ScriptInstance instance, List<object> res)
        {
            var script = (Script)instance;
            lock (script)
            {
                Script.TimerInfo[] timers = script.NamedTimers;
                if (timers.Length > 0)
                {
                    res.Add("namedtimer");
                    res.Add(0);
                    foreach(Script.TimerInfo ti in timers)
                    {
                        if (ti.IsActive)
                        {
                            res.Add(ti.Name);
                            var interval = (long)(script.CurrentTimerInterval * Script.TimeSource.Frequency);
                            res.Add(interval);
                            long timeElapsed = Script.TimeSource.TicksElapsed(Script.TimeSource.TickCount, Interlocked.Read(ref script.LastTimerEventTick));
                            long timeToElapse = (interval - timeElapsed) * 100000000 / Script.TimeSource.Frequency;
                            res.Add(timeToElapse);
                            res.Add(ti.IsOneshot);
                        }
                    }
                    res[1] = res.Count - 2;
                }
            }
        }
    }
}
