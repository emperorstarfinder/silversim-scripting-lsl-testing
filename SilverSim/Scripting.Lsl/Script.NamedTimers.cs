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

using SilverSim.Scripting.Lsl.Event;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;

namespace SilverSim.Scripting.Lsl
{
    public partial class Script
    {
        public string ActiveNamedTimer { get; private set; }

        public sealed class TimerInfo
        {
            public readonly string Name;
            public bool IsPending;
            public bool IsOneshot;
            public bool IsActive => Timer.Enabled;
            public long LastTimerEventTick;
            public double CurrentTimerInterval;
            public System.Timers.Timer Timer = new System.Timers.Timer();
            private readonly object m_Lock = new object();
            private readonly Script m_Script;

            public TimerInfo(string name, Script script)
            {
                Name = name;
                m_Script = script;
                Timer.Elapsed += OnTimerEvent;
            }

            private void OnTimerEvent(object sender, ElapsedEventArgs e)
            {
                lock (m_Lock)
                {
                    if (!IsPending)
                    {
                        m_Script.PostEvent(new NamedTimerEvent { TimerName = Name });
                    }
                    Interlocked.Exchange(ref LastTimerEventTick, TimeSource.TickCount);
                    if (!IsOneshot)
                    {
                        Timer.Interval = CurrentTimerInterval * 1000;
                    }
                    else
                    {
                        Timer.Enabled = false;
                    }
                }
            }

            public void AckTimer()
            {
                IsPending = false;
            }

            public void Reset() => SetTimerEvent(0);

            public void Stop()
            {
                Timer.Stop();
                Timer.Elapsed -= OnTimerEvent;
            }

            public void SetTimerEvent(double interval, double elapsed = 0f, bool oneshot = false)
            {
                lock (m_Lock)
                {
                    CurrentTimerInterval = interval;
                    if (interval < 0.01)
                    {
                        Timer.Enabled = false;
                    }
                    else
                    {
                        IsOneshot = oneshot;
                        Timer.Enabled = false;
                        Interlocked.Exchange(ref LastTimerEventTick, TimeSource.TickCount);
                        Timer.Interval = (interval - elapsed) * 1000;
                        CurrentTimerInterval = interval;
                        Timer.Enabled = true;
                    }
                }
            }
        }

        private readonly Dictionary<string, TimerInfo> m_Timers = new Dictionary<string, TimerInfo>();

        public TimerInfo[] NamedTimers => m_Timers.Values.ToArray();

        public void StopAllNamedTimers()
        {
            foreach(TimerInfo timer in m_Timers.Values)
            {
                timer.Stop();
            }
        }

        protected void RegisterNamedTimer(string name) => m_Timers.Add(name, new TimerInfo(name, this));

        public bool SetTimerEvent(string name, double interval, double elapsed = 0f, bool oneshot = false)
        {
            TimerInfo ti;
            if(string.IsNullOrEmpty(name))
            {
                SetTimerEvent(interval, elapsed, oneshot);
                return true;
            }
            if(m_Timers.TryGetValue(name, out ti))
            {
                ti.SetTimerEvent(interval, elapsed, oneshot);
                return true;
            }
            return false;
        }

        public bool SetTimerOneshot(string name,bool oneshot)
        {
            TimerInfo ti;
            if (m_Timers.TryGetValue(name, out ti))
            {
                ti.IsOneshot = oneshot;
                return true;
            }
            return false;
        }

        public bool TryGetTimerInterval(string name, out double interval)
        {
            TimerInfo ti;
            if(m_Timers.TryGetValue(name, out ti))
            {
                interval = ti.CurrentTimerInterval;
                return true;
            }
            interval = 0;
            return false;
        }

        public bool TryGetIsOneshot(string name, out bool oneshot)
        {
            TimerInfo ti;
            if (m_Timers.TryGetValue(name, out ti))
            {
                oneshot = ti.IsOneshot;
                return true;
            }
            oneshot = false;
            return false;
        }

        public bool HaveTimer(string name) => m_Timers.ContainsKey(name);
    }
}
