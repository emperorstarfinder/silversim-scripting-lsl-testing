// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;

namespace SilverSim.Scripting.Lsl.Api.Sound
{
    public partial class SoundApi
    {
        [APILevel(APIFlags.LSL, "llCollisionSound")]
        public void CollisionSound(ScriptInstance instance, string impact_sound, double impact_volume)
        {
            ObjectPart.CollisionSoundParam para = new ObjectPart.CollisionSoundParam();

            lock (instance)
            {
                para.ImpactVolume = impact_volume.Clamp(0f, 1f);
                if (impact_sound == string.Empty)
                {
                    para.ImpactSound = UUID.Zero;
                    instance.Part.CollisionSound = para;
                }
                else
                {
                    try
                    {
                        para.ImpactSound = GetSoundAssetID(instance, impact_sound);
                        instance.Part.CollisionSound = para;
                    }
                    catch
                    {
                        instance.ShoutError(string.Format("Inventory item {0} does not reference a sound", impact_sound));
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llLoopSound")]
        public void LoopSound(ScriptInstance instance, string sound, double volume)
        {
            lock(instance)
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
            lock(this)
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
            lock(instance)
            {
                ObjectPart thisPart = instance.Part;
                UUID soundID;
                try
                {
                    soundID = GetSoundAssetID(instance, sound);
                }
                catch
                {
                    instance.ShoutError(string.Format("Inventory item {0} does not reference a sound", sound));
                    return;
                }
                thisPart.ObjectGroup.Scene.SendPreloadSound(thisPart, soundID);
            }
        }

        [APILevel(APIFlags.LSL, "llStopSound")]
        public void StopSound(ScriptInstance instance)
        {
            lock(instance)
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
            lock(instance)
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
                    soundID = GetSoundAssetID(instance, sound);
                }
                catch
                {
                    instance.ShoutError(string.Format("Inventory item {0} does not reference a sound", sound));
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
                    soundID = GetSoundAssetID(instance, sound);
                }
                catch
                {
                    instance.ShoutError(string.Format("Inventory item {0} does not reference a sound", sound));
                    return;
                }
                thisPart.ObjectGroup.Scene.SendTriggerSound(thisPart, soundID, volume, thisPart.Sound.Radius, top_north_east, bottom_south_west);
            }
        }

        [APILevel(APIFlags.LSL, "llAdjustSoundVolume")]
        [ForcedSleep(0.1)]
        public void AdjustSoundVolume(ScriptInstance instance, double volume)
        {
            lock(instance)
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
            lock(instance)
            {
                ObjectPart.SoundParam sound = instance.Part.Sound;
                sound.Radius = radius;
                instance.Part.Sound = sound;
            }
        }

        void SendSound(ScriptInstance instance, string sound, double volume, PrimitiveSoundFlags paraflags)
        {
            PrimitiveSoundFlags flags = paraflags;
            ObjectPart thisPart = instance.Part;
            UUID soundID;
            try
            {
                soundID = GetSoundAssetID(instance, sound);
            }
            catch
            {
                instance.ShoutError(string.Format("Inventory item {0} does not reference a sound", sound));
                return;
            }
            if (thisPart.IsSoundQueueing)
            {
                flags |= PrimitiveSoundFlags.Queue;
            }
            ObjectPart.SoundParam soundparams = thisPart.Sound;
            thisPart.ObjectGroup.Scene.SendAttachedSound(thisPart, soundID, volume, soundparams.Radius, flags);
        }

        void LoopSound(ScriptInstance instance, string sound, double volume, PrimitiveSoundFlags paraflags)
        {
            ObjectPart part = instance.Part;
            PrimitiveSoundFlags flags = PrimitiveSoundFlags.Looped | paraflags;

            UUID soundID;
            try
            {
                soundID = GetSoundAssetID(instance, sound);
            }
            catch
            {
                instance.ShoutError(string.Format("Inventory item {0} does not reference a sound", sound));
                return;
            }

            if(part.IsSoundQueueing)
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
