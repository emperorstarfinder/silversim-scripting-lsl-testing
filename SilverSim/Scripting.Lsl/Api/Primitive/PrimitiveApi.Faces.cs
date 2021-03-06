﻿// SilverSim is distributed under the terms of the
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

#pragma warning disable IDE0018, RCS1029

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Primitive;

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

        [APILevel(APIFlags.LSL, "llGetLinkNumberOfSides")]
        public int GetLinkNumberOfSides(ScriptInstance instance, int link)
        {
            if (link == LINK_THIS)
            {
                return GetNumberOfSides(instance);
            }
            else if(link < 0)
            {
                return 0;
            }
            else
            {
                if(link == 0)
                {
                    link = LINK_ROOT;
                }
                lock(instance)
                {
                    ObjectPart part;
                    return (instance.Part.ObjectGroup.TryGetValue(link, out part)) ?
                        part.NumberOfSides :
                        0;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetAlpha")]
        public double GetAlpha(ScriptInstance instance, int face)
        {
            lock (instance)
            {
                if(face >= 0 && face < instance.Part.NumberOfSides)
                { 
                    TextureEntryFace te = instance.Part.TextureEntry[(uint)face];
                    return te.TextureColor.A;
                }
                else
                {
                    return 0f;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llOffsetTexture")]
        [ForcedSleep(0.2)]
        public void OffsetTexture(ScriptInstance instance, double u, double v, int face)
        {
            var uf = (float)u.Clamp(-1, 1);
            var vf = (float)v.Clamp(-1, 1);
            lock(instance)
            {
                int numberOfSides = instance.Part.NumberOfSides;
                if (face == ALL_SIDES)
                {
                    TextureEntry te = instance.Part.TextureEntry;
                    for (face = 0; face < TextureEntry.MAX_TEXTURE_FACES && face < numberOfSides; ++face)
                    {
                        TextureEntryFace texface = te[(uint)face];
                        texface.OffsetU = uf;
                        texface.OffsetV = vf;
                    }
                    te.OptimizeDefault(numberOfSides);
                    instance.Part.TextureEntry = te;
                }
                else if(face >= 0 && face < numberOfSides)
                {
                    TextureEntry te = instance.Part.TextureEntry;
                    TextureEntryFace texface = te[(uint)face];
                    texface.OffsetU = uf;
                    texface.OffsetV = vf;
                    te.OptimizeDefault(numberOfSides);
                    instance.Part.TextureEntry = te;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llScaleTexture")]
        [ForcedSleep(0.2)]
        public void ScaleTexture(ScriptInstance instance, double u, double v, int face)
        {
            var uf = (float)u;
            var vf = (float)v;
            lock (instance)
            {
                int numberOfSides = instance.Part.NumberOfSides;
                if (face == ALL_SIDES)
                {
                    TextureEntry te = instance.Part.TextureEntry;
                    for (face = 0; face < TextureEntry.MAX_TEXTURE_FACES && face < numberOfSides; ++face)
                    {
                        TextureEntryFace texface = te[(uint)face];
                        texface.RepeatU = uf;
                        texface.RepeatV = vf;
                    }
                    te.OptimizeDefault(numberOfSides);
                    instance.Part.TextureEntry = te;
                }
                else if(face >= 0 && face < numberOfSides)
                {
                    TextureEntry te = instance.Part.TextureEntry;
                    TextureEntryFace texface = te[(uint)face];
                    texface.RepeatU = uf;
                    texface.RepeatV = vf;
                    te.OptimizeDefault(numberOfSides);
                    instance.Part.TextureEntry = te;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llRotateTexture")]
        [ForcedSleep(0.2)]
        public void RotateTexture(ScriptInstance instance, double angle, int face)
        {
            var anglef = (float)angle;
            lock (instance)
            {
                int numberOfSides = instance.Part.NumberOfSides;
                if (face == ALL_SIDES)
                {
                    TextureEntry te = instance.Part.TextureEntry;
                    for (face = 0; face < TextureEntry.MAX_TEXTURE_FACES && face < numberOfSides; ++face)
                    {
                        te[(uint)face].Rotation = anglef;
                    }
                    te.OptimizeDefault(numberOfSides);
                    instance.Part.TextureEntry = te;
                }
                else if(face >= 0 && face < numberOfSides)
                {
                    TextureEntry te = instance.Part.TextureEntry;
                    te[(uint)face].Rotation = anglef;
                    te.OptimizeDefault(numberOfSides);
                    instance.Part.TextureEntry = te;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetAlpha")]
        public void SetAlpha(ScriptInstance instance, double alpha, int faces)
        {
            SetLinkAlpha(instance, LINK_THIS, alpha, faces);
        }

        [APILevel(APIFlags.LSL, "llSetLinkAlpha")]
        public void SetLinkAlpha(ScriptInstance instance, int link, double alpha, int face)
        {
            lock(instance)
            {
                alpha = alpha.Clamp(0f, 1f);

                if (face == ALL_SIDES)
                {
                    foreach (ObjectPart part in instance.GetLinkTargets(link))
                    {
                        int numberOfSides = part.NumberOfSides;
                        TextureEntry te = part.TextureEntry;
                        for (face = 0; face < TextureEntry.MAX_TEXTURE_FACES && face < numberOfSides; ++face)
                        {
                            ColorAlpha c = te[(uint)face].TextureColor;
                            c.A = alpha;
                            te[(uint)face].TextureColor = c;
                        }
                        te.OptimizeDefault(numberOfSides);
                        part.TextureEntry = te;
                    }
                }
                else if(face >= 0)
                {
                    foreach (ObjectPart part in instance.GetLinkTargets(link))
                    {
                        int numberOfSides = part.NumberOfSides;
                        if (face < numberOfSides)
                        {
                            TextureEntry te = part.TextureEntry;
                            ColorAlpha c = te[(uint)face].TextureColor;
                            c.A = alpha;
                            te[(uint)face].TextureColor = c;
                            te.OptimizeDefault(numberOfSides);
                            part.TextureEntry = te;
                        }
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
            lock(instance)
            {
                UUID textureID = instance.GetTextureAssetID(texture);
                if(!instance.Part.TryFetchTexture(textureID))
                {
                    return;
                }

                if (face == ALL_SIDES)
                {
                    foreach (ObjectPart part in instance.GetLinkTargets(link))
                    {
                        int numberOfSides = part.NumberOfSides;
                        TextureEntry te = part.TextureEntry;
                        for (face = 0; face < TextureEntry.MAX_TEXTURE_FACES && face < numberOfSides; ++face)
                        {
                            te[(uint)face].TextureID = textureID;
                        }
                        te.OptimizeDefault(numberOfSides);
                        part.TextureEntry = te;
                    }
                }
                else if(face >= 0)
                {
                    foreach (ObjectPart part in instance.GetLinkTargets(link))
                    {
                        int numberOfSides = instance.Part.NumberOfSides;
                        if (face < numberOfSides)
                        {
                            TextureEntry te = part.TextureEntry;
                            te[(uint)face].TextureID = textureID;
                            te.OptimizeDefault(numberOfSides);
                            part.TextureEntry = te;
                        }
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetTextureOffset")]
        public Vector3 GetTextureOffset(ScriptInstance instance, int face)
        {
            lock (instance)
            {
                TextureEntry te = instance.Part.TextureEntry;
                if (face == ALL_SIDES)
                {
                    face = 0;
                }
                TextureEntryFace teface;
                return (te.TryGetValue((uint)face, out teface)) ?
                    new Vector3(teface.OffsetU, teface.OffsetV, 0) :
                    Vector3.Zero;
            }
        }

        [APILevel(APIFlags.LSL, "llGetTextureScale")]
        public Vector3 GetTextureScale(ScriptInstance instance, int face)
        {
            lock (instance)
            {
                TextureEntry te = instance.Part.TextureEntry;
                if (face == ALL_SIDES)
                {
                    face = 0;
                }
                TextureEntryFace teface;
                return (te.TryGetValue((uint)face, out teface)) ?
                    new Vector3(teface.RepeatU, teface.RepeatV, 0) :
                    new Vector3(1.0, 1.0, 0);
            }
        }

        [APILevel(APIFlags.LSL, "llGetTexture")]
        public string GetTexture(ScriptInstance instance, int face)
        {
            lock (instance)
            {
                TextureEntry te = instance.Part.TextureEntry;
                if (face == ALL_SIDES)
                {
                    face = 0;
                }
                TextureEntryFace teface;
                return (te.TryGetValue((uint)face, out teface)) ?
                    teface.TextureID.ToString() :
                    UUID.Zero.ToString();
            }
        }

        [APILevel(APIFlags.LSL, "llGetTextureRot")]
        public double GetTextureRot(ScriptInstance instance, int face)
        {
            lock (instance)
            {
                TextureEntry te = instance.Part.TextureEntry;
                if (face == ALL_SIDES)
                {
                    face = 0;
                }
                TextureEntryFace teface;
                return (te.TryGetValue((uint)face, out teface)) ?
                    teface.Rotation :
                    0;
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

                    for (face = 0; face < TextureEntry.MAX_TEXTURE_FACES && face < instance.Part.NumberOfSides; ++face)
                    {
                        v += te[(uint)face].TextureColor.AsVector3;
                        ++n;
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
            color.X = color.X.Clamp(0f, 1f);
            color.Y = color.Y.Clamp(0f, 1f);
            color.Z = color.Z.Clamp(0f, 1f);

            lock(instance)
            {
                if (face == ALL_SIDES)
                {
                    foreach (ObjectPart part in instance.GetLinkTargets(link))
                    {
                        int numberOfSides = part.NumberOfSides;
                        TextureEntry te = part.TextureEntry;
                        for (face = 0; face < TextureEntry.MAX_TEXTURE_FACES && face < numberOfSides; ++face)
                        {
                            ColorAlpha col = te[(uint)face].TextureColor;
                            col.R = color.X;
                            col.G = color.Y;
                            col.B = color.Z;
                            te[(uint)face].TextureColor = col;
                        }
                        te.OptimizeDefault(numberOfSides);
                        part.TextureEntry = te;
                    }
                }
                else if(face >= 0)
                {
                    foreach (ObjectPart part in instance.GetLinkTargets(link))
                    {
                        int numberOfSides = part.NumberOfSides;
                        if (face < numberOfSides)
                        {
                            TextureEntry te = part.TextureEntry;
                            ColorAlpha col = te[(uint)face].TextureColor;
                            col.R = color.X;
                            col.G = color.Y;
                            col.B = color.Z;
                            te[(uint)face].TextureColor = col;
                            te.OptimizeDefault(numberOfSides);
                            part.TextureEntry = te;
                        }
                    }
                }
            }
        }
        #endregion
    }
}
