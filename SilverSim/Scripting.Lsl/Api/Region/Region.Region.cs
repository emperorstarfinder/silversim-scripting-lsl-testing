// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Region
{
    public partial class RegionApi
    {
        [APILevel(APIFlags.OSSL, "osGetGridName")]
        public string GetGridName(ScriptInstance instance)
        {
            throw new NotImplementedException("osGetGridName()");
        }

        [APILevel(APIFlags.OSSL, "osGetGridNick")]
        public string GetGridNick(ScriptInstance instance)
        {
            throw new NotImplementedException("osGetGridNick()");
        }

        [APILevel(APIFlags.OSSL, "osGetGridLoginURI")]
        public string GetGridLoginURI(ScriptInstance instance)
        {
            throw new NotImplementedException("osGetgridLoginURI()");
        }

        [APILevel(APIFlags.OSSL, "osGetGridHomeURI")]
        public string GetGridHomeURI(ScriptInstance instance)
        {
            throw new NotImplementedException("osGetGridHomeURI()");
        }

        [APILevel(APIFlags.OSSL, "osGetGridGatekeeperURI")]
        public string GetGridGatekeeperURI(ScriptInstance instance)
        {
            lock(instance)
            {
                return instance.Part.ObjectGroup.Scene.RegionData.GridURI;
            }
        }

        [APILevel(APIFlags.OSSL, "osGetGridCustom")]
        public string GetGridCustom(ScriptInstance instance, string name)
        {
            throw new NotImplementedException("osGetGridCustom(string)");
        }

        [APILevel(APIFlags.LSL, "llGetRegionName")]
        public string GetRegionName(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.Name;
            }
        }

        [APILevel(APIFlags.OSSL, "osGetSimulatorVersion")]
        public string GetSimulatorVersion(ScriptInstance instance)
        {
            return VersionInfo.SimulatorVersion;
        }

        [APILevel(APIFlags.OSSL, "osGetRegionSize")]
        public Vector3 GetRegionSize(ScriptInstance instance)
        {
            lock(instance)
            {
                return instance.Part.ObjectGroup.Scene.RegionData.Size;
            }
        }

        [APILevel(APIFlags.OSSL, "osGetMapTexture")]
        public LSLKey GetMapTexture(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.RegionData.RegionMapTexture;
            }
        }

        [APILevel(APIFlags.OSSL, "osGetRegionMapTexture")]
        public LSLKey GetMapTexture(ScriptInstance instance, string regionName)
        {
            throw new NotImplementedException("osGetRegionMapTexture(string)");
        }

        [APILevel(APIFlags.OSSL, "osGetRegionStats")]
        public AnArray GetRegionStats(ScriptInstance instance)
        {
            throw new NotImplementedException("osGetRegionStats()");
        }

        [APILevel(APIFlags.OSSL, "osLoadedCreationDate")]
        public string LoadedCreationDate(ScriptInstance instance)
        {
            throw new NotImplementedException("osLoadedCreationDate()");
        }

        [APILevel(APIFlags.OSSL, "osLoadedCreationTime")]
        public string LoadedCreationTime(ScriptInstance instance)
        {
            throw new NotImplementedException("osLoadedCreationTime()");
        }

        [APILevel(APIFlags.OSSL, "osLoadedCreationID")]
        public LSLKey LoadedCreationID(ScriptInstance instance)
        {
            throw new NotImplementedException("osLoadedCreationID()");
        }

        [APILevel(APIFlags.OSSL, "osGetSimulatorMemory")]
        public int GetSimulatorMemory(ScriptInstance instance)
        {
            lock(instance)
            {
                instance.CheckThreatLevel("osGetSimulatorMemory", ScriptInstance.ThreatLevelType.Moderate);
                long pws = Process.GetCurrentProcess().WorkingSet64;
                if(pws > Int32.MaxValue)
                {
                    return Int32.MaxValue;
                }
                return (int)pws;
            }
        }

        [APILevel(APIFlags.LSL, "llGetRegionFlags")]
        public int GetRegionFlags(ScriptInstance instance)
        {
            int flags = 0;
            lock(instance)
            {
                RegionSettings settings = instance.Part.ObjectGroup.Scene.RegionSettings;
                if(settings.AllowDamage)
                {
                    flags |= REGION_FLAG_ALLOW_DAMAGE;
                }
                if(settings.BlockTerraform)
                {
                    flags |= REGION_FLAG_BLOCK_TERRAFORM;
                }
                if(settings.Sandbox)
                {
                    flags |= REGION_FLAG_SANDBOX;
                }
                if(settings.DisableCollisions)
                {
                    flags |= REGION_FLAG_DISABLE_COLLISIONS;
                }
                if(settings.DisablePhysics)
                {
                    flags |= REGION_FLAG_DISABLE_PHYSICS;
                }
                if(settings.BlockFly)
                {
                    flags |= REGION_FLAG_BLOCK_FLY;
                }
                if(settings.RestrictPushing)
                {
                    flags |= REGION_FLAG_RESTRICT_PUSHOBJECT;
                }
                if(!settings.AllowLandResell)
                {
                    flags |= REGION_FLAGS_BLOCK_LAND_RESELL;
                }
                if(settings.DisableScripts)
                {
                    flags |= REGION_FLAGS_SKIP_SCRIPTS;
                }
                if(settings.AllowLandJoinDivide)
                {
                    flags |= REGION_FLAGS_ALLOW_PARCEL_CHANGES;
                }
            }

            return flags;
        }

        [APILevel(APIFlags.LSL, "llGetRegionTimeDilation")]
        public double GetRegionTimeDilation(ScriptInstance instance)
        {
            lock(instance)
            {
                return instance.Part.ObjectGroup.Scene.PhysicsScene.PhysicsDilationTime;
            }
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

        [APILevel(APIFlags.LSL, "llGetRegionAgentCount")]
        public int GetRegionAgentCount(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.Agents.Count;
            }
        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int DATA_SIM_POS = 5;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int DATA_SIM_STATUS = 6;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int DATA_SIM_RATING = 7;

        [APILevel(APIFlags.LSL, "llScriptDanger")]
        public int ScriptDanger(ScriptInstance instance, Vector3 pos)
        {
            throw new NotImplementedException("llScriptDanger(vector)");
        }

        [APILevel(APIFlags.LSL, "llRequestSimulatorData")]
        [ForcedSleep(1)]
        public LSLKey RequestSimulatorData(ScriptInstance instance, string region, int data)
        {
            throw new NotImplementedException("llRequestSimulatorData(string, integer)");
        }

        [APILevel(APIFlags.LSL, "llEdgeOfWorld")]
        public int EdgeOfWorld(ScriptInstance instance, Vector3 pos, Vector3 dir)
        {
            throw new NotImplementedException("llEdgeOfWorld(vector, vector)");
        }

        [APILevel(APIFlags.LSL, "llGetSunDirection")]
        public Vector3 GetSunDirection(ScriptInstance instance)
        {
            lock(instance)
            {
                return instance.Part.ObjectGroup.Scene.Environment.SunDirection;
            }
        }

        [APILevel(APIFlags.LSL, "llCloud")]
        public double Cloud(ScriptInstance instance, Vector3 offset)
        {
            return 0;
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

        [APILevel(APIFlags.LSL, "llGroundContour")]
        public Vector3 GroundContour(ScriptInstance instance, Vector3 offset)
        {
            Vector3 v = GroundSlope(instance, offset);
            return new Vector3(-v.Y, v.X, 0);
        }

        [APILevel(APIFlags.LSL, "llGroundNormal")]
        public Vector3 GroundNormal(ScriptInstance instance, Vector3 offset)
        {
            lock (instance)
            {
                Vector3 regionPos = instance.Part.GlobalPosition + offset;
                return instance.Part.ObjectGroup.Scene.Terrain.Normal((int)regionPos.X, (int)regionPos.Y);
            }
        }

        [APILevel(APIFlags.LSL, "llGroundSlope")]
        public Vector3 GroundSlope(ScriptInstance instance, Vector3 offset)
        {
            Vector3 vsn = GroundNormal(instance, offset);

            /* Put the x,y coordinates of the slope normal into the plane equation to get
             * the height of that point on the plane.  
             * The resulting vector provides the slope.
             */
            Vector3 vsl = vsn;
            vsl.Z = (((vsn.X * vsn.X) + (vsn.Y * vsn.Y)) / (-1 * vsn.Z));

            return vsl.Normalize();
        }

        [APILevel(APIFlags.LSL, "llWater")]
        public double Water(ScriptInstance instance, Vector3 offset)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.RegionSettings.WaterHeight;
            }
        }

        [APILevel(APIFlags.LSL, "llWind")]
        public Vector3 Wind(ScriptInstance instance, Vector3 offset)
        {
            lock (instance)
            {
                Vector3 regionPos = instance.Part.GlobalPosition + offset;
                return instance.Part.ObjectGroup.Scene.Environment.Wind[regionPos];
            }
        }

        [APILevel(APIFlags.LSL, "llGetRegionFPS")]
        public double GetRegionFPS(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.PhysicsScene.PhysicsFPS;
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

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_LEVEL = 0;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_RAISE = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_LOWER = 2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_SMOOTH = 3;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_NOISE = 4;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_REVERT = 5;

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_SMALL_BRUSH = 0;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_MEDIUM_BRUSH = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_LARGE_BRUSH = 2;

        [APILevel(APIFlags.LSL, "llModifyLand")]
        public void ModifyLand(ScriptInstance instance, int action, int brush)
        {
            throw new NotImplementedException("llModifyLand(integer, integer)");
        }
    }
}
