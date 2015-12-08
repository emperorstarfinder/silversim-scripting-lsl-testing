// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.Lsl.Api.Sound
{
    [ScriptApiName("Sound")]
    [LSLImplementation]
    public partial class SoundApi : IScriptApi, IPlugin
    {
        UUID GetSoundAssetID(ScriptInstance instance, string item)
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

        public SoundApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
    }
}
