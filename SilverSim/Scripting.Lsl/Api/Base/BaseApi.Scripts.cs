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

#pragma warning disable IDE0018
#pragma warning disable RCS1029
#pragma warning disable RCS1163

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
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
        [APIExtension(APIExtension.Extern, "asGetCurrentState")]
        [APILevel(APIFlags.ASSL, "asGetCurrentState")]
        public string GetCurrentState(ScriptInstance instance)
        {
            lock(instance)
            {
                return ((Script)instance).GetCurrentState();
            }
        }

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
        public void ResetScript()
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
                    if (item.InventoryType != InventoryType.LSL)
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
                    if (item.InventoryType != InventoryType.LSL)
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
                    if (item.InventoryType != InventoryType.LSL)
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

        [APILevel(APIFlags.ASSL)]
        public const int REMOTE_LOAD_SUCCESS = 0;
        [APILevel(APIFlags.ASSL)]
        public const int REMOTE_LOAD_BAD_PIN = -1;
        [APILevel(APIFlags.ASSL)]
        public const int REMOTE_LOAD_NO_PIN = -2;
        [APILevel(APIFlags.ASSL)]
        public const int REMOTE_LOAD_NOT_A_SCRIPT = -3;
        [APILevel(APIFlags.ASSL)]
        public const int REMOTE_LOAD_ITEM_DOES_NOT_EXIST = -4;
        [APILevel(APIFlags.ASSL)]
        public const int REMOTE_LOAD_TARGET_DOES_NOT_EXIST = -5;
        [APILevel(APIFlags.ASSL)]
        public const int REMOTE_LOAD_ASSET_MISSING = -6;
        [APILevel(APIFlags.ASSL)]
        public const int REMOTE_LOAD_SAME_PRIM = -7;
        [APILevel(APIFlags.ASSL)]
        public const int REMOTE_LOAD_TRANSFER_REQUIRED = -8;
        [APILevel(APIFlags.ASSL)]
        public const int REMOTE_LOAD_NO_TARGET_PERMISSIONS = -9;
        [APILevel(APIFlags.ASSL)]
        public const int REMOTE_LOAD_COPY_REQUIRED = -10;
        [APILevel(APIFlags.ASSL)]
        public const int REMOTE_LOAD_SCRIPT_ERROR = -11;

        [APILevel(APIFlags.LSL, "llRemoteLoadScriptPin")]
        [ForcedSleep(3)]
        public void RemoteLoadScriptPin(ScriptInstance instance, LSLKey target, string name, int pin, int running, int start_param) =>
            RemoteLoadScriptPin(instance, target, name, pin, running, start_param, true);

        [APILevel(APIFlags.ASSL, "asRemoteLoadScriptPin")]
        public int RemoteLoadScriptPinWithReturn(ScriptInstance instance, LSLKey target, string name, int pin, int running, int start_param) =>
            RemoteLoadScriptPin(instance, target, name, pin, running, start_param, false);

        public int RemoteLoadScriptPin(ScriptInstance instance, LSLKey target, string name, int pin, int running, int start_param, bool doShout)
        {
            lock(instance)
            {
                ObjectPartInventoryItem scriptitem;
                ObjectPart thisPart = instance.Part;
                ObjectGroup thisGroup = thisPart.ObjectGroup;
                SceneInterface thisScene = thisGroup.Scene;
                ObjectPart destpart;
                AssetData asset;
                if(!thisScene.Primitives.TryGetValue(target, out destpart))
                {
                    if (doShout)
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "Function0DestinationPrimDoesNotExist", "{0}: destination prim does not exist", "llRemoteLoadScriptPin"));
                    }
                    return REMOTE_LOAD_TARGET_DOES_NOT_EXIST;
                }

                if(!thisPart.Inventory.TryGetValue(name, out scriptitem))
                {
                    if (doShout)
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "Function0Script1DoesNotExist", "{0}: Script '{1}' does not exist", "llRemoteLoadScriptPin", name));
                    }
                    return REMOTE_LOAD_ITEM_DOES_NOT_EXIST;
                }

                try
                {
                    asset = thisScene.AssetService[scriptitem.AssetID];
                }
                catch
                {
                    if (doShout)
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "Function0FailedToFindAssetForScript1", "{0}: Failed to find asset for script '{1}'", "llRemoteLoadScriptPin", name));
                    }
                    return REMOTE_LOAD_ASSET_MISSING;
                }

                if (destpart.ID == thisPart.ID)
                {
                    if (doShout)
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "Function0UnableToAddItem", "{0}: Unable to add item", "llRemoteLoadScriptPin"));
                    }
                    return REMOTE_LOAD_SAME_PRIM;
                }

                if(scriptitem.InventoryType != InventoryType.LSL || scriptitem.AssetType != AssetType.LSLText || asset.Type != AssetType.LSLText)
                {
                    if (doShout)
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "Function0Inventoryitem1IsNotAScript", "{0}: Inventory item '{1}' is not a script", "llRemoteLoadScriptPin", name));
                    }
                    return REMOTE_LOAD_NOT_A_SCRIPT;
                }

                if (destpart.Owner != thisPart.Owner)
                {
                    if(pin == 0)
                    {
                        if (doShout)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "Function0PinCannotBeZero", "{0}: Pin cannot be zero", "llRemoteLoadScriptPin", scriptitem.Name));
                        }
                        return REMOTE_LOAD_NO_PIN;
                    }
                    else if ((scriptitem.Permissions.Current & InventoryPermissionsMask.Transfer) == 0)
                    {
                        if (doShout)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "Function0Item1DoesNotHaveTransferPermission", "{0}: Item {1} does not have transfer permission", "llRemoteLoadScriptPin", scriptitem.Name));
                        }
                        return REMOTE_LOAD_TRANSFER_REQUIRED;
                    }
                    else if (destpart.CheckPermissions(thisPart.Owner, thisGroup.Group, InventoryPermissionsMask.Modify))
                    {
                        if (doShout)
                        {
                            instance.ShoutError(new LocalizedScriptMessage(this, "Function0DestPrim1DoesNotHaveModifyPermisions", "{0}: Destination prim {1} does not have modify permission", "llRemoteLoadScriptPin", destpart.Name));
                        }
                        return REMOTE_LOAD_NO_TARGET_PERMISSIONS;
                    }
                }
                if ((scriptitem.Permissions.Current & InventoryPermissionsMask.Copy) == 0)
                {
                    if (doShout)
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "Function0Item1DoesNotHaveCopyPermission", "{0}: Item {1} does not have copy permission", "llRemoteLoadScriptPin", scriptitem.Name));
                    }
                    return REMOTE_LOAD_COPY_REQUIRED;
                }

                if(destpart.ObjectGroup.AttachPoint != AttachmentPoint.NotAttached)
                {
                    return REMOTE_LOAD_NO_TARGET_PERMISSIONS;
                }

                if(destpart.ScriptAccessPin != pin)
                {
                    if (doShout)
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "Function0Item1TryingToLoadScriptOntoPrim2WithoutCorrectAccessPin", "{0}: Item {1} trying to load script onto prim {2} without correct access pin", "llRemoteLoadScriptPin", thisPart.Name, destpart.Name));
                    }
                    else
                    {
                        /* prevent PIN poking */
                        instance.Sleep(3);
                    }
                    return REMOTE_LOAD_BAD_PIN;
                }

                var newitem = new ObjectPartInventoryItem(scriptitem);
                destpart.Inventory.Replace(name, newitem);
                ScriptInstance oldInstance = scriptitem.ScriptInstance;
                /* duplicate script state */
                if (oldInstance != null)
                {
                    newitem.ScriptState = oldInstance.ScriptState;
                }
                ScriptInstance newInstance;
                try
                {
                    newInstance = ScriptLoader.Load(destpart, newitem, newitem.Owner, asset, null);
                }
                catch
                {
                    return REMOTE_LOAD_SCRIPT_ERROR;
                }

                newInstance.IsRunningAllowed = thisScene.CanRunScript(instance.Item.Owner, thisGroup.GlobalPosition, instance.Item.AssetID);
                if (oldInstance != null)
                {
                    newInstance.IsRunning = running != 0;
                }
                else if (running != 0)
                {
                    newInstance.Start(start_param);
                }

                return REMOTE_LOAD_SUCCESS;
            }
        }
    }
}
