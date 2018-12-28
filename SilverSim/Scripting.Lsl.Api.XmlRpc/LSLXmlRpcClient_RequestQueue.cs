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


using SilverSim.Http.Client;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using XmlRpcStructs = SilverSim.Types.StructuredData.XmlRpc.XmlRpc;

namespace SilverSim.Scripting.Lsl.Api.XmlRpc
{
    [Description("LSL XMLRPC Client Support")]
    [PluginName("LSLXmlRpcClient")]
    public sealed class LSLXmlRpcClient_RequestQueue : IPlugin, IPluginShutdown
    {
        public class LSLXmlRpcRequest
        {
            public readonly UUID RequestID = UUID.Random;
            public UUID SceneID;
            public UUID PrimID;
            public UUID ItemID;

            public string Channel = string.Empty;
            public int IData;
            public string SData = string.Empty;
            public string DestURI = string.Empty;
        }

        private readonly RwLockedDictionary<UUID, BlockingQueue<LSLXmlRpcRequest>> m_RequestQueues = new RwLockedDictionary<UUID, BlockingQueue<LSLXmlRpcRequest>>();
        private SceneList m_Scenes;

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_Scenes.OnRegionAdd += RegionAdded;
            m_Scenes.OnRegionRemove += RegionRemoved;
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;

        public void Shutdown()
        {
            m_RequestQueues.Clear();
        }

        private void RegionRemoved(SceneInterface scene)
        {
            m_RequestQueues.Remove(scene.ID);
        }

        private void RegionAdded(SceneInterface scene)
        {
            int i;
            try
            {
                m_RequestQueues.Add(scene.ID, new BlockingQueue<LSLXmlRpcRequest>());
            }
            catch
            {
                /* exception intentionally ignored should ever a queue already be added */
            }
            for (i = 0; i < 10; ++i)
            {
                Thread t = ThreadManager.CreateThread(ProcessThread);
                t.Name = "LSL:XmlRpcClient Processor for region " + scene.ID.ToString();
                t.Start(scene.ID);
            }
        }

        internal bool Enqueue(LSLXmlRpcRequest req)
        {
            BlockingQueue<LSLXmlRpcRequest> queue;
            if (m_RequestQueues.TryGetValue(req.SceneID, out queue))
            {
                queue.Enqueue(req);
                return true;
            }
            return false;
        }

        private void ProcessThread(object o)
        {
            var id = (UUID)o;
            LSLXmlRpcRequest reqctx;
            for (; ; )
            {
                BlockingQueue<LSLXmlRpcRequest> reqqueue;

                if (!m_RequestQueues.TryGetValue(id, out reqqueue))
                {
                    /* terminate condition is deletion of the request queue */
                    break;
                }

                try
                {
                    reqctx = reqqueue.Dequeue(1000);
                }
                catch (TimeoutException)
                {
                    continue;
                }

                var req = new XmlRpcStructs.XmlRpcRequest("llRemoteData");
                var m = new Map
                {
                    { "Channel", reqctx.Channel },
                    { "StringValue", reqctx.SData },
                    { "IntValue", reqctx.IData }
                };
                req.Params.Add(m);
                byte[] reqdata = req.Serialize();
                XmlRpcStructs.XmlRpcResponse res;

                string sdata = string.Empty;
                int idata = 0;

                using (Stream respstream = new HttpClient.Post(reqctx.DestURI, "text/xml", reqdata.Length,
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

                if (m_Scenes.TryGetValue(reqctx.SceneID, out scene) &&
                    scene.Primitives.TryGetValue(reqctx.PrimID, out part) &&
                    part.Inventory.TryGetValue(reqctx.ItemID, out item))
                {
                    ScriptInstance instance = item.ScriptInstance;
                    instance?.PostEvent(new RemoteDataEvent
                    {
                        Channel = UUID.Zero,
                        IData = idata,
                        MessageID = reqctx.RequestID,
                        SData = sdata,
                        Sender = string.Empty,
                        Type = XmlRpcApi.REMOTE_DATA_REPLY
                    });
                }
            }
        }
    }
}
