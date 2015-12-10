// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.Lsl
{
    public static class ExtensionMethods
    {
        public static int ToLSLBoolean(this bool v)
        {
            return v ? 1 : 0;
        }

        public static UUID GetSoundAssetID(this ScriptInstance instance, string item)
        {
            UUID assetID;
            if (!UUID.TryParse(item, out assetID))
            {
                /* must be an inventory item */
                lock (instance)
                {
                    ObjectPartInventoryItem i;
                    if (instance.Part.Inventory.TryGetValue(item, out i))
                    {
                        if (i.InventoryType != Types.Inventory.InventoryType.Sound)
                        {
                            throw new InvalidOperationException(string.Format("Inventory item {0} is not a sound", item));
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format("{0} not found in prim's inventory", item));
                    }
                    assetID = i.AssetID;
                }
            }
            return assetID;
        }

        public static UUID GetTextureAssetID(this ScriptInstance instance, string item)
        {
            UUID assetID;
            if (!UUID.TryParse(item, out assetID))
            {
                /* must be an inventory item */
                lock (instance)
                {
                    ObjectPartInventoryItem i;
                    if (instance.Part.Inventory.TryGetValue(item, out i))
                    {
                        if (i.InventoryType != Types.Inventory.InventoryType.Texture)
                        {
                            throw new InvalidOperationException(string.Format("Inventory item {0} is not a texture", item));
                        }
                        assetID = i.AssetID;
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format("{0} not found in prim's inventory", item));
                    }
                }
            }
            return assetID;
        }

    }
}
