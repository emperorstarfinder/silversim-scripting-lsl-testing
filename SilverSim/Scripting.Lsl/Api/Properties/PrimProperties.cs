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
using SilverSim.Scripting.Lsl.Api.Primitive;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SilverSim.Scripting.Lsl.Api.Properties
{
    [LSLImplementation]
    [ScriptApiName("PrimProperties")]
    [Description("Prim Properties API")]
    public class PrimProperties : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APIExtension(APIExtension.Properties, "collisionfilter")]
        [APIDisplayName("collisionfilter")]
        [APIIsVariableType]
        [APIAccessibleMembers]
        [Serializable]
        [APICloneOnAssignment]
        public class CollisionFilter
        {
            public string Name;
            public LSLKey ID;
            public int Accept;

            public CollisionFilter()
            {
                Name = string.Empty;
                ID = UUID.Zero;
                Accept = 1;
            }

            public CollisionFilter(CollisionFilter src)
            {
                Name = src.Name;
                ID = src.ID;
                Accept = src.Accept;
            }
        }

        [APIExtension(APIExtension.Properties, "hovertext")]
        [APIDisplayName("hovertext")]
        [APIIsVariableType]
        [APIAccessibleMembers]
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
        [APIAccessibleMembers]
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
        [APIAccessibleMembers]
        [Serializable]
        public struct Projector
        {
            public int Enabled;
            public LSLKey Texture;
            public double FieldOfView;
            public double Focus;
            public double Ambience;
        }

        [APIExtension(APIExtension.Properties, "linksittarget")]
        [APIDisplayName("linksittarget")]
        [APIAccessibleMembers]
        [Serializable]
        public struct LinksitTarget
        {
            public int Enabled;
            public Vector3 Offset;
            public Quaternion Orientation;
        }

        [APIExtension(APIExtension.Properties, "linkunsittarget")]
        [APIDisplayName("linkunsittarget")]
        [APIAccessibleMembers]
        [Serializable]
        public struct LinkUnSitTarget
        {
            public int Enabled;
            public Vector3 Offset;
            public Quaternion Orientation;
        }

        [APIExtension(APIExtension.Properties, "linkfaceaccess")]
        [APIDisplayName("linkfaceaccess")]
        public class FaceAccessor
        {
            private readonly ScriptInstance Instance;
            private readonly int[] LinkNumbers;

            public FaceAccessor(ScriptInstance instance, int[] linkNumbers)
            {
                Instance = instance;
                LinkNumbers = linkNumbers;
            }

            public FaceProperties.TextureFace this[int faceNo]
            {
                get
                {
                    if (LinkNumbers.Length > 0)
                    {
                        lock (Instance)
                        {
                            var parts = new List<ObjectPart>();
                            var links = new List<int>();
                            foreach (int linkNumber in LinkNumbers)
                            {
                                ObjectPart p;
                                if (linkNumber == PrimitiveApi.LINK_THIS)
                                {
                                    p = Instance.Part;
                                }
                                else if (!Instance.Part.ObjectGroup.TryGetValue(linkNumber, out p))
                                {
                                    continue;
                                }
                                if (faceNo == FaceProperties.ALL_SIDES ||
                                    (faceNo >= 0 && faceNo < p.NumberOfSides))
                                {
                                    parts.Add(p);
                                    links.Add(linkNumber);
                                }
                            }

                            return new FaceProperties.TextureFace(Instance, parts.ToArray(), links.ToArray(), faceNo);
                        }
                    }
                    else
                    {
                        throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                    }
                }
            }
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
            "Description",
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
            "SitTarget",
            "SitAnimation",
            "Velocity",
            "HoverText",
            "AllowUnsit",
            "ScriptedSitOnly",
            "AllowInventoryDrop",
            "Faces")]
        [Serializable]
        public sealed class Prim
        {
            [XmlIgnore]
            [NonSerialized]
            private WeakReference<ScriptInstance> WeakInstance;

            public int[] LinkNumbers = new int[0];

            private T WithPart<T>(Func<ObjectPart, T> getter)
            {
                ScriptInstance instance;
                if (WeakInstance == null || !WeakInstance.TryGetTarget(out instance) || LinkNumbers.Length == 0)
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                }
                if(LinkNumbers.Length != 1)
                {
                    throw new LocalizedScriptErrorException(this, "MultipleLinksCannotBeRead", "Multiple links cannot be read.");
                }
                lock (instance)
                {
                    if (LinkNumbers[0] == PrimitiveApi.LINK_THIS)
                    {
                        return getter(instance.Part);
                    }
                    else
                    {
                        ObjectPart p;
                        if (!instance.Part.ObjectGroup.TryGetValue(LinkNumbers[0], out p))
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
                if (WeakInstance == null || !WeakInstance.TryGetTarget(out instance) || LinkNumbers.Length == 0)
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                }
                if (LinkNumbers.Length != 1)
                {
                    throw new LocalizedScriptErrorException(this, "MultipleLinksCannotBeRead", "Multiple links cannot be read.");
                }
                lock (instance)
                {
                    if (LinkNumbers[0] == PrimitiveApi.LINK_THIS)
                    {
                        return getter(instance, instance.Part);
                    }
                    else
                    {
                        ObjectPart p;
                        if (!instance.Part.ObjectGroup.TryGetValue(LinkNumbers[0], out p))
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
                if (WeakInstance == null || !WeakInstance.TryGetTarget(out instance) || LinkNumbers.Length == 0)
                {
                    throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                }
                lock (instance)
                {
                    foreach (int linkNumber in LinkNumbers)
                    {
                        if (linkNumber == PrimitiveApi.LINK_THIS)
                        {
                            setter(instance.Part, value);
                        }
                        else
                        {
                            ObjectPart p;
                            if (!instance.Part.ObjectGroup.TryGetValue(linkNumber, out p))
                            {
                                throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                            }
                            setter(instance.Part, value);
                        }
                    }
                }
            }

            public Prim()
            {
            }

            public Prim(ScriptInstance instance, int[] linkNumbers)
            {
                WeakInstance = new WeakReference<ScriptInstance>(instance);
                LinkNumbers = linkNumbers;
            }

            public void RestoreFromSerialization(ScriptInstance instance)
            {
                WeakInstance = new WeakReference<ScriptInstance>(instance);
            }

            public LSLKey Creator => WithPart((ObjectPart p) => p.Creator.ID);

            public LSLKey Owner => WithPart((ObjectPart p) => p.Owner.ID);

            public LSLKey Key => WithPart((ObjectPart p) => p.ID);

            [XmlIgnore]
            public string SitAnimation
            {
                get { return WithPart((p) => p.SitAnimation); }
                set { WithPart((p, v) => p.SitAnimation = v, value); }
            }

            [XmlIgnore]
            public Vector3 Velocity
            {
                get { return WithPart((p) => p.Velocity); }
                set { WithPart((p, v) => p.Velocity = v, value); }
            }

            [XmlIgnore]
            public Vector3 AngularVelocity
            {
                get { return WithPart((p) => p.AngularVelocity); }
                set { WithPart((p, v) => p.AngularVelocity = v, value); }
            }

            [XmlIgnore]
            public CollisionFilter CollisionFilter
            {
                get
                {
                    return WithPart((p) =>
                    {
                        ObjectPart.CollisionFilterParam cfp = p.CollisionFilter;
                        return new CollisionFilter
                        {
                            Name = cfp.Name,
                            ID = cfp.ID,
                            Accept = (cfp.Type == ObjectPart.CollisionFilterEnum.Accept).ToLSLBoolean()
                        };
                    });
                }
                set
                {
                    WithPart((p, v) =>
                    {
                        p.CollisionFilter = new ObjectPart.CollisionFilterParam
                        {
                            Name = v.Name,
                            ID = v.ID,
                            Type = v.Accept != 0 ? ObjectPart.CollisionFilterEnum.Accept : ObjectPart.CollisionFilterEnum.Reject
                        };
                    }, value);
                }
            }

            public InventoryProperties.PrimInventory Inventory =>
                WithPart((instance, p) => new InventoryProperties.PrimInventory(instance, p));

            [XmlIgnore]
            public LinksitTarget SitTarget
            {
                get
                {
                    return WithPart((p) => new LinksitTarget
                    {
                        Enabled = p.IsSitTargetActive.ToLSLBoolean(),
                        Offset = p.SitTargetOffset,
                        Orientation = p.SitTargetOrientation
                    });
                }
                set
                {
                    WithPart((p, t) =>
                    {
                        p.SitTargetOffset = t.Offset;
                        p.SitTargetOrientation = t.Orientation;
                        p.IsSitTargetActive = t.Enabled != 0;
                    }, value);
                }
            }

            [XmlIgnore]
            public LinkUnSitTarget UnSitTarget
            {
                get
                {
                    return WithPart((p) => new LinkUnSitTarget
                    {
                        Enabled = p.IsUnSitTargetActive.ToLSLBoolean(),
                        Offset = p.UnSitTargetOffset,
                        Orientation = p.UnSitTargetOrientation
                    });
                }
                set
                {
                    WithPart((p, t) =>
                    {
                        p.UnSitTargetOffset = t.Offset;
                        p.UnSitTargetOrientation = t.Orientation;
                        p.IsUnSitTargetActive = t.Enabled != 0;
                    }, value);
                }
            }

            [XmlIgnore]
            public Hovertext HoverText
            {
                get
                {
                    return WithPart((p) =>
                    {
                        ObjectPart.TextParam text = p.Text;
                        return new Hovertext
                        {
                            Text = text.Text,
                            Color = text.TextColor,
                            Alpha = text.TextColor.A
                        };
                    });
                }
                set
                {
                    WithPart((p, t) => p.Text = t, new ObjectPart.TextParam
                    {
                        Text = value.Text,
                        TextColor = new ColorAlpha(value.Color, value.Alpha)
                    });
                }
            }

            [XmlIgnore]
            public Pointlight PointLight
            {
                get
                {
                    return WithPart((p) =>
                    {
                        ObjectPart.PointLightParam light = p.PointLight;
                        return new Pointlight
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
                    WithPart((p, l) => p.PointLight = l, new ObjectPart.PointLightParam
                    {
                        IsLight = value.Enabled != 0,
                        LightColor = new Color(value.Color),
                        Intensity = value.Intensity,
                        Radius = value.Radius,
                        Falloff = value.Falloff
                    });
                }
            }

            [XmlIgnore]
            public Projector Projector
            {
                get
                {
                    return WithPart((p) =>
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
                    WithPart((p, param) =>
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

            [XmlIgnore]
            public string Description
            {
                get { return WithPart((p) => p.Description); }
                set { WithPart((p, d) => p.Description = d, value); }
            }

            [XmlIgnore]
            public string Name
            {
                get { return WithPart((p) => p.Name); }
                set { WithPart((p, d) => p.Name = d, value); }
            }

            [XmlIgnore]
            public int AllowInventoryDrop
            {
                get { return WithPart((p) => p.IsAllowedDrop.ToLSLBoolean()); }
                set { WithPart((p, v) => p.IsAllowedDrop = v, value != 0); }
            }

            [XmlIgnore]
            public Quaternion LocalRot
            {
                get { return WithPart((p) => p.LocalRotation); }
                set { WithPart((p, q) => p.LocalRotation = q, value); }
            }

            [XmlIgnore]
            public Vector3 Size
            {
                get { return WithPart((p) => p.Size); }
                set { WithPart((p, v) => p.Size = v, value); }
            }

            [XmlIgnore]
            public Vector3 LocalPos
            {
                get { return WithPart((p) => p.LocalPosition); }
                set { WithPart((p, v) => p.LocalPosition = v, value); }
            }

            [XmlIgnore]
            public Quaternion Rotation
            {
                get { return WithPart((p) => p.Rotation); }
                set { WithPart((p, q) => p.Rotation = q, value); }
            }

            [XmlIgnore]
            public Vector3 Position
            {
                get { return WithPart((p) => p.Position); }
                set { WithPart((p, v) => p.Position = v, value); }
            }

            [XmlIgnore]
            public int PhysicsShapeType
            {
                get { return (int)WithPart((p) => p.PhysicsShapeType); }
                set
                {
                    if (value >= (int)PrimitivePhysicsShapeType.Prim && value <= (int)PrimitivePhysicsShapeType.Convex)
                    {
                        WithPart((p, s) => p.PhysicsShapeType = s,
                            (PrimitivePhysicsShapeType)value);
                    }
                }
            }

            [XmlIgnore]
            public int Material
            {
                get { return (int)WithPart((p) => p.Material); }
                set
                {
                    if (value >= (int)PrimitiveMaterial.Stone && value <= (int)PrimitiveMaterial.Light)
                    {
                        WithPart((p, m) => p.Material = m, (PrimitiveMaterial)value);
                    }
                }
            }

            [XmlIgnore]
            public int IsPhantom
            {
                get { return WithPart((p) => p.IsPhantom.ToLSLBoolean()); }
                set { WithPart((p, v) => p.IsPhantom = v, value != 0); }
            }

            [XmlIgnore]
            public int IsPhysics
            {
                get { return WithPart((p) => p.IsPhysics.ToLSLBoolean()); }
                set { WithPart((p, v) => p.IsPhysics = v, value != 0); }
            }

            [XmlIgnore]
            public int IsTempOnRez
            {
                get { return WithPart((p) => p.ObjectGroup.IsTempOnRez.ToLSLBoolean()); }
                set { WithPart((p, v) => p.ObjectGroup.IsTempOnRez = v, value != 0); }
            }

            [XmlIgnore]
            public int IsVolumeDetect
            {
                get { return WithPart((p) => p.ObjectGroup.IsVolumeDetect.ToLSLBoolean()); }
                set { WithPart((p, v) => p.ObjectGroup.IsVolumeDetect = v, value != 0); }
            }

            [XmlIgnore]
            public int AllowUnsit
            {
                get { return WithPart((p) => p.AllowUnsit.ToLSLBoolean()); }
                set { WithPart((p, v) => p.AllowUnsit = v, value != 0); }
            }

            [XmlIgnore]
            public int ScriptedSitOnly
            {
                get { return WithPart((p) => p.IsScriptedSitOnly.ToLSLBoolean()); }
                set { WithPart((p, v) => p.IsScriptedSitOnly = v, value != 0); }
            }

            [XmlIgnore]
            public double Buoyancy
            {
                get { return WithPart((p) => p.Buoyancy); }
                set { WithPart((p, v) => p.Buoyancy = v, value); }
            }

            [APIExtension(APIExtension.Properties)]
            public static implicit operator bool(Prim c) => c.LinkNumbers.Length > 0;

            [APIExtension(APIExtension.Properties)]
            public static implicit operator int(Prim c) => c.LinkNumbers.Length;

            [APIExtension(APIExtension.Properties)]
            public static explicit operator string(Prim c) => c.LinkNumbers.Length.ToString();

            public FaceAccessor Faces
            {
                get
                {
                    ScriptInstance instance;
                    if (WeakInstance.TryGetTarget(out instance) && LinkNumbers.Length > 0)
                    {
                        lock (instance)
                        {
                            var links = new List<int>();
                            foreach (int linkNumber in LinkNumbers)
                            {
                                ObjectPart p;
                                if (linkNumber == PrimitiveApi.LINK_THIS)
                                {
                                    p = instance.Part;
                                }
                                else if (!instance.Part.ObjectGroup.TryGetValue(linkNumber, out p))
                                {
                                    continue;
                                }
                                links.Add(linkNumber);
                            }

                            return new FaceAccessor(instance, links.ToArray());
                        }
                    }
                    else
                    {
                        throw new LocalizedScriptErrorException(this, "ValueContentsNotAssignedType0", "Value contents not assigned. (Type {0})", "link");
                    }
                }
            }
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "this")]
        public Prim GetThis(ScriptInstance instance)
        {
            lock (instance)
            {
                return new Prim(instance, new int[] { instance.Part.LinkNumber });
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
                        var links = new List<int>();
                        if (linkno == PrimitiveApi.LINK_ALL_CHILDREN)
                        {
                            foreach(KeyValuePair<int, ObjectPart> kvp in Instance.Part.ObjectGroup.Key1ValuePairs)
                            {
                                if (kvp.Key != PrimitiveApi.LINK_ROOT)
                                {
                                    links.Add(kvp.Key);
                                }
                            }
                        }
                        else if (linkno == PrimitiveApi.LINK_ALL_OTHERS)
                        {
                            foreach (KeyValuePair<int, ObjectPart> kvp in Instance.Part.ObjectGroup.Key1ValuePairs)
                            {
                                if (kvp.Value != Instance.Part)
                                {
                                    links.Add(kvp.Key);
                                }
                            }
                        }
                        else if (linkno == PrimitiveApi.LINK_ROOT)
                        {
                            links.Add(PrimitiveApi.LINK_ROOT);
                        }
                        else if (linkno == PrimitiveApi.LINK_SET)
                        {
                            links.AddRange(Instance.Part.ObjectGroup.Keys1);
                        }
                        else
                        {
                            links.Add(linkno);
                        }
                        return new Prim(Instance, links.ToArray());
                    }
                }
            }

            public Prim this[AnArray linklist]
            {
                get
                {
                    lock (Instance)
                    {
                        var links = new List<int>();
                        foreach(IValue val in linklist)
                        {
                            int linkno = val.AsInt;

                            if (linkno == PrimitiveApi.LINK_ALL_CHILDREN)
                            {
                                foreach (KeyValuePair<int, ObjectPart> kvp in Instance.Part.ObjectGroup.Key1ValuePairs)
                                {
                                    if (kvp.Key != PrimitiveApi.LINK_ROOT && !links.Contains(kvp.Key))
                                    {
                                        links.Add(kvp.Key);
                                    }
                                }
                            }
                            else if (linkno == PrimitiveApi.LINK_ALL_OTHERS)
                            {
                                foreach (KeyValuePair<int, ObjectPart> kvp in Instance.Part.ObjectGroup.Key1ValuePairs)
                                {
                                    if (kvp.Value != Instance.Part && !links.Contains(kvp.Key))
                                    {
                                        links.Add(kvp.Key);
                                    }
                                }
                            }
                            else if (linkno == PrimitiveApi.LINK_ROOT)
                            {
                                if (!links.Contains(linkno))
                                {
                                    links.Add(linkno);
                                }
                            }
                            else if (linkno == PrimitiveApi.LINK_SET)
                            {
                                foreach (int l in Instance.Part.ObjectGroup.Keys1)
                                {
                                    if(!links.Contains(l))
                                    {
                                        links.Add(l);
                                    }
                                }
                            }
                            else if (!links.Contains(linkno))
                            {
                                links.Add(linkno);
                            }
                        }
                        return new Prim(Instance, links.ToArray());
                    }
                }
            }

            public Prim this[string name]
            {
                get
                {
                    lock (Instance)
                    {
                        var links = new List<int>();
                        foreach (KeyValuePair<int, ObjectPart> kvp in Instance.Part.ObjectGroup.Key1ValuePairs)
                        {
                            if (kvp.Value.Name == name)
                            {
                                links.Add(kvp.Key);
                            }
                        }
                        return new Prim(Instance, links.ToArray());
                    }
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

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "Root")]
        public Prim GetRoot(ScriptInstance instance)
        {
            lock (instance)
            {
                return new Prim(instance, new int[] { PrimitiveApi.LINK_ROOT });
            }
        }

        [APIExtension(APIExtension.Properties, "SitTarget")]
        public LinksitTarget GetSitTarget(int enabled, Vector3 offset, Quaternion orientation) => new LinksitTarget
        {
            Enabled = enabled,
            Offset = offset,
            Orientation = orientation
        };

        [APIExtension(APIExtension.Properties, "UnSitTarget")]
        public LinkUnSitTarget GetUnSitTarget(int enabled, Vector3 offset, Quaternion orientation) => new LinkUnSitTarget
        {
            Enabled = enabled,
            Offset = offset,
            Orientation = orientation
        };

        [APIExtension(APIExtension.Properties, "HoverText")]
        public Hovertext GetHovertext(string text, Vector3 color, double alpha) => new Hovertext
        {
            Text = text,
            Color = color,
            Alpha = alpha
        };

        [APIExtension(APIExtension.Properties, "PointLight")]
        public Pointlight GetPointlight(int enabled, Vector3 color, double intensity, double radius, double falloff) => new Pointlight
        {
            Enabled = enabled,
            Color = color,
            Intensity = intensity,
            Radius = radius,
            Falloff = falloff
        };

        [APIExtension(APIExtension.Properties, "Projector")]
        public Projector GetProjector(int enabled, LSLKey texture, double fov, double focus, double ambience) => new Projector
        {
            Enabled = enabled,
            Texture = texture,
            FieldOfView = fov,
            Focus = focus,
            Ambience = ambience
        };

        [APIExtension(APIExtension.Properties, "Color")]
        [APIDisplayName("Color")]
        [APIAccessibleMembers]
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

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "Color")]
        public ColorSet GetColors() => Colors;
    }
}
