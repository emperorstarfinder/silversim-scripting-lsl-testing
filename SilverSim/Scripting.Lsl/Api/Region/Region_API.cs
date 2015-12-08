// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.Lsl.Api.Region
{
    [ScriptApiName("Region")]
    [LSLImplementation]
    public partial class RegionApi : IScriptApi, IPlugin
    {
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int REGION_FLAG_ALLOW_DAMAGE = 0x1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int REGION_FLAG_FIXED_SUN = 0x10;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int REGION_FLAG_BLOCK_TERRAFORM = 0x40;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int REGION_FLAG_SANDBOX = 0x100;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int REGION_FLAG_DISABLE_COLLISIONS = 0x1000;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int REGION_FLAG_DISABLE_PHYSICS = 0x4000;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int REGION_FLAG_BLOCK_FLY = 0x80000;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int REGION_FLAG_ALLOW_DIRECT_TELEPORT = 0x100000;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int REGION_FLAG_RESTRICT_PUSHOBJECT = 0x400000;

        /* private constants not exported */
        public const int REGION_FLAGS_ALLOW_LANDMARK = 0x00000002;
        public const int REGION_FLAGS_ALLOW_SET_HOME = 0x00000004;
        public const int REGION_FLAGS_RESET_HOME_ON_TELEPORT = 0x00000008;
        public const int REGION_FLAGS_BLOCK_LAND_RESELL = 0x00000080;
        public const int REGION_FLAGS_SKIP_SCRIPTS = 0x00002000;
        public const int REGION_FLAGS_EXTERNALLY_VISIBLE = 0x00008000;
        public const int REGION_FLAGS_ALLOW_RETURN_ENCROACHING_OBJECT = 0x00010000;
        public const int REGION_FLAGS_ALLOW_RETURN_ENCROACHING_ESTATE_OBJECT = 0x00020000;
        public const int REGION_FLAGS_BLOCK_DWELL = 0x00040000;
        public const int REGION_FLAGS_ESTATE_SKIP_SCRIPTS = 0x00200000;
        public const int REGION_FLAGS_DENY_ANONYMOUS = 0x00800000;
        public const int REGION_FLAGS_ALLOW_PARCEL_CHANGES = 0x04000000;
        public const int REGION_FLAGS_ALLOW_VOICE = 0x10000000;
        public const int REGION_FLAGS_BLOCK_PARCEL_SEARCH = 0x20000000;
        public const int REGION_FLAGS_DENY_AGEUNVERIFIED = 0x40000000;

        public RegionApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        UUID GetTextureAssetID(ScriptInstance instance, string item)
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
