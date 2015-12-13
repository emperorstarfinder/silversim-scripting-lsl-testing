// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

namespace SilverSim.Scripting.Lsl.Api.Teleport
{
    [ScriptApiName("Teleport")]
    [LSLImplementation]
    public class TeleportApi : IScriptApi, IPlugin
    {
        public TeleportApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        void TeleportAgentViaLandmark(ScriptInstance instance, IAgent agent, string landmark, Vector3 lookAt)
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
                    if (landmarkData.Location.GridX == scene.RegionData.Location.GridX &&
                        landmarkData.Location.GridY == scene.RegionData.Location.GridY)
                    {
                        /* same region, skip the teleport protocol */
                        TeleportAgentInRegion(instance, agent, landmarkData.LocalPos, lookAt);
                    }
                    else
                    {
                        /* other region */
                        if (agent.SittingOnObject != null)
                        {
                            if (!agent.UnSit())
                            {
                                return;
                            }
                        }

                        if (!agent.TeleportTo(scene, agent, landmarkData.RegionID, landmarkData.LocalPos, lookAt, TeleportFlags.ViaLandmark))
                        {
                            agent.SendAlertMessage("Landmark destination not found", scene.ID);
                        }
                    }
                }
                else
                {
                    /* other grid */
                    if (agent.SittingOnObject != null)
                    {
                        if (!agent.UnSit())
                        {
                            return;
                        }
                    }

                    if (!agent.TeleportTo(scene, agent, landmarkData.GatekeeperURI, landmarkData.RegionID, landmarkData.LocalPos, lookAt, TeleportFlags.ViaLandmark))
                    {
                        agent.SendAlertMessage("Landmark destination not found", scene.ID);
                    }
                }
            }
            else
            {
                instance.ShoutError("Landmark asset not found");
            }
        }

        void TeleportAgentViaGlobalCoords(ScriptInstance instance, IAgent agent, GridVector location, Vector3 position, Vector3 lookAt)
        {
            /* instance already locked */
            SceneInterface scene = instance.Part.ObjectGroup.Scene;
            if (location.GridX == scene.RegionData.Location.GridX &&
                location.GridY == scene.RegionData.Location.GridY)
            {
                /* same region, skip the teleport protocol */
                TeleportAgentInRegion(instance, agent, position, lookAt);
            }
            else
            {
                if (agent.SittingOnObject != null)
                {
                    if (!agent.UnSit())
                    {
                        return;
                    }
                }
                if (!agent.TeleportTo(scene, agent, location, position, lookAt, TeleportFlags.ViaLocation))
                {
                    agent.SendAlertMessage(string.Format("Location '{0}' not found.", location.ToString()), scene.ID);
                }
            }
        }

        void TeleportAgentViaRegionName(ScriptInstance instance, IAgent agent, string regionName, Vector3 position, Vector3 lookAt)
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
                if (agent.SittingOnObject != null)
                {
                    if (!agent.UnSit())
                    {
                        return;
                    }
                }
                if (!agent.TeleportTo(scene, agent, regionName, position, lookAt, TeleportFlags.ViaRegionID))
                {
                    agent.SendAlertMessage(string.Format("Region '{0}' not found.", regionName), scene.ID);
                }
            }
        }

        void TeleportAgentInRegion(ScriptInstance instance, IAgent agent, Vector3 position, Vector3 lookAt)
        {
            /* instance already locked */

            /* Remarks: teleporting in same region does not need teleport protocol */
            SceneInterface scene = instance.Part.ObjectGroup.Scene;
            GridVector size = scene.RegionData.Size;
            if(position.X >= size.X || position.Y >= size.Y || position.X < 0 || position.Y < 0)
            {
                /* refuse teleport, out of region */
                return;
            }
            if(agent.SittingOnObject != null)
            {
                if(!agent.UnSit())
                {
                    return;
                }
            }

            double minPos = scene.Terrain[position] + agent.Appearance.AvatarHeight / 2;
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

                    if(!objectParcel.Owner.EqualsGrid(grp.Owner))
                    {
                        if (!objectParcel.GroupOwned)
                        {
                            return;
                        }
                        else if(!scene.HasGroupPower(grp.Owner, objectParcel.Group, GroupPowers.LandEjectAndFreeze))
                        {
                            return;
                        }
                    }
                    if(!agent.TeleportHome(scene, agent))
                    {
                        agent.SendAlertMessage("Teleport home failed", scene.ID);
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
                        instance.ShoutError("Sitting avatars cannot be teleported");
                        return;
                    }
                    else if (!agent.Owner.EqualsGrid(grantinfo.PermsGranter))
                    {
                        instance.ShoutError("Teleport permission is not granted by avatar");
                        return;
                    }
                    else if ((grantinfo.PermsMask & ScriptPermissions.Teleport) == 0)
                    {
                        instance.ShoutError("Teleport permission is not granted by avatar");
                        return;
                    }

                    GridVector location = new GridVector();
                    location.GridX = (ushort)globalCoordinates.X.Clamp(0, 65535);
                    location.GridY = (ushort)globalCoordinates.Y.Clamp(0, 65535);
                    TeleportAgentViaGlobalCoords(instance, agent, location, regionCoordinates, lookAt);
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
                        instance.ShoutError("Sitting avatars cannot be teleported");
                        return;
                    }
                    else if(!agent.Owner.EqualsGrid(grantinfo.PermsGranter))
                    {
                        instance.ShoutError("Teleport permission is not granted by avatar");
                        return;
                    }
                    else if ((grantinfo.PermsMask & ScriptPermissions.Teleport) == 0)
                    {
                        instance.ShoutError("Teleport permission is not granted by avatar");
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
        public void TeleportAgent(ScriptInstance instance, LSLKey avatar, int regionX, int regionY, Vector3 position, Vector3 lookAt)
        {
            lock (instance)
            {
                instance.CheckThreatLevel("osTeleportAgent", ScriptInstance.ThreatLevelType.VeryHigh);
                IAgent agent;
                if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(avatar, out agent))
                {
                    ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                    if (agent.SittingOnObject != null)
                    {
                        /* refuse sitting avatars */
                        instance.ShoutError("Sitting avatars cannot be teleported");
                        return;
                    }
                    else if (!agent.Owner.EqualsGrid(grantinfo.PermsGranter))
                    {
                        instance.ShoutError("Teleport permission is not granted by avatar");
                        return;
                    }
                    else if ((grantinfo.PermsMask & ScriptPermissions.Teleport) == 0)
                    {
                        instance.ShoutError("Teleport permission is not granted by avatar");
                        return;
                    }

                    GridVector location = new GridVector();
                    location.GridX = (ushort)regionX;
                    location.GridY = (ushort)regionY;
                    TeleportAgentViaGlobalCoords(instance, agent, location, position, lookAt);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osTeleportAgent")]
        public void TeleportAgent(ScriptInstance instance, LSLKey avatar, string regionName, Vector3 position, Vector3 lookAt)
        {
            lock (instance)
            {
                instance.CheckThreatLevel("osTeleportAgent", ScriptInstance.ThreatLevelType.VeryHigh);
                IAgent agent;
                if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(avatar, out agent))
                {
                    TeleportAgentViaRegionName(instance, agent, regionName, position, lookAt);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osTeleportAgentLandmark")]
        public void OsTeleportAgentLandmark(ScriptInstance instance, LSLKey avatar, string landmark, Vector3 lookAt)
        {
            lock (instance)
            {
                instance.CheckThreatLevel("osTeleportAgentLandmark", ScriptInstance.ThreatLevelType.VeryHigh);
                IAgent agent;
                if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(avatar, out agent))
                {
                    TeleportAgentViaLandmark(instance, agent, landmark, lookAt);
                }
            }
        }

        [APILevel(APIFlags.OSSL, "osTeleportAgent")]
        public void TeleportAgent(ScriptInstance instance, LSLKey avatar, Vector3 position, Vector3 lookAt)
        {
            lock (instance)
            {
                instance.CheckThreatLevel("osTeleportAgent", ScriptInstance.ThreatLevelType.VeryHigh);
                IAgent agent;
                if (instance.Part.ObjectGroup.Scene.RootAgents.TryGetValue(avatar, out agent))
                {
                    TeleportAgentInRegion(instance, agent, position, lookAt);
                }
            }
            throw new NotImplementedException("osTeleportAgent(key, vector, vector)");
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

                    GridVector location = new GridVector();
                    location.X = (ushort)regionX.Clamp(0, 65535);
                    location.Y = (ushort)regionY.Clamp(0, 65535);
                    TeleportAgentViaGlobalCoords(instance, agent, location, position, lookAt);
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
