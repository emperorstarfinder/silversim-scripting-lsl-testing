// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Animation
{
    [ScriptApiName("Animation")]
    [LSLImplementation]
    [Description("LSL/OSSL Animation API")]
    public partial class AnimationApi : IScriptApi, IPlugin
    {
        public AnimationApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL, "llStartAnimation")]
        public void StartAnimation(
            ScriptInstance instance,
            [LSLTooltip("animation to be played")]
            string anim)
        {
            lock (instance)
            {
                UUID animID = instance.GetAnimationAssetID(anim);
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if ((grantinfo.PermsMask & ScriptPermissions.TriggerAnimation) == 0 ||
                    grantinfo.PermsGranter == UUI.Unknown)
                {
                    return;
                }
                try
                {
                    agent = instance.Part.ObjectGroup.Scene.Agents[grantinfo.PermsGranter.ID];
                }
                catch
                {
                    instance.ShoutError("llStartAnimation: permission granter not in region");
                    return;
                }

                agent.PlayAnimation(animID, instance.Part.ID);
            }
        }

        [APILevel(APIFlags.OSSL, "osAvatarPlayAnimation")]
        [APILevel(APIFlags.OSSL, "osNpcPlayAnimation")]
        [LSLTooltip("causes an animation to be played on the specified avatar.")]
        public void AvatarPlayAnimation(
            ScriptInstance instance, 
            [LSLTooltip("UUID of the agent")]
            LSLKey avatar, 
            [LSLTooltip("animation to be played")]
            string animation)
        {
            lock (instance)
            {
                instance.CheckThreatLevel("osAvatarPlayAnimation", ScriptInstance.ThreatLevelType.VeryHigh);
                UUID animID = instance.GetAnimationAssetID(animation);
                IAgent agent;
                try
                {
                    agent = instance.Part.ObjectGroup.Scene.Agents[avatar];
                }
                catch
                {
                    instance.ShoutError("osAvatarPlayAnimation: agent not in region");
                    return;
                }

                agent.PlayAnimation(animID, instance.Part.ID);
            }
        }

        [APILevel(APIFlags.LSL, "llStopAnimation")]
        public void StopAnimation(
            ScriptInstance instance, 
            [LSLTooltip("animation to be stopped")]
            string anim)
        {
            lock (instance)
            {
                UUID animID = instance.GetAnimationAssetID(anim);
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if ((grantinfo.PermsMask & ScriptPermissions.TriggerAnimation) == 0 ||
                    grantinfo.PermsGranter == UUI.Unknown)
                {
                    return;
                }
                try
                {
                    agent = instance.Part.ObjectGroup.Scene.Agents[grantinfo.PermsGranter.ID];
                }
                catch
                {
                    instance.ShoutError("llStopAnimation: permission granter not in region");
                    return;
                }

                if (grantinfo.PermsGranter.EqualsGrid(agent.Owner) && (grantinfo.PermsMask & ScriptPermissions.TriggerAnimation) != 0)
                {
                    agent.StopAnimation(animID, instance.Part.ID);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osAvatarStopAnimation")]
        [APILevel(APIFlags.OSSL, "osNpcStopAnimation")]
        [LSLTooltip("stops the specified animation if it is playing on the avatar given.")]
        public void AvatarStopAnimation(
            ScriptInstance instance,
            [LSLTooltip("UUID of the agent")]
            LSLKey avatar,
            [LSLTooltip("animation to be stopped")]
            string animation)
        {
            lock (instance)
            {
                instance.CheckThreatLevel("osAvatarStopAnimation", ScriptInstance.ThreatLevelType.VeryHigh);
                UUID animID = instance.GetAnimationAssetID(animation);
                IAgent agent;
                try
                {
                    agent = instance.Part.ObjectGroup.Scene.Agents[avatar];
                }
                catch
                {
                    instance.ShoutError("osAvatarStopAnimation: agent not in region");
                    return;
                }

                agent.PlayAnimation(animID, instance.Part.ID);
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

            AnArray res = new AnArray();
            foreach(UUID id in playingAnimations)
            {
                res.Add(id);
            }
            return res;
        }
    }
}
