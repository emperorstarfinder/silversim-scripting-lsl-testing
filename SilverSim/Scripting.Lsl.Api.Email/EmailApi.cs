// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
    [Description("LSL Email API")]
    public class EmailApi : IScriptApi, IPlugin
    {
        readonly string m_ServiceName;
        IEmailService m_Service;

        static EmailApi()
        {
            if (!ConfigurationLoader.FeaturesTable.ContainsKey(typeof(IEmailService)))
            {
                ConfigurationLoader.FeaturesTable[typeof(IEmailService)] = "Email Service";
            }
        }

        public EmailApi(string emailServiceName)
        {
            m_ServiceName = emailServiceName;
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
