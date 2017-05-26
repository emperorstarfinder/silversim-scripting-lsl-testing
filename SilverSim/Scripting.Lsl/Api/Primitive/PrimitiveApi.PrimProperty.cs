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

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
#pragma warning disable IDE1006
        [APIExtension(APIExtension.Properties, "link")]
        [APIDisplayName("link")]
        [APIIsVariableType]
        [APIAccessibleMembers(
            "key",
            "name",
            "desc",
            "localrot",
            "localpos",
            "rotation",
            "position",
            "physicsshapetype",
            "material",
            "isphysics",
            "istemponrez",
            "size",
            "allowunsit",
            "scriptedsitonly",
            "allowinventorydrop")]
        public class Prim
        {
            public ScriptInstance Instance { get; }
            public ObjectPart Part { get; }

            public Prim(ScriptInstance instance, ObjectPart part)
            {
                Instance = instance;
                Part = part;
            }

            public LSLKey key
            {
                get
                {
                    lock(Instance)
                    {
                        return Part.ID;
                    }
                }
            }

            public string desc
            {
                get
                {
                    lock(Instance)
                    {
                        return Part.Description;
                    }
                }
                set
                {
                    lock(Instance)
                    {
                        Part.Description = value;
                    }
                }
            }

            public string name
            {
                get
                {
                    lock(Instance)
                    {
                        return Part.Name;
                    }
                }

                set
                {
                    lock(Instance)
                    {
                        Part.Name = value;
                    }
                }
            }

            public int allowinventorydrop
            {
                get
                {
                    lock(Instance)
                    {
                        return Part.IsAllowedDrop.ToLSLBoolean();
                    }
                }
                set
                {
                    lock(Instance)
                    {
                        Part.IsAllowedDrop = value != 0;
                    }
                }
            }

            public Quaternion localrot
            {
                get
                {
                    lock(Instance)
                    {
                        return Part.LocalRotation;
                    }
                }
                set
                {
                    lock(Instance)
                    {
                        Part.LocalRotation = value;
                    }
                }
            }

            public Vector3 size
            {
                get
                {
                    lock(Instance)
                    {
                        return Part.Size;
                    }
                }
                set
                {
                    lock(Instance)
                    {
                        Part.Size = value;
                    }
                }
            }

            public Vector3 localpos
            {
                get
                {
                    lock(Instance)
                    {
                        return Part.LocalPosition;
                    }
                }
                set
                {
                    lock(Instance)
                    {
                        Part.LocalPosition = value;
                    }
                }
            }

            public Quaternion rotation
            {
                get
                {
                    lock(Instance)
                    {
                        return Part.Rotation;
                    }
                }
                set
                {
                    lock(Instance)
                    {
                        Part.Rotation = value;
                    }
                }
            }

            public Vector3 position
            {
                get
                {
                    lock(Instance)
                    {
                        return Part.Position;
                    }
                }
                set
                {
                    lock(Instance)
                    {
                        Part.Position = value;
                    }
                }
            }

            public int physicsshapetype
            {
                get
                {
                    lock(Instance)
                    {
                        return (int)Part.PhysicsShapeType;
                    }
                }
                set
                {
                    lock (Instance)
                    {
                        if (value >= (int)PrimitivePhysicsShapeType.Prim && value <= (int)PrimitivePhysicsShapeType.Convex)
                        {
                            Part.PhysicsShapeType = (PrimitivePhysicsShapeType)value;
                        }
                    }
                }
            }

            public int material
            {
                get
                {
                    lock(Instance)
                    {
                        return (int)Part.Material;
                    }
                }
                set
                {
                    lock(Instance)
                    {
                        if(value >= (int)PrimitiveMaterial.Stone && value <= (int)PrimitiveMaterial.Light)
                        {
                            Part.Material = (PrimitiveMaterial)value;
                        }
                    }
                }
            }

            public int isphysics
            {
                get
                {
                    lock(Instance)
                    {
                        return Part.IsPhysics.ToLSLBoolean();
                    }
                }
                set
                {
                    lock(Instance)
                    {
                        Part.IsPhysics = value != 0;
                    }
                }
            }

            public int istemponrez
            {
                get
                {
                    lock (Instance)
                    {
                        return Part.ObjectGroup.IsTempOnRez.ToLSLBoolean();
                    }
                }
                set
                {
                    lock (Instance)
                    {
                        Part.ObjectGroup.IsTempOnRez = value != 0;
                    }
                }
            }

            public int allowunsit
            {
                get
                {
                    lock(Instance)
                    {
                        return Part.AllowUnsit.ToLSLBoolean();
                    }
                }
                set
                {
                    lock(Instance)
                    {
                        Part.AllowUnsit = value != 0;
                    }
                }
            }

            public int scriptedsitonly
            {
                get
                {
                    lock(Instance)
                    {
                        return Part.IsScriptedSitOnly.ToLSLBoolean();
                    }
                }
                set
                {
                    lock(Instance)
                    {
                        Part.IsScriptedSitOnly = value != 0;
                    }
                }
            }
        }
#pragma warning restore IDE1006

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "this")]
        public Prim GetThis(ScriptInstance instance)
        {
            lock(instance)
            {
                return new Prim(instance, instance.Part);
            }
        }
    }
}
