// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Scripting.Common;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.LSL.Api.Sound
{
    public partial class SoundApi
    {
        [APILevel(APIFlags.LSL, "llCollisionSound")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void CollisionSound(ScriptInstance instance, string impact_sound, double impact_volume)
        {
            ObjectPart.CollisionSoundParam para = new ObjectPart.CollisionSoundParam();

            lock (instance)
            {
                if (impact_volume < 0f) impact_volume = 0f;
                if (impact_volume > 1f) impact_volume = 1f;

                para.ImpactVolume = impact_volume;
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

        [APILevel(APIFlags.LSL, "llLoopSound")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void LoopSound(ScriptInstance instance, string sound, double volume)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llLoopSoundMaster")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void LoopSoundMaster(ScriptInstance instance, string sound, double volume)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llLoopSoundSlave")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void LoopSoundSlave(ScriptInstance instance, string sound, double volume)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llPreloadSound")]
        [ForcedSleep(1)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void PreloadSound(ScriptInstance instance, string sound)
        {
            lock(instance)
            {
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
                instance.Part.ObjectGroup.Scene.SendPreloadSound(instance.Part, soundID);
            }
        }

        [APILevel(APIFlags.LSL, "llStopSound")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void StopSound(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llPlaySound")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void PlaySound(ScriptInstance instance, string sound, double volume)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llPlaySoundSlave")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void PlaySoundSlave(ScriptInstance instance, string sound, double volume)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llTriggerSound")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void TriggerSound(ScriptInstance instance, string sound, double volume)
        {
            lock (instance)
            {
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
                instance.Part.ObjectGroup.Scene.SendTriggerSound(instance.Part, soundID, volume, 20);
            }
        }

        [APILevel(APIFlags.LSL, "llTriggerSoundLimited")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void TriggerSoundLimited(ScriptInstance instance, string sound, double volume, Vector3 top_north_east, Vector3 bottom_south_west)
        {
            lock (instance)
            {
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
                instance.Part.ObjectGroup.Scene.SendTriggerSound(instance.Part, soundID, volume, 20, top_north_east, bottom_south_west);
            }
        }

        [APILevel(APIFlags.LSL, "llAdjustSoundVolume")]
        [ForcedSleep(0.1)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void AdjustSoundVolume(ScriptInstance instance, double volume)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llSetSoundQueueing")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetSoundQueueing(ScriptInstance instance, int queue)
        {
            lock (instance)
            {
                instance.Part.IsSoundQueueing = queue != 0;
            }
        }

        [APILevel(APIFlags.LSL, "llSetSoundRadius")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetSoundRadius(ScriptInstance instance, double radius)
        {
            throw new NotImplementedException();
        }
    }
}
