// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;

namespace SilverSim.Scripting.Lsl.Api.Email
{
    [PluginName("LSL_Email")]
    public sealed class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new EmailApi(ownSection.GetString("EmailService", string.Empty));
        }
    }

    [PluginName("LSL_Email_LocalOnlyService")]
    public sealed class ServiceFactory : IPluginFactory
    {
        public ServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new LocalEmailService();
        }
    }
}
