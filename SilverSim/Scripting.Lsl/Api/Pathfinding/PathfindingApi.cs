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
using SilverSim.Scene.Types.Pathfinding;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Pathfinding
{
    [ScriptApiName("Pathfinding")]
    [LSLImplementation]
    [Description("LSL/ASSL Pathfinding Api")]
    public class PathfindingApi : IPlugin, IScriptApi
    {
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_DESIRED_SPEED = 1;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_RADIUS = 2;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_LENGTH = 3;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_ORIENTATION = 4;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_AVOIDANCE_MODE = 5;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_TYPE = 6;
        [APILevel(APIFlags.LSL)]
        public const int TRAVERSAL_TYPE = 7;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_MAX_ACCEL = 8;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_MAX_DECEL = 9;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_MAX_TURN_RADIUS = 10;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_DESIRED_TURN_SPEED = 12;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_MAX_SPEED = 13;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_ACCOUNT_FOR_SKIPPED_FRAMES = 14;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_STAY_WITHIN_PARCEL = 15;

        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_TYPE_NONE = 4;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_TYPE_A = 0;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_TYPE_B = 1;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_TYPE_C = 2;
        [APILevel(APIFlags.LSL)]
        public const int CHARACTER_TYPE_D = 3;

        [APILevel(APIFlags.LSL)]
        public const int GCNP_RADIUS = 0;
        [APILevel(APIFlags.LSL)]
        public const int GCNP_STATIC = 1;

        [APILevel(APIFlags.LSL)]
        public const int VERTICAL = 0;
        [APILevel(APIFlags.LSL)]
        public const int HORIZONTAL = 1;

        [APILevel(APIFlags.LSL)]
        public const int TRAVERSAL_TYPE_SLOW = 0;
        [APILevel(APIFlags.LSL)]
        public const int TRAVERSAL_TYPE_FAST = 1;
        [APILevel(APIFlags.LSL)]
        public const int TRAVERSAL_TYPE_NONE = 2;

        [APILevel(APIFlags.LSL)]
        public const int AVOID_CHARACTERS = 1;
        [APILevel(APIFlags.LSL)]
        public const int AVOID_DYNAMIC_OBSTACLES = 2;
        [APILevel(APIFlags.LSL)]
        public const int AVOID_NONE = 0;

        [APILevel(APIFlags.LSL)]
        public const int PU_SLOWDOWN_DISTANCE_REACHED = 0x00;
        [APILevel(APIFlags.LSL)]
        public const int PU_GOAL_REACHED = 0x01;
        [APILevel(APIFlags.LSL)]
        public const int PU_FAILURE_INVALID_START = 0x02;
        [APILevel(APIFlags.LSL)]
        public const int PU_FAILURE_INVALID_GOAL = 0x03;
        [APILevel(APIFlags.LSL)]
        public const int PU_FAILURE_UNREACHABLE = 0x04;
        [APILevel(APIFlags.LSL)]
        public const int PU_FAILURE_TARGET_GONE = 0x05;
        [APILevel(APIFlags.LSL)]
        public const int PU_FAILURE_NO_VALID_DESTINATION = 0x06;
        [APILevel(APIFlags.LSL)]
        public const int PU_EVADE_HIDDEN = 0x07;
        [APILevel(APIFlags.LSL)]
        public const int PU_EVADE_SPOTTED = 0x08;
        [APILevel(APIFlags.LSL)]
        public const int PU_FAILURE_NO_NAVMESH = 0x09;
        [APILevel(APIFlags.LSL)]
        public const int PU_FAILURE_DYNAMIC_PATHFINDING_DISABLED = 0x0A;
        [APILevel(APIFlags.LSL)]
        public const int PU_FAILURE_PARCEL_UNREACHABLE = 0x0B;
        [APILevel(APIFlags.LSL)]
        public const int PU_FAILURE_OTHER = 0xF4240;

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APIExtension(APIExtension.Pathfinding, "pfRebuildNavmesh")]
        public void RebuildNavmesh(ScriptInstance instance)
        {
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if(!scene.IsEstateManager(instance.Item.Owner))
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0CanOnlyBeCalledByEstateManagerOrOwner", "{0} can only be called by estate manager or owner", "pfRebuildNavmesh"));
                    return;
                }

                IPathfindingService pathfindingService = scene.PathfindingService;
                if(pathfindingService == null)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "PathfindingIsNotAvailable", "Pathfinding is not available"));
                    return;
                }
                pathfindingService.TriggerRebuild();
            }
        }

        [APILevel(APIFlags.LSL, "llGetClosestNavPoint")]
        public AnArray GetClosestNavPoint(ScriptInstance instance, Vector3 point, AnArray options)
        {
            lock (instance)
            {
                double radius = 20.0;
                bool staticOnly = false;
                var cType = CharacterType.None;
                for (int i = 0; i < options.Count - 1; ++i)
                {
                    switch (options[i].AsInt)
                    {
                        case GCNP_RADIUS:
                            radius = options[i + 1].AsReal;
                            break;

                        case GCNP_STATIC:
                            staticOnly = options[i + 1].AsBoolean;
                            break;

                        case CHARACTER_TYPE:
                            switch (options[i + 1].AsInt)
                            {
                                case CHARACTER_TYPE_NONE:
                                    cType = CharacterType.None;
                                    break;

                                case CHARACTER_TYPE_A:
                                    cType = CharacterType.A;
                                    break;

                                case CHARACTER_TYPE_B:
                                    cType = CharacterType.B;
                                    break;

                                case CHARACTER_TYPE_C:
                                    cType = CharacterType.C;
                                    break;

                                case CHARACTER_TYPE_D:
                                    cType = CharacterType.D;
                                    break;

                                default:
                                    return new AnArray();
                            }
                            break;

                        default:
                            return new AnArray();
                    }
                }

                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                IPathfindingService pathfindingService = scene.PathfindingService;
                if (pathfindingService == null)
                {
                    return new AnArray();
                }
                Vector3 navPoint;
                var res = new AnArray();
                if (pathfindingService.TryGetClosestNavPoint(point, radius, staticOnly, cType, out navPoint))
                {
                    res.Add(navPoint);
                }
                return res;
            }
        }

        [APILevel(APIFlags.LSL, "llGetStaticPath")]
        public AnArray GetStaticPath(ScriptInstance instance, Vector3 start, Vector3 end, double radius, AnArray param)
        {
            var cInfo = new CharacterInfo()
            {
                CollisionCapsuleRadius = radius
            };
            for (int i = 0; i < param.Count - 1; ++i)
            {
                if (param[i].AsInt == CHARACTER_TYPE)
                {
                    switch (param[i + 1].AsInt)
                    {
                        case CHARACTER_TYPE_A:
                            cInfo.Type = CharacterType.A;
                            break;

                        case CHARACTER_TYPE_B:
                            cInfo.Type = CharacterType.B;
                            break;

                        case CHARACTER_TYPE_C:
                            cInfo.Type = CharacterType.C;
                            break;

                        case CHARACTER_TYPE_D:
                            cInfo.Type = CharacterType.D;
                            break;

                        case CHARACTER_TYPE_NONE:
                            cInfo.Type = CharacterType.None;
                            break;

                        default:
                            break;
                    }
                }
            }

            var res = new AnArray();
            lock (instance)
            {
                IPathfindingService pathfindingService = instance.Part.ObjectGroup.Scene.PathfindingService;
                if (pathfindingService == null)
                {
                    res.Add(PU_FAILURE_NO_NAVMESH);
                }
                else
                {
                    List<WaypointData> wpdata;
                    ResolvePathStatus status = pathfindingService.TryResolvePath(start, end, cInfo, out wpdata);
                    if (status == ResolvePathStatus.Success)
                    {
                        res.Add(0);
                        foreach (WaypointData wp in wpdata)
                        {
                            res.Add(wp.Position);
                        }
                    }
                    else
                    {
                        switch (status)
                        {
                            case ResolvePathStatus.DynamicPathfindingDisabled:
                                res.Add(PU_FAILURE_DYNAMIC_PATHFINDING_DISABLED);
                                break;

                            case ResolvePathStatus.GoalReached:
                                res.Add(PU_GOAL_REACHED);
                                break;

                            case ResolvePathStatus.InvalidStart:
                                res.Add(PU_FAILURE_INVALID_START);
                                break;

                            case ResolvePathStatus.InvalidGoal:
                                res.Add(PU_FAILURE_INVALID_GOAL);
                                break;

                            case ResolvePathStatus.Unreachable:
                                res.Add(PU_FAILURE_UNREACHABLE);
                                break;

                            case ResolvePathStatus.TargetGone:
                                res.Add(PU_FAILURE_TARGET_GONE);
                                break;

                            case ResolvePathStatus.NoValidDestination:
                                res.Add(PU_FAILURE_NO_VALID_DESTINATION);
                                break;

                            case ResolvePathStatus.EvadeHidden:
                                res.Add(PU_EVADE_HIDDEN);
                                break;

                            case ResolvePathStatus.EvadeSpotted:
                                res.Add(PU_EVADE_SPOTTED);
                                break;

                            case ResolvePathStatus.NoNavMesh:
                                res.Add(PU_FAILURE_NO_NAVMESH);
                                break;

                            case ResolvePathStatus.ParcelUnreachable:
                                res.Add(PU_FAILURE_PARCEL_UNREACHABLE);
                                break;

                            default:
                                res.Add(PU_FAILURE_OTHER);
                                break;
                        }
                    }
                }
            }
            return res;
        }
    }
}
