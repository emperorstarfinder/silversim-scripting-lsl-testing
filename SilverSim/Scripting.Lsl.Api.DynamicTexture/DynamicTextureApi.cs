// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using CSJ2K;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SilverSim.Scripting.Lsl.Api.DynamicTexture
{
    [ScriptApiName("DynamicTexture")]
    [Description("Dynamic Texture OSSL API")]
    [LSLImplementation]
    public class DynamicTextureApi : IScriptApi, IPlugin
    {
        /* graphics context specifically used for GetDrawStringSize */
        readonly Graphics m_FontRequestContext;
        readonly Dictionary<string, Func<string, string, Bitmap>> m_Renderers = new Dictionary<string, Func<string, string, Bitmap>>();

        public DynamicTextureApi()
        {
            m_FontRequestContext = Graphics.FromImage(new Bitmap(256, 256, PixelFormat.Format24bppRgb));
            m_Renderers.Add("image", RenderDynamicTexture.LoadImage);
            m_Renderers.Add("vector", RenderDynamicTexture.RenderTexture);
        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.OSSL, "osMovePen")]
        public string MovePen(ScriptInstance instance, string drawList, int x, int y)
        {
            return drawList + "MoveTo " + x + "," + y + ";";
        }

        [APILevel(APIFlags.OSSL, "osDrawLine")]
        public string DrawLine(ScriptInstance instance, string drawList, int startX, int startY, int endX, int endY)
        {
            return drawList + "MoveTo " + startX.ToString() + "," + startY.ToString() + ";" + 
                "LineTo " + endX.ToString() + "," + endY.ToString() + ";";
        }

        [APILevel(APIFlags.OSSL, "osDrawLine")]
        public string DrawLine(ScriptInstance instance, string drawList, int endX, int endY)
        {
            return drawList + "LineTo " + endX.ToString() + "," + endY.ToString() + ";";
        }

        [APILevel(APIFlags.OSSL, "osDrawText")]
        public string DrawText(ScriptInstance instance, string drawList, string text)
        {
            return drawList + "Text " + text + "; ";
        }

        [APILevel(APIFlags.OSSL, "osDrawEllipse")]
        public string DrawEllipse(ScriptInstance instance, string drawList, int width, int height)
        {
            return drawList + "Ellipse " + width.ToString() + "," + height.ToString() + ";";
        }

        [APILevel(APIFlags.OSSL, "osDrawRectangle")]
        public string DrawRectangle(ScriptInstance instance, string drawList, int width, int height)
        {
            return drawList + "Rectangle " + width.ToString() + "," + height.ToString() + ";";
        }

        [APILevel(APIFlags.OSSL, "osDrawFilledRectangle")]
        public string DrawFilledRectangle(ScriptInstance instance, string drawList, int width, int height)
        {
            return drawList + "FillRectangle " + width.ToString() + "," + height.ToString() + ";";
        }

        [APILevel(APIFlags.OSSL, "osDrawFilledPolygon")]
        public string DrawFilledPolygon(ScriptInstance instance, string drawList, AnArray x, AnArray y)
        {
            int xCount = x.Count;
            if (xCount != y.Count || xCount < 3)
            {
                return "";
            }
            drawList += "FillPolygon " + x[0].AsReal.ToString() + "," + y[0].AsReal.ToString();
            for (int i = 1; i < xCount; i++)
            {
                drawList += "," + x[i].AsReal.ToString() + "," + y[i].AsReal.ToString();
            }
            drawList += ";";
            return drawList;
        }

        [APILevel(APIFlags.OSSL, "osDrawPolygon")]
        public string DrawPolygon(ScriptInstance instance, string drawList, AnArray x, AnArray y)
        {
            int xCount = x.Count;
            if (xCount != y.Count || xCount < 3)
            {
                return "";
            }
            drawList += "Polygon " + x[0].AsReal.ToString() + "," + y[0].AsReal.ToString();
            for (int i = 1; i < xCount; i++)
            {
                drawList += "," + x[i].AsReal.ToString() + "," + y[i].AsReal.ToString();
            }
            drawList += ";";
            return drawList;
        }

        [APILevel(APIFlags.OSSL, "osSetFontSize")]
        public string SetFontSize(ScriptInstance instance, string drawList, int fontSize)
        {
            return drawList + "FontSize " + fontSize + ";";
        }

        [APILevel(APIFlags.OSSL, "osSetFontName")]
        public string SetFontName(ScriptInstance instance, string drawList, string fontName)
        {
            return drawList + "FontName " + fontName + ";";
        }

        [APILevel(APIFlags.OSSL, "osSetPenSize")]
        public string SetPenSize(ScriptInstance instance, string drawList, int penSize)
        {
            return drawList + "PenSize " + penSize + ";";
        }

        [APILevel(APIFlags.OSSL, "osSetPenColor")]
        [APILevel(APIFlags.OSSL, "osSetPenColour")]
        public string SetPenColor(ScriptInstance instance, string drawList, string color)
        {
            return drawList + "PenColor " + color + ";";
        }

        [APILevel(APIFlags.OSSL, "osSetPenCap")]
        public string SetPenCap(ScriptInstance instance, string drawList, string direction, string type)
        {
            return drawList + "PenCap " + direction + "," + type + ";";
        }

        [APILevel(APIFlags.OSSL, "osDrawImage")]
        public string DrawImage(ScriptInstance instance, string drawList, int width, int height, string imageUrl)
        {
            return drawList + "Image " + width + "," + height + "," + imageUrl + ";";
        }

        [APILevel(APIFlags.OSSL, "osGetDrawStringSize")]
        public Vector3 GetDrawStringSize(ScriptInstance instance, string contentType, string text, string fontName, int fontSize)
        {
            lock (instance)
            {
                using (Font myFont = new Font(fontName, fontSize))
                {
                    SizeF stringSize;
                    lock(m_FontRequestContext)
                    {
                        stringSize = m_FontRequestContext.MeasureString(text, myFont);
                        return new Vector3(stringSize.Width, stringSize.Height, 0);
                    }
                }
            }
        }

        private const int ALL_SIDES = -1;

        public const int DISP_EXPIRE = 1;
        public const int DISP_TEMP = 2;


        [APILevel(APIFlags.OSSL, "osSetDynamicTextureURL")]
        public LSLKey SetDynamicTextureURL(
            ScriptInstance instance,
            string dynamicID, string contentType, string data, string extraParams,
                                             int timer)
        {
            return AddDynamicTextureData(
                instance,
                dynamicID,
                contentType,
                data,
                extraParams,
                timer,
                false,
                DISP_TEMP | DISP_EXPIRE,
                255,
                ALL_SIDES);
        }

        [APILevel(APIFlags.OSSL, "osSetDynamicTextureURLBlend")]
        public LSLKey SetDynamicTextureURLBlend(
            ScriptInstance instance,
            string dynamicID, 
            string contentType, 
            string data, 
            string extraParams,
            int timer, 
            int alpha)
        {
            return AddDynamicTextureData(
                instance,
                dynamicID,
                contentType,
                data,
                extraParams,
                timer,
                true,
                DISP_TEMP | DISP_EXPIRE,
                (byte)alpha,
                ALL_SIDES);
        }

        [APILevel(APIFlags.OSSL, "osSetDynamicTextureURLBlendFace")]
        public LSLKey SetDynamicTextureURLBlendFace(
            ScriptInstance instance,
            string dynamicID, 
            string contentType, 
            string data, 
            string extraParams,
            int blend, 
            int disp, 
            int timer, 
            int alpha, 
            int face)
        {
            return AddDynamicTextureData(
                instance,
                dynamicID,
                contentType,
                data,
                extraParams,
                timer,
                blend != 0,
                disp,
                (byte)alpha,
                face);
        }

        [APILevel(APIFlags.OSSL, "osSetDynamicTextureData")]
        public LSLKey SetDynamicTextureData(
            ScriptInstance instance,
            string dynamicID, 
            string contentType,
            string data,
            string extraParams,
            int timer)
        {
            return AddDynamicTextureData(
                instance,
                dynamicID,
                contentType,
                data,
                extraParams,
                timer,
                false,
                DISP_TEMP | DISP_EXPIRE,
                255,
                ALL_SIDES);
        }

        [APILevel(APIFlags.OSSL, "osSetDynamicTextureDataBlend")]
        public LSLKey SetDynamicTextureDataBlend(
            ScriptInstance instance,
            string dynamicID, 
            string contentType,
            string data,
            string extraParams,
            int timer, 
            int alpha)
        {
            return AddDynamicTextureData(
                instance,
                dynamicID,
                contentType,
                data,
                extraParams,
                timer,
                true,
                DISP_TEMP | DISP_EXPIRE,
                (byte)alpha,
                ALL_SIDES);
        }

        [APILevel(APIFlags.OSSL, "osSetDynamicTextureDataBlendFace")]
        public LSLKey SetDynamicTextureDataBlendFace(
            ScriptInstance instance,
            string dynamicID,
            string contentType,
            string data,
            string extraParams,
            int blend, 
            int disp, 
            int timer,
            int alpha, 
            int face)
        {
            return AddDynamicTextureData(
                instance,
                dynamicID,
                contentType,
                data,
                extraParams,
                timer,
                blend != 0,
                disp,
                (byte)alpha,
                face);
        }

        LSLKey AddDynamicTextureData(
            ScriptInstance instance, 
            string dynamicID,
            string contentType, 
            string data,
            string extraParams, 
            int updateTimer, 
            bool setBlending, 
            int disp, 
            byte AlphaValue, 
            int face)
        {
            Func<string, string, Bitmap> renderer;
            if (!m_Renderers.TryGetValue(contentType, out renderer))
            {
                return UUID.Zero;
            }

            Bitmap frontImage = null;
            Bitmap backImage = null;
            Bitmap mergeImage = null;
            AssetData textureAsset;
            UUID textureAssetID = UUID.Zero;

            try
            {
                frontImage = renderer(data, extraParams);
                if (setBlending)
                {
                    UUID oldTexture;
                    TextureEntry te = instance.Part.TextureEntry;
                    oldTexture =(face == ALL_SIDES) ?
                        te.DefaultTexture.TextureID :
                        te[(uint)face].TextureID;

                    using (Stream js2k = instance.Part.ObjectGroup.Scene.AssetService.Data[oldTexture])
                    {
                        using (Image j2k = J2kImage.FromStream(js2k))
                        {
                            backImage = new Bitmap(j2k);
                        }
                    }
                }
                if(null != backImage)
                {
                    mergeImage = RenderDynamicTexture.BlendTextures(frontImage, backImage, false, AlphaValue);
                    textureAsset = mergeImage.ToTexture();
                }
                else
                {
                    textureAsset = frontImage.ToTexture();
                }
                textureAsset.Creator = instance.Part.Owner;
                textureAsset.Temporary = (disp & DISP_TEMP) != 0;
                instance.Part.ObjectGroup.Scene.AssetService.Store(textureAsset);
                textureAssetID = textureAsset.ID;

                if (face == ALL_SIDES)
                {
                    TextureEntry te = instance.Part.TextureEntry;
                    for (face = 0; face < TextureEntry.MAX_TEXTURE_FACES && face < instance.Part.NumberOfSides; ++face)
                    {
                        te[(uint)face].TextureID = textureAssetID;
                    }
                    instance.Part.TextureEntry = te;
                }
                else
                {
                    try
                    {
                        TextureEntry te = instance.Part.TextureEntry;
                        te[(uint)face].TextureID = textureAssetID;
                        instance.Part.TextureEntry = te;
                    }
                    catch
                    {
                        /* intentionally left empty */
                    }
                }
            }
            finally
            {
                if(frontImage != null)
                {
                    frontImage.Dispose();
                }
                if(backImage != null)
                {
                    backImage.Dispose();
                }
                if(mergeImage != null)
                {
                    mergeImage.Dispose();
                }
            }
            return textureAssetID;
        }
    }
}
