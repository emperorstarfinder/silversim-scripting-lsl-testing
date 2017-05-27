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

#pragma warning disable IDE0018

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Properties
{
    [LSLImplementation]
    [ScriptApiName("PrimProperties")]
    [Description("Prim Properties API")]
    public class PrimProperties : IScriptApi, IPlugin
    {
        public const int LINK_INVALID = -1;
        public const int LINK_THIS = -4;

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APIExtension(APIExtension.Properties, "hovertext")]
        [APIDisplayName("hovertext")]
        [APIIsVariableType]
        [APIAccessibleMembers(
            "Text",
            "Color",
            "Alpha")]
        [Serializable]
        [APICloneOnAssignment]
        public class Hovertext
        {
            public string Text;
            public Vector3 Color;
            public double Alpha;

            public Hovertext()
            {
                Text = string.Empty;
                Color = Vector3.One;
                Alpha = 0;
            }

            public Hovertext(Hovertext t)
            {
                Text = t.Text;
                Color = t.Color;
                Alpha = t.Alpha;
            }
        }

        [APIExtension(APIExtension.Properties, "pointlight")]
        [APIDisplayName("pointlight")]
        [APIIsVariableType]
        [APIAccessibleMembers(
            "Enabled",
            "Color",
            "Intensity",
            "Radius",
            "Falloff")]
        [Serializable]
        public struct Pointlight
        {
            public int Enabled;
            public Vector3 Color;
            public double Intensity;
            public double Radius;
            public double Falloff;
        }

        [APIExtension(APIExtension.Properties, "projector")]
        [APIDisplayName("projector")]
        [APIIsVariableType]
        [APIAccessibleMembers(
            "Enabled",
            "Texture",
            "FieldOfView",
            "Focus",
            "Ambience")]
        [Serializable]
        public struct Projector
        {
            public int Enabled;
            public LSLKey Texture;
            public double FieldOfView;
            public double Focus;
            public double Ambience;
        }

        [APIExtension(APIExtension.Properties, "link")]
        [APIDisplayName("link")]
        [APIIsVariableType]
        [ImplementsCustomTypecasts]
        [APIAccessibleMembers(
            "Key",
            "Owner",
            "Creator",
            "Inventory",
            "Name",
            "Desc",
            "Buoyancy",
            "PointLight",
            "Projector",
            "LocalRot",
            "LocalPos",
            "Rotation",
            "Position",
            "PhysicsShapeType",
            "Material",
            "IsPhysics",
            "IsTempOnRez",
            "IsPhantom",
            "Size",
            "Velocity",
            "HoverText",
            "AllowUnsit",
            "ScriptedSitOnly",
            "AllowInventoryDrop")]
        [Serializable]
        public sealed class Prim
        {
            [NonSerialized]
            private WeakReference<ScriptInstance> WeakInstance;

            public int LinkNumber;

            private T WithPart<T>(Func<ObjectPart, T> getter)
            {
                ScriptInstance instance;
                if (WeakInstance == null || !WeakInstance.TryGetTarget(out instance))
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                }
                lock (instance)
                {
                    if (LinkNumber == LINK_THIS)
                    {
                        return getter(instance.Part);
                    }
                    else
                    {
                        ObjectPart p;
                        if (!instance.Part.ObjectGroup.TryGetValue(LinkNumber, out p))
                        {
                            throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                        }
                        return getter(p);
                    }
                }
            }

            private T WithPart<T>(Func<ScriptInstance, ObjectPart, T> getter)
            {
                ScriptInstance instance;
                if (WeakInstance == null || !WeakInstance.TryGetTarget(out instance))
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                }
                lock (instance)
                {
                    if (LinkNumber == LINK_THIS)
                    {
                        return getter(instance, instance.Part);
                    }
                    else
                    {
                        ObjectPart p;
                        if (!instance.Part.ObjectGroup.TryGetValue(LinkNumber, out p))
                        {
                            throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                        }
                        return getter(instance, p);
                    }
                }
            }

            private void WithPart<T>(Action<ObjectPart, T> setter, T value)
            {
                ScriptInstance instance;
                if (WeakInstance == null || !WeakInstance.TryGetTarget(out instance))
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                }
                lock (instance)
                {
                    if (LinkNumber == LINK_THIS)
                    {
                        setter(instance.Part, value);
                    }
                    else
                    {
                        ObjectPart p;
                        if (!instance.Part.ObjectGroup.TryGetValue(LinkNumber, out p))
                        {
                            throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                        }
                        setter(instance.Part, value);
                    }
                }
            }

            public Prim()
            {
                LinkNumber = LINK_INVALID;
            }

            public Prim(ScriptInstance instance, int linkNumber)
            {
                WeakInstance = new WeakReference<ScriptInstance>(instance);
                LinkNumber = linkNumber;
            }

            public void RestoreFromSerialization(ScriptInstance instance)
            {
                WeakInstance = new WeakReference<ScriptInstance>(instance);
            }

            public LSLKey Creator => WithPart((ObjectPart p) => p.Creator.ID);

            public LSLKey Owner => WithPart((ObjectPart p) => p.Owner.ID);

            public LSLKey Key => WithPart((ObjectPart p) => p.ID);

            public Vector3 Velocity
            {
                get { return WithPart((ObjectPart p) => p.Velocity); }
                set { WithPart((ObjectPart p, Vector3 v) => p.Velocity = v, value); }
            }

            public Vector3 AngularVelocity
            {
                get { return WithPart((ObjectPart p) => p.AngularVelocity); }
                set { WithPart((ObjectPart p, Vector3 v) => p.AngularVelocity = v, value); }
            }

            public InventoryProperties.PrimInventory Inventory =>
                WithPart((ScriptInstance instance, ObjectPart p) => new InventoryProperties.PrimInventory(instance, p));

            public Hovertext HoverText
            {
                get
                {
                    return WithPart((ObjectPart p) =>
                    {
                        ObjectPart.TextParam text = p.Text;
                        return new Hovertext()
                        {
                            Text = text.Text,
                            Color = text.TextColor,
                            Alpha = text.TextColor.A
                        };
                    });
                }
                set
                {
                    WithPart((ObjectPart p, ObjectPart.TextParam t) => p.Text = t, new ObjectPart.TextParam
                    {
                        Text = value.Text,
                        TextColor = new ColorAlpha(value.Color, value.Alpha)
                    });
                }
            }

            public Pointlight PointLight
            {
                get
                {
                    return WithPart((ObjectPart p) =>
                    {
                        ObjectPart.PointLightParam light = p.PointLight;
                        return new Pointlight()
                        {
                            Enabled = light.IsLight.ToLSLBoolean(),
                            Color = light.LightColor,
                            Intensity = light.Intensity,
                            Radius = light.Radius,
                            Falloff = light.Falloff
                        };
                    });
                }
                set
                {
                    WithPart((ObjectPart p, ObjectPart.PointLightParam l) => p.PointLight = l, new ObjectPart.PointLightParam
                    {
                        IsLight = value.Enabled != 0,
                        LightColor = new Color(value.Color),
                        Intensity = value.Intensity,
                        Radius = value.Radius,
                        Falloff = value.Falloff
                    });
                }
            }

            public Projector Projector
            {
                get
                {
                    return WithPart((ObjectPart p) =>
                    {
                        ObjectPart.ProjectionParam param = p.Projection;
                        return new Projector
                        {
                            Enabled = param.IsProjecting.ToLSLBoolean(),
                            Texture = param.ProjectionTextureID,
                            FieldOfView = param.ProjectionFOV,
                            Focus = param.ProjectionFocus,
                            Ambience = param.ProjectionAmbience
                        };
                    });
                }
                set
                {
                    ScriptInstance instance;
                    if(WeakInstance.TryGetTarget(out instance))
                    {
                        value.Texture = instance.GetTextureAssetID(value.Texture.ToString());
                    }
                    WithPart((ObjectPart p, Projector param) =>
                    {
                        p.Projection = new ObjectPart.ProjectionParam
                        {
                            IsProjecting = param.Enabled != 0,
                            ProjectionTextureID = param.Texture,
                            ProjectionFOV = param.FieldOfView,
                            ProjectionFocus = param.Focus,
                            ProjectionAmbience = param.Ambience
                        };
                    }, value);
                }
            }

            public string Desc
            {
                get { return WithPart((ObjectPart p) => p.Description); }
                set { WithPart((ObjectPart p, string d) => p.Description = d, value); }
            }

            public string Name
            {
                get { return WithPart((ObjectPart p) => p.Name); }
                set { WithPart((ObjectPart p, string d) => p.Name = d, value); }
            }

            public int AllowInventoryDrop
            {
                get { return WithPart((ObjectPart p) => p.IsAllowedDrop.ToLSLBoolean()); }
                set { WithPart((ObjectPart p, bool v) => p.IsAllowedDrop = v, value != 0); }
            }

            public Quaternion LocalRot
            {
                get { return WithPart((ObjectPart p) => p.LocalRotation); }
                set { WithPart((ObjectPart p, Quaternion q) => p.LocalRotation = q, value); }
            }

            public Vector3 Size
            {
                get { return WithPart((ObjectPart p) => p.Size); }
                set { WithPart((ObjectPart p, Vector3 v) => p.Size = v, value); }
            }

            public Vector3 LocalPos
            {
                get { return WithPart((ObjectPart p) => p.LocalPosition); }
                set { WithPart((ObjectPart p, Vector3 v) => p.LocalPosition = v, value); }
            }

            public Quaternion Rotation
            {
                get { return WithPart((ObjectPart p) => p.Rotation); }
                set { WithPart((ObjectPart p, Quaternion q) => p.Rotation = q, value); }
            }

            public Vector3 Position
            {
                get { return WithPart((ObjectPart p) => p.Position); }
                set { WithPart((ObjectPart p, Vector3 v) => p.Position = v, value); }
            }

            public int PhysicsShapeType
            {
                get { return (int)WithPart((ObjectPart p) => p.PhysicsShapeType); }
                set
                {
                    if (value >= (int)PrimitivePhysicsShapeType.Prim && value <= (int)PrimitivePhysicsShapeType.Convex)
                    {
                        WithPart((ObjectPart p, PrimitivePhysicsShapeType s) => p.PhysicsShapeType = s,
                            (PrimitivePhysicsShapeType)value);
                    }
                }
            }

            public int Material
            {
                get { return (int)WithPart((ObjectPart p) => p.Material); }
                set
                {
                    if (value >= (int)PrimitiveMaterial.Stone && value <= (int)PrimitiveMaterial.Light)
                    {
                        WithPart((ObjectPart p, PrimitiveMaterial m) => p.Material = m, (PrimitiveMaterial)value);
                    }
                }
            }

            public int IsPhantom
            {
                get { return WithPart((ObjectPart p) => p.IsPhantom.ToLSLBoolean()); }
                set { WithPart((ObjectPart p, bool v) => p.IsPhantom = v, value != 0); }
            }

            public int IsPhysics
            {
                get { return WithPart((ObjectPart p) => p.IsPhysics.ToLSLBoolean()); }
                set { WithPart((ObjectPart p, bool v) => p.IsPhysics = v, value != 0); }
            }

            public int IsTempOnRez
            {
                get { return WithPart((ObjectPart p) => p.ObjectGroup.IsTempOnRez.ToLSLBoolean()); }
                set { WithPart((ObjectPart p, bool v) => p.ObjectGroup.IsTempOnRez = v, value != 0); }
            }

            public int IsVolumeDetect
            {
                get { return WithPart((ObjectPart p) => p.ObjectGroup.IsVolumeDetect.ToLSLBoolean()); }
                set { WithPart((ObjectPart p, bool v) => p.ObjectGroup.IsVolumeDetect = v, value != 0); }
            }

            public int AllowUnsit
            {
                get { return WithPart((ObjectPart p) => p.AllowUnsit.ToLSLBoolean()); }
                set { WithPart((ObjectPart p, bool v) => p.AllowUnsit = v, value != 0); }
            }

            public int ScriptedSitOnly
            {
                get { return WithPart((ObjectPart p) => p.IsScriptedSitOnly.ToLSLBoolean()); }
                set { WithPart((ObjectPart p, bool v) => p.IsScriptedSitOnly = v, value != 0); }
            }

            public double Buoyancy
            {
                get { return WithPart((ObjectPart p) => p.Buoyancy); }
                set { WithPart((ObjectPart p, double v) => p.Buoyancy = v, value); }
            }

            [APIExtension(APIExtension.Properties)]
            public static implicit operator bool(Prim c) => c.LinkNumber > 0 || c.LinkNumber == LINK_THIS;

            [APIExtension(APIExtension.Properties)]
            public static implicit operator int(Prim c) => c.LinkNumber;

            public FaceProperties.TextureFace this[int faceNo]
            {
                get
                {
                    return WithPart((ScriptInstance instance, ObjectPart p) =>
                    {
                        if(faceNo == FaceProperties.ALL_SIDES)
                        {
                            return new FaceProperties.TextureFace(instance, p, LinkNumber, faceNo);
                        }
                        else if(faceNo >= 0 && faceNo < p.NumberOfSides)
                        {
                            return new FaceProperties.TextureFace(instance, p, LinkNumber, faceNo);
                        }
                        else
                        {
                            return new FaceProperties.TextureFace();
                        }
                    });
                }
            }
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "this")]
        public Prim GetThis(ScriptInstance instance)
        {
            lock (instance)
            {
                return new Prim(instance, instance.Part.LinkNumber);
            }
        }

        [APIExtension(APIExtension.Properties, "linkset")]
        [APIDisplayName("linkset")]
        public class LinkSetAccessor
        {
            private readonly ScriptInstance Instance;

            public LinkSetAccessor(ScriptInstance instance)
            {
                Instance = instance;
            }

            public Prim this[int linkno]
            {
                get
                {
                    lock (Instance)
                    {
                        return new Prim(Instance, linkno);
                    }
                }
            }

            public Prim this[string name]
            {
                get
                {
                    lock (Instance)
                    {
                        foreach (ObjectPart p in Instance.Part.ObjectGroup.ValuesByKey1)
                        {
                            if (p.Name == name)
                            {
                                return new Prim(Instance, p.LinkNumber);
                            }
                        }
                    }
                    return new Prim();
                }
            }
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "LinkSet")]
        public LinkSetAccessor GetLink(ScriptInstance instance)
        {
            lock (instance)
            {
                return new LinkSetAccessor(instance);
            }
        }

        [APIExtension(APIExtension.Properties, "HoverText")]
        public Hovertext GetHovertext(ScriptInstance instance, string text, Vector3 color, double alpha) => new Hovertext
        {
            Text = text,
            Color = color,
            Alpha = alpha
        };

        [APIExtension(APIExtension.Properties, "PointLight")]
        public Pointlight GetPointlight(ScriptInstance instance, int enabled, Vector3 color, double intensity, double radius, double falloff) => new Pointlight
        {
            Enabled = enabled,
            Color = color,
            Intensity = intensity,
            Radius = radius,
            Falloff = falloff
        };

        [APIExtension(APIExtension.Properties, "Projector")]
        public Projector GetProjector(ScriptInstance instance, int enabled, LSLKey texture, double fov, double focus, double ambience) => new Projector
        {
            Enabled = enabled,
            Texture = texture,
            FieldOfView = fov,
            Focus = focus,
            Ambience = ambience
        };

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "Color")]
        [APIDisplayName("Color")]
        public class ColorSet
        {
            // Red tones
            public Vector3 IndianRed => new Vector3(205, 92, 92) / 255;
            public Vector3 LightCoral => new Vector3(240, 128, 128) / 255;
            public Vector3 Salmon => new Vector3(250, 128, 114) / 255;
            public Vector3 DarkSalmon => new Vector3(233, 150, 122) / 255;
            public Vector3 LightSalmon => new Vector3(255, 160, 122) / 255;
            public Vector3 Crimson => new Vector3(220, 20, 60) / 255;
            public Vector3 Red => new Vector3(255, 0, 0) / 255;
            public Vector3 FireBrick => new Vector3(178, 34, 34) / 255;
            public Vector3 DarkRed => new Vector3(139, 0, 0) / 255;

            //Pink tones
            public Vector3 Pink => new Vector3(255, 192, 203) / 255;
            public Vector3 LightPink => new Vector3(255, 182, 193) / 255;
            public Vector3 HotPink => new Vector3(255, 105, 180) / 255;
            public Vector3 DeepPink => new Vector3(255, 20, 147) / 255;
            public Vector3 MediumVioletRed => new Vector3(199, 21, 133) / 255;
            public Vector3 PaleVioletRed => new Vector3(219, 112, 147) / 255;

            //Orange tones
            public Vector3 Coral => new Vector3(255, 127, 80) / 255;
            public Vector3 Tomato => new Vector3(255, 99, 71) / 255;
            public Vector3 OrangeRed => new Vector3(255, 69, 0) / 255;
            public Vector3 DarkOrange => new Vector3(255, 140, 0) / 255;
            public Vector3 Orange => new Vector3(255, 165, 0) / 255;

            //Yellow tones
            public Vector3 Gold => new Vector3(255, 215, 0) / 255;
            public Vector3 Yellow => new Vector3(255, 255, 0) / 255;
            public Vector3 LightYellow => new Vector3(255, 255, 224) / 255;
            public Vector3 LemonChiffon => new Vector3(255, 250, 205) / 255;
            public Vector3 LightGoldenrodYellow => new Vector3(250, 250, 210) / 255;
            public Vector3 PapayaWhip => new Vector3(255, 239, 213) / 255;
            public Vector3 Moccasin => new Vector3(255, 228, 181) / 255;
            public Vector3 PeachPuff => new Vector3(255, 218, 185) / 255;
            public Vector3 PaleGoldenrod => new Vector3(238, 232, 170) / 255;
            public Vector3 Khaki => new Vector3(240, 230, 140) / 255;
            public Vector3 DarkKhaki => new Vector3(189, 183, 107) / 255;

            //Purple tones
            public Vector3 Lavender => new Vector3(230, 230, 250) / 255;
            public Vector3 Thistle => new Vector3(216, 191, 216) / 255;
            public Vector3 Plum => new Vector3(221, 160, 221) / 255;
            public Vector3 Violet => new Vector3(238, 130, 238) / 255;
            public Vector3 Orchid => new Vector3(218, 112, 214) / 255;
            public Vector3 Fuchsia => new Vector3(255, 0, 255) / 255;
            public Vector3 Magenta => new Vector3(255, 0, 255) / 255;
            public Vector3 MediumOrchid => new Vector3(186, 85, 211) / 255;
            public Vector3 MediumPurple => new Vector3(147, 112, 219) / 255;
            public Vector3 RebeccaPurple => new Vector3(102, 51, 153) / 255;
            public Vector3 BlueViolet => new Vector3(138, 43, 226) / 255;
            public Vector3 DarkViolet => new Vector3(148, 0, 211) / 255;
            public Vector3 DarkOrchid => new Vector3(153, 50, 204) / 255;
            public Vector3 DarkMagenta => new Vector3(139, 0, 139) / 255;
            public Vector3 Purple => new Vector3(128, 0, 128) / 255;
            public Vector3 Indigo => new Vector3(75, 0, 130) / 255;
            public Vector3 SlateBlue => new Vector3(106, 90, 205) / 255;
            public Vector3 DarkSlateBlue => new Vector3(72, 61, 139) / 255;

            //Green tones
            public Vector3 GreenYellow => new Vector3(173, 255, 47) / 255;
            public Vector3 Chartreuse => new Vector3(127, 255, 0) / 255;
            public Vector3 LawnGreen => new Vector3(124, 252, 0) / 255;
            public Vector3 Lime => new Vector3(0, 255, 0) / 255;
            public Vector3 LimeGreen => new Vector3(50, 205, 50) / 255;
            public Vector3 PaleGreen => new Vector3(152, 251, 152) / 255;
            public Vector3 LightGreen => new Vector3(144, 238, 144) / 255;
            public Vector3 MediumSpringGreen => new Vector3(0, 250, 154) / 255;
            public Vector3 SpringGreen => new Vector3(0, 255, 127) / 255;
            public Vector3 MediumSeaGreen => new Vector3(60, 179, 113) / 255;
            public Vector3 SeaGreen => new Vector3(46, 139, 87) / 255;
            public Vector3 ForestGreen => new Vector3(34, 139, 34) / 255;
            public Vector3 Green => new Vector3(0, 128, 0) / 255;
            public Vector3 DarkGreen => new Vector3(0, 100, 0) / 255;
            public Vector3 YellowGreen => new Vector3(154, 205, 50) / 255;
            public Vector3 OliveDrab => new Vector3(107, 142, 35) / 255;
            public Vector3 Olive => new Vector3(128, 128, 0) / 255;
            public Vector3 DarkOliveGreen => new Vector3(85, 107, 47) / 255;
            public Vector3 MediumAquamarine => new Vector3(102, 205, 170) / 255;
            public Vector3 DarkSeaGreen => new Vector3(143, 188, 139) / 255;
            public Vector3 LightSeaGreen => new Vector3(32, 178, 170) / 255;
            public Vector3 DarkCyan => new Vector3(0, 139, 139) / 255;
            public Vector3 Teal => new Vector3(0, 128, 128) / 255;

            //Blue tones
            public Vector3 Aqua => new Vector3(0, 255, 255) / 255;
            public Vector3 Cyan => new Vector3(0, 255, 255) / 255;
            public Vector3 LightCyan => new Vector3(224, 255, 255) / 255;
            public Vector3 PaleTurquoise => new Vector3(175, 238, 238) / 255;
            public Vector3 Aquamarine => new Vector3(127, 255, 212) / 255;
            public Vector3 Turquoise => new Vector3(64, 224, 208) / 255;
            public Vector3 MediumTurquoise => new Vector3(72, 209, 204) / 255;
            public Vector3 DarkTurquoise => new Vector3(0, 206, 209) / 255;
            public Vector3 CadetBlue => new Vector3(95, 158, 160) / 255;
            public Vector3 SteelBlue => new Vector3(70, 130, 180) / 255;
            public Vector3 LightSteelBlue => new Vector3(176, 196, 222) / 255;
            public Vector3 PowderBlue => new Vector3(176, 224, 230) / 255;
            public Vector3 LightBlue => new Vector3(173, 216, 230) / 255;
            public Vector3 SkyBlue => new Vector3(135, 206, 235) / 255;
            public Vector3 LightSkyBlue => new Vector3(135, 206, 250) / 255;
            public Vector3 DeepSkyBlue => new Vector3(0, 191, 255) / 255;
            public Vector3 DodgerBlue => new Vector3(30, 144, 255) / 255;
            public Vector3 CornflowerBlue => new Vector3(100, 149, 237) / 255;
            public Vector3 MediumSlateBlue => new Vector3(123, 104, 238) / 255;
            public Vector3 RoyalBlue => new Vector3(65, 105, 225) / 255;
            public Vector3 Blue => new Vector3(0, 0, 255) / 255;
            public Vector3 MediumBlue => new Vector3(0, 0, 205) / 255;
            public Vector3 DarkBlue => new Vector3(0, 0, 139) / 255;
            public Vector3 Navy => new Vector3(0, 0, 128) / 255;
            public Vector3 MidnightBlue => new Vector3(25, 25, 112) / 255;

            //Brown tones
            public Vector3 Cornsilk => new Vector3(255, 248, 220) / 255;
            public Vector3 BlanchedAlmond => new Vector3(255, 235, 205) / 255;
            public Vector3 Bisque => new Vector3(255, 228, 196) / 255;
            public Vector3 NavajoWhite => new Vector3(255, 222, 173) / 255;
            public Vector3 Wheat => new Vector3(245, 222, 179) / 255;
            public Vector3 BurlyWood => new Vector3(222, 184, 135) / 255;
            public Vector3 Tan => new Vector3(210, 180, 140) / 255;
            public Vector3 RosyBrown => new Vector3(188, 143, 143) / 255;
            public Vector3 SandyBrown => new Vector3(244, 164, 96) / 255;
            public Vector3 Goldenrod => new Vector3(218, 165, 32) / 255;
            public Vector3 DarkGoldenrod => new Vector3(184, 134, 11) / 255;
            public Vector3 Peru => new Vector3(205, 133, 63) / 255;
            public Vector3 Chocolate => new Vector3(210, 105, 30) / 255;
            public Vector3 SaddleBrown => new Vector3(139, 69, 19) / 255;
            public Vector3 Sienna => new Vector3(160, 82, 45) / 255;
            public Vector3 Brown => new Vector3(165, 42, 42) / 255;
            public Vector3 Maroon => new Vector3(128, 0, 0) / 255;

            //White tones
            public Vector3 White => new Vector3(255, 255, 255) / 255;
            public Vector3 Snow => new Vector3(255, 250, 250) / 255;
            public Vector3 HoneyDew => new Vector3(240, 255, 240) / 255;
            public Vector3 MintCream => new Vector3(245, 255, 250) / 255;
            public Vector3 Azure => new Vector3(240, 255, 255) / 255;
            public Vector3 AliceBlue => new Vector3(240, 248, 255) / 255;
            public Vector3 GhostWhite => new Vector3(248, 248, 255) / 255;
            public Vector3 WhiteSmoke => new Vector3(245, 245, 245) / 255;
            public Vector3 SeaShell => new Vector3(255, 245, 238) / 255;
            public Vector3 Beige => new Vector3(245, 245, 220) / 255;
            public Vector3 OldLace => new Vector3(253, 245, 230) / 255;
            public Vector3 FloralWhite => new Vector3(255, 250, 240) / 255;
            public Vector3 Ivory => new Vector3(255, 255, 240) / 255;
            public Vector3 AntiqueWhite => new Vector3(250, 235, 215) / 255;
            public Vector3 Linen => new Vector3(250, 240, 230) / 255;
            public Vector3 LavenderBlush => new Vector3(255, 240, 245) / 255;
            public Vector3 MistyRose => new Vector3(255, 228, 225) / 255;

            //Gray tones
            public Vector3 Gainsboro => new Vector3(220, 220, 220) / 255;
            public Vector3 LightGray => new Vector3(211, 211, 211) / 255;
            public Vector3 Silver => new Vector3(192, 192, 192) / 255;
            public Vector3 DarkGray => new Vector3(169, 169, 169) / 255;
            public Vector3 Gray => new Vector3(128, 128, 128) / 255;
            public Vector3 DimGray => new Vector3(105, 105, 105) / 255;
            public Vector3 LightSlateGray => new Vector3(119, 136, 153) / 255;
            public Vector3 SlateGray => new Vector3(112, 128, 144) / 255;
            public Vector3 DarkSlateGray => new Vector3(47, 79, 79) / 255;
            public Vector3 Black => new Vector3(0, 0, 0) / 255;
        }     

        private static readonly ColorSet Colors = new ColorSet();

        [APIExtension(APIExtension.Properties, "Color")]
        public ColorSet GetColors(ScriptInstance instance) => Colors;
    }
}
