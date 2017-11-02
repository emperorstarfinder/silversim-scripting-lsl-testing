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

#pragma warning disable RCS1029, IDE0018

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
    public partial class XmlRpcApi
    {
        private class SendRemoteDataInfo
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
                var rdi = new SendRemoteDataInfo(key, part.ObjectGroup.Scene.ID, part.ID, instance.Item.ID, channel.ToString(), dest, idata, sdata);
                ThreadManager.CreateThread(SendRequest).Start(rdi);
                return key;
            }
        }

        private void SendRequest(object o)
        {
            var rdi = (SendRemoteDataInfo)o;

            var req = new XmlRpcStructs.XmlRpcRequest("llRemoteData");
            var m = new Map
            {
                { "Channel", rdi.Channel },
                { "StringValue", rdi.SData },
                { "IntValue", rdi.IData }
            };
            req.Params.Add(m);
            byte[] reqdata = req.Serialize();
            XmlRpcStructs.XmlRpcResponse res;

            string sdata = string.Empty;
            int idata = 0;

            using (Stream respstream = new HttpClient.Post(rdi.DestURI, "text/xml", reqdata.Length,
                (Stream s) => s.Write(reqdata, 0, reqdata.Length))
            {
                TimeoutMs = 30000
            }.ExecuteStreamRequest())
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

            if (res != null)
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
                instance?.PostEvent(new RemoteDataEvent
                    {
                        Channel = UUID.Zero,
                        IData = idata,
                        MessageID = rdi.Key,
                        SData = sdata,
                        Sender = string.Empty,
                        Type = REMOTE_DATA_REPLY
                    });
            }
        }
    }
}
