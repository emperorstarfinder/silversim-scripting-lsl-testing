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
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Sound
{
    [ScriptApiName("Sound")]
    [LSLImplementation]
    [Description("LSL Sound API")]
    public class SoundApi : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL, "llCollisionSound")]
        public void CollisionSound(ScriptInstance instance, string impact_sound, double impact_volume)
        {
            var para = new ObjectPart.CollisionSoundParam();

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
                        instance.Part.CollisionSound = para;
                    }
                    catch
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "InventoryItem0DoesNotReferenceASound", "Inventory item {0} does not reference a sound", impact_sound));
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llLoopSound")]
        public void LoopSound(ScriptInstance instance, string sound, double volume)
        {
            lock (instance)
            {
                LoopSound(instance, sound, volume, 0);
            }
        }

        [APILevel(APIFlags.LSL, "llLoopSoundMaster")]
        public void LoopSoundMaster(ScriptInstance instance, string sound, double volume)
        {
            lock (instance)
            {
                LoopSound(instance, sound, volume, PrimitiveSoundFlags.SyncMaster);
            }
        }

        [APILevel(APIFlags.LSL, "llLoopSoundSlave")]
        public void LoopSoundSlave(ScriptInstance instance, string sound, double volume)
        {
            lock (instance)
            {
                LoopSound(instance, sound, volume, PrimitiveSoundFlags.SyncSlave);
            }
        }

        [APILevel(APIFlags.LSL, "llSound")]
        public void Sound(ScriptInstance instance, string sound, double volume, int queue, int loop)
        {
            lock (instance)
            {
                instance.Part.IsSoundQueueing = queue != 0;
                if (loop != 0)
                {
                    LoopSound(instance, sound, volume, 0);
                }
                else
                {
                    SendSound(instance, sound, volume, 0);
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
                thisPart.ObjectGroup.Scene.SendPreloadSound(thisPart, soundID);
            }
        }

        [APILevel(APIFlags.LSL, "llStopSound")]
        public void StopSound(ScriptInstance instance)
        {
            lock (instance)
            {
                ObjectPart part = instance.Part;
                ObjectPart.SoundParam param = part.Sound;
                param.SoundID = UUID.Zero;
                param.Flags = PrimitiveSoundFlags.Stop;
                param.Gain = 0;
                part.Sound = param;
            }
        }

        [APILevel(APIFlags.LSL, "llPlaySound")]
        public void PlaySound(ScriptInstance instance, string sound, double volume)
        {
            lock (instance)
            {
                SendSound(instance, sound, volume, 0);
            }
        }

        [APILevel(APIFlags.LSL, "llPlaySoundSlave")]
        public void PlaySoundSlave(ScriptInstance instance, string sound, double volume)
        {
            lock (instance)
            {
                SendSound(instance, sound, volume, PrimitiveSoundFlags.SyncSlave);
            }
        }

        [APILevel(APIFlags.LSL, "llTriggerSound")]
        public void TriggerSound(ScriptInstance instance, string sound, double volume)
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
                thisPart.ObjectGroup.Scene.SendTriggerSound(thisPart, soundID, volume, 20);
            }
        }

        [APILevel(APIFlags.LSL, "llTriggerSoundLimited")]
        public void TriggerSoundLimited(ScriptInstance instance, string sound, double volume, Vector3 top_north_east, Vector3 bottom_south_west)
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
                thisPart.ObjectGroup.Scene.SendTriggerSound(thisPart, soundID, volume, thisPart.Sound.Radius, top_north_east, bottom_south_west);
            }
        }

        [APILevel(APIFlags.LSL, "llAdjustSoundVolume")]
        [ForcedSleep(0.1)]
        public void AdjustSoundVolume(ScriptInstance instance, double volume)
        {
            lock (instance)
            {
                ObjectPart thisPart = instance.Part;
                thisPart.ObjectGroup.Scene.SendAttachedSoundGainChange(thisPart, volume, thisPart.Sound.Radius);
            }
        }

        [APILevel(APIFlags.LSL, "llSetSoundQueueing")]
        public void SetSoundQueueing(ScriptInstance instance, int queue)
        {
            lock (instance)
            {
                instance.Part.IsSoundQueueing = queue != 0;
            }
        }

        [APILevel(APIFlags.LSL, "llSetSoundRadius")]
        public void SetSoundRadius(ScriptInstance instance, double radius)
        {
            lock (instance)
            {
                ObjectPart.SoundParam sound = instance.Part.Sound;
                sound.Radius = radius;
                instance.Part.Sound = sound;
            }
        }

        private void SendSound(ScriptInstance instance, string sound, double volume, PrimitiveSoundFlags paraflags)
        {
            PrimitiveSoundFlags flags = paraflags;
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
            if (thisPart.IsSoundQueueing)
            {
                flags |= PrimitiveSoundFlags.Queue;
            }
            ObjectPart.SoundParam soundparams = thisPart.Sound;
            thisPart.ObjectGroup.Scene.SendAttachedSound(thisPart, soundID, volume, soundparams.Radius, flags);
        }

        private void LoopSound(ScriptInstance instance, string sound, double volume, PrimitiveSoundFlags paraflags)
        {
            ObjectPart part = instance.Part;
            PrimitiveSoundFlags flags = PrimitiveSoundFlags.Looped | paraflags;

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

            if (part.IsSoundQueueing)
            {
                flags |= PrimitiveSoundFlags.Queue;
            }

            ObjectPart.SoundParam soundparams = part.Sound;
            soundparams.SoundID = soundID;
            soundparams.Gain = volume.Clamp(0, 1);
            soundparams.Flags = flags;
            part.Sound = soundparams;
        }
    }
}
