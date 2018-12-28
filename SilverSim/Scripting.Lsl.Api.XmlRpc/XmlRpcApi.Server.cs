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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.Lsl.Api.XmlRpc
{
    public partial class XmlRpcApi
    {
        [APILevel(APIFlags.LSL, "llCloseRemoteDataChannel")]
        [ForcedSleep(1.0)]
        public void CloseRemoteDataChannel(ScriptInstance instance, LSLKey key)
        {
            lock (instance)
            {
                m_Server.RemoveChannel(instance.Item.ID, key.AsUUID);
            }
        }

        [APILevel(APIFlags.LSL, "llOpenRemoteDataChannel")]
        [ForcedSleep(1.0)]
        public void OpenRemoteDataChannel(ScriptInstance instance)
        {
            lock (instance)
            {
                ObjectPart part = instance.Part;
                m_Server.RegisterChannel(part.ObjectGroup.Scene.ID, part.ID, instance.Item.ID);
            }
        }

        [APILevel(APIFlags.LSL, "llRemoteDataReply")]
        [ForcedSleep(3.0)]
        public void RemoteDataReply(ScriptInstance instance, LSLKey channel, LSLKey message_id, string sdata, int idata)
        {
            lock (instance)
            {
                m_Server.ReplyXmlRpc(message_id.AsUUID, idata, sdata);
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
            lock (instance)
            {
                m_Server.Remove(instance.Item.ID);
            }
        }
    }
}
