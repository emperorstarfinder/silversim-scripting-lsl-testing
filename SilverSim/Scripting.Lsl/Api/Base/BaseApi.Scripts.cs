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
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Common;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llGetScriptName")]
        public string GetScriptName(ScriptInstance instance)
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

        [APILevel(APIFlags.LSL, "llMinEventDelay")]
        public void MinEventDelay(ScriptInstance instance, double delay)
        {
            var script = (Script)instance;
            lock(script)
            {
                script.MinEventDelay = delay;
            }
        }

        [APILevel(APIFlags.LSL, "llGetStartParameter")]
        public int GetStartParameter(ScriptInstance instance)
        {
            lock(instance)
            {
                return ((Script)instance).StartParameter;
            }
        }

        [APILevel(APIFlags.LSL, "llResetScript")]
        public void ResetScript(ScriptInstance instance)
        {
            throw new ResetScriptException(); /* exception triggers state change code */
        }

        [APILevel(APIFlags.LSL, "llResetOtherScript")]
        public void ResetOtherScript(ScriptInstance instance, string name)
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
                        throw new LocalizedScriptErrorException(this, "Function0Inventoryitem1IsNotAScript", "{0}: Inventory item {1} is not a script", "llResetOtherScript", name);
                    }
                    else if (si == null)
                    {
                        throw new LocalizedScriptErrorException(this, "Function0Inventoryitem1IsNotACompiledScript", "{0}: Inventory item {1} is not a compiled script.", "llResetOtherScript", name);
                    }
                    else
                    {
                        si.PostEvent(new ResetScriptEvent());
                    }
                }
                else
                {
                    throw new LocalizedScriptErrorException(this, "Function0Script1DoesNotExist", "{0}: Script {1} does not exist", "llResetOtherScript", name);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llGetScriptState")]
        public int GetScriptState(ScriptInstance instance, string script)
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
                        throw new LocalizedScriptErrorException(this, "Function0Inventoryitem1IsNotAScript", "{0}: Inventory item {1} is not a script", "llResetOtherScript", script);
                    }
                    else if (si == null)
                    {
                        throw new LocalizedScriptErrorException(this, "Function0Inventoryitem1IsNotACompiledScript", "{0}: Inventory item {1} is not a compiled script.", "llResetOtherScript", script);
                    }
                    else
                    {
                        return si.IsRunning ? TRUE : FALSE;
                    }
                }
                else
                {
                    throw new LocalizedScriptErrorException(this, "Function0Script1DoesNotExist", "{0}: Script {1} does not exist", "llResetOtherScript", script);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llSetScriptState")]
        public void SetScriptState(ScriptInstance instance, string script, int running)
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
                        throw new LocalizedScriptErrorException(this, "Function0Inventoryitem1IsNotAScript", "{0}: Inventory item {1} is not a script", "llResetOtherScript", script);
                    }
                    else if (si == null)
                    {
                        throw new LocalizedScriptErrorException(this, "Function0Inventoryitem1IsNotACompiledScript", "{0}: Inventory item {1} is not a compiled script.", "llResetOtherScript", script);
                    }
                    else
                    {
                        si.IsRunning = running != 0;
                    }
                }
                else
                {
                    throw new LocalizedScriptErrorException(this, "Function0Script1DoesNotExist", "{0}: Script {1} does not exist", "llResetOtherScript", script);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llRemoteLoadScript")]
        public void RemoteLoadScript(ScriptInstance instance, LSLKey target, string name, int running, int start_param)
        {
            lock (instance)
            {
                instance.ShoutError(new LocalizedScriptMessage(this, "DeprecatedllRemoteLoadScript", "This function has been deprecated. Please use llRemoteLoadScriptPin instead"));
            }
        }

        [APILevel(APIFlags.LSL, "llRemoteLoadScriptPin")]
        [ForcedSleep(3)]
        public void RemoteLoadScriptPin(ScriptInstance instance, LSLKey target, string name, int pin, int running, int start_param)
        {
            lock(instance)
            {
                ObjectPartInventoryItem scriptitem;
                ObjectPart thisPart = instance.Part;
                ObjectGroup thisGroup = thisPart.ObjectGroup;
                ObjectPart destpart;
                AssetData asset;
                if(!thisGroup.Scene.Primitives.TryGetValue(target, out destpart))
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0DestinationPrimDoesNotExist", "{0}: destination prim does not exist", "llRemoteLoadScriptPin"));
                    return;
                }

                if(!thisPart.Inventory.TryGetValue(name, out scriptitem))
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0Script1DoesNotExist", "{0}: Script '{1}' does not exist", "llRemoteLoadScriptPin", name));
                    return;
                }

                try
                {
                    asset = instance.Part.ObjectGroup.Scene.AssetService[scriptitem.AssetID];
                }
                catch
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0FailedToFindAssetForScript1", "{0}: Failed to find asset for script '{1}'", "llRemoteLoadScriptPin", name));
                    return;
                }

                if (destpart.ID == thisPart.ID)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0UnableToAddItem", "{0}: Unable to add item", "llRemoteLoadScriptPin"));
                    return;
                }

                if(scriptitem.InventoryType != InventoryType.LSLText)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0Inventoryitem1IsNotAScript", "{0}: Inventory item '{1}' is not a script", "llRemoteLoadScriptPin", name));
                    return;
                }

                if (destpart.Owner != thisPart.Owner)
                {
                    if ((scriptitem.Permissions.Current & InventoryPermissionsMask.Transfer) == 0)
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "Function0Item1DoesNotHaveTransferPermission", "{0}: Item {1} does not have transfer permission", "llRemoteLoadScriptPin", scriptitem.Name));
                        return;
                    }
                    else if (destpart.CheckPermissions(thisPart.Owner, thisGroup.Group, InventoryPermissionsMask.Modify))
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "Function0DestPrim1DoesNotHaveModifyPermisions", "{0}: Destination prim {1} does not have modify permission", "llRemoteLoadScriptPin", destpart.Name));
                        return;
                    }
                }
                if ((scriptitem.Permissions.Current & InventoryPermissionsMask.Copy) == 0)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0Item1DoesNotHaveCopyPermission", "{0}: Item {1} does not have copy permission", "llRemoteLoadScriptPin", scriptitem.Name));
                    return;
                }

                if(destpart.ObjectGroup.AttachPoint != AttachmentPoint.NotAttached)
                {
                    return;
                }

                if(destpart.ScriptAccessPin != pin)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0Item1TryingToLoadScriptOntoPrim2WithoutCorrectAccessPin", "{0}: Item {1} trying to load script onto prim {2} without correct access pin", "llRemoteLoadScriptPin", thisPart.Name, destpart.Name));
                    return;
                }

                var newitem = new ObjectPartInventoryItem(scriptitem);
                destpart.Inventory.Replace(name, newitem);
                ScriptInstance oldInstance = scriptitem.ScriptInstance;
                /* duplicate script state */
                if (oldInstance != null)
                {
                    newitem.ScriptState = oldInstance.ScriptState;
                }
                ScriptInstance newInstance = ScriptLoader.Load(destpart, newitem, newitem.Owner, asset, null);
                if(oldInstance != null)
                {
                    newInstance.IsRunning = running != 0;
                }
                else if (running != 0)
                {
                    newInstance.Start(start_param);
                }
            }
        }
    }
}
