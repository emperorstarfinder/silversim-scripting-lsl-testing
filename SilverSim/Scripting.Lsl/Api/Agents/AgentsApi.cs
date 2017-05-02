﻿// SilverSim is distributed under the terms of the
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
using SilverSim.Scene.Management.IM;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Groups;
using SilverSim.Types.IM;
using SilverSim.Types.Inventory;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    [ScriptApiName("Agents")]
    [LSLImplementation]
    [Description("LSL/OSSL Agents API")]
    public class AgentsApi : IScriptApi, IPlugin
    {
        List<IUserAgentServicePlugin> m_UserAgentServicePlugins = new List<IUserAgentServicePlugin>();
        public AgentsApi()
        {
            /* intentionally left empty */
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
            m_UserAgentServicePlugins = loader.GetServicesByValue<IUserAgentServicePlugin>();
        }

        [APILevel(APIFlags.LSL)]
        public const int AGENT_FLYING = 0x0001;
        [APILevel(APIFlags.LSL)]
        public const int AGENT_ATTACHMENTS = 0x0002;
        [APILevel(APIFlags.LSL)]
        public const int AGENT_SCRIPTED = 0x0004;
        [APILevel(APIFlags.LSL)]
        public const int AGENT_MOUSELOOK = 0x0008;
        [APILevel(APIFlags.LSL)]
        public const int AGENT_SITTING = 0x0010;
        [APILevel(APIFlags.LSL)]
        public const int AGENT_ON_OBJECT = 0x0020;
        [APILevel(APIFlags.LSL)]
        public const int AGENT_AWAY = 0x0040;
        [APILevel(APIFlags.LSL)]
        public const int AGENT_WALKING = 0x0080;
        [APILevel(APIFlags.LSL)]
        public const int AGENT_IN_AIR = 0x0100;
        [APILevel(APIFlags.LSL)]
        public const int AGENT_TYPING = 0x0200;
        [APILevel(APIFlags.LSL)]
        public const int AGENT_CROUCHING = 0x0400;
        [APILevel(APIFlags.LSL)]
        public const int AGENT_BUSY = 0x0800;
        [APILevel(APIFlags.LSL)]
        public const int AGENT_ALWAYS_RUN = 0x1000;
        [APILevel(APIFlags.LSL)]
        public const int AGENT_AUTOPILOT = 0x2000;

        [APILevel(APIFlags.LSL)]
        public const int AGENT_LIST_PARCEL = 1;
        [APILevel(APIFlags.LSL)]
        public const int AGENT_LIST_PARCEL_OWNER = 2;
        [APILevel(APIFlags.LSL)]
        public const int AGENT_LIST_REGION = 4;

        [APILevel(APIFlags.LSL, "llGetAgentList")]
        public AnArray GetAgentList(ScriptInstance instance, int scope, AnArray options)
        {
            AnArray res = new AnArray();
            lock (instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                if (scope == AGENT_LIST_PARCEL)
                {
                    ParcelInfo thisParcel;
                    if (scene.Parcels.TryGetValue(grp.GlobalPosition, out thisParcel))
                    {
                        foreach (IAgent agent in scene.RootAgents)
                        {
                            ParcelInfo pInfo;
                            if(scene.Parcels.TryGetValue(agent.GlobalPosition, out pInfo) &&
                                pInfo.ID == thisParcel.ID)
                            {
                                res.Add(agent.Owner.ID);
                            }
                        }
                    }
                }
                else
                {
                    foreach (IAgent agent in scene.RootAgents)
                    {
                        if (scope == AGENT_LIST_PARCEL_OWNER)
                        {
                            ParcelInfo pInfo;
                            if (scene.Parcels.TryGetValue(agent.GlobalPosition, out pInfo) ||
                                agent.Owner.EqualsGrid(pInfo.Owner))
                            {
                                res.Add(agent.Owner.ID);
                            }
                        }
                        else if (scope == AGENT_LIST_REGION)
                        {
                            res.Add(agent.Owner.ID);
                        }
                    }
                }
            }
            return res;
        }

        [APILevel(APIFlags.LSL, "llGetAgentInfo")]
        public int GetAgentInfo(ScriptInstance instance, LSLKey id)
        {
            int flags = 0;
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                IAgent agent;
                if(scene.Agents.TryGetValue(id.AsUUID, out agent))
                {
                    foreach(ObjectGroup att in agent.Attachments.All)
                    {
                        flags |= AGENT_ATTACHMENTS;
                        foreach(ObjectPart part in att.Values)
                        {
                            if(part.IsScripted)
                            {
                                flags |= AGENT_SCRIPTED;
                            }
                        }
                    }
                    if(agent.Attachments.Count != 0)
                    {
                        flags |= AGENT_ATTACHMENTS;
                    }

                    /* TODO: AGENT_AWAY, AGENT_BUSY, AGENT_CROUCHING, AGENT_FLYING, AGENT_IN_AIR, AGENT_TYPING, AGENT_WALING */

                    if(agent.SittingOnObject != null)
                    {
                        flags |= AGENT_ON_OBJECT | AGENT_SITTING;
                    }

                    if(agent.IsInMouselook)
                    {
                        flags |= AGENT_MOUSELOOK;
                    }
                }
            }
            return flags;
        }

        [APILevel(APIFlags.LSL)]
        public const int DATA_ONLINE = 1;
        [APILevel(APIFlags.LSL)]
        public const int DATA_NAME = 2;
        [APILevel(APIFlags.LSL)]
        public const int DATA_BORN = 3;
        [APILevel(APIFlags.LSL)]
        public const int DATA_RATING = 4;
        [APILevel(APIFlags.LSL)]
        public const int DATA_PAYINFO = 8;

        [APILevel(APIFlags.LSL)]
        public const int PAYMENT_INFO_ON_FILE = 0x1;
        [APILevel(APIFlags.LSL)]
        public const int PAYMENT_INFO_USED = 0x2;

        bool TryConnectUserAgent(string uri, out UserAgentServiceInterface uaservice)
        {
            uaservice = null;
            Dictionary<string, string> heloheaders = ServicePluginHelo.HeloRequest(uri);
            foreach(IUserAgentServicePlugin plugin in m_UserAgentServicePlugins)
            {
                if(plugin.IsProtocolSupported(uri, heloheaders))
                {
                    uaservice = plugin.Instantiate(uri);
                    return true;
                }
            }

            return false;
        }

        [APILevel(APIFlags.LSL, "llRequestAgentData")]
        [ForcedSleep(0.1)]
        public LSLKey RequestAgentData(ScriptInstance instance, LSLKey id, int data)
        {
            lock(instance)
            {
                IAgent agent;
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                DataserverEvent ev;
                UUID queryid = UUID.Random;
                UUI uui;

                if (scene.Agents.TryGetValue(id.AsUUID, out agent))
                {
                    switch(data)
                    {
                        case DATA_ONLINE:
                            ev = new DataserverEvent();
                            ev.QueryID = queryid;
                            ev.Data = "1";
                            instance.PostEvent(ev);
                            break;

                        case DATA_NAME:
                            ev = new DataserverEvent();
                            ev.QueryID = queryid;
                            ev.Data = agent.FirstName + " " + agent.LastName;
                            instance.PostEvent(ev);
                            break;

                        case DATA_BORN:
                            UserAgentServiceInterface uaservice = agent.UserAgentService;
                            if(null != uaservice)
                            {
                                UserAgentServiceInterface.UserInfo ui;
                                try
                                {
                                    ui = uaservice.GetUserInfo(agent.Owner);
                                }
                                catch
                                {
                                    return UUID.Zero;
                                }
                                ev = new DataserverEvent();
                                ev.QueryID = queryid;
                                ev.Data = ui.UserCreated.ToString("yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo);
                                instance.PostEvent(ev);
                            }
                            break;

                        case DATA_RATING:
                            ev = new DataserverEvent();
                            ev.QueryID = queryid;
                            ev.Data = "[0,0,0,0,0,0]";
                            instance.PostEvent(ev);
                            break;

                        case DATA_PAYINFO:
                            ev = new DataserverEvent();
                            ev.QueryID = queryid;
                            ev.Data = "0";
                            instance.PostEvent(ev);
                            break;

                        default:
                            break;
                    }

                    return queryid;
                }
                else if(scene.AvatarNameService.TryGetValue(id.AsUUID, out uui))
                {
                    UserAgentServiceInterface uaservice;
                    if(uui.HomeURI == null || !TryConnectUserAgent(uui.HomeURI.ToString(), out uaservice))
                    {
                        return UUID.Zero;
                    }
                    else
                    {
                        switch (data)
                        {
                            case DATA_ONLINE:
                                ev = new DataserverEvent();
                                ev.QueryID = queryid;
                                ev.Data = uaservice.IsOnline(uui) ? "1" : "0";
                                instance.PostEvent(ev);
                                break;

                            case DATA_NAME:
                                ev = new DataserverEvent();
                                ev.QueryID = queryid;
                                ev.Data = uui.FullName;
                                instance.PostEvent(ev);
                                break;

                            case DATA_BORN:
                                UserAgentServiceInterface.UserInfo ui;
                                try
                                {
                                    ui = uaservice.GetUserInfo(agent.Owner);
                                }
                                catch
                                {
                                    return UUID.Zero;
                                }
                                ev = new DataserverEvent();
                                ev.QueryID = queryid;
                                ev.Data = ui.UserCreated.ToString("yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo);
                                instance.PostEvent(ev);
                                break;

                            case DATA_RATING:
                                ev = new DataserverEvent();
                                ev.QueryID = queryid;
                                ev.Data = "[0,0,0,0,0,0]";
                                instance.PostEvent(ev);
                                break;

                            case DATA_PAYINFO:
                                ev = new DataserverEvent();
                                ev.QueryID = queryid;
                                ev.Data = "0";
                                instance.PostEvent(ev);
                                break;

                            default:
                                break;
                        }
                        return queryid;
                    }
                }
            }

            return UUID.Zero;
        }

        [APILevel(APIFlags.OSSL, "osSetSpeed")]
        public void SetSpeed(ScriptInstance instance, LSLKey id, double speedfactor)
        {
            throw new NotImplementedException("osSetSpeed(key, float)");
        }

        [APILevel(APIFlags.OSSL, "osInviteToGroup")]
        public int OsInviteToGroup(ScriptInstance instance, LSLKey id)
        {
            lock(instance)
            {
                ObjectPart part = instance.Part;
                ObjectGroup grp = part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                GroupsServiceInterface groupsService = scene.GroupsService;
                IAgent agent;
                UGI group = grp.Group;
                UUI owner = grp.Owner;
                if(scene.Agents.TryGetValue(id.AsUUID, out agent) &&
                    group != UGI.Unknown && 
                    (groupsService.GetAgentPowers(group, owner) & GroupPowers.Invite) == GroupPowers.Invite)
                {
                    GroupInvite invite = new GroupInvite();
                    invite.Principal = agent.Owner;
                    invite.RoleID = UUID.Zero;
                    invite.Group = group;
                    invite.ID = UUID.Random;

                    try
                    {
                        groupsService.Invites.Add(grp.Owner, invite);
                    }
                    catch
                    {
                        return 0;
                    }

                    GridInstantMessage gim = new GridInstantMessage();
                    gim.FromGroup = group;
                    gim.FromAgent = owner;
                    gim.Message = string.Format(this.GetLanguageString(agent.CurrentCulture, "osGroupInviteMessage", "{0} has invited you to join a group called {1}. There is no cost to join this group."),
                        owner.FullName, group.GroupName);
                    gim.IsFromGroup = true;
                    gim.RegionID = scene.ID;
                    gim.BinaryBucket = new byte[20];
                    gim.IMSessionID = invite.ID;

                    return agent.IMSend(gim) ? 1 : 0;
                }
                else
                {
                    return 0;
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osEjectFromGroup")]
        public int OsEjectFromToGroup(ScriptInstance instance, LSLKey id)
        {
            lock(instance)
            {
                ObjectPart part = instance.Part;
                ObjectGroup grp = part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                GroupsServiceInterface groupsService = scene.GroupsService;
                UGI group = grp.Group;
                UUI owner = grp.Owner;
                UUI ejectee;
                if (group != UGI.Unknown &&
                    (groupsService.GetAgentPowers(group, owner) & GroupPowers.Eject) == GroupPowers.Eject &&
                    scene.AvatarNameService.TryGetValue(id.AsUUID, out ejectee))
                {
                    try
                    {
                        groupsService.Members.Delete(owner, group, ejectee);
                    }
                    catch
                    {
                        return 0;
                    }

                    IAgent agent;
                    GridInstantMessage gim = new GridInstantMessage();
                    if (scene.Agents.TryGetValue(ejectee.ID, out agent))
                    {
                        gim.Dialog = GridInstantMessageDialog.MessageFromAgent;
                    }
                    else
                    {
                        gim.Dialog = GridInstantMessageDialog.EjectedFromGroup;
                    }

                    gim.IMSessionID = group.ID;
                    gim.FromAgent = owner;
                    gim.FromGroup = group;
                    gim.RegionID = scene.ID;
                    gim.Message = string.Format("You have been ejected from '{1}' by {0}.", owner.FullName, group.GroupName);
                    gim.OnResult = delegate (GridInstantMessage g, bool result) { };
                    IMRouter router = scene.GetService<IMRouter>();
                    router.SendWithResultDelegate(gim);

                    gim = new GridInstantMessage();
                    gim.IMSessionID = UUID.Zero;
                    gim.FromAgent = owner;
                    gim.FromGroup = group;
                    gim.IsFromGroup = true;
                    gim.Message = string.Format("{2} has been ejected from '{1}' by {0}.", ejectee.FullName, group.GroupName, owner.FullName);
                    gim.Dialog = GridInstantMessageDialog.MessageFromAgent;
                    gim.RegionID = scene.ID;
                    gim.OnResult = delegate (GridInstantMessage g, bool result) { };
                    router.SendWithResultDelegate(gim);

                    return 1;
                }

                return 0;
            }
        }

        [APILevel(APIFlags.LSL, "llRequestDisplayName")]
        public LSLKey RequestDisplayName(ScriptInstance instance, LSLKey id)
        {
            lock (instance)
            {
                IAgent agent;
                UUI agentid;
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.Agents.TryGetValue(id.AsUUID, out agent))
                {
                    string displayname;
                    if(!agent.UserAgentService.DisplayName.TryGetValue(agent.Owner, out displayname))
                    {
                        displayname = agent.Owner.FullName;
                    }
                    DataserverEvent ev = new DataserverEvent();
                    ev.QueryID = UUID.Random;
                    ev.Data = displayname;
                    instance.PostEvent(ev);
                    return ev.QueryID;
                }
                else if(scene.AvatarNameService.TryGetValue(id.AsUUID, out agentid) && agentid.HomeURI != null)
                {
                    UserAgentServiceInterface userAgentService = null;
                    Dictionary<string, string> heloheaders = ServicePluginHelo.HeloRequest(agentid.HomeURI.ToString());
                    foreach(IUserAgentServicePlugin plugin in m_UserAgentServicePlugins)
                    {
                        if(plugin.IsProtocolSupported(agentid.HomeURI.ToString(), heloheaders))
                        {
                            userAgentService = plugin.Instantiate(agentid.HomeURI.ToString());
                            break;
                        }
                    }
                    if(userAgentService != null)
                    {
                        string displayname;
                        if(!userAgentService.DisplayName.TryGetValue(agentid, out displayname))
                        {
                            displayname = agentid.FullName;
                        }
                        DataserverEvent ev = new DataserverEvent();
                        ev.QueryID = UUID.Random;
                        ev.Data = displayname;
                        instance.PostEvent(ev);
                        return ev.QueryID;
                    }
                }
                return UUID.Zero;
            }
        }

        [APILevel(APIFlags.LSL, "llGetUsername")]
        public string GetUsername(ScriptInstance instance, LSLKey id)
        {
            /* only when child or root agent is in sim */
            lock(instance)
            {
                IAgent agent;
                if(instance.Part.ObjectGroup.Scene.Agents.TryGetValue(id.AsUUID, out agent))
                {
                    return agent.Owner.FullName.Replace(' ', '.');
                }
                return string.Empty;
            }
        }

        [APILevel(APIFlags.LSL, "llRequestUsername")]
        public LSLKey RequestUsername(ScriptInstance instance, LSLKey id)
        {
            lock(instance)
            {
                IAgent agent;
                UUI uui;
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if(scene.Agents.TryGetValue(id.AsUUID, out agent))
                {
                    DataserverEvent ev = new DataserverEvent();
                    UUID queryid = UUID.Random;
                    ev.QueryID = queryid;
                    ev.Data = agent.Owner.FullName;
                    return queryid;
                }
                else if(scene.AvatarNameService.TryGetValue(id.AsUUID, out uui))
                {
                    DataserverEvent ev = new DataserverEvent();
                    UUID queryid = UUID.Random;
                    ev.QueryID = queryid;
                    ev.Data = uui.FullName;
                    return queryid;
                }

                return UUID.Zero;
            }
        }

        [APILevel(APIFlags.LSL, "llGetDisplayName")]
        public string GetDisplayName(ScriptInstance instance, LSLKey id)
        {
            lock (instance)
            {
                IAgent agent;
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.Agents.TryGetValue(id.AsUUID, out agent))
                {
                    string displayname;
                    if (!agent.UserAgentService.DisplayName.TryGetValue(agent.Owner, out displayname))
                    {
                        displayname = agent.Owner.FullName;
                    }
                    return displayname;
                }
                return string.Empty;
            }
        }

        [APILevel(APIFlags.LSL, "llKey2Name")]
        public string Key2Name(ScriptInstance instance, LSLKey id)
        {
            lock(instance)
            {
                IObject obj;
                if(instance.Part.ObjectGroup.Scene.Objects.TryGetValue(id, out obj))
                {
                    return obj.Name;
                }
            }
            return string.Empty;
        }

        [APILevel(APIFlags.LSL, "osKey2Name")]
        public string OsKey2Name(ScriptInstance instance, LSLKey id)
        {
            lock (instance)
            {
                UUI uui;
                if (instance.Part.ObjectGroup.Scene.AvatarNameService.TryGetValue(id, out uui))
                {
                    return uui.FullName;
                }
            }
            return string.Empty;
        }

        [APILevel(APIFlags.ASSL, "asGetAppearanceParams")]
        [APIExtension(APIExtension.InWorldz, "iwGetAppearanceParams")]
        public int GetAppearanceParams(ScriptInstance instance, LSLKey id, int which)
        {
            lock (instance)
            {
                IAgent agent;
                if (!instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(id, out agent))
                {
                    byte[] vp = agent.VisualParams;
                    if(which == -1)
                    {
                        return vp.Length;
                    }
                    if (vp.Length > which)
                    {
                        return vp[which];
                    }
                }
                return -1;
            }
        }

        [APILevel(APIFlags.LSL, "osGetGender")]
        public string OsGetGender(ScriptInstance instance, LSLKey id)
        {
            lock (instance)
            {
                IAgent agent;
                if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(id, out agent))
                {
                    byte[] vp = agent.VisualParams;
                    if(vp.Length > 31)
                    {
                        return vp[31] > 128 ? "male" : "female";
                    }
                }
                return "unknown";
            }
        }

        [APILevel(APIFlags.OSSL, "osGetHealth")]
        public double GetHealth(ScriptInstance instance, LSLKey id)
        {
            lock(instance)
            {
                IAgent agent;
                if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(id, out agent))
                {
                    return agent.Health;
                }
            }
            return -1;
        }

        [APILevel(APIFlags.OSSL, "osCauseDamage")]
        public void CauseDamage(ScriptInstance instance, LSLKey id, double damage)
        {
            if(damage < 0)
            {
                return;
            }
            lock(instance)
            {
                IAgent agent;
                ObjectPart part = instance.Part;
                SceneInterface scene = part.ObjectGroup.Scene;
                if(scene.RootAgents.TryGetValue(id, out agent))
                {
                    ParcelInfo agentParcel;
                    ParcelInfo objectParcel;
                    if(scene.Parcels.TryGetValue(agent.GlobalPosition, out agentParcel) &&
                        scene.Parcels.TryGetValue(part.GlobalPosition, out objectParcel) &&
                        (agentParcel.Flags & ParcelFlags.AllowDamage) != 0 &&
                        (objectParcel.Flags & ParcelFlags.AllowDamage) != 0)
                    {
                        /* only allow damage to avatars when they are actually killed on allow damage land by object on allow damage land */
                        agent.DecreaseHealth(damage);
                    }
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osCauseHealing")]
        public void CauseHealing(ScriptInstance instance, LSLKey id, double damage)
        {
            if(damage < 0)
            {
                return;
            }
            lock (instance)
            {
                IAgent agent;
                ObjectPart part = instance.Part;
                SceneInterface scene = part.ObjectGroup.Scene;
                if (scene.RootAgents.TryGetValue(id, out agent))
                {
                    /* allow healing from any parcel */
                    agent.IncreaseHealth(damage);
                }
            }
        }

        [APILevel(APIFlags.LSL, "osAvatarName2Key")]
        public LSLKey OsAvatarName2Key(ScriptInstance instance, string firstName, string lastName)
        {
            lock(instance)
            {
                UUI uui;
                if(instance.Part.ObjectGroup.Scene.AvatarNameService.TryGetValue(firstName, lastName, out uui))
                {
                    return uui.ID;
                }
                return string.Empty;
            }
        }

        [APILevel(APIFlags.LSL, "llGetAgentSize")]
        public Vector3 GetAgentSize(ScriptInstance instance, LSLKey id)
        {
            lock (instance)
            {
                IAgent agent;
                if(!instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(id, out agent))
                {
                    return Vector3.Zero;
                }
                return agent.Size;
            }
        }

        public LSLKey SaveAppearance(ScriptInstance instance, UUID agentId, string notecard)
        {
            IAgent agent;
            ObjectPart part = instance.Part;
            SceneInterface scene = part.ObjectGroup.Scene;
            if (scene.RootAgents.TryGetValue(agentId.AsUUID, out agent))
            {
                Notecard nc = (Notecard)agent.Appearance;
                AssetData asset = nc.Asset();
                asset.Name = "Saved Appearance";
                asset.ID = UUID.Random;
                scene.AssetService.Store(asset);

                ObjectPartInventoryItem item = new ObjectPartInventoryItem();
                item.AssetID = asset.ID;
                item.AssetType = AssetType.Notecard;
                item.Creator = part.Owner;
                item.ParentFolderID = part.ID;
                item.InventoryType = InventoryType.Notecard;
                item.LastOwner = part.Owner;
                item.Permissions.Base = InventoryPermissionsMask.Every;
                item.Permissions.Current = InventoryPermissionsMask.Every;
                item.Permissions.Group = InventoryPermissionsMask.None;
                item.Permissions.NextOwner = InventoryPermissionsMask.All;
                item.Permissions.EveryOne = InventoryPermissionsMask.None;

                item.Name = notecard;
                item.Description = "Saved Appearance";
                part.Inventory.Add(item);
                return asset.ID;
            }
            return UUID.Zero;
        }

        [APILevel(APIFlags.LSL, "osAgentSaveAppearance")]
        [ThreatLevelUsed]
        public LSLKey AgentSaveAppearance(ScriptInstance instance, LSLKey agentId, string notecard)
        {
            lock (instance)
            {
                ((Script)instance).CheckThreatLevel("osAgentSaveAppearance", Script.ThreatLevelType.VeryHigh);
                return SaveAppearance(instance, agentId.AsUUID, notecard);
            }
        }

        [APILevel(APIFlags.LSL, "osOwnerSaveAppearance")]
        [ThreatLevelUsed]
        public LSLKey OwnerSaveAppearance(ScriptInstance instance, string notecard)
        {
            lock (instance)
            {
                ((Script)instance).CheckThreatLevel("osOwnerSaveAppearance", Script.ThreatLevelType.High);
                return SaveAppearance(instance, instance.Part.Owner.ID, notecard);
            }
        }

        [APILevel(APIFlags.OSSL, "osKickAvatar")]
        public void KickAvatar(ScriptInstance instance, string firstName, string lastName, string alert)
        {
            lock(instance)
            {
                foreach(IAgent agent in instance.Part.ObjectGroup.Scene.RootAgents)
                {
                    if(agent.Owner.FullName == firstName + " " + lastName)
                    {
                        agent.KickUser(alert);
                        break;
                    }
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osForceOtherSit")]
        [ThreatLevelUsed]
        public void ForceOtherSit(ScriptInstance instance, LSLKey avatar)
        {
            lock(instance)
            {
                ((Script)instance).CheckThreatLevel("osForceOtherSit", Script.ThreatLevelType.VeryHigh);
            }
            throw new NotImplementedException("osForceOtherSit(key)");
        }

        [APILevel(APIFlags.OSSL, "osForceOtherSit")]
        [ThreatLevelUsed]
        public void ForceOtherSit(ScriptInstance instance, LSLKey avatar, LSLKey target)
        {
            lock (instance)
            {
                ((Script)instance).CheckThreatLevel("osForceOtherSit", Script.ThreatLevelType.VeryHigh);
            }
            throw new NotImplementedException("osForceOtherSit(key, key)");
        }

        [APILevel(APIFlags.LSL, "llGetAgentLanguage")]
        public string GetAgentLanguage(ScriptInstance instance, LSLKey avatar)
        {
            /* Details from LSL wiki
             *
             * If the user has "Share language with objects" disabled then this function returns an empty string.
             * During a 1-5 seconds period after which an agent is logging in, this function will return an empty string as well, until the viewer sends the data to the simulator.             
             * Users may prefer to see the client interface in a language that is not their native language, and some may prefer to use objects in the native language of the creator, or dislike low-quality translations. Consider providing a manual language override when it is appropriate. 
             * New language/variant values may be added later. Scripts may need to be prepared for unexpected values.
             * If the viewer is set to "System Default" the possible return may be outside the list given above. see List of ISO 639-1 codes for reference.
             * Viewers can specify other arbitrary language strings with the 'InstallLanguage' debug setting. For example, launching the viewer with "--set InstallLanguage american" results this function returning 'american' for the avatar. VWR-12222
             *   If the viewer supplies a multiline value, the simulator will only accept the first line and ignore all others. SVC-5503
             * 
             * Technically, viewer uses UpdateAgentLanguage to tell the simulator the language of the agent so that llGetAgentLanguage can do its
             * job if allowed by the viewer.
             */
            lock(instance)
            {
                IAgent agent;
                if(instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(avatar.AsUUID, out agent))
                {
                    return agent.AgentLanguage;
                }
                return string.Empty;
            }
        }

        #region osGetAvatarList
        [APILevel(APIFlags.OSSL, "osGetAvatarList")]
        public AnArray GetAvatarList(ScriptInstance instance)
        {
            AnArray res = new AnArray();

            lock (instance)
            {
                SceneInterface thisScene = instance.Part.ObjectGroup.Scene;
                UUID ownerID = thisScene.Owner.ID;
                foreach (IAgent agent in thisScene.Agents)
                {
                    if (agent.ID == ownerID)
                    {
                        continue;
                    }
                    res.Add(new LSLKey(agent.ID));
                    res.Add(agent.GlobalPosition);
                    res.Add(agent.Name);
                }
            }
            return res;
        }
        #endregion

        #region osGetAgents
        [APILevel(APIFlags.OSSL, "osGetAgents")]
        public AnArray GetAgents(ScriptInstance instance)
        {
            AnArray res = new AnArray();

            lock (instance)
            {
                foreach (IAgent agent in instance.Part.ObjectGroup.Scene.Agents)
                {
                    res.Add(agent.Name);
                }
            }
            return res;
        }
        #endregion

        [APILevel(APIFlags.OSSL, "osGetAgentIP")]
        [ThreatLevelUsed]
        public string GetAgentIP(ScriptInstance instance, LSLKey key)
        {
            lock(instance)
            {
                ((Script)instance).CheckThreatLevel("osGetAgentIP", Script.ThreatLevelType.High);

                IAgent agent;
                if(instance.Part.ObjectGroup.Scene.Agents.TryGetValue(key.AsUUID, out agent))
                {
                    return agent.Client.ClientIP;
                }
                return string.Empty;
            }
        }

        [APILevel(APIFlags.OSSL, "osGetAvatarHomeURI")]
        [ThreatLevelUsed]
        public string GetAvatarHomeURI(ScriptInstance instance, LSLKey key)
        {
            lock (instance)
            {
                ((Script)instance).CheckThreatLevel("osGetAvatarHomeURI", Script.ThreatLevelType.Low);

                IAgent agent;
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (scene.Agents.TryGetValue(key.AsUUID, out agent))
                {
                    if (agent.Owner.HomeURI != null)
                    {
                        return agent.Owner.HomeURI.ToString();
                    }
                    GridInfoServiceInterface gridInfoService = scene.GetService<GridInfoServiceInterface>();
                    if(null != gridInfoService)
                    {
                        return gridInfoService.HomeURI;
                    }
                }
                return string.Empty;
            }
        }
    }
}