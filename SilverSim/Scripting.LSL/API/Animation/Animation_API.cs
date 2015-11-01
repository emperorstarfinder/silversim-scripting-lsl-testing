// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Animation
{
    [ScriptApiName("Animation")]
    [LSLImplementation]
    public partial class AnimationApi : IScriptApi, IPlugin
    {
        public AnimationApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        UUID GetAnimationAssetID(ScriptInstance instance, string item)
        {
            UUID assetID;
            if (!UUID.TryParse(item, out assetID))
            {
#warning Implement viewer built-in animations
                /* must be an inventory item */
                lock (instance)
                {
                    ObjectPartInventoryItem i = instance.Part.Inventory[item];
                    if (i.InventoryType != Types.Inventory.InventoryType.Animation)
                    {
                        throw new InvalidOperationException(string.Format("Inventory item {0} is not an animation", item));
                    }
                    assetID = i.AssetID;
                }
            }
            return assetID;
        }

        [APILevel(APIFlags.LSL, "llStartAnimation")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void StartAnimation(
            ScriptInstance instance,
            [LSLTooltip("animation to be played")]
            string anim)
        {
            lock (instance)
            {
                UUID animID = GetAnimationAssetID(instance, anim);
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
        [LSLTooltip("causes an animation to be played on the specified avatar.")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void AvatarPlayAnimation(
            ScriptInstance instance, 
            [LSLTooltip("UUID of the agent")]
            LSLKey avatar, 
            [LSLTooltip("animation to be played")]
            string animation)
        {
            lock (instance)
            {
                instance.CheckThreatLevel("osAvatarPlayAnimation", ScriptInstance.ThreatLevelType.VeryHigh);
                UUID animID = GetAnimationAssetID(instance, animation);
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void StopAnimation(
            ScriptInstance instance, 
            [LSLTooltip("animation to be stopped")]
            string anim)
        {
            lock (instance)
            {
                UUID animID = GetAnimationAssetID(instance, anim);
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

                agent.StopAnimation(animID, instance.Part.ID);
            }
        }

        [APILevel(APIFlags.OSSL, "osAvatarStopAnimation")]
        [LSLTooltip("stops the specified animation if it is playing on the avatar given.")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void AvatarStopAnimation(
            ScriptInstance instance,
            [LSLTooltip("UUID of the agent")]
            LSLKey avatar,
            [LSLTooltip("animation to be stopped")]
            string animation)
        {
            lock (instance)
            {
                instance.CheckThreatLevel("osAvatarStopAnimation", ScriptInstance.ThreatLevelType.VeryHigh);
                UUID animID = GetAnimationAssetID(instance, animation);
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
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

        [APILevel(APIFlags.LSL, "llGetAnimation")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal string GetAnimation(ScriptInstance instance, LSLKey agent)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetAnimationList")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal string GetAnimationList(ScriptInstance instance, LSLKey agent)
        {
            throw new NotImplementedException();
        }
    }
}
