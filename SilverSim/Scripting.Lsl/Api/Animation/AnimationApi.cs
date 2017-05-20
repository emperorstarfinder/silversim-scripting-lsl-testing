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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Script;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Animation
{
    [ScriptApiName("Animation")]
    [LSLImplementation]
    [Description("LSL/OSSL Animation API")]
    public partial class AnimationApi : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        private void StartAnimation(
            ScriptInstance instance,
            UUID agentid,
            string animation,
            string functionname)
        {
            UUID animID = instance.GetAnimationAssetID(animation);
            IAgent agent;
            if(!instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(agentid, out agent))
            {
                instance.ShoutError(new LocalizedScriptMessage(this, "Function0AgentNotInRegion", "{0}: agent not in region", functionname));
                return;
            }

            agent.PlayAnimation(animID, instance.Part.ID);
        }

        [APILevel(APIFlags.LSL, "llStartAnimation")]
        public void StartAnimation(
            ScriptInstance instance,
            [Description("animation to be played")]
            string anim)
        {
            lock (instance)
            {
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if ((grantinfo.PermsMask & ScriptPermissions.TriggerAnimation) == 0 ||
                    grantinfo.PermsGranter == UUI.Unknown)
                {
                    return;
                }
                StartAnimation(instance, grantinfo.PermsGranter.ID, anim, "llStartAnimation");
            }
        }

        [APILevel(APIFlags.OSSL, "osAvatarPlayAnimation")]
        [ThreatLevelRequired(ThreatLevel.VeryHigh)]
        [Description("causes an animation to be played on the specified avatar.")]
        public void AvatarPlayAnimation(
            ScriptInstance instance,
            [Description("UUID of the agent")]
            LSLKey avatar,
            [Description("animation to be played")]
            string animation)
        {
            lock (instance)
            {
                StartAnimation(instance, avatar.AsUUID, animation, "osAvatarPlayAnimation");
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcPlayAnimation")]
        [ThreatLevelRequired(ThreatLevel.VeryHigh)]
        [Description("causes an animation to be played on the specified avatar.")]
        public void NpcPlayAnimation(
            ScriptInstance instance,
            [Description("UUID of the agent")]
            LSLKey avatar,
            [Description("animation to be played")]
            string animation)
        {
            lock (instance)
            {
                StartAnimation(instance, avatar.AsUUID, animation, "osNpcPlayAnimation");
            }
        }

        private void StopAnimation(
            ScriptInstance instance,
            UUID agentid,
            string animation,
            string functionname)
        {
            IAgent agent;
            UUID animID = instance.GetAnimationAssetID(animation);
            if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(agentid, out agent))
            {
                instance.ShoutError(new LocalizedScriptMessage(this, "Function0PermissionGranterNotInRegion", "{0}: permission granter not in region", functionname));
                return;
            }

            agent.StopAnimation(animID, instance.Part.ID);
        }

        [APILevel(APIFlags.LSL, "llStopAnimation")]
        public void StopAnimation(
            ScriptInstance instance,
            [Description("animation to be stopped")]
            string anim)
        {
            lock (instance)
            {
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if ((grantinfo.PermsMask & ScriptPermissions.TriggerAnimation) == 0 ||
                    grantinfo.PermsGranter == UUI.Unknown)
                {
                    return;
                }
                StopAnimation(instance, grantinfo.PermsGranter.ID, anim, "llStopAnimation");
            }
        }

        [APILevel(APIFlags.OSSL, "osAvatarStopAnimation")]
        [Description("stops the specified animation if it is playing on the avatar given.")]
        [ThreatLevelRequired(ThreatLevel.VeryHigh)]
        public void AvatarStopAnimation(
            ScriptInstance instance,
            [Description("UUID of the agent")]
            LSLKey avatar,
            [Description("animation to be stopped")]
            string animation)
        {
            lock (instance)
            {
                StopAnimation(instance, avatar.AsUUID, animation, "osAvatarStopAnimation");
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcStopAnimation")]
        [Description("stops the specified animation if it is playing on the avatar given.")]
        [ThreatLevelRequired(ThreatLevel.VeryHigh)]
        public void NpcStopAnimation(
            ScriptInstance instance,
            [Description("UUID of the agent")]
            LSLKey avatar,
            [Description("animation to be stopped")]
            string animation)
        {
            lock (instance)
            {
                StopAnimation(instance, avatar.AsUUID, animation, "osNpcStopAnimation");
            }
        }

        [APILevel(APIFlags.LSL, "llGetAnimationList")]
        public AnArray GetAnimationList(ScriptInstance instance, LSLKey agentkey)
        {
            List<UUID> playingAnimations;
            lock(instance)
            {
                IAgent agent;
                if(!instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(agentkey, out agent))
                {
                    return new AnArray();
                }
                playingAnimations = agent.GetPlayingAnimations();
            }

            var res = new AnArray();
            foreach(UUID id in playingAnimations)
            {
                res.Add(id);
            }
            return res;
        }
    }
}
