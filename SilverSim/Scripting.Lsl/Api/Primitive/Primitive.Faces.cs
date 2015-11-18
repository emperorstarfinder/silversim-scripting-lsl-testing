// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        #region Faces
        [APILevel(APIFlags.LSL, "llGetNumberOfSides")]
        public int GetNumberOfSides(ScriptInstance instance)
        {
            return instance.Part.NumberOfSides;
        }

        [APILevel(APIFlags.LSL, "llGetAlpha")]
        public double GetAlpha(ScriptInstance instance, int face)
        {
            lock (instance)
            {
                try
                {
                    TextureEntryFace te = instance.Part.TextureEntry[(uint)face];
                    return te.TextureColor.A;
                }
                catch
                {
                    return 0f;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llOffsetTexture")]
        public void OffsetTexture(ScriptInstance instance, double u, double v, int face)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llRotateTexture")]
        public void RotateTexture(ScriptInstance instance, double angle, int face)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llSetAlpha")]
        public void SetAlpha(ScriptInstance instance, double alpha, int faces)
        {
            SetLinkAlpha(instance, LINK_THIS, alpha, faces);
        }

        [APILevel(APIFlags.LSL, "llSetLinkAlpha")]
        public void SetLinkAlpha(ScriptInstance instance, int link, double alpha, int face)
        {
            if (alpha < 0) alpha = 0;
            if (alpha > 1) alpha = 1;

            if (face == ALL_SIDES)
            {
                foreach (ObjectPart part in GetLinkTargets(instance, link))
                {
                    TextureEntry te = part.TextureEntry;
                    for (face = 0; face < te.FaceTextures.Length; ++face)
                    {
                        te.FaceTextures[face].TextureColor.A = alpha;
                    }
                    part.TextureEntry = te;
                }
            }
            else
            {
                foreach (ObjectPart part in GetLinkTargets(instance, link))
                {
                    try
                    {
                        TextureEntry te = part.TextureEntry;
                        te.FaceTextures[face].TextureColor.A = alpha;
                        part.TextureEntry = te;
                    }
                    catch
                    {

                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetTexture")]
        [ForcedSleep(0.2)]
        public void SetTexture(ScriptInstance instance, string texture, int face)
        {
            SetLinkTexture(instance, LINK_THIS, texture, face);
        }

        [APILevel(APIFlags.LSL, "llSetLinkTexture")]
        [ForcedSleep(0.2)]
        public void SetLinkTexture(ScriptInstance instance, int link, string texture, int face)
        {
            UUID textureID = GetTextureAssetID(instance, texture);

            if (face == ALL_SIDES)
            {
                foreach (ObjectPart part in GetLinkTargets(instance, link))
                {
                    TextureEntry te = part.TextureEntry;
                    for (face = 0; face < te.FaceTextures.Length; ++face)
                    {
                        te.FaceTextures[face].TextureID = textureID;
                    }
                    part.TextureEntry = te;
                }
            }
            else
            {
                foreach (ObjectPart part in GetLinkTargets(instance, link))
                {
                    try
                    {
                        TextureEntry te = part.TextureEntry;
                        te.FaceTextures[face].TextureID = textureID;
                        part.TextureEntry = te;
                    }
                    catch
                    {

                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetColor")]
        public Vector3 GetColor(ScriptInstance instance, int face)
        {
            lock (instance)
            {
                TextureEntry te = instance.Part.TextureEntry;
                if (face == ALL_SIDES)
                {
                    Vector3 v = Vector3.Zero;
                    int n = 0;

                    for (face = 0; face < te.FaceTextures.Length && face < instance.Part.NumberOfSides; ++face)
                    {
                        v += te.FaceTextures[face].TextureColor.AsVector3;
                    }
                    v /= n;
                    return v;
                }
                else
                {
                    TextureEntryFace teface;
                    return (te.TryGetValue((uint)face, out teface)) ?
                        teface.TextureColor.AsVector3 :
                        Vector3.Zero;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetColor")]
        public void SetColor(ScriptInstance instance, Vector3 color, int face)
        {
            SetLinkColor(instance, LINK_THIS, color, face);
        }

        [APILevel(APIFlags.LSL, "llSetLinkColor")]
        public void SetLinkColor(ScriptInstance instance, int link, Vector3 color, int face)
        {
            if (color.X < 0) color.X = 0;
            if (color.X > 1) color.X = 1;
            if (color.Y < 0) color.Y = 0;
            if (color.Y > 1) color.Y = 1;
            if (color.Z < 0) color.Z = 0;
            if (color.Z > 1) color.Z = 1;

            if (face == ALL_SIDES)
            {
                foreach (ObjectPart part in GetLinkTargets(instance, link))
                {
                    TextureEntry te = part.TextureEntry;
                    for (face = 0; face < te.FaceTextures.Length; ++face)
                    {
                        ColorAlpha col = te.FaceTextures[face].TextureColor;
                        col.R = color.X;
                        col.G = color.Y;
                        col.B = color.Z;
                        te.FaceTextures[face].TextureColor = col;
                    }
                    part.TextureEntry = te;
                }
            }
            else
            {
                foreach (ObjectPart part in GetLinkTargets(instance, link))
                {
                    try
                    {
                        TextureEntry te = part.TextureEntry;
                        ColorAlpha col = te.FaceTextures[face].TextureColor;
                        col.R = color.X;
                        col.G = color.Y;
                        col.B = color.Z;
                        te.FaceTextures[face].TextureColor = col;
                        part.TextureEntry = te;
                    }
                    catch
                    {

                    }
                }
            }
        }
        #endregion
    }
}
