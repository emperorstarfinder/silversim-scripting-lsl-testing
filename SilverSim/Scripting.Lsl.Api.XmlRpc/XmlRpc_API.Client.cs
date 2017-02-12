// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Http.Client;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Threading;
using SilverSim.Types;
using System.IO;
using XmlRpcStructs = SilverSim.Types.StructuredData.XmlRpc.XmlRpc;

namespace SilverSim.Scripting.Lsl.Api.XmlRpc
{
    partial class XmlRpcApi
    {
        class SendRemoteDataInfo
        {
            public UUID Key;
            public UUID SceneID;
            public UUID ObjectID;
            public UUID ItemID;

            public string Channel;
            public int IData;
            public string SData;
            public string DestURI;

            public SendRemoteDataInfo(UUID key, UUID sceneID, UUID objectID, UUID itemID, string channel, string dest, int idata, string sdata)
            {
                Key = key;
                Channel = channel;
                DestURI = dest;
                SceneID = sceneID;
                ObjectID = objectID;
                ItemID = itemID;
                IData = idata;
                SData = sdata;
            }
        }

        [APILevel(APIFlags.LSL, "llSendRemoteData")]
        [ForcedSleep(3.0)]
        public LSLKey SendRemoteData(ScriptInstance instance, LSLKey channel, string dest, int idata, string sdata)
        {
            lock (instance)
            {
                UUID key = UUID.Random;
                ObjectPart part = instance.Part;
                SendRemoteDataInfo rdi = new SendRemoteDataInfo(key, part.ObjectGroup.Scene.ID, part.ID, instance.Item.ID, channel.ToString(), dest, idata, sdata);
                ThreadManager.CreateThread(SendRequest).Start(rdi);
                return key;
            }
        }

        void SendRequest(object o)
        {
            SendRemoteDataInfo rdi = (SendRemoteDataInfo)o;

            XmlRpcStructs.XmlRpcRequest req = new XmlRpcStructs.XmlRpcRequest("llRemoteData");
            Map m = new Map();
            m.Add("Channel", rdi.Channel);
            m.Add("StringValue", rdi.SData);
            m.Add("IntValue", rdi.IData);
            req.Params.Add(m);
            byte[] reqdata = req.Serialize();
            XmlRpcStructs.XmlRpcResponse res;

            string sdata = string.Empty;
            int idata = 0;

            using (Stream respstream = HttpClient.DoStreamRequest("POST", rdi.DestURI, null, "text/xml", reqdata.Length,
                delegate (Stream s)
                {
                    s.Write(reqdata, 0, reqdata.Length);
                }, false, 30000))
            {
                try
                {
                    res = XmlRpcStructs.DeserializeResponse(respstream);
                }
                catch (XmlRpcStructs.XmlRpcFaultException e)
                {
                    res = null;
                    sdata = e.Message;
                    idata = e.FaultCode;
                }
                catch
                {
                    res = null;
                    sdata = string.Empty;
                    idata = -32700;
                }
            }

            if (null != res)
            {
                m = res.ReturnValue as Map;
                if (m == null)
                {
                    sdata = string.Empty;
                    idata = -32700;
                }

                IValue iv;
                if (m.TryGetValue("StringValue", out iv))
                {
                    sdata = iv.ToString();
                }
                if (m.TryGetValue("IntValue", out iv))
                {
                    idata = iv.AsInt;
                }
            }

            SceneInterface scene;
            ObjectPart part;
            ObjectPartInventoryItem item;

            if (m_Scenes.TryGetValue(rdi.SceneID, out scene) &&
                scene.Primitives.TryGetValue(rdi.ObjectID, out part) &&
                part.Inventory.TryGetValue(rdi.ItemID, out item))
            {
                ScriptInstance instance = item.ScriptInstance;
                if (null != instance)
                {
                    RemoteDataEvent ev = new RemoteDataEvent();
                    ev.Channel = UUID.Zero;
                    ev.IData = idata;
                    ev.MessageID = rdi.Key;
                    ev.SData = sdata;
                    ev.Sender = string.Empty;
                    ev.Type = REMOTE_DATA_REPLY;
                    instance.PostEvent(ev);
                }
            }
        }
    }
}
