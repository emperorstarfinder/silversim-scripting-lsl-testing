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

#pragma warning disable IDE0018, RCS1029, IDE0019

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.XmlRpc
{
    [ScriptApiName("XMLRPC")]
    [LSLImplementation]
    [PluginName("LSL_XMLRPC")]
    [Description("LSL XMLRPC API")]
    public partial class XmlRpcApi : IScriptApi, IPlugin
    {
        private SceneList m_Scenes;
        private LSLXmlRpcServer m_Server;
        private LSLXmlRpcClient_RequestQueue m_Client;
        private readonly string m_ServerName;
        private readonly string m_ClientName;

        public XmlRpcApi(IConfig config)
        {
            m_ServerName = config.GetString("Server", "LSL_XMLRPCServer");
            m_ClientName = config.GetString("Client", "LSL_XMLRPCClient");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            loader.GetService(m_ServerName, out m_Server);
            loader.GetService(m_ClientName, out m_Client);
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

        [APILevel(APIFlags.LSL, "llSendRemoteData")]
        [ForcedSleep(3.0)]
        public LSLKey SendRemoteData(ScriptInstance instance, LSLKey channel, string dest, int idata, string sdata)
        {
            lock (instance)
            {
                ObjectPart part = instance.Part;
                var req = new LSLXmlRpcClient_RequestQueue.LSLXmlRpcRequest
                {
                    Channel = channel.ToString(),
                    IData = idata,
                    SData = sdata,
                    ItemID = instance.Item.ID,
                    PrimID = part.ID,
                    SceneID = part.ObjectGroup.Scene.ID,
                    DestURI = dest
                };
                m_Client.Enqueue(req);
                return req.RequestID;
            }
        }
    }
}
