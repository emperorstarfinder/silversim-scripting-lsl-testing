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
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using System.ComponentModel;
using System.Net.Mail;

namespace SilverSim.Scripting.Lsl.Api.Email
{
    [Description("Email service")]
    [PluginName("LSL_Email_SmtpService")]
    [ServerParam("LSL.SmtpThrottleDelayMs", DefaultValue = 20, Description = "Time between emails from user", ParameterType = typeof(uint))]
    public sealed class SmtpEmailService : LocalEmailService
    {
        private string m_SmtpHost;
        private readonly int m_SmtpPort;

        public SmtpEmailService(IConfig config) : base(config)
        {
            m_SmtpHost = config.GetString("SmtpHost");
            m_SmtpPort = config.GetInt("SmtpPort", 25);
        }

        protected override void SendRemote(UUID fromSceneID, UUID fromObjectID, string toaddress, string subject, string body)
        {
            SceneInterface scene;
            ObjectPart part;
            if(!m_Scenes.TryGetValue(fromSceneID, out scene) ||
                !scene.Primitives.TryGetValue(fromObjectID, out part))
            {
                return;
            }
            m_BlockedUntilTimestamp.RemoveIf(part.Owner.ID, (until) => TimeSource.TicksElapsed(TimeSource.TickCount, until) > 0);
            if(m_BlockedUntilTimestamp.ContainsKey(part.Owner.ID))
            {
                return;
            }
            Vector3 pos = part.GlobalPosition;
            GridVector regionLoc = scene.GridPosition;
            var mail = new MailMessage(fromObjectID + "@" + m_LocalDomain, toaddress)
            {
                Body = $"Object-Name: {part.Name}\nRegion: {scene.Name} ({regionLoc.X}, {regionLoc.Y})\nLocal-Position: ({pos.X}, {pos.Y}, {pos.Z})\n\n" + body,
                Subject = subject
            };

            long untilTimestamp;
            uint timedelay;
            if (!m_Throttles.TryGetValue(fromSceneID, out timedelay) &&
                !m_Throttles.TryGetValue(UUID.Zero, out timedelay))
            {
                timedelay = 20000;
            }

            if (timedelay > 0)
            {
                untilTimestamp = TimeSource.TickCount + TimeSource.MsecsToTicks(timedelay);
                m_BlockedUntilTimestamp[part.Owner.ID] = untilTimestamp;
            }

            var client = new SmtpClient(m_SmtpHost, m_SmtpPort);
            client.Send(mail);
        }

        private readonly RwLockedDictionary<UUID, uint> m_Throttles = new RwLockedDictionary<UUID, uint>();
        private readonly TimeProvider TimeSource = TimeProvider.StopWatch;
        private readonly RwLockedDictionary<UUID, long> m_BlockedUntilTimestamp = new RwLockedDictionary<UUID, long>();

        [ServerParam("LSL.SmtpThrottleDelay")]
        public void MaxListenersPerScriptUpdated(UUID regionID, string value)
        {
            uint uintval;
            if (value.Length == 0)
            {
                m_Throttles.Remove(regionID);
            }
            else if (uint.TryParse(value, out uintval))
            {
                m_Throttles[regionID] = uintval;
            }
        }
    }
}
