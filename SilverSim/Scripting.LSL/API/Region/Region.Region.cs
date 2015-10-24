// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using System;
using SilverSim.Scripting.Common;

namespace SilverSim.Scripting.LSL.API.Region
{
    public partial class Region_API
    {
        [APILevel(APIFlags.LSL, "llGetRegionName")]
        public string GetRegionName(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.Name;
            }
        }

        [APILevel(APIFlags.LSL, "llGetSimulatorHostname")]
        [ForcedSleep(10)]
        public string GetSimulatorHostname(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetRegionCorner")]
        public Vector3 GetRegionCorner(ScriptInstance instance)
        {
            throw new NotImplementedException("llGetRegionCorner()");
        }

        [APILevel(APIFlags.LSL, "llRequestSimulatorData")]
        [ForcedSleep(1)]
        public LSLKey RequestSimulatorData(ScriptInstance instance, string region, int data)
        {
            throw new NotImplementedException("llRequestSimulatorData(string, integer)");
        }
    }
}
