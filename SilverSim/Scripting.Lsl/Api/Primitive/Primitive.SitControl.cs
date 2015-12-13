// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        #region Sit Targets
        [APILevel(APIFlags.LSL, "llSitTarget")]
        public void SitTarget(ScriptInstance instance, Vector3 offset, Quaternion rot)
        {
            lock (instance)
            {
                instance.Part.SitTargetOffset = offset;
                instance.Part.SitTargetOrientation = rot;
            }
        }

        [APILevel(APIFlags.LSL, "llLinkSitTarget")]
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
        public LSLKey AvatarOnSitTarget(ScriptInstance instance)
        {
            return AvatarOnLinkSitTarget(instance, LINK_THIS);
        }

        [APILevel(APIFlags.LSL, "llAvatarOnLinkSitTarget")]
        public LSLKey AvatarOnLinkSitTarget(ScriptInstance instance, int link)
        {
            lock(instance)
            {
                ObjectPart thisPart = instance.Part;
                ObjectPart part;
                ObjectGroup grp = thisPart.ObjectGroup;
                if(link == 0)
                {
                    link = LINK_ROOT;
                }
                if(link == LINK_THIS)
                {
                    part = thisPart;
                }
                else if(!grp.TryGetValue(link, out part))
                {
                    return UUID.Zero;
                }
                IAgent agent;
                if(grp.AgentSitting.TryGetValue(part, out agent))
                {
                    return agent.Owner.ID;
                }
                return UUID.Zero;
            }
        }

        [APILevel(APIFlags.LSL, "llForceMouselook")]
        public void ForceMouselook(ScriptInstance instance, int mouselook)
        {
            lock(instance)
            {
                instance.Part.ForceMouselook = mouselook != 0;
            }
        }

        [APILevel(APIFlags.LSL, "llUnSit")]
        public void UnSit(ScriptInstance instance, LSLKey id)
        {
            lock(instance)
            {
                ObjectGroup group = instance.Part.ObjectGroup;
                IAgent agent;
                if(group.AgentSitting.TryGetValue(id, out agent))
                {
                    agent.UnSit();
                }
            }
        }
        #endregion
    }
}
