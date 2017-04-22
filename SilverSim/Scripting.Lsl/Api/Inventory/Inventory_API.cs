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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scene.Types.Transfer;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using SilverSim.Types.ServerURIs;
using SilverSim.Viewer.Messages.Inventory;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Inventory
{
    [ScriptApiName("Inventory")]
    [LSLImplementation]
    [Description("LSL/OSSL Inventory API")]
    public partial class InventoryApi : IScriptApi, IPlugin
    {
        List<IUserAgentServicePlugin> m_UserAgentServicePlugins;
        List<IAssetServicePlugin> m_AssetServicePlugins;
        List<IInventoryServicePlugin> m_InventoryServicePlugins;

        public InventoryApi()
        {
            /* intentionally left empty */
        }

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

        /* private constants */
        const int LINK_ROOT = 1;
        const int LINK_SET = -1;
        const int LINK_ALL_OTHERS = -2;
        const int LINK_ALL_CHILDREN = -3;
        const int LINK_THIS = -4;

        List<ObjectPart> DetermineLinks(ObjectPart thisPart, int link)
        {
            List<ObjectPart> res = new List<ObjectPart>();
            ObjectGroup grp = thisPart.ObjectGroup;
            switch(link)
            {
                case LINK_THIS:
                    res.Add(thisPart);
                    break;

                case LINK_ROOT:
                    res.Add(grp.RootPart);
                    break;

                case LINK_ALL_OTHERS:
                    foreach (ObjectPart p in grp.ValuesByKey1)
                    {
                        if (p != thisPart)
                        {
                            res.Add(p);
                        }
                    }
                    break;

                case LINK_ALL_CHILDREN:
                    foreach(ObjectPart p in grp.ValuesByKey1)
                    {
                        if (p != grp.RootPart)
                        {
                            res.Add(p);
                        }
                    }
                    break;

                default:
                    ObjectPart part;
                    if(grp.TryGetValue(link, out part))
                    {
                        res.Add(part);
                    }
                    break;
            }

            return res;
        }

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
                foreach (ObjectPart part in DetermineLinks(instance.Part, link))
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
                foreach (ObjectPart part in DetermineLinks(instance.Part, link))
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
                foreach (ObjectPart part in DetermineLinks(instance.Part, link))
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
                    foreach (ObjectPart part in DetermineLinks(instance.Part, link))
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
                    foreach (ObjectPart part in DetermineLinks(instance.Part, link))
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
                foreach (ObjectPart part in DetermineLinks(instance.Part, link))
                {
                    cnt += ((type == INVENTORY_ALL) ? part.Inventory.Count : part.Inventory.CountType((InventoryType)type));
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
                foreach (ObjectPart part in DetermineLinks(instance.Part, link))
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
                foreach (ObjectPart part in DetermineLinks(instance.Part, link))
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
                foreach (ObjectPart part in DetermineLinks(instance.Part, link))
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

                    DataserverEvent e = new DataserverEvent();
                    e.QueryID = UUID.Random;
                    e.Data = landmark.LocalPos.ToString();
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
                foreach(ObjectPart part in DetermineLinks(instance.Part, link))
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

        bool TryGetServices(UUI targetAgentId, out InventoryServiceInterface inventoryService, out AssetServiceInterface assetService)
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

            if (null == userAgentService)
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

            return null != inventoryService && null != assetService;
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
                UUI targetAgentId;
                AnArray array = new AnArray();
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
                UUI targetAgentId;
                AnArray array = new AnArray();
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

        void GiveInventoryToPrim(ScriptInstance instance, ObjectPart target, ObjectPart origin, AnArray inventoryitems, bool skipNoCopy)
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
                    /* TODO: implement object InventoryOffered message */
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
