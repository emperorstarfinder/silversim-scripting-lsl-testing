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
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Targeting
{
    [ScriptApiName("Targeting")]
    [LSLImplementation]
    [Description("LSL Targeting API")]
    [ServerParam("LSL.MaxTargetsPerScript", ParameterType = typeof(uint), DefaultValue = 8)]
    [ServerParam("LSL.MaxRotTargetsPerScript", ParameterType = typeof(uint), DefaultValue = 8)]
    public class TargetingApi : IScriptApi, IPlugin
    {
        public TargetingApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        int GetMaxPosTargets(UUID regionID)
        {
            int value;
            if (m_MaxPosTargetHandleParams.TryGetValue(regionID, out value) ||
                m_MaxPosTargetHandleParams.TryGetValue(UUID.Zero, out value))
            {
                return value;
            }
            return 8;
        }

        int GetMaxRotTargets(UUID regionID)
        {
            int value;
            if (m_MaxRotTargetHandleParams.TryGetValue(regionID, out value) ||
                m_MaxRotTargetHandleParams.TryGetValue(UUID.Zero, out value))
            {
                return value;
            }
            return 8;
        }

        readonly RwLockedDictionary<UUID, int> m_MaxPosTargetHandleParams = new RwLockedDictionary<UUID, int>();
        readonly RwLockedDictionary<UUID, int> m_MaxRotTargetHandleParams = new RwLockedDictionary<UUID, int>();

        [ServerParam("LSL.MaxTargetsPerScript")]
        public void MaxTargetsPerScriptUpdated(UUID regionID, string value)
        {
            int intval;
            if (value.Length == 0)
            {
                m_MaxPosTargetHandleParams.Remove(regionID);
            }
            else if (int.TryParse(value, out intval))
            {
                m_MaxPosTargetHandleParams[regionID] = intval;
            }
        }

        [ServerParam("LSL.MaxRotTargetsPerScript")]
        public void MaxRotTargetsPerScriptUpdated(UUID regionID, string value)
        {
            int intval;
            if (value.Length == 0)
            {
                m_MaxRotTargetHandleParams.Remove(regionID);
            }
            else if (int.TryParse(value, out intval))
            {
                m_MaxRotTargetHandleParams[regionID] = intval;
            }
        }

        [APILevel(APIFlags.LSL, "at_rot_target")]
        [StateEventDelegate]
        public delegate void State_at_rot_target(int handle, Quaternion targetrot, Quaternion ourrot);

        [APILevel(APIFlags.LSL, "at_target")]
        [StateEventDelegate]
        public delegate void State_at_target(int tnum, Vector3 targetpos, Vector3 ourpos);

        [APILevel(APIFlags.LSL, "not_at_rot_target")]
        [StateEventDelegate]
        public delegate void State_not_at_rot_target();

        [APILevel(APIFlags.LSL, "not_at_target")]
        [StateEventDelegate]
        public delegate void State_not_at_target();

        [APILevel(APIFlags.LSL, "llTarget")]
        public int Target(ScriptInstance instance, Vector3 position, double range)
        {
            throw new NotImplementedException("llTarget(vector, float)");
        }

        [APILevel(APIFlags.LSL, "llTargetRemove")]
        public void TargetRemove(ScriptInstance instance, int handle)
        {
            throw new NotImplementedException("llTargetRemove(integer)");
        }

        [APILevel(APIFlags.LSL, "llRotTarget")]
        public int RotTarget(ScriptInstance instance, Quaternion rot, double error)
        {
            throw new NotImplementedException("llRotTarget(rotation, float)");
        }

        [APILevel(APIFlags.LSL, "llRotTargetRemove")]
        public void RotTargetRemove(ScriptInstance instance, int handle)
        {
            throw new NotImplementedException("llRotTargetRemove(integer)");
        }
    }
}
