// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.Lsl.Api.LightShare
{
    [ScriptApiName("LightShare")]
    [LSLImplementation]
    public class LightShareApi : IScriptApi, IPlugin
    {
        public LightShareApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_WATER_COLOR = 0;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_WATER_FOG_DENSITY_EXPONENT = 1;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_UNDERWATER_FOG_MODIFIER = 2;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_REFLECTION_WAVELET_SCALE = 3;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_FRESNEL_SCALE = 4;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_FRESNEL_OFFSET = 5;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_REFRACT_SCALE_ABOVE = 6;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_REFRACT_SCALE_BELOW = 7;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_BLUR_MULTIPLIER = 8;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_BIG_WAVE_DIRECTION = 9;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_LITTLE_WAVE_DIRECTION = 10;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_NORMAL_MAP_TEXTURE = 11;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_HORIZON = 12;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_HAZE_HORIZON = 13;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_BLUE_DENSITY = 14;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_HAZE_DENSITY = 15;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_DENSITY_MULTIPLIER = 16;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_DISTANCE_MULTIPLIER = 17;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_MAX_ALTITUDE = 18;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_SUN_MOON_COLOR = 19;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_AMBIENT = 20;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_EAST_ANGLE = 21;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_SUN_GLOW_FOCUS = 22;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_SUN_GLOW_SIZE = 23;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_SCENE_GAMMA = 24;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_STAR_BRIGHTNESS = 25;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_CLOUD_COLOR = 26;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_CLOUD_XY_DENSITY = 27;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_CLOUD_COVERAGE = 28;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_CLOUD_SCALE = 29;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_CLOUD_DETAIL_XY_DENSITY = 30;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_CLOUD_SCROLL_X = 31;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_CLOUD_SCROLL_Y = 32;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_CLOUD_SCROLL_Y_LOCK = 33;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_CLOUD_SCROLL_X_LOCK = 34;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_DRAW_CLASSIC_CLOUDS = 35;
        [APIExtension(APIExtension.LightShare, APILevel.KeepCsName)]
        public const int WL_SUN_MOON_POSITION = 36;

        [APIExtension(APIExtension.LightShare, "lsGetWindlightScene")]
        public AnArray GetWindlightScene(ScriptInstance instance, AnArray rules)
        {
            throw new NotImplementedException();
        }

        [APIExtension(APIExtension.LightShare, "lsSetWindlightScene")]
        public int SetWindlightScene(ScriptInstance instance, AnArray rules)
        {
            throw new NotImplementedException();
        }

        [APIExtension(APIExtension.LightShare, "lsClearWindlightScene")]
        public void ClearWindlightScene(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        [APIExtension(APIExtension.LightShare, "lsSetWindlightSceneTargeted")]
        public int SetWindlightSceneTargeted(ScriptInstance instance, AnArray rules, LSLKey target)
        {
            throw new NotImplementedException();
        }
    }
}
