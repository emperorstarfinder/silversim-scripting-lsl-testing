// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.Lsl.Api.Chat
{
    [LSLImplementation]
    [ScriptApiName("Chat")]
    public partial class ChatApi : IScriptApi, IPlugin
    {
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int PUBLIC_CHANNEL = 0;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        const int DEBUG_CHANNEL = 0x7FFFFFFF;

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
                ev.ID = instance.Part.ObjectGroup.ID;
                ev.Name = instance.Part.ObjectGroup.Name;
                instance.Part.ObjectGroup.Scene.GetService<ChatServiceInterface>().Send(ev);
            }
        }

        [APILevel(APIFlags.OSSL, APILevel.KeepCsName)]
        const int OS_LISTEN_REGEX_NAME = 0x1;
        [APILevel(APIFlags.OSSL, APILevel.KeepCsName)]
        const int OS_LISTEN_REGEX_MESSAGE = 0x2;

        public ChatApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }
    }
}
