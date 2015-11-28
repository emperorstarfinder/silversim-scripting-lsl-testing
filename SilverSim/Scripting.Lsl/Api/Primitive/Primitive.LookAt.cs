using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
