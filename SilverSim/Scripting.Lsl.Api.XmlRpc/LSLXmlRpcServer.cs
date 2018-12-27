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
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Timers;
using XmlRpcStructs = SilverSim.Types.StructuredData.XmlRpc.XmlRpc;

namespace SilverSim.Scripting.Lsl.Api.XmlRpc
{
    [Description("LSL XMLRPC Server Support")]
    [PluginName("LSLXmlRpcServer")]
    public sealed class LSLXmlRpcServer : IPlugin, IPluginShutdown
    {
        private HttpXmlRpcHandler m_XmlRpcHandler;
        private Timer m_RpcTimer;
        private SceneList m_Scenes;

        public class XmlRpcInfo
        {
            public DateTime ValidUntil;
            public HttpRequest HttpRequest;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_RpcTimer = new Timer(1000);
            m_RpcTimer.Elapsed += TimerEvent;
            m_RpcTimer.Start();

            m_Scenes = loader.Scenes;
            m_XmlRpcHandler = loader.XmlRpcServer;
            m_XmlRpcHandler.XmlRpcMethods_DiscThread.Add("llRemoteData", RemoteDataXmlRpc);
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;

        private void TimerEvent(object sender, ElapsedEventArgs e)
        {
            var RemoveList = new List<UUID>();
            foreach (KeyValuePair<UUID, XmlRpcInfo> kvp in m_ActiveRequests)
            {
                if (kvp.Value.ValidUntil < DateTime.UtcNow)
                {
                    RemoveList.Add(kvp.Key);
                }
            }

            XmlRpcInfo reqdata;
            foreach (UUID id in RemoveList)
            {
                if (m_ActiveRequests.Remove(id, out reqdata))
                {
                    FaultResponse(reqdata.HttpRequest, 3, "Timeout");
                }
            }
        }

        private void FaultResponse(HttpRequest req, int faultCode, string faultString)
        {
            using (HttpResponse response = req.BeginResponse("text/xml"))
            {
                var res = new XmlRpcStructs.XmlRpcFaultResponse
                {
                    FaultCode = faultCode,
                    FaultString = faultString
                };
                byte[] buffer = res.Serialize();
                response.GetOutputStream(buffer.LongLength).Write(buffer, 0, buffer.Length);
            }
        }

        public void Shutdown()
        {
            m_RpcTimer.Stop();
            m_RpcTimer.Elapsed -= TimerEvent;
            m_XmlRpcHandler.XmlRpcMethods_DiscThread.Remove("llRemoteData");

            XmlRpcInfo reqdata;
            foreach (UUID id in m_ActiveRequests.Keys.ToArray())
            {
                if (m_ActiveRequests.Remove(id, out reqdata))
                {
                    FaultResponse(reqdata.HttpRequest, 4, "Script Shutdown");
                }
            }
        }

        public class ChannelInfo
        {
            public UUID ChannelID;
            public UUID SceneID;
            public UUID ObjectID;
            public UUID ItemID;

            public ChannelInfo(UUID channelID, UUID sceneID, UUID objectID, UUID itemID)
            {
                ChannelID = channelID;
                SceneID = sceneID;
                ObjectID = objectID;
                ItemID = itemID;
            }
        }

        public void RegisterChannel(UUID sceneID, UUID objectID, UUID scriptID)
        {
            ChannelInfo ci;
            /* TODO: how to deal with persistency of channel ids here? As of now simply re-use objectid */
            if (!m_ScriptChannels.TryGetValue(scriptID, out ci))
            {
                ci = new ChannelInfo(scriptID, sceneID, objectID, scriptID);
                m_ScriptChannels.Add(scriptID, ci);
                m_Channels.Add(ci.ChannelID, ci);
            }
            RegisterDataChannel(ci);
        }

        private void RegisterDataChannel(ChannelInfo ci)
        {
            SceneInterface scene;
            ObjectPart part;
            ObjectPartInventoryItem item;

            if (m_Scenes.TryGetValue(ci.SceneID, out scene) &&
                scene.Primitives.TryGetValue(ci.ObjectID, out part) &&
                part.Inventory.TryGetValue(ci.ItemID, out item))
            {
                ScriptInstance instance = item.ScriptInstance;
                instance?.PostEvent(new RemoteDataEvent
                {
                    Channel = ci.ChannelID,
                    IData = 0,
                    MessageID = UUID.Zero,
                    SData = string.Empty,
                    Sender = string.Empty,
                    Type = XmlRpcApi.REMOTE_DATA_REQUEST
                });
            }
        }

        public void RemoveChannel(UUID scriptID, UUID channelid)
        {
            ChannelInfo ci;
            if (m_ScriptChannels.TryGetValue(scriptID, out ci) && ci.ChannelID == channelid)
            {
                Remove(scriptID);
            }
        }

        public void Remove(UUID scriptID)
        {
            ChannelInfo channel;
            if (m_ScriptChannels.TryGetValue(scriptID, out channel))
            {
                m_Channels.RemoveIf(channel.ChannelID, (ChannelInfo ci) => ci == channel);
            }
        }

        private readonly RwLockedDictionary<UUID /* channelid */, ChannelInfo> m_Channels = new RwLockedDictionary<UUID, ChannelInfo>();
        private readonly RwLockedDictionary<UUID /* itemid */, ChannelInfo> m_ScriptChannels = new RwLockedDictionary<UUID, ChannelInfo>();

        private readonly RwLockedDictionary<UUID, XmlRpcInfo> m_ActiveRequests = new RwLockedDictionary<UUID, XmlRpcInfo>();

        public void ReplyXmlRpc(UUID reqid, int intval, string strval)
        {
            XmlRpcInfo info;
            if(m_ActiveRequests.Remove(reqid, out info))
            {
                using (HttpResponse res = info.HttpRequest.BeginResponse("text/xml"))
                {
                    using (Stream s = res.GetOutputStream())
                    {
                        new XmlRpcStructs.XmlRpcResponse
                        {
                            ReturnValue = new Map
                            {
                                { "StringValue", strval },
                                { "IntValue", intval }
                            }
                        }.Serialize(s);
                    }
                }
            }
        }

        private XmlRpcStructs.XmlRpcResponse RemoteDataXmlRpc(HttpRequest httpreq, XmlRpcStructs.XmlRpcRequest req)
        {
            if (req.Params.Count != 1)
            {
                throw new XmlRpcStructs.XmlRpcFaultException(-32604, "Invalid parameters");
            }

            Map m;
            if (!req.Params.TryGetValue(0, out m))
            {
                throw new XmlRpcStructs.XmlRpcFaultException(-32604, "Invalid parameters");
            }

            UUID channelid;
            Integer intval;
            AString strval;
            if (!m.TryGetValue("Channel", out channelid) ||
                !m.TryGetValue("IntValue", out intval) ||
                !m.TryGetValue("StringValue", out strval))
            {
                throw new XmlRpcStructs.XmlRpcFaultException(-32604, "Invalid parameters");
            }

            ChannelInfo channel;
            if (!m_Channels.TryGetValue(channelid, out channel))
            {
                throw new XmlRpcStructs.XmlRpcFaultException(1, "Unknown channel");
            }

            SceneInterface scene;
            if (!m_Scenes.TryGetValue(channel.SceneID, out scene))
            {
                m_Channels.Remove(channelid);
                m_ScriptChannels.RemoveIf(channel.ItemID, (ChannelInfo ci) => ci == channel);
                throw new XmlRpcStructs.XmlRpcFaultException(1, "Unknown channel");
            }

            ObjectPart part;
            ObjectPartInventoryItem item;
            ScriptInstance instance;
            if (!scene.Primitives.TryGetValue(channel.ObjectID, out part))
            {
                m_Channels.Remove(channelid);
                m_ScriptChannels.RemoveIf(channel.ItemID, (ChannelInfo ci) => ci == channel);
                throw new XmlRpcStructs.XmlRpcFaultException(1, "Unknown channel");
            }

            if (!part.Inventory.TryGetValue(channel.ItemID, out item))
            {
                m_Channels.Remove(channelid);
                m_ScriptChannels.RemoveIf(channel.ItemID, (ChannelInfo ci) => ci == channel);
                throw new XmlRpcStructs.XmlRpcFaultException(1, "Unknown channel");
            }

            instance = item.ScriptInstance;
            if (instance == null)
            {
                m_Channels.Remove(channelid);
                m_ScriptChannels.RemoveIf(channel.ItemID, (ChannelInfo ci) => ci == channel);
                throw new XmlRpcStructs.XmlRpcFaultException(1, "Unknown channel");
            }

            UUID messageId = UUID.Random;
            try
            {
                m_ActiveRequests.Add(messageId, new XmlRpcInfo
                {
                    ValidUntil = Date.Now.AddSeconds(30),
                    HttpRequest = httpreq
                });
            }
            catch
            {
                throw new XmlRpcStructs.XmlRpcFaultException(2, "Unexpected error");
            }
            httpreq.SetConnectionClose();

            instance.PostEvent(new RemoteDataEvent
            {
                Channel = channelid,
                IData = intval.AsInt,
                SData = strval.ToString(),
                Sender = string.Empty,
                Type = XmlRpcApi.REMOTE_DATA_CHANNEL,
                MessageID = messageId
            });
            return null;
        }
    }
}
