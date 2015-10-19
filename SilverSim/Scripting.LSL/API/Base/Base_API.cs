// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL.API.Base
{
    [ScriptApiName("Base")]
    [LSLImplementation]
    public partial class Base_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("at_rot_target")]
        public delegate void State_at_rot_target(int handle, Quaternion targetrot, Quaternion ourrot);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("at_target")]
        public delegate void State_at_target(int tnum, Vector3 targetpos, Vector3 ourpos);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("attach")]
        public delegate void State_attach(LSLKey id);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("changed")]
        public delegate void State_changed(int change);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("collision")]
        public delegate void State_collision(int num_detected);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("collision_end")]
        public delegate void State_collision_end(int num_detected);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("collision_start")]
        public delegate void State_collision_start(int num_detected);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("dataserver")]
        public delegate void State_dataserver(LSLKey queryid, string data);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("email")]
        public delegate void State_email(string time, string address, string subject, string message, int num_left);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("http_request")]
        public delegate void State_http_request(LSLKey request_id, string method, string body);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("http_response")]
        public delegate void State_http_response(LSLKey request_id, int status, AnArray metadata, string body);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("land_collision")]
        public delegate void State_land_collision(Vector3 pos);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("land_collision_end")]
        public delegate void State_land_collision_end(Vector3 pos);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("land_collision_start")]
        public delegate void State_land_collision_start(Vector3 pos);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("link_message")]
        public delegate void State_link_message(int sender_num, int num, string str, LSLKey id);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("listen")]
        public delegate void State_listen(int channel, string name, LSLKey id, string message);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("money")]
        public delegate void State_money(LSLKey id, int amount);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("moving_end")]
        public delegate void State_moving_end();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("moving_start")]
        public delegate void State_moving_start();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("no_sensor")]
        public delegate void State_no_sensor();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("not_at_rot_target")]
        public delegate void State_not_at_rot_target();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("not_at_target")]
        public delegate void State_not_at_target();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("object_rez")]
        public delegate void State_object_rez(LSLKey id);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("on_rez")]
        public delegate void State_on_rez(int start_param);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("path_update")]
        public delegate void State_path_update(int type, AnArray reserved);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("remote_data")]
        public delegate void State_remote_data(int event_type, LSLKey channel, LSLKey message_id, string sender, int idata, string sdata);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("sensor")]
        public delegate void State_sensor(int num_detected);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("state_entry")]
        public delegate void State_state_entry();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("state_exit")]
        public delegate void State_state_exit();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("timer")]
        public delegate void State_timer();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("touch")]
        public delegate void State_touch(int num_detected);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("touch_end")]
        public delegate void State_touch_end(int num_detected);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate("touch_start")]
        public delegate void State_touch_start(int num_detected);

        public Base_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSleep")]
        public void Sleep(ScriptInstance instance, double secs)
        {
            instance.Sleep(secs);
        }

        [APILevel(APIFlags.ASSL)]
        [ScriptFunctionName("asSetForcedSleep")]
        public void SetForcedSleep(ScriptInstance instance, int flag, double factor)
        {
            if(factor > 1)
            {
                factor = 1;
            }
            if(factor <= 0)
            {
                flag = 0;
            }
            lock(instance)
            {
                Script script = (Script)instance;
                script.ForcedSleepFactor = factor;
                script.UseForcedSleep = flag != 0;
            }
        }

        [APILevel(APIFlags.ASSL)]
        [ScriptFunctionName("asSetForcedSleepEnable")]
        public void SetForcedSleepEnable(ScriptInstance instance, int flag)
        {
            lock(instance)
            {
                Script script = (Script)instance;
                script.UseForcedSleep = flag != 0;
            }
        }
    }
}
