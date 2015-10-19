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
        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDetectedGrab")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDetectedGroup")]
        public int DetectedGroup(ScriptInstance instance, int number)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDetectedKey")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDetectedLinkNumber")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDetectedName")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDetectedOwner")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDetectedPos")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDetectedRot")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDetectedTouchBinormal")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDetectedTouchFace")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDetectedTouchNormal")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDetectedTouchPos")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDetectedTouchST")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDetectedTouchUV")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llDetectedType")]
        public int DetectedType(ScriptInstance instance, int number)
        {
            throw new NotImplementedException();
        }

        [ScriptFunctionName("llDetectedVel")]
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
