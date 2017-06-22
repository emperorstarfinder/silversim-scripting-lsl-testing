using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl.Api.Primitive;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SilverSim.Scripting.Lsl.Api.Properties
{
    [LSLImplementation]
    [ScriptApiName("FaceInventoryProperties")]
    [Description("FaceInventory Properties API")]
    public class FaceProperties : IPlugin, IScriptApi
    {
        public const int ALL_SIDES = -1;

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APIExtension(APIExtension.Properties, "normalmap")]
        [APIDisplayName("normalmap")]
        [APIIsVariableType]
        [APIAccessibleMembers(
            "Texture",
            "Repeats",
            "Offset",
            "Rotation")]
        [Serializable]
        [APICloneOnAssignment]
        public class NormalMap
        {
            public LSLKey Texture = PrimitiveApi.TEXTURE_BLANK;
            public Vector3 Repeats = Vector3.One;
            public Vector3 Offset;
            public double Rotation;

            public NormalMap()
            {
            }

            public NormalMap(NormalMap m)
            {
                Texture = m.Texture;
                Offset = m.Offset;
                Repeats = m.Repeats;
                Rotation = m.Rotation;
            }
        }

        [APIExtension(APIExtension.Properties, "specularmap")]
        [APIDisplayName("specularmap")]
        [APIIsVariableType]
        [APIAccessibleMembers(
            "Texture",
            "Repeats",
            "Offset",
            "Rotation",
            "Color",
            "Alpha",
            "Glossiness",
            "Environment")]
        [Serializable]
        [APICloneOnAssignment]
        public class SpecularMap
        {
            public LSLKey Texture = PrimitiveApi.TEXTURE_BLANK;
            public Vector3 Repeats = Vector3.One;
            public Vector3 Offset;
            public double Rotation;
            public Vector3 Color = Vector3.One;
            public double Alpha = 1;
            public int Glossiness = (int)(0.2f * 255);
            public int Environment;

            public SpecularMap()
            {
            }

            public SpecularMap(SpecularMap m)
            {
                Texture = m.Texture;
                Offset = m.Offset;
                Repeats = m.Repeats;
                Rotation = m.Rotation;
                Color = m.Color;
                Alpha = m.Alpha;
                Glossiness = m.Glossiness;
                Environment = m.Environment;
            }
        }

        [APIExtension(APIExtension.Properties, "alphamode")]
        [APIDisplayName("alphamode")]
        [APIIsVariableType]
        [APIAccessibleMembers(
            "DiffuseMode",
            "MaskCutoff")]
        [Serializable]
        [APICloneOnAssignment]
        public class AlphaMode
        {
            public int DiffuseMode;
            public int MaskCutoff = 1;

            public AlphaMode()
            {
            }

            public AlphaMode(AlphaMode m)
            {
                DiffuseMode = m.DiffuseMode;
                MaskCutoff = m.MaskCutoff;
            }
        }

        [APIExtension(APIExtension.Properties, "linkface")]
        [APIDisplayName("linkface")]
        [APIIsVariableType]
        [APIAccessibleMembers(
            "Texture",
            "TextureOffset",
            "TextureScale",
            "TextureRotation",
            "Color",
            "Alpha",
            "Bump",
            "Shiny",
            "FullBright",
            "TexGen",
            "Flow",
            "NormalMap",
            "SpecularMap",
            "AlphaMode")]
        [ImplementsCustomTypecasts]
        [Serializable]
        public class TextureFace
        {
            [NonSerialized]
            [XmlIgnore]
            public WeakReference<ScriptInstance> WeakInstance;
            [NonSerialized]
            [XmlIgnore]
            public List<WeakReference<ObjectPart>> WeakParts = new List<WeakReference<ObjectPart>>();
            public int[] LinkNumbers;
            public int FaceNumber;

            public TextureFace()
            {
                LinkNumbers = new int[0];
            }

            public TextureFace(ScriptInstance instance, ObjectPart[] parts, int[] linkNumbers, int faceNumber)
            {
                WeakInstance = new WeakReference<ScriptInstance>(instance);
                foreach (ObjectPart part in parts)
                {
                    WeakParts.Add(new WeakReference<ObjectPart>(part));
                }
                LinkNumbers = linkNumbers;
                FaceNumber = faceNumber;
            }

            public void RestoreFromSerialization(ScriptInstance instance)
            {
                WeakInstance = new WeakReference<ScriptInstance>(instance);
                var parts = new List<WeakReference<ObjectPart>>();
                foreach (int linkNumber in LinkNumbers)
                {
                    if (linkNumber == PrimitiveApi.LINK_THIS)
                    {
                        parts.Add(new WeakReference<ObjectPart>(instance.Part));
                    }
                    else
                    {
                        ObjectPart part;
                        if (instance.Part.ObjectGroup.TryGetValue(linkNumber, out part))
                        {
                            parts.Add(new WeakReference<ObjectPart>(part));
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                WeakParts = parts;
            }

            private T With<T>(Func<ObjectPart, T> getter)
            {
                ScriptInstance instance;
                ObjectPart part;
                if (WeakInstance != null && WeakInstance.TryGetTarget(out instance))
                {
                    if (WeakParts.Count == 1 && WeakParts[0].TryGetTarget(out part))
                    {
                        lock (instance)
                        {
                            return getter(part);
                        }
                    }
                    else if (WeakParts.Count > 1)
                    {
                        throw new LocalizedScriptErrorException(this, "MultipleLinksCannotBeRead", "Multiple links cannot be read.");
                    }
                    else
                    {
                        throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "linkface");
                    }
                }
                else
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "linkface");
                }
            }

            private T With<T>(Func<Material, T> getter) => With(getter, default(T));

            private T With<T>(Func<Material, T> getter, T defvalue)
            {
                ScriptInstance instance;
                ObjectPart part;
                if (WeakInstance != null && WeakInstance.TryGetTarget(out instance))
                {
                    if (WeakParts.Count == 1 && WeakParts[0].TryGetTarget(out part))
                    {
                        lock (instance)
                        {
                            try
                            {
                                TextureEntryFace face = part.TextureEntry[(uint)FaceNumber];
                                Material mat;
                                try
                                {
                                    mat = part.ObjectGroup.Scene.GetMaterial(face.MaterialID);
                                }
                                catch
                                {
                                    mat = new Material();
                                }
                                return getter(mat);
                            }
                            catch
                            {
                                return defvalue;
                            }
                        }
                    }
                    else if (WeakParts.Count > 1)
                    {
                        throw new LocalizedScriptErrorException(this, "MultipleLinksCannotBeRead", "Multiple links cannot be read.");
                    }
                    else
                    {
                        throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "linkface");
                    }
                }
                else
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "linkface");
                }
            }

            private T With<T>(Func<TextureEntryFace, T> getter) => With(getter, default(T));

            private T With<T>(Func<TextureEntryFace, T> getter, T defvalue)
            {
                ScriptInstance instance;
                ObjectPart part;
                if (WeakInstance != null && WeakInstance.TryGetTarget(out instance))
                {
                    if (WeakParts.Count == 1 && WeakParts[0].TryGetTarget(out part))
                    {
                        lock (instance)
                        {
                            try
                            {
                                return getter(part.TextureEntry[(uint)FaceNumber]);
                            }
                            catch
                            {
                                return defvalue;
                            }
                        }
                    }
                    else if (WeakParts.Count > 1)
                    {
                        throw new LocalizedScriptErrorException(this, "MultipleLinksCannotBeRead", "Multiple links cannot be read.");
                    }
                    else
                    {
                        throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "linkface");
                    }
                }
                else
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "linkface");
                }
            }

            private void With<T>(Action<ObjectPart, T> setter, T value)
            {
                ScriptInstance instance;
                ObjectPart part;
                if (WeakInstance != null && WeakInstance.TryGetTarget(out instance))
                {
                    foreach (WeakReference<ObjectPart> weakPart in WeakParts)
                    {
                        if (weakPart.TryGetTarget(out part))
                        {
                            lock (instance)
                            {
                                setter(part, value);
                            }
                        }
                    }
                }
                else
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "linkface");
                }
            }

            private void With<T>(Action<TextureEntryFace, T> setter, T value)
            {
                With((ScriptInstance instance, TextureEntryFace face, T v) => setter(face, v), value);
            }

            private void With<T>(Action<ScriptInstance, TextureEntryFace, T> setter, T value)
            {
                ScriptInstance instance;
                ObjectPart part;
                if (WeakInstance != null && WeakInstance.TryGetTarget(out instance))
                {
                    foreach (WeakReference<ObjectPart> weakPart in WeakParts)
                    {
                        if (weakPart.TryGetTarget(out part))
                        {
                            lock (instance)
                            {
                                if (FaceNumber == ALL_SIDES)
                                {
                                    TextureEntry te = part.TextureEntry;
                                    for (int face = 0; face < TextureEntry.MAX_TEXTURE_FACES && face < part.NumberOfSides; ++face)
                                    {
                                        setter(instance, te[(uint)face], value);
                                    }
                                    part.TextureEntry = te;
                                }
                                else
                                {
                                    TextureEntry te = part.TextureEntry;
                                    TextureEntryFace face = te[(uint)FaceNumber];
                                    setter(instance, face, value);
                                    part.TextureEntry = te;
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "linkface");
                }
            }

            private void With<T>(Action<Material, T> setter, T value)
            {
                With((ScriptInstance instance, TextureEntryFace face, T v) =>
                {
                    SceneInterface scene = instance.Part.ObjectGroup.Scene;
                    Material mat;
                    try
                    {
                        mat = scene.GetMaterial(face.MaterialID);
                    }
                    catch
                    {
                        mat = new Material();
                    }
                    setter(mat, v);
                    mat.MaterialID = UUID.Random;
                    scene.StoreMaterial(mat);
                    face.MaterialID = mat.MaterialID;
                }, value);
            }

            [XmlIgnore]
            public LSLKey Texture
            {
                get { return With((TextureEntryFace f) => new LSLKey(f.TextureID), new LSLKey()); }
                set
                {
                    UUID textureID = UUID.Zero;
                    ScriptInstance actInstance;
                    if (WeakInstance != null && WeakInstance.TryGetTarget(out actInstance))
                    {
                        textureID = actInstance.GetTextureAssetID(value.ToString());
                    }
                    With((ScriptInstance instance, TextureEntryFace f, LSLKey texture) => f.TextureID = texture, textureID);
                }
            }

            [XmlIgnore]
            public Vector3 TextureOffset
            {
                get { return With((TextureEntryFace f) => new Vector3(f.OffsetU, f.OffsetV, 0)); }
                set
                {
                    With((TextureEntryFace f, Vector3 v) =>
                    {
                        f.OffsetU = (float)v.X;
                        f.OffsetV = (float)v.Y;
                    }, value);
                }
            }

            [XmlIgnore]
            public Vector3 TextureScale
            {
                get { return With((TextureEntryFace f) => new Vector3(f.RepeatU, f.RepeatV, 0)); }
                set
                {
                    With((TextureEntryFace f, Vector3 v) =>
                    {
                        f.RepeatU = (float)v.X;
                        f.RepeatV = (float)v.Y;
                    }, value);
                }
            }

            [XmlIgnore]
            public double TextureRotation
            {
                get { return With((TextureEntryFace f) => f.Rotation); }
                set { With((TextureEntryFace f, float v) => f.Rotation = v, (float)value); }
            }

            [XmlIgnore]
            public Vector3 Color
            {
                get
                {
                    if(FaceNumber == ALL_SIDES)
                    {
                        return With((ObjectPart p) =>
                        {
                            Vector3 v = Vector3.Zero;
                            int n = 0;

                            TextureEntry entry = p.TextureEntry;
                            for (int face = 0; face < TextureEntry.MAX_TEXTURE_FACES && face < p.NumberOfSides; ++face)
                            {
                                v += entry[(uint)face].TextureColor.AsVector3;
                                ++n;
                            }
                            v /= n;
                            return v;
                        });
                    }
                    else
                    {
                        return With((TextureEntryFace f) => f.TextureColor);
                    }
                }
                set
                {
                    With((TextureEntryFace f, Vector3 v) =>
                    {
                        ColorAlpha c = f.TextureColor;
                        c.R = v.X;
                        c.G = v.Y;
                        c.B = v.Z;
                        f.TextureColor = c;
                    }, value);
                }
            }

            [XmlIgnore]
            public double Alpha
            {
                get { return With((TextureEntryFace f) => f.TextureColor.A); }
                set { With((TextureEntryFace f, double v) => f.TextureColor.A = v, value); }
            }

            [XmlIgnore]
            public int Bump
            {
                get { return With((TextureEntryFace f) => (int)f.Bump); }
                set
                {
                    if (value >= 0 && value <= (int)Bumpiness.Weave)
                    {
                        With((TextureEntryFace f, Bumpiness v) => f.Bump = v, (Bumpiness)value);
                    }
                }
            }

            [XmlIgnore]
            public int Shiny
            {
                get { return With((TextureEntryFace f) => (int)f.Shiny); }
                set
                {
                    if (value >= 0 && value <= (int)Shininess.High)
                    {
                        With((TextureEntryFace f, Shininess v) => f.Shiny = v, (Shininess)value);
                    }
                }
            }

            [XmlIgnore]
            public int FullBright
            {
                get { return With((TextureEntryFace f) => f.FullBright.ToLSLBoolean()); }
                set { With((TextureEntryFace f, bool v) => f.FullBright = v, value != 0); }
            }

            [XmlIgnore]
            public int TexGen
            {
                get { return With((TextureEntryFace f) => (int)f.TexMapType); }
                set
                {
                    if (value >= 0 && value <= 1)
                    {
                        With((TextureEntryFace f, MappingType v) => f.TexMapType = v, (MappingType)value);
                    }
                }
            }

            [XmlIgnore]
            public double Glow
            {
                get { return With((TextureEntryFace f) => f.Glow); }
                set { With((TextureEntryFace f, float v) => f.Glow = v, (float)value); }
            }

            [XmlIgnore]
            public NormalMap NormalMap
            {
                get
                {
                    return With((Material m) => new NormalMap
                    {
                        Texture = m.NormMap,
                        Offset = new Vector3(m.NormOffsetX, m.NormOffsetY, 0) / Material.MATERIALS_MULTIPLIER,
                        Repeats = new Vector3(m.NormRepeatX, m.NormRepeatY, 0) / Material.MATERIALS_MULTIPLIER,
                        Rotation = m.NormRepeatY / Material.MATERIALS_MULTIPLIER
                    });
                }
                set
                {
                    UUID textureID = UUID.Zero;
                    ScriptInstance actInstance;
                    if (WeakInstance != null && WeakInstance.TryGetTarget(out actInstance))
                    {
                        value.Texture = actInstance.GetTextureAssetID(value.Texture.ToString());
                    }

                    With((Material mat, NormalMap m) =>
                    {
                        m.Offset *= Material.MATERIALS_MULTIPLIER;
                        m.Repeats *= Material.MATERIALS_MULTIPLIER;
                        mat.NormMap = m.Texture;
                        mat.NormOffsetX = (int)Math.Round(m.Offset.X);
                        mat.NormOffsetY = (int)Math.Round(m.Offset.Y);
                        mat.NormRepeatX = (int)Math.Round(m.Repeats.X);
                        mat.NormRepeatY = (int)Math.Round(m.Repeats.Y);
                    }, value);
                }
            }

            [XmlIgnore]
            public SpecularMap SpecularMap
            {
                get
                {
                    return With((Material m) => new SpecularMap
                    {
                        Texture = m.SpecMap,
                        Offset = new Vector3(m.SpecOffsetX, m.SpecOffsetY, 0) / Material.MATERIALS_MULTIPLIER,
                        Repeats = new Vector3(m.SpecRepeatX, m.SpecRepeatY, 0) / Material.MATERIALS_MULTIPLIER,
                        Color = m.SpecColor,
                        Alpha = m.SpecColor.A,
                        Rotation = m.SpecRotation / Material.MATERIALS_MULTIPLIER,
                        Environment = m.EnvIntensity,
                        Glossiness = m.SpecExp
                    });
                }
                set
                {
                    UUID textureID = UUID.Zero;
                    ScriptInstance actInstance;
                    if (WeakInstance != null && WeakInstance.TryGetTarget(out actInstance))
                    {
                        value.Texture = actInstance.GetTextureAssetID(value.Texture.ToString());
                    }
                    value.Offset *= Material.MATERIALS_MULTIPLIER;
                    value.Repeats *= Material.MATERIALS_MULTIPLIER;
                    value.Rotation %= Math.PI * 2;
                    value.Rotation *= Material.MATERIALS_MULTIPLIER;
                    value.Environment = value.Environment.Clamp(0, 255);
                    value.Glossiness = value.Glossiness.Clamp(0, 255);

                    With((Material mat, SpecularMap v) =>
                    {
                        mat.SpecMap = value.Texture;
                        mat.SpecColor = new ColorAlpha(value.Color, value.Alpha);
                        mat.SpecOffsetX = (int)Math.Round(value.Offset.X);
                        mat.SpecOffsetY = (int)Math.Round(value.Offset.Y);
                        mat.SpecRepeatX = (int)Math.Round(value.Repeats.X);
                        mat.SpecRepeatY = (int)Math.Round(value.Repeats.Y);
                        mat.EnvIntensity = v.Environment;
                        mat.SpecExp = v.Glossiness;
                    }, value);
                }
            }

            [XmlIgnore]
            public AlphaMode AlphaMode
            {
                get
                {
                    return With((Material m) => new AlphaMode
                    {
                        DiffuseMode = m.DiffuseAlphaMode,
                        MaskCutoff = m.AlphaMaskCutoff
                    });
                }
                set
                {
                    value.DiffuseMode = value.DiffuseMode.Clamp(0, 3);
                    value.MaskCutoff = value.MaskCutoff.Clamp(0, 3);

                    With((Material mat, AlphaMode v) =>
                    {
                        mat.DiffuseAlphaMode = v.DiffuseMode;
                        mat.AlphaMaskCutoff = v.MaskCutoff;
                    }, value);
                }
            }

            [APIExtension(APIExtension.Properties)]
            public static implicit operator bool(TextureFace tf) =>
                tf.With((TextureEntryFace f) => true);
        }
    }
}
