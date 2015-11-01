// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
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
        [LSLTooltip("Creates a notecard with text in the prim that contains the script. Contents can be either a list or a string.")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void MakeNotecard(
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
        [LSLTooltip("Creates a notecard with text in the prim that contains the script. Contents can be either a list or a string.")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void MakeNotecard(
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
                    if (i == 0)
                    {
                        item.Name = notecardName;
                    }
                    else
                    {
                        item.Name = string.Format("{0} {1}", notecardName, i);
                    }
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal string GetNotecard(
            ScriptInstance instance, 
            [LSLTooltip("name of notecard in inventory")]
            string name)
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
                        Notecard nc = instance.Part.ObjectGroup.Scene.GetService<NotecardCache>()[item.AssetID];
                        return nc.Text;
                    }
                }
                else
                {
                    throw new ArgumentException(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }
        #endregion

        #region osGetNotecardLine
        [APILevel(APIFlags.OSSL, "osGetNotecardLine")]
        [LSLTooltip("read a line of a notecard directly.\nIt does not use the dataserver event.")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal string OsGetNotecardLine(
            ScriptInstance instance, 
            [LSLTooltip("name of notecard in inventory")]
            string name, 
            [LSLTooltip("line number (starting at 0)")]
            int line)
        {
            ObjectPartInventoryItem item;
            lock (instance)
            {
                ObjectPart thisPart = instance.Part;
                if (thisPart.Inventory.TryGetValue(name, out item))
                {
                    if (item.InventoryType != InventoryType.Notecard)
                    {
                        throw new ArgumentException(string.Format("Inventory item {0} is not a notecard", name));
                    }
                    else
                    {
                        Notecard nc = thisPart.ObjectGroup.Scene.GetService<NotecardCache>()[item.AssetID];
                        string[] lines = nc.Text.Split('\n');
                        if (line >= lines.Length || line < 0)
                        {
                            return EOF;
                        }
                        return lines[line];
                    }
                }
                else
                {
                    throw new ArgumentException(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }
        #endregion

        #region osGetNumberOfNotecardLines
        [APILevel(APIFlags.OSSL, "osGetNumberOfNotecardLines")]
        [LSLTooltip("read number of lines of a notecard directly.\nIt does not use the dataserver event.")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int OsGetNumberOfNotecardLines(
            ScriptInstance instance,
            [LSLTooltip("name of notecard in inventory")]
            string name)
        {
            ObjectPartInventoryItem item;
            lock (instance)
            {
                ObjectPart thisPart = instance.Part;
                if (thisPart.Inventory.TryGetValue(name, out item))
                {
                    if (item.InventoryType != InventoryType.Notecard)
                    {
                        throw new ArgumentException(string.Format("Inventory item {0} is not a notecard", name));
                    }
                    else
                    {
                        Notecard nc = thisPart.ObjectGroup.Scene.GetService<NotecardCache>()[item.AssetID];
                        return nc.Text.Split('\n').Length;
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
