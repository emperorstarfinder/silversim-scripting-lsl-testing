// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;

namespace SilverSim.Scripting.Lsl.Api.Detected
{
    [ScriptApiName("Detected")]
    [LSLImplementation]
    public partial class DetectedApi : IScriptApi, IPlugin
    {
        [APILevel(APIFlags.LSL)]
        public const int TOUCH_INVALID_FACE = -1;
        [APILevel(APIFlags.LSL)]
        public static readonly Vector3 TOUCH_INVALID_TEXCOORD = new Vector3(-1.0, -1.0, 0.0);
        [APILevel(APIFlags.LSL)]
        public static readonly Vector3 TOUCH_INVALID_VECTOR = Vector3.Zero;

        public DetectedApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
    }
}
