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
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using KFM = SilverSim.Scene.Types.KeyframedMotion.KeyframedMotion;
using KFMEntry = SilverSim.Scene.Types.KeyframedMotion.Keyframe;

namespace SilverSim.Scripting.Lsl.Api.KeyframedMotion
{
    public sealed partial class KeyframedMotionApi
    {
        [APIExtension(APIExtension.Properties, "keyframedmotionentry")]
        [APIDisplayName("keyframedmotionentry")]
        [APIIsVariableType]
        [APIAccessibleMembers]
        [Serializable]
        public struct KeyframedMotionEntry
        {
            public Vector3 TargetPosition;
            public Quaternion TargetRotation;
            public double Duration;
        }

        [APIExtension(APIExtension.Properties, "keyframedmotion")]
        [APIDisplayName("keyframedmotion")]
        [APIIsVariableType]
        [APICloneOnAssignment]
        [APIAccessibleMembers("Mode", "DataFlags", "Length")]
        [Serializable]
        public class KeyframedMotionList
        {
            public int Mode = KFM_FORWARD;
            public int DataFlags = KFM_TRANSLATION | KFM_ROTATION;
            internal readonly List<KeyframedMotionEntry> m_Entries = new List<KeyframedMotionEntry>();

            public KeyframedMotionList()
            {
                /* intentionally left empty */
            }

            public KeyframedMotionList(KeyframedMotionList src)
            {
                Mode = src.Mode;
                DataFlags = src.DataFlags;
                m_Entries = new List<KeyframedMotionEntry>(m_Entries);
            }

            public void Add(KeyframedMotionEntry e) => m_Entries.Add(e);

            public KeyframedMotionEntry this[int index]
            {
                get
                {
                    if (index < 0 || index >= m_Entries.Count)
                    {
                        return new KeyframedMotionEntry();
                    }
                    return m_Entries[index];
                }
                set
                {
                    if (index < 0 || index > m_Entries.Count)
                    {
                        return;
                    }
                    if (index == m_Entries.Count)
                    {
                        m_Entries.Add(value);
                    }
                    else
                    {
                        m_Entries[index] = value;
                    }
                }
            }

            public int Length => m_Entries.Count;
        }

        [APIExtension(APIExtension.Properties, "keyframedmotionaccess")]
        [APIDisplayName("keyframedmotionaccess")]
        [APIAccessibleMembers]
        public class KeyframedMotionAccessor
        {
            private ScriptInstance m_Instance;

            public KeyframedMotionAccessor(ScriptInstance instance)
            {
                m_Instance = instance;
            }

            private T With<T>(Func<KFM, T> del)
            {
                lock (m_Instance)
                {
                    return del(m_Instance.Part.ObjectGroup.KeyframedMotion);
                }
            }

            public KeyframedMotionList Program
            {
                get
                {
                    return With((kfm) =>
                    {
                        var list = new KeyframedMotionList();
                        foreach (KFMEntry k in kfm)
                        {
                            list.Add(new KeyframedMotionEntry
                            {
                                TargetPosition = k.TargetPosition,
                                TargetRotation = k.TargetRotation
                            });
                        }
                        switch (kfm.PlayMode)
                        {
                            case KFM.Mode.Forward:
                                list.Mode = KFM_FORWARD;
                                break;

                            case KFM.Mode.Loop:
                                list.Mode = KFM_LOOP;
                                break;

                            case KFM.Mode.PingPong:
                                list.Mode = KFM_PING_PONG;
                                break;

                            case KFM.Mode.Reverse:
                                list.Mode = KFM_REVERSE;
                                break;
                        }

                        list.DataFlags = 0;
                        if ((kfm.Flags & KFM.DataFlags.Rotation) != 0)
                        {
                            list.DataFlags |= KFM_ROTATION;
                        }
                        if ((kfm.Flags & KFM.DataFlags.Translation) != 0)
                        {
                            list.DataFlags |= KFM_TRANSLATION;
                        }
                        return list;
                    });
                }
                set
                {
                    lock (m_Instance)
                    {
                        ObjectGroup grp = m_Instance.Part.ObjectGroup;
                        if (grp.IsAttached)
                        {
                            return;
                        }

                        if (value.Length == 0)
                        {
                            grp.KeyframedMotion = null;
                        }
                        else
                        {
                            var newMotion = new KFM();
                            KFMEntry entry;

                            switch (value.Mode)
                            {
                                case KFM_LOOP:
                                    newMotion.PlayMode = KFM.Mode.Loop;
                                    break;

                                case KFM_PING_PONG:
                                    newMotion.PlayMode = KFM.Mode.PingPong;
                                    break;

                                case KFM_REVERSE:
                                    newMotion.PlayMode = KFM.Mode.Reverse;
                                    break;

                                default:
                                    newMotion.PlayMode = KFM.Mode.Forward;
                                    break;
                            }

                            newMotion.Flags = 0;
                            int div = 1;
                            if ((value.DataFlags & KFM_TRANSLATION) != 0)
                            {
                                ++div;
                                newMotion.Flags |= KFM.DataFlags.Translation;
                            }
                            if ((value.DataFlags & KFM_ROTATION) != 0)
                            {
                                ++div;
                                newMotion.Flags |= KFM.DataFlags.Rotation;
                            }

                            if (div < 2)
                            {
                                return;
                            }

                            foreach (KeyframedMotionEntry src_entry in value.m_Entries)
                            {
                                entry = new KFMEntry();
                                if ((newMotion.Flags & KFM.DataFlags.Translation) != 0)
                                {
                                    entry.TargetPosition = src_entry.TargetPosition;
                                }
                                if ((newMotion.Flags & KFM.DataFlags.Rotation) != 0)
                                {
                                    entry.TargetRotation = src_entry.TargetRotation;
                                }
                                entry.Duration = src_entry.Duration;
                            }
                            grp.KeyframedMotion = newMotion;
                        }
                    }
                }
            }

            public void Play()
            {
                lock (m_Instance)
                {
                    m_Instance.Part.ObjectGroup.PlayKeyframedMotion();
                }
            }

            public void Pause()
            {
                lock (m_Instance)
                {
                    m_Instance.Part.ObjectGroup.PauseKeyframedMotion();
                }
            }

            public void Stop()
            {
                lock (m_Instance)
                {
                    m_Instance.Part.ObjectGroup.StopKeyframedMotion();
                }
            }

            public int IsPlaying => With((kfm) => kfm.IsRunning.ToLSLBoolean());
            public int IsPlayingReserve => With((kfm) => kfm.IsRunningReverse.ToLSLBoolean());
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "KeyframedMotion")]
        public KeyframedMotionAccessor GetAccessor(ScriptInstance instance) => new KeyframedMotionAccessor(instance);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Play")]
        public void Play(KeyframedMotionAccessor accessor) => accessor.Play();

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Pause")]
        public void Pause(KeyframedMotionAccessor accessor) => accessor.Pause();

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Stop")]
        public void Stop(KeyframedMotionAccessor accessor) => accessor.Stop();
    }
}
