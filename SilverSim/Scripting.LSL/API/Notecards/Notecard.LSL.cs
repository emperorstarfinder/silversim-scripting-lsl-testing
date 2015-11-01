// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Remoting.Messaging;

namespace SilverSim.Scripting.Lsl.Api.Notecards
{
    public partial class NotecardApi
    {
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const string EOF = "\n\n\n";

        #region llGetNotecardLine
        void GetNotecardLine(ObjectPart part, UUID queryID, UUID assetID, int line)
        {
            Notecard nc = part.ObjectGroup.Scene.GetService<NotecardCache>()[assetID];
            string[] lines = nc.Text.Split('\n');
            DataserverEvent e = new DataserverEvent();
            if (line >= lines.Length || line < 0)
            {
                e.Data = EOF;
                e.QueryID = queryID;
                part.PostEvent(e);
            }

            e.Data = lines[line];
            e.QueryID = queryID;
            part.PostEvent(e);
        }

        void GetNotecardLineEnd(IAsyncResult ar)
        {
            AsyncResult r = (AsyncResult)ar;
            Action<ObjectPart, UUID, UUID, int> caller = (Action<ObjectPart, UUID, UUID, int>)r.AsyncDelegate;
            caller.EndInvoke(ar);
        }

        void GetNotecardLineAsync(ScriptInstance instance, UUID queryID, UUID assetID, int line)
        {
            Action<ObjectPart, UUID, UUID, int> del = GetNotecardLine;
            del.BeginInvoke(instance.Part, queryID, assetID, line, GetNotecardLineEnd, this);
        }

        [APILevel(APIFlags.LSL, "llGetNotecardLine")]
        [ForcedSleep(0.1)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal LSLKey GetNotecardLine(ScriptInstance instance, string name, int line)
        {
            lock (instance)
            {
                ObjectPartInventoryItem item;
                if (instance.Part.Inventory.TryGetValue(name, out item))
                {
                    if (item.InventoryType != InventoryType.Notecard)
                    {
                        throw new ArgumentException(string.Format("Inventory item {0} is not a notecard", name));
                    }
                    else
                    {
                        UUID query = UUID.Random;
                        GetNotecardLineAsync(instance, query, item.AssetID, line);
                        return query;
                    }
                }
                else
                {
                    throw new ArgumentException(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }
        #endregion

        #region llGetNumberOfNotecardLines
        void GetNumberOfNotecardLines(ObjectPart part, UUID queryID, UUID assetID)
        {
            Notecard nc = part.ObjectGroup.Scene.GetService<NotecardCache>()[assetID];
            DataserverEvent e = new DataserverEvent();
            int n = 1;
            foreach (char c in nc.Text)
            {
                if (c == '\n')
                {
                    ++n;
                }
            }
            e.Data = n.ToString();
            e.QueryID = queryID;
            part.PostEvent(e);
        }

        void GetNumberOfNotecardLinesEnd(IAsyncResult ar)
        {
            AsyncResult r = (AsyncResult)ar;
            Action<ObjectPart, UUID, UUID> caller = (Action<ObjectPart, UUID, UUID>)r.AsyncDelegate;
            caller.EndInvoke(ar);
        }

        void GetNumberOfNotecardLinesAsync(ScriptInstance instance, UUID queryID, UUID assetID)
        {
            Action<ObjectPart, UUID, UUID> del = GetNumberOfNotecardLines;
            del.BeginInvoke(instance.Part, queryID, assetID, GetNumberOfNotecardLinesEnd, this);
        }

        [APILevel(APIFlags.LSL, "llGetNumberOfNotecardLines")]
        [ForcedSleep(0.1)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal LSLKey GetNumberOfNotecardLines(ScriptInstance instance, string name)
        {
            ObjectPartInventoryItem item;
            lock (instance)
            {
                if (instance.Part.Inventory.TryGetValue(name, out item))
                {
                    if (item.InventoryType != InventoryType.Notecard)
                    {
                        throw new ArgumentException(string.Format("Inventory item {0} is not a notecard", name));
                    }
                    else
                    {
                        UUID query = UUID.Random;
                        GetNumberOfNotecardLinesAsync(instance, query, item.AssetID);
                        return query;
                    }
                }
                else
                {
                    throw new ArgumentException(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }
        #endregion
    }
}
