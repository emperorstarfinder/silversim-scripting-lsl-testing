// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    [ScriptApiName("Base")]
    [LSLImplementation]
    [Description("LSL/OSSL Base API")]
    public partial class BaseApi : IScriptApi, IPlugin
    {
        [APILevel(APIFlags.LSL, "attach")]
        [StateEventDelegate]
        public delegate void State_attach(LSLKey id);

        [APILevel(APIFlags.LSL, "changed")]
        [StateEventDelegate]
        public delegate void State_changed(int change);

        [APILevel(APIFlags.LSL, "collision")]
        [StateEventDelegate]
        public delegate void State_collision(int num_detected);

        [APILevel(APIFlags.LSL, "collision_end")]
        [StateEventDelegate]
        public delegate void State_collision_end(int num_detected);

        [APILevel(APIFlags.LSL, "collision_start")]
        [StateEventDelegate]
        public delegate void State_collision_start(int num_detected);

        [APILevel(APIFlags.LSL, "dataserver")]
        [StateEventDelegate]
        public delegate void State_dataserver(LSLKey queryid, string data);

        [APILevel(APIFlags.LSL, "http_request")]
        [StateEventDelegate]
        public delegate void State_http_request(LSLKey request_id, string method, string body);

        [APILevel(APIFlags.LSL, "http_response")]
        [StateEventDelegate]
        public delegate void State_http_response(LSLKey request_id, int status, AnArray metadata, string body);

        [APILevel(APIFlags.LSL, "land_collision")]
        [StateEventDelegate]
        public delegate void State_land_collision(Vector3 pos);

        [APILevel(APIFlags.LSL, "land_collision_end")]
        [StateEventDelegate]
        public delegate void State_land_collision_end(Vector3 pos);

        [APILevel(APIFlags.LSL, "land_collision_start")]
        [StateEventDelegate]
        public delegate void State_land_collision_start(Vector3 pos);

        [APILevel(APIFlags.LSL, "link_message")]
        [StateEventDelegate]
        public delegate void State_link_message(int sender_num, int num, string str, LSLKey id);

        [APILevel(APIFlags.LSL, "money")]
        [StateEventDelegate]
        public delegate void State_money(LSLKey id, int amount);

        [APILevel(APIFlags.LSL, "moving_end")]
        [StateEventDelegate]
        public delegate void State_moving_end();

        [APILevel(APIFlags.LSL, "moving_start")]
        [StateEventDelegate]
        public delegate void State_moving_start();

        [APILevel(APIFlags.LSL, "object_rez")]
        [StateEventDelegate]
        public delegate void State_object_rez(LSLKey id);

        [APILevel(APIFlags.LSL, "on_rez")]
        [StateEventDelegate]
        public delegate void State_on_rez(int start_param);

        [APILevel(APIFlags.LSL, "path_update")]
        [StateEventDelegate]
        public delegate void State_path_update(int type, AnArray reserved);

        [APILevel(APIFlags.LSL, "state_entry")]
        [StateEventDelegate]
        public delegate void State_state_entry();

        [APILevel(APIFlags.LSL, "state_exit")]
        [StateEventDelegate]
        public delegate void State_state_exit();

        [APILevel(APIFlags.LSL, "touch")]
        [StateEventDelegate]
        public delegate void State_touch(int num_detected);

        [APILevel(APIFlags.LSL, "touch_end")]
        [StateEventDelegate]
        public delegate void State_touch_end(int num_detected);

        [APILevel(APIFlags.LSL, "touch_start")]
        [StateEventDelegate]
        public delegate void State_touch_start(int num_detected);

        public BaseApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL, "llSleep")]
        public void Sleep(ScriptInstance instance, double secs)
        {
            instance.Sleep(secs);
        }

        [APILevel(APIFlags.ASSL, "asSetForcedSleep")]
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

        [APILevel(APIFlags.ASSL, "asSetForcedSleepEnable")]
        public void SetForcedSleepEnable(ScriptInstance instance, int flag)
        {
            lock(instance)
            {
                Script script = (Script)instance;
                script.UseForcedSleep = flag != 0;
            }
        }

        [APILevel(APIFlags.OSSL, "osGetScriptEngineName")]
        public string GetScriptEngineName(ScriptInstance instance)
        {
            return "Porthos";
        }
    }
}
