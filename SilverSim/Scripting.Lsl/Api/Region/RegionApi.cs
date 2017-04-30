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
using SilverSim.Main.Common.CmdIO;
using SilverSim.Scene.Types.Script;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Region
{
    [ScriptApiName("Region")]
    [LSLImplementation]
    [Description("LSL/OSSL Region API")]
    public partial class RegionApi : IScriptApi, IPlugin
    {
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_ALLOW_DAMAGE = 0x1;
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_FIXED_SUN = 0x10;
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_BLOCK_TERRAFORM = 0x40;
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_SANDBOX = 0x100;
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_DISABLE_COLLISIONS = 0x1000;
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_DISABLE_PHYSICS = 0x4000;
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_BLOCK_FLY = 0x80000;
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_ALLOW_DIRECT_TELEPORT = 0x100000;
        [APILevel(APIFlags.LSL)]
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

        CommandRegistry m_Commands;

        public RegionApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Commands = loader.CommandRegistry;
        }
    }
}
