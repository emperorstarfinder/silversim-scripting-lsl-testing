// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Physics
{
    [ScriptApiName("Physics")]
    [LSLImplementation]
    public class PhysicsApi : IScriptApi, IPlugin
    {
        public PhysicsApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL, "llSetTorque")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetTorque(ScriptInstance instance, Vector3 torque, int local)
        {
            lock (instance)
            {
                IPhysicsObject physobj = instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (null == physobj)
                {
                    instance.ShoutError("Object has not physical properties");
                    return;
                }
                
                if (local != 0)
                {
                    physobj.AppliedTorque = torque / instance.Part.ObjectGroup.GlobalRotation;
                }
                else
                {
                    physobj.AppliedTorque = torque;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetForce")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetForce(ScriptInstance instance, Vector3 force, int local)
        {
            lock (instance)
            {
                IPhysicsObject physobj = instance.Part.ObjectGroup.RootPart.PhysicsActor;
                if (null == physobj)
                {
                    instance.ShoutError("Object has not physical properties");
                    return;
                }

                if (local != 0)
                {
                    physobj.AppliedForce = force / instance.Part.ObjectGroup.GlobalRotation;
                }
                else
                {
                    physobj.AppliedForce = force;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetForceAndTorque")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetForceAndTorque(ScriptInstance instance, Vector3 force, Vector3 torque, int local)
        {
            lock (instance)
            {
                IPhysicsObject physobj = instance.Part.ObjectGroup.RootPart.PhysicsActor;
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
                    physobj.AppliedForce = force / instance.Part.ObjectGroup.GlobalRotation;
                    physobj.AppliedTorque = torque / instance.Part.ObjectGroup.GlobalRotation;
                }
                else
                {
                    physobj.AppliedForce = force;
                    physobj.AppliedTorque = torque;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetBuoyancy")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetBuoyancy(ScriptInstance instance, double buoyancy)
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

        [APILevel(APIFlags.LSL, "llPushObject")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void PushObject(ScriptInstance instance, LSLKey target, Vector3 impulse, Vector3 ang_impulse, int local)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llApplyImpulse")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void ApplyImpulse(ScriptInstance instance, Vector3 momentum, int local)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llApplyRotationalImpulse")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void ApplyRotationalImpulse(ScriptInstance instance, Vector3 ang_impulse, int local)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llSetVelocity")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetVelocity(ScriptInstance instance, Vector3 velocity, int local)
        {
            lock (instance)
            {
                /* we leave the physics check out here since it has an interesting use */
                if (local != 0)
                {
                    instance.Part.ObjectGroup.Velocity = velocity / instance.Part.ObjectGroup.GlobalRotation;
                }
                else
                {
                    instance.Part.ObjectGroup.Velocity = velocity;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetAngularVelocity")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetAngularVelocity(ScriptInstance instance, Vector3 initial_omega, int local)
        {
            lock (instance)
            {
                /* we leave the physics check out here since it has an interesting use */
                if (local != 0)
                {
                    instance.Part.ObjectGroup.AngularVelocity = initial_omega / instance.Part.ObjectGroup.GlobalRotation;
                }
                else
                {
                    instance.Part.ObjectGroup.AngularVelocity = initial_omega;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetPhysicsMaterial")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        AnArray GetPhysicsMaterial(ScriptInstance instance)
        {
            AnArray array = new AnArray();
            lock (instance)
            {
                array.Add(instance.Part.ObjectGroup.RootPart.PhysicsGravityMultiplier);
                array.Add(instance.Part.ObjectGroup.RootPart.PhysicsRestitution);
                array.Add(instance.Part.ObjectGroup.RootPart.PhysicsFriction);
                array.Add(instance.Part.ObjectGroup.RootPart.PhysicsDensity);
                return array;
            }
        }

        const int DENSITY = 1;
        const int FRICTION = 2;
        const int RESTITUTION = 4;
        const int GRAVITY_MULTIPLIER = 8;

        [APILevel(APIFlags.LSL, "llSetPhysicsMaterial")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetPhysicsMaterial(ScriptInstance instance, int mask, double gravity_multiplier, double restitution, double friction, double density)
        {
            lock (instance)
            {
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
                    instance.Part.ObjectGroup.RootPart.PhysicsDensity = density;
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
                    instance.Part.ObjectGroup.RootPart.PhysicsFriction = friction;
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
                    instance.Part.ObjectGroup.RootPart.PhysicsRestitution = restitution;
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
                    instance.Part.ObjectGroup.RootPart.PhysicsGravityMultiplier = gravity_multiplier;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetHoverHeight")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetHoverHeight(ScriptInstance instance, double height, int water, double tau)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llStopHover")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void StopHover(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }
    }
}
