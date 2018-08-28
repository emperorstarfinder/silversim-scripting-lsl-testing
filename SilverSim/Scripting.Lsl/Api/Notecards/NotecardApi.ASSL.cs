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
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using System;
using System.Runtime.Remoting.Messaging;

namespace SilverSim.Scripting.Lsl.Api.Notecards
{
    public partial class NotecardApi
    {
        #region asGetNotecardLineUnicast
        private void GetNotecardLineUnicast(ObjectPart part, UUID itemid, UUID queryID, UUID assetID, int line)
        {
            try
            {
                Notecard nc = part.ObjectGroup.Scene.GetService<NotecardCache>()[assetID];
                string[] lines = nc.Text.Split('\n');
                ObjectPartInventoryItem item;
                if (part.Inventory.TryGetValue(itemid, out item))
                {
                    ScriptInstance instance = item.ScriptInstance;
                    if (line >= lines.Length || line < 0)
                    {
                        instance?.PostEvent(new DataserverEvent
                        {
                            Data = EOF,
                            QueryID = queryID
                        });
                    }
                    else
                    {
                        instance?.PostEvent(new DataserverEvent
                        {
                            Data = lines[line],
                            QueryID = queryID
                        });
                    }
                }
            }
            catch (Exception e)
            {
                /* do not push any exceptions on system threadpool */
                m_Log.Error("asGetUnicastNotecardLine failed with exception", e);
            }
        }

        private void GetNotecardLineUnicastEnd(IAsyncResult ar)
        {
            var r = (AsyncResult)ar;
            var caller = (Action<ObjectPart, UUID, UUID, int>)r.AsyncDelegate;
            caller.EndInvoke(ar);
        }

        private void GetNotecardLineUnicastAsyncInvoke(ScriptInstance instance, UUID queryID, UUID assetID, int line)
        {
            Action<ObjectPart, UUID, UUID, UUID, int> del = GetNotecardLineUnicast;
            del.BeginInvoke(instance.Part, instance.Item.ID, queryID, assetID, line, GetNotecardLineUnicastEnd, this);
        }

        [APILevel(APIFlags.ASSL, "asGetUnicastNotecardLine")]
        public LSLKey GetNotecardLineUnicast(ScriptInstance instance, string name, int line)
        {
            lock (instance)
            {
                UUID assetID = instance.GetNotecardAssetID(name);
                UUID query = UUID.Random;
                GetNotecardLineUnicastAsyncInvoke(instance, query, assetID, line);
                return query;
            }
        }

        [APILevel(APIFlags.ASSL, "asGetUnicastLinkNotecardLine")]
        public LSLKey GetNotecardLineUnicast(ScriptInstance instance, int link, string name, int line)
        {
            lock (instance)
            {
                UUID assetID = instance.GetNotecardAssetID(name, link);
                UUID query = UUID.Random;
                GetNotecardLineUnicastAsyncInvoke(instance, query, assetID, line);
                return query;
            }
        }
        #endregion

        #region asGetNumberOfNotecardLinesUnicast
        private void GetNumberOfNotecardLinesUnicast(ObjectPart part, UUID itemID, UUID queryID, UUID assetID)
        {
            try
            {
                Notecard nc = part.ObjectGroup.Scene.GetService<NotecardCache>()[assetID];
                int n = 1;
                foreach (char c in nc.Text)
                {
                    if (c == '\n')
                    {
                        ++n;
                    }
                }
                ObjectPartInventoryItem item;
                if (part.Inventory.TryGetValue(itemID, out item))
                {
                    item.ScriptInstance?.PostEvent(new DataserverEvent
                    {
                        Data = n.ToString(),
                        QueryID = queryID
                    });
                }
            }
            catch (Exception e)
            {
                /* do not push any exceptions on system threadpool */
                m_Log.Error("asGetUnicastNumberOfNotecardLines failed with exception", e);
            }
        }

        private void GetNumberOfNotecardLinesUnicastEnd(IAsyncResult ar)
        {
            var r = (AsyncResult)ar;
            var caller = (Action<ObjectPart, UUID, UUID>)r.AsyncDelegate;
            caller.EndInvoke(ar);
        }

        private void GetNumberOfNotecardLinesUnicastAsyncInvoke(ScriptInstance instance, UUID queryID, UUID assetID)
        {
            Action<ObjectPart, UUID, UUID, UUID> del = GetNumberOfNotecardLinesUnicast;
            del.BeginInvoke(instance.Part, instance.Item.ID, queryID, assetID, GetNumberOfNotecardLinesUnicastEnd, this);
        }

        [APILevel(APIFlags.LSL, "asGetUnicastNumberOfNotecardLines")]
        public LSLKey GetNumberOfNotecardLinesUnicast(ScriptInstance instance, string name)
        {
            lock (instance)
            {
                UUID assetID = instance.GetNotecardAssetID(name);
                UUID query = UUID.Random;
                GetNumberOfNotecardLinesAsyncInvoke(instance, query, assetID);
                return query;
            }
        }

        [APILevel(APIFlags.LSL, "asGetUnicastLinkNumberOfNotecardLines")]
        public LSLKey GetNumberOfNotecardLinesUnicast(ScriptInstance instance, int link, string name)
        {
            lock (instance)
            {
                UUID assetID = instance.GetNotecardAssetID(name, link);
                UUID query = UUID.Random;
                GetNumberOfNotecardLinesUnicastAsyncInvoke(instance, query, assetID);
                return query;
            }
        }
        #endregion
    }
}
