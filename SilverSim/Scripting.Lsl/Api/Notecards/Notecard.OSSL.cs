// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Notecards
{
    public partial class NotecardApi
    {
        #region osMakeNotecard
        [APILevel(APIFlags.OSSL, "osMakeNotecard")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void MakeNotecard(
            ScriptInstance instance, 
            [LSLTooltip("Name of notecard to be created")]
            string notecardName, 
            [LSLTooltip("Contents for the notecard. string is also allowed here.")]
            AnArray contents)
        {
            string nc = string.Empty;

            foreach(IValue val in contents)
            {
                if(!string.IsNullOrEmpty(nc))
                {
                    nc += "\n";
                }
                nc += val.ToString();
            }
            MakeNotecard(instance, notecardName, nc);
        }

        [APILevel(APIFlags.OSSL, "osMakeNotecard")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void MakeNotecard(
            ScriptInstance instance,
            [LSLTooltip("Name of notecard to be created")]
            string notecardName,
            [LSLTooltip("Contents for the notecard.")]
            string contents)
        {
            lock (instance)
            {
                ObjectPart thisPart = instance.Part;
                ObjectGroup thisGroup = thisPart.ObjectGroup;
                Notecard nc = new Notecard();
                nc.Text = contents;
                AssetData asset = nc;
                asset.ID = UUID.Random;
                asset.Name = notecardName;
                asset.Creator = thisGroup.Owner;
                thisGroup.Scene.AssetService.Store(asset);
                ObjectPartInventoryItem item = new ObjectPartInventoryItem(asset);
                item.ParentFolderID = thisPart.ID;

                for (uint i = 0; i < 1000; ++i)
                {
                    item.Name = (i == 0) ?
                        notecardName :
                        string.Format("{0} {1}", notecardName, i);

                    try
                    {
                        thisPart.Inventory.Add(item.ID, item.Name, item);
                    }
                    catch
                    {
                        return;
                    }
                }
            }
            throw new ArgumentException(string.Format("Could not store notecard with name {0}", notecardName));
        }
        #endregion

        #region osGetNotecard
        [APILevel(APIFlags.OSSL, "osGetNotecard")]
        [LSLTooltip("read the entire contents of a notecard directly.\nIt does not use the dataserver event.")]
        public string GetNotecard(
            ScriptInstance instance, 
            [LSLTooltip("name of notecard in inventory")]
            string name)
        {
            lock (instance)
            {
                UUID assetID = instance.GetNotecardAssetID(name);
                Notecard nc = instance.Part.ObjectGroup.Scene.GetService<NotecardCache>()[assetID];
                return nc.Text;
            }
        }
        #endregion

        #region osGetNotecardLine
        [APILevel(APIFlags.OSSL, "osGetNotecardLine")]
        [LSLTooltip("read a line of a notecard directly.\nIt does not use the dataserver event.")]
        public string OsGetNotecardLine(
            ScriptInstance instance, 
            [LSLTooltip("name of notecard in inventory")]
            string name, 
            [LSLTooltip("line number (starting at 0)")]
            int line)
        {
            lock (instance)
            {
                UUID assetID = instance.GetNotecardAssetID(name);
                Notecard nc = instance.Part.ObjectGroup.Scene.GetService<NotecardCache>()[assetID];
                string[] lines = nc.Text.Split('\n');
                if (line >= lines.Length || line < 0)
                {
                    return EOF;
                }
                return lines[line];
            }
        }
        #endregion

        #region osGetNumberOfNotecardLines
        [APILevel(APIFlags.OSSL, "osGetNumberOfNotecardLines")]
        [LSLTooltip("read number of lines of a notecard directly.\nIt does not use the dataserver event.")]
        public int OsGetNumberOfNotecardLines(
            ScriptInstance instance,
            [LSLTooltip("name of notecard in inventory")]
            string name)
        {
            lock (instance)
            {
                UUID assetID = instance.GetNotecardAssetID(name);
                Notecard nc = instance.Part.ObjectGroup.Scene.GetService<NotecardCache>()[assetID];
                return nc.Text.Split('\n').Length;
            }
        }
        #endregion
    }
}
