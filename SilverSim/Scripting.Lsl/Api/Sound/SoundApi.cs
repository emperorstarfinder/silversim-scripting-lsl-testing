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
using SilverSim.Scene.Types.Object.Parameters;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl.Api.Primitive;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Primitive;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Sound
{
    [ScriptApiName("Sound")]
    [LSLImplementation]
    [Description("LSL Sound API")]
    public sealed partial class SoundApi : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        private static bool TryFetchSound(ScriptInstance instance, UUID soundID)
        {
            ObjectGroup grp = instance.Part.ObjectGroup;
            SceneInterface scene = grp.Scene;
            AssetServiceInterface assetService = scene.AssetService;
            AssetMetadata metadata;
            AssetData data;
            if (!assetService.Metadata.TryGetValue(soundID, out metadata))
            {
                if (grp.IsAttached) /* on attachments, we have to fetch from agent eventually */
                {
                    IAgent owner;
                    if (!grp.Scene.RootAgents.TryGetValue(grp.Owner.ID, out owner))
                    {
                        return false;
                    }
                    if (!owner.AssetService.TryGetValue(soundID, out data))
                    {
                        /* not found */
                        return false;
                    }
                    assetService.Store(data);
                    if (data.Type != AssetType.Sound)
                    {
                        /* ignore wrong asset here */
                        return false;
                    }
                }
                else
                {
                    /* ignore missing asset here */
                    return false;
                }
            }
            else if (metadata.Type != AssetType.Sound)
            {
                /* ignore wrong asset here */
                return false;
            }

            return true;
        }

        [APILevel(APIFlags.LSL, "llCollisionSound")]
        public void CollisionSound(ScriptInstance instance, string impact_sound, double impact_volume)
        {
            var para = new CollisionSoundParam();

            lock (instance)
            {
                para.ImpactVolume = impact_volume.Clamp(0f, 1f);
                if (string.IsNullOrEmpty(impact_sound))
                {
                    para.ImpactSound = UUID.Zero;
                    instance.Part.CollisionSound = para;
                }
                else
                {
                    try
                    {
                        para.ImpactSound = instance.GetSoundAssetID(impact_sound);
                        if (TryFetchSound(instance, para.ImpactSound))
                        {
                            instance.Part.CollisionSound = para;
                        }
                    }
                    catch
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0DoesNotReferenceASound", "Inventory item {0} does not reference a sound", impact_sound));
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llLoopSound")]
        public void LoopSound(ScriptInstance instance, string sound, double volume) => LoopSound(instance, PrimitiveApi.LINK_THIS, sound, volume);

        [APILevel(APIFlags.ASSL, "asLinkLoopSound")]
        public void LoopSound(ScriptInstance instance, int link, string sound, double volume)
        {
            lock (instance)
            {
                LoopSound(instance, link, sound, volume, 0);
            }
        }

        [APILevel(APIFlags.LSL, "llLoopSoundMaster")]
        public void LoopSoundMaster(ScriptInstance instance, string sound, double volume) => LoopSoundMaster(instance, PrimitiveApi.LINK_THIS, sound, volume);

        [APILevel(APIFlags.ASSL, "asLinkLoopSoundMaster")]
        public void LoopSoundMaster(ScriptInstance instance, int link, string sound, double volume)
        {
            lock (instance)
            {
                LoopSound(instance, link, sound, volume, PrimitiveSoundFlags.SyncMaster);
            }
        }

        [APILevel(APIFlags.LSL, "llLoopSoundSlave")]
        public void LoopSoundSlave(ScriptInstance instance, string sound, double volume) => LoopSoundSlave(instance, PrimitiveApi.LINK_THIS, sound, volume);

        [APILevel(APIFlags.ASSL, "asLoopSoundSlave")]
        public void LoopSoundSlave(ScriptInstance instance, int link, string sound, double volume)
        {
            lock (instance)
            {
                LoopSound(instance, link, sound, volume, PrimitiveSoundFlags.SyncSlave);
            }
        }

        [APILevel(APIFlags.LSL, "llSound")]
        public void Sound(ScriptInstance instance, string sound, double volume, int queue, int loop) => Sound(instance, PrimitiveApi.LINK_THIS, sound, volume, queue, loop);

        [APILevel(APIFlags.ASSL, "asLinkSound")]
        public void Sound(ScriptInstance instance, int link, string sound, double volume, int queue, int loop)
        {
            lock (instance)
            {
                foreach (ObjectPart p in instance.GetLinkTargets(link))
                {
                    p.IsSoundQueueing = queue != 0;
                }
                if (loop != 0)
                {
                    LoopSound(instance, link, sound, volume, 0);
                }
                else
                {
                    SendSound(instance, link, sound, volume, 0);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSoundPreload")]
        /* Even though LSL wiki considers this as deprecated, it will be support since it has no defined forced delay */
        public void SoundPreload(ScriptInstance instance, string sound)
        {
            PreloadSound(instance, sound);
        }

        [APILevel(APIFlags.LSL, "llPreloadSound")]
        [ForcedSleep(1)]
        public void PreloadSound(ScriptInstance instance, string sound)
        {
            lock (instance)
            {
                ObjectPart thisPart = instance.Part;
                UUID soundID;
                try
                {
                    soundID = instance.GetSoundAssetID(sound);
                }
                catch
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0DoesNotReferenceASound", "Inventory item {0} does not reference a sound", sound));
                    return;
                }
                if (TryFetchSound(instance, soundID))
                {
                    thisPart.ObjectGroup.Scene.SendPreloadSound(thisPart, soundID);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llStopSound")]
        public void StopSound(ScriptInstance instance) => StopSound(instance, PrimitiveApi.LINK_THIS);

        [APILevel(APIFlags.ASSL, "asLinkStopSound")]
        public void StopSound(ScriptInstance instance, int link)
        {
            lock (instance)
            {
                foreach (ObjectPart part in instance.GetLinkTargets(link))
                {
                    SoundParam param = part.Sound;
                    param.SoundID = UUID.Zero;
                    param.Flags = PrimitiveSoundFlags.Stop;
                    param.Gain = 0;
                    part.Sound = param;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llPlaySound")]
        public void PlaySound(ScriptInstance instance, string sound, double volume) => PlaySound(instance, PrimitiveApi.LINK_THIS, sound, volume);

        [APILevel(APIFlags.ASSL, "asLinkPlaySound")]
        public void PlaySound(ScriptInstance instance, int link, string sound, double volume)
        {
            lock (instance)
            {
                SendSound(instance, link, sound, volume, 0);
            }
        }

        [APILevel(APIFlags.LSL, "llPlaySoundSlave")]
        public void PlaySoundSlave(ScriptInstance instance, string sound, double volume) => PlaySoundSlave(instance, PrimitiveApi.LINK_THIS, sound, volume);

        [APILevel(APIFlags.ASSL, "asLinkPlaySoundSlave")]
        public void PlaySoundSlave(ScriptInstance instance, int link, string sound, double volume)
        {
            lock (instance)
            {
                SendSound(instance, link, sound, volume, PrimitiveSoundFlags.SyncSlave);
            }
        }

        [APILevel(APIFlags.LSL, "llTriggerSound")]
        public void TriggerSound(ScriptInstance instance, string sound, double volume) => TriggerSound(instance, PrimitiveApi.LINK_THIS, sound, volume);

        [APILevel(APIFlags.ASSL, "asLinkTriggerSound")]
        public void TriggerSound(ScriptInstance instance, int link, string sound, double volume)
        {
            lock (instance)
            {
                UUID soundID;
                try
                {
                    soundID = instance.GetSoundAssetID(sound);
                }
                catch
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0DoesNotReferenceASound", "Inventory item {0} does not reference a sound", sound));
                    return;
                }
                if (TryFetchSound(instance, soundID))
                {
                    SceneInterface scene = instance.Part.ObjectGroup.Scene;
                    foreach (ObjectPart thisPart in instance.GetLinkTargets(link))
                    {
                        scene.SendTriggerSound(thisPart, soundID, volume, 20);
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llTriggerSoundLimited")]
        public void TriggerSoundLimited(ScriptInstance instance, string sound, double volume, Vector3 top_north_east, Vector3 bottom_south_west) => TriggerSoundLimited(instance, PrimitiveApi.LINK_THIS, sound, volume, top_north_east, bottom_south_west);

        [APILevel(APIFlags.ASSL, "asLinkTriggerSoundLimited")]
        public void TriggerSoundLimited(ScriptInstance instance, int link, string sound, double volume, Vector3 top_north_east, Vector3 bottom_south_west)
        {
            lock (instance)
            {
                UUID soundID;
                try
                {
                    soundID = instance.GetSoundAssetID(sound);
                }
                catch
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0DoesNotReferenceASound", "Inventory item {0} does not reference a sound", sound));
                    return;
                }
                if (TryFetchSound(instance, soundID))
                {
                    SceneInterface scene = instance.Part.ObjectGroup.Scene;
                    foreach (ObjectPart thisPart in instance.GetLinkTargets(link))
                    {
                        scene.SendTriggerSound(thisPart, soundID, volume, thisPart.Sound.Radius, top_north_east, bottom_south_west);
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llAdjustSoundVolume")]
        [ForcedSleep(0.1)]
        public void AdjustSoundVolume(ScriptInstance instance, double volume) => AdjustSoundVolume(instance, PrimitiveApi.LINK_THIS, volume);

        [APILevel(APIFlags.ASSL, "asAdjustSoundVolume")]
        public void AdjustSoundVolume2(ScriptInstance instance, double volume) => AdjustSoundVolume(instance, PrimitiveApi.LINK_THIS, volume);

        [APILevel(APIFlags.ASSL, "asLinkAdjustSoundVolume")]
        public void AdjustSoundVolume(ScriptInstance instance, int link, double volume)
        {
            lock (instance)
            {
                foreach (ObjectPart thisPart in instance.GetLinkTargets(link))
                {
                    thisPart.ObjectGroup.Scene.SendAttachedSoundGainChange(thisPart, volume, thisPart.Sound.Radius);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetSoundQueueing")]
        public void SetSoundQueueing(ScriptInstance instance, int queue) => SetSoundQueueing(instance, PrimitiveApi.LINK_THIS, queue);

        [APILevel(APIFlags.LSL, "asLinkSetSoundQueueing")]
        public void SetSoundQueueing(ScriptInstance instance, int link, int queue)
        {
            lock (instance)
            {
                foreach (ObjectPart part in instance.GetLinkTargets(link))
                {
                    part.IsSoundQueueing = queue != 0;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetSoundRadius")]
        public void SetSoundRadius(ScriptInstance instance, double radius) => SetSoundRadius(instance, PrimitiveApi.LINK_THIS, radius);

        [APILevel(APIFlags.LSL, "asLinkSetSoundRadius")]
        public void SetSoundRadius(ScriptInstance instance, int link, double radius)
        {
            lock (instance)
            {
                foreach (ObjectPart part in instance.GetLinkTargets(link))
                {
                    SoundParam sound = part.Sound;
                    sound.Radius = radius;
                    part.Sound = sound;
                }
            }
        }

        private void SendSound(ScriptInstance instance, int link, string sound, double volume, PrimitiveSoundFlags paraflags)
        {
            UUID soundID;
            try
            {
                soundID = instance.GetSoundAssetID(sound);
            }
            catch
            {
                instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0DoesNotReferenceASound", "Inventory item {0} does not reference a sound", sound));
                return;
            }
            if (TryFetchSound(instance, soundID))
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                foreach (ObjectPart thisPart in instance.GetLinkTargets(link))
                {
                    PrimitiveSoundFlags flags = paraflags;
                    if (thisPart.IsSoundQueueing)
                    {
                        flags |= PrimitiveSoundFlags.Queue;
                    }
                    SoundParam soundparams = thisPart.Sound;
                    scene.SendAttachedSound(thisPart, soundID, volume, soundparams.Radius, flags);
                }
            }
        }

        private void LoopSound(ScriptInstance instance, int link, string sound, double volume, PrimitiveSoundFlags paraflags)
        {
            UUID soundID;
            try
            {
                soundID = instance.GetSoundAssetID(sound);
            }
            catch
            {
                instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0DoesNotReferenceASound", "Inventory item {0} does not reference a sound", sound));
                return;
            }

            if (TryFetchSound(instance, soundID))
            {
                foreach (ObjectPart part in instance.GetLinkTargets(link))
                {
                    PrimitiveSoundFlags flags = PrimitiveSoundFlags.Looped | paraflags;
                    if (part.IsSoundQueueing)
                    {
                        flags |= PrimitiveSoundFlags.Queue;
                    }

                    SoundParam soundparams = part.Sound;
                    soundparams.SoundID = soundID;
                    soundparams.Gain = volume.Clamp(0, 1);
                    soundparams.Flags = flags;
                    part.Sound = soundparams;
                }
            }
        }
    }
}
