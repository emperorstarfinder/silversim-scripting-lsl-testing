// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using System;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Agent;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Detected
{
    public partial class DetectedApi
    {
        /* REMARKS: The internal attribute for the LSLScript has been done deliberately here.
         * The other option of implementing this would have been to make it a namespace class of the Script class.
         */
        [APILevel(APIFlags.LSL, "llDetectedGrab")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Vector3 DetectedGrab(ScriptInstance instance, int number)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int DetectedGroup(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].Object.Group.Equals(instance.Part.Group).ToLSLBoolean();
                }
                return 0;
            }
        }

        [APILevel(APIFlags.LSL, "llDetectedKey")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal LSLKey DetectedKey(ScriptInstance instance, int number)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int DetectedLinkNumber(ScriptInstance instance, int number)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal string DetectedName(ScriptInstance instance, int number)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal LSLKey DetectedOwner(ScriptInstance instance, int number)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Vector3 DetectedPos(ScriptInstance instance, int number)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Quaternion DetectedRot(ScriptInstance instance, int number)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Vector3 DetectedTouchBinormal(ScriptInstance instance, int number)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int DetectedTouchFace(ScriptInstance instance, int number)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Vector3 DetectedTouchNormal(ScriptInstance instance, int number)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Vector3 DetectedTouchPos(ScriptInstance instance, int number)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Vector3 DetectedTouchST(ScriptInstance instance, int number)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Vector3 DetectedTouchUV(ScriptInstance instance, int number)
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

        const int AGENT = 1;
        const int ACTIVE = 2;
        const int PASSIVE = 4;
        const int SCRIPTED = 8;

        [APILevel(APIFlags.LSL, "llDetectedType")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int DetectedType(ScriptInstance instance, int number)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    IObject obj = script.m_Detected[number].Object;

                    ObjectGroup grp;
                    ObjectPart part;
                    IAgent agent;

                    agent = obj as IAgent;
                    if(null != agent)
                    {
                        return (agent.SittingOnObject != null) ?
                            AGENT :
                            (AGENT | ACTIVE);
                    }

                    grp = obj as ObjectGroup;
                    if(null != grp)
                    {
                        int flags;
                        
                        flags = (obj.PhysicsActor.IsPhysicsActive) ?
                            ACTIVE :
                            PASSIVE;

                        foreach(ObjectPart p in grp.Values)
                        {
                            if(p.Inventory.CountScripts != 0)
                            {
                                flags |= SCRIPTED;
                                break;
                            }
                        }
                        return flags;
                    }

                    part = obj as ObjectPart;
                    if (null != part)
                    {
                        int flags;

                        flags = (part.ObjectGroup.PhysicsActor.IsPhysicsActive) ?
                            ACTIVE :
                            PASSIVE;

                        if(part.Inventory.CountScripts != 0)
                        {
                            flags |= SCRIPTED;
                        }
                        return flags;
                    }
                }
                return 0;
            }
        }

        [APILevel(APIFlags.LSL, "llDetectedVel")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Vector3 DetectedVel(ScriptInstance instance, int number)
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
