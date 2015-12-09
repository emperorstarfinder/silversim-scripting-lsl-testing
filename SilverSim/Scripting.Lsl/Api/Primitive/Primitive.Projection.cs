// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System.Reflection;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        /// <summary>
        /// Set parameters for light projection in host prim 
        /// </summary>
        [APILevel(APIFlags.OSSL, "osSetProjectionParams")]
        public void SetProjectionParams(ScriptInstance instance, int projection, LSLKey texture, double fov, double focus, double amb)
        {
            SetLinkProjectionParams(instance, LINK_THIS, projection, texture, fov, focus, amb);
        }

        [APILevel(APIFlags.OSSL, "osSetLinkProjectionParams")]
        public void SetLinkProjectionParams(ScriptInstance instance, int link, int projection, LSLKey texture, double fov, double focus, double amb)
        {
            ObjectPart.ProjectionParam p = new ObjectPart.ProjectionParam();
            p.IsProjecting = projection != 0;
            p.ProjectionTextureID = GetTextureAssetID(instance, texture.ToString());
            p.ProjectionFOV = fov;
            p.ProjectionFocus = focus;
            p.ProjectionAmbience = amb;

            foreach(ObjectPart part in GetLinkTargets(instance, link))
            {
                part.Projection = p;
            }
        }

        /// <summary>
        /// Set parameters for light projection with uuid of target prim
        /// </summary>
        [APILevel(APIFlags.OSSL, "osSetProjectionParams")]
        public void SetProjectionParams(ScriptInstance instance, LSLKey prim, int projection, LSLKey texture, double fov, double focus, double amb)
        {
            lock (instance)
            {
                if (UUID.Zero != prim)
                {
                    instance.CheckThreatLevel(MethodBase.GetCurrentMethod().Name, ScriptInstance.ThreatLevelType.High);
                }

                ObjectPart part;
                if (prim == UUID.Zero)
                {
                    part = instance.Part;
                }
                else
                {
                    try
                    {
                        part = instance.Part.ObjectGroup.Scene.Primitives[prim];
                    }
                    catch
                    {
                        return;
                    }
                }

                ObjectPart.ProjectionParam p = new ObjectPart.ProjectionParam();
                p.IsProjecting = projection != 0;
                p.ProjectionTextureID = GetTextureAssetID(instance, texture.ToString());
                p.ProjectionFOV = fov;
                p.ProjectionFocus = focus;
                p.ProjectionAmbience = amb;
                part.Projection = p;
            }
        }
    }
}
