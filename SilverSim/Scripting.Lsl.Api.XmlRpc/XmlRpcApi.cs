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
