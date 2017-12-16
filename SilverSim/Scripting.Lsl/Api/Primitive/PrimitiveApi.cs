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

#pragma warning disable IDE0018, RCS1029

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    [ScriptApiName("Primitive")]
    [LSLImplementation]
    [Description("LSL/OSSL Primitive API")]
    public partial class PrimitiveApi : IScriptApi, IPlugin
    {
        [APILevel(APIFlags.LSL)]
        public const int PAY_HIDE = -1;
        [APILevel(APIFlags.LSL)]
        public const int PAY_DEFAULT = -2;

        [APILevel(APIFlags.LSL)]
        public const int LINK_ROOT = 1;
        [APILevel(APIFlags.LSL)]
        public const int LINK_SET = -1;
        [APILevel(APIFlags.LSL)]
        public const int LINK_ALL_OTHERS = -2;
        [APILevel(APIFlags.LSL)]
        public const int LINK_ALL_CHILDREN = -3;
        [APILevel(APIFlags.LSL)]
        public const int LINK_THIS = -4;

        [APILevel(APIFlags.LSL)]
        public const int PRIM_MATERIAL = 2;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_PHYSICS = 3;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_TEMP_ON_REZ = 4;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_PHANTOM = 5;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_POSITION = 6;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_SIZE = 7;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_ROTATION = 8;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_TYPE = 9;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_TEXTURE = 17;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_COLOR = 18;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_SHINY = 19;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_FULLBRIGHT = 20;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_FLEXIBLE = 21;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_TEXGEN = 22;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_CAST_SHADOWS = 24; // Not implemented, here for completeness sake
        [APILevel(APIFlags.LSL)]
        public const int PRIM_POINT_LIGHT = 23; // Huh?
        [APILevel(APIFlags.LSL)]
        public const int PRIM_GLOW = 25;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_TEXT = 26;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_NAME = 27;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_DESC = 28;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_ROT_LOCAL = 29;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_OMEGA = 32;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_POS_LOCAL = 33;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_LINK_TARGET = 34;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_SLICE = 35;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_SPECULAR = 36;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_NORMAL = 37;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_ALPHA_MODE = 38;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_ALLOW_UNSIT = 39;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_SCRIPTED_SIT_ONLY = 40;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_SIT_TARGET = 41;

        [APILevel(APIFlags.ASSL)]
        [APIExtension(APIExtension.InWorldz, "IW_PRIM_ALPHA")]
        [Description("Get/set alpha value of texture [integer face, float alpha]")]
        public const int PRIM_ALPHA = 11001;

        [APILevel(APIFlags.ASSL)]
        [APIExtension(APIExtension.InWorldz, "IW_PRIM_PROJECTOR")]
        [Description("Get/set projection params [integer enable, string texture, float fov, float focus, float ambience]")]
        public const int PRIM_PROJECTOR = 11100;
        [APILevel(APIFlags.ASSL)]
        [APIExtension(APIExtension.InWorldz, "IW_PRIM_PROJECTOR_ENABLED")]
        [Description("Get/set projection enable [integer enable]")]
        public const int PRIM_PROJECTOR_ENABLED = 11101;
        [APILevel(APIFlags.ASSL)]
        [APIExtension(APIExtension.InWorldz, "IW_PRIM_PROJECTOR_TEXTURE")]
        [Description("Get/set projection texture [string texture]")]
        public const int PRIM_PROJECTOR_TEXTURE = 11102;
        [APILevel(APIFlags.ASSL)]
        [APIExtension(APIExtension.InWorldz, "IW_PRIM_PROJECTOR_FOV")]
        [Description("Get/set projection fov [float fov]")]
        public const int PRIM_PROJECTOR_FOV = 11103;
        [APILevel(APIFlags.ASSL)]
        [APIExtension(APIExtension.InWorldz, "IW_PRIM_PROJECTOR_FOCUS")]
        [Description("Get/set projection focus [float focus]")]
        public const int PRIM_PROJECTOR_FOCUS = 11104;
        [APILevel(APIFlags.ASSL)]
        [APIExtension(APIExtension.InWorldz, "IW_PRIM_PROJECTOR_AMBIENCE")]
        [Description("Get/set projection ambience [float ambience]")]
        public const int PRIM_PROJECTOR_AMBIENCE = 11105;

        [APILevel(APIFlags.ASSL)]
        [Description("Get/set unsit target")]
        public const int PRIM_UNSIT_TARGET = 12000;
        [APILevel(APIFlags.ASSL)]
        [Description("Get/set sit animation")]
        public const int PRIM_SIT_ANIMATION = 12001;

        [APILevel(APIFlags.LSL)]
        public const int PRIM_TEXGEN_DEFAULT = 0;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_TEXGEN_PLANAR = 1;

        [APILevel(APIFlags.LSL)]
        public const int PRIM_ALPHA_MODE_NONE = 0;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_ALPHA_MODE_BLEND = 1;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_ALPHA_MODE_MASK = 2;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_ALPHA_MODE_EMISSIVE = 3;

        [APILevel(APIFlags.LSL)]
        public const int PRIM_TYPE_BOX = 0;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_TYPE_CYLINDER = 1;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_TYPE_PRISM = 2;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_TYPE_SPHERE = 3;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_TYPE_TORUS = 4;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_TYPE_TUBE = 5;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_TYPE_RING = 6;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_TYPE_SCULPT = 7;

        [APILevel(APIFlags.LSL)]
        public const int PRIM_HOLE_DEFAULT = 0;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_HOLE_CIRCLE = 16;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_HOLE_SQUARE = 32;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_HOLE_TRIANGLE = 48;

        [APILevel(APIFlags.LSL)]
        public const int PRIM_MATERIAL_STONE = 0;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MATERIAL_METAL = 1;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MATERIAL_GLASS = 2;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MATERIAL_WOOD = 3;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MATERIAL_FLESH = 4;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MATERIAL_PLASTIC = 5;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MATERIAL_RUBBER = 6;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MATERIAL_LIGHT = 7;

        [APILevel(APIFlags.LSL)]
        public const int PRIM_SHINY_NONE = 0;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_SHINY_LOW = 1;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_SHINY_MEDIUM = 2;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_SHINY_HIGH = 3;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_NONE = 0;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_BRIGHT = 1;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_DARK = 2;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_WOOD = 3;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_BARK = 4;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_BRICKS = 5;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_CHECKER = 6;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_CONCRETE = 7;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_TILE = 8;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_STONE = 9;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_DISKS = 10;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_GRAVEL = 11;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_BLOBS = 12;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_SIDING = 13;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_LARGETILE = 14;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_STUCCO = 15;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_SUCTION = 16;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_BUMP_WEAVE = 17;

        [APILevel(APIFlags.LSL)]
        public const int PRIM_SCULPT_TYPE_SPHERE = 1;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_SCULPT_TYPE_TORUS = 2;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_SCULPT_TYPE_PLANE = 3;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_SCULPT_TYPE_CYLINDER = 4;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_SCULPT_FLAG_INVERT = 64;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_SCULPT_FLAG_MIRROR = 128;

        [APILevel(APIFlags.LSL)]
        public const int ALL_SIDES = -1;

        [APILevel(APIFlags.LSL)]
        public const int CLICK_ACTION_NONE = 0;
        [APILevel(APIFlags.LSL)]
        public const int CLICK_ACTION_TOUCH = 0;
        [APILevel(APIFlags.LSL)]
        public const int CLICK_ACTION_SIT = 1;
        [APILevel(APIFlags.LSL)]
        public const int CLICK_ACTION_BUY = 2;
        [APILevel(APIFlags.LSL)]
        public const int CLICK_ACTION_PAY = 3;
        [APILevel(APIFlags.LSL)]
        public const int CLICK_ACTION_OPEN = 4;
        [APILevel(APIFlags.LSL)]
        public const int CLICK_ACTION_PLAY = 5;
        [APILevel(APIFlags.LSL)]
        public const int CLICK_ACTION_OPEN_MEDIA = 6;
        [APILevel(APIFlags.LSL)]
        public const int CLICK_ACTION_ZOOM = 7;

        [APILevel(APIFlags.LSL)]
        public const int PRIM_PHYSICS_SHAPE_TYPE = 30;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_PHYSICS_SHAPE_PRIM = 0;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_PHYSICS_SHAPE_CONVEX = 2;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_PHYSICS_SHAPE_NONE = 1;

        [APILevel(APIFlags.LSL)]
        public const int PRIM_PHYSICS_MATERIAL = 31;
        [APILevel(APIFlags.LSL)]
        public const int DENSITY = 1;
        [APILevel(APIFlags.LSL)]
        public const int FRICTION = 2;
        [APILevel(APIFlags.LSL)]
        public const int RESTITUTION = 4;
        [APILevel(APIFlags.LSL)]
        public const int GRAVITY_MULTIPLIER = 8;

        [APILevel(APIFlags.LSL)]
        public const string TEXTURE_BLANK = "5748decc-f629-461c-9a36-a35a221fe21f";
        [APILevel(APIFlags.LSL)]
        public const string TEXTURE_DEFAULT = "89556747-24cb-43ed-920b-47caed15465f";
        [APILevel(APIFlags.LSL)]
        public const string TEXTURE_PLYWOOD = "89556747-24cb-43ed-920b-47caed15465f";
        [APILevel(APIFlags.LSL)]
        public const string TEXTURE_TRANSPARENT = "8dcd4a48-2d37-4909-9f78-f7a9eb4ef903";
        [APILevel(APIFlags.LSL)]
        public const string TEXTURE_MEDIA = "8b5fec65-8d8d-9dc5-cda8-8fdf2716e361";

        [APILevel(APIFlags.LSL)]
        public const int PASS_IF_NOT_HANDLED = 0;
        [APILevel(APIFlags.LSL)]
        public const int PASS_ALWAYS = 1;
        [APILevel(APIFlags.LSL)]
        public const int PASS_NEVER = 2;

        private List<ObjectPart> GetLinkTargets(ScriptInstance instance, int link)
        {
            var list = new List<ObjectPart>();
            ObjectPart thisPart = instance.Part;
            ObjectGroup thisGroup = thisPart.ObjectGroup;
            if (link == LINK_THIS)
            {
                list.Add(thisPart);
            }
            else if (link == LINK_ROOT)
            {
                list.Add(thisGroup.RootPart);
            }
            else if (link == LINK_SET)
            {
                list.AddRange(thisGroup.Values);
            }
            else if (link == LINK_ALL_OTHERS)
            {
                foreach (ObjectPart part in thisGroup.Values)
                {
                    if (part != instance.Part)
                    {
                        list.Add(part);
                    }
                }
            }
            else
            {
                list.Add(thisGroup[link]);
            }

            return list;
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
    }
}
