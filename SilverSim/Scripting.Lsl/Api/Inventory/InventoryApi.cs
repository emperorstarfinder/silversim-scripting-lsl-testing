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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scene.Types.Transfer;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.IM;
using SilverSim.Types.Inventory;
using SilverSim.Types.ServerURIs;
using SilverSim.Viewer.Messages.Inventory;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using SoundAsset = SilverSim.Types.Asset.Format.Sound;
using AnimationAsset = SilverSim.Types.Asset.Format.Animation;

namespace SilverSim.Scripting.Lsl.Api.Inventory
{
    [ScriptApiName("Inventory")]
    [LSLImplementation]
    [Description("LSL/OSSL Inventory API")]
    public partial class InventoryApi : IScriptApi, IPlugin
    {
        private List<IUserAgentServicePlugin> m_UserAgentServicePlugins;
        private List<IAssetServicePlugin> m_AssetServicePlugins;
        private List<IInventoryServicePlugin> m_InventoryServicePlugins;

        public void Startup(ConfigurationLoader loader)
        {
            m_UserAgentServicePlugins = loader.GetServicesByValue<IUserAgentServicePlugin>();
            m_AssetServicePlugins = loader.GetServicesByValue<IAssetServicePlugin>();
            m_InventoryServicePlugins = loader.GetServicesByValue<IInventoryServicePlugin>();
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

        [APILevel(APIFlags.ASSL, "asRemoveLinkInventory")]
        [APIExtension(APIExtension.InWorldz, "iwRemoveLinkInventory")]
        public void RemoveInventory(ScriptInstance instance, int link, string item)
        {
            lock (instance)
            {
                foreach (ObjectPart part in instance.GetLinkTargets(link))
                {
                    ObjectPartInventoryItem resitem;
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

        [APILevel(APIFlags.ASSL, "asGetLinkInventoryCreator")]
        [APIExtension(APIExtension.InWorldz, "iwGetLinkInventoryCreator")]
        public LSLKey GetInventoryCreator(ScriptInstance instance, int link, string name)
        {
            lock(instance)
            {
                ObjectPartInventoryItem item;
                foreach (ObjectPart part in instance.GetLinkTargets(link))
                {
                    if(part.Inventory.TryGetValue(name, out item))
                    {
                        return item.Creator.ID;
                    }
                }
                return UUID.Zero;
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

        [APILevel(APIFlags.ASSL, "asGetLinkInventoryKey")]
        [APIExtension(APIExtension.InWorldz, "iwGetLinkInventoryKey")]
        public LSLKey GetInventoryKey(ScriptInstance instance, int link, string name)
        {
            lock (instance)
            {
                ObjectPartInventoryItem item;
                foreach (ObjectPart part in instance.GetLinkTargets(link))
                {
                    if (part.Inventory.TryGetValue(name, out item))
                    {
                        return item.AssetID;
                    }
                }
            }
            return UUID.Zero;
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

        [APILevel(APIFlags.ASSL, "asGetLinkInventoryName")]
        [APIExtension(APIExtension.InWorldz, "iwGetLinkInventoryName")]
        public string GetInventoryName(ScriptInstance instance, int link, int type, int number)
        {
            lock (instance)
            {
                if (type == INVENTORY_ALL)
                {
                    foreach (ObjectPart part in instance.GetLinkTargets(link))
                    {
                        int cnt = part.Inventory.Count;
                        if (number < cnt)
                        {
                            try
                            {
                                return part.Inventory[(uint)number].Name;
                            }
                            catch
                            {
                                /* no action required */
                            }
                        }
                        number -= cnt;
                        if (number < 0)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    foreach (ObjectPart part in instance.GetLinkTargets(link))
                    {
                        int cnt = part.Inventory.CountType((InventoryType)type);
                        if (number < cnt)
                        {
                            try
                            {
                                return part.Inventory[(InventoryType)type, (uint)number].Name;
                            }
                            catch
                            {
                                /* no action required */
                            }
                        }
                        number -= cnt;
                        if (number < 0)
                        {
                            break;
                        }
                    }
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

        [APILevel(APIFlags.ASSL, "asGetLinkInventoryNumber")]
        [APIExtension(APIExtension.InWorldz, "iwGetLinkInventoryNumber")]
        public int GetInventoryNumber(ScriptInstance instance, int link, int type)
        {
            lock (instance)
            {
                int cnt = 0;
                foreach (ObjectPart part in instance.GetLinkTargets(link))
                {
                    cnt += (type == INVENTORY_ALL) ? part.Inventory.Count : part.Inventory.CountType((InventoryType)type);
                }
                return cnt;
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

                            default:
                                break;
                        }
                    }
                    catch
                    {
                        throw new LocalizedScriptErrorException(this, "InventoryItem0NotFound", "Inventory item {} not found", name);
                    }
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asSetLinkInventoryPermMask")]
        public void SetInventoryPermMask(ScriptInstance instance, int link, string name, int category, int mask)
        {
            if (instance.Part.ObjectGroup.Scene.IsSimConsoleAllowed(instance.Part.Owner))
            {
                foreach (ObjectPart part in instance.GetLinkTargets(link))
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

                            default:
                                break;
                        }
                        return;
                    }
                    catch
                    {
                        throw new LocalizedScriptErrorException(this, "InventoryItem0NotFound", "Inventory item {} not found", name);
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
                    throw new LocalizedScriptErrorException(this, "InventoryItem0NotFound", "Inventory item {} not found", name);
                }
            }
        }

        [APILevel(APIFlags.ASSL, "asGetLinkInventoryPermMask")]
        [APIExtension(APIExtension.InWorldz, "iwGetLinkInventoryPermMask")]
        public int GetInventoryPermMask(ScriptInstance instance, int link, string name, int category)
        {
            lock (instance)
            {
                foreach (ObjectPart part in instance.GetLinkTargets(link))
                {
                    try
                    {
                        ObjectPartInventoryItem item = instance.Part.Inventory[name];
                        InventoryPermissionsMask mask;
                        switch (category)
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
                        /* no action required */
                    }
                }
            }
            throw new LocalizedScriptErrorException(this, "InventoryItem0NotFound", "Inventory item {} not found", name);
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

        [APILevel(APIFlags.ASSL, "asGetLinkInventoryType")]
        [APIExtension(APIExtension.InWorldz, "iwGetLinkInventoryType")]
        public int GetInventoryType(ScriptInstance instance, int link, string name)
        {
            lock (instance)
            {
                foreach (ObjectPart part in instance.GetLinkTargets(link))
                {
                    ObjectPartInventoryItem item;
                    if(part.Inventory.TryGetValue(name, out item))
                    {
                        return (int)instance.Part.Inventory[name].InventoryType;
                    }
                }
                return -1;
            }
        }

        [APILevel(APIFlags.LSL, "llRequestInventoryData")]
        [ForcedSleep(1.0)]
        public LSLKey RequestInventoryData(ScriptInstance instance, string name)
        {
            lock(instance)
            {
                ObjectPartInventoryItem item;
                try
                {
                    item = instance.Part.Inventory[name];
                }
                catch
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0IsMissingInObjectsInventory", "Inventory item '{0}' is missing in object's inventory.", name));
                    return UUID.Zero;
                }

                if(item.AssetType == AssetType.Landmark)
                {
                    AssetData data;
                    Landmark landmark;
                    try
                    {
                        data = instance.Part.ObjectGroup.AssetService[item.AssetID];
                        landmark = new Landmark(data);
                    }
                    catch
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "LandmarkDataFor0NotFoundOrInvalid", "Landmark data for '{0}' not found or invalid", name));
                        return UUID.Zero;
                    }

                    var e = new DataserverEvent
                    {
                        QueryID = UUID.Random,
                        Data = landmark.LocalPos.ToString()
                    };
                    instance.PostEvent(e);
                    return e.QueryID;
                }
                else if(item.AssetType == AssetType.Animation)
                {
                    AssetData data;
                    AnimationAsset anim;
                    try
                    {
                        data = instance.Part.ObjectGroup.AssetService[item.AssetID];
                        anim = new AnimationAsset(data);
                    }
                    catch
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "AnimationDataFor0NotFoundOrInvalid", "Animation data for '{0}' not found or invalid", name));
                        return UUID.Zero;
                    }

                    var e = new DataserverEvent
                    {
                        QueryID = UUID.Random,
                        Data = anim.Duration.ToString()
                    };
                    instance.PostEvent(e);
                    return e.QueryID;
                }
                else if(item.AssetType == AssetType.Sound)
                {
                    AssetData data;
                    SoundAsset sound;
                    try
                    {
                        data = instance.Part.ObjectGroup.AssetService[item.AssetID];
                        sound = new SoundAsset(data);
                    }
                    catch
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "SoundDataFor0NotFoundOrInvalid", "Sound data for '{0}' not found or invalid", name));
                        return UUID.Zero;
                    }

                    var e = new DataserverEvent
                    {
                        QueryID = UUID.Random,
                        Data = sound.Duration.ToString()
                    };
                    instance.PostEvent(e);
                    return e.QueryID;
                }
                else
                {
                    return UUID.Zero;
                }
            }
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

        [APILevel(APIFlags.ASSL, "asGetLinkInventoryDesc")]
        [APIExtension(APIExtension.InWorldz, "iwGetLinkInventoryDesc")]
        public string GetInventoryDesc(ScriptInstance instance, int link, string name)
        {
            lock(instance)
            {
                foreach(ObjectPart part in instance.GetLinkTargets(link))
                {
                    ObjectPartInventoryItem item;
                    if(part.Inventory.TryGetValue(name, out item))
                    {
                        return item.Description;
                    }
                }
                return string.Empty;
            }
        }
        #endregion

        private bool TryGetServices(UGUI targetAgentId, out InventoryServiceInterface inventoryService, out AssetServiceInterface assetService)
        {
            inventoryService = null;
            assetService = null;
            UserAgentServiceInterface userAgentService = null;
            if (targetAgentId.HomeURI == null)
            {
                return false;
            }
            string homeUri = targetAgentId.HomeURI.ToString();
            Dictionary<string, string> heloheaders = ServicePluginHelo.HeloRequest(homeUri);
            foreach (IUserAgentServicePlugin userAgentPlugin in m_UserAgentServicePlugins)
            {
                if (userAgentPlugin.IsProtocolSupported(homeUri, heloheaders))
                {
                    userAgentService = userAgentPlugin.Instantiate(homeUri);
                }
            }

            if (userAgentService == null)
            {
                return false;
            }

            ServerURIs serverurls = userAgentService.GetServerURLs(targetAgentId);
            string inventoryServerURI = serverurls.InventoryServerURI;
            string assetServerURI = serverurls.AssetServerURI;

            heloheaders = ServicePluginHelo.HeloRequest(inventoryServerURI);
            foreach (IInventoryServicePlugin inventoryPlugin in m_InventoryServicePlugins)
            {
                if (inventoryPlugin.IsProtocolSupported(inventoryServerURI, heloheaders))
                {
                    inventoryService = inventoryPlugin.Instantiate(inventoryServerURI);
                    break;
                }
            }

            heloheaders = ServicePluginHelo.HeloRequest(assetServerURI);
            foreach (IAssetServicePlugin assetPlugin in m_AssetServicePlugins)
            {
                if (assetPlugin.IsProtocolSupported(assetServerURI, heloheaders))
                {
                    assetService = assetPlugin.Instantiate(assetServerURI);
                    break;
                }
            }

            return inventoryService != null && assetService != null;
        }

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
                UGUI targetAgentId;
                var array = new AnArray();
                InventoryServiceInterface inventoryService;
                AssetServiceInterface assetService;
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
                else if(scene.AvatarNameService.TryGetValue(id, out targetAgentId) &&
                    TryGetServices(targetAgentId, out inventoryService, out assetService))
                {
                    GiveInventoryToAgent(
                        instance,
                        targetAgentId,
                        inventoryService,
                        assetService,
                        scene,
                        instance.Part,
                        string.Empty,
                        array,
                        false);
                }
                else
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "CouldNotFindDestination", "Could not find destination"));
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
                UGUI targetAgentId;
                var array = new AnArray();
                InventoryServiceInterface inventoryService;
                AssetServiceInterface assetService;
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
                else if (scene.AvatarNameService.TryGetValue(id, out targetAgentId) &&
                    TryGetServices(targetAgentId, out inventoryService, out assetService))
                {
                    GiveInventoryToAgent(
                        instance,
                        targetAgentId,
                        inventoryService,
                        assetService,
                        scene,
                        instance.Part,
                        folder,
                        array,
                        true);
                }
                else
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "CouldNotFindDestination", "Could not find destination"));
                }
            }
        }

        private void GiveInventoryToPrim(ScriptInstance instance, ObjectPart target, ObjectPart origin, AnArray inventoryitems, bool skipNoCopy)
        {
            if(!target.CheckPermissions(origin.Owner, origin.Group, InventoryPermissionsMask.Modify))
            {
                instance.ShoutError(new LocalizedScriptMessage(this, "BlockedByPermission", "Blocked by permissions"));
                return;
            }

            foreach(IValue iv in inventoryitems)
            {
                ObjectPartInventoryItem sourceItem;
                string inventory = iv.ToString();
                if(!origin.Inventory.TryGetValue(inventory, out sourceItem))
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0NotFound", "Inventory item '{0}' not found", inventory));
                }
                else
                {
                    bool removeItem = false;
                    if(!target.Owner.EqualsGrid(origin.Owner) && sourceItem.CheckPermissions(origin.Owner, origin.Group, InventoryPermissionsMask.Transfer))
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0HasNoTransferPermission", "Inventory item '{0}' has no transfer permission.", inventory));
                        continue;
                    }

                    if (!sourceItem.CheckPermissions(origin.Owner, origin.Group, InventoryPermissionsMask.Copy))
                    {
                        removeItem = true;
                        if (skipNoCopy)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0HasNoCopyPermission", "Inventory item '{0}' has no copy permission.", inventory));
                            continue;
                        }
                    }

                    if(removeItem)
                    {
                        ScriptInstance oldInstance = sourceItem.RemoveScriptInstance;
                        oldInstance?.Abort();

                        origin.Inventory.Remove(sourceItem.ID);
                        sourceItem.SetNewID(UUID.Random);
                        /* reset script if set */
                        sourceItem.ScriptState = null;
                        target.Inventory.Add(sourceItem);
                    }
                    else
                    {
                        /* duplicate item */
                        var newItem = new ObjectPartInventoryItem(UUID.Random, sourceItem);
                        target.Inventory.Add(newItem);
                    }
                }
            }
        }

        private void GiveInventoryToAgent(
            ScriptInstance instance,
            UGUI agent,
            InventoryServiceInterface inventoryService, AssetServiceInterface assetService,
            SceneInterface scene,
            ObjectPart origin,
            string folderName, AnArray inventoryitems, bool createFolderAndSkipNoCopy)
        {
            var givenItems = new List<InventoryItem>();
            var assetIDs = new List<UUID>();

            foreach (IValue iv in inventoryitems)
            {
                ObjectPartInventoryItem sourceItem;
                string inventory = iv.ToString();
                if (!origin.Inventory.TryGetValue(inventory, out sourceItem))
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0NotFound", "Inventory item '{0}' not found", inventory));
                }
                else
                {
                    bool removeItem = false;
                    if (!agent.EqualsGrid(origin.Owner) && sourceItem.CheckPermissions(origin.Owner, origin.Group, InventoryPermissionsMask.Transfer))
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0HasNoTransferPermission", "Inventory item '{0}' has no transfer permission.", inventory));
                        continue;
                    }

                    if (!sourceItem.CheckPermissions(origin.Owner, origin.Group, InventoryPermissionsMask.Copy))
                    {
                        removeItem = true;
                        if (createFolderAndSkipNoCopy)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0HasNoCopyPermission", "Inventory item '{0}' has no copy permission.", inventory));
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
                instance.Part.ObjectGroup.Owner,
                instance.Part.ObjectGroup.Group,
                instance.Part.ObjectGroup.IsGroupOwned,
                instance.Part.Name,
                agent,
                inventoryService, assetService,
                scene,
                assetIDs,
                givenItems,
                folderName,
                createFolderAndSkipNoCopy).QueueWorkItem();
        }

        private sealed class InventoryTransferItem : AssetTransferWorkItem
        {
            private readonly InventoryServiceInterface m_InventoryService;
            private readonly UGUI m_Owner;
            private readonly UGI m_Group;
            private readonly UGUI m_DestinationAgent;
            private readonly UUID m_SceneID;
            private readonly List<InventoryItem> m_Items;
            private readonly string m_DestinationFolder = string.Empty;
            private readonly SceneInterface.TryGetSceneDelegate TryGetScene;
            private readonly bool m_CreateFolder;
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
                List<InventoryItem> items,
                string destinationFolder,
                bool createFolder)
                : base(assetService, scene.AssetService, assetids, ReferenceSource.Source)
            {
                m_ObjectName = objectName;
                m_Owner = owner;
                m_Group = group;
                m_IsFromGroup = isFromGroup;
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

                var selectedFolder = new Dictionary<AssetType, UUID>();
                UUID givenToFolderID = UUID.Zero;

                if (m_CreateFolder)
                {
                    if (!m_InventoryService.Folder.TryGetValue(m_DestinationAgent.ID, AssetType.RootFolder, out folder))
                    {
                        return;
                    }
                    UUID rootFolderID = folder.ID;
                    folder = new InventoryFolder
                    {
                        Owner = m_DestinationAgent,
                        ParentFolderID = rootFolderID,
                        DefaultType = AssetType.Unknown,
                        Version = 1,
                        Name = m_DestinationFolder,
                        ID = UUID.Random
                    };
                    m_InventoryService.Folder.Add(folder);
                    givenToFolderID = folder.ID;

                    if (agent != null)
                    {
                        var msg = new BulkUpdateInventory
                        {
                            AgentID = m_DestinationAgent.ID,
                            TransactionID = UUID.Zero
                        };
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
                    foreach (AssetType assetType in new AssetType[] { sellItem.AssetType, AssetType.Object, AssetType.RootFolder })
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

                    var item = new InventoryItem(sellItem);
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

                    if (!m_CreateFolder)
                    {
                        var binbucket = new byte[17];
                        binbucket[0] = (byte)item.InventoryType;
                        item.ID.ToBytes(binbucket, 1);
                        IMServiceInterface imservice = scene.GetService<IMServiceInterface>();
                        imservice?.Send(new GridInstantMessage
                        {
                            FromGroup = m_Group,
                            FromAgent = scene.AvatarNameService.ResolveName(m_Owner),
                            Message = string.Format(this.GetLanguageString(agent.CurrentCulture, "Object0HasGivenYouAnItem1", "Object {0} has given you an item \"{1}\"."),
                                m_ObjectName, item.Name),
                            IsFromGroup = m_IsFromGroup,
                            RegionID = scene.ID,
                            BinaryBucket = binbucket,
                            IMSessionID = item.ID,
                            Dialog = GridInstantMessageDialog.TaskInventoryOffered,
                            OnResult = (GridInstantMessage imret, bool success) => { }
                        });
                    }
                }

                if(m_CreateFolder)
                {
                    var binbucket = new byte[17];
                    binbucket[0] = (byte)InventoryType.Folder;
                    givenToFolderID.ToBytes(binbucket, 1);
                    IMServiceInterface imservice = scene.GetService<IMServiceInterface>();
                    imservice?.Send(new GridInstantMessage
                    {
                        FromGroup = m_Group,
                        FromAgent = scene.AvatarNameService.ResolveName(m_Owner),
                        Message = string.Format(this.GetLanguageString(agent.CurrentCulture, "Object0HasGivenYouAFolder1", "Object {0} has given you a folder \"{1}\"."),
                            m_ObjectName, m_DestinationFolder),
                        IsFromGroup = false,
                        RegionID = scene.ID,
                        BinaryBucket = binbucket,
                        IMSessionID = givenToFolderID,
                        Dialog = GridInstantMessageDialog.TaskInventoryOffered,
                        OnResult = (GridInstantMessage imret, bool success) => { }
                    });
                }
            }

            public override void AssetTransferFailed(Exception e)
            {
                SceneInterface scene;
                IAgent agent;
                if (TryGetScene(m_DestinationAgent.ID, out scene) &&
                    scene.Agents.TryGetValue(m_DestinationAgent.ID, out agent))
                {

                }
            }
        }
        #endregion
    }
}
