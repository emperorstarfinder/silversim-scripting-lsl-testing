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

#pragma warning disable IDE0018, RCS1029

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        private void CheckDeLinkingPerms(ScriptInstance instance, Action delinkAction)
        {
            ObjectGroup grp = instance.Part.ObjectGroup;
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
            if (grp.IsAttached)
            {
                return;
            }
            else if(!instance.Part.CheckPermissions(instance.Part.Owner, instance.Part.Group, Types.Inventory.InventoryPermissionsMask.Modify))
            {
                instance.ShoutError(new LocalizedScriptMessage(this, "DelinkFailedDueNoEditPerms", "Delink failed because you do not have edit permission"));
            }
            else if (grantinfo.PermsGranter != UGUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.ChangeLinks) != 0)
            {
                if (grantinfo.PermsGranter != instance.Part.Owner)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "OnlyOwnerIsAbleToDelinkObjectByScript", "Only owner is able to delink by script"));
                }
                else
                {
                    delinkAction();
                }
            }
        }

        private void CheckLinkingPerms(ScriptInstance instance, Action linkAction)
        {
            ObjectGroup grp = instance.Part.ObjectGroup;
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
            if (grp.IsAttached)
            {
                return;
            }
            else if (!instance.Part.CheckPermissions(instance.Part.Owner, instance.Part.Group, Types.Inventory.InventoryPermissionsMask.Modify))
            {
                instance.ShoutError(new LocalizedScriptMessage(this, "LinkFailedDueNoEditPerms", "Linking failed because you do not have edit permission"));
            }
            else if (grantinfo.PermsGranter != UGUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.ChangeLinks) != 0)
            {
                if (grantinfo.PermsGranter != instance.Part.Owner)
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "OnlyOwnerIsAbleToLinkObjectByScript", "Only owner is able to link by script"));
                }
                else
                {
                    linkAction();
                }
            }
        }

        [APILevel(APIFlags.LSL, "llCreateLink")]
        [ForcedSleep(1.0)]
        public void CreateLink(ScriptInstance instance, LSLKey key, int parent)
        {
            lock (instance)
            {
                CheckLinkingPerms(instance, () =>
                {
                    ObjectGroup thisGrp = instance.Part.ObjectGroup;
                    SceneInterface scene = thisGrp.Scene;
                    ObjectGroup targetGrp;
                    if (!scene.ObjectGroups.TryGetValue(key, out targetGrp))
                    {
                        return;
                    }

                    if (!targetGrp.Owner.EqualsGrid(thisGrp.Owner))
                    {
                        return;
                    }

                    if (parent != 0)
                    {
                        scene.LinkObjects(new List<UUID> { thisGrp.ID, targetGrp.ID }, true);
                    }
                    else
                    {
                        scene.LinkObjects(new List<UUID> { targetGrp.ID, thisGrp.ID }, true);
                    }
                });
            }
        }

        [APILevel(APIFlags.LSL, "llBreakLink")]
        public void BreakLink(ScriptInstance instance, int link)
        {
            lock (instance)
            {
                CheckDeLinkingPerms(instance, () =>
                {
                    ObjectGroup grp = instance.Part.ObjectGroup;
                    grp.AgentSitting.UnSitAll();
                    ObjectPart part;
                    if (grp.TryGetValue(link, out part))
                    {
                        grp.Scene.UnlinkObjects(new List<UUID> { part.ID });
                    }
                });
            }
        }

        [APILevel(APIFlags.LSL, "llBreakAllLinks")]
        public void BreakAllLinks(ScriptInstance instance)
        {
            lock (instance)
            {
                CheckDeLinkingPerms(instance, () =>
                {
                    ObjectGroup grp = instance.Part.ObjectGroup;
                    grp.AgentSitting.UnSitAll();
                    grp.Scene.UnlinkObjects(grp.Keys2);
                });
            }
        }

        [APILevel(APIFlags.OSSL, "osForceCreateLink")]
        [CheckFunctionPermission("osForceCreateLink")]
        public void ForceCreateLink(ScriptInstance instance, LSLKey target, int parent)
        {
            lock (instance)
            {
                ObjectGroup thisGrp = instance.Part.ObjectGroup;
                SceneInterface scene = thisGrp.Scene;
                ObjectGroup targetGrp;
                if (!scene.ObjectGroups.TryGetValue(target, out targetGrp))
                {
                    return;
                }

                if (!targetGrp.Owner.EqualsGrid(thisGrp.Owner))
                {
                    return;
                }

                if (parent != 0)
                {
                    scene.LinkObjects(new List<UUID> { thisGrp.ID, targetGrp.ID });
                }
                else
                {
                    scene.LinkObjects(new List<UUID> { targetGrp.ID, thisGrp.ID });
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osForceBreakLink")]
        [CheckFunctionPermission("osForceCreateLink")]
        public void ForceBreakLink(ScriptInstance instance, int link)
        {
            lock(instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                grp.AgentSitting.UnSitAll();
                ObjectPart part;
                if (grp.TryGetValue(link, out part))
                {
                    grp.Scene.UnlinkObjects(new List<UUID> { part.ID });
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osForceBreakAllLinks")]
        [CheckFunctionPermission("osForceCreateLink")]
        public void ForceBreakAllLinks(ScriptInstance instance)
        {
            lock(instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                grp.AgentSitting.UnSitAll();
                grp.Scene.UnlinkObjects(grp.Keys2);
            }
        }
    }
}
