// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types;
using SilverSim.Types;
using SilverSim.Scripting.Common;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        #region Sit Targets
        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSitTarget")]
        public void SitTarget(ScriptInstance instance, Vector3 offset, Quaternion rot)
        {
            lock (instance)
            {
                instance.Part.SitTargetOffset = offset;
                instance.Part.SitTargetOrientation = rot;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llLinkSitTarget")]
        public void LinkSitTarget(ScriptInstance instance, int link, Vector3 offset, Quaternion rot)
        {
            ObjectPart part;
            lock (instance)
            {
                if (link == LINK_THIS)
                {
                    part = instance.Part;
                }
                else if (!instance.Part.ObjectGroup.TryGetValue(link, out part))
                {
                    return;
                }

                part.SitTargetOffset = offset;
                part.SitTargetOrientation = rot;
            }
        }

        [APILevel(APIFlags.ASSL)]
        [ScriptFunctionName("asGetSitTarget")]
        public AnArray GetSitTarget(ScriptInstance instance)
        {
            AnArray res = new AnArray();
            lock(instance)
            {
                res.Add(instance.Part.SitTargetOffset);
                res.Add(instance.Part.SitTargetOrientation);
            }
            return res;
        }

        [APILevel(APIFlags.ASSL)]
        [ScriptFunctionName("asGetLinkSitTarget")]
        public AnArray GetLinkSitTarget(ScriptInstance instance, int link)
        {
            ObjectPart part;
            AnArray res = new AnArray();
            lock (instance)
            {
                if (link == LINK_THIS)
                {
                    part = instance.Part;
                }
                else if (!instance.Part.ObjectGroup.TryGetValue(link, out part))
                {
                    return res;
                }

                res.Add(part.SitTargetOffset);
                res.Add(part.SitTargetOrientation);
            }
            return res;
        }
        #endregion

        #region Sit control
        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llAvatarOnSitTarget")]
        public LSLKey AvatarOnSitTarget(ScriptInstance instance)
        {
            return AvatarOnLinkSitTarget(instance, LINK_THIS);
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llAvatarOnLinkSitTarget")]
        public LSLKey AvatarOnLinkSitTarget(ScriptInstance instance, int link)
        {
            throw new NotImplementedException("llAvatarOnLinkSitTarget(integer)");
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llForceMouselook")]
        public void ForceMouselook(ScriptInstance instance, int mouselook)
        {
            throw new NotImplementedException("llForceMouselook(integer)");
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llUnSit")]
        public void UnSit(ScriptInstance instance, LSLKey id)
        {
            throw new NotImplementedException("llUnSit(key)");
        }
        #endregion
    }
}
