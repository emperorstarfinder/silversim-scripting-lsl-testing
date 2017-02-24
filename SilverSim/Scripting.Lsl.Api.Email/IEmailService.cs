// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Scripting.Lsl.Api.Email
{
    public interface IEmailService
    {
        void Send(UUID fromSceneID, UUID fromObjectID, string toaddress, string subject, string body);
        /** return path is handled by EmailService */
        void RequestNext(UUID sceneID, UUID objectID, string sender, string subject);
    }
}
