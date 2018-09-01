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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scene.Types.Transfer;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using SilverSim.Viewer.Messages.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Scripting.Lsl.Api.Attachments
{
    public partial  class AttachmentsApi
    {
        [APILevel(APIFlags.OSSL, "osForceAttachToOtherAvatarFromInventory")]
        [CheckFunctionPermission]
        public void ForceAttachToOtherAvatarFromInventory(ScriptInstance instance, LSLKey destination, string item_name, int attach_point)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                IAgent targetAgent;
                UUID id = destination.AsUUID;

                if (scene.Agents.TryGetValue(id, out targetAgent))
                {
                    GiveInventoryToAgent(
                        instance,
                        targetAgent.Owner,
                        targetAgent.InventoryService,
                        targetAgent.AssetService,
                        scene,
                        instance.Part,
                        item_name);
                }
            }
        }

        private void GiveInventoryToAgent(
            ScriptInstance instance,
            UGUI agent,
            InventoryServiceInterface inventoryService, AssetServiceInterface assetService,
            SceneInterface scene,
            ObjectPart origin,
            string inventory)
        {
            InventoryItem givenItem;
            var assetIDs = new List<UUID>();

            ObjectPartInventoryItem sourceItem;
            if (!origin.Inventory.TryGetValue(inventory, out sourceItem))
            {
                instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0NotFound", "Inventory item '{0}' not found", inventory));
                return;
            }
            else if(sourceItem.AssetType != AssetType.Object)
            {
                /* ignore non-objects */
                return;
            }
            else
            {
                bool removeItem = false;
                if (!agent.EqualsGrid(origin.Owner) && sourceItem.CheckPermissions(origin.Owner, origin.Group, InventoryPermissionsMask.Transfer))
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0HasNoTransferPermission", "Inventory item '{0}' has no transfer permission.", inventory));
                    return;
                }

                if (!sourceItem.CheckPermissions(origin.Owner, origin.Group, InventoryPermissionsMask.Copy))
                {
                    removeItem = true;
                }

                if (removeItem)
                {
                    origin.Inventory.Remove(sourceItem.ID);
                }
                assetIDs.Add(sourceItem.AssetID);
                givenItem = new InventoryItem(sourceItem);
            }

            new InventoryTransferItem(
                instance.Part.ObjectGroup.Owner,
                instance.Part.ObjectGroup.Group,
                instance.Part.ObjectGroup.IsGroupOwned,
                instance.Part.Name,
                agent,
                inventoryService, assetService,
                scene,
                assetIDs,
                givenItem).QueueWorkItem();
        }

        private sealed class InventoryTransferItem : AssetTransferWorkItem
        {
            private readonly InventoryServiceInterface m_InventoryService;
            private readonly UGUI m_Owner;
            private readonly UGI m_Group;
            private readonly UGUI m_DestinationAgent;
            private readonly UUID m_SceneID;
            private readonly InventoryItem m_Item;
            private readonly SceneInterface.TryGetSceneDelegate TryGetScene;
            private readonly bool m_IsFromGroup;
            private readonly string m_ObjectName;

            public InventoryTransferItem(
                UGUI owner,
                UGI group,
                bool isFromGroup,
                string objectName,
                UGUI targetAgent,
                InventoryServiceInterface inventoryService,
                AssetServiceInterface assetService,
                SceneInterface scene,
                List<UUID> assetids,
                InventoryItem item)
                : base(assetService, scene.AssetService, assetids, ReferenceSource.Source)
            {
                m_ObjectName = objectName;
                m_Owner = owner;
                m_Group = group;
                m_IsFromGroup = isFromGroup;
                m_InventoryService = inventoryService;
                m_DestinationAgent = targetAgent;
                m_SceneID = scene.ID;
                m_Item = item;
                TryGetScene = scene.TryGetScene;
            }

            public override void AssetTransferComplete()
            {
                InventoryFolder folder;
                SceneInterface scene = null;
                IAgent agent = null;
                if (!TryGetScene(m_SceneID, out scene) ||
                    !scene.Agents.TryGetValue(m_DestinationAgent.ID, out agent))
                {
                    agent = null;
                }

                var selectedFolder = new Dictionary<AssetType, UUID>();
                UUID givenToFolderID = UUID.Zero;

                UUID folderID = UUID.Zero;
                foreach (AssetType assetType in new AssetType[] { m_Item.AssetType, AssetType.Object, AssetType.RootFolder })
                {
                    if (!selectedFolder.TryGetValue(assetType, out folderID))
                    {
                        if (m_InventoryService.Folder.TryGetValue(m_DestinationAgent.ID, assetType, out folder))
                        {
                            folderID = folder.ID;
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (UUID.Zero == folderID)
                {
                    return;
                }

                var item = new InventoryItem(m_Item);
                item.LastOwner = item.Owner;
                item.Owner = m_DestinationAgent;
                item.ParentFolderID = folderID;
                item.IsGroupOwned = false;
                m_InventoryService.Item.Add(item);
                if (agent != null)
                {
                    var msg = new UpdateCreateInventoryItem
                    {
                        AgentID = m_DestinationAgent.ID,
                        SimApproved = true
                    };
                    msg.AddItem(item, 0);
                    agent.SendMessageAlways(msg, m_SceneID);
                }

                /* attach */
                if(agent == null)
                {
                    return;
                }

                AssetData data;
                if (agent.Owner.EqualsGrid(m_DestinationAgent) &&
                    scene.AssetService.TryGetValue(m_Item.AssetID, out data))
                {
                    List<ObjectGroup> objgroups;
                    try
                    {
                        objgroups = ObjectXML.FromAsset(data, m_DestinationAgent);
                    }
                    catch (Exception e)
                    {
                        return;
                    }

                    if (objgroups.Count != 1)
                    {
                        return;
                    }

                    ObjectGroup grp = objgroups[0];

                    bool attachPointChanged = false;

                    foreach (var part in grp.Values)
                    {
                        if (part.Shape.PCode == PrimitiveCode.Grass ||
                            part.Shape.PCode == PrimitiveCode.Tree ||
                            part.Shape.PCode == PrimitiveCode.NewTree)
                        {
                            return;
                        }
                    }

                    AttachmentPoint attachAt = grp.AttachPoint;

                    if (attachAt == AttachmentPoint.NotAttached)
                    {
                        grp.AttachPoint = AttachmentPoint.LeftHand;
                        grp.AttachedPos = Vector3.Zero;
                    }

                    grp.Owner = m_DestinationAgent;
                    grp.FromItemID = item.ID;
                    grp.IsAttached = true;
                    grp.Position = grp.AttachedPos;
                    grp.IsChangedEnabled = true;

                    if (attachPointChanged)
                    {
                        grp.AttachPoint = attachAt;
                    }

                    try
                    {
                        scene.Add(grp);
                        agent.Attachments.Add(grp.AttachPoint, grp);
                    }
                    catch
                    {
                        return;
                    }
                    scene.RezScriptsForObject(grp);
                    grp.PostEvent(new OnRezEvent());
                    grp.PostEvent(new AttachEvent { ObjectID = grp.Owner.ID });
                }
            }

            public override void AssetTransferFailed(Exception e)
            {
                /* no activity here */
            }
        }
    }
}