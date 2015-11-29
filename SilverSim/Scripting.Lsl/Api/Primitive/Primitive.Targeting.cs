// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
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
            throw new NotImplementedException("llTarget");
        }

        [APILevel(APIFlags.LSL, "llTargetRemove")]
        public void TargetRemove(ScriptInstance instance, int handle)
        {
            throw new NotImplementedException("llTargetRemove");
        }

        [APILevel(APIFlags.LSL, "llRotTarget")]
        public int RotTarget(ScriptInstance instance, Quaternion rot, double error)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llRotTargetRemove")]
        public void RotTargetRemove(ScriptInstance instance, int handle)
        {
            throw new NotImplementedException();
        }
    }
}
