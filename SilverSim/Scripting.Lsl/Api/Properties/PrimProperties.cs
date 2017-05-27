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
    }
}
