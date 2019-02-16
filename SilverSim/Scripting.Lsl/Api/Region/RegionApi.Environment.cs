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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Region
{
    public partial class RegionApi
    {
        [APILevel(APIFlags.OSSL, "osSetRegionSunSettings")]
        [Description("set new region sun settings (EM, EO or RO only)")]
        public void SetRegionSunSettings(ScriptInstance instance,
            [Description("set to TRUE if region uses estate sun parameters")]
            int useEstateSun,
            [Description("set to TRUE if sun position is fixed see sunHour")]
            int isFixed,
            [Description("position of sun when set to be fixed (0-24, 0 => sunrise, 6 => midday, 12 => dusk, 18 => midnight)")]
            double sunHour)
        {
            lock (instance)
            {
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                SceneInterface scene = thisGroup.Scene;
                if (!scene.IsEstateManager(thisGroup.Owner) && !scene.Owner.EqualsGrid(thisGroup.Owner))
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0ObjectOwnerMustBeAbleToManageEstate", "{0}: Object owner must manage region.", "osSetRegionSunSettings"));
                    return;
                }

                scene.RegionSettings.IsSunFixed = isFixed != 0;
                scene.RegionSettings.UseEstateSun = useEstateSun != 0;
                scene.RegionSettings.SunPosition = sunHour.Clamp(0, 24) % 24f;
                scene.TriggerRegionSettingsChanged();
            }
        }

        [APILevel(APIFlags.OSSL, "osGetCurrentSunHour")]
        public double GetCurrentSunHour(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.Environment.ActualSunPosition;
            }
        }

        [APILevel(APIFlags.LSL, "llGetRegionDayLength")]
        public int GetRegionDayLength(ScriptInstance instance)
        {
            lock(instance)
            {
                uint secperday;
                uint daysperyear;
                instance.Part.ObjectGroup.Scene.Environment.GetSunDurationParams(out secperday, out daysperyear);
                return (int)secperday;
            }
        }

        [APILevel(APIFlags.LSL, "llGetRegionDayOffset")]
        public int GetRegionDayOffset(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetRegionMoonDirection")]
        public Vector3 GetRegionMoonDirection(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetRegionMoonRotation")]
        public Quaternion GetRegionMoonRotation(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetRegionSunDirection")]
        public Vector3 GetRegionSunDirection(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.Environment.SunDirection;
            }
        }

        [APILevel(APIFlags.LSL, "llGetRegionSunRotation")]
        public Quaternion GetRegionSunRotation(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llCloud")]
        public double Cloud(ScriptInstance instance, Vector3 offset)
        {
            return 0;
        }

        [APILevel(APIFlags.LSL, "llGround")]
        public double Ground(ScriptInstance instance, Vector3 offset)
        {
            lock (instance)
            {
                Vector3 regionPos = instance.Part.GlobalPosition + offset;
                return instance.Part.ObjectGroup.Scene.Terrain[regionPos];
            }
        }

        [APILevel(APIFlags.LSL, "llGroundContour")]
        public Vector3 GroundContour(ScriptInstance instance, Vector3 offset)
        {
            lock (instance)
            {
                Vector3 regionPos = instance.Part.GlobalPosition + offset;
                return instance.Part.ObjectGroup.Scene.Terrain.SurfaceContour(regionPos.X, regionPos.Y);
            }
        }

        [APILevel(APIFlags.LSL, "llGroundNormal")]
        public Vector3 GroundNormal(ScriptInstance instance, Vector3 offset)
        {
            lock (instance)
            {
                Vector3 regionPos = instance.Part.GlobalPosition + offset;
                return instance.Part.ObjectGroup.Scene.Terrain.SurfaceNormal(regionPos.X, regionPos.Y);
            }
        }

        [APILevel(APIFlags.LSL, "llGroundSlope")]
        public Vector3 GroundSlope(ScriptInstance instance, Vector3 offset)
        {
            lock (instance)
            {
                Vector3 regionPos = instance.Part.GlobalPosition + offset;
                return instance.Part.ObjectGroup.Scene.Terrain.SurfaceSlope(regionPos.X, regionPos.Y);
            }
        }

        [APILevel(APIFlags.LSL, "llWater")]
        public double Water(ScriptInstance instance, Vector3 offset)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.GetLocationInfoProvider().At(offset).WaterHeight;
            }
        }

        [APILevel(APIFlags.LSL, "llWind")]
        public Vector3 Wind(ScriptInstance instance, Vector3 offset)
        {
            lock (instance)
            {
                Vector3 regionPos = instance.Part.GlobalPosition + offset;
                return instance.Part.ObjectGroup.Scene.Environment.Wind[regionPos];
            }
        }

        [APILevel(APIFlags.OSSL, "osWindActiveModelPluginName")]
        public string GetWindActiveModelPluginName(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.Environment.Wind.Name;
            }
        }

        #region EEP
        [APILevel(APIFlags.LSL)]
        public const int SKY_AMBIENT = 0;
        [APILevel(APIFlags.LSL)]
        public const int SKY_TEXTURE_DEFAULTS = 1;
        [APILevel(APIFlags.LSL)]
        public const int SKY_CLOUDS = 2;
        [APILevel(APIFlags.LSL)]
        public const int SKY_DOME = 4;
        [APILevel(APIFlags.LSL)]
        public const int SKY_GAMMA = 5;
        [APILevel(APIFlags.LSL)]
        public const int SKY_GLOW = 6;
        [APILevel(APIFlags.LSL)]
        public const int SKY_LIGHT = 8;
        [APILevel(APIFlags.LSL)]
        public const int SKY_MOON = 9;
        [APILevel(APIFlags.LSL)]
        public const int SKY_PLANET = 10;
        [APILevel(APIFlags.LSL)]
        public const int SKY_REFRACTION = 11;
        [APILevel(APIFlags.LSL)]
        public const int SKY_STAR_BRIGHTNESS = 13;
        [APILevel(APIFlags.LSL)]
        public const int SKY_SUN = 14;
        [APILevel(APIFlags.LSL)]
        public const int SKY_TRACKS = 15;

        [APILevel(APIFlags.LSL)]
        public const int WATER_BLUR_MULTIPLIER = 100;
        [APILevel(APIFlags.LSL)]
        public const int WATER_FOG = 101;
        [APILevel(APIFlags.LSL)]
        public const int WATER_FRESNEL = 102;
        [APILevel(APIFlags.LSL)]
        public const int WATER_TEXTURE_DEFAULTS = 103;
        [APILevel(APIFlags.LSL)]
        public const int WATER_NORMAL_SCALE = 104;
        [APILevel(APIFlags.LSL)]
        public const int WATER_REFRACTION = 105;
        [APILevel(APIFlags.LSL)]
        public const int WATER_WAVE_DIRECTION = 106;

        [APILevel(APIFlags.LSL)]
        public const int ENVIRONMENT_DAYINFO = 200;

        [APILevel(APIFlags.LSL, "llGetEnvironment")]
        public AnArray GetEnvironment(ScriptInstance instance, Vector3 pos, AnArray param)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
