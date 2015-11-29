// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using System;

namespace SilverSim.Scripting.Lsl.Api.XmlRpc
{
    [ScriptApiName("XMLRPC")]
    [LSLImplementation]
    public class XmlRpcApi : IScriptApi, IPlugin
    {

        public XmlRpcApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int REMOTE_DATA_CHANNEL = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int REMOTE_DATA_REQUEST = 2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int REMOTE_DATA_REPLY = 3;

        [APILevel(APIFlags.LSL, "llCloseRemoteDataChannel")]
        public void CloseRemoteDataChannel(ScriptInstance instance, LSLKey key)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llOpenRemoteDataChannel")]
        [ForcedSleep(1.0)]
        public void OpenRemoteDataChannel(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llRemoteDataReply")]
        [ForcedSleep(3.0)]
        public void RemoteDataReply(ScriptInstance instance, LSLKey channel, LSLKey message_id, string sdata, int idata)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llSendRemoteData")]
        [ForcedSleep(3.0)]
        public LSLKey SendRemoteData(ScriptInstance instance, LSLKey channel, string dest, int idata, string sdata)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llRemoteDataSetRegion")]
        public void RemoteDataSetRegion(ScriptInstance instance)
        {
            OpenRemoteDataChannel(instance);
        }

        [APILevel(APIFlags.LSL, "remote_data")]
        [StateEventDelegate]
        public delegate void State_remote_data(int event_type, LSLKey channel, LSLKey message_id, string sender, int idata, string sdata);
    }
}
