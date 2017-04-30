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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Threading;
using SilverSim.Types;
using XmlRpcStructs = SilverSim.Types.StructuredData.XmlRpc.XmlRpc;

namespace SilverSim.Scripting.Lsl.Api.XmlRpc
{
    partial class XmlRpcApi
    {
        public class RemoteDataReplyEvent
        {
            public UUID ChannelID;
            public string SData;
            public int IData;

            public RemoteDataReplyEvent(UUID channelID, string sdata, int idata)
            {
                ChannelID = channelID;
                SData = sdata;
                IData = idata;
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

            public readonly RwLockedDictionary<UUID, BlockingQueue<RemoteDataReplyEvent>> ActiveRequests = new RwLockedDictionary<UUID, BlockingQueue<XmlRpcApi.RemoteDataReplyEvent>>();
        }

        readonly RwLockedDictionary<UUID /* channelid */, ChannelInfo> m_Channels = new RwLockedDictionary<UUID, ChannelInfo>();
        readonly RwLockedDictionary<UUID /* itemid */, ChannelInfo> m_ScriptChannels = new RwLockedDictionary<UUID, ChannelInfo>();

        XmlRpcStructs.XmlRpcResponse RemoteDataXmlRpc(XmlRpcStructs.XmlRpcRequest req)
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
                m_ScriptChannels.RemoveIf(channel.ItemID, delegate (ChannelInfo ci) { return ci == channel; });
                throw new XmlRpcStructs.XmlRpcFaultException(1, "Unknown channel");
            }

            ObjectPart part;
            ObjectPartInventoryItem item;
            ScriptInstance instance;
            if (!scene.Primitives.TryGetValue(channel.ObjectID, out part))
            {
                m_Channels.Remove(channelid);
                m_ScriptChannels.RemoveIf(channel.ItemID, delegate (ChannelInfo ci) { return ci == channel; });
                throw new XmlRpcStructs.XmlRpcFaultException(1, "Unknown channel");
            }

            if (!part.Inventory.TryGetValue(channel.ItemID, out item))
            {
                m_Channels.Remove(channelid);
                m_ScriptChannels.RemoveIf(channel.ItemID, delegate (ChannelInfo ci) { return ci == channel; });
                throw new XmlRpcStructs.XmlRpcFaultException(1, "Unknown channel");
            }

            instance = item.ScriptInstance;
            if (instance == null)
            {
                m_Channels.Remove(channelid);
                m_ScriptChannels.RemoveIf(channel.ItemID, delegate (ChannelInfo ci) { return ci == channel; });
                throw new XmlRpcStructs.XmlRpcFaultException(1, "Unknown channel");
            }

            BlockingQueue<RemoteDataReplyEvent> repqueue = new BlockingQueue<RemoteDataReplyEvent>();
            UUID messageId = UUID.Random;
            try
            {
                channel.ActiveRequests.Add(messageId, repqueue);
            }
            catch
            {
                throw new XmlRpcStructs.XmlRpcFaultException(2, "Unexpected error");
            }

            RemoteDataEvent ev = new RemoteDataEvent();
            ev.Channel = channelid;
            ev.IData = intval.AsInt;
            ev.SData = strval.ToString();
            ev.Sender = string.Empty;
            ev.Type = REMOTE_DATA_CHANNEL;
            ev.MessageID = messageId;
            instance.PostEvent(ev);

            RemoteDataReplyEvent reply;
            try
            {
                reply = repqueue.Dequeue(30000);
            }
            catch
            {
                channel.ActiveRequests.Remove(messageId);
                throw new XmlRpcStructs.XmlRpcFaultException(3, "Timeout");
            }
            channel.ActiveRequests.Remove(messageId);

            Map res = new Map();
            res.Add("StringValue", reply.SData);
            res.Add("IntValue", reply.IData);
            return new XmlRpcStructs.XmlRpcResponse { ReturnValue = res };
        }

        [APILevel(APIFlags.LSL, "llCloseRemoteDataChannel")]
        [ForcedSleep(1.0)]
        public void CloseRemoteDataChannel(ScriptInstance instance, LSLKey key)
        {
            lock (instance)
            {
                ChannelInfo ci;
                UUID scriptID = instance.Item.ID;
                if (m_ScriptChannels.TryGetValue(scriptID, out ci) && ci.ChannelID == key.AsUUID)
                {
                    Remove(scriptID);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llOpenRemoteDataChannel")]
        [ForcedSleep(1.0)]
        public void OpenRemoteDataChannel(ScriptInstance instance)
        {
            ChannelInfo ci;
            lock (instance)
            {
                UUID scriptID = instance.Item.ID;
                ObjectPart part = instance.Part;
                UUID objectID = part.ID;
                UUID sceneID = part.ObjectGroup.Scene.ID;
                /* TODO: how to deal with persistency of channel ids here? As of now simply re-use objectid */
                if (!m_ScriptChannels.TryGetValue(scriptID, out ci))
                {
                    ci = new ChannelInfo(scriptID, sceneID, objectID, scriptID);
                    m_ScriptChannels.Add(scriptID, ci);
                    m_Channels.Add(ci.ChannelID, ci);

                }
                RegisterDataChannel(ci);
            }
        }

        void RegisterDataChannel(ChannelInfo ci)
        {
            SceneInterface scene;
            ObjectPart part;
            ObjectPartInventoryItem item;

            if (m_Scenes.TryGetValue(ci.SceneID, out scene) &&
                scene.Primitives.TryGetValue(ci.ObjectID, out part) &&
                part.Inventory.TryGetValue(ci.ItemID, out item))
            {
                ScriptInstance instance = item.ScriptInstance;
                if (null != instance)
                {
                    RemoteDataEvent ev = new RemoteDataEvent();
                    ev.Channel = ci.ChannelID;
                    ev.IData = 0;
                    ev.MessageID = UUID.Zero;
                    ev.SData = string.Empty;
                    ev.Sender = string.Empty;
                    ev.Type = REMOTE_DATA_REQUEST;
                    instance.PostEvent(ev);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llRemoteDataReply")]
        [ForcedSleep(3.0)]
        public void RemoteDataReply(ScriptInstance instance, LSLKey channel, LSLKey message_id, string sdata, int idata)
        {
            lock (instance)
            {
                ChannelInfo ci;
                BlockingQueue<RemoteDataReplyEvent> replyqueue;
                if (m_ScriptChannels.TryGetValue(channel.AsUUID, out ci) &&
                    ci.ActiveRequests.TryGetValue(message_id.AsUUID, out replyqueue))
                {
                    replyqueue.Enqueue(new RemoteDataReplyEvent(channel.AsUUID, sdata, idata));
                }
            }
        }

        [APILevel(APIFlags.LSL, "llRemoteDataSetRegion")]
        public void RemoteDataSetRegion(ScriptInstance instance)
        {
            OpenRemoteDataChannel(instance);
        }

        [ExecutedOnScriptReset]
        [ExecutedOnScriptRemove]
        public void ScriptResetOrRemove(ScriptInstance instance)
        {
            UUID itemid;
            lock (instance)
            {
                itemid = instance.Item.ID;
            }
            Remove(itemid);
        }
    }
}
