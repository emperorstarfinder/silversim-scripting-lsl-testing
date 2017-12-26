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

        [APILevel(APIFlags.LSL, "path_update")]
        [StateEventDelegate]
        public delegate void PathUpdateDelegate(int type, AnArray reserved);

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
                var script = (Script)instance;
                script.ForcedSleepFactor = factor;
                script.UseForcedSleep = flag != 0;
            }
        }

        [APILevel(APIFlags.ASSL, "asSetForcedSleepEnable")]
        public void SetForcedSleepEnable(ScriptInstance instance, int flag)
        {
            lock(instance)
            {
                var script = (Script)instance;
                script.UseForcedSleep = flag != 0;
            }
        }

        [APILevel(APIFlags.OSSL, "osGetScriptEngineName")]
        public string GetScriptEngineName(ScriptInstance instance) => "Porthos";
    }
}
