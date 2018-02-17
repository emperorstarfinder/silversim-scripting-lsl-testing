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

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llResetTime")]
        public void ResetTime(ScriptInstance instance)
        {
            lock(instance)
            {
                var script = (Script)instance;
                script.GetAndResetTime();
            }
        }

        [APILevel(APIFlags.LSL, "llGetTime")]
        public double GetTime(ScriptInstance instance)
        {
            lock (instance)
            {
                var script = (Script)instance;
                return script.GetTime();
            }
        }

        [APILevel(APIFlags.LSL, "llGetAndResetTime")]
        public double GetAndResetTime(ScriptInstance instance)
        {
            lock(instance)
            {
                var script = (Script)instance;
                return script.GetAndResetTime();
            }
        }

        [APIExtension(APIExtension.Properties, "executiontime")]
        [APIDisplayName("executiontime")]
        [APIAccessibleMembers]
        public class ExecutionTime
        {
            private readonly Script m_Instance;

            public ExecutionTime(Script instance)
            {
                m_Instance = instance;
            }

            public double Now
            {
                get
                {
                    lock(m_Instance)
                    {
                        return m_Instance.GetTime();
                    }
                }
            }

            public double GetAndReset()
            {
                lock(m_Instance)
                {
                    return m_Instance.GetAndResetTime();
                }
            }
        }

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "GetAndReset")]
        public double GetAndResetTime(ExecutionTime time) => time.GetAndReset();

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Reset")]
        public void ResetTime(ExecutionTime time) => time.GetAndReset();

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "ExecutionTime")]
        public ExecutionTime GetAccessor(ScriptInstance instance) => new ExecutionTime((Script)instance);
    }
}
