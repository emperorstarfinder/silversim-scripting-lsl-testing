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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Physics
{
    [ScriptApiName("Physics")]
    [LSLImplementation]
    [Description("LSL/OSSL Physics API")]
    public class PhysicsApi : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL, "llGetMass")]
        public double GetMass(ScriptInstance instance)
        {
            lock(instance)
            {
                return instance.Part.Mass;
            }
        }

        [APILevel(APIFlags.LSL, "llGetMassMKS")]
        public double GetMassMKS(ScriptInstance instance) => GetMass(instance) * 100;

        [APILevel(APIFlags.LSL, "llVolumeDetect")]
        public void VolumeDetect(ScriptInstance instance, int enable)
        {
            lock(instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                grp.IsVolumeDetect = enable != 0;
            }
        }

        [APILevel(APIFlags.LSL, "llSetTorque")]
        public void SetTorque(ScriptInstance instance, Vector3 torque, int local)
        {
            lock (instance)
            {
                IPhysicsObject physobj = instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (physobj == null)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "ObjectHasNoPhysicalProperties", "Object has no physical properties"));
                    return;
                }

                physobj.SetAppliedTorque((local != 0) ?
                    torque / instance.Part.ObjectGroup.GlobalRotation :
                    torque);
            }
        }

        [APILevel(APIFlags.LSL, "llGetForce")]
        public Vector3 GetForce(ScriptInstance instance)
        {
            lock(instance)
            {
                IPhysicsObject physobj = instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (physobj == null)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "ObjectHasNoPhysicalProperties", "Object has no physical properties"));
                    return Vector3.Zero;
                }

                return physobj.Force;
            }
        }

        [APILevel(APIFlags.LSL, "llGetTorque")]
        public Vector3 GetTorque(ScriptInstance instance)
        {
            lock (instance)
            {
                IPhysicsObject physobj = instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (physobj == null)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "ObjectHasNoPhysicalProperties", "Object has no physical properties"));
                    return Vector3.Zero;
                }

                return physobj.Torque;
            }
        }

        [APILevel(APIFlags.LSL, "llGetAccel")]
        public Vector3 GetAccel(ScriptInstance instance)
        {
            lock(instance)
            {
                return instance.Part.ObjectGroup.Acceleration;
            }
        }

        [APILevel(APIFlags.LSL, "llGetOmega")]
        public Vector3 GetOmega(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.AngularVelocity;
            }
        }

        [APILevel(APIFlags.LSL, "llSetForce")]
        public void SetForce(ScriptInstance instance, Vector3 force, int local)
        {
            lock (instance)
            {
                IPhysicsObject physobj = instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (physobj == null)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "ObjectHasNoPhysicalProperties", "Object has no physical properties"));
                    return;
                }

                physobj.SetAppliedForce((local != 0) ?
                    force / instance.Part.ObjectGroup.GlobalRotation :
                    force);
            }
        }

        [APILevel(APIFlags.LSL, "llSetForceAndTorque")]
        public void SetForceAndTorque(ScriptInstance instance, Vector3 force, Vector3 torque, int local)
        {
            lock (instance)
            {
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                IPhysicsObject physobj = thisGroup.RootPart.PhysicsActor;
                if (physobj == null)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "ObjectHasNoPhysicalProperties", "Object has no physical properties"));
                    return;
                }

                if (force == Vector3.Zero || torque == Vector3.Zero)
                {
                    physobj.SetAppliedTorque(Vector3.Zero);
                    physobj.SetAppliedForce(Vector3.Zero);
                }
                else if(local != 0)
                {
                    physobj.SetAppliedForce(force / thisGroup.GlobalRotation);
                    physobj.SetAppliedTorque(torque / thisGroup.GlobalRotation);
                }
                else
                {
                    physobj.SetAppliedForce(force);
                    physobj.SetAppliedTorque(torque);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetBuoyancy")]
        public void SetBuoyancy(ScriptInstance instance, double buoyancy)
        {
            lock(instance)
            {
                IPhysicsObject physobj = instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (physobj == null)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "ObjectHasNoPhysicalProperties", "Object has no physical properties"));
                    return;
                }

                physobj.Buoyancy = buoyancy;
            }
        }

        [APILevel(APIFlags.LSL, "llPushObject")]
        public void PushObject(ScriptInstance instance, LSLKey target, Vector3 impulse, Vector3 ang_impulse, int local)
        {
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                ObjectGroup grp;
                IAgent agent;
                IPhysicsObject physObj = null;
                Quaternion rot;

                if (scene.RootAgents.TryGetValue(target.AsUUID, out agent))
                {
                    physObj = agent.PhysicsActor;
                    rot = agent.GlobalRotation;
                }
                else if (scene.ObjectGroups.TryGetValue(target.AsUUID, out grp))
                {
                    physObj = grp.PhysicsActor;
                    rot = grp.GlobalRotation;
                }
                else
                {
                    rot = Quaternion.Identity;
                }

                if (physObj != null)
                {
                    if(local != 0)
                    {
                        impulse /= rot;
                        ang_impulse /= rot;
                    }
                    physObj.SetLinearImpulse(impulse);
                    physObj.SetAngularImpulse(ang_impulse);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llApplyImpulse")]
        public void ApplyImpulse(ScriptInstance instance, Vector3 momentum, int local)
        {
            throw new NotImplementedException("llApplyImpulse(vector, integer)");
        }

        [APILevel(APIFlags.LSL, "llApplyRotationalImpulse")]
        public void ApplyRotationalImpulse(ScriptInstance instance, Vector3 ang_impulse, int local)
        {
            throw new NotImplementedException("llApplyRotationalImpulse(vector, integer)");
        }

        [APILevel(APIFlags.LSL, "llSetVelocity")]
        public void SetVelocity(ScriptInstance instance, Vector3 velocity, int local)
        {
            lock (instance)
            {
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                /* we leave the physics check out here since it has an interesting use */
                thisGroup.Velocity = (local != 0) ?
                    velocity / thisGroup.GlobalRotation :
                    velocity;
            }
        }

        [APILevel(APIFlags.LSL, "llSetAngularVelocity")]
        public void SetAngularVelocity(ScriptInstance instance, Vector3 initial_omega, int local)
        {
            lock (instance)
            {
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                /* we leave the physics check out here since it has an interesting use */
                thisGroup.AngularVelocity = (local != 0) ?
                    initial_omega / thisGroup.GlobalRotation :
                    initial_omega;
            }
        }

        [APILevel(APIFlags.LSL, "llGetPhysicsMaterial")]
        public AnArray GetPhysicsMaterial(ScriptInstance instance)
        {
            AnArray array = new AnArray();
            lock (instance)
            {
                ObjectPart rootPart = instance.Part.ObjectGroup.RootPart;
                array.Add(rootPart.PhysicsGravityMultiplier);
                array.Add(rootPart.PhysicsRestitution);
                array.Add(rootPart.PhysicsFriction);
                array.Add(rootPart.PhysicsDensity);
                return array;
            }
        }

        /* private constants, public ones are in PrimitiveAPI */
        private const int DENSITY = 1;
        private const int FRICTION = 2;
        private const int RESTITUTION = 4;
        private const int GRAVITY_MULTIPLIER = 8;

        [APILevel(APIFlags.LSL, "llSetPhysicsMaterial")]
        public void SetPhysicsMaterial(ScriptInstance instance, int mask, double gravity_multiplier, double restitution, double friction, double density)
        {
            lock (instance)
            {
                ObjectPart rootPart = instance.Part.ObjectGroup.RootPart;
                if (0 != (mask & DENSITY))
                {
                    if (density < 1)
                    {
                        density = 1;
                    }
                    else if (density > 22587f)
                    {
                        density = 22587f;
                    }
                    rootPart.PhysicsDensity = density;
                }
                if (0 != (mask & FRICTION))
                {
                    if (friction < 0)
                    {
                        friction = 0f;
                    }
                    else if (friction > 255f)
                    {
                        friction = 255f;
                    }
                    rootPart.PhysicsFriction = friction;
                }
                if (0 != (mask & RESTITUTION))
                {
                    if (restitution < 0f)
                    {
                        restitution = 0f;
                    }
                    else if (restitution > 1f)
                    {
                        restitution = 1f;
                    }
                    rootPart.PhysicsRestitution = restitution;
                }
                if (0 != (mask & GRAVITY_MULTIPLIER))
                {
                    if (gravity_multiplier < -1f)
                    {
                        gravity_multiplier = -1f;
                    }
                    else if (gravity_multiplier > 28f)
                    {
                        gravity_multiplier = 28f;
                    }
                    rootPart.PhysicsGravityMultiplier = gravity_multiplier;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGroundRepel")]
        public void GroundRepel(ScriptInstance instance, double height, int water, double tau)
        {
            lock (instance)
            {
                IPhysicsObject physobj = instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (physobj == null)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "ObjectHasNoPhysicalProperties", "Object has no physical properties"));
                    return;
                }

                physobj.SetHoverHeight(height, water != 0, tau);
            }
        }

        [APILevel(APIFlags.LSL, "llSetHoverHeight")]
        public void SetHoverHeight(ScriptInstance instance, double height, int water, double tau)
        {
            lock (instance)
            {
                IPhysicsObject physobj = instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (physobj == null)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "ObjectHasNoPhysicalProperties", "Object has no physical properties"));
                    return;
                }

                physobj.SetHoverHeight(height, water != 0, tau);
            }
        }

        [APILevel(APIFlags.LSL, "llStopHover")]
        public void StopHover(ScriptInstance instance)
        {
            lock (instance)
            {
                IPhysicsObject physobj = instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (physobj == null)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "ObjectHasNoPhysicalProperties", "Object has no physical properties"));
                    return;
                }

                physobj.StopHover();
            }
        }

        [APILevel(APIFlags.LSL, "llMoveToTarget")]
        public void MoveToTarget(ScriptInstance instance, Vector3 target, double tau)
        {
            lock(instance)
            {
                ObjectPart part = instance.Part;
                ObjectGroup grp = part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                if(grp.IsAttached)
                {
                    IAgent agent;
                    if(scene.RootAgents.TryGetValue(scene.ID, out agent))
                    {
                        agent.MoveToTarget(target, tau, part.ID, instance.Item.ID);
                    }
                }
                else
                {
                    grp.MoveToTarget(target, tau, part.ID, instance.Item.ID);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llStopMoveToTarget")]
        public void StopMoveToTarget(ScriptInstance instance)
        {
            lock (instance)
            {
                ObjectPart part = instance.Part;
                ObjectGroup grp = part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                if (grp.IsAttached)
                {
                    IAgent agent;
                    if (scene.RootAgents.TryGetValue(scene.ID, out agent))
                    {
                        agent.StopMoveToTarget();
                    }
                }
                else
                {
                    grp.StopMoveToTarget();
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetObjectMass")]
        public double GetObjectMass(ScriptInstance instance, LSLKey id)
        {
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                ObjectPart part;
                IAgent agent;
                if(scene.Primitives.TryGetValue(id.AsUUID, out part))
                {
                    return part.Mass;
                }
                else if(scene.Agents.TryGetValue(id.AsUUID, out agent))
                {
                    return agent.IsInScene(scene) ? agent.PhysicsActor.Mass : 0.01;
                }
                else
                {
                    return 0;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llCollisionFilter")]
        public void CollisionFilter(ScriptInstance instance, string name, LSLKey id, int accept)
        {
            throw new NotImplementedException("llCollisionFilter(string, key, integer)");
        }

        [APILevel(APIFlags.LSL, "llCollisionSprite")]
        public void CollisionSprite(ScriptInstance instance, string impact_sprite)
        {
            throw new NotImplementedException("llCollisionSprite(string)");
        }

        [APILevel(APIFlags.OSSL, "osGetPhysicsEngineType")]
        public string GetPhysicsEngineType(ScriptInstance instance)
        {
            lock(instance)
            {
                throw new NotImplementedException("osGetPhysicsEngineType()");
            }
        }
    }
}
