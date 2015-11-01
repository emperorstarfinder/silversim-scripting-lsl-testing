// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llResetTime")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void ResetTime(ScriptInstance instance)
        {
            lock(instance)
            {
                instance.ExecutionTime = 0;
            }
        }

        [APILevel(APIFlags.LSL, "llGetTime")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal double GetTime(ScriptInstance instance)
        {
            double v;
            lock (instance)
            {
                v = instance.ExecutionTime;
            }
            return v;
        }

        [APILevel(APIFlags.LSL, "llGetAndResetTime")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        double GetAndResetTime(ScriptInstance instance)
        {
            double old;
            lock(instance)
            {
                old = instance.ExecutionTime;
                instance.ExecutionTime = 0;
            }
            return old;
        }
    }
}
