// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Object;
using SilverSim.Scripting.Common;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.LSL.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llDie")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void Die(ScriptInstance instance)
        {
            instance.AbortBegin();
            instance.Part.ObjectGroup.Scene.Remove(instance.Part.ObjectGroup, instance);
            throw new ScriptAbortException();
        }
    }
}
