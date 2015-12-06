// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Terraform
{
    [ScriptApiName("Terraform")]
    [LSLImplementation]
    public class TerraformApi : IScriptApi, IPlugin
    {
        public TerraformApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_LEVEL = 0;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_RAISE = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_LOWER = 2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_SMOOTH = 3;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_NOISE = 4;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_REVERT = 5;

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_SMALL_BRUSH = 0;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_MEDIUM_BRUSH = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LAND_LARGE_BRUSH = 2;

        [APILevel(APIFlags.LSL, "llModifyLand")]
        public void ModifyLand(ScriptInstance instance, int action, int brush)
        {
            throw new NotImplementedException("llModifyLand(integer, integer)");
        }
    }
}
