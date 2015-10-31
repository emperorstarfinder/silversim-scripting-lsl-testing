// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scene.Types.Script;
using System.Reflection;
using SilverSim.Scripting.Common;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        /// <summary>
        /// Set parameters for light projection in host prim 
        /// </summary>
        [APILevel(APIFlags.OSSL, "osSetProjectionParams")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetProjectionParams(ScriptInstance instance, int projection, LSLKey texture, double fov, double focus, double amb)
        {
            SetLinkProjectionParams(instance, LINK_THIS, projection, texture, fov, focus, amb);
        }

        [APILevel(APIFlags.OSSL, "osSetLinkProjectionParams")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetLinkProjectionParams(ScriptInstance instance, int link, int projection, LSLKey texture, double fov, double focus, double amb)
        {
            ObjectPart.ProjectionParam p = new ObjectPart.ProjectionParam();
            p.IsProjecting = projection != 0;
            p.ProjectionTextureID = texture;
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetProjectionParams(ScriptInstance instance, LSLKey prim, int projection, LSLKey texture, double fov, double focus, double amb)
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
                p.ProjectionTextureID = texture;
                p.ProjectionFOV = fov;
                p.ProjectionFocus = focus;
                p.ProjectionAmbience = amb;
                part.Projection = p;
            }
        }
    }
}
