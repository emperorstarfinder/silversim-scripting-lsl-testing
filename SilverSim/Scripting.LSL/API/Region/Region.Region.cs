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
        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetRegionName")]
        public string GetRegionName(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.Name;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(10)]
        [ScriptFunctionName("llGetSimulatorHostname")]
        public string GetSimulatorHostname(ScriptInstance instance)
        {
#warning Implement llGetSimulatorHostname()
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetRegionCorner")]
        public Vector3 GetRegionCorner(ScriptInstance instance)
        {
#warning Implement llGetRegionCorner()
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(1)]
        [ScriptFunctionName("llRequestSimulatorData")]
        public LSLKey RequestSimulatorData(ScriptInstance instance, string region, int data)
        {
#warning Implement llRequestSimulatorData()
            throw new NotImplementedException();
        }
    }
}
