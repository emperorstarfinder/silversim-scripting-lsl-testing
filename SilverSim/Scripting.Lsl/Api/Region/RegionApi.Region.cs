// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

#pragma warning disable IDE0018, RCS1029

using SilverSim.Main.Common;
using SilverSim.Main.Common.CmdIO;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Pathfinding;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SilverSim.Scripting.Lsl.Api.Region
{
    public partial class RegionApi
    {
        [APILevel(APIFlags.OSSL)]
        public const int STATS_TIME_DILATION = 0;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_SIM_FPS = 1;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_PHYSICS_FPS = 2;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_AGENT_UPDATES = 3;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_ROOT_AGENTS = 4;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_CHILD_AGENTS = 5;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_TOTAL_PRIMS = 6;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_ACTIVE_PRIMS = 7;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_FRAME_MS = 8;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_NET_MS = 9;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_PHYSICS_MS = 10;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_IMAGE_MS = 11;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_OTHER_MS = 12;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_IN_PACKETS_PER_SECOND = 13;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_OUT_PACKETS_PER_SECOND = 14;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_UNACKED_BYTES = 15;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_AGENT_MS = 16;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_PENDING_DOWNLOADS = 17;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_PENDING_UPLOADS = 18;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_ACTIVE_SCRIPTS = 19;
        [APILevel(APIFlags.OSSL)]
        public const int STATS_SCRIPT_LPS = 20;

        private sealed class ConsoleCommandTTY : TTY
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
                    var tty = new ConsoleCommandTTY();
                    m_Commands.ExecuteCommandString(cmd, tty, scene.ID);
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
            lock (instance)
            {
                GridInfoServiceInterface gridInfoService = instance.Part.ObjectGroup.Scene.GetService<GridInfoServiceInterface>();
                if (gridInfoService == null)
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
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.GatekeeperURI;
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
        public string GetSimulatorVersion(ScriptInstance instance) => VersionInfo.SimulatorVersion;

        [APILevel(APIFlags.OSSL, "osGetRegionSize")]
        public Vector3 GetRegionSize(ScriptInstance instance)
        {
            lock (instance)
            {
                return new Vector3(
                    instance.Part.ObjectGroup.Scene.SizeX,
                    instance.Part.ObjectGroup.Scene.SizeY,
                    0);
            }
        }

        [APILevel(APIFlags.OSSL, "osGetMapTexture")]
        public LSLKey GetMapTexture(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.RegionMapTexture;
            }
        }

        [APILevel(APIFlags.OSSL, "osGetRegionMapTexture")]
        public LSLKey GetMapTexture(ScriptInstance instance, string regionName)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (string.Equals(scene.Name, regionName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(regionName, scene.ID.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return GetMapTexture(instance);
                }

                UUID regionid;
                RegionInfo regionInfo;
                if (UUID.TryParse(regionName, out regionid))
                {
                    if (!scene.GridService.TryGetValue(regionid, out regionInfo))
                    {
                        return UUID.Zero;
                    }
                }
                else
                {
                    if (!scene.GridService.TryGetValue(regionName, out regionInfo))
                    {
                        return UUID.Zero;
                    }
                }
                return regionInfo.RegionMapTexture;
            }
        }

        [APILevel(APIFlags.OSSL, "osGetRegionStats")]
        [CheckFunctionPermission]
        public AnArray GetRegionStats(ScriptInstance instance)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                IPhysicsScene physicsScene = scene.PhysicsScene;
                return new AnArray
                {
                    { physicsScene.PhysicsDilationTime }, /* STATS_TIME_DILATION */
                    { scene.Environment.EnvironmentFps }, /* STATS_SIM_FPS */
                    { physicsScene.PhysicsFPS }, /* STATS_PHYSICS_FPS */
                    { 0 }, /* STATS_AGENT_UPDATES */
                    { scene.RootAgents.Count }, /* STATS_ROOT_AGENTS */
                    { scene.Agents.Count - scene.RootAgents.Count }, /* STATS_CHILD_AGENTS */
                    { scene.Primitives.Count }, /* STATS_TOTAL_PRIMS */
                    { scene.ActiveObjects }, /* STATS_ACTIVE_PRIMS */
                    { 0 }, /* STATS_FRAME_MS */
                    { 0 }, /* STATS_NET_MS */
                    { 0 }, /* STATS_PHYSICS_MS */
                    { 0 }, /* STATS_IMAGE_MS */
                    { 0 }, /* STATS_OTHER_MS */
                    { 0 }, /* STATS_IN_PACKETS_PER_SECOND */
                    { 0 }, /* STATS_OUT_PACKETS_PER_SECOND */
                    { 0 }, /* STATS_UNACKED_BYTES */
                    { 0 }, /* STATS_AGENT_MS */
                    { 0 }, /* STATS_PENDING_DOWNLOADS */
                    { 0 }, /* STATS_PENDING_UPLOADS */
                    { scene.ActiveScripts }, /* STATS_ACTIVE_SCRIPTS */
                    { scene.ScriptThreadPool.ScriptEventsPerSec } /* STATS_SCRIPT_LPS */
                };
            }
        }

        [APILevel(APIFlags.OSSL, "osGetSimulatorMemory")]
        [CheckFunctionPermission]
        public int GetSimulatorMemory(ScriptInstance instance)
        {
            lock (instance)
            {
                long pws = Process.GetCurrentProcess().WorkingSet64;
                if (pws > Int32.MaxValue)
                {
                    return Int32.MaxValue;
                }
                return (int)pws;
            }
        }

        [APILevel(APIFlags.OSSL, "osGetSimulatorMemoryKB")]
        [CheckFunctionPermission("osGetSimulatorMemory")]
        public int GetSimulatorMemoryKB(ScriptInstance instance)
        {
            lock (instance)
            {
                long pws = (Process.GetCurrentProcess().WorkingSet64 + 1023) / 1024;
                if (pws > Int32.MaxValue)
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
            lock (instance)
            {
                RegionSettings settings = instance.Part.ObjectGroup.Scene.RegionSettings;
                if (settings.AllowDamage)
                {
                    flags |= REGION_FLAG_ALLOW_DAMAGE;
                }
                if (settings.BlockTerraform)
                {
                    flags |= REGION_FLAG_BLOCK_TERRAFORM;
                }
                if (settings.Sandbox)
                {
                    flags |= REGION_FLAG_SANDBOX;
                }
                if (settings.DisableCollisions)
                {
                    flags |= REGION_FLAG_DISABLE_COLLISIONS;
                }
                if (settings.DisablePhysics)
                {
                    flags |= REGION_FLAG_DISABLE_PHYSICS;
                }
                if (settings.BlockFly)
                {
                    flags |= REGION_FLAG_BLOCK_FLY;
                }
                if (settings.RestrictPushing)
                {
                    flags |= REGION_FLAG_RESTRICT_PUSHOBJECT;
                }
                if (!settings.AllowLandResell)
                {
                    flags |= REGION_FLAGS_BLOCK_LAND_RESELL;
                }
                if (settings.DisableScripts)
                {
                    flags |= REGION_FLAGS_SKIP_SCRIPTS;
                }
                if (settings.AllowLandJoinDivide)
                {
                    flags |= REGION_FLAGS_ALLOW_PARCEL_CHANGES;
                }
            }

            return flags;
        }

        [APILevel(APIFlags.LSL, "llGetRegionTimeDilation")]
        public double GetRegionTimeDilation(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.PhysicsScene.PhysicsDilationTime;
            }
        }

        [APILevel(APIFlags.LSL, "llGetSimulatorHostname")]
        [ForcedSleep(10)]
        public string GetSimulatorHostname(ScriptInstance instance)
        {
            lock (instance)
            {
                var uri = new Uri(instance.Part.ObjectGroup.Scene.ServerURI);
                return uri.Host;
            }
        }

        [APILevel(APIFlags.LSL, "llGetRegionCorner")]
        public Vector3 GetRegionCorner(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.GridPosition;
            }
        }

        [APILevel(APIFlags.LSL, "llGetRegionAgentCount")]
        public int GetRegionAgentCount(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.RootAgents.Count;
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

        [APILevel(APIFlags.LSL, "llRequestSimulatorData")]
        [ForcedSleep(1)]
        public LSLKey RequestSimulatorData(ScriptInstance instance, string region, int data)
        {
            if (DATA_SIM_RELEASE == data)
            {
                UUID queryID = UUID.Random;
                instance.Part.PostEvent(new DataserverEvent
                {
                    Data = VersionInfo.SimulatorVersion,
                    QueryID = queryID
                });
                return queryID;
            }

            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;

                if (string.Equals(scene.Name, region, StringComparison.OrdinalIgnoreCase))
                {
                    UUID queryID = UUID.Random;
                    var e = new DataserverEvent
                    {
                        QueryID = queryID
                    };
                    switch (data)
                    {
                        case DATA_SIM_POS:
                            e.Data = LSLCompiler.TypecastVectorToString5Places(new Vector3(scene.GridPosition));
                            instance.Part.PostEvent(e);
                            return queryID;

                        case DATA_SIM_STATUS:
                            e.Data = "up"; /* no information yet available */
                            instance.Part.PostEvent(e);
                            return queryID;

                        case DATA_SIM_RATING:
                            switch (scene.Access)
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
                            instance.Part.PostEvent(e);
                            return queryID;

                        default:
                            return UUID.Zero;
                    }
                }
                else
                {
                    RegionInfo ri;
                    if (scene.GridService.TryGetValue(region, out ri))
                    {
                        UUID queryID = UUID.Random;
                        var e = new DataserverEvent
                        {
                            QueryID = queryID
                        };
                        switch (data)
                        {
                            case DATA_SIM_POS:
                                e.Data = new Vector3(ri.Location).ToString();
                                instance.Part.PostEvent(e);
                                return queryID;

                            case DATA_SIM_STATUS:
                                e.Data = (ri.Flags & RegionFlags.RegionOnline) != 0 ? "up" : "down";
                                instance.Part.PostEvent(e);
                                return queryID;

                            case DATA_SIM_RATING:
                                switch (ri.Access)
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
                                instance.Part.PostEvent(e);
                                return queryID;

                            default:
                                return UUID.Zero;
                        }
                    }
                    else
                    {
                        return UUID.Zero;
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llEdgeOfWorld")]
        public int EdgeOfWorld(ScriptInstance instance, Vector3 pos, Vector3 dir)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                Vector3 edgeOfWorld;

                if (Math.Abs(dir.X) < double.Epsilon && Math.Abs(dir.Y) < double.Epsilon)
                {
                    return 1;
                }

                if (Math.Abs(dir.X) < double.Epsilon)
                {
                    /* special case: we cannot use slope-intercept formula here  */
                    if (dir.Y >= 0)
                    {
                        edgeOfWorld = pos;
                        edgeOfWorld.Y = scene.SizeY;
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

                    var e0 = new Vector3();
                    var e1 = new Vector3();
                    var e2 = new Vector3();
                    var e3 = new Vector3();
                    e0.X = 0;
                    e0.Y = b;

                    e1.X = scene.SizeX;
                    e1.Y = m * e1.X + b;

                    e2.Y = 0;
                    e2.X = (e2.Y - b) / m;

                    e3.Y = scene.SizeY;
                    e3.X = (e3.Y - b) / m;

                    double magSquared = (e0 - pos).LengthSquared;
                    edgeOfWorld = e0;
                    /* we use squared length here, it makes no difference in checking for the minimum */
                    if (magSquared > (e1 - pos).LengthSquared)
                    {
                        magSquared = (e1 - pos).LengthSquared;
                        edgeOfWorld = e1;
                    }
                    if (magSquared > (e2 - pos).LengthSquared)
                    {
                        magSquared = (e2 - pos).LengthSquared;
                        edgeOfWorld = e2;
                    }
                    if (magSquared > (e3 - pos).LengthSquared)
                    {
                        edgeOfWorld = e3;
                    }
                }

                foreach (SceneInterface.NeighborEntry neighbor in scene.Neighbors.Values)
                {
                    Vector3 swCorner = neighbor.RemoteRegionData.Location;
                    Vector3 neCorner = swCorner + neighbor.RemoteRegionData.Size;
                    /* little safety margin */
                    swCorner.X -= double.Epsilon;
                    swCorner.Y -= double.Epsilon;
                    neCorner.X += double.Epsilon;
                    neCorner.Y += double.Epsilon;
                    if (swCorner.X <= edgeOfWorld.X &&
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

        [APILevel(APIFlags.LSL, "llGetRegionFPS")]
        public double GetRegionFPS(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.PhysicsScene.PhysicsFPS;
            }
        }

        [APILevel(APIFlags.LSL, "llGetEnv")]
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
                    try
                    {
                        lock (instance)
                        {
                            IPathfindingService pathfinding = instance.Part.ObjectGroup.Scene.PathfindingService;
                            return pathfinding.IsDynamicEnabled ? "enabled" : "disabled";
                        }
                    }
                    catch
                    {
                        return "disabled";
                    }

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
                    lock (instance)
                    {
                        return instance.Part.ObjectGroup.Scene.FrameNumber.ToString();
                    }

                case "region_cpu_ratio":
                    return "1";

                case "region_idle":
                    return "0";

                case "region_size_x":
                    lock (instance)
                    {
                        return instance.Part.ObjectGroup.Scene.GetRegionInfo().Size.X.ToString();
                    }

                case "region_size_y":
                    lock (instance)
                    {
                        return instance.Part.ObjectGroup.Scene.GetRegionInfo().Size.Y.ToString();
                    }

                case "region_product_name":
                    lock (instance)
                    {
                        return instance.Part.ObjectGroup.Scene.ProductName;
                    }

                case "region_start_time":
                    lock (instance)
                    {
                        return instance.Part.ObjectGroup.Scene.RegionStartTime.AsULong.ToString();
                    }

                case "region_product_sku":
                case "sim_channel":
                    return VersionInfo.ProductName;

                case "sim_version":
                    return VersionInfo.Version;

                case "simulator_hostname":
                    return GetSimulatorHostname(instance);

                case "region_object_bonus":
                    lock (instance)
                    {
                        return instance.Part.ObjectGroup.Scene.RegionSettings.ObjectBonus.ToString();
                    }

                default:
                    return string.Empty;
            }
        }

        [APILevel(APIFlags.OSSL, "osRegionNotice")]
        [CheckFunctionPermission]
        public void RegionNotice(ScriptInstance instance, string msg)
        {
            lock (instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                foreach (IAgent agent in scene.RootAgents)
                {
                    agent.SendRegionNotice(grp.Owner, msg, scene.ID);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osRegionNotice")]
        [CheckFunctionPermission]
        public void RegionNotice(ScriptInstance instance, LSLKey agentid, string msg)
        {
            lock (instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                IAgent agent;
                if (scene.RootAgents.TryGetValue(agentid.AsUUID, out agent))
                {
                    agent.SendRegionNotice(grp.Owner, msg, scene.ID);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osRegionRestart")]
        public int RegionRestart(ScriptInstance instance, double seconds)
        {
            lock (instance)
            {
                ObjectPart part = instance.Part;
                SceneInterface scene = part.ObjectGroup.Scene;
                if (scene.IsRegionOwner(part.Owner) || scene.IsEstateManager(part.Owner))
                {
                    if (seconds < 15)
                    {
                        instance.Part.ObjectGroup.Scene.AbortRegionRestart();
                    }
                    else
                    {
                        instance.Part.ObjectGroup.Scene.RequestRegionRestart((int)seconds);
                    }
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asAddRezzingScriptToWhiteList")]
        [CheckFunctionPermission("RezzingControl")]
        public void AddRezzingScriptToWhiteList(ScriptInstance instance, LSLKey scriptassetid)
        {
            lock (instance)
            {
                if (scriptassetid != UUID.Zero)
                {
                    instance.Part.ObjectGroup.Scene.WhiteListedRezzingScriptAssetIds.AddIfNotExists(scriptassetid);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asRemoveRezzingScriptFromWhiteList")]
        [CheckFunctionPermission("RezzingControl")]
        public void RemoveRezzingScriptFromWhiteList(ScriptInstance instance, LSLKey scriptassetid)
        {
            lock (instance)
            {
                if (scriptassetid != UUID.Zero)
                {
                    instance.Part.ObjectGroup.Scene.WhiteListedRezzingScriptAssetIds.AddIfNotExists(scriptassetid);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asReplaceRezzingScriptWhiteList")]
        [CheckFunctionPermission("RezzingControl")]
        public void ReplaceRezzingScriptWhiteList(ScriptInstance instance, AnArray scriptassetids)
        {
            lock(instance)
            {
                List<UUID> uuids = new List<UUID>();
                RwLockedList<UUID> whitelist = instance.Part.ObjectGroup.Scene.WhiteListedRezzingScriptAssetIds;
                foreach (IValue k in scriptassetids)
                {
                    if (!uuids.Contains(k.AsUUID) && k.AsUUID != UUID.Zero)
                    {
                        uuids.Add(k.AsUUID);
                    }
                }

                foreach(UUID e in new List<UUID>(whitelist))
                {
                    if(!uuids.Contains(e))
                    {
                        whitelist.Remove(e);
                    }
                }

                foreach(UUID e in uuids)
                {
                    whitelist.AddIfNotExists(e);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asAddRezzableObjectToWhiteList")]
        [CheckFunctionPermission("RezzingControl")]
        public void AddRezzableObjectToWhiteList(ScriptInstance instance, LSLKey objectassetid)
        {
            lock (instance)
            {
                if (objectassetid != UUID.Zero)
                {
                    instance.Part.ObjectGroup.Scene.WhiteListedRezzingScriptAssetIds.AddIfNotExists(objectassetid);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asRemoveRezzableObjectFromWhiteList")]
        [CheckFunctionPermission("RezzingControl")]
        public void RemoveRezzableObjectFromWhiteList(ScriptInstance instance, LSLKey objectassetid)
        {
            lock (instance)
            {
                if(objectassetid != UUID.Zero)
                {
                    instance.Part.ObjectGroup.Scene.WhiteListedRezzingScriptAssetIds.Remove(objectassetid);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asReplaceRezzableObjectWhiteList")]
        [CheckFunctionPermission("RezzingControl")]
        public void ReplaceRezzableObjectWhiteList(ScriptInstance instance, AnArray objectassetids)
        {
            lock (instance)
            {
                List<UUID> uuids = new List<UUID>();
                RwLockedList<UUID> whitelist = instance.Part.ObjectGroup.Scene.WhiteListedRezzableAssetIds;
                foreach (IValue k in objectassetids)
                {
                    if (!uuids.Contains(k.AsUUID) && k.AsUUID != UUID.Zero)
                    {
                        uuids.Add(k.AsUUID);
                    }
                }

                foreach (UUID e in new List<UUID>(whitelist))
                {
                    if (!uuids.Contains(e))
                    {
                        whitelist.Remove(e);
                    }
                }

                foreach (UUID e in uuids)
                {
                    whitelist.AddIfNotExists(e);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asAddRezzableObjectToBlackList")]
        [CheckFunctionPermission("RezzingControl")]
        public void AddRezzableObjectToBlackList(ScriptInstance instance, LSLKey objectassetid)
        {
            lock (instance)
            {
                if (objectassetid != UUID.Zero)
                {
                    instance.Part.ObjectGroup.Scene.BlackListedRezzableAssetIds.AddIfNotExists(objectassetid);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asRemoveRezzableObjectFromBlackList")]
        [CheckFunctionPermission("RezzingControl")]
        public void RemoveRezzableObjectFromBlackList(ScriptInstance instance, LSLKey objectassetid)
        {
            lock (instance)
            {
                if (objectassetid != UUID.Zero)
                {
                    instance.Part.ObjectGroup.Scene.BlackListedRezzableAssetIds.Remove(objectassetid);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asReplaceRezzableObjectBlackList")]
        [CheckFunctionPermission("RezzingControl")]
        public void ReplaceRezzableObjectBlackList(ScriptInstance instance, AnArray objectassetids)
        {
            lock (instance)
            {
                List<UUID> uuids = new List<UUID>();
                RwLockedList<UUID> blacklist = instance.Part.ObjectGroup.Scene.BlackListedRezzableAssetIds;
                foreach (IValue k in objectassetids)
                {
                    if (!uuids.Contains(k.AsUUID) && k.AsUUID != UUID.Zero)
                    {
                        uuids.Add(k.AsUUID);
                    }
                }

                foreach (UUID e in new List<UUID>(blacklist))
                {
                    if (!uuids.Contains(e))
                    {
                        blacklist.Remove(e);
                    }
                }

                foreach (UUID e in uuids)
                {
                    blacklist.AddIfNotExists(e);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asAddScriptAssetToRunWhiteList")]
        [CheckFunctionPermission("ScriptRunWhiteList")]
        public void AddScriptAssetToRunWhiteList(ScriptInstance instance, LSLKey scriptassetid)
        {
            lock (instance)
            {
                if (scriptassetid != UUID.Zero)
                {
                    instance.Part.ObjectGroup.Scene.WhiteListedRunScriptAssetIds.AddIfNotExists(scriptassetid);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asRemoveScriptAssetFromWhiteList")]
        [CheckFunctionPermission("ScriptRunWhiteList")]
        public void RemoveScriptAssetFromRunWhiteList(ScriptInstance instance, LSLKey scriptassetid)
        {
            lock (instance)
            {
                if (scriptassetid != UUID.Zero)
                {
                    instance.Part.ObjectGroup.Scene.WhiteListedRunScriptAssetIds.Remove(scriptassetid);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asReplaceScriptAssetRunWhiteList")]
        [CheckFunctionPermission("ScriptRunWhiteList")]
        public void ReplaceScriptAssetRunWhiteList(ScriptInstance instance, AnArray scriptassetids)
        {
            lock (instance)
            {
                List<UUID> uuids = new List<UUID>();
                RwLockedList<UUID> whitelist = instance.Part.ObjectGroup.Scene.WhiteListedRunScriptAssetIds;
                foreach (IValue k in scriptassetids)
                {
                    if (!uuids.Contains(k.AsUUID) && k.AsUUID != UUID.Zero)
                    {
                        uuids.Add(k.AsUUID);
                    }
                }

                foreach (UUID e in new List<UUID>(whitelist))
                {
                    if (!uuids.Contains(e))
                    {
                        whitelist.Remove(e);
                    }
                }

                foreach (UUID e in uuids)
                {
                    whitelist.AddIfNotExists(e);
                }
            }
        }
    }
}
