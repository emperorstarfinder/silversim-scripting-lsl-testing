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

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        #region Sit Targets
        private void SetSitTarget(ObjectPart part, Vector3 offset, Quaternion rot)
        {
            bool sitTargetActive = !(offset.ApproxEquals(Vector3.Zero, double.Epsilon) && rot.ApproxEquals(Quaternion.Identity, double.Epsilon));
            if (!sitTargetActive)
            {
                part.IsSitTargetActive = false;
            }
            part.SitTargetOffset = offset;
            part.SitTargetOrientation = rot;
            if (sitTargetActive)
            {
                part.IsSitTargetActive = true;
            }
        }

        [APILevel(APIFlags.LSL, "llSitTarget")]
        public void SitTarget(ScriptInstance instance, Vector3 offset, Quaternion rot)
        {
            lock (instance)
            {
                SetSitTarget(instance.Part, offset, rot);
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

                SetSitTarget(part, offset, rot);
            }
        }

        [APILevel(APIFlags.ASSL, "asGetSitTarget")]
        public AnArray GetSitTarget(ScriptInstance instance)
        {
            var res = new AnArray();
            lock(instance)
            {
                res.Add(instance.Part.IsSitTargetActive);
                res.Add(instance.Part.SitTargetOffset);
                res.Add(instance.Part.SitTargetOrientation);
            }
            return res;
        }

        [APILevel(APIFlags.ASSL, "asGetLinkSitTarget")]
        public AnArray GetLinkSitTarget(ScriptInstance instance, int link)
        {
            ObjectPart part;
            var res = new AnArray();
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

                res.Add(part.IsSitTargetActive);
                res.Add(part.SitTargetOffset);
                res.Add(part.SitTargetOrientation);
            }
            return res;
        }
        #endregion

        #region Sit control
        [APILevel(APIFlags.LSL, "llAvatarOnSitTarget")]
        public LSLKey AvatarOnSitTarget(ScriptInstance instance) =>
            AvatarOnLinkSitTarget(instance, LINK_THIS);

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
