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
    [PluginName("LSL_DynamicTexture")]
    [LSLImplementation]
    public partial class DynamicTextureApi : IScriptApi, IPlugin
    {
        /* graphics context specifically used for GetDrawStringSize */
        private readonly Graphics m_FontRequestContext;
        private readonly Dictionary<string, Func<string, string, Bitmap>> m_Renderers = new Dictionary<string, Func<string, string, Bitmap>>();
        private readonly Dictionary<string, Action<Bitmap, string, string>> m_ModifyRenderers = new Dictionary<string, Action<Bitmap, string, string>>();

        public DynamicTextureApi()
        {
            m_FontRequestContext = Graphics.FromImage(new Bitmap(256, 256, PixelFormat.Format24bppRgb));
            m_Renderers.Add("image", RenderDynamicTexture.LoadImage);
            m_Renderers.Add("vector", RenderDynamicTexture.RenderTexture);
            m_ModifyRenderers.Add("vector", RenderDynamicTexture.RenderTexture);
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.OSSL, "osGetDrawStringSize")]
        public Vector3 GetDrawStringSize(ScriptInstance instance, string contentType, string text, string fontName, int fontSize)
        {
            lock (instance)
            {
                using (var myFont = new Font(fontName, fontSize))
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
                                             int timer) =>
            AddDynamicTextureData(
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

        [APILevel(APIFlags.OSSL, "osSetDynamicTextureURLBlend")]
        public LSLKey SetDynamicTextureURLBlend(
            ScriptInstance instance,
            string dynamicID,
            string contentType,
            string data,
            string extraParams,
            int timer,
            int alpha) =>
            AddDynamicTextureData(
                instance,
                dynamicID,
                contentType,
                data,
                extraParams,
                timer,
                alpha < 256,
                DISP_TEMP | DISP_EXPIRE,
                (byte)alpha.Clamp(0, 255),
                ALL_SIDES);

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
            int face) =>
            AddDynamicTextureData(
                instance,
                dynamicID,
                contentType,
                data,
                extraParams,
                timer,
                blend != 0,
                disp,
                (byte)alpha.Clamp(0, 255),
                face);

        [APILevel(APIFlags.OSSL, "osSetDynamicTextureData")]
        public LSLKey SetDynamicTextureData(
            ScriptInstance instance,
            string dynamicID,
            string contentType,
            string data,
            string extraParams,
            int timer) =>
            AddDynamicTextureData(
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

        [APILevel(APIFlags.OSSL, "osSetDynamicTextureDataFace")]
        public LSLKey SetDynamicTextureDataFace(
            ScriptInstance instance,
            string dynamicID,
            string contentType,
            string data,
            string extraParams,
            int timer,
            int face) =>
            AddDynamicTextureData(
                instance,
                dynamicID,
                contentType,
                data,
                extraParams,
                timer,
                false,
                DISP_TEMP | DISP_EXPIRE,
                255,
                face);

        [APILevel(APIFlags.OSSL, "osSetDynamicTextureDataBlend")]
        public LSLKey SetDynamicTextureDataBlend(
            ScriptInstance instance,
            string dynamicID,
            string contentType,
            string data,
            string extraParams,
            int timer,
            int alpha) =>
            AddDynamicTextureData(
                instance,
                dynamicID,
                contentType,
                data,
                extraParams,
                timer,
                alpha < 256,
                DISP_TEMP | DISP_EXPIRE,
                (byte)alpha.Clamp(0, 255),
                ALL_SIDES);

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
            int face) =>
            AddDynamicTextureData(
                instance,
                dynamicID,
                contentType,
                data,
                extraParams,
                timer,
                blend != 0,
                disp,
                (byte)alpha.Clamp(0, 255),
                face);

        private LSLKey AddDynamicTextureData(
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
            Func<string, string, Bitmap> renderer = null;
            Action<Bitmap, string, string> modifyRenderer = null;

            if (string.IsNullOrEmpty(dynamicID))
            {
                if (!m_Renderers.TryGetValue(contentType, out renderer))
                {
                    return UUID.Zero;
                }
            }
            else if (!m_ModifyRenderers.TryGetValue(contentType, out modifyRenderer))
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
                if (renderer != null)
                {
                    frontImage = renderer(data, extraParams);
                }
                else if(modifyRenderer != null)
                {
                    AssetData updatedata;
                    if(!instance.Part.ObjectGroup.AssetService.TryGetValue(instance.GetTextureAssetID(dynamicID), out updatedata) ||
                        updatedata.Type != AssetType.Texture)
                    {
                        return UUID.Zero;
                    }

                    using (Stream js2k = updatedata.InputStream)
                    {
                        using (Image j2k = J2kImage.FromStream(updatedata.InputStream))
                        {
                            frontImage = new Bitmap(j2k);
                        }
                    }
                    modifyRenderer(frontImage, data, extraParams);
                }

                if (setBlending)
                {
                    UUID oldTexture;
                    TextureEntry te = instance.Part.TextureEntry;
                    oldTexture = (face == ALL_SIDES) ?
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
                if(backImage != null)
                {
                    mergeImage = RenderDynamicTexture.BlendTextures(frontImage, backImage, false, AlphaValue);
                    textureAsset = mergeImage.ToTexture();
                }
                else
                {
                    textureAsset = frontImage.ToTexture();
                }
                textureAsset.ID = UUID.Random;
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
                frontImage?.Dispose();
                backImage?.Dispose();
                mergeImage?.Dispose();
            }
            return textureAssetID;
        }
    }
}
