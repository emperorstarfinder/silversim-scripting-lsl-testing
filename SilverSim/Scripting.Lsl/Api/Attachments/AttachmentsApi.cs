// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Script;
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Attachments
{
    [ScriptApiName("Agents")]
    [LSLImplementation]
    [Description("LSL/OSSL Agents API")]
    public class AttachmentsApi : IScriptApi
    {
        public AttachmentsApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL, "llAttachToAvatar")]
        public void AttachToAvatar(ScriptInstance instance, int attach_point)
        {
            lock (instance)
            {
                ObjectPartInventoryItem.PermsGranterInfo grantInfo = instance.Item.PermsGranter;
                if (!grantInfo.PermsGranter.EqualsGrid(instance.Part.Owner) ||
                    !grantInfo.PermsMask.HasFlag(ScriptPermissions.Attach))
                {
                    return;
                }
            }
            throw new NotImplementedException("llAttachToAvatar(integer)");
        }

        [APILevel(APIFlags.LSL, "llAttachToAvatarTemp")]
        public void AttachToAvatarTemp(ScriptInstance instance, int attach_point)
        {
            lock (instance)
            {
                ObjectPartInventoryItem.PermsGranterInfo grantInfo = instance.Item.PermsGranter;
                if (!grantInfo.PermsGranter.EqualsGrid(instance.Part.Owner) ||
                    !grantInfo.PermsMask.HasFlag(ScriptPermissions.Attach))
                {
                    return;
                }
            }
            throw new NotImplementedException("llAttachToAvatarTemp(integer)");
        }

        [APILevel(APIFlags.LSL, "llDetachFromAvatar")]
        public void DetachFromAvatar(ScriptInstance instance)
        {
            lock(instance)
            {
                ObjectPartInventoryItem.PermsGranterInfo grantInfo = instance.Item.PermsGranter;
                if(!grantInfo.PermsGranter.EqualsGrid(instance.Part.Owner) ||
                    !grantInfo.PermsMask.HasFlag(ScriptPermissions.Attach))
                {
                    return;
                }
            }
            ForceDetachFromAvatar(instance);
        }

        [APILevel(APIFlags.OSSL, "osForceDetachFromAvatar")]
        public void ForceDetachFromAvatar(ScriptInstance instance)
        {
            lock(instance)
            {
                ObjectPart part = instance.Part;
                ObjectGroup group = part.ObjectGroup;
                SceneInterface scene = group.Scene;
                IAgent agent;
                if(!scene.RootAgents.TryGetValue(group.Owner.ID, out agent))
                {
                    return;
                }

                if (group.FromItemID != UUID.Zero)
                {
                    AssetData data = group.Asset(XmlSerializationOptions.WriteXml2);
                    data.ID = UUID.Random;
                    agent.AssetService.Store(data);
                    InventoryItem item;
                    if(agent.InventoryService.Item.TryGetValue(group.FromItemID, out item) && item.AssetType == AssetType.Object)
                    {
                        item.AssetID = data.ID;
                        agent.InventoryService.Item.Update(item);
                    }
                }

                agent.Attachments.Remove(group.ID);
                scene.Remove(group);
            }
        }

        [APILevel(APIFlags.OSSL, "osForceAttachToOtherAvatarFromInventory")]
        public void ForceAttachToOtherAvatarFromInventory(ScriptInstance instance, LSLKey id, string item_name, int attach_point)
        {
            lock (instance)
            {
                instance.CheckThreatLevel("osForceAttachToOtherAvatarFromInventory", ScriptInstance.ThreatLevelType.VeryHigh);
                throw new NotImplementedException("osForceAttachToOtherAvatarFromInventory(key, string, integer)");
            }
        }

        [APILevel(APIFlags.OSSL, "osDropAttachment")]
        public void DropAttachment(ScriptInstance instance)
        {
            lock (instance)
            {
                ObjectPartInventoryItem.PermsGranterInfo grantInfo = instance.Item.PermsGranter;
                if (!grantInfo.PermsGranter.EqualsGrid(instance.Part.Owner) ||
                    !grantInfo.PermsMask.HasFlag(ScriptPermissions.Attach))
                {
                    return;
                }
            }
            ForceDropAttachment(instance);
        }

        [APILevel(APIFlags.OSSL, "osDropAttachmentAt")]
        public void DropAttachmentAt(ScriptInstance instance, Vector3 pos, Quaternion rot)
        {
            lock (instance)
            {
                ObjectPartInventoryItem.PermsGranterInfo grantInfo = instance.Item.PermsGranter;
                if (!grantInfo.PermsGranter.EqualsGrid(instance.Part.Owner) ||
                    !grantInfo.PermsMask.HasFlag(ScriptPermissions.Attach))
                {
                    return;
                }
            }
            ForceDropAttachmentAt(instance, pos, rot);
        }

        [APILevel(APIFlags.OSSL, "osForceDropAttachment")]
        public void ForceDropAttachment(ScriptInstance instance)
        {
            throw new NotImplementedException("osForceDropAttachment()");
        }

        [APILevel(APIFlags.OSSL, "osForceDropAttachmentAt")]
        public void ForceDropAttachmentAt(ScriptInstance instance, Vector3 pos, Quaternion rot)
        {
            throw new NotImplementedException("osForceDropAttachmentAt(vector, rotation)");
        }

        [APILevel(APIFlags.OSSL, "osGetNumberOfAttachments")]
        public AnArray GetNumberOfAttachments(ScriptInstance instance, LSLKey avatar, AnArray attachmentPoints)
        {
            AnArray res = new AnArray();
            lock (instance)
            {
                IAgent agent;
                if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(avatar.AsUUID, out agent))
                {
                    foreach (IValue iv in attachmentPoints)
                    {
                        int point = iv.AsInt;
                        res.Add(point);
                        if (0 == point)
                        {
                            res.Add(0);
                        }
                        else
                        {
                            res.Add(agent.Attachments[(AttachmentPoint)point].Count);
                        }
                    }
                }
            }
            return res;
        }

        static AttachmentPoint[] PublicAttachments = new AttachmentPoint[]
        {
            AttachmentPoint.Chest,
            AttachmentPoint.Head,
            AttachmentPoint.LeftShoulder,
            AttachmentPoint.RightShoulder,
            AttachmentPoint.LeftHand,
            AttachmentPoint.RightHand,
            AttachmentPoint.LeftFoot,
            AttachmentPoint.RightFoot,
            AttachmentPoint.Back,
            AttachmentPoint.Pelvis,
            AttachmentPoint.Mouth,
            AttachmentPoint.Chin,
            AttachmentPoint.LeftEar,
            AttachmentPoint.RightEar,
            AttachmentPoint.LeftEye,
            AttachmentPoint.RightEye,
            AttachmentPoint.Nose,
            AttachmentPoint.RightUpperArm,
            AttachmentPoint.RightLowerArm,
            AttachmentPoint.LeftUpperArm,
            AttachmentPoint.LeftLowerArm,
            AttachmentPoint.RightHip,
            AttachmentPoint.RightUpperLeg,
            AttachmentPoint.RightLowerLeg,
            AttachmentPoint.LeftHip,
            AttachmentPoint.LeftUpperLeg,
            AttachmentPoint.LeftLowerLeg,
            AttachmentPoint.Belly,
            AttachmentPoint.RightPec,
            AttachmentPoint.LeftPec,
            AttachmentPoint.Neck,
            AttachmentPoint.AvatarCenter
        };

        [APILevel(APIFlags.OSSL, "llGetAttachedList")]
        public AnArray GetAttachedList(ScriptInstance instance, LSLKey avatar)
        {
            AnArray res = new AnArray();
            lock (instance)
            {
                IAgent agent;
                if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(avatar.AsUUID, out agent))
                {
                    foreach(AttachmentPoint ap in PublicAttachments)
                    {
                        if(agent.Attachments[ap].Count != 0)
                        {
                            res.Add((int)ap);
                        }
                    }
                }
            }
            return res;
        }
    }
}
