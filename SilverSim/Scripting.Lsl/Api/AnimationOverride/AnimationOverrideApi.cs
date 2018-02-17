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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.AnimationOverride
{
    [ScriptApiName("AnimationOverride")]
    [LSLImplementation]
    [Description("LSL/OSSL AnimationOverride API")]
    public class AnimationOverrideApi : IScriptApi, IPlugin
    {
        private static readonly Dictionary<string, string> m_DefaultAnimationTranslate = new Dictionary<string, string>();
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

                if(!m_DefaultAnimationTranslate.TryGetValue(anim_state, out anim_state))
                {
                    return;
                }

                try
                {
                    agent = instance.Part.ObjectGroup.Scene.Agents[grantinfo.PermsGranter.ID];
                }
                catch
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0PermissionGranterNotInRegion", "{0}: permission granter not in region", "llSetAnimationOverride"));
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

                if (!m_DefaultAnimationTranslate.TryGetValue(anim_state, out anim_state))
                {
                    return string.Empty;
                }

                try
                {
                    agent = instance.Part.ObjectGroup.Scene.Agents[grantinfo.PermsGranter.ID];
                }
                catch
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0PermissionGranterNotInRegion", "{0}: permission granter not in region", "llGetAnimationOverride"));
                    return string.Empty;
                }

                return agent.GetAnimationOverride(anim_state);
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
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0PermissionGranterNotInRegion", "{0}: permission granter not in region", "llResetAnimationOverride"));
                    return;
                }

                agent.ResetAnimationOverride(anim_state);
            }
        }

        public sealed class AnimationOverrideEnumerator : IEnumerator<KeyValuePair<string, string>>
        {
            private readonly KeyValuePair<string, string>[] m_Animations;
            private int m_Position = -1;

            public AnimationOverrideEnumerator(KeyValuePair<string, string>[] anims)
            {
                m_Animations = anims;
            }

            public KeyValuePair<string, string> Current => m_Animations[m_Position];

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                /* intentionally left empty */
            }

            public bool MoveNext() => ++m_Position < m_Animations.Length;

            public void Reset() => m_Position = -1;
        }

        [APIExtension(APIExtension.Properties, "animationoverride")]
        [APIDisplayName("animationoverride")]
        public class AnimationOverrideAccessor
        {
            private readonly ScriptInstance m_Instance;

            public AnimationOverrideAccessor(ScriptInstance instance)
            {
                m_Instance = instance;
            }

            [AllowKeyOnlyEnumerationOnKeyValuePair]
            public AnimationOverrideEnumerator GetLslForeachEnumerator()
            {
                lock (m_Instance)
                {
                    IAgent agent;
                    ObjectPartInventoryItem.PermsGranterInfo grantinfo = m_Instance.Item.PermsGranter;
                    if (((grantinfo.PermsMask & ScriptPermissions.OverrideAnimations) == 0 &&
                        (grantinfo.PermsMask & ScriptPermissions.TriggerAnimation) == 0) ||
                        grantinfo.PermsGranter == UUI.Unknown)
                    {
                        return new AnimationOverrideEnumerator(new KeyValuePair<string, string>[0]);
                    }
                    try
                    {
                        agent = m_Instance.Part.ObjectGroup.Scene.Agents[grantinfo.PermsGranter.ID];
                    }
                    catch
                    {
                        m_Instance.ShoutError(new LocalizedScriptMessage(this, "Function0PermissionGranterNotInRegion", "{0}: permission granter not in region", "llGetAnimationOverride"));
                        return new AnimationOverrideEnumerator(new KeyValuePair<string, string>[0]);
                    }

                    var list = new List<KeyValuePair<string, string>>();
                    foreach(KeyValuePair<string, string> kvp in m_DefaultAnimationTranslate)
                    {
                        list.Add(new KeyValuePair<string, string>(kvp.Key, agent.GetAnimationOverride(kvp.Value)));
                    }
                    return new AnimationOverrideEnumerator(list.ToArray());
                }
            }

            public string this[string anim_state]
            {
                get
                {
                    IAgent agent;
                    lock (m_Instance)
                    {
                        ObjectPartInventoryItem.PermsGranterInfo grantinfo = m_Instance.Item.PermsGranter;
                        if (((grantinfo.PermsMask & ScriptPermissions.OverrideAnimations) == 0 &&
                            (grantinfo.PermsMask & ScriptPermissions.TriggerAnimation) == 0) ||
                            grantinfo.PermsGranter == UUI.Unknown)
                        {
                            return string.Empty;
                        }

                        if (!m_DefaultAnimationTranslate.TryGetValue(anim_state, out anim_state))
                        {
                            return string.Empty;
                        }

                        try
                        {
                            agent = m_Instance.Part.ObjectGroup.Scene.Agents[grantinfo.PermsGranter.ID];
                        }
                        catch
                        {
                            m_Instance.ShoutError(new LocalizedScriptMessage(this, "Function0PermissionGranterNotInRegion", "{0}: permission granter not in region", "llGetAnimationOverride"));
                            return string.Empty;
                        }

                        return agent.GetAnimationOverride(anim_state);
                    }
                }
                set
                {
                    lock (m_Instance)
                    {
                        IAgent agent;
                        ObjectPartInventoryItem.PermsGranterInfo grantinfo = m_Instance.Item.PermsGranter;

                        if ((grantinfo.PermsMask & ScriptPermissions.OverrideAnimations) == 0 ||
                            grantinfo.PermsGranter == UUI.Unknown)
                        {
                            return;
                        }

                        if (!m_DefaultAnimationTranslate.TryGetValue(anim_state, out anim_state))
                        {
                            return;
                        }

                        try
                        {
                            agent = m_Instance.Part.ObjectGroup.Scene.Agents[grantinfo.PermsGranter.ID];
                        }
                        catch
                        {
                            m_Instance.ShoutError(new LocalizedScriptMessage(this, "Function0PermissionGranterNotInRegion", "{0}: permission granter not in region", "llSetAnimationOverride"));
                            return;
                        }

                        try
                        {
                            if (string.IsNullOrEmpty(value))
                            {
                                agent.ResetAnimationOverride(anim_state);
                            }
                            else
                            {
                                agent.SetAnimationOverride(anim_state, value);
                            }
                        }
                        catch (Exception e)
                        {
                            m_Instance.ShoutError(e.Message);
                        }
                    }
                }
            }
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "AnimationOverride")]
        public AnimationOverrideAccessor GetAccessor(ScriptInstance instance) => 
            new AnimationOverrideAccessor(instance);
    }
}
