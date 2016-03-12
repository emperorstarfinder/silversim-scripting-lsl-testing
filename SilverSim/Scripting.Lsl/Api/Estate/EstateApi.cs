// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

        public EstateApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
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
                    instance.ShoutError("osSetEstateSunSettings object owner must manage estate.");
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
                    estateService[estateID] = estate;
                    foreach(UUID regionID in estateService.RegionMap[estateID])
                    {
                        SceneInterface estateScene;
                        if(SceneManager.Scenes.TryGetValue(regionID, out estateScene))
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
                    instance.ShoutError("llManageEstateAccess object owner must manage estate.");
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
                        if(null == groupsNameService ||
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
                    GridInstantMessage im = new GridInstantMessage();
                    im.FromAgent.ID = thisPart.Owner.ID;
                    im.FromAgent.FullName = thisGroup.Name;
                    im.IMSessionID = thisGroup.ID;
                    im.ToAgent.ID = thisGroup.Owner.ID;
                    im.Position = thisGroup.GlobalPosition;
                    im.RegionID = scene.ID;
                    im.Message = message;
                    im.Dialog = GridInstantMessageDialog.MessageFromObject;
                    string binBuck = string.Format("{0}/{1}/{2}/{3}\0",
                        scene.Name,
                        (int)Math.Floor(im.Position.X),
                        (int)Math.Floor(im.Position.Y),
                        (int)Math.Floor(im.Position.Z));
                    im.BinaryBucket = binBuck.ToUTF8Bytes();
                    im.OnResult = delegate (GridInstantMessage imret, bool success) { };

                    IMServiceInterface imservice = instance.Part.ObjectGroup.Scene.GetService<IMServiceInterface>();
                    imservice.Send(im);
                }
                return 1;
            }
        }
    }
}
