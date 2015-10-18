﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL.API.Physics
{
    [ScriptApiName("Physics")]
    [LSLImplementation]
    public class Physics_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public Physics_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetTorque")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetForce")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetForceAndTorque")]
        public void SetForceAndTorque(ScriptInstance instance, Vector3 force, Vector3 torque, int local)
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetBuoyancy")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llPushObject")]
        public void PushObject(ScriptInstance instance, LSLKey target, Vector3 impulse, Vector3 ang_impulse, int local)
        {
#warning Implement llPushObject
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llApplyImpulse")]
        public void ApplyImpulse(ScriptInstance instance, Vector3 momentum, int local)
        {
#warning Implement llApplyImpulse
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llApplyRotationalImpulse")]
        public void ApplyRotationalImpulse(ScriptInstance instance, Vector3 ang_impulse, int local)
        {
#warning Implement llApplyRotationalImpulse
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetVelocity")]
        public void SetVelocity(ScriptInstance instance, Vector3 velocity, int local)
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetAngularVelocity")]
        public void SetAngularVelocity(ScriptInstance instance, Vector3 initial_omega, int local)
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetPhysicsMaterial")]
        public AnArray GetPhysicsMaterial(ScriptInstance instance)
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetPhysicsMaterial")]
        public void SetPhysicsMaterial(ScriptInstance instance, int mask, double gravity_multiplier, double restitution, double friction, double density)
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetHoverHeight")]
        public void SetHoverHeight(ScriptInstance instance, double height, int water, double tau)
        {
#warning Implement llSetHoverHeight
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llStopHover")]
        public void StopHover(ScriptInstance instance)
        {
#warning Implement llStopHover
            throw new NotImplementedException();
        }
    }
}
