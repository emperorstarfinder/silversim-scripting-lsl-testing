// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.KeyframeMotion
{
    [ScriptApiName("Keyframe")]
    [LSLImplementation]
    [Description("LSL KeyframeMotion API")]
    public class KeyframeMotionApi : IScriptApi, IPlugin
    {
        public KeyframeMotionApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        public const int KFM_COMMAND = 0;
        [APILevel(APIFlags.LSL)]
        public const int KFM_MODE = 1;
        [APILevel(APIFlags.LSL)]
        public const int KFM_DATA = 2;

        [APILevel(APIFlags.LSL)]
        public const int KFM_CMD_PLAY = 0;
        [APILevel(APIFlags.LSL)]
        public const int KFM_CMD_STOP = 1;
        [APILevel(APIFlags.LSL)]
        public const int KFM_CMD_PAUSE = 2;

        [APILevel(APIFlags.LSL)]
        public const int KFM_FORWARD = 0;
        [APILevel(APIFlags.LSL)]
        public const int KFM_LOOP = 1;
        [APILevel(APIFlags.LSL)]
        public const int KFM_PING_PONG = 2;
        [APILevel(APIFlags.LSL)]
        public const int KFM_REVERSE = 3;

        [APILevel(APIFlags.LSL)]
        public const int KFM_ROTATION = 1;
        public const int KFM_TRANSLATION = 2;

        [APILevel(APIFlags.LSL, "llSetKeyframedMotion")]
        public void SetKeyframedMotion(ScriptInstance instance, AnArray keyframes, AnArray options)
        {
            throw new NotImplementedException("llSetKeyframedMotion(list, list)");
        }
    }
}
