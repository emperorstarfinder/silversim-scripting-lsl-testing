// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        [APILevel(APIFlags.LSL, "llLookAt")]
        public void LookAt(ScriptInstance instance, Vector3 target, double strength, double damping)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llRotLookAt")]
        public void RotLookAt(ScriptInstance instance, Quaternion target_direction, double strength, double damping)
        {
            throw new NotImplementedException();
        }
    }
}
