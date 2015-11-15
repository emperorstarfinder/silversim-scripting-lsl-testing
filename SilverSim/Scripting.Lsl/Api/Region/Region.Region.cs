// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Region
{
    public partial class RegionApi
    {
        [APILevel(APIFlags.LSL, "llGetRegionName")]
        public string GetRegionName(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.Name;
            }
        }

        [APILevel(APIFlags.LSL, "llGetRegionTimeDilation")]
        public double GetRegionTimeDilation(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetSimulatorHostname")]
        [ForcedSleep(10)]
        public string GetSimulatorHostname(ScriptInstance instance)
        {
            lock(this)
            {
                Uri uri = new Uri(instance.Part.ObjectGroup.Scene.RegionData.ServerURI);
                return uri.Host;
            }
        }

        [APILevel(APIFlags.LSL, "llGetRegionCorner")]
        public Vector3 GetRegionCorner(ScriptInstance instance)
        {
            lock(this)
            {
                return instance.Part.ObjectGroup.Scene.RegionData.Location;
            }
        }

        [APILevel(APIFlags.LSL, "llRequestSimulatorData")]
        [ForcedSleep(1)]
        public LSLKey RequestSimulatorData(ScriptInstance instance, string region, int data)
        {
            throw new NotImplementedException("llRequestSimulatorData(string, integer)");
        }

        [APILevel(APIFlags.LSL, "llGetSunDirection")]
        public Vector3 GetSunDirection(ScriptInstance instance)
        {
            throw new NotImplementedException("GetSunDirection");
        }

        [APILevel(APIFlags.LSL, "llGround")]
        public double Ground(ScriptInstance instance, Vector3 offset)
        {
            lock(instance)
            {
                Vector3 regionPos = instance.Part.GlobalPosition + offset;
                return instance.Part.ObjectGroup.Scene.Terrain[regionPos];
            }
        }

        [APILevel(APIFlags.LSL, "llGetEnv")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public string GetEnv(ScriptInstance instance, string name)
        {
            switch (name)
            {
                case "agent_limit":
                    lock (instance)
                    {
                        return instance.Part.ObjectGroup.Scene.RegionSettings.AgentLimit.ToString();
                    }

                case "dynamic_pathfinding":
                    return "disabled";

                case "estate_id":
                    lock (instance)
                    {
                        return instance.Part.ObjectGroup.Scene.EstateService.RegionMap[instance.Part.ObjectGroup.Scene.ID].ToString();
                    }

                case "estate_name":
                    lock (instance)
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
