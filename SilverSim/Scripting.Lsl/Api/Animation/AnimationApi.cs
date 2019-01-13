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
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Script;
using System.Collections;
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
            ObjectGroup grp = instance.Part.ObjectGroup;
            SceneInterface scene = grp.Scene;
            if(!scene.RootAgents.TryGetValue(agentid, out agent))
            {
                instance.ShoutError(new LocalizedScriptMessage(this, "Function0AgentNotInRegion", "{0}: agent not in region", functionname));
                return;
            }

            AssetServiceInterface assetService = scene.AssetService;
            AssetMetadata metadata;
            AssetData data;
            if(animID.IsInternalAnimationID())
            {
                /* anim is an internal one so viewer knows it */
            }
            else if (!assetService.Metadata.TryGetValue(animID, out metadata))
            {
                if (grp.IsAttached) /* on attachments, we have to fetch from agent eventually */
                {
                    IAgent owner;
                    if (!grp.Scene.RootAgents.TryGetValue(grp.Owner.ID, out owner))
                    {
                        return;
                    }
                    if(!owner.AssetService.TryGetValue(animID, out data))
                    {
                        /* not found */
                        return;
                    }
                    assetService.Store(data);
                    if(data.Type != AssetType.Animation)
                    {
                        /* ignore wrong asset here */
                        return;
                    }
                }
                else
                {
                    /* ignore missing asset here */
                    return;
                }
            }
            else if(metadata.Type != AssetType.Animation)
            {
                /* ignore wrong asset here */
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
                    grantinfo.PermsGranter == UGUI.Unknown)
                {
                    return;
                }
                StartAnimation(instance, grantinfo.PermsGranter.ID, anim, "llStartAnimation");
            }
        }

        [APILevel(APIFlags.OSSL, "osAvatarPlayAnimation")]
        [CheckFunctionPermission]
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
        [CheckFunctionPermission]
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
            if (!instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(agentid, out agent))
            {
                instance.ShoutError(new LocalizedScriptMessage(this, "Function0AgentNotInRegion", "{0}: agent not in region", functionname));
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
                    grantinfo.PermsGranter == UGUI.Unknown)
                {
                    return;
                }
                StopAnimation(instance, grantinfo.PermsGranter.ID, anim, "llStopAnimation");
            }
        }

        [APILevel(APIFlags.OSSL, "osAvatarStopAnimation")]
        [Description("stops the specified animation if it is playing on the avatar given.")]
        [CheckFunctionPermission("osAvatarPlayAnimation")]
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
        [CheckFunctionPermission("osNpcPlayAnimation")]
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
        public AnArray GetAnimationList(ScriptInstance instance, LSLKey agentkey) => GetAnimationListInternal(instance, agentkey);

        private static AnArray GetAnimationListInternal(ScriptInstance instance, LSLKey agentkey)
        {
            var res = new AnArray();
            lock (instance)
            {
                IAgent agent;
                if (!instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(agentkey, out agent))
                {
                    return new AnArray();
                }

                foreach (UUID id in agent.GetPlayingAnimations())
                {
                    res.Add(instance.FindAnimationName(id));
                }
            }
            return res;
        }

        [APILevel(APIFlags.ASSL, "asSetSitAnimation")]
        public void SetSitAnimation(ScriptInstance instance, string animation)
        {
            lock(instance)
            {
                instance.Part.SitAnimation = animation;
            }
        }

        [APILevel(APIFlags.ASSL, "asGetSitAnimation")]
        public string GetSitAnimation(ScriptInstance instance)
        {
            lock(instance)
            {
                return instance.Part.SitAnimation;
            }
        }

        public const int LINK_THIS = -4;

        [APILevel(APIFlags.ASSL, "asSetLinkSitAnimation")]
        public void SetSitAnimation(ScriptInstance instance, int link, string animation)
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
                part.SitAnimation = animation;
            }
        }

        [APILevel(APIFlags.ASSL, "asGetLinkSitAnimation")]
        public string GetSitAnimation(ScriptInstance instance, int link)
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
                    return string.Empty;
                }
                return part.SitAnimation;
            }
        }

        public sealed class AnimationListEnumerator : IEnumerator<LSLKey>
        {
            private readonly AnArray m_Animations;
            private int m_Position = -1;

            public AnimationListEnumerator(AnArray anims)
            {
                m_Animations = anims;
            }

            public LSLKey Current => m_Animations[m_Position].AsUUID;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                /* intentionally left empty */
            }

            public bool MoveNext() => ++m_Position < m_Animations.Count;

            public void Reset() => m_Position = -1;
        }

        [APIExtension(APIExtension.Properties, "animationlist")]
        [APIDisplayName("animationlist")]
        [ImplementsCustomTypecasts]
        public sealed class AnimationListDetails
        {
            private readonly ScriptInstance m_Instance;
            private readonly LSLKey m_Agent;

            public AnimationListDetails(ScriptInstance instance, LSLKey agent)
            {
                m_Instance = instance;
                m_Agent = agent;
            }

            public static implicit operator AnArray(AnimationListDetails details) => GetAnimationListInternal(details.m_Instance, details.m_Agent);

            public AnimationListEnumerator GetLslForeachEnumerator() => new AnimationListEnumerator(GetAnimationListInternal(m_Instance, m_Agent));
        }

        [APIExtension(APIExtension.Properties, "animationlistaccessor")]
        [APIDisplayName("animationlistaccessor")]
        public sealed class AnimationListAccessor
        {
            private readonly ScriptInstance m_Instance;

            public AnimationListAccessor(ScriptInstance instance)
            {
                m_Instance = instance;
            }

            public AnimationListDetails this[LSLKey agent] => new AnimationListDetails(m_Instance, agent);
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "AnimationList")]
        public AnimationListAccessor GetAnimationListAccessor(ScriptInstance instance) => new AnimationListAccessor(instance);
    }
}
