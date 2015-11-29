// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.Lsl.Api.Physics
{
    [ScriptApiName("Physics")]
    [LSLImplementation]
    public class PhysicsApi : IScriptApi, IPlugin
    {
        public PhysicsApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL, "llGetMass")]
        public double GetMass(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llVolumeDetect")]
        public void VolumeDetect(ScriptInstance instance, int enable)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llSetTorque")]
        public void SetTorque(ScriptInstance instance, Vector3 torque, int local)
        {
            lock (instance)
            {
                IPhysicsObject physobj = instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (null == physobj)
                {
                    instance.ShoutError("Object has not physical properties");
                    return;
                }
                
                physobj.AppliedTorque = (local != 0) ?
                    torque / instance.Part.ObjectGroup.GlobalRotation :
                    torque;
            }
        }

        [APILevel(APIFlags.LSL, "llSetForce")]
        public void SetForce(ScriptInstance instance, Vector3 force, int local)
        {
            lock (instance)
            {
                IPhysicsObject physobj = instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (null == physobj)
                {
                    instance.ShoutError("Object has not physical properties");
                    return;
                }

                physobj.AppliedForce = (local != 0) ?
                    force / instance.Part.ObjectGroup.GlobalRotation :
                    force;
            }
        }

        [APILevel(APIFlags.LSL, "llSetForceAndTorque")]
        public void SetForceAndTorque(ScriptInstance instance, Vector3 force, Vector3 torque, int local)
        {
            lock (instance)
            {
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                IPhysicsObject physobj = thisGroup.RootPart.PhysicsActor;
                if (null == physobj)
                {
                    instance.ShoutError("Object has not physical properties");
                    return;
                }

                if (force == Vector3.Zero || torque == Vector3.Zero)
                {
                    physobj.AppliedTorque = Vector3.Zero;
                    physobj.AppliedForce = Vector3.Zero;
                }
                else if(local != 0)
                {
                    physobj.AppliedForce = force / thisGroup.GlobalRotation;
                    physobj.AppliedTorque = torque / thisGroup.GlobalRotation;
                }
                else
                {
                    physobj.AppliedForce = force;
                    physobj.AppliedTorque = torque;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetBuoyancy")]
        public void SetBuoyancy(ScriptInstance instance, double buoyancy)
        {
            lock(instance)
            {
                IPhysicsObject physobj = instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (null == physobj)
                {
                    instance.ShoutError("Object has not physical properties");
                    return;
                }

                physobj.Buoyancy = buoyancy;
            }
        }

        [APILevel(APIFlags.LSL, "llGroundRepel")]
        public void GroundRepel(ScriptInstance instance, double height, int water, double tau)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llPushObject")]
        public void PushObject(ScriptInstance instance, LSLKey target, Vector3 impulse, Vector3 ang_impulse, int local)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llApplyImpulse")]
        public void ApplyImpulse(ScriptInstance instance, Vector3 momentum, int local)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llApplyRotationalImpulse")]
        public void ApplyRotationalImpulse(ScriptInstance instance, Vector3 ang_impulse, int local)
        {
            throw new NotImplementedException();
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

        const int DENSITY = 1;
        const int FRICTION = 2;
        const int RESTITUTION = 4;
        const int GRAVITY_MULTIPLIER = 8;

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

        [APILevel(APIFlags.LSL, "llSetHoverHeight")]
        public void SetHoverHeight(ScriptInstance instance, double height, int water, double tau)
        {
            throw new NotImplementedException("llSetHoverHeight");
        }

        [APILevel(APIFlags.LSL, "llStopHover")]
        public void StopHover(ScriptInstance instance)
        {
            throw new NotImplementedException("llStopHover");
        }

        [APILevel(APIFlags.LSL, "llMoveToTarget")]
        public void MoveToTarget(ScriptInstance instance, Vector3 target, double tau)
        {
            throw new NotImplementedException("llMoveToTarget");
        }

        [APILevel(APIFlags.LSL, "llStopMoveToTarget")]
        public void llStopMoveToTarget(ScriptInstance instance)
        {
            throw new NotImplementedException("llStopMoveToTarget");
        }
    }
}
