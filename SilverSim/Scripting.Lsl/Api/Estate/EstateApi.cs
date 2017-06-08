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

using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.IM;
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Estate
{
    [ScriptApiName("Estates")]
    [LSLImplementation]
    [Description("LSL/OSSL Estate API")]
    public class EstateApi : IScriptApi, IPlugin
    {
        [APILevel(APIFlags.LSL)]
        [Description("Add the agent to this estate's Allowed Residents list.")]
        public const int ESTATE_ACCESS_ALLOWED_AGENT_ADD = 0x4;
        [APILevel(APIFlags.LSL)]
        [Description("Remove the agent from this estate's Allowed Residents list.")]
        public const int ESTATE_ACCESS_ALLOWED_AGENT_REMOVE = 0x8;
        [APILevel(APIFlags.LSL)]
        [Description("Add the group to this estate's Allowed groups list.")]
        public const int ESTATE_ACCESS_ALLOWED_GROUP_ADD = 0x10;
        [APILevel(APIFlags.LSL)]
        [Description("Remove the group from this estate's Allowed groups list.")]
        public const int ESTATE_ACCESS_ALLOWED_GROUP_REMOVE = 0x20;
        [APILevel(APIFlags.LSL)]
        [Description("Add the agent to this estate's Banned residents list.")]
        public const int ESTATE_ACCESS_BANNED_AGENT_ADD = 0x40;
        [APILevel(APIFlags.LSL)]
        [Description("Remove the agent from this estate's Banned residents list.")]
        public const int ESTATE_ACCESS_BANNED_AGENT_REMOVE = 0x80;

        private SceneList m_Scenes;

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
        }

        [APILevel(APIFlags.OSSL, "osSetEstateSunSettings")]
        [Description("set new estate sun settings (EM or EO only)")]
        public void SetEstateSunSettings(ScriptInstance instance,
            [Description("set to TRUE if sun position is fixed see sunHour")]
            int isFixed,
            [Description("position of sun when set to be fixed (0-24, 0 => sunrise, 6 => midday, 12 => dusk, 18 => midnight)")]
            double sunHour)
        {
            lock (instance)
            {
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                SceneInterface scene = thisGroup.Scene;
                if (!scene.IsEstateManager(thisGroup.Owner))
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0ObjectOwnerMustBeAbleToManageEstate", "{0}: object owner must be able to manage estate.", "osSetEstateSunSettings"));
                    return;
                }

                EstateInfo estate;
                uint estateID;
                EstateServiceInterface estateService = scene.EstateService;
                if(estateService.RegionMap.TryGetValue(scene.ID, out estateID) &&
                    estateService.TryGetValue(estateID, out estate))
                {
                    estate.UseGlobalTime = isFixed != 0;
                    estate.SunPosition = sunHour.Clamp(0, 24);
                    estateService.Update(estate);
                    foreach(UUID regionID in estateService.RegionMap[estateID])
                    {
                        SceneInterface estateScene;
                        if(m_Scenes.TryGetValue(regionID, out estateScene))
                        {
                            estateScene.TriggerEstateUpdate();
                        }
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llManageEstateAccess")]
        [Description("Add or remove agents from the estate's agent access or ban lists or groups from the estate's group access list.")]
        public int ManageEstateAccess(ScriptInstance instance,
            [Description("ESTATE_ACCESS_* flag")]
            int action,
            [Description("avatar or group UUID")]
            LSLKey avatar)
        {
            lock(instance)
            {
                ObjectPart thisPart = instance.Part;
                ObjectGroup thisGroup = thisPart.ObjectGroup;
                SceneInterface scene = thisGroup.Scene;
                if (!scene.IsEstateManager(thisGroup.Owner))
                {
                    instance.ShoutError(new LocalizedScriptMessage(this, "Function0ObjectOwnerMustBeAbleToManageEstate", "{0}: object owner must be able to manage estate.", "llManageEstateAccess"));
                    return 0;
                }

                UUI uui = UUI.Unknown;
                UGI ugi = UGI.Unknown;
                GroupsNameServiceInterface groupsNameService = scene.GroupsNameService;
                switch(action)
                {
                    case ESTATE_ACCESS_ALLOWED_AGENT_ADD:
                    case ESTATE_ACCESS_ALLOWED_AGENT_REMOVE:
                    case ESTATE_ACCESS_BANNED_AGENT_ADD:
                    case ESTATE_ACCESS_BANNED_AGENT_REMOVE:
                        if (!scene.AvatarNameService.TryGetValue(avatar.AsUUID, out uui))
                        {
                            return 0;
                        }
                        break;

                    case ESTATE_ACCESS_ALLOWED_GROUP_ADD:
                    case ESTATE_ACCESS_ALLOWED_GROUP_REMOVE:
                        if(groupsNameService == null ||
                            !groupsNameService.TryGetValue(avatar.AsUUID, out ugi))
                        {
                            return 0;
                        }
                        break;

                    default:
                        return 0;
                }

                uint estateID;
                EstateInfo estate;
                EstateServiceInterface estateService = scene.EstateService;
                if(!estateService.RegionMap.TryGetValue(scene.ID, out estateID) ||
                    !estateService.TryGetValue(estateID, out estate))
                {
                    return 0;
                }

                string message = string.Empty;
                switch(action)
                {
                    case ESTATE_ACCESS_ALLOWED_AGENT_ADD:
                        estateService.EstateAccess[estateID, uui] = true;
                        message = string.Format("Added agent {0} to allowed list for estate {1}", uui.FullName, estate.Name);
                        break;

                    case ESTATE_ACCESS_ALLOWED_AGENT_REMOVE:
                        estateService.EstateAccess[estateID, uui] = true;
                        message = string.Format("Removed agent {0} from allowed list for estate {1}", uui.FullName, estate.Name);
                        break;

                    case ESTATE_ACCESS_BANNED_AGENT_ADD:
                        if(scene.IsEstateManager(uui))
                        {
                            return 0;
                        }
                        estateService.EstateBans[estateID, uui] = true;
                        message = string.Format("Added agent {0} to banned list for estate {1}", uui.FullName, estate.Name);
                        break;

                    case ESTATE_ACCESS_BANNED_AGENT_REMOVE:
                        estateService.EstateBans[estateID, uui] = false;
                        message = string.Format("Added agent {0} from banned list for estate {1}", uui.FullName, estate.Name);
                        break;

                    case ESTATE_ACCESS_ALLOWED_GROUP_ADD:
                        estateService.EstateGroup[estateID, ugi] = true;
                        message = string.Format("Added group {0} to allowed list for estate {1}", ugi.FullName, estate.Name);
                        break;

                    case ESTATE_ACCESS_ALLOWED_GROUP_REMOVE:
                        estateService.EstateGroup[estateID, ugi] = false;
                        message = string.Format("Removed group {0} from allowed list for estate {1}", ugi.FullName, estate.Name);
                        break;

                    default:
                        break;
                }

                ObjectPartInventoryItem.PermsGranterInfo grantInfo = instance.Item.PermsGranter;
                if (!grantInfo.PermsGranter.EqualsGrid(thisGroup.Owner) ||
                    (grantInfo.PermsMask & Types.Script.ScriptPermissions.SilentEstateManagement) == 0)
                {
                    /* send owner IM */
                    Vector3 globPos = thisGroup.GlobalPosition;
                    string binBuck = string.Format("{0}/{1}/{2}/{3}\0",
                        scene.Name,
                        (int)Math.Floor(globPos.X),
                        (int)Math.Floor(globPos.Y),
                        (int)Math.Floor(globPos.Z));

                    var im = new GridInstantMessage()
                    {
                        FromAgent = new UUI { ID = thisPart.Owner.ID, FullName = thisGroup.Name },
                        IMSessionID = thisGroup.ID,
                        ToAgent = thisGroup.Owner,
                        Position = globPos,
                        RegionID = scene.ID,
                        Message = message,
                        Dialog = GridInstantMessageDialog.MessageFromObject,
                        BinaryBucket = binBuck.ToUTF8Bytes(),
                        OnResult = (GridInstantMessage imret, bool success) => { }
                    };
                    IMServiceInterface imservice = instance.Part.ObjectGroup.Scene.GetService<IMServiceInterface>();
                    imservice.Send(im);
                }
                return 1;
            }
        }
    }
}
