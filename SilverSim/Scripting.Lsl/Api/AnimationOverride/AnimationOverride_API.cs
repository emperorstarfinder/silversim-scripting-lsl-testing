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

namespace SilverSim.Scripting.Lsl.Api.AnimationOverride
{
    [ScriptApiName("AnimationOverride")]
    [LSLImplementation]
    [Description("LSL/OSSL AnimationOverride API")]
    public partial class AnimationOverrideApi : IScriptApi, IPlugin
    {
        static readonly Dictionary<string, string> m_DefaultAnimationTranslate = new Dictionary<string, string>();
        static AnimationOverrideApi()
        {
            m_DefaultAnimationTranslate["crouching"] = "Crouching";
            m_DefaultAnimationTranslate["crouchwalking"] = "CrouchWalking";
            m_DefaultAnimationTranslate["falling down"] = "Falling Down";
            m_DefaultAnimationTranslate["flying"] = "Flying";
            m_DefaultAnimationTranslate["flyingslow"] = "FlyingSlow";
            m_DefaultAnimationTranslate["hovering"] = "Hovering";
            m_DefaultAnimationTranslate["hovering down"] = "Hovering Down";
            m_DefaultAnimationTranslate["hovering up"] = "Hovering Up";
            m_DefaultAnimationTranslate["jumping"] = "Jumping";
            m_DefaultAnimationTranslate["landing"] = "Landing";
            m_DefaultAnimationTranslate["prejumping"] = "PreJumping";
            m_DefaultAnimationTranslate["running"] = "Running";
            m_DefaultAnimationTranslate["sitting"] = "Sitting";
            m_DefaultAnimationTranslate["sitting on ground"] = "Sitting on Ground";
            m_DefaultAnimationTranslate["standing"] = "Standing";
            m_DefaultAnimationTranslate["standing up"] = "Standing Up";
            m_DefaultAnimationTranslate["striding"] = "Striding";
            m_DefaultAnimationTranslate["soft landing"] = "Soft Landing";
            m_DefaultAnimationTranslate["taking off"] = "Taking Off";
            m_DefaultAnimationTranslate["turning left"] = "Turning Left";
            m_DefaultAnimationTranslate["turning right"] = "Turning Right";
            m_DefaultAnimationTranslate["walking"] = "Walking";
        }

        public AnimationOverrideApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL, "llGetAnimation")]
        public string GetAnimation(ScriptInstance instance, LSLKey agentkey)
        {
            lock (instance)
            {
                IAgent agent;
                if (!instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(agentkey, out agent))
                {
                    return string.Empty;
                }
                string defaultanim = agent.GetDefaultAnimation();
                if (defaultanim.Length > 0)
                {
                    string res;
                    if(m_DefaultAnimationTranslate.TryGetValue(defaultanim, out res))
                    {
                        return res;
                    }
                    return char.ToUpper(defaultanim[0]).ToString() + defaultanim.Substring(1);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetAnimationOverride")]
        public void SetAnimationOverride(ScriptInstance instance, string anim_state, string anim)
        {
            lock (instance)
            {
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;

                if ((grantinfo.PermsMask & ScriptPermissions.OverrideAnimations) == 0 ||
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
                    instance.ShoutError("llSetAnimationOverride: permission granter not in region");
                    return;
                }

                try
                {
                    agent.SetAnimationOverride(anim_state, anim);
                }
                catch (Exception e)
                {
                    instance.ShoutError(e.Message);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetAnimationOverride")]
        public string GetAnimationOverride(ScriptInstance instance, string anim_state)
        {
            lock (instance)
            {
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if (((grantinfo.PermsMask & ScriptPermissions.OverrideAnimations) == 0 &&
                    (grantinfo.PermsMask & ScriptPermissions.TriggerAnimation) == 0) ||
                    grantinfo.PermsGranter == UUI.Unknown)
                {
                    return string.Empty;
                }
                try
                {
                    agent = instance.Part.ObjectGroup.Scene.Agents[grantinfo.PermsGranter.ID];
                }
                catch
                {
                    instance.ShoutError("llSetAnimationOverride: permission granter not in region");
                    return string.Empty;
                }

                agent.ResetAnimationOverride(anim_state);
                return anim_state;
            }
        }

        [APILevel(APIFlags.LSL, "llResetAnimationOverride")]
        public void ResetAnimationOverride(ScriptInstance instance, string anim_state)
        {
            lock (instance)
            {
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if ((grantinfo.PermsMask & ScriptPermissions.OverrideAnimations) == 0 ||
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
                    instance.ShoutError("llSetAnimationOverride: permission granter not in region");
                    return;
                }

                agent.ResetAnimationOverride(anim_state);
            }
        }
    }
}
