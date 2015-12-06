// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using System;

namespace SilverSim.Scripting.Lsl.Api.Sensor
{
    [ScriptApiName("Sensor")]
    [LSLImplementation]
    public class SensorApi : IScriptApi, IPlugin
    {
        public SensorApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL, "sensor")]
        [StateEventDelegate]
        public delegate void State_sensor(int num_detected);

        [APILevel(APIFlags.LSL, "no_sensor")]
        [StateEventDelegate]
        public delegate void State_no_sensor();

        [APILevel(APIFlags.LSL, "llSensor")]
        public void Sensor(ScriptInstance instance, string name, LSLKey id, int type, double radius, double arc)
        {
            throw new NotImplementedException("llSensor(string, key, integer, float, float)");
        }

        [APILevel(APIFlags.LSL, "llSensorRepeat")]
        public void SensorRepeat(ScriptInstance instance, string name, LSLKey id, int type, double range, double arc, double rate)
        {
            /* there is only one repeating sensor per script */
            throw new NotImplementedException("llSensorRepeat(string, key, integer, float, float, float)");
        }

        [APILevel(APIFlags.LSL, "llSensorRemove")]
        public void SensorRemove(ScriptInstance instance)
        {
            throw new NotImplementedException("llSensorRemove()");
        }
    }
}
