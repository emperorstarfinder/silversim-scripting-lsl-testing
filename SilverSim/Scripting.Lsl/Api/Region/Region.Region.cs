// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Main.Common.CmdIO;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Region
{
    public partial class RegionApi
    {
        sealed class ConsoleCommandTTY : TTY
        {
            public string OutputBuffer { get; private set; }
            public ConsoleCommandTTY()
            {
                OutputBuffer = string.Empty;
            }

            public override void Write(string text)
            {
                OutputBuffer += text;
            }
        }

        [APILevel(APIFlags.OSSL, "osConsoleCommand")]
        public string ConsoleCommand(ScriptInstance instance, string cmd)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.IsSimConsoleAllowed(instance.Part.Owner))
                {
                    ConsoleCommandTTY tty = new ConsoleCommandTTY();
                    List<string> args = tty.GetCmdLine(cmd);
                    CommandRegistry.ExecuteCommand(args, tty, scene.ID);
                    return tty.OutputBuffer;
                }
                else
                {
                    return "NOT ALLOWED";
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osGetGridName")]
        public string GetGridName(ScriptInstance instance)
        {
            lock(instance)
            {
                GridInfoServiceInterface gridInfoService = instance.Part.ObjectGroup.Scene.GetService<GridInfoServiceInterface>();
                if(gridInfoService == null)
                {
                    return "error";
                }
                return gridInfoService.GridName;
            }
        }

        [APILevel(APIFlags.OSSL, "osGetGridNick")]
        public string GetGridNick(ScriptInstance instance)
        {
            lock (instance)
            {
                GridInfoServiceInterface gridInfoService = instance.Part.ObjectGroup.Scene.GetService<GridInfoServiceInterface>();
                if (gridInfoService == null)
                {
                    return "error";
                }
                return gridInfoService.GridNick;
            }
        }

        [APILevel(APIFlags.OSSL, "osGetGridLoginURI")]
        public string GetGridLoginURI(ScriptInstance instance)
        {
            lock (instance)
            {
                GridInfoServiceInterface gridInfoService = instance.Part.ObjectGroup.Scene.GetService<GridInfoServiceInterface>();
                if (gridInfoService == null)
                {
                    return "error";
                }
                return gridInfoService.LoginURI;
            }
        }

        [APILevel(APIFlags.OSSL, "osGetGridHomeURI")]
        public string GetGridHomeURI(ScriptInstance instance)
        {
            lock (instance)
            {
                GridInfoServiceInterface gridInfoService = instance.Part.ObjectGroup.Scene.GetService<GridInfoServiceInterface>();
                if (gridInfoService == null)
                {
                    return "error";
                }
                return gridInfoService.HomeURI;
            }
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
            lock (instance)
            {
                string value;
                GridInfoServiceInterface gridInfoService = instance.Part.ObjectGroup.Scene.GetService<GridInfoServiceInterface>();
                if (gridInfoService == null)
                {
                    return "error";
                }
                if (gridInfoService.TryGetValue(name, out value))
                {
                    return value;
                }
                return "error";
            }
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
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if(scene.Name.ToLower() == regionName.ToLower() ||
                    regionName.ToLower() == scene.ID.ToString().ToLower())
                {
                    return GetMapTexture(instance);
                }

                UUID regionid;
                RegionInfo regionInfo;
                if(UUID.TryParse(regionName, out regionid))
                {
                    if(!scene.GridService.TryGetValue(regionid, out regionInfo))
                    {
                        return UUID.Zero;
                    }
                }
                else
                {
                    if(!scene.GridService.TryGetValue(regionName, out regionInfo))
                    {
                        return UUID.Zero;
                    }
                }
                return regionInfo.RegionMapTexture;
            }
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

        [APILevel(APIFlags.LSL)]
        public const int DATA_SIM_POS = 5;
        [APILevel(APIFlags.LSL)]
        public const int DATA_SIM_STATUS = 6;
        [APILevel(APIFlags.LSL)]
        public const int DATA_SIM_RATING = 7;

        [APILevel(APIFlags.OSSL)]
        public const int DATA_SIM_RELEASE = 128;

        [APILevel(APIFlags.LSL, "llScriptDanger")]
        public int ScriptDanger(ScriptInstance instance, Vector3 pos)
        {
            throw new NotImplementedException("llScriptDanger(vector)");
        }

        [APILevel(APIFlags.LSL, "llRequestSimulatorData")]
        [ForcedSleep(1)]
        public LSLKey RequestSimulatorData(ScriptInstance instance, string region, int data)
        {
            if (DATA_SIM_RELEASE == data)
            {
                UUID queryID = UUID.Random;
                DataserverEvent e = new DataserverEvent();
                e.Data = VersionInfo.SimulatorVersion;
                e.QueryID = queryID;
                instance.PostEvent(e);
                return queryID;
            }

            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;

                if(scene.Name.ToLower() == region.ToLower())
                {
                    UUID queryID = UUID.Random;
                    DataserverEvent e = new DataserverEvent();
                    e.QueryID = queryID;
                    switch (data)
                    {
                        case DATA_SIM_POS:
                            e.Data = new Vector3(scene.RegionData.Location).ToString();
                            instance.PostEvent(e);
                            return queryID;

                        case DATA_SIM_STATUS:
                            e.Data = "up"; /* no information yet available */
                            instance.PostEvent(e);
                            return queryID;

                        case DATA_SIM_RATING:
                            switch(scene.RegionData.Access)
                            {
                                case RegionAccess.Adult:
                                    e.Data = "ADULT";
                                    break;

                                case RegionAccess.Mature:
                                    e.Data = "MATURE";
                                    break;

                                case RegionAccess.PG:
                                    e.Data = "PG";
                                    break;

                                default:
                                    e.Data = "UNKNOWN";
                                    break;
                            }
                            instance.PostEvent(e);
                            return queryID;

                        default:
                            return UUID.Zero;
                    }
                }
                else
                {
                    throw new NotImplementedException("llRequestSimulatorData(string, integer): Requesting region data of another region not yet supported");
                }
            }
        }

        [APILevel(APIFlags.LSL, "llEdgeOfWorld")]
        public int EdgeOfWorld(ScriptInstance instance, Vector3 pos, Vector3 dir)
        {
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                GridVector regionSize = scene.RegionData.Size;
                Vector3 edgeOfWorld;

                if(Math.Abs(dir.X) < double.Epsilon && Math.Abs(dir.Y) < double.Epsilon)
                {
                    return 1;
                }

                if (Math.Abs(dir.X) < double.Epsilon)
                {
                    /* special case: we cannot use slope-intercept formula here  */
                    if(dir.Y >= 0)
                    {
                        edgeOfWorld = pos;
                        edgeOfWorld.Y = regionSize.Y;
                    }
                    else
                    {
                        edgeOfWorld = pos;
                        edgeOfWorld.Y = 0;
                    }
                }
                else
                {
                    /* calculate slope-intercept line formula from point and direction */
                    double m = dir.Y / dir.X;
                    double b = pos.Y - m * pos.X;

                    Vector3 e0 = new Vector3();
                    Vector3 e1 = new Vector3();
                    Vector3 e2 = new Vector3();
                    Vector3 e3 = new Vector3();
                    e0.X = 0;
                    e0.Y = b;

                    e1.X = regionSize.X;
                    e1.Y = m * e1.X + b;

                    e2.Y = 0;
                    e2.X = (e2.Y - b) / m;

                    e3.Y = regionSize.Y;
                    e3.X = (e3.Y - b) / m;

                    double magSquared = (e0 - pos).LengthSquared;
                    edgeOfWorld = e0;
                    /* we use squared length here, it makes no difference in checking for the minimum */
                    if(magSquared > (e1 - pos).LengthSquared)
                    {
                        magSquared = (e1 - pos).LengthSquared;
                        edgeOfWorld = e1;
                    }
                    if(magSquared > (e2 - pos).LengthSquared)
                    {
                        magSquared = (e2 - pos).LengthSquared;
                        edgeOfWorld = e2;
                    }
                    if (magSquared > (e3 - pos).LengthSquared)
                    {
                        edgeOfWorld = e3;
                    }
                }

                foreach(SceneInterface.NeighborEntry neighbor in scene.Neighbors.Values)
                {
                    Vector3 swCorner = neighbor.RemoteRegionData.Location;
                    Vector3 neCorner = swCorner + neighbor.RemoteRegionData.Size;
                    /* little safety margin */
                    swCorner.X -= double.Epsilon;
                    swCorner.Y -= double.Epsilon;
                    neCorner.X += double.Epsilon;
                    neCorner.Y += double.Epsilon;
                    if(swCorner.X <= edgeOfWorld.X && 
                        neCorner.X >= edgeOfWorld.X &&
                        swCorner.Y <= edgeOfWorld.Y &&
                        neCorner.Y >= edgeOfWorld.Y)
                    {
                        return 1;
                    }
                }
            }
            return 0;
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
                    return instance.Part.ObjectGroup.Scene.RegionData.ProductName;

                case "region_product_sku":
                    return VersionInfo.ProductName;

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

        [APILevel(APIFlags.OSSL, "osRegionNotice")]
        public void RegionNotice(ScriptInstance instance, string msg)
        {
            lock(instance)
            {
                instance.CheckThreatLevel("osRegionNotice", ScriptInstance.ThreatLevelType.VeryHigh);
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                foreach(IAgent agent in scene.RootAgents)
                {
                    agent.SendRegionNotice(grp.Owner, msg, scene.ID);
                }
            }
        }
    }
}
