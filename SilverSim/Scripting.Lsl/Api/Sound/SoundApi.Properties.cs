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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Object.Parameters;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Primitive;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SilverSim.Scripting.Lsl.Api.Sound
{
    public sealed partial class SoundApi
    {
        [APIExtension(APIExtension.Properties, "sound")]
        [APIDisplayName("sound")]
        [APIIsVariableType]
        [ImplementsCustomTypecasts]
        [Serializable]
        public sealed class SoundItem
        {
            [XmlIgnore]
            [NonSerialized]
            private WeakReference<ScriptInstance> WeakInstance;
            private readonly UUID m_SoundID;

            public void RestoreFromSerialization(ScriptInstance instance)
            {
                WeakInstance = new WeakReference<ScriptInstance>(instance);
            }

            public SoundItem(ScriptInstance instance, UUID id)
            {
                WeakInstance = new WeakReference<ScriptInstance>(instance);
                m_SoundID = id;
            }

            public static implicit operator bool(SoundItem item) => item.WeakInstance != null;
            public static explicit operator LSLKey(SoundItem item) => item.m_SoundID;

            public void Loop(double volume, PrimitiveSoundFlags paraflags)
            {
                ScriptInstance instance = null;
                if (WeakInstance.TryGetTarget(out instance))
                {
                    lock (instance)
                    {
                        ObjectPart part = instance.Part;
                        PrimitiveSoundFlags flags = PrimitiveSoundFlags.Looped | paraflags;

                        if (part.IsSoundQueueing)
                        {
                            flags |= PrimitiveSoundFlags.Queue;
                        }

                        SoundParam soundparams = part.Sound;
                        soundparams.SoundID = m_SoundID;
                        soundparams.Gain = volume.Clamp(0, 1);
                        soundparams.Flags = flags;
                        if (TryFetchSound(instance, m_SoundID))
                        {
                            part.Sound = soundparams;
                        }
                    }
                }
            }

            public void Send(double volume, PrimitiveSoundFlags paraflags)
            {
                ScriptInstance instance;
                if (WeakInstance.TryGetTarget(out instance))
                {
                    lock (instance)
                    {
                        PrimitiveSoundFlags flags = paraflags;
                        ObjectPart thisPart = instance.Part;
                        if (thisPart.IsSoundQueueing)
                        {
                            flags |= PrimitiveSoundFlags.Queue;
                        }
                        SoundParam soundparams = thisPart.Sound;
                        if (TryFetchSound(instance, m_SoundID))
                        {
                            thisPart.ObjectGroup.Scene.SendAttachedSound(thisPart, m_SoundID, volume, soundparams.Radius, flags);
                        }
                    }
                }
            }

            public void Trigger(double volume)
            {
                ScriptInstance instance;
                if (WeakInstance.TryGetTarget(out instance))
                {
                    lock (instance)
                    {
                        ObjectPart thisPart = instance.Part;
                        if (TryFetchSound(instance, m_SoundID))
                        {
                            thisPart.ObjectGroup.Scene.SendTriggerSound(thisPart, m_SoundID, volume, 20);
                        }
                    }
                }
            }

            public void TriggerLimited(double volume, Vector3 top_north_east, Vector3 bottom_south_west)
            {
                ScriptInstance instance;
                if (WeakInstance.TryGetTarget(out instance))
                {
                    lock (instance)
                    {
                        ObjectPart thisPart = instance.Part;
                        if (TryFetchSound(instance, m_SoundID))
                        {
                            thisPart.ObjectGroup.Scene.SendTriggerSound(thisPart, m_SoundID, volume, thisPart.Sound.Radius, top_north_east, bottom_south_west);
                        }
                    }
                }
            }

            public void Preload()
            {
                ScriptInstance instance;
                if (WeakInstance.TryGetTarget(out instance))
                {
                    lock (instance)
                    {
                        ObjectPart thisPart = instance.Part;
                        if (TryFetchSound(instance, m_SoundID))
                        {
                            thisPart.ObjectGroup.Scene.SendPreloadSound(thisPart, m_SoundID);
                        }
                    }
                }
            }
        }

        public sealed class SoundEnumerator : IEnumerator<string>
        {
            private readonly string[] m_Sounds;
            private int m_Position = -1;

            public SoundEnumerator(string[] sounds)
            {
                m_Sounds = sounds;
            }

            public string Current => m_Sounds[m_Position];

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                /* intentionally left empty */
            }

            public bool MoveNext() => ++m_Position < m_Sounds.Length;

            public void Reset() => m_Position = -1;
        }

        [APIExtension(APIExtension.Properties, "soundaccessor")]
        [APIDisplayName("soundaccessor")]
        [APIAccessibleMembers]
        public sealed class SoundAccessor
        {
            private readonly ScriptInstance m_Instance;

            public SoundAccessor(ScriptInstance instance)
            {
                m_Instance = instance;
            }

            public SoundEnumerator GetLslForeachEnumerator()
            {
                var sounds = new List<string>();
                lock(m_Instance)
                {
                    foreach(KeyValuePair<string, ObjectPartInventoryItem> kvp in m_Instance.Part.Inventory.Key2ValuePairs)
                    {
                        if (kvp.Value.AssetType == AssetType.Sound)
                        {
                            sounds.Add(kvp.Key);
                        }
                    }
                }
                return new SoundEnumerator(sounds.ToArray());
            }

            public SoundItem this[string sound]
            {
                get
                {
                    lock (m_Instance)
                    {
                        ObjectPart thisPart = m_Instance.Part;
                        UUID soundID;
                        try
                        {
                            soundID = m_Instance.GetSoundAssetID(sound);
                        }
                        catch
                        {
                            return new SoundItem(null, UUID.Zero);
                        }
                        return new SoundItem(m_Instance, soundID);
                    }
                }
            }

            public int AreQueueing
            {
                get
                {
                    lock (m_Instance)
                    {
                        return m_Instance.Part.IsSoundQueueing.ToLSLBoolean();
                    }
                }
                set
                {
                    lock (m_Instance)
                    {
                        m_Instance.Part.IsSoundQueueing = value != 0;
                    }
                }
            }

            public double Radius
            {
                get
                {
                    lock (m_Instance)
                    {
                        SoundParam sound = m_Instance.Part.Sound;
                        return sound.Radius;
                    }
                }
                
                set
                {
                    lock (m_Instance)
                    {
                        SoundParam sound = m_Instance.Part.Sound;
                        sound.Radius = value;
                        m_Instance.Part.Sound = sound;
                    }
                }
            }

            public void Stop()
            {
                lock (m_Instance)
                {
                    ObjectPart part = m_Instance.Part;
                    SoundParam param = part.Sound;
                    param.SoundID = UUID.Zero;
                    param.Flags = PrimitiveSoundFlags.Stop;
                    param.Gain = 0;
                    part.Sound = param;
                }
            }
        }

        [APIExtension(APIExtension.Properties, "Sounds")]
        public SoundAccessor GetSounds(ScriptInstance instance) => new SoundAccessor(instance);

        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "Loop")]
        public void SoundItemLoop(SoundItem item, double volume) => item.Loop(volume, 0);

        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "LoopMaster")]
        public void SoundItemLoopMaster(SoundItem item, double volume) => item.Loop(volume, PrimitiveSoundFlags.SyncMaster);

        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "LoopSlave")]
        public void SoundItemLoopSlave(SoundItem item, double volume) => item.Loop(volume, PrimitiveSoundFlags.SyncSlave);

        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "Play")]
        public void SoundItemPlay(SoundItem item, double volume) => item.Send(volume, 0);

        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "PlaySlave")]
        public void SoundItemPlaySlave(SoundItem item, double volume) => item.Send(volume, PrimitiveSoundFlags.SyncSlave);

        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "Trigger")]
        public void SoundItemTrigger(SoundItem item, double volume) => item.Trigger(volume);

        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "TriggerLimited")]
        public void SoundItemTriggerLimited(SoundItem item, double volume, Vector3 top_north_east, Vector3 bottom_south_west) => item.TriggerLimited(volume, top_north_east, bottom_south_west);

        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "Stop")]
        public void SoundItemStop(SoundAccessor accessor) => accessor.Stop();

        [APIExtension(APIExtension.Properties, APIUseAsEnum.MemberFunction, "AdjustVolume")]
        public void AdjustVolume(ScriptInstance instance, SoundAccessor accessor, double volume) => AdjustSoundVolume(instance, volume);
    }
}
