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

using SilverSim.Scene.Types.Script.Events;
using System.Threading;
using System.Timers;

namespace SilverSim.Scripting.Lsl
{
    public partial class Script
    {
        private readonly System.Timers.Timer Timer = new System.Timers.Timer();
        public long LastTimerEventTick;
        public double CurrentTimerInterval;
        public bool IsTimerEnabled => Timer.Enabled;
        public bool IsTimerOneshot { get; set; }
        public bool IsInTimerEvent { get; private set; }

        private void OnTimerEvent(object sender, ElapsedEventArgs e)
        {
            lock (m_Lock)
            {
                if (!m_HaveQueuedTimerEvent)
                {
                    PostEvent(new TimerEvent());
                }
                Interlocked.Exchange(ref LastTimerEventTick, TimeSource.TickCount);
                if (IsTimerOneshot)
                {
                    Timer.Enabled = false;
                }
                else
                {
                    Timer.Interval = CurrentTimerInterval * 1000;
                }
            }
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
                    Timer.Enabled = false;
                    Interlocked.Exchange(ref LastTimerEventTick, TimeSource.TickCount);
                    Timer.Interval = (interval - elapsed) * 1000;
                    CurrentTimerInterval = interval;
                    IsTimerOneshot = oneshot;
                    Timer.Enabled = true;
                }
            }
        }
    }
}
