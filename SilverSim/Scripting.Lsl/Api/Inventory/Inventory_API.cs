// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Transfer;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Messages.Inventory;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace SilverSim.Scripting.Lsl.Api.Inventory
{
    [ScriptApiName("Inventory")]
    [LSLImplementation]
    [Description("LSL/OSSL Inventory API")]
    public class InventoryApi : IScriptApi, IPlugin
    {
        public InventoryApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_ALL = -1;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_NONE = -1;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_TEXTURE = 0;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_SOUND = 1;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_LANDMARK = 3;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_CLOTHING = 5;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_OBJECT = 6;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_NOTECARD = 7;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_SCRIPT = 10;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_BODYPART = 13;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_ANIMATION = 20;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_GESTURE = 21;


        [APILevel(APIFlags.LSL)]
        public const int MASK_BASE = 0;
        [APILevel(APIFlags.LSL)]
        public const int MASK_OWNER = 1;
        [APILevel(APIFlags.LSL)]
        public const int MASK_GROUP = 2;
        [APILevel(APIFlags.LSL)]
        public const int MASK_EVERYONE = 3;
        [APILevel(APIFlags.LSL)]
        public const int MASK_NEXT = 4;

        [APILevel(APIFlags.LSL)]
        public const int PERM_TRANSFER = 8192;
        [APILevel(APIFlags.LSL)]
        public const int PERM_MODIFY = 16384;
        [APILevel(APIFlags.LSL)]
        public const int PERM_COPY = 32768;
        [APILevel(APIFlags.LSL)]
        public const int PERM_MOVE = 524288;
        [APILevel(APIFlags.LSL)]
        public const int PERM_ALL = 2147483647;

        [APILevel(APIFlags.LSL, "llRemoveInventory")]
        public void RemoveInventory(ScriptInstance instance, string item)
        {
            ObjectPartInventoryItem resitem;
            lock (instance)
            {
                if (instance.Part.Inventory.TryGetValue(item, out resitem))
                {
                    ScriptInstance si = resitem.ScriptInstance;

                    instance.Part.Inventory.Remove(resitem.ID);
                    if (si == instance)
                    {
                        throw new ScriptAbortException();
                    }
                    else if (si != null)
                    {
                        si = resitem.RemoveScriptInstance;
                        if (si != null)
                        {
                            si.Abort();
                            si.Remove();
                        }
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetInventoryCreator")]
        public LSLKey GetInventoryCreator(ScriptInstance instance, string item)
        {
            lock (instance)
            {
                try
                {
                    return instance.Part.Inventory[item].Creator.ID;
                }
                catch
                {
                    return UUID.Zero;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetInventoryKey")]
        public LSLKey GetInventoryKey(ScriptInstance instance, string item)
        {
            lock (instance)
            {
                try
                {
                    return instance.Part.Inventory[item].AssetID;
                }
                catch
                {
                    return UUID.Zero;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetInventoryName")]
        public string GetInventoryName(ScriptInstance instance, int type, int number)
        {
            lock(instance)
            {
                try
                {
                    if (type == INVENTORY_ALL)
                    {
                        return instance.Part.Inventory[(uint)number].Name;
                    }
                    else if (type >= 0)
                    {
                        return instance.Part.Inventory[(InventoryType)type, (uint)number].Name;
                    }
                }
                catch
                {
                    /* no action required */
                }
            }
            return string.Empty;
        }

        [APILevel(APIFlags.LSL, "llGetInventoryNumber")]
        public int GetInventoryNumber(ScriptInstance instance, int type)
        {
            lock (instance)
            {
                if (type == INVENTORY_ALL)
                {
                    return instance.Part.Inventory.Count;
                }
                return instance.Part.Inventory.CountType((InventoryType)type);
            }
        }

        [APILevel(APIFlags.LSL, "llSetInventoryPermMask")]
        public void SetInventoryPermMask(ScriptInstance instance, string name, int category, int mask)
        {
            lock (instance)
            {
                if (instance.Part.ObjectGroup.Scene.IsSimConsoleAllowed(instance.Part.Owner))
                {
                    try
                    {
                        ObjectPartInventoryItem item = instance.Part.Inventory[name];
                        switch (category)
                        {
                            case MASK_BASE:
                                item.Permissions.Base = (InventoryPermissionsMask)mask;
                                break;

                            case MASK_EVERYONE:
                                item.Permissions.EveryOne = (InventoryPermissionsMask)mask;
                                break;

                            case MASK_GROUP:
                                item.Permissions.Group = (InventoryPermissionsMask)mask;
                                break;

                            case MASK_NEXT:
                                item.Permissions.NextOwner = (InventoryPermissionsMask)mask;
                                break;

                            case MASK_OWNER:
                                item.Permissions.Current = (InventoryPermissionsMask)mask;
                                break;
                        }
                    }
                    catch
                    {
                        throw new ArgumentException(string.Format("Inventory item {0} does not exist", name));
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetInventoryPermMask")]
        public int GetInventoryPermMask(ScriptInstance instance, string name, int category)
        {
            lock(instance)
            {
                try
                {
                    ObjectPartInventoryItem item = instance.Part.Inventory[name];
                    InventoryPermissionsMask mask;
                    switch(category)
                    {
                        case MASK_BASE:
                            mask = item.Permissions.Base;
                            break;

                        case MASK_EVERYONE:
                            mask = item.Permissions.EveryOne;
                            break;

                        case MASK_GROUP:
                            mask = item.Permissions.Group;
                            break;

                        case MASK_NEXT:
                            mask = item.Permissions.NextOwner;
                            break;

                        case MASK_OWNER:
                            mask = item.Permissions.Current;
                            break;

                        default:
                            mask = InventoryPermissionsMask.None;
                            break;
                    }
                    return (int)(uint)mask;
                }
                catch
                {
                    throw new ArgumentException(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetInventoryType")]
        public int GetInventoryType(ScriptInstance instance, string name)
        {
            lock (instance)
            {
                try
                {
                    return (int)instance.Part.Inventory[name].InventoryType;
                }
                catch
                {
                    return -1;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llRequestInventoryData")]
        [ForcedSleep(1.0)]
        public LSLKey RequestInventoryData(ScriptInstance instance, string name)
        {
            throw new NotImplementedException("llRequestInventoryData(string)");
        }

        #region osGetInventoryDesc
        [APILevel(APIFlags.OSSL, "osGetInventoryDesc")]
        public string GetInventoryDesc(ScriptInstance instance, string item)
        {
            lock (instance)
            {
                try
                {
                    return instance.Part.Inventory[item].Description;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }
        #endregion

        #region Rez Inventory
        [APILevel(APIFlags.LSL, "llRezObject")]
        public void RezObject(ScriptInstance instance, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param)
        {
            throw new NotImplementedException("llRezObject(string, vector, vector, rotation, integer)");
        }

        [APILevel(APIFlags.LSL, "llRezAtRoot")]
        public void RezAtRoot(ScriptInstance instance, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param)
        {
            throw new NotImplementedException("llRezAtRoot(string, vector, vector, rotation, integer)");
        }
        #endregion

        #region Give Inventory
        [APILevel(APIFlags.LSL, "llGiveInventory")]
        public void GiveInventory(ScriptInstance instance, LSLKey destination, string inventory)
        {
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                ObjectPart targetPart;
                IAgent targetAgent;
                UUID id = destination.AsUUID;
                UUI targetAgentId;
                AnArray array = new AnArray();
                array.Add(inventory);

                if (scene.Primitives.TryGetValue(id, out targetPart))
                {
                    GiveInventoryToPrim(instance, targetPart, instance.Part, array, false);
                }
                else if(scene.Agents.TryGetValue(id, out targetAgent))
                {
                    GiveInventoryToAgent(
                        instance,
                        targetAgent.Owner,
                        targetAgent.InventoryService,
                        targetAgent.AssetService,
                        scene,
                        instance.Part,
                        string.Empty,
                        array,
                        false);
                }
                else if(scene.AvatarNameService.TryGetValue(id, out targetAgentId))
                {

                }
                else
                {
                    instance.ShoutError("Could not find destination");
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGiveInventoryList")]
        public void GiveInventoryList(ScriptInstance instance, LSLKey destination, string folder, AnArray inventory)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                ObjectPart targetPart;
                IAgent targetAgent;
                UUID id = destination.AsUUID;
                UUI targetAgentId;
                AnArray array = new AnArray();
                array.Add(inventory);

                if (scene.Primitives.TryGetValue(id, out targetPart))
                {
                    GiveInventoryToPrim(instance, targetPart, instance.Part, array, true);
                }
                else if (scene.Agents.TryGetValue(id, out targetAgent))
                {
                    GiveInventoryToAgent(
                        instance,
                        targetAgent.Owner,
                        targetAgent.InventoryService,
                        targetAgent.AssetService,
                        scene,
                        instance.Part,
                        folder,
                        array,
                        true);
                }
                else if (scene.AvatarNameService.TryGetValue(id, out targetAgentId))
                {

                }
                else
                {
                    instance.ShoutError("Could not find destination");
                }
            }
        }

        void GiveInventoryToPrim(ScriptInstance instance, ObjectPart target, ObjectPart origin, AnArray inventoryitems, bool skipNoCopy)
        {
            if(!target.CheckPermissions(origin.Owner, origin.Group, InventoryPermissionsMask.Modify))
            {
                instance.ShoutError("Blocked by permissions");
                return;
            }

            foreach(IValue iv in inventoryitems)
            {
                ObjectPartInventoryItem sourceItem;
                string inventory = iv.ToString();
                if(!origin.Inventory.TryGetValue(inventory, out sourceItem))
                {
                    instance.ShoutError("Inventory item " + inventory + " not found");
                }
                else
                {
                    bool removeItem = false;
                    if(!target.Owner.EqualsGrid(origin.Owner) && sourceItem.CheckPermissions(origin.Owner, origin.Group, InventoryPermissionsMask.Transfer))
                    {
                        instance.ShoutError("Inventory item " + inventory + " has no transfer permission.");
                        continue;
                    }

                    if (!sourceItem.CheckPermissions(origin.Owner, origin.Group, InventoryPermissionsMask.Copy))
                    {
                        removeItem = true;
                        if (skipNoCopy)
                        {
                            instance.ShoutError("Inventory item " + inventory + " has no copy permission.");
                            continue;
                        }
                    }

                    if(removeItem)
                    {
                        ScriptInstance oldInstance = sourceItem.RemoveScriptInstance;
                        if(null != oldInstance)
                        {
                            oldInstance.Abort();
                        }

                        origin.Inventory.Remove(sourceItem.ID);
                        sourceItem.ID = UUID.Random;
                        /* reset script if set */
                        sourceItem.ScriptState = null;
                        target.Inventory.Add(sourceItem);
                    }
                    else
                    {
                        /* duplicate item */
                        ObjectPartInventoryItem newItem = new ObjectPartInventoryItem(sourceItem);
                        newItem.ID = UUID.Random;
                        target.Inventory.Add(newItem);
                    }
                }
            }
        }

        void GiveInventoryToAgent(
            ScriptInstance instance,
            UUI agent,
            InventoryServiceInterface inventoryService, AssetServiceInterface assetService,
            SceneInterface scene,
            ObjectPart origin,
            string folderName, AnArray inventoryitems, bool createFolderAndSkipNoCopy)
        {
            List<InventoryItem> givenItems = new List<InventoryItem>();
            List<UUID> assetIDs = new List<UUID>();

            foreach (IValue iv in inventoryitems)
            {
                ObjectPartInventoryItem sourceItem;
                string inventory = iv.ToString();
                if (!origin.Inventory.TryGetValue(inventory, out sourceItem))
                {
                    instance.ShoutError("Inventory item " + inventory + " not found");
                }
                else
                {
                    bool removeItem = false;
                    if (!agent.EqualsGrid(origin.Owner) && sourceItem.CheckPermissions(origin.Owner, origin.Group, InventoryPermissionsMask.Transfer))
                    {
                        instance.ShoutError("Inventory item " + inventory + " has no transfer permission.");
                        continue;
                    }

                    if (!sourceItem.CheckPermissions(origin.Owner, origin.Group, InventoryPermissionsMask.Copy))
                    {
                        removeItem = true;
                        if (createFolderAndSkipNoCopy)
                        {
                            instance.ShoutError("Inventory item " + inventory + " has no copy permission.");
                            continue;
                        }
                    }

                    if (removeItem)
                    {
                        origin.Inventory.Remove(sourceItem.ID);
                    }
                    assetIDs.Add(sourceItem.AssetID);
                    givenItems.Add(new InventoryItem(sourceItem));
                }
            }

            new InventoryTransferItem(
                agent,
                inventoryService, assetService,
                scene,
                assetIDs,
                givenItems,
                folderName,
                createFolderAndSkipNoCopy).QueueWorkItem();
        }

        sealed class InventoryTransferItem : AssetTransferWorkItem
        {
            readonly InventoryServiceInterface m_InventoryService;
            readonly UUI m_DestinationAgent;
            readonly UUID m_SceneID;
            readonly List<InventoryItem> m_Items;
            readonly string m_DestinationFolder = string.Empty;
            readonly SceneInterface.TryGetSceneDelegate TryGetScene;
            readonly bool m_CreateFolder;

            public InventoryTransferItem(
                UUI targetAgent,
                InventoryServiceInterface inventoryService,
                AssetServiceInterface assetService,
                SceneInterface scene,
                List<UUID> assetids,
                List<InventoryItem> items,
                string destinationFolder,
                bool createFolder)
                : base(assetService, scene.AssetService, assetids, ReferenceSource.Source)
            {
                m_InventoryService = inventoryService;
                m_DestinationAgent = targetAgent;
                m_SceneID = scene.ID;
                m_Items = items;
                m_DestinationFolder = destinationFolder;
                TryGetScene = scene.TryGetScene;
                m_CreateFolder = createFolder;
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

                Dictionary<AssetType, UUID> selectedFolder = new Dictionary<AssetType, UUID>();

                if (m_CreateFolder)
                {
                    if (!m_InventoryService.Folder.TryGetValue(m_DestinationAgent.ID, AssetType.RootFolder, out folder))
                    {
                        return;
                    }
                    UUID rootFolderID = folder.ID;
                    folder = new InventoryFolder();
                    folder.Owner = m_DestinationAgent;
                    folder.ParentFolderID = rootFolderID;
                    folder.InventoryType = InventoryType.Unknown;
                    folder.Version = 1;
                    folder.Name = m_DestinationFolder;
                    folder.ID = UUID.Random;
                    m_InventoryService.Folder.Add(folder);

                    if (agent != null)
                    {
                        BulkUpdateInventory msg = new BulkUpdateInventory();
                        msg.AgentID = m_DestinationAgent.ID;
                        msg.TransactionID = UUID.Zero;
                        msg.AddInventoryFolder(folder);
                        agent.SendMessageAlways(msg, m_SceneID);
                    }

                    foreach (AssetType type in typeof(AssetType).GetEnumValues())
                    {
                        selectedFolder.Add(type, folder.ID);
                    }
                }

                foreach (InventoryItem sellItem in m_Items)
                {
                    UUID folderID = UUID.Zero;
                    AssetType[] assetTypes = new AssetType[] { sellItem.AssetType, AssetType.Object, AssetType.RootFolder };
                    foreach (AssetType assetType in assetTypes)
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

                    if(UUID.Zero == folderID)
                    {
                        continue;
                    }

                    InventoryItem item = new InventoryItem(sellItem);
                    item.LastOwner = item.Owner;
                    item.Owner = m_DestinationAgent;
                    item.ParentFolderID = folderID;
                    item.IsGroupOwned = false;
                    m_InventoryService.Item.Add(item);
                    if (null != agent)
                    {
                        UpdateCreateInventoryItem msg = new UpdateCreateInventoryItem();
                        msg.AgentID = m_DestinationAgent.ID;
                        msg.AddItem(item, 0);
                        msg.SimApproved = true;
                        agent.SendMessageAlways(msg, m_SceneID);
                    }
                }
            }

            public override void AssetTransferFailed(Exception e)
            {
                SceneInterface scene;
                IAgent agent;
                if (!TryGetScene(m_DestinationAgent.ID, out scene) &&
                    scene.Agents.TryGetValue(m_DestinationAgent.ID, out agent))
                {

                }
            }
        }
        #endregion
    }
}
