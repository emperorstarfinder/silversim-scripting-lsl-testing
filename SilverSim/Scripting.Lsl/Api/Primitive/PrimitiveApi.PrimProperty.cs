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
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
#pragma warning disable IDE1006
        [APIExtension(APIExtension.Properties, "hovertext")]
        [APIDisplayName("hovertext")]
        [APIIsVariableType]
        [ImplementsCustomTypecasts]
        [APIAccessibleMembers(
            "text",
            "color",
            "alpha")]
        [Serializable]
        [APICloneOnAssignment]
        public class Hovertext
        {
            public string text;
            public Vector3 color;
            public double alpha;

            public Hovertext()
            {
                text = string.Empty;
                color = Vector3.One;
                alpha = 0;
            }

            public Hovertext(Hovertext t)
            {
                text = t.text;
                color = t.color;
                alpha = t.alpha;
            }
        }

        [APIExtension(APIExtension.Properties, "pointlight")]
        [APIDisplayName("pointlight")]
        [APIIsVariableType]
        [ImplementsCustomTypecasts]
        [APIAccessibleMembers(
            "enabled",
            "color",
            "intensity",
            "radius",
            "falloff")]
        [Serializable]
        public struct Pointlight
        {
            public int enabled;
            public Vector3 color;
            public double intensity;
            public double radius;
            public double falloff;
        }


        [APIExtension(APIExtension.Properties, "link")]
        [APIDisplayName("link")]
        [APIIsVariableType]
        [ImplementsCustomTypecasts]
        [APIAccessibleMembers(
            "Key",
            "Name",
            "Desc",
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
            "HoverText",
            "AllowUnsit",
            "ScriptedSitOnly",
            "AllowInventoryDrop")]
        [Serializable]
        public sealed class Prim
        {
            private ScriptInstance Instance { get; set; }
            private T WithPart<T>(Func<ObjectPart, T> getter)
            {
                if(Instance == null)
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                }
                lock(Instance)
                {
                    if (LinkNumber == LINK_THIS)
                    {
                        return getter(Instance.Part);
                    }
                    else
                    {
                        ObjectPart p;
                        if(!Instance.Part.ObjectGroup.TryGetValue(LinkNumber, out p))
                        {
                            throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                        }
                        return getter(p);
                    }
                }
            }

            private void WithPart<T>(Action<ObjectPart, T> setter, T value)
            {
                if (Instance == null)
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                }
                lock (Instance)
                {
                    if (LinkNumber == LINK_THIS)
                    {
                        setter(Instance.Part, value);
                    }
                    else
                    {
                        ObjectPart p;
                        if (!Instance.Part.ObjectGroup.TryGetValue(LinkNumber, out p))
                        {
                            throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                        }
                        setter(Instance.Part, value);
                    }
                }
            }

            public int LinkNumber;

            public Prim()
            {
                LinkNumber = LINK_SET;
            }

            public Prim(ScriptInstance instance, int linkNumber)
            {
                Instance = instance;
                LinkNumber = linkNumber;
            }

            public void RestoreFromSerialization(ScriptInstance instance)
            {
                Instance = instance;
            }

            public LSLKey key => WithPart((ObjectPart p) => p.ID);

            public Hovertext HoverText
            {
                get
                {
                    return WithPart((ObjectPart p) =>
                    {
                        ObjectPart.TextParam text = p.Text;
                        return new Hovertext()
                        {
                            text = text.Text,
                            color = text.TextColor,
                            alpha = text.TextColor.A
                        };
                    });
                }
                set
                {
                    WithPart((ObjectPart p, ObjectPart.TextParam t) => p.Text = t, new ObjectPart.TextParam
                    {
                        Text = value.text,
                        TextColor = new ColorAlpha(value.color, value.alpha)
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
                            enabled = light.IsLight.ToLSLBoolean(),
                            color = light.LightColor,
                            intensity = light.Intensity,
                            radius = light.Radius,
                            falloff = light.Falloff
                        };
                    });
                }
                set
                {
                    WithPart((ObjectPart p, ObjectPart.PointLightParam l) => p.PointLight = l, new ObjectPart.PointLightParam
                    {
                        IsLight = value.enabled != 0,
                        LightColor = new Color(value.color),
                        Intensity = value.intensity,
                        Radius = value.radius,
                        Falloff = value.falloff
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
            public static implicit operator bool(Prim c) => c.LinkNumber > 0;

            [APIExtension(APIExtension.Properties)]
            public static implicit operator int(Prim c) => c.LinkNumber;
        }

#pragma warning restore IDE1006

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "this")]
        public Prim GetThis(ScriptInstance instance)
        {
            lock(instance)
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
                        if (linkno == LINK_ROOT)
                        {
                            linkno = 1;
                        }
                        return new Prim(Instance, linkno);
                    }
                }
            }

            public Prim this[string name]
            {
                get
                {
                    lock(Instance)
                    {
                        foreach (ObjectPart p in Instance.Part.ObjectGroup.ValuesByKey1)
                        {
                            if(p.Name == name)
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
            lock(instance)
            {
                return new LinkSetAccessor(instance);
            }
        }
    }
}
