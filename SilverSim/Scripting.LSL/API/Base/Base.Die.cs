// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Object;
using SilverSim.Scripting.Common;

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDie")]
        public void Die(ScriptInstance instance)
        {
            instance.AbortBegin();
            instance.Part.ObjectGroup.Scene.Remove(instance.Part.ObjectGroup, instance);
            throw new ScriptAbortException();
        }
    }
}
