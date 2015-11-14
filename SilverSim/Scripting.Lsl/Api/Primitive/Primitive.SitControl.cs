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
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        #region Sit Targets
        [APILevel(APIFlags.LSL, "llSitTarget")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void SitTarget(ScriptInstance instance, Vector3 offset, Quaternion rot)
        {
            lock (instance)
            {
                instance.Part.SitTargetOffset = offset;
                instance.Part.SitTargetOrientation = rot;
            }
        }

        [APILevel(APIFlags.LSL, "llLinkSitTarget")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
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

        [APILevel(APIFlags.ASSL, "asGetSitTarget")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
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

        [APILevel(APIFlags.ASSL, "asGetLinkSitTarget")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
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
        [APILevel(APIFlags.LSL, "llAvatarOnSitTarget")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public LSLKey AvatarOnSitTarget(ScriptInstance instance)
        {
            return AvatarOnLinkSitTarget(instance, LINK_THIS);
        }

        [APILevel(APIFlags.LSL, "llAvatarOnLinkSitTarget")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public LSLKey AvatarOnLinkSitTarget(ScriptInstance instance, int link)
        {
            throw new NotImplementedException("llAvatarOnLinkSitTarget(integer)");
        }

        [APILevel(APIFlags.LSL, "llForceMouselook")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void ForceMouselook(ScriptInstance instance, int mouselook)
        {
            lock(instance)
            {
                instance.Part.ForceMouselook = mouselook != 0;
            }
        }

        [APILevel(APIFlags.LSL, "llUnSit")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void UnSit(ScriptInstance instance, LSLKey id)
        {
            throw new NotImplementedException("llUnSit(key)");
        }
        #endregion
    }
}
