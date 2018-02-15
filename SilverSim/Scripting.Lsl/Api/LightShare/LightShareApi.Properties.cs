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

using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static SilverSim.Scene.Types.SceneEnvironment.EnvironmentController;

namespace SilverSim.Scripting.Lsl.Api.LightShare
{
    public sealed partial class LightShareApi
    {
        [APIExtension(APIExtension.Properties, "lightshare")]
        [APIDisplayName("lightshare")]
        [APIAccessibleMembers(
            "AmbientColor",
            "SunMoonColor",
            "SunGlowSize",
            "SunGlowFocus",
            "StarBrightness",
            "SceneGamma",
            "MaxAltitude",
            "HorizonColor",
            "HazeHorizon",
            "HazeDensity",
            "EastAngle",
            "SunMoonPosition",
            "DrawClassicClouds",
            "DensityMultiplier",
            "CloudXYDensity",
            "CloudScrollYLock",
            "CloudScrollXLock",
            "CloudScroll",
            "CloudScale",
            "CloudDetailXYDensity",
            "BlueDensityColor",
            "CloudCoverage",
            "CloudColor",
            "DistanceMultiplier",
            "BigWaveDirection",
            "LittleWaveDirection",
            "BlurMultiplier",
            "FresnelScale",
            "FresnelOffset",
            "WaterNormalMapTexture",
            "ReflectionWaveletScale",
            "RefractScaleAbove",
            "RefractScaleBelow",
            "UnderwaterFogModifier",
            "WaterColor",
            "FogDensityExponent")]
        [APIIsVariableType]
        [Serializable]
        [APICloneOnAssignment]
        public class LightShareData
        {
            public WeakReference<ScriptInstance> WeakInstance;

            public LightShareData(LightShareData d)
            {
                ScriptInstance target;
                d.WeakInstance.TryGetTarget(out target);
                WeakInstance = new WeakReference<ScriptInstance>(target);
            }

            public LightShareData(ScriptInstance instance)
            {
                WeakInstance = new WeakReference<ScriptInstance>(instance);
            }
            [XmlIgnore]
            public WindlightSkyData Sky = WindlightSkyData.Defaults;
            [XmlIgnore]
            public WindlightWaterData Water = WindlightWaterData.Defaults;

            public void RestoreFromSerialization(ScriptInstance instance)
            {
                WeakInstance = new WeakReference<ScriptInstance>(instance);
            }

            public Quaternion AmbientColor
            {
                get { return Sky.Ambient; }
                set { Sky.Ambient = new WLVector4(value); }
            }

            public Quaternion SunMoonColor
            {
                get { return Sky.SunMoonColor; }
                set { Sky.SunMoonColor = new WLVector4(value); }
            }

            public double SunGlowSize
            {
                get { return Sky.SunGlowSize; }
                set { Sky.SunGlowSize = value; }
            }

            public double SunGlowFocus
            {
                get { return Sky.SunGlowFocus; }
                set { Sky.SunGlowFocus = value; }
            }

            public double StarBrightness
            {
                get { return Sky.StarBrightness; }
                set { Sky.StarBrightness = value; }
            }

            public double SceneGamma
            {
                get { return Sky.SceneGamma; }
                set { Sky.SceneGamma = value; }
            }
            public int MaxAltitude
            {
                get { return Sky.MaxAltitude; }
                set { Sky.MaxAltitude = value; }
            }
            public Quaternion HorizonColor
            {
                get { return Sky.Horizon; }
                set { Sky.Horizon = new WLVector4(value); }
            }
            public double HazeHorizon
            {
                get { return Sky.HazeHorizon; }
                set { Sky.HazeHorizon = value; }
            }
            public double HazeDensity
            {
                get { return Sky.HazeDensity; }
                set { Sky.HazeDensity = value; }
            }
            public double EastAngle
            {
                get { return Sky.EastAngle; }
                set { Sky.EastAngle = value; }
            }
            public double SunMoonPosition
            {
                get { return Sky.SunMoonPosition; }
                set { Sky.SunMoonPosition = value; }
            }
            public int DrawClassicClouds
            {
                get { return Sky.DrawClassicClouds.ToLSLBoolean(); }
                set { Sky.DrawClassicClouds = value != 0; }
            }
            public double DensityMultiplier
            {
                get { return Sky.DensityMultiplier; }
                set { Sky.DensityMultiplier = value; }
            }
            public Vector3 CloudXYDensity
            {
                get { return Sky.CloudXYDensity; }
                set { Sky.CloudXYDensity = value; }
            }
            public int CloudScrollYLock
            {
                get { return Sky.CloudScrollYLock.ToLSLBoolean(); }
                set { Sky.CloudScrollYLock = value != 0; }
            }
            public int CloudScrollXLock
            {
                get { return Sky.CloudScrollXLock.ToLSLBoolean(); }
                set { Sky.CloudScrollXLock = value != 0; }
            }
            public Vector3 CloudScroll
            {
                get { return Sky.CloudScroll; }
                set { Sky.CloudScroll = new WLVector2(value); }
            }
            public double CloudScale
            {
                get { return Sky.CloudScale; }
                set { Sky.CloudScale = value; }
            }
            public Vector3 CloudDetailXYDensity
            {
                get { return Sky.CloudDetailXYDensity; }
                set { Sky.CloudDetailXYDensity = value; }
            }
            public Quaternion BlueDensityColor
            {
                get { return Sky.BlueDensity; }
                set { Sky.BlueDensity = new WLVector4(value); }
            }
            public double CloudCoverage
            {
                get { return Sky.CloudCoverage; }
                set { Sky.CloudCoverage = value; }
            }
            public Quaternion CloudColor
            {
                get { return Sky.CloudColor; }
                set { Sky.CloudColor = new WLVector4(value); }
            }
            public double DistanceMultiplier
            {
                get { return Sky.DistanceMultiplier; }
                set { Sky.DistanceMultiplier = value; }
            }

            public Vector3 BigWaveDirection
            {
                get { return Water.BigWaveDirection; }
                set { Water.BigWaveDirection = new WLVector2(value); }
            }
            public Vector3 LittleWaveDirection
            {
                get { return Water.LittleWaveDirection; }
                set { Water.LittleWaveDirection = new WLVector2(value); }
            }
            public double BlurMultiplier
            {
                get { return Water.BlurMultiplier; }
                set { Water.BlurMultiplier = value; }
            }
            public double FresnelScale
            {
                get { return Water.FresnelScale; }
                set { Water.FresnelScale = value; }
            }
            public double FresnelOffset
            {
                get { return Water.FresnelOffset; }
                set { Water.FresnelOffset = value; }
            }
            public LSLKey WaterNormalMapTexture
            {
                get { return Water.NormalMapTexture; }
                set { Water.NormalMapTexture = value.AsUUID; }
            }
            public Vector3 ReflectionWaveletScale
            {
                get { return Water.ReflectionWaveletScale; }
                set { Water.ReflectionWaveletScale = value; }
            }
            public double RefractScaleAbove
            {
                get { return Water.RefractScaleAbove; }
                set { Water.RefractScaleAbove = value; }
            }
            public double RefractScaleBelow
            {
                get { return Water.RefractScaleBelow; }
                set { Water.RefractScaleBelow = value; }
            }
            public double UnderwaterFogModifier
            {
                get { return Water.UnderwaterFogModifier; }
                set { Water.UnderwaterFogModifier = value; }
            }
            public Color WaterColor
            {
                get { return Water.Color; }
                set { Water.Color = new Color(value); }
            }
            public double FogDensityExponent
            {
                get { return Water.FogDensityExponent; }
                set { Water.FogDensityExponent = value; }
            }

            [XmlIgnore]
            public LightShareData Defaults =>
                new LightShareData(this) { Water = WindlightWaterData.Defaults, Sky = WindlightSkyData.Defaults };
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "Lightshare")]
        public LightShareData GetWindlightScene(ScriptInstance instance)
        {
            lock (instance)
            {
                LightShareData d = new LightShareData(instance);
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.IsEstateManager(instance.Item.Owner))
                {
                    EnvironmentController envcontrol = scene.Environment;
                    d.Water = envcontrol.WaterData;
                    d.Sky = envcontrol.SkyData;

                }
                return d;
            }
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Setter, "Lightshare")]
        public void SetWindlightScene(ScriptInstance instance, LightShareData d)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.IsEstateManager(instance.Item.Owner))
                {
                    EnvironmentController envcontrol = scene.Environment;
                    envcontrol.WaterData = d.Water;
                    envcontrol.SkyData = d.Sky;
                }
            }
        }
    }
}
