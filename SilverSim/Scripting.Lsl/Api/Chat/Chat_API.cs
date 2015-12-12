// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;

namespace SilverSim.Scripting.Lsl.Api.Chat
{
    [LSLImplementation]
    [ScriptApiName("Chat")]
    public partial class ChatApi : IScriptApi, IPlugin
    {
        [APILevel(APIFlags.LSL)]
        public const int PUBLIC_CHANNEL = 0;
        [APILevel(APIFlags.LSL)]
        public const int DEBUG_CHANNEL = 0x7FFFFFFF;

        UUID GetOwner(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Owner.ID;
            }
        }

        void SendChat(ScriptInstance instance, ListenEvent ev)
        {
            lock (instance)
            {
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                ev.ID = thisGroup.ID;
                ev.GlobalPosition = instance.Part.GlobalPosition;
                ev.Name = thisGroup.Name;
                thisGroup.Scene.GetService<ChatServiceInterface>().Send(ev);
            }
        }

        [APILevel(APIFlags.OSSL)]
        public const int OS_LISTEN_REGEX_NAME = 0x1;
        [APILevel(APIFlags.OSSL)]
        public const int OS_LISTEN_REGEX_MESSAGE = 0x2;

        public ChatApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
    }
}
