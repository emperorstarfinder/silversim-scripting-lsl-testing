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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Grid;
using SilverSim.Types.Groups;
using SilverSim.Types.Parcel;
using SilverSim.Types.Script;
using System;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Teleport
{
    [ScriptApiName("Teleport")]
    [LSLImplementation]
    [Description("LSL/OSSL Teleport API")]
    public class TeleportApi : IScriptApi, IPlugin
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        private void TeleportAgentViaLandmark(ScriptInstance instance, IAgent agent, string landmark, Vector3 lookAt)
        {
            /* instance already locked */
            Landmark landmarkData;
            AssetData assetData;
            UUID landmarkID = instance.GetLandmarkAssetID(landmark);
            SceneInterface scene = instance.Part.ObjectGroup.Scene;
            if(scene.AssetService.TryGetValue(landmarkID, out assetData))
            {
                landmarkData = new Landmark(assetData);
                if(landmarkData.GatekeeperURI == scene.GatekeeperURI)
                {
                    /* same grid */
                    if (landmarkData.Location.GridX == scene.GridPosition.GridX &&
                        landmarkData.Location.GridY == scene.GridPosition.GridY)
                    {
                        /* same region, skip the teleport protocol */
                        TeleportAgentInRegion(instance, agent, landmarkData.LocalPos, lookAt);
                    }
                    else
                    {
                        /* other region */
                        if (agent.SittingOnObject != null && !agent.UnSit())
                        {
                            return;
                        }

                        if (!agent.TeleportTo(scene, landmarkData.RegionID, landmarkData.LocalPos, lookAt, TeleportFlags.ViaLandmark))
                        {
                            agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "LandmarkDestinationNotFound", "Landmark destination not found"), scene.ID);
                        }
                    }
                }
                else
                {
                    /* other grid */
                    if (agent.SittingOnObject != null && !agent.UnSit())
                    {
                        return;
                    }

                    if (!agent.TeleportTo(scene, landmarkData.GatekeeperURI, landmarkData.RegionID, landmarkData.LocalPos, lookAt, TeleportFlags.ViaLandmark))
                    {
                        agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "LandmarkDestinationNotFound", "Landmark destination not found"), scene.ID);
                    }
                }
            }
            else
            {
                instance.ShoutError(new LocalizedScriptMessage(this, "LandmarkAssetNotFound", "Landmark asset not found"));
            }
        }

        private void TeleportAgentViaGlobalCoords(ScriptInstance instance, IAgent agent, GridVector location, Vector3 position, Vector3 lookAt)
        {
            /* instance already locked */
            SceneInterface scene = instance.Part.ObjectGroup.Scene;
            if (location.GridX == scene.GridPosition.GridX &&
                location.GridY == scene.GridPosition.GridY)
            {
                /* same region, skip the teleport protocol */
                TeleportAgentInRegion(instance, agent, position, lookAt);
            }
            else
            {
                if (agent.SittingOnObject != null && !agent.UnSit())
                {
                    return;
                }
                if (!agent.TeleportTo(scene, location, position, lookAt, TeleportFlags.ViaLocation))
                {
                    agent.SendAlertMessage(string.Format(this.GetLanguageString(agent.CurrentCulture, "TeleportLocation0NotFound", "Location '{0}' not found."), location.ToString()), scene.ID);
                }
            }
        }

        private void TeleportAgentViaRegionName(ScriptInstance instance, IAgent agent, string regionName, Vector3 position, Vector3 lookAt)
        {
            /* instance already locked */
            SceneInterface scene = instance.Part.ObjectGroup.Scene;
            if (regionName.ToLower() == scene.Name || regionName.Length == 0)
            {
                /* same region, skip the teleport protocol */
                TeleportAgentInRegion(instance, agent, position, lookAt);
            }
            else
            {
                if (agent.SittingOnObject != null && !agent.UnSit())
                {
                    return;
                }
                if (!agent.TeleportTo(scene, regionName, position, lookAt, TeleportFlags.ViaRegionID))
                {
                    agent.SendAlertMessage(string.Format(this.GetLanguageString(agent.CurrentCulture, "TeleportRegion0NotFound", "Region '{0}' not found."), regionName), scene.ID);
                }
            }
        }

        private void TeleportAgentInRegion(ScriptInstance instance, IAgent agent, Vector3 position, Vector3 lookAt)
        {
            /* instance already locked */

            /* Remarks: teleporting in same region does not need teleport protocol */
            SceneInterface scene = instance.Part.ObjectGroup.Scene;
            if(position.X >= scene.SizeX || position.Y >= scene.SizeY || position.X < 0 || position.Y < 0)
            {
                /* refuse teleport, out of region */
                return;
            }
            if(agent.SittingOnObject != null && !agent.UnSit())
            {
                return;
            }

            double minPos = scene.Terrain[position] + (agent.Appearance.AvatarHeight / 2);
            if(position.Z < minPos)
            {
                position.Z = minPos;
            }
            /* we do not trigger an actual TP here, we simply catapult the avatar to its target position */
            agent.GlobalPosition = position;
            agent.LookAt = lookAt;
        }

        [APILevel(APIFlags.LSL, "llTeleportAgentHome")]
        /* we leave out the ForcedSleep(5.0) here */
        public void TeleportAgentHome(ScriptInstance instance, LSLKey avatar)
        {
            lock (instance)
            {
                IAgent agent;
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                ParcelInfo agentParcel;
                ParcelInfo objectParcel;
                if (scene.RootAgents.TryGetValue(avatar, out agent) &&
                    scene.Parcels.TryGetValue(agent.GlobalPosition, out agentParcel) &&
                    scene.Parcels.TryGetValue(grp.GlobalPosition, out objectParcel))
                {
                    if(agentParcel.ID != objectParcel.ID)
                    {
                        return;
                    }

                    if(!objectParcel.Owner.EqualsGrid(grp.Owner) &&
                        (!objectParcel.GroupOwned || !scene.HasGroupPower(grp.Owner, objectParcel.Group, GroupPowers.LandEjectAndFreeze)))
                    {
                        return;
                    }
                    if(!agent.TeleportHome(scene))
                    {
                        agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "TeleportHomeFailed", "Teleport home failed"), scene.ID);
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llTeleportAgentGlobalCoords")]
        public void TeleportAgentGlobalCoords(ScriptInstance instance, LSLKey avatar, Vector3 globalCoordinates, Vector3 regionCoordinates, Vector3 lookAt)
        {
            lock(instance)
            {
                IAgent agent;
                if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(avatar, out agent))
                {
                    ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                    if (agent.SittingOnObject != null)
                    {
                        /* refuse sitting avatars */
                        instance.ShoutError(new LocalizedScriptMessage(this, "SittingAvatarsCannotBeTeleported", "Sitting avatars cannot be teleported"));
                        return;
                    }
                    else if (!agent.Owner.EqualsGrid(grantinfo.PermsGranter) ||
                        (grantinfo.PermsMask & ScriptPermissions.Teleport) == 0)
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "TeleportPermissionIsNotGrantedByAvatar", "Teleport permission is not granted by avatar"));
                        return;
                    }

                    TeleportAgentViaGlobalCoords(instance, agent, new GridVector()
                    {
                        GridX = (ushort)globalCoordinates.X.Clamp(0, 65535),
                        GridY = (ushort)globalCoordinates.Y.Clamp(0, 65535)
                    }, regionCoordinates, lookAt);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llTeleportAgent")]
        public void TeleportAgentLandmark(ScriptInstance instance, LSLKey avatar, string landmark, Vector3 position, Vector3 lookAt)
        {
            lock(instance)
            {
                IAgent agent;
                if(instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(avatar, out agent))
                {
                    ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                    if (agent.SittingOnObject != null)
                    {
                        /* refuse sitting avatars */
                        instance.ShoutError(new LocalizedScriptMessage(this, "SittingAvatarsCannotBeTeleported", "Sitting avatars cannot be teleported"));
                        return;
                    }
                    else if(!agent.Owner.EqualsGrid(grantinfo.PermsGranter) ||
                        (grantinfo.PermsMask & ScriptPermissions.Teleport) == 0)
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "TeleportPermissionIsNotGrantedByAvatar", "Teleport permission is not granted by avatar"));
                        return;
                    }

                    if (landmark.Length == 0)
                    {
                        TeleportAgentInRegion(instance, agent, position, lookAt);
                    }
                    else
                    {
                        TeleportAgentViaLandmark(instance, agent, landmark, lookAt);
                    }
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osTeleportAgent")]
        [ThreatLevelRequired(ThreatLevel.VeryHigh)]
        public void TeleportAgent(ScriptInstance instance, LSLKey avatar, int regionX, int regionY, Vector3 position, Vector3 lookAt)
        {
            lock (instance)
            {
                IAgent agent;
                if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(avatar, out agent))
                {
                    ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                    if (agent.SittingOnObject != null)
                    {
                        /* refuse sitting avatars */
                        instance.ShoutError(new LocalizedScriptMessage(this, "SittingAvatarsCannotBeTeleported", "Sitting avatars cannot be teleported"));
                        return;
                    }
                    else if (!agent.Owner.EqualsGrid(grantinfo.PermsGranter) ||
                        (grantinfo.PermsMask & ScriptPermissions.Teleport) == 0)
                    {
                        instance.ShoutError(new LocalizedScriptMessage(this, "TeleportPermissionIsNotGrantedByAvatar", "Teleport permission is not granted by avatar"));
                        return;
                    }

                    TeleportAgentViaGlobalCoords(instance, agent, new GridVector()
                    {
                        GridX = (ushort)regionX,
                        GridY = (ushort)regionY
                    }, position, lookAt);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osTeleportAgent")]
        [ThreatLevelRequired(ThreatLevel.VeryHigh)]
        public void TeleportAgent(ScriptInstance instance, LSLKey avatar, string regionName, Vector3 position, Vector3 lookAt)
        {
            lock (instance)
            {
                IAgent agent;
                if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(avatar, out agent))
                {
                    TeleportAgentViaRegionName(instance, agent, regionName, position, lookAt);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osTeleportAgentLandmark")]
        [ThreatLevelRequired(ThreatLevel.VeryHigh)]
        public void OsTeleportAgentLandmark(ScriptInstance instance, LSLKey avatar, string landmark, Vector3 lookAt)
        {
            lock (instance)
            {
                IAgent agent;
                if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(avatar, out agent))
                {
                    TeleportAgentViaLandmark(instance, agent, landmark, lookAt);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osTeleportAgent")]
        [ThreatLevelRequired(ThreatLevel.VeryHigh)]
        public void TeleportAgent(ScriptInstance instance, LSLKey avatar, Vector3 position, Vector3 lookAt)
        {
            lock (instance)
            {
                IAgent agent;
                if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(avatar, out agent))
                {
                    TeleportAgentInRegion(instance, agent, position, lookAt);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osTeleportOwner")]
        public void TeleportOwner(ScriptInstance instance, int regionX, int regionY, Vector3 position, Vector3 lookAt)
        {
            lock(instance)
            {
                IAgent agent;
                ObjectPart part = instance.Part;
                if(part.ObjectGroup.Scene.RootAgents.TryGetValue(part.Owner.ID, out agent))
                {
                    if(!agent.Owner.EqualsGrid(part.Owner))
                    {
                        return;
                    }

                    TeleportAgentViaGlobalCoords(instance, agent, new GridVector()
                    {
                        X = (ushort)regionX.Clamp(0, 65535),
                        Y = (ushort)regionY.Clamp(0, 65535)
                    }, position, lookAt);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osTeleportOwner")]
        public void TeleportOwner(ScriptInstance instance, string regionName, Vector3 position, Vector3 lookAt)
        {
            lock (instance)
            {
                IAgent agent;
                ObjectPart part = instance.Part;
                if (part.ObjectGroup.Scene.RootAgents.TryGetValue(part.Owner.ID, out agent))
                {
                    if (!agent.Owner.EqualsGrid(part.Owner))
                    {
                        return;
                    }

                    TeleportAgentViaRegionName(instance, agent, regionName, position, lookAt);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osTeleportOwnerLandmark")]
        public void TeleportOwnerLandmark(ScriptInstance instance, string landmark, Vector3 lookAt)
        {
            lock (instance)
            {
                IAgent agent;
                ObjectPart part = instance.Part;
                if (part.ObjectGroup.Scene.RootAgents.TryGetValue(part.Owner.ID, out agent))
                {
                    if (!agent.Owner.EqualsGrid(part.Owner))
                    {
                        return;
                    }

                    TeleportAgentViaLandmark(instance, agent, landmark, lookAt);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osTeleportOwner")]
        public void TeleportOwner(ScriptInstance instance, Vector3 position, Vector3 lookAt)
        {
            lock (instance)
            {
                IAgent agent;
                ObjectPart part = instance.Part;
                if (part.ObjectGroup.Scene.RootAgents.TryGetValue(part.Owner.ID, out agent))
                {
                    if (!agent.Owner.EqualsGrid(part.Owner))
                    {
                        return;
                    }

                    TeleportAgentInRegion(instance, agent, position, lookAt);
                }
            }
        }
    }
}
