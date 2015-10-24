// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using System;
using System.Reflection;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_UNKNOWN_DETAIL = -1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_NAME = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_DESC = 2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_POS = 3;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_ROT = 4;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_VELOCITY = 5;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_OWNER = 6;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_GROUP = 7;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_CREATOR = 8;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_RUNNING_SCRIPT_COUNT = 9;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_TOTAL_SCRIPT_COUNT = 10;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_SCRIPT_MEMORY = 11;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_SCRIPT_TIME = 12;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_PRIM_EQUIVALENCE = 13;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_SERVER_COST = 14;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_STREAMING_COST = 15;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_PHYSICS_COST = 16;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_CHARACTER_TIME = 17;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_ROOT = 18;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_ATTACHED_POINT = 19;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_PATHFINDING_TYPE = 20;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_PHYSICS = 21;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_PHANTOM = 22;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int OBJECT_TEMP_ON_REZ = 23;

        [APILevel(APIFlags.LSL, "llGetCenterOfMass")]
        public Vector3 GetCenterOfMass(ScriptInstance instance)
        {
            throw new NotImplementedException("llGetCenterOfMass()");
        }

        [APILevel(APIFlags.LSL, "llGetCreator")]
        public LSLKey GetCreator(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.Creator.ID;
            }
        }

        [APILevel(APIFlags.LSL, "llGetObjectDesc")]
        public string GetObjectDesc(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.Name;
            }
        }

        [APILevel(APIFlags.LSL, "llGetObjectDetails")]
        public AnArray GetObjectDetails(ScriptInstance instance, AnArray param)
        {
            AnArray parout = new AnArray();
            lock (instance)
            {
                instance.Part.ObjectGroup.GetObjectDetails(param.GetEnumerator(), ref parout);
            }
            return parout;
        }

        [APILevel(APIFlags.LSL, "llGetObjectName")]
        public string GetObjectName(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.Description;
            }
        }

        [APILevel(APIFlags.LSL, "llSetObjectDesc")]
        public void SetObjectDesc(ScriptInstance instance, string desc)
        {
            lock (instance)
            {
                instance.Part.Description = desc;
            }
        }

        [APILevel(APIFlags.LSL, "llSetObjectName")]
        public void SetObjectName(ScriptInstance instance, string name)
        {
            lock (instance)
            {
                instance.Part.Name = name;
            }
        }

        [APILevel(APIFlags.LSL, "llSetRegionPos")]
        public int SetRegionPos(ScriptInstance instance, Vector3 pos)
        {
            throw new NotImplementedException("llSetRegionPos(vector)");
        }

        [APILevel(APIFlags.LSL, "llGetVel")]
        public Vector3 GetVel(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Velocity;
            }
        }

        [APILevel(APIFlags.LSL, "llGetOwner")]
        public LSLKey GetOwner(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Owner.ID;
            }
        }

        [APILevel(APIFlags.LSL, "llGetOwnerKey")]
        public LSLKey GetOwnerKey(ScriptInstance instance, LSLKey id)
        {
            lock (instance)
            {
                ObjectPart part;
                try
                {
                    part = instance.Part.ObjectGroup.Scene.Primitives[id];
                }
                catch
                {
                    return id;
                }
                return part.Owner.ID;
            }
        }

        [APILevel(APIFlags.LSL, "llGetNumberOfPrims")]
        public int GetNumberOfPrims(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Count;
            }
        }

        [APILevel(APIFlags.LSL, "llGetLinkKey")]
        public LSLKey GetLinkKey(ScriptInstance instance, int link)
        {
            lock (instance)
            {
                if (link == LINK_THIS)
                {
                    return instance.Part.ID;
                }
                else
                {
                    return instance.Part.ObjectGroup[link].ID;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetLinkName")]
        public string GetLinkName(ScriptInstance instance, int link)
        {
            lock (instance)
            {
                if (link == LINK_THIS)
                {
                    return instance.Part.Name;
                }
                else
                {
                    return instance.Part.ObjectGroup[link].Name;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetLinkNumber")]
        public int GetLinkNumber(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.LinkNumber;
            }
        }

        #region osMessageObject
        [APILevel(APIFlags.ASSL, "object_message")]
        [StateEventDelegate]
        public delegate void State_object_message(LSLKey id, string data);

        [APILevel(APIFlags.OSSL, "osMessageObject")]
        public void MessageObject(ScriptInstance instance, LSLKey objectUUID, string message)
        {
            lock (instance)
            {
                instance.CheckThreatLevel(MethodBase.GetCurrentMethod().Name, ScriptInstance.ThreatLevelType.Low);

                IObject obj = instance.Part.ObjectGroup.Scene.Objects[objectUUID];
                MessageObjectEvent ev = new MessageObjectEvent();
                ev.Data = message;
                ev.ObjectID = instance.Part.ObjectGroup.ID;
                obj.PostEvent(ev);
            }
        }
        #endregion
    }
}
