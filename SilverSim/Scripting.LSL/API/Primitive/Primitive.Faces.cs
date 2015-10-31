// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using SilverSim.Types.Primitive;
using SilverSim.Scripting.Common;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        #region Faces
        [APILevel(APIFlags.LSL, "llGetNumberOfSides")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        int GetNumberOfSides(ScriptInstance instance)
        {
            return instance.Part.NumberOfSides;
        }

        [APILevel(APIFlags.LSL, "llGetAlpha")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        double GetAlpha(ScriptInstance instance, int face)
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

        [APILevel(APIFlags.LSL, "llSetAlpha")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetAlpha(ScriptInstance instance, double alpha, int faces)
        {
            SetLinkAlpha(instance, LINK_THIS, alpha, faces);
        }

        [APILevel(APIFlags.LSL, "llSetLinkAlpha")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetLinkAlpha(ScriptInstance instance, int link, double alpha, int face)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetTexture(ScriptInstance instance, string texture, int face)
        {
            SetLinkTexture(instance, LINK_THIS, texture, face);
        }

        [APILevel(APIFlags.LSL, "llSetLinkTexture")]
        [ForcedSleep(0.2)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetLinkTexture(ScriptInstance instance, int link, string texture, int face)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        Vector3 GetColor(ScriptInstance instance, int face)
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
                    if (te.TryGetValue((uint)face, out teface))
                    {
                        return teface.TextureColor.AsVector3;
                    }
                    else
                    {
                        return Vector3.Zero;
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetColor")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetColor(ScriptInstance instance, Vector3 color, int face)
        {
            SetLinkColor(instance, LINK_THIS, color, face);
        }

        [APILevel(APIFlags.LSL, "llSetLinkColor")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetLinkColor(ScriptInstance instance, int link, Vector3 color, int face)
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
                        te.FaceTextures[face].TextureColor.R = color.X;
                        te.FaceTextures[face].TextureColor.G = color.Y;
                        te.FaceTextures[face].TextureColor.B = color.Z;
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
                        te.FaceTextures[face].TextureColor.R = color.X;
                        te.FaceTextures[face].TextureColor.G = color.Y;
                        te.FaceTextures[face].TextureColor.B = color.Z;
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
