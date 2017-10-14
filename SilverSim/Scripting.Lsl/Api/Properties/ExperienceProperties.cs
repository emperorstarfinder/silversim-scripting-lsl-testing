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
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.ServiceInterfaces.Experience;
using SilverSim.Types;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Properties
{
    [LSLImplementation]
    [ScriptApiName("ExperienceProperties")]
    [Description("Experience Properties API")]
    public sealed class ExperienceProperties : IPlugin, IScriptApi
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APIExtension(APIExtension.Properties, "experiencekeyvaluestore")]
        [APIDisplayName("experiencekeyvaluestore")]
        [APIIsVariableType]
        public class KeyValueStoreAccessor
        {
            private readonly ScriptInstance m_ScriptInstance;

            public KeyValueStoreAccessor(ScriptInstance instance)
            {
                m_ScriptInstance = instance;
            }

            public string this[string valuename]
            {
                get
                {
                    lock (m_ScriptInstance)
                    {
                        SceneInterface scene = m_ScriptInstance.Part.ObjectGroup.Scene;
                        UUID experienceID = m_ScriptInstance.Item.ExperienceID;
                        ExperienceServiceInterface experinceService = scene.ExperienceService;
                        if (experienceID == UUID.Zero || experinceService == null)
                        {
                            return string.Empty;
                        }

                        string value;
                        if(!scene.ExperienceService.KeyValueStore.TryGetValue(experienceID, valuename, out value))
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
                        UUID experienceID = m_ScriptInstance.Item.ExperienceID;
                        ExperienceServiceInterface experinceService = scene.ExperienceService;
                        if (experienceID == UUID.Zero || experinceService == null)
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
        [APIAccessibleMembers("KeyValueStore", "ID", "Enabled")]
        public class ExperienceAccessor
        {
            private readonly ScriptInstance m_ScriptInstance;

            public ExperienceAccessor(ScriptInstance instance)
            {
                m_ScriptInstance = instance;
            }

            public KeyValueStoreAccessor KeyValueStore => new KeyValueStoreAccessor(m_ScriptInstance);

            public LSLKey ID => m_ScriptInstance.Item.ExperienceID;

            public int Enabled => (m_ScriptInstance.Item.ExperienceID != UUID.Zero).ToLSLBoolean();
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "Experience")]
        public ExperienceAccessor GetExperience(ScriptInstance instance) => new ExperienceAccessor(instance);
    }
}
