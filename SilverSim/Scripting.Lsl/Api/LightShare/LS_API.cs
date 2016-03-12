// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.ComponentModel;
using EnvironmentController = SilverSim.Scene.Types.SceneEnvironment.EnvironmentController;
using WindlightSkyData = SilverSim.Scene.Types.SceneEnvironment.EnvironmentController.WindlightSkyData;
using WindlightWaterData = SilverSim.Scene.Types.SceneEnvironment.EnvironmentController.WindlightWaterData;
using WLVector4 = SilverSim.Scene.Types.SceneEnvironment.EnvironmentController.WLVector4;

namespace SilverSim.Scripting.Lsl.Api.LightShare
{
    [ScriptApiName("LightShare")]
    [LSLImplementation]
    [Description("OSSL LightShare API")]
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

        [APIExtension(APIExtension.LightShare)]
        public const int WL_WATER_COLOR = 0;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_WATER_FOG_DENSITY_EXPONENT = 1;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_UNDERWATER_FOG_MODIFIER = 2;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_REFLECTION_WAVELET_SCALE = 3;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_FRESNEL_SCALE = 4;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_FRESNEL_OFFSET = 5;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_REFRACT_SCALE_ABOVE = 6;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_REFRACT_SCALE_BELOW = 7;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_BLUR_MULTIPLIER = 8;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_BIG_WAVE_DIRECTION = 9;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_LITTLE_WAVE_DIRECTION = 10;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_NORMAL_MAP_TEXTURE = 11;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_HORIZON = 12;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_HAZE_HORIZON = 13;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_BLUE_DENSITY = 14;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_HAZE_DENSITY = 15;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_DENSITY_MULTIPLIER = 16;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_DISTANCE_MULTIPLIER = 17;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_MAX_ALTITUDE = 18;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_SUN_MOON_COLOR = 19;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_AMBIENT = 20;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_EAST_ANGLE = 21;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_SUN_GLOW_FOCUS = 22;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_SUN_GLOW_SIZE = 23;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_SCENE_GAMMA = 24;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_STAR_BRIGHTNESS = 25;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_CLOUD_COLOR = 26;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_CLOUD_XY_DENSITY = 27;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_CLOUD_COVERAGE = 28;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_CLOUD_SCALE = 29;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_CLOUD_DETAIL_XY_DENSITY = 30;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_CLOUD_SCROLL_X = 31;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_CLOUD_SCROLL_Y = 32;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_CLOUD_SCROLL_Y_LOCK = 33;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_CLOUD_SCROLL_X_LOCK = 34;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_DRAW_CLASSIC_CLOUDS = 35;
        [APIExtension(APIExtension.LightShare)]
        public const int WL_SUN_MOON_POSITION = 36;

        [APIExtension(APIExtension.LightShare, "lsGetWindlightScene")]
        public AnArray GetWindlightScene(ScriptInstance instance, AnArray rules)
        {
            AnArray res = new AnArray();
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.IsEstateManager(instance.Item.Owner))
                {
                    EnvironmentController envcontrol = scene.Environment;
                    WindlightSkyData skyData = envcontrol.SkyData;
                    WindlightWaterData waterData = envcontrol.WaterData;
                    foreach (IValue iv in rules)
                    {
                        int type = iv.AsInt;
                        switch (type)
                        {
                            case WL_WATER_COLOR:
                                res.Add(type);
                                res.Add(waterData.Color.AsVector3 * 255f);
                                break;

                            case WL_WATER_FOG_DENSITY_EXPONENT:
                                res.Add(type);
                                res.Add(waterData.FogDensityExponent);
                                break;

                            case WL_UNDERWATER_FOG_MODIFIER:
                                res.Add(type);
                                res.Add(waterData.UnderwaterFogModifier);
                                break;

                            case WL_REFLECTION_WAVELET_SCALE:
                                res.Add(type);
                                res.Add(waterData.ReflectionWaveletScale);
                                break;

                            case WL_FRESNEL_SCALE:
                                res.Add(type);
                                res.Add(waterData.FresnelScale);
                                break;

                            case WL_FRESNEL_OFFSET:
                                res.Add(type);
                                res.Add(waterData.FresnelOffset);
                                break;

                            case WL_REFRACT_SCALE_ABOVE:
                                res.Add(type);
                                res.Add(waterData.RefractScaleAbove);
                                break;

                            case WL_REFRACT_SCALE_BELOW:
                                res.Add(type);
                                res.Add(waterData.RefractScaleBelow);
                                break;

                            case WL_BLUR_MULTIPLIER:
                                res.Add(type);
                                res.Add(waterData.BlurMultiplier);
                                break;

                            case WL_BIG_WAVE_DIRECTION:
                                res.Add(type);
                                res.Add((Vector3)waterData.BigWaveDirection);
                                break;

                            case WL_LITTLE_WAVE_DIRECTION:
                                res.Add(type);
                                res.Add((Vector3)waterData.LittleWaveDirection);
                                break;

                            case WL_NORMAL_MAP_TEXTURE:
                                res.Add(type);
                                res.Add(waterData.NormalMapTexture);
                                break;

                            case WL_HORIZON:
                                res.Add(type);
                                res.Add((Quaternion)skyData.Horizon);
                                break;

                            case WL_HAZE_HORIZON:
                                res.Add(type);
                                res.Add(skyData.HazeHorizon);
                                break;

                            case WL_BLUE_DENSITY:
                                res.Add(type);
                                res.Add((Quaternion)skyData.BlueDensity);
                                break;

                            case WL_HAZE_DENSITY:
                                res.Add(type);
                                res.Add(skyData.HazeDensity);
                                break;

                            case WL_DENSITY_MULTIPLIER:
                                res.Add(type);
                                res.Add(skyData.DensityMultiplier);
                                break;

                            case WL_DISTANCE_MULTIPLIER:
                                res.Add(type);
                                res.Add(skyData.DistanceMultiplier);
                                break;

                            case WL_MAX_ALTITUDE:
                                res.Add(type);
                                res.Add(skyData.MaxAltitude);
                                break;

                            case WL_SUN_MOON_COLOR:
                                res.Add(type);
                                res.Add((Quaternion)skyData.SunMoonColor);
                                break;

                            case WL_AMBIENT:
                                res.Add(type);
                                res.Add((Quaternion)skyData.Ambient);
                                break;

                            case WL_EAST_ANGLE:
                                res.Add(type);
                                res.Add(skyData.EastAngle);
                                break;

                            case WL_SUN_GLOW_FOCUS:
                                res.Add(type);
                                res.Add(skyData.SunGlowFocus);
                                break;

                            case WL_SUN_GLOW_SIZE:
                                res.Add(type);
                                res.Add(skyData.SunGlowSize);
                                break;

                            case WL_SCENE_GAMMA:
                                res.Add(type);
                                res.Add(skyData.SceneGamma);
                                break;

                            case WL_STAR_BRIGHTNESS:
                                res.Add(type);
                                res.Add(skyData.StarBrightness);
                                break;

                            case WL_CLOUD_COLOR:
                                res.Add(type);
                                res.Add((Quaternion)skyData.CloudColor);
                                break;

                            case WL_CLOUD_XY_DENSITY:
                                res.Add(type);
                                res.Add(skyData.CloudXYDensity);
                                break;

                            case WL_CLOUD_COVERAGE:
                                res.Add(type);
                                res.Add(skyData.CloudCoverage);
                                break;

                            case WL_CLOUD_SCALE:
                                res.Add(type);
                                res.Add(skyData.CloudScale);
                                break;

                            case WL_CLOUD_DETAIL_XY_DENSITY:
                                res.Add(type);
                                res.Add(skyData.CloudDetailXYDensity);
                                break;

                            case WL_CLOUD_SCROLL_X:
                                res.Add(type);
                                res.Add(skyData.CloudScroll.X);
                                break;

                            case WL_CLOUD_SCROLL_Y:
                                res.Add(type);
                                res.Add(skyData.CloudScroll.Y);
                                break;

                            case WL_CLOUD_SCROLL_X_LOCK:
                                res.Add(type);
                                res.Add(skyData.CloudScrollXLock);
                                break;

                            case WL_CLOUD_SCROLL_Y_LOCK:
                                res.Add(type);
                                res.Add(skyData.CloudScrollYLock);
                                break;

                            case WL_DRAW_CLASSIC_CLOUDS:
                                res.Add(type);
                                res.Add(skyData.DrawClassicClouds);
                                break;

                            case WL_SUN_MOON_POSITION:
                                res.Add(type);
                                res.Add(skyData.SunMoonPosition);
                                break;
                        }
                    }
                }
            }

            return res;
        }

        [APIExtension(APIExtension.LightShare, "lsSetWindlightScene")]
        public int SetWindlightScene(ScriptInstance instance, AnArray rules)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.IsEstateManager(instance.Item.Owner))
                {
                    EnvironmentController envcontrol = scene.Environment;
                    WindlightSkyData skyData = envcontrol.SkyData;
                    WindlightWaterData waterData = envcontrol.WaterData;
                    try
                    {
                        ModifyWindlightData(instance, ref skyData, ref waterData, rules, "lsSetWindlightScene");
                    }
                    catch (InvalidCastException e)
                    {
                        instance.ShoutError(e.Message);
                        return 0;
                    }
                    envcontrol.SkyData = skyData;
                    envcontrol.WaterData = waterData;
                    scene.TriggerLightShareSettingsChanged(); /* only called on non-targeted changes */
                    return 1;
                }
            }
            return 0;
        }

        [APIExtension(APIExtension.LightShare, "lsClearWindlightScene")]
        public void ClearWindlightScene(ScriptInstance instance)
        {
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.IsEstateManager(instance.Item.Owner))
                {
                    instance.Part.ObjectGroup.Scene.Environment.ResetLightShare();
                    scene.TriggerLightShareSettingsChanged(); /* only called on non-targeted changes */
                }
            }
        }

        [APIExtension(APIExtension.LightShare, "lsSetWindlightSceneTargeted")]
        public int SetWindlightSceneTargeted(ScriptInstance instance, AnArray rules, LSLKey target)
        {
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.IsEstateManager(instance.Item.Owner))
                {
                    EnvironmentController envcontrol = scene.Environment;
                    WindlightSkyData skyData = envcontrol.SkyData;
                    WindlightWaterData waterData = envcontrol.WaterData;
                    try
                    {
                        ModifyWindlightData(instance, ref skyData, ref waterData, rules, "lsSetWindlightSceneTargeted");
                    }
                    catch(InvalidCastException e)
                    {
                        instance.ShoutError(e.Message);
                        return 0;
                    }
                    envcontrol.SendTargetedWindlightProfile(target.AsUUID, skyData, waterData);
                    return 1;
                }
            }
            return 0;
        }

        void ModifyWindlightData(ScriptInstance instance, ref WindlightSkyData skyData, ref WindlightWaterData waterData, AnArray rules, string functionName)
        {
            if(rules.Count % 2 != 0)
            {
                throw new ArgumentException("rules list to " + functionName + " must have an even number of parameters.");
            }

            int idx = 0;
            while(idx < rules.Count)
            {
                switch(rules[idx++].AsInt)
                {
                    case WL_WATER_COLOR:
                        try
                        {
                            waterData.Color = new Color(rules[idx++].AsVector3 / 255f);
                        }
                        catch(InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_WATER_COLOR");
                        }
                        break;

                    case WL_WATER_FOG_DENSITY_EXPONENT:
                        try
                        {
                            waterData.FogDensityExponent = rules[idx++].AsReal;
                        }
                        catch(InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_WATER_FOG_DENSITY_EXPONENT");
                        }
                        break;

                    case WL_UNDERWATER_FOG_MODIFIER:
                        try
                        {
                            waterData.UnderwaterFogModifier = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_UNDERWATER_FOG_MODIFIER");
                        }
                        break;

                    case WL_REFLECTION_WAVELET_SCALE:
                        try
                        {
                            waterData.ReflectionWaveletScale = rules[idx++].AsVector3;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_REFLECTION_WAVELET_SCALE");
                        }
                        break;

                    case WL_FRESNEL_SCALE:
                        try
                        {
                            waterData.FresnelScale = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_FRESNEL_SCALE");
                        }
                        break;

                    case WL_FRESNEL_OFFSET:
                        try
                        {
                            waterData.FresnelOffset = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_FRESNEL_OFFSET");
                        }
                        break;

                    case WL_REFRACT_SCALE_ABOVE:
                        try
                        {
                            waterData.RefractScaleAbove = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_REFRACT_SCALE_ABOVE");
                        }
                        break;

                    case WL_REFRACT_SCALE_BELOW:
                        try
                        {
                            waterData.RefractScaleBelow = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_REFRACT_SCALE_BELOW");
                        }
                        break;

                    case WL_BLUR_MULTIPLIER:
                        try
                        {
                            waterData.BlurMultiplier = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_BLUR_MULTIPLIER");
                        }
                        break;

                    case WL_BIG_WAVE_DIRECTION:
                        try
                        {
                            waterData.BigWaveDirection = new EnvironmentController.WLVector2(rules[idx++].AsVector3);
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_BIG_WAVE_DIRECTION");
                        }
                        break;

                    case WL_LITTLE_WAVE_DIRECTION:
                        try
                        {
                            waterData.LittleWaveDirection = new EnvironmentController.WLVector2(rules[idx++].AsVector3);
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_LITTLE_WAVE_DIRECTION");
                        }
                        break;

                    case WL_NORMAL_MAP_TEXTURE:
                        try
                        {
                            waterData.NormalMapTexture = instance.GetTextureAssetID(rules[idx++].ToString());
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_NORMAL_MAP_TEXTURE");
                        }
                        break;

                    case WL_HORIZON:
                        try
                        {
                            skyData.Horizon = new WLVector4(rules[idx++].AsQuaternion);
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_HORIZON");
                        }
                        break;

                    case WL_HAZE_HORIZON:
                        try
                        {
                            skyData.HazeHorizon = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_HAZE_HORIZON");
                        }
                        break;

                    case WL_BLUE_DENSITY:
                        try
                        {
                            skyData.BlueDensity = new WLVector4(rules[idx++].AsQuaternion);
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_BLUE_DENSITY");
                        }
                        break;

                    case WL_HAZE_DENSITY:
                        try
                        {
                            skyData.HazeDensity = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_HAZE_DENSITY");
                        }
                        break;

                    case WL_DENSITY_MULTIPLIER:
                        try
                        {
                            skyData.DensityMultiplier = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_DENSITY_MULTIPLIER");
                        }
                        break;

                    case WL_DISTANCE_MULTIPLIER:
                        try
                        {
                            skyData.DistanceMultiplier = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_DISTANCE_MULTIPLIER");
                        }
                        break;

                    case WL_MAX_ALTITUDE:
                        try
                        {
                            skyData.MaxAltitude = rules[idx++].AsInt;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_MAX_ALTITUDE");
                        }
                        break;

                    case WL_SUN_MOON_COLOR:
                        try
                        {
                            skyData.SunMoonColor = new WLVector4(rules[idx++].AsQuaternion);
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_SUN_MOON_COLOR");
                        }
                        break;

                    case WL_AMBIENT:
                        try
                        {
                            skyData.Ambient = new WLVector4(rules[idx++].AsQuaternion);
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_AMBIENT");
                        }
                        break;

                    case WL_EAST_ANGLE:
                        try
                        {
                            skyData.EastAngle = rules[idx++].AsReal.Clamp(0, 2 * Math.PI);
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_EAST_ANGLE");
                        }
                        break;

                    case WL_SUN_GLOW_FOCUS:
                        try
                        {
                            skyData.SunGlowFocus = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_SUN_GLOW_FOCUS");
                        }
                        break;

                    case WL_SUN_GLOW_SIZE:
                        try
                        {
                            skyData.SunGlowSize = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_SUN_GLOW_SIZE");
                        }
                        break;

                    case WL_SCENE_GAMMA:
                        try
                        {
                            skyData.SceneGamma = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_SCENE_GAMMA");
                        }
                        break;

                    case WL_STAR_BRIGHTNESS:
                        try
                        {
                            skyData.StarBrightness = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_STAR_BRIGHTNESS");
                        }
                        break;

                    case WL_CLOUD_COLOR:
                        try
                        {
                            skyData.CloudColor = new WLVector4(rules[idx++].AsQuaternion);
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_CLOUD_COLOR");
                        }
                        break;

                    case WL_CLOUD_XY_DENSITY:
                        try
                        {
                            skyData.CloudXYDensity = rules[idx++].AsVector3;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_CLOUD_XY_DENSITY");
                        }
                        break;

                    case WL_CLOUD_COVERAGE:
                        try
                        {
                            skyData.CloudCoverage = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_CLOUD_COVERAGE");
                        }
                        break;

                    case WL_CLOUD_SCALE:
                        try
                        {
                            skyData.CloudScale = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_CLOUD_SCALE");
                        }
                        break;

                    case WL_CLOUD_DETAIL_XY_DENSITY:
                        try
                        {
                            skyData.CloudDetailXYDensity = rules[idx++].AsVector3;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_CLOUD_DETAIL_XY_DENSITY");
                        }
                        break;

                    case WL_CLOUD_SCROLL_X:
                        try
                        {
                            skyData.CloudScroll.X = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_CLOUD_SCROLL_X");
                        }
                        break;

                    case WL_CLOUD_SCROLL_Y:
                        try
                        {
                            skyData.CloudScroll.Y = rules[idx++].AsReal;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_CLOUD_SCROLL_Y");
                        }
                        break;

                    case WL_CLOUD_SCROLL_X_LOCK:
                        try
                        {
                            skyData.CloudScrollXLock = rules[idx++].AsBoolean;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_CLOUD_SCROLL_X_LOCK");
                        }
                        break;

                    case WL_CLOUD_SCROLL_Y_LOCK:
                        try
                        {
                            skyData.CloudScrollYLock = rules[idx++].AsBoolean;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_CLOUD_SCROLL_Y_LOCK");
                        }
                        break;

                    case WL_DRAW_CLASSIC_CLOUDS:
                        try
                        {
                            skyData.DrawClassicClouds = rules[idx++].AsBoolean;
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_DRAW_CLASSIC_CLOUDS");
                        }
                        break;

                    case WL_SUN_MOON_POSITION:
                        try
                        {
                            skyData.SunMoonPosition = rules[idx++].AsReal.Clamp(0, 2 * Math.PI);
                        }
                        catch (InvalidCastException)
                        {
                            throw new ArgumentException(functionName + ": Invalid parameter to WL_SUN_MOON_POSITION");
                        }
                        break;

                    default:
                        throw new ArgumentException(functionName + ": Invalid parameter " + rules[idx - 1].ToString());
                }
            }
        }
    }
}
