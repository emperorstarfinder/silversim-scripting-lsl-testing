// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using System;
using SilverSim.Scripting.Common;
using SilverSim.Main.Common;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Region
{
    public partial class RegionApi
    {
        [APILevel(APIFlags.LSL, "llGetRegionName")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        string GetRegionName(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.Name;
            }
        }

        [APILevel(APIFlags.LSL, "llGetSimulatorHostname")]
        [ForcedSleep(10)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        string GetSimulatorHostname(ScriptInstance instance)
        {
            lock(this)
            {
                Uri uri = new Uri(instance.Part.ObjectGroup.Scene.RegionData.ServerURI);
                return uri.Host;
            }
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetRegionCorner")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        Vector3 GetRegionCorner(ScriptInstance instance)
        {
            lock(this)
            {
                return instance.Part.ObjectGroup.Scene.RegionData.Location;
            }
        }

        [APILevel(APIFlags.LSL, "llRequestSimulatorData")]
        [ForcedSleep(1)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        LSLKey RequestSimulatorData(ScriptInstance instance, string region, int data)
        {
            throw new NotImplementedException("llRequestSimulatorData(string, integer)");
        }

        [APILevel(APIFlags.LSL, "llGetEnv")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        string llGetEnv(ScriptInstance instance, string name)
        {
            switch(name)
            {
                case "agent_limit":
                    lock(instance)
                    {
                        return instance.Part.ObjectGroup.Scene.RegionSettings.AgentLimit.ToString();
                    }

                case "dynamic_pathfinding":
                    return "disabled";
                    
                case "estate_id":
                    lock(instance)
                    {
                        return instance.Part.ObjectGroup.Scene.EstateService.RegionMap[instance.Part.ObjectGroup.Scene.ID].ToString();
                    }
                    
                case "estate_name":
                    lock(instance)
                    {
                        return instance.Part.ObjectGroup.Scene.EstateService[instance.Part.ObjectGroup.Scene.EstateService.RegionMap[instance.Part.ObjectGroup.Scene.ID]].Name;
                    }

                case "frame_number":
                    return "0";

                case "region_cpu_ratio":
                    return "1";

                case "region_idle":
                    return "0";

                case "region_product_name":
                    return string.Empty;

                case "region_product_sku":
                    return string.Empty;

                case "region_start_time":
                    return "0";

                case "sim_channel":
                    return VersionInfo.ProductName;

                case "sim_version":
                    return VersionInfo.Version;

                case "simulator_hostname":
                    return GetSimulatorHostname(instance);

                default:
                    return string.Empty;
            }
        }
    }
}
