// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
    [ServerParam("LSL.MaxTargetsPerScript", ParameterType = typeof(uint))]
    [ServerParam("LSL.MaxRotTargetsPerScript", ParameterType = typeof(uint))]
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
