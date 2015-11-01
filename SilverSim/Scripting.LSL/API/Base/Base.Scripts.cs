// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.LSL.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llGetScriptName")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        string GetScriptName(ScriptInstance instance)
        {
            lock (instance)
            {
                try
                {
                    return instance.Item.Name;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        [APILevel(APIFlags.LSL, "llResetScript")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void ResetScript(ScriptInstance instance)
        {
            throw new ResetScriptException(); /* exception triggers state change code */
        }

        [APILevel(APIFlags.LSL, "llResetOtherScript")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void ResetOtherScript(ScriptInstance instance, string name)
        {
            lock (instance)
            {
                ObjectPartInventoryItem item;
                ScriptInstance si;
                if (instance.Part.Inventory.TryGetValue(name, out item))
                {
                    si = item.ScriptInstance;
                    if (item.InventoryType != InventoryType.LSLText && item.InventoryType != InventoryType.LSLBytecode)
                    {
                        throw new ArgumentException(string.Format("Inventory item {0} is not a script", name));
                    }
                    else if (null == si)
                    {
                        throw new ArgumentException(string.Format("Inventory item {0} is not a compiled script", name));
                    }
                    else
                    {
                        si.PostEvent(new ResetScriptEvent());
                    }
                }
                else
                {
                    throw new ArgumentException(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetScriptState")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        int GetScriptState(ScriptInstance instance, string script)
        {
            ObjectPartInventoryItem item;
            ScriptInstance si;
            lock (instance)
            {
                if (instance.Part.Inventory.TryGetValue(script, out item))
                {
                    si = item.ScriptInstance;
                    if (item.InventoryType != InventoryType.LSLText && item.InventoryType != InventoryType.LSLBytecode)
                    {
                        throw new ArgumentException(string.Format("Inventory item {0} is not a script", script));
                    }
                    else if (null == si)
                    {
                        throw new ArgumentException(string.Format("Inventory item {0} is not a compiled script", script));
                    }
                    else
                    {
                        return si.IsRunning ? TRUE : FALSE;
                    }
                }
                else
                {
                    throw new ArgumentException(string.Format("Inventory item {0} does not exist", script));
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetScriptState")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetScriptState(ScriptInstance instance, string script, int running)
        {
            ObjectPartInventoryItem item;
            ScriptInstance si;
            lock (instance)
            {
                if (instance.Part.Inventory.TryGetValue(script, out item))
                {
                    si = item.ScriptInstance;
                    if (item.InventoryType != InventoryType.LSLText && item.InventoryType != InventoryType.LSLBytecode)
                    {
                        throw new ArgumentException(string.Format("Inventory item {0} is not a script", script));
                    }
                    else if (null == si)
                    {
                        throw new ArgumentException(string.Format("Inventory item {0} is not a compiled script", script));
                    }
                    else
                    {
                        si.IsRunning = running != 0;
                    }
                }
                else
                {
                    throw new ArgumentException(string.Format("Inventory item {0} does not exist", script));
                }
            }
        }

        [APILevel(APIFlags.LSL, "llRemoteLoadScript")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void RemoteLoadScript(ScriptInstance instance, LSLKey target, string name, int running, int start_param)
        {
            lock (instance)
            {
                instance.ShoutError("This function has been deprecated. Please use llRemoteLoadscriptPin instead");
            }
        }

        [APILevel(APIFlags.LSL, "llRemoteLoadScriptPin")]
        [ForcedSleep(3)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void RemoteLoadScriptPin(ScriptInstance instance, LSLKey target, string name, int pin, int running, int start_param)
        {
            lock(instance)
            {
                ObjectPartInventoryItem scriptitem;
                ObjectPart destpart;
                AssetData asset;
                try
                {
                    destpart = instance.Part.ObjectGroup.Scene.Primitives[target];
                }
                catch
                {
                    instance.ShoutError("llRemoteLoadScriptPin: destination prim does not exist");
                    return;
                }

                try
                {
                    scriptitem = instance.Part.Inventory[name];
                }
                catch
                {
                    instance.ShoutError(string.Format("llRemoteLoadScriptPin: Script '{0}' does not exist", name));
                    return;
                }

                try
                {
                    asset = instance.Part.ObjectGroup.Scene.AssetService[scriptitem.AssetID];
                }
                catch
                {
                    instance.ShoutError(string.Format("llRemoteLoadScriptPin: Failed to find asset for script '{0}'", name));
                    return;
                }

                if (destpart.ID == instance.Part.ID)
                {
                    instance.ShoutError("llRemoteLoadScriptPin: Unable to add item");
                    return;
                }

                if(scriptitem.InventoryType != InventoryType.LSLText)
                {
                    instance.ShoutError(string.Format("llRemoteLoadScriptPin: Inventory item '{0}' is not a script", name));
                    return;
                }

                if (destpart.Owner != instance.Part.Owner)
                {
                    if ((scriptitem.Permissions.Current & InventoryPermissionsMask.Transfer) == 0)
                    {
                        instance.ShoutError(string.Format("llRemoteLoadScriptPin: Item {0} does not have transfer permission", scriptitem.Name));
                        return;
                    }
                    else if(destpart.CheckPermissions(instance.Part.Owner, instance.Part.ObjectGroup.Group, InventoryPermissionsMask.Modify))
                    {
                        instance.ShoutError(string.Format("llRemoteLoadScriptPin: Dest Part {0} does not have modify permission", destpart.Name));
                        return;
                    }
                }
                if ((scriptitem.Permissions.Current & InventoryPermissionsMask.Copy) == 0)
                {
                    instance.ShoutError(string.Format("llRemoteLoadScriptPin: Item {0} does not have copy permission", scriptitem.Name));
                    return;
                }

                if(destpart.ObjectGroup.AttachPoint != AttachmentPoint.NotAttached)
                {
                    return;
                }

                if(destpart.ScriptAccessPin != pin)
                {
                    instance.ShoutError(string.Format("llRemoteLoadScriptPin: Item {0} trying to load script onto prim {1} without correct access pin", instance.Part.Name, destpart.Name));
                    return;
                }

                ObjectPartInventoryItem newitem = new ObjectPartInventoryItem(scriptitem);
                destpart.Inventory.Replace(name, newitem);
                ScriptInstance newInstance = ScriptLoader.Load(destpart, newitem, newitem.Owner, asset);
                newInstance.IsRunning = running != 0;
                newInstance.PostEvent(new OnRezEvent(start_param));
            }
        }
    }
}
