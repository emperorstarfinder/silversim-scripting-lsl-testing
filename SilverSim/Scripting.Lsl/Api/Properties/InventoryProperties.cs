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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types.Inventory;
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Properties
{
    [LSLImplementation]
    [ScriptApiName("PrimInventoryProperties")]
    [Description("PrimInventory Properties API")]
    public class InventoryProperties : IPlugin, IScriptApi
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APIExtension(APIExtension.Properties, "inventoryitem")]
        [APIDisplayName("inventoryitem")]
        [APIAccessibleMembers(
            "Key",
            "Name",
            "Desc",
            "Owner",
            "Creator",
            "Type")]
        [APIIsVariableType]
        [ImplementsCustomTypecasts]
        public class PrimInventoryItem
        {
            private readonly WeakReference<ScriptInstance> WeakInstance;
            private readonly WeakReference<ObjectPart> WeakPart;
            private readonly WeakReference<ObjectPartInventoryItem> WeakItem;

            public PrimInventoryItem()
            {
            }

            public PrimInventoryItem(ScriptInstance instance, ObjectPart part, ObjectPartInventoryItem item)
            {
                WeakInstance = new WeakReference<ScriptInstance>(instance);
                WeakPart = new WeakReference<ObjectPart>(part);
                WeakItem = new WeakReference<ObjectPartInventoryItem>(item);
            }

            private T With<T>(Func<ObjectPartInventoryItem, T> getter)
            {
                ScriptInstance instance;
                ObjectPartInventoryItem item;
                if (WeakInstance != null && WeakInstance.TryGetTarget(out instance) &&
                    WeakItem != null && WeakItem.TryGetTarget(out item))
                {
                    lock (instance)
                    {
                        return getter(item);
                    }
                }
                return default(T);
            }

            private void With<T>(Action<ObjectPartInventoryItem, T> setter, T value)
            {
                ScriptInstance instance;
                ObjectPartInventoryItem item;
                if (WeakInstance != null && WeakInstance.TryGetTarget(out instance) &&
                    WeakItem != null && WeakItem.TryGetTarget(out item))
                {
                    lock (instance)
                    {
                        setter(item, value);
                    }
                }
            }

            public string Name
            {
                get { return With((item) => item.Name); }
            }

            public string Desc
            {
                get { return With((item) => item.Description); }
                set { With((item, v) => item.Description = v, value); }
            }

            public int Type => With((item) => (int)item.InventoryType);

            public LSLKey Owner => With((item) => item.Owner.ID);

            public LSLKey Creator => With((item) => item.Creator.ID);

            public LSLKey Key => With((item) => item.AssetID);

            [APIExtension(APIExtension.Properties)]
            public static implicit operator bool(PrimInventoryItem c)
            {
                ScriptInstance instance;
                ObjectPart part;
                ObjectPartInventoryItem item;
                return c.WeakInstance.TryGetTarget(out instance) && c.WeakPart.TryGetTarget(out part) && c.WeakItem.TryGetTarget(out item);
            }
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "ScriptItem")]
        public PrimInventoryItem GetScript(ScriptInstance instance)
        {
            lock(instance)
            {
                return new PrimInventoryItem(instance, instance.Part, instance.Item);
            }
        }

        [APIExtension(APIExtension.Properties, "inventory")]
        [APIDisplayName("inventory")]
        [APIIsVariableType]
        public class PrimInventory
        {
            private readonly WeakReference<ScriptInstance> WeakInstance;
            private readonly WeakReference<ObjectPart> WeakPart;

            public PrimInventory(ScriptInstance instance, ObjectPart part)
            {
                WeakInstance = new WeakReference<ScriptInstance>(instance);
                WeakPart = new WeakReference<ObjectPart>(part);
            }

            private PrimInventoryItem WithInventory(Func<ScriptInstance, ObjectPart, PrimInventoryItem> action)
            {
                ScriptInstance instance;
                ObjectPart part;
                if (WeakInstance.TryGetTarget(out instance) &&
                    WeakPart.TryGetTarget(out part))
                {
                    lock (instance)
                    {
                        return action(instance, part);
                    }
                }
                else
                {
                    return new PrimInventoryItem();
                }
            }

            public PrimInventoryItem this[int type, int number] =>
                WithInventory((instance, part) =>
                {
                    try
                    {
                        return new PrimInventoryItem(instance, part, part.Inventory[(InventoryType)type, (uint)number]);
                    }
                    catch
                    {
                        return new PrimInventoryItem();
                    }
                });

            public PrimInventoryItem this[int number] =>
                WithInventory((instance, part) =>
                {
                    try
                    {
                        return new PrimInventoryItem(instance, part, part.Inventory[(uint)number]);
                    }
                    catch
                    {
                        return new PrimInventoryItem();
                    }
                });

            public PrimInventoryItem this[string name] =>
                WithInventory((instance, part) =>
                {
                    try
                    {
                        return new PrimInventoryItem(instance, part, part.Inventory[name]);
                    }
                    catch
                    {
                        return new PrimInventoryItem();
                    }
                });
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "Inventory")]
        public PrimInventory GetInventory(ScriptInstance instance)
        {
            lock (instance)
            {
                return new PrimInventory(instance, instance.Part);
            }
        }
    }
}
