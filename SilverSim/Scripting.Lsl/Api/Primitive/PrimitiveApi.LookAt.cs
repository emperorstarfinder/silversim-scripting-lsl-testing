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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        [APILevel(APIFlags.LSL, "llLookAt")]
        public void LookAt(ScriptInstance instance, Vector3 target, double strength, double damping)
        {
            Quaternion targetRotation;

            lock (instance)
            {
                Vector3 direction = (target - instance.Part.GlobalPosition).Normalize();
                double z = Math.Atan2(direction.Y, direction.X);
                double y = Math.Atan2(direction.Z, Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y));

                targetRotation = Quaternion.CreateFromEulers(new Vector3(0, y, 0)) * Quaternion.CreateFromEulers(new Vector3(0, 0, z));
            }

            RotLookAt(instance, targetRotation, strength, damping);
        }

        [APILevel(APIFlags.LSL, "llStopLookAt")]
        public void StopLookAt(ScriptInstance instance)
        {
            lock(instance)
            {
                instance.Part.ObjectGroup.PhysicsActor.StopLookAt();
            }
        }

        [APILevel(APIFlags.LSL, "llRotLookAt")]
        public void RotLookAt(ScriptInstance instance, Quaternion target_direction, double strength, double damping)
        {
            lock (instance)
            {
                ObjectPart part = instance.Part;
                if (part.IsPhysics)
                {
                    part.PhysicsActor.SetLookAt(target_direction, strength, damping);
                }
                else
                {
                    /* from http://wiki.secondlife.com/wiki/LlRotLookAt: Non-physical objects are a straight set of rotation */
                    part.Rotation = target_direction;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llPointAt")]
        public void PointAt(ScriptInstance instance, Vector3 pos)
        {
            throw new NotImplementedException("llPointAt(vector)");
        }

        [APILevel(APIFlags.LSL, "llStopPointAt")]
        public void StopPointAt(ScriptInstance instance)
        {
            throw new NotImplementedException("llStopPointAt()");
        }
    }
}
