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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Email
{
    [ScriptApiName("Email")]
    [LSLImplementation]
    [PluginName("LSL_Email")]
    [Description("LSL Email API")]
    public class EmailApi : IScriptApi, IPlugin
    {
        private readonly string m_ServiceName;
        private IEmailService m_Service;

        static EmailApi()
        {
            if (!ConfigurationLoader.FeaturesTable.ContainsKey(typeof(IEmailService)))
            {
                ConfigurationLoader.FeaturesTable[typeof(IEmailService)] = "Email Service";
            }
        }

        public EmailApi(IConfig ownSection)
        {
            m_ServiceName = ownSection.GetString("EmailService", string.Empty);
        }

        public void Startup(ConfigurationLoader loader)
        {
            if (!string.IsNullOrEmpty(m_ServiceName))
            {
                m_Service = loader.GetService<IEmailService>(m_ServiceName);
            }
        }

        [APILevel(APIFlags.LSL, "email")]
        [StateEventDelegate]
        public delegate void State_email(string time, string address, string subject, string message, int num_left);

        [APILevel(APIFlags.LSL, "llEmail")]
        [ForcedSleep(20)]
        public void Email(ScriptInstance instance, string address, string subject, string message)
        {
            if(m_Service == null)
            {
                return;
            }
            /* Object Message Prefix Specification
             
                    Object-Name: *prim*
                    Region: *simname* (*simpos.x*, *simpos.y*)
                    Local-Position: (*primpos.x*, *primpos.y*, *primpos.z*)

                    *message*
             */
            lock(instance)
            {
                ObjectPart part = instance.Part;
                ObjectGroup grp = part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                RegionInfo ri = scene.GetRegionInfo();
                GridVector location = ri.Location;
                Vector3 primPos = part.GlobalPosition;
                string prefix = string.Format("Object-Name: {0}\nRegion: {1} ({2}, {3})\nLocal-Position: ({4}, {5}, {6})\n\n",
                    part.Name,
                    scene.Name, location.GridX, location.GridY,
                    primPos.X, primPos.Y, primPos.Z);
                m_Service.Send(scene.ID, part.ID, address, subject, prefix + message);
            }
        }

        [APILevel(APIFlags.LSL, "llGetNextEmail")]
        public void GetNextEmail(ScriptInstance instance, string address, string subject)
        {
            if (m_Service == null)
            {
                return;
            }
            lock (instance)
            {
                ObjectPart part = instance.Part;
                SceneInterface scene = part.ObjectGroup.Scene;
                m_Service.RequestNext(scene.ID, part.ID, address, subject);
            }
        }
    }
}
