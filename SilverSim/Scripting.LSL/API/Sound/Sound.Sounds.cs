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

namespace SilverSim.Scripting.LSL.API.Sound
{
    public partial class Sound_API
    {
        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llCollisionSound")]
        public void CollisionSound(ScriptInstance instance, string impact_sound, double impact_volume)
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llLoopSound")]
        public void LoopSound(ScriptInstance instance, string sound, double volume)
        {
#warning Implement llLoopSound(string, double)
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llLoopSoundMaster")]
        public void LoopSoundMaster(ScriptInstance instance, string sound, double volume)
        {
#warning Implement llLoopSoundMaster(string, double)
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llLoopSoundSlave")]
        public void LoopSoundSlave(ScriptInstance instance, string sound, double volume)
        {
#warning Implement llLoopSoundSlave(string, double)
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(1)]
        [ScriptFunctionName("llPreloadSound")]
        public void PreloadSound(ScriptInstance instance, string sound)
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llStopSound")]
        public void StopSound(ScriptInstance instance)
        {
#warning Implement llStopSound()
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llPlaySound")]
        public void PlaySound(ScriptInstance instance, string sound, double volume)
        {
#warning Implement llPlaySound(string, double)
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llPlaySoundSlave")]
        public void PlaySoundSlave(ScriptInstance instance, string sound, double volume)
        {
#warning Implement llPlaySoundSlave(string, double)
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llTriggerSound")]
        public void TriggerSound(ScriptInstance instance, string sound, double volume)
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llTriggerSoundLimited")]
        public void TriggerSoundLimited(ScriptInstance instance, string sound, double volume, Vector3 top_north_east, Vector3 bottom_south_west)
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

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(0.1)]
        [ScriptFunctionName("llAdjustSoundVolume")]
        public void AdjustSoundVolume(ScriptInstance instance, double volume)
        {
#warning Implement llAdjustSoundVolume(double)
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetSoundQueueing")]
        public void SetSoundQueueing(ScriptInstance instance, int queue)
        {
            lock (instance)
            {
                instance.Part.IsSoundQueueing = queue != 0;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetSoundRadius")]
        public void SetSoundRadius(ScriptInstance instance, double radius)
        {
#warning Implement llSetSoundRadius(double)
            throw new NotImplementedException();
        }
    }
}
