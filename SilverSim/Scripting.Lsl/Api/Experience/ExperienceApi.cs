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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Experience;
using SilverSim.Types;
using SilverSim.Types.Experience;
using SilverSim.Types.Parcel;
using SilverSim.Types.Script;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Experience
{
    [ScriptApiName("Experience")]
    [LSLImplementation]
    [Description("LSL Experience API")]
    public sealed partial class ExperienceApi : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_NONE = 0;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_THROTTLED = 1;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_EXPERIENCES_DISABLED = 2;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_INVALID_PARAMETERS = 3;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_NOT_PERMITTED = 4;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_NO_EXPERIENCE = 5;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_NOT_FOUND = 6;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_INVALID_EXPERIENCE = 7;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_EXPERIENCE_DISABLED = 8;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_EXPERIENCE_SUSPENDED = 9;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_UNKNOWN_ERROR = 10;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_QUOTA_EXCEEDED = 11;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_STORE_DISABLED = 12;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_STORAGE_EXCEPTION = 13;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_KEY_NOT_FOUND = 14;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_RETRY_UPDATE = 15;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_MATURITY_EXCEEDED = 16;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_NOT_PERMITTED_LAND = 17;
        [APILevel(APIFlags.LSL)]
        public const int XP_ERROR_REQUEST_PERM_TIMEOUT = 18;

        [APILevel(APIFlags.LSL)]
        public const int SIT_NOT_EXPERIENCE = -1;
        [APILevel(APIFlags.LSL)]
        public const int SIT_NO_EXPERIENCE_PERMISSION = -2;
        [APILevel(APIFlags.LSL)]
        public const int SIT_NO_SIT_TARGET = -3;
        [APILevel(APIFlags.LSL)]
        public const int SIT_INVALID_AGENT = -4;
        [APILevel(APIFlags.LSL)]
        public const int SIT_INVALID_LINK = -5;
        [APILevel(APIFlags.LSL)]
        public const int SIT_NO_ACCESS = -6;
        [APILevel(APIFlags.LSL)]
        public const int SIT_INVALID_OBJECT = -7;

        [APILevel(APIFlags.LSL, "experience_permissions")]
        [StateEventDelegate]
        public delegate void State_experience_permissions(LSLKey agent_id);

        [APILevel(APIFlags.LSL, "experience_permissions_denied")]
        [StateEventDelegate]
        public delegate void State_experience_permissions_denied(LSLKey agent_id, int reason);

        [APILevel(APIFlags.LSL, "llAgentInExperience")]
        public int AgentInExperience(ScriptInstance instance, LSLKey agent)
        {
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                ExperienceServiceInterface experienceService = scene.ExperienceService;
                if(experienceService == null)
                {
                    return 0;
                }
                UUID experienceid = instance.Item.ExperienceID;
                if(experienceid == UUID.Zero)
                {
                    return 0;
                }

                UGUI uui;
                return (scene.AvatarNameService.TryGetValue(agent.AsUUID, out uui) && 
                    experienceService.Permissions[experienceid, uui]).ToLSLBoolean();
            }
        }

        private static UUID SendExperienceError(ScriptInstance instance, int error)
        {
            var e = new DataserverEvent
            {
                QueryID = UUID.Random,
                Data = string.Format("0,{0}", error)
            };
            instance.Part.PostEvent(e, instance.Item.ExperienceID);
            return e.QueryID;
        }

        private static int CheckExperienceAllowed(ScriptInstance instance, ExperienceServiceInterface experienceService, UUID experienceid)
        {
            if (experienceid == UUID.Zero)
            {
                return XP_ERROR_NO_EXPERIENCE;
            }

            ExperienceInfo info;
            SceneInterface scene = instance.Part.ObjectGroup.Scene;
            if (!experienceService.TryGetValue(experienceid, out info))
            {
                return XP_ERROR_INVALID_EXPERIENCE;
            }

            if ((info.Properties & ExperiencePropertyFlags.Disabled) != 0)
            {
                return XP_ERROR_EXPERIENCE_DISABLED;
            }

            if ((info.Properties & ExperiencePropertyFlags.Suspended) != 0)
            {
                return XP_ERROR_EXPERIENCE_SUSPENDED;
            }

            if (info.Maturity > scene.GetRegionInfo().Access)
            {
                return XP_ERROR_MATURITY_EXCEEDED;
            }

            bool allowedbefore = false;

            EstateExperienceInfo estateExperience;
            if (scene.EstateService.Experiences.TryGetValue(scene.ParentEstateID, experienceid, out estateExperience))
            {
                if (!estateExperience.IsAllowed)
                {
                    return XP_ERROR_NOT_PERMITTED_LAND;
                }
                allowedbefore = true;
            }
            else if ((info.Properties & ExperiencePropertyFlags.Grid) == 0)
            {
                return XP_ERROR_NOT_PERMITTED_LAND;
            }

            RegionExperienceInfo reginfo;
            if (scene.RegionExperiences.TryGetValue(scene.ID, experienceid, out reginfo))
            {
                if (!reginfo.IsAllowed)
                {
                    return XP_ERROR_NOT_PERMITTED_LAND;
                }
                allowedbefore = true;
            }
            else if ((info.Properties & ExperiencePropertyFlags.Grid) == 0 && !allowedbefore)
            {
                return XP_ERROR_NOT_PERMITTED_LAND;
            }

            ParcelInfo pInfo;
            if (!scene.Parcels.TryGetValue(instance.Part.ObjectGroup.GlobalPosition, out pInfo))
            {
                return XP_ERROR_NOT_PERMITTED_LAND;
            }

            ParcelExperienceEntry parcelExperience;
            if (scene.Parcels.Experiences.TryGetValue(scene.ID, pInfo.ID, experienceid, out parcelExperience))
            {
                if (!parcelExperience.IsAllowed)
                {
                    return XP_ERROR_NOT_PERMITTED_LAND;
                }
            }
            else if ((info.Properties & ExperiencePropertyFlags.Grid) == 0 && !allowedbefore)
            {
                return XP_ERROR_NOT_PERMITTED_LAND;
            }

            return XP_ERROR_NONE;
        }

        private static UUID CheckExperienceStatus(ScriptInstance instance, ExperienceServiceInterface experienceService, UUID experienceid)
        {
            int result = CheckExperienceAllowed(instance, experienceService, experienceid);
            if(result != XP_ERROR_NONE)
            {
                return SendExperienceError(instance, result);
            }

            return UUID.Zero;
        }

        [APILevel(APIFlags.LSL, "llGetExperienceDetails")]
        public AnArray GetExperienceDetails(ScriptInstance instance, LSLKey experience_id)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                ExperienceServiceInterface experienceService = scene.ExperienceService;
                if (experienceService == null)
                {
                    return new AnArray();
                }
                UUID experienceid = experience_id.AsUUID;

                if (experienceid == UUID.Zero)
                {
                    experienceid = instance.Item.ExperienceID;
                }

                if(experienceid == UUID.Zero)
                {
                    return new AnArray();
                }

                ExperienceInfo experienceInfo;
                var res = new AnArray();
                if(experienceService.TryGetValue(experienceid, out experienceInfo))
                {
                    res.Add(experienceInfo.Name);
                    res.Add(experienceInfo.Owner.ID);
                    res.Add(experienceInfo.ID);
                    res.Add(0); /* state unclear */
                    res.Add(string.Empty); /* state message unclear */
                    res.Add(experienceInfo.Group.ID);
                }

                return res;
            }
        }

        [APILevel(APIFlags.LSL, "llGetExperienceErrorMessage")]
        public string GetExperienceErrorMessage(ScriptInstance instance, int error)
        {
            switch(error)
            {
                case XP_ERROR_NONE: return "no error";
                case XP_ERROR_THROTTLED: return "exceeded throttle";
                case XP_ERROR_EXPERIENCES_DISABLED: return "experiences are disabled";
                case XP_ERROR_INVALID_PARAMETERS: return "invalid parameters";
                case XP_ERROR_NOT_PERMITTED: return "operation not permitted";
                case XP_ERROR_NO_EXPERIENCE: return "script not associated with an experience";
                case XP_ERROR_NOT_FOUND: return "not found";
                case XP_ERROR_INVALID_EXPERIENCE: return "invalid experience";
                case XP_ERROR_EXPERIENCE_DISABLED: return "experience is disabled";
                case XP_ERROR_EXPERIENCE_SUSPENDED: return "experience is suspended";
                case XP_ERROR_UNKNOWN_ERROR: return "unknown error";
                case XP_ERROR_QUOTA_EXCEEDED: return "experience data quota exceeded";
                case XP_ERROR_STORE_DISABLED: return "key-value store is disabled";
                case XP_ERROR_STORAGE_EXCEPTION: return "key-value store communication failed";
                case XP_ERROR_KEY_NOT_FOUND: return "key doesn't exist";
                case XP_ERROR_RETRY_UPDATE: return "retry update";
                case XP_ERROR_MATURITY_EXCEEDED: return "experience content rating too high";
                default: return "unknown error code";
            }
        }

        [APILevel(APIFlags.LSL, "llRequestExperiencePermissions")]
        public void RequestExperiencePermissions(ScriptInstance instance, LSLKey agentID, string name /* unused */)
        {
            lock (instance)
            {
                IAgent a;
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                try
                {
                    a = scene.Agents[agentID];
                }
                catch
                {
                    instance.Item.PermsGranter = null;
                    return;
                }

                ExperienceServiceInterface experienceService = scene.ExperienceService;
                if(experienceService == null)
                {
                    instance.PostEvent(new ExperiencePermissionsDeniedEvent
                    {
                        AgentId = a.Owner,
                        Reason = XP_ERROR_EXPERIENCES_DISABLED
                    });
                    return;
                }

                UUID experienceid = instance.Item.ExperienceID;
                int reason = CheckExperienceAllowed(instance, experienceService, experienceid);
                if (reason != XP_ERROR_NONE)
                {
                    instance.PostEvent(new ExperiencePermissionsDeniedEvent
                    {
                        AgentId = a.Owner,
                        Reason = reason
                    });
                    return;
                }

                bool allowed;
                var perms = ScriptPermissions.ExperienceGrantedPermissions;
                if (!a.IsActiveGod && experienceService.Permissions.TryGetValue(experienceid, a.Owner, out allowed))
                {
                    if (allowed)
                    {
                        instance.PostEvent(new ExperiencePermissionsEvent
                        {
                            PermissionsKey = a.Owner
                        });
                        scene.SendExperienceLog(a, instance.Part.ObjectGroup, SceneInterface.ExperienceLogType.AutoAccept, experienceid);
                    }
                    else
                    {
                        instance.PostEvent(new ExperiencePermissionsDeniedEvent
                        {
                            AgentId = a.Owner,
                            Reason = XP_ERROR_NOT_PERMITTED
                        });
                    }
                }
                else
                {
                    perms = a.RequestPermissions(instance.Part, instance.Item.ID, perms, experienceid);
                    if (perms != ScriptPermissions.None)
                    {
                        instance.PostEvent(new ExperiencePermissionsEvent
                        {
                            PermissionsKey = a.Owner
                        });
                        scene.SendExperienceLog(a, instance.Part.ObjectGroup, SceneInterface.ExperienceLogType.AutoAccept, experienceid);
                    }
                }
            }
        }

        private const int LINK_THIS = -4;

        [APILevel(APIFlags.LSL, "llSitOnLink")]
        public int SitOnLink(ScriptInstance instance, LSLKey agent_id, int link)
        {
            /*
             * The avatar specified by agent_id is forced to sit on the sit target of the prim indicated by the link parameter. If the specified link is already occupied, the simulator searches down the chain of prims in the link set looking for an available sit target.
             * 
             * If successful, this method returns 1.
             * If the function fails, it returns a negative number constant.
             * Link constants that indicate a single prim may be used for the link parameter. These are LINK_ROOT and LINK_THIS. Other constants such as LINK_SET, LINK_CHILDREN, LINK_ALL_OTHERS will return an INVALID_LINK error.
             * This method must be called from an experience enabled script running on land that has enabled the experience key. If these conditions are not met this method returns a NOT_EXPERIENCE error.
             * The targeted avatar must also have accepted the experience. If the user is not participating in the experience this method returns NO_EXPERIENCE_PERMISSION. If the avatar id can not be found or is not over land that has enabled the experience this method returns INVALID_AGENT.
             * If there are no valid sit targets remaining in the linkset this method returns NO_SIT_TARGET and no action is taken with the avatar.
             * If the avatar does not have access to the parcel containing the prim running this script, this call fails. 
             */
            lock(instance)
            {
                ObjectGroup objgrp = instance.Part.ObjectGroup;
                SceneInterface scene = objgrp.Scene;
                ParcelInfo pInfo;
                IAgent agent;
                if (link == LINK_THIS)
                {
                    link = instance.Part.LinkNumber;
                }
                else if (!objgrp.ContainsKey(link))
                {
                    return SIT_INVALID_LINK;
                }

                if (objgrp.IsAttached)
                {
                    return SIT_INVALID_OBJECT;
                }
                else if (!scene.RootAgents.TryGetValue(agent_id.AsUUID, out agent))
                {
                    return SIT_INVALID_AGENT;
                }
                ExperienceServiceInterface experienceService = objgrp.Scene.ExperienceService;
                UUID experienceID = instance.Item.ExperienceID;
                string reason;
                if(experienceService == null || experienceID == UUID.Zero)
                {
                    return SIT_NOT_EXPERIENCE;
                }
                else if(!scene.Parcels.TryGetValue(objgrp.GlobalPosition, out pInfo))
                {
                    return SIT_INVALID_OBJECT;
                }
                else if(!scene.CheckParcelAccessRights(agent, pInfo, out reason))
                {
                    return SIT_NO_ACCESS;
                }
                else if(XP_ERROR_NONE != CheckExperienceAllowed(instance, experienceService, experienceID))
                {
                    return SIT_NOT_EXPERIENCE;
                }
                else if(!experienceService.Permissions[experienceID, agent.Owner])
                {
                    return SIT_NO_EXPERIENCE_PERMISSION;
                }
                else if(!objgrp.AgentSitting.Sit(agent, link, true))
                {
                    return SIT_NO_SIT_TARGET;
                }
                return 1;
            }
        }
    }
}
