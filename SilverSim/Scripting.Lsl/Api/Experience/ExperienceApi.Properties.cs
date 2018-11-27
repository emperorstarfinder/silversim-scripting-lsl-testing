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

using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.ServiceInterfaces.Experience;
using SilverSim.Types;
using SilverSim.Types.Experience;
using System.Collections;
using System.Collections.Generic;

namespace SilverSim.Scripting.Lsl.Api.Experience
{
    public sealed partial class ExperienceApi
    {
        public class KeyValueStoreEnumerator : IEnumerator<string>
        {
            private readonly string[] m_Keys;
            private int m_Position = -1;

            public KeyValueStoreEnumerator(ScriptInstance instance)
            {
                lock (instance)
                {
                    ExperienceServiceInterface experienceService = instance.Part.ObjectGroup.Scene.ExperienceService;
                    UEI experienceId = instance.Item.ExperienceID;
                    UUID queryid = CheckExperienceStatus(instance, experienceService, experienceId);
                    if (queryid != UUID.Zero)
                    {
                        m_Keys = new string[0];
                        return;
                    }

                    try
                    {
                        m_Keys = experienceService.KeyValueStore.GetKeys(experienceId).ToArray();
                    }
                    catch
                    {
                        m_Keys = new string[0];
                    }
                }
            }

            public string Current => m_Keys[m_Position];

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                /* intentionally left empty */
            }

            public bool MoveNext() => ++m_Position < m_Keys.Length;

            public void Reset() => m_Position = -1;
        }

        [APIExtension(APIExtension.Properties, "experiencekeyvaluestorekeys")]
        [APIDisplayName("experiencekeyvaluestorekeys")]
        public class KeyValueStoreKeysAccessor
        {
            private readonly ScriptInstance m_ScriptInstance;

            public KeyValueStoreKeysAccessor(ScriptInstance instance)
            {
                m_ScriptInstance = instance;
            }

            public KeyValueStoreEnumerator GetLslForeachEnumerator() => new KeyValueStoreEnumerator(m_ScriptInstance);
        }

        [APIExtension(APIExtension.Properties, "experiencekeyvaluestore")]
        [APIDisplayName("experiencekeyvaluestore")]
        [APIIsVariableType]
        [APIAccessibleMembers]
        public class KeyValueStoreAccessor
        {
            private readonly ScriptInstance m_ScriptInstance;

            public KeyValueStoreAccessor(ScriptInstance instance)
            {
                m_ScriptInstance = instance;
            }

            public KeyValueStoreKeysAccessor Keys => new KeyValueStoreKeysAccessor(m_ScriptInstance);

            public string this[string valuename]
            {
                get
                {
                    lock (m_ScriptInstance)
                    {
                        SceneInterface scene = m_ScriptInstance.Part.ObjectGroup.Scene;
                        UEI experienceID = m_ScriptInstance.Item.ExperienceID;
                        ExperienceServiceInterface experinceService = scene.ExperienceService;
                        if (experienceID == UEI.Unknown || experinceService == null)
                        {
                            return string.Empty;
                        }

                        string value;
                        if (!scene.ExperienceService.KeyValueStore.TryGetValue(experienceID, valuename, out value))
                        {
                            value = string.Empty;
                        }
                        return value;
                    }
                }
                set
                {
                    lock (m_ScriptInstance)
                    {
                        SceneInterface scene = m_ScriptInstance.Part.ObjectGroup.Scene;
                        UEI experienceID = m_ScriptInstance.Item.ExperienceID;
                        ExperienceServiceInterface experinceService = scene.ExperienceService;
                        if (experienceID == UEI.Unknown || experinceService == null)
                        {
                            return;
                        }

                        scene.ExperienceService.KeyValueStore.Store(experienceID, valuename, value);
                    }
                }
            }
        }

        [APIExtension(APIExtension.Properties, "experience")]
        [APIDisplayName("experience")]
        [APIAccessibleMembers("KeyValueStore", "ID", "Enabled", "Name", "OwnerID", "GroupID")]
        public class ExperienceAccessor
        {
            private readonly ScriptInstance m_ScriptInstance;

            public ExperienceAccessor(ScriptInstance instance)
            {
                m_ScriptInstance = instance;
            }

            public KeyValueStoreAccessor KeyValueStore => new KeyValueStoreAccessor(m_ScriptInstance);

            public LSLKey ID => m_ScriptInstance.Item.ExperienceID.ID;

            public int Enabled => (m_ScriptInstance.Item.ExperienceID.ID != UUID.Zero).ToLSLBoolean();

            public string Name
            {
                get
                {
                    lock (m_ScriptInstance)
                    {
                        ExperienceServiceInterface experienceService = m_ScriptInstance.Part.ObjectGroup.Scene.ExperienceService;
                        UEI experienceID = m_ScriptInstance.Item.ExperienceID;
                        if (experienceService == null || experienceID == UEI.Unknown)
                        {
                            return string.Empty;
                        }
                        ExperienceInfo info;
                        if (experienceService.TryGetValue(experienceID, out info))
                        {
                            return info.ID.ExperienceName;
                        }
                        return string.Empty;
                    }
                }
            }

            public LSLKey OwnerID
            {
                get
                {
                    lock (m_ScriptInstance)
                    {
                        ExperienceServiceInterface experienceService = m_ScriptInstance.Part.ObjectGroup.Scene.ExperienceService;
                        UEI experienceID = m_ScriptInstance.Item.ExperienceID;
                        if (experienceService == null || experienceID == UEI.Unknown)
                        {
                            return string.Empty;
                        }
                        ExperienceInfo info;
                        if (experienceService.TryGetValue(experienceID, out info))
                        {
                            return info.Owner.ID;
                        }
                        return string.Empty;
                    }
                }
            }

            public LSLKey GroupID
            {
                get
                {
                    lock (m_ScriptInstance)
                    {
                        ExperienceServiceInterface experienceService = m_ScriptInstance.Part.ObjectGroup.Scene.ExperienceService;
                        UEI experienceID = m_ScriptInstance.Item.ExperienceID;
                        if (experienceService == null || experienceID == UEI.Unknown)
                        {
                            return string.Empty;
                        }
                        ExperienceInfo info;
                        if (experienceService.TryGetValue(experienceID, out info))
                        {
                            return info.Group.ID;
                        }
                        return string.Empty;
                    }
                }
            }
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "Experience")]
        public ExperienceAccessor GetExperience(ScriptInstance instance) => new ExperienceAccessor(instance);
    }
}
