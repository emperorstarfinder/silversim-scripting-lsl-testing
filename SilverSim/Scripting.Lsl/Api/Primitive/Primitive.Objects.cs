// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using System;
using System.Reflection;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
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

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int STATUS_PHYSICS = 0x00000001;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int STATUS_ROTATE_X = 0x00000002;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int STATUS_ROTATE_Y = 0x00000004;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int STATUS_ROTATE_Z = 0x00000008;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int STATUS_PHANTOM = 0x00000010;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int STATUS_SANDBOX = 0x00000020;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int STATUS_BLOCK_GRAB = 0x00000040;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int STATUS_DIE_AT_EDGE = 0x00000080;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int STATUS_RETURN_AT_EDGE = 0x00000100;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int STATUS_CAST_SHADOWS = 0x00000200;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int STATUS_BLOCK_GRAB_OBJECT = 0x00000400;

        [APILevel(APIFlags.LSL, "llSetDamage")]
        public void SetDamage(ScriptInstance instance, double damage)
        {
            throw new NotImplementedException("llSetDamage(float)");
        }

        [APILevel(APIFlags.LSL, "llGetEnergy")]
        public double GetEnergy(ScriptInstance instance)
        {
            throw new NotImplementedException("llGetEnergy()");
        }

        [APILevel(APIFlags.LSL, "llGetBoundingBox")]
        public AnArray GetBoundingBox(ScriptInstance instance, LSLKey objectKey)
        {
            throw new NotImplementedException("llGetBoundingBox(key)");
        }

        [APILevel(APIFlags.LSL, "llGetGeometricCenter")]
        public Vector3 GetGeometricCenter(ScriptInstance instance)
        {
            throw new NotImplementedException("llGetGeometricCenter()");
        }

        [APILevel(APIFlags.LSL, "llGetAttached")]
        public int GetAttached(ScriptInstance instance)
        {
            lock(instance)
            {
                return (int)instance.Part.ObjectGroup.AttachPoint;
            }
        }

        [APILevel(APIFlags.LSL, "llGetObjectPrimCount")]
        public int GetObjectPrimCount(ScriptInstance instance, LSLKey key)
        {
            lock (instance)
            {
                ObjectGroup obj;
                if (!instance.Part.ObjectGroup.Scene.ObjectGroups.TryGetValue(key.AsUUID, out obj))
                {
                    return obj.Count;
                }
                return 0;
            }
        }

        [APILevel(APIFlags.OSSL, "osGetRezzingObject")]
        public LSLKey OsGetRezzingObject(ScriptInstance instance)
        {
            throw new NotImplementedException("osGetRezzingObject()");
        }

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
        public AnArray GetObjectDetails(ScriptInstance instance, LSLKey key, AnArray param)
        {
            AnArray parout = new AnArray();
            lock (instance)
            {
                IObject obj;
                if(!instance.Part.ObjectGroup.Scene.Objects.TryGetValue(key.AsUUID, out obj))
                {
                    return parout;
                }
                obj.GetObjectDetails(param.GetEnumerator(), ref parout);
            }
            return parout;
        }

        [APILevel(APIFlags.LSL, "llSameGroup")]
        public int SameGroup(ScriptInstance instance, LSLKey key)
        {
            lock(instance)
            {
                IObject obj;
                if (!instance.Part.ObjectGroup.Scene.Objects.TryGetValue(key.AsUUID, out obj))
                {
                    return 0;
                }
                return obj.Group.Equals(instance.Part.ObjectGroup.Group) ? 1 : 0;
            }
        }

        /* Private constants, exported once are in InventoryApi */
        public const int MASK_BASE = 0;
        public const int MASK_OWNER = 1;
        public const int MASK_GROUP = 2;
        public const int MASK_EVERYONE = 3;
        public const int MASK_NEXT = 4;

        [APILevel(APIFlags.LSL, "llGetObjectPermMask")]
        public int GetObjectPermMask(ScriptInstance instance, int mask)
        {
            lock(instance)
            {
                switch (mask)
                {
                    case MASK_BASE:
                        return (int)instance.Part.BaseMask;

                    case MASK_OWNER:
                        return (int)instance.Part.OwnerMask;

                    case MASK_GROUP:
                        return (int)instance.Part.GroupMask;

                    case MASK_EVERYONE:
                        return (int)instance.Part.EveryoneMask;

                    case MASK_NEXT:
                        return (int)instance.Part.NextOwnerMask;

                    default:
                        return 0;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetObjectPermMask")]
        public void SetObjectPermMask(ScriptInstance instance, int mask, int value)
        {
            lock(instance)
            {
                if(instance.Part.ObjectGroup.Scene.IsSimConsoleAllowed(instance.Part.Owner))
                {
                    switch(mask)
                    {
                        case MASK_BASE:
                            instance.Part.BaseMask = (InventoryPermissionsMask)value;
                            break;

                        case MASK_OWNER:
                            instance.Part.OwnerMask = (InventoryPermissionsMask)value;
                            break;

                        case MASK_GROUP:
                            instance.Part.GroupMask = (InventoryPermissionsMask)value;
                            break;

                        case MASK_EVERYONE:
                            instance.Part.EveryoneMask = (InventoryPermissionsMask)value;
                            break;

                        case MASK_NEXT:
                            instance.Part.NextOwnerMask = (InventoryPermissionsMask)value;
                            break;

                        default:
                            break;
                    }
                }
            }
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
                return (link == LINK_THIS) ?
                    instance.Part.ID :
                    instance.Part.ObjectGroup[link].ID;
            }
        }

        [APILevel(APIFlags.LSL, "llGetLinkName")]
        public string GetLinkName(ScriptInstance instance, int link)
        {
            lock (instance)
            {
                return (link == LINK_THIS) ?
                    instance.Part.Name :
                    instance.Part.ObjectGroup[link].Name;
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

        [APILevel(APIFlags.LSL, "llSetStatus")]
        public void SetStatus(ScriptInstance instance, int status, int value)
        {
            throw new NotImplementedException("llSetStatus(integer, integer)");
        }

        [APILevel(APIFlags.LSL, "llGetStatus")]
        public int SetStatus(ScriptInstance instance, int status)
        {
            throw new NotImplementedException("llGetStatus(integer)");
        }

        #region osMessageObject
        [APILevel(APIFlags.OSSL, APILevel.KeepCsName)]
        public const int OS_ATTACH_MSG_ALL = 0xFFFF;
        [APILevel(APIFlags.OSSL, APILevel.KeepCsName)]
        public const int OS_ATTACH_MSG_INVERT_POINTS = 1;
        [APILevel(APIFlags.OSSL, APILevel.KeepCsName)]
        public const int OS_ATTACH_MSG_OBJECT_CREATOR = 2;
        [APILevel(APIFlags.OSSL, APILevel.KeepCsName)]
        public const int OS_ATTACH_MSG_SCRIPT_CREATOR = 4;

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

        [APILevel(APIFlags.OSSL, "osMessageAttachments")]
        public void MessageAttachments(ScriptInstance instance, LSLKey avatar, string message, AnArray attachmentPoints, int options)
        {
            throw new NotImplementedException("osMessageAttachments(key, string, list, integer)");
        }
        #endregion
    }
}
