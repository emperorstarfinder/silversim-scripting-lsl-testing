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
using SilverSim.Scene.Types.Script;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Agents.Properties
{
    [ScriptApiName("AgentInventoryProperties")]
    [Description("Agent Inventory Properties API")]
    [LSLImplementation]
    public class AgentInventoryApi : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APIExtension(APIExtension.AgentInventory, "agentid")]
        [APIDisplayName("agentid")]
        [APIIsVariableType]
        [APIAccessibleMembers("Key", "FirstName", "LastName", "HomeUri", "FullName")]
        public class AgentId
        {
            private readonly UUI m_Uui;

            public AgentId()
            {
                m_Uui = UUI.Unknown;
            }

            public AgentId(UUI uui)
            {
                m_Uui = uui;
            }

            public LSLKey Key => m_Uui.ID;
            public string FirstName => m_Uui.FirstName;
            public string LastName => m_Uui.LastName;
            public string HomeUri => m_Uui.HomeURI != null ? m_Uui.HomeURI.ToString() : string.Empty;
            public string FullName => m_Uui.FullName;
            [APIExtension(APIExtension.AgentInventory, "agentid")]
            public static implicit operator bool(AgentId id) => id.m_Uui.ID != UUID.Zero;
            [APIExtension(APIExtension.AgentInventory, "agentid")]
            public static implicit operator int(AgentId id) => ((bool)id).ToLSLBoolean();
        }

        [APIExtension(APIExtension.AgentInventory, "agentinventoryitem")]
        [APIDisplayName("agentinventoryitem")]
        [APIIsVariableType]
        [ImplementsCustomTypecasts]
        [APIAccessibleMembers(
            "Key",
            "Name",
            "Description",
            "InventoryType",
            "AssetType",
            "AssetID",
            "CreationDate",
            "Flags",
            "LastOwner",
            "Owner",
            "Creator",
            "CurrentPermissions",
            "NextOwnerPermissions",
            "BasePermissions",
            "EveryOnePermissions",
            "GroupPermissions",
            "SaleType",
            "SalePrice")]
        public class AgentInventoryItem
        {
            private readonly ScriptInstance m_Instance;
            public UUI InventoryOwner { get; }
            public UUID Key { get; }
            public InventoryServiceInterface InventoryService { get; }
            public bool IsLibrary { get; }

            public AgentInventoryItem()
            {
                InventoryOwner = UUI.Unknown;
                Key = UUID.Zero;
            }

            public AgentInventoryItem(ScriptInstance instance, InventoryServiceInterface inventoryService, UUI owner, UUID itemID, bool isLibrary)
            {
                m_Instance = instance;
                InventoryService = inventoryService;
                InventoryOwner = owner;
                Key = itemID;
                IsLibrary = isLibrary;
            }

            private T With<T>(Func<InventoryItem, T> getter, T defvalue)
            {
                InventoryItem item;
                if (m_Instance == null)
                {
                    return defvalue;
                }
                lock (m_Instance)
                {
                    if (InventoryService.Item.TryGetValue(InventoryOwner.ID, Key, out item))
                    {
                        return getter(item);
                    }
                }
                return defvalue;
            }

            private void With<T>(Action<InventoryItem, T> setter, T value)
            {
                InventoryItem item;
                if (m_Instance == null)
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "agentinventoryitem");
                }
                if (IsLibrary)
                {
                    return;
                }
                lock (m_Instance)
                {
                    if (InventoryService.Item.TryGetValue(InventoryOwner.ID, Key, out item))
                    {
                        setter(item, value);
                        InventoryService.Item.Update(item);
                    }
                }
            }

            public string Name
            {
                get { return With((item) => item.Name, string.Empty); }
                set { With((item, v) => item.Name = v, value); }
            }

            public string Description
            {
                get { return With((item) => item.Description, string.Empty); }
                set { With((item, v) => item.Description = v, value); }
            }

            public int InventoryType => (int)With((item) => item.InventoryType, Types.Inventory.InventoryType.Unknown);

            public int AssetType => (int)With((item) => item.AssetType, Types.Asset.AssetType.Unknown);

            public LSLKey AssetID
            {
                get { return With((item) => item.AssetID, UUID.Zero); }
                set { With((item, id) => item.AssetID = id, value.AsUUID); }
            }

            public long CreationDate => With((item) => item.CreationDate.AsLong, 0);

            public int Flags
            {
                get { return With((item) => (int)item.Flags, 0); }
                set { With((item, f) => item.Flags = (InventoryFlags)f, value); }
            }

            public AgentId LastOwner => With((item) => new AgentId(item.LastOwner), new AgentId());

            public AgentId Owner => With((item) => new AgentId(item.Owner), new AgentId());

            public AgentId Creator => With((item) => new AgentId(item.Creator), new AgentId());

            public int CurrentPermissions => With((item) => (int)item.Permissions.Current, 0);

            public int NextOwnerPermissions => With((item) => (int)item.Permissions.NextOwner, 0);

            public int BasePermissions
            {
                get { return With((item) => (int)item.Permissions.Base, 0); }
                set { With((item, v) => item.Permissions.NextOwner = item.Permissions.Base & v, (InventoryPermissionsMask)value); }
            }

            public int EveryOnePermissions => With((item) => (int)item.Permissions.EveryOne, 0);

            public int GroupPermissions => With((item) => (int)item.Permissions.Group, 0);

            public int SaleType => With((item) => (int)item.SaleInfo.Type, 0);

            public int SalePrice => With((item) => item.SaleInfo.Price, 0);

            public AgentInventoryFolder ParentFolder => With((item) => new AgentInventoryFolder(m_Instance, InventoryService, InventoryOwner, item.ParentFolderID, IsLibrary), new AgentInventoryFolder());

            [APIExtension(APIExtension.AgentInventory, "agentid")]
            public static implicit operator bool(AgentInventoryItem item) => item.With((InventoryItem i) => true, false);
            [APIExtension(APIExtension.AgentInventory, "agentid")]
            public static implicit operator int(AgentInventoryItem item) => ((bool)item).ToLSLBoolean();
        }

        [APIExtension(APIExtension.AgentInventory, "agentinventorychildfolderaccessor")]
        [APIDisplayName("agentinventorychildfolderaccessor")]
        public class AgentInventoryChildFolderAccessor
        {
            private readonly ScriptInstance m_Instance;
            private readonly UUI m_InventoryOwner;
            private readonly InventoryServiceInterface m_InventoryService;
            private readonly UUID m_FolderID;
            private readonly bool m_IsLibrary;

            public AgentInventoryChildFolderAccessor(ScriptInstance instance, InventoryServiceInterface inventoryService, UUI owner, UUID folderID, bool isLibrary)
            {
                m_Instance = instance;
                m_InventoryOwner = owner;
                m_InventoryService = inventoryService;
                m_FolderID = folderID;
                m_IsLibrary = isLibrary;
            }

            [APIExtension(APIExtension.AgentInventory)]
            public static explicit operator AnArray(AgentInventoryChildFolderAccessor acc)
            {
                AnArray res = new AnArray();
                if (acc.m_Instance == null || acc.m_InventoryService == null)
                {
                    return res;
                }
                foreach (InventoryFolder folder in acc.m_InventoryService.Folder.GetFolders(acc.m_InventoryOwner.ID, acc.m_FolderID))
                {
                    res.Add(new LSLKey(folder.ID));
                }
                return res;
            }

            public AgentInventoryFolder this[string name, int index]
            {
                get
                {
                    if (m_Instance == null || m_InventoryService == null)
                    {
                        return new AgentInventoryFolder();
                    }

                    lock (m_Instance)
                    {
                        int idx = index;
                        foreach(InventoryFolder folder in m_InventoryService.Folder.GetFolders(m_InventoryOwner.ID, m_FolderID))
                        {
                            if(string.Compare(folder.Name, name, true) == 0 && idx-- == 0)
                            {
                                return new AgentInventoryFolder(m_Instance, m_InventoryService, m_InventoryOwner, folder.ID, m_IsLibrary);
                            }
                        }
                    }

                    return new AgentInventoryFolder();
                }
            }

            public AgentInventoryFolder this[string name] => this[name, 0];
        }

        [APIExtension(APIExtension.AgentInventory, "agentinventorychilditemaccessor")]
        [APIDisplayName("agentinventorychilditemaccessor")]
        [ImplementsCustomTypecasts]
        public class AgentInventoryChildItemAccessor
        {
            private readonly ScriptInstance m_Instance;
            private readonly UUI m_InventoryOwner;
            private readonly InventoryServiceInterface m_InventoryService;
            private readonly UUID m_FolderID;
            private readonly bool m_IsLibrary;

            public AgentInventoryChildItemAccessor(ScriptInstance instance, InventoryServiceInterface inventoryService, UUI owner, UUID folderID, bool isLibrary)
            {
                m_Instance = instance;
                m_InventoryOwner = owner;
                m_InventoryService = inventoryService;
                m_FolderID = folderID;
                m_IsLibrary = isLibrary;
            }

            [APIExtension(APIExtension.AgentInventory)]
            public static explicit operator AnArray(AgentInventoryChildItemAccessor acc)
            {
                AnArray res = new AnArray();
                if (acc.m_Instance == null || acc.m_InventoryService == null)
                {
                    return res;
                }
                foreach (InventoryItem item in acc.m_InventoryService.Folder.GetItems(acc.m_InventoryOwner.ID, acc.m_FolderID))
                {
                    res.Add(new LSLKey(item.ID));
                }
                return res;
            }

            public AgentInventoryItem this[string name, int index]
            {
                get
                {
                    if (m_Instance == null || m_InventoryService == null)
                    {
                        return new AgentInventoryItem();
                    }

                    lock (m_Instance)
                    {
                        int idx = index;
                        foreach (InventoryItem item in m_InventoryService.Folder.GetItems(m_InventoryOwner.ID, m_FolderID))
                        {
                            if (string.Compare(item.Name, name, true) == 0 && idx-- == 0)
                            {
                                return new AgentInventoryItem(m_Instance, m_InventoryService, m_InventoryOwner, item.ID, m_IsLibrary);
                            }
                        }
                    }

                    return new AgentInventoryItem();
                }
            }

            public AgentInventoryItem this[string name] => this[name, 0];
        }

        [APIExtension(APIExtension.AgentInventory, "agentinventoryfolder")]
        [APIDisplayName("agentinventoryfolder")]
        [APIIsVariableType]
        [ImplementsCustomTypecasts]
        [APIAccessibleMembers(
            "Key",
            "Name",
            "Version",
            "DefaultType",
            "ItemKeys",
            "FolderKeys",
            "ParentFolder",
            "ChildFolders",
            "ChildItems")]
        public class AgentInventoryFolder
        {
            private readonly ScriptInstance m_Instance;
            public UUI InventoryOwner { get; }
            public InventoryServiceInterface InventoryService { get; }
            public UUID Key { get; }
            public bool IsLibrary { get; }

            public AgentInventoryFolder()
            {
                InventoryOwner = UUI.Unknown;
                Key = UUID.Zero;
            }

            public AgentInventoryFolder(ScriptInstance instance, InventoryServiceInterface inventoryService, UUI owner, UUID folderID, bool isLibrary)
            {
                m_Instance = instance;
                InventoryService = inventoryService;
                InventoryOwner = owner;
                Key = folderID;
                IsLibrary = isLibrary;
            }

            private T With<T>(Func<InventoryFolder, T> getter, T defvalue)
            {
                InventoryFolder folder;
                if (m_Instance == null)
                {
                    return defvalue;
                }
                lock (m_Instance)
                {
                    if (InventoryService.Folder.TryGetValue(InventoryOwner.ID, Key, out folder))
                    {
                        return getter(folder);
                    }
                }
                return defvalue;
            }

            private void With<T>(Action<InventoryFolder, T> setter, T value)
            {
                InventoryFolder folder;
                if (m_Instance == null)
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "agentinventoryfolder");
                }
                lock (m_Instance)
                {
                    if (InventoryService.Folder.TryGetValue(InventoryOwner.ID, Key, out folder))
                    {
                        setter(folder, value);
                        InventoryService.Folder.Update(folder);
                    }
                }
            }

            public string Name
            {
                get { return With((folder) => folder.Name, string.Empty); }
                set { With((folder, v) => folder.Name = v, value); }
            }

            public AnArray FolderKeys
            {
                get
                {
                    var res = new AnArray();
                    if (m_Instance == null)
                    {
                        return res;
                    }

                    foreach (InventoryFolder folder in InventoryService.Folder.GetFolders(InventoryOwner.ID, Key))
                    {
                        res.Add(new LSLKey(folder.ID));
                    }
                    return res;
                }
            }

            public AnArray ItemKeys
            {
                get
                {
                    var res = new AnArray();
                    if (m_Instance == null)
                    {
                        return res;
                    }

                    foreach (InventoryItem item in InventoryService.Folder.GetItems(InventoryOwner.ID, Key))
                    {
                        res.Add(new LSLKey(item.ID));
                    }
                    return res;
                }
            }

            public void IncrementVersion()
            {
                if (m_Instance == null)
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                }
                lock (m_Instance)
                {
                    InventoryService.Folder.IncrementVersion(InventoryOwner.ID, Key);
                }
            }

            public AgentInventoryFolder ParentFolder => With((item) => new AgentInventoryFolder(m_Instance, InventoryService, InventoryOwner, item.ParentFolderID, IsLibrary), new AgentInventoryFolder());

            public int Version => With((folder) => folder.Version, 0);
            public int DefaultType => (int)With((folder) => folder.DefaultType, AssetType.Unknown);
            public AgentInventoryChildFolderAccessor ChildFolders => new AgentInventoryChildFolderAccessor(m_Instance, InventoryService, InventoryOwner, Key, IsLibrary);
            public AgentInventoryChildItemAccessor ChildItems => new AgentInventoryChildItemAccessor(m_Instance, InventoryService, InventoryOwner, Key, IsLibrary);
        }

        [APIExtension(APIExtension.AgentInventory, "agentinventoryfolderaccessor")]
        [APIDisplayName("agentinventoryfolderaccessor")]
        public class AgentInventoryFolderAccessor
        {
            private readonly ScriptInstance m_Instance;
            private readonly UUI m_Owner;
            private readonly InventoryServiceInterface m_InventoryService;
            private readonly bool m_IsLibrary;

            public AgentInventoryFolderAccessor(ScriptInstance instance, InventoryServiceInterface inventoryService, UUI owner, bool isLibrary)
            {
                m_Instance = instance;
                m_Owner = owner;
                m_InventoryService = inventoryService;
                m_IsLibrary = isLibrary;
            }

            public AgentInventoryFolder this[LSLKey key] => new AgentInventoryFolder(m_Instance, m_InventoryService, m_Owner, key.AsUUID, m_IsLibrary);

            private AgentInventoryFolder GetFolderType(AssetType type)
            {
                InventoryFolder folder;
                if (m_Instance != null && m_InventoryService.Folder.TryGetValue(m_Owner.ID, type, out folder))
                {
                    return new AgentInventoryFolder(m_Instance, m_InventoryService, m_Owner, folder.ID, m_IsLibrary);
                }
                return new AgentInventoryFolder();
            }

            public AgentInventoryFolder Root => GetFolderType(AssetType.RootFolder);
            public AgentInventoryFolder Trash => GetFolderType(AssetType.TrashFolder);
            public AgentInventoryFolder CurrentOutfit => GetFolderType(AssetType.CurrentOutfitFolder);
            public AgentInventoryFolder Notecards => GetFolderType(AssetType.Notecard);
            public AgentInventoryFolder Scripts => GetFolderType(AssetType.LSLText);
            public AgentInventoryFolder Textures => GetFolderType(AssetType.Texture);
            public AgentInventoryFolder Clothing => GetFolderType(AssetType.Clothing);
            public AgentInventoryFolder Bodyparts => GetFolderType(AssetType.Bodypart);
            public AgentInventoryFolder Animations => GetFolderType(AssetType.Animation);
            public AgentInventoryFolder Callingcards => GetFolderType(AssetType.CallingCard);
            public AgentInventoryFolder Landmarks => GetFolderType(AssetType.Landmark);
            public AgentInventoryFolder Sounds => GetFolderType(AssetType.Sound);
            public AgentInventoryFolder Objects => GetFolderType(AssetType.Object);
            public AgentInventoryFolder Snapshots => GetFolderType(AssetType.SnapshotFolder);
            public AgentInventoryFolder LostAndFound => GetFolderType(AssetType.LostAndFoundFolder);
            public AgentInventoryFolder Gestures => GetFolderType(AssetType.Gesture);
            public AgentInventoryFolder Favorites => GetFolderType(AssetType.FavoriteFolder);
            public AgentInventoryFolder MyOutfits => GetFolderType(AssetType.MyOutfitsFolder);
            public AgentInventoryFolder Inbox => GetFolderType(AssetType.Inbox);
            public AgentInventoryFolder Outbox => GetFolderType(AssetType.Outbox);
        }

        [APIExtension(APIExtension.AgentInventory, "AssetType")]
        [APIDisplayName("AssetType")]
        [APIAccessibleMembers]
        public class AgentAssetType
        {
            public const int Unknown = -1;
            public const int Texture = 0;
            public const int Sound = 1;
            public const int CallingCard = 2;
            public const int Landmark = 3;
            public const int Clothing = 5;
            public const int Object = 6;
            public const int Notecard = 7;
            public const int RootFolder = 8;
            public const int LSLText = 10;
            public const int LSLBytecode = 11;
            public const int TextureTGA = 12;
            public const int Bodypart = 13;
            public const int TrashFolder = 14;
            public const int SnapshotFolder = 15;
            public const int LostAndFoundFolder = 16;
            public const int SoundWAV = 17;
            public const int ImageTGA = 18;
            public const int ImageJPEG = 19;
            public const int Animation = 20;
            public const int Gesture = 21;
            public const int Simstate = 22;
            public const int FavoriteFolder = 23;
            public const int Link = 24;
            public const int LinkFolder = 25;
            public const int EnsembleStart = 26;
            public const int EnsembleEnd = 45;
            public const int CurrentOutfitFolder = 46;
            public const int OutfitFolder = 47;
            public const int MyOutfitsFolder = 48;
            public const int Mesh = 49;
            public const int Inbox = 50;
            public const int Outbox = 51;
            public const int BasicRoot = 52;
            public const int MarketplaceListings = 53;
            public const int MarketplaceStock = 54;
        }

        [APIExtension(APIExtension.AgentInventory, "InventoryType")]
        [APIDisplayName("InventoryType")]
        [APIAccessibleMembers]
        public class AgentInventoryType
        {
            public const int Unknown = -1;
            public const int Texture = 0;
            public const int Sound = 1;
            public const int CallingCard = 2;
            public const int Landmark = 3;
            public const int Clothing = 5;
            public const int Object = 6;
            public const int Notecard = 7;
            public const int Folder = 8;
            public const int RootFolder = 9;
            public const int LSLText = 10;
            public const int LSLBytecode = 11;
            public const int TextureTGA = 12;
            public const int Bodypart = 13;
            public const int TrashFolder = 14;
            public const int SnapshotFolder = 15;
            public const int Snapshot = 15;
            public const int LostAndFoundFolder = 16;
            public const int Attachable = 17;
            public const int Wearable = 18;
            public const int Animation = 20;
            public const int Gesture = 21;
            public const int Simstate = 22;
            public const int FavoriteFolder = 23;
            public const int CurrentOutfitFolder = 46;
            public const int OutfitFolder = 47;
            public const int MyOutfitsFolder = 48;
            public const int Mesh = 49;
            public const int Inbox = 50;
            public const int Outbox = 51;
            public const int BasicRoot = 51;
        }

        [APIExtension(APIExtension.AgentInventory, "agentinventoryitemaccessor")]
        [APIDisplayName("agentinventoryitemaccessor")]
        public class AgentInventoryItemAccessor
        {
            private readonly ScriptInstance m_Instance;
            private readonly UUI m_Owner;
            private readonly InventoryServiceInterface m_InventoryService;
            private readonly bool m_IsLibrary;

            public AgentInventoryItemAccessor(ScriptInstance instance, InventoryServiceInterface inventoryService, UUI owner, bool isLibrary)
            {
                m_Instance = instance;
                m_Owner = owner;
                m_InventoryService = inventoryService;
                m_IsLibrary = isLibrary;
            }

            public AgentInventoryItem this[LSLKey key] => new AgentInventoryItem(m_Instance, m_InventoryService, m_Owner, key.AsUUID, m_IsLibrary);
        }

        [APIExtension(APIExtension.AgentInventory, "agentinventory")]
        [APIDisplayName("agentinventory")]
        [APIIsVariableType]
        [ImplementsCustomTypecasts]
        [APIAccessibleMembers("Owner")]
        public class AgentInventory
        {
            private readonly ScriptInstance m_Instance;
            private readonly UUI m_Owner;
            private readonly InventoryServiceInterface m_InventoryService;
            private readonly bool m_IsLibrary;

            public AgentInventory()
            {
                m_Owner = UUI.Unknown;
            }

            public AgentInventory(ScriptInstance instance, InventoryServiceInterface inventoryService, UUI owner, bool isLibrary)
            {
                m_Instance = instance;
                m_Owner = owner;
                m_InventoryService = inventoryService;
                m_IsLibrary = isLibrary;
            }

            public LSLKey Owner => m_Owner.ID;

            public AgentInventoryFolderAccessor Folders => new AgentInventoryFolderAccessor(m_Instance, m_InventoryService, m_Owner, m_IsLibrary);
            public AgentInventoryItemAccessor Items => new AgentInventoryItemAccessor(m_Instance, m_InventoryService, m_Owner, m_IsLibrary);

            [APIExtension(APIExtension.AgentInventory, "agentinventory")]
            public static implicit operator bool(AgentInventory inv) => inv.m_Instance != null;
            [APIExtension(APIExtension.AgentInventory, "agentinventory")]
            public static implicit operator int(AgentInventory inv) => ((bool)inv).ToLSLBoolean();
        }

        [APIExtension(APIExtension.AgentInventory, "invIncrementFolderVersion")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "IncrementVersion")]
        public void IncrementFolderVersion(ScriptInstance instance, AgentInventoryFolder folder)
        {
            folder.IncrementVersion();
        }

        [APIExtension(APIExtension.AgentInventory, "invCreateFolder")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "CreateFolder")]
        public LSLKey CreateFolder(ScriptInstance instance, AgentInventoryFolder folder, string name, int type)
        {
            if(folder.InventoryService == null)
            {
                throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "agentinventory");
            }
            lock (instance)
            {
                var newfolder = new InventoryFolder
                {
                    ParentFolderID = folder.Key,
                    Name = name,
                    DefaultType = (AssetType)type,
                    Owner = folder.InventoryOwner,
                    ID = UUID.Random
                };
                folder.InventoryService.Folder.Add(newfolder);
                return newfolder.ID;
            }
        }

        [APIExtension(APIExtension.AgentInventory, "invCreateLink")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "CreateLink")]
        public LSLKey CreateLink(ScriptInstance instance, AgentInventoryItem originalItem, AgentInventoryFolder toFolder, string name, string description)
        {
            lock (instance)
            {
                if (toFolder.InventoryService == null)
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "agentinventoryfolder");
                }
                if(originalItem.InventoryService == null)
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "agentinventoryitem");
                }
                if (toFolder.IsLibrary || originalItem.IsLibrary)
                {
                    return UUID.Zero;
                }
                if (!toFolder.InventoryOwner.EqualsGrid(originalItem.InventoryOwner))
                {
                    return UUID.Zero;
                }

                InventoryItem linktoitem;
                if (originalItem.InventoryService.Item.TryGetValue(originalItem.InventoryOwner.ID, originalItem.Key, out linktoitem))
                {
                    if (linktoitem.AssetType == AssetType.Link || linktoitem.AssetType == AssetType.LinkFolder)
                    {
                        return UUID.Zero;
                    }
                    var linkitem = new InventoryItem
                    {
                        AssetID = originalItem.Key,
                        Name = name,
                        Description = description,
                        Creator = toFolder.InventoryOwner,
                        Owner = toFolder.InventoryOwner,
                        LastOwner = toFolder.InventoryOwner,
                        InventoryType = linktoitem.InventoryType,
                        AssetType = AssetType.Link,
                        ParentFolderID = toFolder.Key,
                        Flags = linktoitem.Flags
                    };
                    toFolder.InventoryService.Item.Add(linkitem);
                    return linkitem.ID;
                }
                return UUID.Zero;
            }
        }

        [APIExtension(APIExtension.AgentInventory, "invCreateLink")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "CreateLink")]
        public LSLKey CreateLink(ScriptInstance instance, AgentInventoryFolder originalFolder, AgentInventoryFolder toFolder, string name, string description)
        {
            lock (instance)
            {
                if (toFolder.InventoryService == null || originalFolder.InventoryService == null)
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "agentinventoryfolder");
                }
                if (toFolder.IsLibrary || originalFolder.IsLibrary)
                {
                    return UUID.Zero;
                }
                if (!toFolder.InventoryOwner.EqualsGrid(originalFolder.InventoryOwner))
                {
                    return UUID.Zero;
                }

                InventoryFolder linktofolder;
                if (originalFolder.InventoryService.Folder.TryGetValue(originalFolder.InventoryOwner.ID, originalFolder.Key, out linktofolder))
                {
                    var linkitem = new InventoryItem
                    {
                        AssetID = originalFolder.Key,
                        Name = name,
                        Description = description,
                        Creator = toFolder.InventoryOwner,
                        Owner = toFolder.InventoryOwner,
                        LastOwner = toFolder.InventoryOwner,
                        InventoryType = InventoryType.Folder,
                        AssetType = AssetType.LinkFolder,
                        ParentFolderID = toFolder.Key,
                        Flags = 0
                    };
                    toFolder.InventoryService.Item.Add(linkitem);
                    return linkitem.ID;
                }
                return UUID.Zero;
            }
        }

        [APIExtension(APIExtension.AgentInventory, "invPurge")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Purge")]
        public void Purge(ScriptInstance instance, AgentInventoryFolder folder)
        {
            if (folder.InventoryService == null)
            {
                throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "agentinventoryfolder");
            }
            if (folder.IsLibrary)
            {
                return;
            }
            lock (instance)
            {
                folder.InventoryService.Folder.Purge(folder.InventoryOwner.ID, folder.Key);
            }
        }

        [APIExtension(APIExtension.AgentInventory, "invDelete")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Delete")]
        public void Delete(ScriptInstance instance, AgentInventoryFolder folder)
        {
            if (folder.InventoryService == null)
            {
                throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "agentinventoryfolder");
            }
            if(folder.IsLibrary)
            {
                return;
            }
            lock (instance)
            {
                folder.InventoryService.Folder.Delete(folder.InventoryOwner.ID, folder.Key);
            }
        }

        [APIExtension(APIExtension.AgentInventory, "invDelete")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Delete")]
        public void Delete(ScriptInstance instance, AgentInventoryItem item)
        {
            if (item.InventoryService == null)
            {
                throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "agentinventoryitem");
            }
            if (item.IsLibrary)
            {
                return;
            }
            lock (instance)
            {
                item.InventoryService.Item.Delete(item.InventoryOwner.ID, item.Key);
            }
        }

        [APIExtension(APIExtension.AgentInventory, "invMove")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Move")]
        public void Move(ScriptInstance instance, AgentInventoryFolder folder, AgentInventoryFolder toFolder)
        {
            if (folder.InventoryService == null || toFolder.InventoryService == null)
            {
                throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "agentinventoryfolder");
            }
            if (folder.IsLibrary || toFolder.IsLibrary)
            {
                return;
            }
            if(!toFolder.InventoryOwner.EqualsGrid(folder.InventoryOwner))
            {
                return;
            }
            lock(instance)
            {
                folder.InventoryService.Folder.Move(folder.InventoryOwner.ID, folder.Key, toFolder.Key);
            }
        }

        [APIExtension(APIExtension.AgentInventory, "invMove")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Move")]
        public void Move(ScriptInstance instance, AgentInventoryItem item, AgentInventoryFolder toFolder)
        {
            if (item.InventoryService == null)
            {
                throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "agentinventoryitem");
            }
            if (toFolder.InventoryService == null)
            {
                throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "agentinventoryfolder");
            }
            if (item.IsLibrary || toFolder.IsLibrary)
            {
                return;
            }
            if (!toFolder.InventoryOwner.EqualsGrid(item.InventoryOwner))
            {
                return;
            }
            lock (instance)
            {
                item.InventoryService.Item.Move(item.InventoryOwner.ID, item.Key, toFolder.Key);
            }
        }
    }
}
