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

#pragma warning disable IDE0018, RCS1029, IDE0019

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Viewer.Messages.Land;
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Terraform
{
    [ScriptApiName("Terraform")]
    [LSLImplementation]
    [PluginName("LSL_Terraform")]
    [Description("LSL/OSSL Terraforming API")]
    public class TerraformApi : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL)]
        public const int LAND_LEVEL = 0;
        [APILevel(APIFlags.LSL)]
        public const int LAND_RAISE = 1;
        [APILevel(APIFlags.LSL)]
        public const int LAND_LOWER = 2;
        [APILevel(APIFlags.LSL)]
        public const int LAND_SMOOTH = 3;
        [APILevel(APIFlags.LSL)]
        public const int LAND_NOISE = 4;
        [APILevel(APIFlags.LSL)]
        public const int LAND_REVERT = 5;

        [APILevel(APIFlags.LSL)]
        public const int LAND_SMALL_BRUSH = 0;
        [APILevel(APIFlags.LSL)]
        public const int LAND_MEDIUM_BRUSH = 1;
        [APILevel(APIFlags.LSL)]
        public const int LAND_LARGE_BRUSH = 2;

        [APILevel(APIFlags.LSL, "llModifyLand")]
        public void ModifyLand(ScriptInstance instance, int action, int brush)
        {
            lock (instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                Vector3 pos = instance.Part.ObjectGroup.GlobalPosition;
                double duration = 0.25;
                if (action == 0)
                {
                    duration = 4.0;
                }
                var modLand = new ModifyLand()
                {
                    Height = pos.Z,
                    Size = (byte)brush,
                    Seconds = duration
                };
                var landData = new ModifyLand.Data()
                {
                    BrushSize = brush,
                    East = pos.X,
                    West = pos.X,
                    North = pos.Y,
                    South = pos.Y
                };
                modLand.ParcelData.Add(landData);

                Action<UUI, SceneInterface, ModifyLand, ModifyLand.Data> modifier;

                if (Terraforming.PaintEffects.TryGetValue((Terraforming.StandardTerrainEffect)action, out modifier))
                {
                    modifier(grp.Owner, grp.Scene, modLand, landData);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osGetTerrainHeight")]
        public double GetTerrainHeight(ScriptInstance instance, int x, int y)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (x < 0 || y < 0 || x >= scene.SizeX || y >= scene.SizeY)
                {
                    throw new LocalizedScriptErrorException(this, "CoordinateOutOfBounds", "Coordinate out of bounds");
                }
                return instance.Part.ObjectGroup.Scene.Terrain[(uint)x, (uint)y];
            }
        }

        [APILevel(APIFlags.OSSL, "osSetTerrainHeight")]
        public int SetTerrainHeight(ScriptInstance instance, int x, int y, double val)
        {
            lock (instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                if (x < 0 || y < 0 || x >= scene.SizeX || y >= scene.SizeY)
                {
                    throw new LocalizedScriptErrorException(this, "CoordinateOutOfBounds", "Coordinate out of bounds");
                }
                if (scene.CanTerraform(grp.Owner, new Vector3(x, y, 0)))
                {
                    instance.Part.ObjectGroup.Scene.Terrain[(uint)x, (uint)y] = val;
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osTerrainFlush")]
        public void TerrainFlush(ScriptInstance instance)
        {
            lock(instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                if (scene.CanTerraform(grp.Owner, grp.GlobalPosition))
                {
                    scene.Terrain.Flush();
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osSetTerrainTexture")]
        public void SetTerrainTexture(ScriptInstance instance, int level, LSLKey texture)
        {
            if (level < 0 || level > 3)
            {
                return;
            }

            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.IsEstateManager(instance.Part.Owner) || scene.IsRegionOwner(instance.Part.Owner))
                {
                    UUID textureID = instance.GetTextureAssetID(texture.ToString());

                    switch (level)
                    {
                        case 0:
                            scene.RegionSettings.TerrainTexture1 = textureID;
                            break;
                        case 1:
                            scene.RegionSettings.TerrainTexture2 = textureID;
                            break;
                        case 2:
                            scene.RegionSettings.TerrainTexture3 = textureID;
                            break;
                        case 3:
                            scene.RegionSettings.TerrainTexture4 = textureID;
                            break;

                        default:
                            break;
                    }
                    scene.TriggerRegionSettingsChanged();
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osSetTerrainTextureHeight")]
        public void SetTerrainTextureHeight(ScriptInstance instance, int corner, double low, double high)
        {
            if (corner < 0 || corner > 3)
            {
                return;
            }

            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.IsEstateManager(instance.Part.Owner) || scene.IsRegionOwner(instance.Part.Owner))
                {
                    switch (corner)
                    {
                        case 0:
                            scene.RegionSettings.Elevation1SW = low;
                            scene.RegionSettings.Elevation2SW = high;
                            break;

                        case 1:
                            scene.RegionSettings.Elevation1NW = low;
                            scene.RegionSettings.Elevation2NW = high;
                            break;

                        case 2:
                            scene.RegionSettings.Elevation1SE = low;
                            scene.RegionSettings.Elevation2SE = high;
                            break;

                        case 3:
                            scene.RegionSettings.Elevation1NE = low;
                            scene.RegionSettings.Elevation2NE = high;
                            break;

                        default:
                            break;
                    }
                    scene.TriggerRegionSettingsChanged();
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osSetRegionWaterHeight")]
        public void SetRegionWaterHeight(ScriptInstance instance, double waterheight)
        {
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.IsEstateManager(instance.Part.Owner) || scene.IsRegionOwner(instance.Part.Owner))
                {
                    scene.RegionSettings.WaterHeight = waterheight;
                    scene.TriggerRegionSettingsChanged();
                }
            }
        }
    }
}
