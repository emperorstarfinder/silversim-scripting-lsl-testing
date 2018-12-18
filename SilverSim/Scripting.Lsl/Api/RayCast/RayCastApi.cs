﻿// SilverSim is distributed under the terms of the
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

using log4net;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.RayCast
{
    [ScriptApiName("RayCast")]
    [LSLImplementation]
    [Description("LSL RayCast API")]
    public class RayCastApi : IScriptApi, IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LSL RAYCAST");

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL)]
        public const int RCERR_UNKNOWN = -1;
        [APILevel(APIFlags.LSL)]
        public const int RCERR_SIM_PERF_LOW = -2;
        [APILevel(APIFlags.LSL)]
        public const int RCERR_CAST_TIME_EXCEEDED = -3;

        [APILevel(APIFlags.LSL)]
        public const int RC_REJECT_TYPES = 0;
        [APILevel(APIFlags.LSL)]
        public const int RC_DETECT_PHANTOM = 1;
        [APILevel(APIFlags.LSL)]
        public const int RC_DATA_FLAGS = 2;
        [APILevel(APIFlags.LSL)]
        public const int RC_MAX_HITS = 3;

        [APILevel(APIFlags.LSL)]
        public const int RC_REJECT_AGENTS = 1;
        [APILevel(APIFlags.LSL)]
        public const int RC_REJECT_PHYSICAL = 2;
        [APILevel(APIFlags.LSL)]
        public const int RC_REJECT_NONPHYSICAL = 4;
        [APILevel(APIFlags.LSL)]
        public const int RC_REJECT_LAND = 8;

        [APILevel(APIFlags.LSL)]
        public const int RC_GET_NORMAL = 1;
        [APILevel(APIFlags.LSL)]
        public const int RC_GET_ROOT_KEY = 2;
        [APILevel(APIFlags.LSL)]
        public const int RC_GET_LINK_NUM = 4;

        [APILevel(APIFlags.ASSL, "asCastRayCollision")]
        [Description("Cast a ray from start to end and report collision data for intersection with object and trigger collision events")]
        public AnArray CastRayCollisions(
            ScriptInstance instance,
            [Description("starting location")]
            Vector3 start,
            [Description("ending location")]
            Vector3 end,
            AnArray options) => CastRay(instance, start, end, options, HandleRayCollisionResult);

        private void HandleRayCollisionResult(ScriptInstance instance, RayResult result)
        {
            IAgent agent;
            ObjectPart thisPart = instance.Part;
            ObjectGroup thisGroup = thisPart?.ObjectGroup;
            ObjectPart part;
            SceneInterface scene = thisGroup?.Scene;
            if (scene == null)
            {
            }
            else if (scene.Primitives.TryGetValue(result.ObjectId, out part))
            {
                DetectInfo di = new DetectInfo
                {
                    Key = thisGroup.ID,
                    Name = thisGroup.Name,
                    Group = thisGroup.Group,
                    ObjType = thisGroup.DetectedType,
                    Owner = thisGroup.Owner,
                    Position = thisGroup.GlobalPosition,
                    Rotation = thisGroup.GlobalRotation,
                    Velocity = thisGroup.Velocity,
                    LinkNumber = part.LinkNumber
                };
                var ev = new CollisionEvent { Type = CollisionEvent.CollisionType.Start };
                ev.Detected.Add(di);
                part.PostEvent(ev);
                ev = new CollisionEvent { Type = CollisionEvent.CollisionType.Continuous };
                ev.Detected.Add(di);
                part.PostEvent(ev);
                ev = new CollisionEvent { Type = CollisionEvent.CollisionType.End };
                ev.Detected.Add(di);
                part.PostEvent(ev);
            }
            else if (scene.RootAgents.TryGetValue(result.ObjectId, out agent))
            {
                DetectInfo di = new DetectInfo
                {
                    Key = thisGroup.ID,
                    Name = thisGroup.Name,
                    Group = thisGroup.Group,
                    ObjType = thisGroup.DetectedType,
                    Owner = thisGroup.Owner,
                    Position = thisGroup.GlobalPosition,
                    Rotation = thisGroup.GlobalRotation,
                    Velocity = thisGroup.Velocity,
                    LinkNumber = thisPart.LinkNumber
                };
                var ev = new CollisionEvent { Type = CollisionEvent.CollisionType.Start };
                ev.Detected.Add(di);
                agent.PostEvent(ev);
                ev = new CollisionEvent { Type = CollisionEvent.CollisionType.Continuous };
                ev.Detected.Add(di);
                agent.PostEvent(ev);
                ev = new CollisionEvent { Type = CollisionEvent.CollisionType.End };
                ev.Detected.Add(di);
                agent.PostEvent(ev);
            }
        }

        [APILevel(APIFlags.LSL, "llCastRay")]
        [Description("Cast a ray from start to end and report collision data for intersections with objects")]
        public AnArray CastRay(
            ScriptInstance instance,
            [Description("starting location")]
            Vector3 start,
            [Description("ending location")]
            Vector3 end,
            AnArray options) => CastRay(instance, start, end, options, null);

        private AnArray CastRay(
            ScriptInstance instance,
            [Description("starting location")]
            Vector3 start,
            [Description("ending location")]
            Vector3 end,
            AnArray options,
            Action<ScriptInstance, RayResult> action)
        {
            var resArray = new AnArray();
            RayTestHitFlags hitFlags = RayTestHitFlags.Avatar | RayTestHitFlags.NonPhysical | RayTestHitFlags.Physical;
            int i;
            int maxHits = 1;
            int flags;
            int dataFlags = 0;

            try
            {
                for (i = 0; i + 1 < options.Count; i += 2)
                {
                    switch (options[i].AsInt)
                    {
                        case RC_REJECT_TYPES:
                            flags = options[i + 1].AsInt;
                            if ((flags & RC_REJECT_AGENTS) != 0)
                            {
                                hitFlags &= ~RayTestHitFlags.Avatar;
                            }
                            else
                            {
                                hitFlags |= RayTestHitFlags.Avatar;
                            }
                            if ((flags & RC_REJECT_PHYSICAL) != 0)
                            {
                                hitFlags &= ~RayTestHitFlags.Physical;
                            }
                            else
                            {
                                hitFlags |= RayTestHitFlags.Avatar;
                            }
                            if ((flags & RC_REJECT_NONPHYSICAL) != 0)
                            {
                                hitFlags &= ~RayTestHitFlags.NonPhysical;
                            }
                            else
                            {
                                hitFlags |= RayTestHitFlags.Avatar;
                            }
                            if ((flags & RC_REJECT_LAND) != 0)
                            {
                                hitFlags &= ~RayTestHitFlags.Terrain;
                            }
                            else
                            {
                                hitFlags |= RayTestHitFlags.Avatar;
                            }
                            break;

                        case RC_DATA_FLAGS:
                            dataFlags = options[i + 1].AsInt;
                            break;

                        case RC_MAX_HITS:
                            maxHits = options[i + 1].AsInt.Clamp(0, 256);
                            break;

                        case RC_DETECT_PHANTOM:
                            if (options[i + 1].AsBoolean)
                            {
                                hitFlags |= RayTestHitFlags.Phantom;
                            }
                            else
                            {
                                hitFlags &= ~RayTestHitFlags.Phantom;
                            }
                            break;
                    }
                }
            }
            catch(Exception e)
            {
                m_Log.Debug("Raycast parameter decoding encountered exception at prim " + instance.Part.ID + " within object " + instance.Part.ObjectGroup.ID, e);
                resArray.Add(RCERR_UNKNOWN);
                return resArray;
            }

            RayResult[] results;
            lock (instance)
            {
                BoundingBox bbox;
                instance.Part.GetBoundingBox(out bbox);
                Vector3 relstart = start - bbox.CenterOffset;
                UUID ourID = UUID.Zero;
                
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                try
                {
                    if (maxHits == 0)
                    {
                        results = new RayResult[0];
                    }
                    else
                    {
                        if (Math.Abs(relstart.X) < bbox.Size.X && Math.Abs(relstart.Y) < bbox.Size.Y && Math.Abs(relstart.Z) < bbox.Size.Z)
                        {
                            ourID = instance.Part.ID;
                            if (maxHits != int.MaxValue)
                            {
                                ++maxHits;
                            }
                        }

                        results = scene.PhysicsScene.RayTest(start, end, hitFlags, (uint)maxHits);
                    }
                }
                catch(Exception e)
                {
                    m_Log.Debug("Raycast encountered exception at prim " + instance.Part.ID + " within object " + instance.Part.ObjectGroup.ID, e);
                    resArray.Add(RCERR_UNKNOWN);
                    return resArray;
                }

                foreach(RayResult result in results)
                {
                    if (result.IsTerrain)
                    {
                        resArray.Add(UUID.Zero);
                    }
                    else if(ourID == result.PartId)
                    {
                        continue;
                    }
                    else if(maxHits >= resArray.Count)
                    {
                        break;
                    }
                    else
                    {
                        action?.Invoke(instance, result);
                        resArray.Add((dataFlags & RC_GET_ROOT_KEY) != 0 ? result.ObjectId : result.PartId);
                    }
                    if((dataFlags & RC_GET_LINK_NUM) != 0)
                    {
                        ObjectPart hitPart;
                        if(scene.Primitives.TryGetValue(result.PartId, out hitPart))
                        {
                            resArray.Add(hitPart.LinkNumber);
                        }
                        else
                        {
                            resArray.Add(-1);
                        }
                    }
                    resArray.Add(result.HitPointWorld);
                    if ((dataFlags & RC_GET_NORMAL) != 0)
                    {
                        resArray.Add(result.HitNormalWorld);
                    }
                }
            }
            resArray.Add(results.Length);
            return resArray;
        }
    }
}
