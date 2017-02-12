// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.XmlRpc
{
    [ScriptApiName("XMLRPC")]
    [LSLImplementation]
    [Description("LSL XMLRPC API")]
    public partial class XmlRpcApi : IScriptApi, IPlugin
    {
        SceneList m_Scenes;

        public XmlRpcApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            loader.XmlRpcServer.XmlRpcMethods.Add("llRemoteData", RemoteDataXmlRpc);
        }

        [APILevel(APIFlags.LSL)]
        public const int REMOTE_DATA_CHANNEL = 1;
        [APILevel(APIFlags.LSL)]
        public const int REMOTE_DATA_REQUEST = 2;
        [APILevel(APIFlags.LSL)]
        public const int REMOTE_DATA_REPLY = 3;

        [APILevel(APIFlags.LSL, "remote_data")]
        [StateEventDelegate]
        public delegate void State_remote_data(int event_type, LSLKey channel, LSLKey message_id, string sender, int idata, string sdata);

        void Remove(UUID scriptid)
        {
            ChannelInfo channel;
            if(m_ScriptChannels.TryGetValue(scriptid, out channel))
            {
                m_Channels.RemoveIf(channel.ChannelID, delegate (ChannelInfo ci) { return ci == channel; });
            }
        }
    }
}
