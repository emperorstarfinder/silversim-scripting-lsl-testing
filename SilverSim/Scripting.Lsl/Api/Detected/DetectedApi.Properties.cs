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
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SilverSim.Scripting.Lsl.Api.Detected
{
    public sealed partial class DetectedApi
    {
        [APIExtension(APIExtension.Properties, "detectedlist")]
        [APIDisplayName("detectedlist")]
        [APIAccessibleMembers("Count")]
        public sealed class DetectedAccessor
        {
            private readonly Script Instance;

            public DetectedAccessor(Script instance)
            {
                Instance = instance;
            }

            public int Count
            {
                get
                {
                    lock (Instance)
                    {
                        return Instance.m_Detected?.Count ?? 0;
                    }
                }
            }

            public DetectedData this[int index]
            {
                get
                {
                    lock (Instance)
                    {
                        if(index < 0 || index >= (Instance.m_Detected?.Count ?? 0))
                        {
                            return new DetectedData();
                        }
                        return new DetectedData(Instance.m_Detected[index], Instance);
                    }
                }
            }

            public DetectedEnumerator GetLslForeachEnumerator() => new DetectedEnumerator(Instance);
        }

        [APIExtension(APIExtension.Properties, "detecteddata")]
        [APIDisplayName("detecteddata")]
        [APIAccessibleMembers]
        [ImplementsCustomTypecasts]
        public sealed class DetectedData
        {
            private readonly DetectInfo? m_DetectInfo;

            public int Type => (int)(m_DetectInfo?.ObjType ?? DetectedTypeFlags.None);
            public Vector3 Velocity => m_DetectInfo?.Velocity ?? Vector3.Zero;
            public LSLKey Key => m_DetectInfo?.Key ?? UUID.Zero;
            public Vector3 GrabOffset => m_DetectInfo?.GrabOffset ?? Vector3.Zero;
            public int IsSameGroup { get; }
            public int LinkNumber => m_DetectInfo?.LinkNumber ?? -1;
            public string Name => m_DetectInfo?.Name ?? string.Empty;
            public LSLKey Owner => m_DetectInfo?.Owner.ID ?? UUID.Zero;
            public Vector3 Position => m_DetectInfo?.Position ?? Vector3.Zero;
            public Quaternion Rotation => m_DetectInfo?.Rotation ?? Quaternion.Identity;
            public Vector3 TouchBinormal => m_DetectInfo?.TouchBinormal ?? Vector3.Zero;
            public int TouchFace => m_DetectInfo?.TouchFace ?? 0;
            public Vector3 TouchNormal => m_DetectInfo?.TouchNormal ?? Vector3.Zero;
            public Vector3 TouchPosition => m_DetectInfo?.TouchPosition ?? Vector3.Zero;
            public Vector3 TouchST => m_DetectInfo?.TouchST ?? Vector3.Zero;
            public Vector3 TouchUV => m_DetectInfo?.TouchUV ?? Vector3.Zero;
            public LSLKey Group => m_DetectInfo?.Group.ID ?? UUID.Zero;

            public static implicit operator bool(DetectedData v) => v.m_DetectInfo.HasValue;

            public DetectedData(DetectInfo di, Script instance)
            {
                m_DetectInfo = di;
                IsSameGroup = di.Group.Equals(instance.Part.Group).ToLSLBoolean();
            }

            public DetectedData()
            {
            }
        }

        public sealed class DetectedEnumerator : IEnumerator<DetectedData>
        {
            private readonly Script Instance;
            private int Position = -1;

            public DetectedEnumerator(Script script)
            {
                Instance = script;
            }

            public DetectedData Current
            {
                get
                {
                    List<DetectInfo> list = Instance.m_Detected;
                    if (list == null || Position < 0 || Position >= list.Count)
                    {
                        return new DetectedData();
                    }
                    return new DetectedData(list[Position], Instance);
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext() => ++Position < (Instance.m_Detected?.Count ?? 0);

            public void Reset() => Position = -1;
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "Detected")]
        public DetectedAccessor GetDetected(ScriptInstance instance) => new DetectedAccessor((Script)instance);
    }
}
