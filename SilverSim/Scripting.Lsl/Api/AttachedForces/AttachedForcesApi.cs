// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.AttachedForces
{
    [ScriptApiName("AttachedForces")]
    [LSLImplementation]
    [Description("AttachedForces API")]
    public class AttachedForcesApi : IScriptApi, IPlugin
    {
        public AttachedForcesApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            /* nothing to do */
        }

        [APIExtension(APIExtension.AdvancedPhysics)]
        [Description("defines a prim-attached 3D force\n[PHYSICS_ATTACHED_FORCE, vector force]")]
        public const int PHYSICS_ATTACHED_FORCE = 1;
        [APIExtension(APIExtension.AdvancedPhysics)]
        [Description("defines an additional link target to be affected\n[PHYSICS_ADD_LINK_TARGET, integer link]")]
        public const int PHYSICS_ADD_LINK_TARGET = 2;
        [APIExtension(APIExtension.AdvancedPhysics)]
        [Description("defines a new single link target to be affected.\n[PHYSICS_NEW_LINK_TARGET, integer link]")]
        public const int PHYSICS_NEW_LINK_TARGET = 3;

        [APIExtension(APIExtension.AdvancedPhysics, "asSetAdvancedPhysics")]
        public void SetAdvancedPhysics(ScriptInstance instance, AnArray rules)
        {
            List<UUID> linktargets = new List<UUID>();
            lock(instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                if (rules.Count == 0)
                {
                    /* shutdown advanced physics */
                    grp.AttachedForces.Clear();
                    return;
                }

                linktargets.Add(instance.Part.ID);
                ObjectPart linkTarget;
                for(int i = 0; i < rules.Count - 1; i += 2)
                {
                    int ruleType = rules[i].AsInt;
                    switch(ruleType)
                    {
                        case PHYSICS_NEW_LINK_TARGET:
                            if(!grp.TryGetValue(rules[i + 1].AsInt, out linkTarget))
                            {
                                instance.ShoutError("asSetAdvancedPhysics: Unknown link target " + rules[i + 1].AsInt.ToString() + " for PHYSICS_NEW_LINK_TARGET");
                                return;
                            }
                            linktargets.Clear();
                            linktargets.Add(linkTarget.ID);
                            break;

                        case PHYSICS_ADD_LINK_TARGET:
                            if (!grp.TryGetValue(rules[i + 1].AsInt, out linkTarget))
                            {
                                instance.ShoutError("asSetAdvancedPhysics: Unknown link target " + rules[i + 1].AsInt.ToString() + " for PHYSICS_ADD_LINK_TARGET");
                                return;
                            }
                            linktargets.Add(linkTarget.ID);
                            break;

                        case PHYSICS_ATTACHED_FORCE:
                            Vector3 force = rules[i + 1].AsVector3;
                            foreach(UUID lTarget in linktargets)
                            {
                                grp.AttachedForces[lTarget] = force;
                            }
                            break;

                        default:
                            instance.ShoutError("asSetAdvancedPhysics: Unknown rule type " + ruleType.ToString());
                            break;
                    }
                }
            }
        }
    }
}
