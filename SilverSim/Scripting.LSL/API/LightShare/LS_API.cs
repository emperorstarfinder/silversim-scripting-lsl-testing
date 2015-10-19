﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL.API.LightShare
{
    [ScriptApiName("LightShare")]
    [LSLImplementation]
    public class LS_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public LS_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LightShare)]
        public const int WL_WATER_COLOR = 0;
        [APILevel(APIFlags.LightShare)]
        public const int WL_WATER_FOG_DENSITY_EXPONENT = 1;
        [APILevel(APIFlags.LightShare)]
        public const int WL_UNDERWATER_FOG_MODIFIER = 2;
        [APILevel(APIFlags.LightShare)]
        public const int WL_REFLECTION_WAVELET_SCALE = 3;
        [APILevel(APIFlags.LightShare)]
        public const int WL_FRESNEL_SCALE = 4;
        [APILevel(APIFlags.LightShare)]
        public const int WL_FRESNEL_OFFSET = 5;
        [APILevel(APIFlags.LightShare)]
        public const int WL_REFRACT_SCALE_ABOVE = 6;
        [APILevel(APIFlags.LightShare)]
        public const int WL_REFRACT_SCALE_BELOW = 7;
        [APILevel(APIFlags.LightShare)]
        public const int WL_BLUR_MULTIPLIER = 8;
        [APILevel(APIFlags.LightShare)]
        public const int WL_BIG_WAVE_DIRECTION = 9;
        [APILevel(APIFlags.LightShare)]
        public const int WL_LITTLE_WAVE_DIRECTION = 10;
        [APILevel(APIFlags.LightShare)]
        public const int WL_NORMAL_MAP_TEXTURE = 11;
        [APILevel(APIFlags.LightShare)]
        public const int WL_HORIZON = 12;
        [APILevel(APIFlags.LightShare)]
        public const int WL_HAZE_HORIZON = 13;
        [APILevel(APIFlags.LightShare)]
        public const int WL_BLUE_DENSITY = 14;
        [APILevel(APIFlags.LightShare)]
        public const int WL_HAZE_DENSITY = 15;
        [APILevel(APIFlags.LightShare)]
        public const int WL_DENSITY_MULTIPLIER = 16;
        [APILevel(APIFlags.LightShare)]
        public const int WL_DISTANCE_MULTIPLIER = 17;
        [APILevel(APIFlags.LightShare)]
        public const int WL_MAX_ALTITUDE = 18;
        [APILevel(APIFlags.LightShare)]
        public const int WL_SUN_MOON_COLOR = 19;
        [APILevel(APIFlags.LightShare)]
        public const int WL_AMBIENT = 20;
        [APILevel(APIFlags.LightShare)]
        public const int WL_EAST_ANGLE = 21;
        [APILevel(APIFlags.LightShare)]
        public const int WL_SUN_GLOW_FOCUS = 22;
        [APILevel(APIFlags.LightShare)]
        public const int WL_SUN_GLOW_SIZE = 23;
        [APILevel(APIFlags.LightShare)]
        public const int WL_SCENE_GAMMA = 24;
        [APILevel(APIFlags.LightShare)]
        public const int WL_STAR_BRIGHTNESS = 25;
        [APILevel(APIFlags.LightShare)]
        public const int WL_CLOUD_COLOR = 26;
        [APILevel(APIFlags.LightShare)]
        public const int WL_CLOUD_XY_DENSITY = 27;
        [APILevel(APIFlags.LightShare)]
        public const int WL_CLOUD_COVERAGE = 28;
        [APILevel(APIFlags.LightShare)]
        public const int WL_CLOUD_SCALE = 29;
        [APILevel(APIFlags.LightShare)]
        public const int WL_CLOUD_DETAIL_XY_DENSITY = 30;
        [APILevel(APIFlags.LightShare)]
        public const int WL_CLOUD_SCROLL_X = 31;
        [APILevel(APIFlags.LightShare)]
        public const int WL_CLOUD_SCROLL_Y = 32;
        [APILevel(APIFlags.LightShare)]
        public const int WL_CLOUD_SCROLL_Y_LOCK = 33;
        [APILevel(APIFlags.LightShare)]
        public const int WL_CLOUD_SCROLL_X_LOCK = 34;
        [APILevel(APIFlags.LightShare)]
        public const int WL_DRAW_CLASSIC_CLOUDS = 35;
        [APILevel(APIFlags.LightShare)]
        public const int WL_SUN_MOON_POSITION = 36;

        [APILevel(APIFlags.LightShare)]
        [ScriptFunctionName("lsGetWindlightScene")]
        public AnArray GetWindlightScene(ScriptInstance instance, AnArray rules)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LightShare)]
        [ScriptFunctionName("lsSetWindlightScene")]
        public int SetWindlightScene(ScriptInstance instance, AnArray rules)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LightShare)]
        [ScriptFunctionName("lsClearWindlightScene")]
        public void ClearWindlightScene(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LightShare)]
        [ScriptFunctionName("lsSetWindlightSceneTargeted")]
        public int SetWindlightSceneTargeted(ScriptInstance instance, AnArray rules, LSLKey target)
        {
            throw new NotImplementedException();
        }
    }
}
