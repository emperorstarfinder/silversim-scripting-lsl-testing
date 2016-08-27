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

        void StartAnimation(
            ScriptInstance instance,
            UUID agentid,
            string animation,
            string functionname)
        {
            UUID animID = instance.GetAnimationAssetID(animation);
            IAgent agent;
            if(!instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(agentid, out agent))
            {
                instance.ShoutError(functionname + ": agent not in region");
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
                if (!grantinfo.PermsMask.HasFlag(ScriptPermissions.TriggerAnimation) ||
                    grantinfo.PermsGranter == UUI.Unknown)
                {
                    return;
                }
                StartAnimation(instance, grantinfo.PermsGranter.ID, anim, "llStartAnimation");
            }
        }

        [APILevel(APIFlags.OSSL, "osAvatarPlayAnimation")]
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
                ((Script)instance).CheckThreatLevel("osAvatarPlayAnimation", Script.ThreatLevelType.VeryHigh);
                StartAnimation(instance, avatar.AsUUID, animation, "osAvatarPlayAnimation");
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcPlayAnimation")]
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
                ((Script)instance).CheckThreatLevel("osNpcPlayAnimation", Script.ThreatLevelType.VeryHigh);
                StartAnimation(instance, avatar.AsUUID, animation, "osNpcPlayAnimation");
            }
        }

        void StopAnimation(
            ScriptInstance instance,
            UUID agentid,
            string animation,
            string functionname)
        {
            IAgent agent;
            UUID animID = instance.GetAnimationAssetID(animation);
            if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(agentid, out agent))
            {
                instance.ShoutError(functionname + ": permission granter not in region");
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
                if (!grantinfo.PermsMask.HasFlag(ScriptPermissions.TriggerAnimation) ||
                    grantinfo.PermsGranter == UUI.Unknown)
                {
                    return;
                }
                StopAnimation(instance, grantinfo.PermsGranter.ID, anim, "llStopAnimation");
            }
        }

        [APILevel(APIFlags.OSSL, "osAvatarStopAnimation")]
        [Description("stops the specified animation if it is playing on the avatar given.")]
        public void AvatarStopAnimation(
            ScriptInstance instance,
            [Description("UUID of the agent")]
            LSLKey avatar,
            [Description("animation to be stopped")]
            string animation)
        {
            lock (instance)
            {
                ((Script)instance).CheckThreatLevel("osAvatarStopAnimation", Script.ThreatLevelType.VeryHigh);
                StopAnimation(instance, avatar.AsUUID, animation, "osAvatarStopAnimation");
            }
        }

        [APILevel(APIFlags.OSSL, "osNpcStopAnimation")]
        [Description("stops the specified animation if it is playing on the avatar given.")]
        public void NpcStopAnimation(
            ScriptInstance instance,
            [Description("UUID of the agent")]
            LSLKey avatar,
            [Description("animation to be stopped")]
            string animation)
        {
            lock (instance)
            {
                ((Script)instance).CheckThreatLevel("osNpcStopAnimation", Script.ThreatLevelType.VeryHigh);
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

            AnArray res = new AnArray();
            foreach(UUID id in playingAnimations)
            {
                res.Add(id);
            }
            return res;
        }
    }
}
