// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        [APILevel(APIFlags.LSL, "llSetRemoteScriptAccessPin")]
        public void SetRemoteScriptAccessPin(ScriptInstance instance, int accesspin)
        {
            lock(instance)
            {
                instance.Part.ScriptAccessPin = accesspin;
            }
        }
    }
}
