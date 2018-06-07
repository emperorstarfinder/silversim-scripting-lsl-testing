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
using System;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public sealed partial class BaseApi
    {
        [APIExtension(APIExtension.Properties, "intervaltimer")]
        [APIDisplayName("intervaltimer")]
        [APIAccessibleMembers(
            "Interval",
            "IsInEvent")]
        [APIIsVariableType]
        public sealed class TimerControl
        {
            private readonly WeakReference<ScriptInstance> WeakInstance;
            public string TimerName;

            public TimerControl(ScriptInstance instance, string name = "")
            {
                TimerName = name;
                WeakInstance = new WeakReference<ScriptInstance>(instance);
            }

            public TimerControl this[string name]
            {
                get
                {
                    ScriptInstance instance;
                    if (WeakInstance.TryGetTarget(out instance))
                    {
                        return new TimerControl(instance, name);
                    }
                    else
                    {
                        return new TimerControl(null);
                    }
                }
            }


            public bool IsInEvent
            {
                get
                {
                    bool inevent = false;
                    ScriptInstance instance;
                    if (WeakInstance.TryGetTarget(out instance))
                    {
                        var script = (Script)instance;
                        lock (script)
                        {
                            inevent = script.IsInTimerEvent &&
                                script.ActiveNamedTimer == TimerName;
                        }
                    }
                    return inevent;
                }
            }

            public static implicit operator bool(TimerControl control) => control.IsValid();

            public bool IsValid()
            {
                bool found = false;
                if (string.IsNullOrEmpty(TimerName))
                {
                    found = true;
                }
                else
                {
                    ScriptInstance instance;
                    if (WeakInstance.TryGetTarget(out instance))
                    {
                        var script = (Script)instance;
                        lock (script)
                        {
                            found = script.HaveTimer(TimerName);
                        }
                    }
                }
                return found;
            }

            public double Interval
            {
                get
                {
                    ScriptInstance instance;
                    if (WeakInstance.TryGetTarget(out instance))
                    {
                        var script = (Script)instance;
                        lock (script)
                        {
                            double interval;
                            if(string.IsNullOrEmpty(TimerName))
                            {
                                return script.CurrentTimerInterval;
                            }
                            else if(script.TryGetTimerInterval(TimerName, out interval))
                            {
                                return interval;
                            }
                        }
                    }
                    return 0;
                }
                set
                {
                    ScriptInstance instance;
                    if (WeakInstance.TryGetTarget(out instance))
                    {
                        var script = (Script)instance;
                        lock (script)
                        {
                            if (string.IsNullOrEmpty(TimerName))
                            {
                                script.SetTimerEvent(value);
                            }
                            else
                            {
                                script.SetTimerEvent(TimerName, value);
                            }
                        }
                    }
                }
            }
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "Timer")]
        public TimerControl GetScript(ScriptInstance instance)
        {
            lock (instance)
            {
                return new TimerControl(instance);
            }
        }

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Stop")]
        public void StopTimer(TimerControl control)
        {
            control.Interval = 0;
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "IsInTimerEvent")]
        public int IsInTimerEvent(ScriptInstance instance)
        {
            lock(instance)
            {
                var script = (Script)instance;
                return script.IsInTimerEvent.ToLSLBoolean();
            }
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "ActiveTimerName")]
        public string ActiveTimerName(ScriptInstance instance)
        {
            lock (instance)
            {
                var script = (Script)instance;
                return script.ActiveNamedTimer;
            }
        }
    }
}
