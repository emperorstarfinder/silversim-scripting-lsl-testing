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
using AnimationState = SilverSim.Scene.Types.Agent.AgentAnimationController.AnimationState;

namespace SilverSim.Scripting.Lsl.Api.AnimationOverride
{
    [ScriptApiName("AnimationOverride")]
    [LSLImplementation]
    [Description("LSL/OSSL AnimationOverride API")]
    public class AnimationOverrideApi : IScriptApi, IPlugin
    {
        private static readonly Dictionary<string, AnimationState> m_DefaultAnimationTranslate = new Dictionary<string, AnimationState>();
        static AnimationOverrideApi()
        {
            m_DefaultAnimationTranslate["Crouching"] = AnimationState.Crouching;
            m_DefaultAnimationTranslate["CrouchWalking"] = AnimationState.CrouchWalking;
            m_DefaultAnimationTranslate["Falling Down"] = AnimationState.FallingDown;
            m_DefaultAnimationTranslate["Flying"] = AnimationState.Flying;
            m_DefaultAnimationTranslate["FlyingSlow"] = AnimationState.FlyingSlow;
            m_DefaultAnimationTranslate["Hovering"] = AnimationState.Hovering;
            m_DefaultAnimationTranslate["Hovering Down"] = AnimationState.HoveringDown;
            m_DefaultAnimationTranslate["Hovering Up"] = AnimationState.HoveringUp;
            m_DefaultAnimationTranslate["Jumping"] = AnimationState.Jumping;
            m_DefaultAnimationTranslate["Landing"] = AnimationState.Landing;
            m_DefaultAnimationTranslate["PreJumping"] = AnimationState.Prejumping;
            m_DefaultAnimationTranslate["Running"] = AnimationState.Running;
            m_DefaultAnimationTranslate["Sitting"] = AnimationState.Sitting;
            m_DefaultAnimationTranslate["Sitting on Ground"] = AnimationState.SittingOnGround;
            m_DefaultAnimationTranslate["Standing"] = AnimationState.Standing;
            m_DefaultAnimationTranslate["Standing Up"] = AnimationState.StandingUp;
            m_DefaultAnimationTranslate["Striding"] = AnimationState.Striding;
            m_DefaultAnimationTranslate["Soft Landing"] = AnimationState.SoftLanding;
            m_DefaultAnimationTranslate["Taking Off"] = AnimationState.TakingOff;
            m_DefaultAnimationTranslate["Turning Left"] = AnimationState.TurningLeft;
            m_DefaultAnimationTranslate["Turning Right"] = AnimationState.TurningRight;
            m_DefaultAnimationTranslate["Walking"] = AnimationState.Walking;

            /* extensions */
            m_DefaultAnimationTranslate["Floating"] = AnimationState.Floating;
            m_DefaultAnimationTranslate["Swimming"] = AnimationState.Swimming;
            m_DefaultAnimationTranslate["SwimmingSlow"] = AnimationState.SwimmingSlow;
            m_DefaultAnimationTranslate["Swimming Down"] = AnimationState.SwimmingDown;
            m_DefaultAnimationTranslate["Swimming Up"] = AnimationState.SwimmingSlow;
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
                AnimationState defaultanim = agent.GetDefaultAnimation();
                foreach(KeyValuePair<string, AnimationState> kvp in m_DefaultAnimationTranslate)
                {
                    if(kvp.Value == defaultanim)
                    {
                        return kvp.Key;
                    }
                }
                string n = defaultanim.ToString();
                return char.ToUpper(n[0]).ToString() + n.Substring(1);
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
                    grantinfo.PermsGranter == UGUI.Unknown)
                {
                    return;
                }

                AnimationState selState;
                if(!m_DefaultAnimationTranslate.TryGetValue(anim_state, out selState))
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
                    agent.SetAnimationOverride(selState, anim);
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
                    grantinfo.PermsGranter == UGUI.Unknown)
                {
                    return string.Empty;
                }

                AnimationState selState;
                if (!m_DefaultAnimationTranslate.TryGetValue(anim_state, out selState))
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

                return instance.FindAnimationName(agent.GetAnimationOverride(selState));
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
                    grantinfo.PermsGranter == UGUI.Unknown)
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

                AnimationState selState;
                if (anim_state == "ALL")
                {
                    agent.ResetAnimationOverride();
                }
                else if(m_DefaultAnimationTranslate.TryGetValue(anim_state, out selState))
                {
                    agent.ResetAnimationOverride(selState);
                }
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

            [AllowKeyOnlyEnumerationOnKeyValuePairAttribute]
            public AnimationOverrideEnumerator GetLslForeachEnumerator()
            {
                lock (m_Instance)
                {
                    IAgent agent;
                    ObjectPartInventoryItem.PermsGranterInfo grantinfo = m_Instance.Item.PermsGranter;
                    if (((grantinfo.PermsMask & ScriptPermissions.OverrideAnimations) == 0 &&
                        (grantinfo.PermsMask & ScriptPermissions.TriggerAnimation) == 0) ||
                        grantinfo.PermsGranter == UGUI.Unknown)
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
                    foreach(KeyValuePair<string, AnimationState> kvp in m_DefaultAnimationTranslate)
                    {
                        list.Add(new KeyValuePair<string, string>(kvp.Key, m_Instance.FindAnimationName(agent.GetAnimationOverride(kvp.Value))));
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
                            grantinfo.PermsGranter == UGUI.Unknown)
                        {
                            return string.Empty;
                        }

                        AnimationState selState;
                        if (!m_DefaultAnimationTranslate.TryGetValue(anim_state, out selState))
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

                        return m_Instance.FindAnimationName(agent.GetAnimationOverride(selState));
                    }
                }
                set
                {
                    lock (m_Instance)
                    {
                        IAgent agent;
                        ObjectPartInventoryItem.PermsGranterInfo grantinfo = m_Instance.Item.PermsGranter;

                        if ((grantinfo.PermsMask & ScriptPermissions.OverrideAnimations) == 0 ||
                            grantinfo.PermsGranter == UGUI.Unknown)
                        {
                            return;
                        }

                        AnimationState selState;
                        if (!m_DefaultAnimationTranslate.TryGetValue(anim_state, out selState))
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
                                agent.ResetAnimationOverride(selState);
                            }
                            else
                            {
                                agent.SetAnimationOverride(selState, value);
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
