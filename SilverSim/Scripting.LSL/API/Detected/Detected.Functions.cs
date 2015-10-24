// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using System;

namespace SilverSim.Scripting.LSL.API.Detected
{
    public partial class Detected_API
    {
        /* REMARKS: The internal attribute for the LSLScript has been done deliberately here.
         * The other option of implementing this would have been to make it a namespace class of the Script class.
         */
        [APILevel(APIFlags.LSL, "llDetectedGrab")]
        public Vector3 DetectedGrab(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].GrabOffset;
                }
                return Vector3.Zero;
            }
        }

        [APILevel(APIFlags.LSL, "llDetectedGroup")]
        public int DetectedGroup(ScriptInstance instance, int number)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llDetectedKey")]
        public LSLKey DetectedKey(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].Object.ID;
                }
                return UUID.Zero;
            }
        }

        [APILevel(APIFlags.LSL, "llDetectedLinkNumber")]
        public int DetectedLinkNumber(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].LinkNumber;
                }
                return -1;
            }
        }

        [APILevel(APIFlags.LSL, "llDetectedName")]
        public string DetectedName(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].Object.Name;
                }
                return string.Empty;
            }
        }

        [APILevel(APIFlags.LSL, "llDetectedOwner")]
        public LSLKey DetectedOwner(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].Object.Owner.ID;
                }
                return UUID.Zero;
            }
        }

        [APILevel(APIFlags.LSL, "llDetectedPos")]
        public Vector3 DetectedPos(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].Object.GlobalPosition;
                }
                return Vector3.Zero;
            }
        }

        [APILevel(APIFlags.LSL, "llDetectedRot")]
        public Quaternion DetectedRot(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].Object.GlobalRotation;
                }
                return Quaternion.Identity;
            }
        }

        [APILevel(APIFlags.LSL, "llDetectedTouchBinormal")]
        public Vector3 DetectedTouchBinormal(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].TouchBinormal;
                }
                return Vector3.Zero;
            }
        }

        [APILevel(APIFlags.LSL, "llDetectedTouchFace")]
        public int DetectedTouchFace(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].TouchFace;
                }
                return -1;
            }
        }

        [APILevel(APIFlags.LSL, "llDetectedTouchNormal")]
        public Vector3 DetectedTouchNormal(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].TouchNormal;
                }
                return Vector3.Zero;
            }
        }

        [APILevel(APIFlags.LSL, "llDetectedTouchPos")]
        public Vector3 DetectedTouchPos(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].TouchPosition;
                }
            }
            return Vector3.Zero;
        }

        [APILevel(APIFlags.LSL, "llDetectedTouchST")]
        public Vector3 DetectedTouchST(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].TouchST;
                }
                return Vector3.Zero;
            }
        }

        [APILevel(APIFlags.LSL, "llDetectedTouchUV")]
        public Vector3 DetectedTouchUV(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].TouchUV;
                }
                return Vector3.Zero;
            }
        }

        [APILevel(APIFlags.LSL, "llDetectedType")]
        public int DetectedType(ScriptInstance instance, int number)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llDetectedVel")]
        public Vector3 DetectedVel(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].Object.Velocity;
                }
                return Vector3.Zero;
            }
        }
    }
}
