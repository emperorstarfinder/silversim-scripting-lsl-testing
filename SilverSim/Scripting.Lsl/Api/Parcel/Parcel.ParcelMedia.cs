// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Parcel;
using SilverSim.Viewer.Messages.Parcel;
using System;

namespace SilverSim.Scripting.Lsl.Api.Parcel
{
    public partial class ParcelApi
    {
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_STOP = 0;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_PAUSE = 1;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_PLAY = 2;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_LOOP = 3;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_TEXTURE = 4;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_URL = 5;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_TIME = 6;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_AGENT = 7;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_UNLOAD = 8;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_AUTO_ALIGN = 9;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_TYPE = 10;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_SIZE = 11;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_DESC = 12;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_LOOP_SET = 13;

        [APILevel(APIFlags.LSL, "llParcelMediaCommandList")]
        [ForcedSleep(2)]
        public void ParcelMediaCommandList(ScriptInstance instance, AnArray commandList)
        {
            lock(instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                ParcelInfo parcelInfo;
                if (scene.Parcels.TryGetValue(grp.GlobalPosition, out parcelInfo))
                {
                    if(!scene.CanEditParcelDetails(grp.Owner, parcelInfo))
                    {
                        return;
                    }
                    string mediaUrl = parcelInfo.MediaURI ?? string.Empty;
                    UUID mediaTexture = parcelInfo.MediaID;
                    bool mediaAutoAlign = parcelInfo.MediaAutoScale;
                    string mediaType = parcelInfo.MediaType;
                    string mediaDescription = parcelInfo.MediaDescription;
                    int mediaWidth = parcelInfo.MediaWidth;
                    int mediaHeight = parcelInfo.MediaHeight;

                    int sendCommand = -1;
                    double mediaStartTime = -1;
                    double mediaLoopTime = -1;

                    int i = 0;
                    int numCommands = commandList.Count;
                    IAgent agent = null;

                    while (i < numCommands)
                    {
                        int cmd = commandList[i++].AsInt;
                        switch (cmd)
                        {
                            case PARCEL_MEDIA_COMMAND_AGENT:
                                if (i < numCommands)
                                {
                                    UUID agentID = commandList[i++].AsUUID;
                                    if (!scene.RootAgents.TryGetValue(agentID, out agent))
                                    {
                                        return;
                                    }
                                }
                                break;

                            case PARCEL_MEDIA_COMMAND_LOOP:
                            case PARCEL_MEDIA_COMMAND_PAUSE:
                            case PARCEL_MEDIA_COMMAND_PLAY:
                            case PARCEL_MEDIA_COMMAND_UNLOAD:
                                sendCommand = cmd;
                                break;

                            case PARCEL_MEDIA_COMMAND_URL:
                                if (i < numCommands)
                                {
                                    mediaUrl = commandList[i++].ToString();
                                }
                                break;

                            case PARCEL_MEDIA_COMMAND_TEXTURE:
                                if (i < numCommands)
                                {
                                    /* let us accept more than just UUIDs. Accepting prim inventory simplifies a lot. */
                                    mediaTexture = instance.GetTextureAssetID(commandList[i++].ToString());
                                }
                                break;

                            case PARCEL_MEDIA_COMMAND_TIME:
                                if (i < numCommands)
                                {
                                    mediaStartTime = commandList[i++].AsReal;
                                }
                                break;

                            case PARCEL_MEDIA_COMMAND_LOOP_SET:
                                if (i < numCommands)
                                {
                                    mediaLoopTime = commandList[i++].AsReal;
                                }
                                break;

                            case PARCEL_MEDIA_COMMAND_AUTO_ALIGN:
                                if (i < numCommands)
                                {
                                    mediaAutoAlign = commandList[i++].AsBoolean;
                                }
                                break;

                            case PARCEL_MEDIA_COMMAND_TYPE:
                                if (i < numCommands)
                                {
                                    mediaType = commandList[i++].ToString();
                                }
                                break;

                            case PARCEL_MEDIA_COMMAND_DESC:
                                if (i < numCommands)
                                {
                                    mediaDescription = commandList[i++].ToString();
                                }
                                break;

                            case PARCEL_MEDIA_COMMAND_SIZE:
                                if (i + 1 < numCommands)
                                {
                                    mediaWidth = commandList[i++].AsInt;
                                    mediaHeight = commandList[i++].AsInt;
                                }
                                break;

                            default:
                                instance.ShoutError("llParcelMediaCommandList: Unknown parameter");
                                break;
                        }
                    }

                    if (0 > sendCommand)
                    {
                        /* ignore */
                    }
                    else
                    {
                        ParcelMediaUpdate pmu = new ParcelMediaUpdate();
                        pmu.MediaAutoScale = mediaAutoAlign;
                        pmu.MediaDesc = mediaDescription;
                        pmu.MediaHeight = mediaHeight;
                        pmu.MediaID = mediaTexture;
                        pmu.MediaLoop = sendCommand == PARCEL_MEDIA_COMMAND_LOOP;
                        pmu.MediaType = mediaType;
                        pmu.MediaURL = mediaUrl;
                        pmu.MediaWidth = mediaWidth;

                        if (agent != null)
                        {
                            /* per agent */
                            ParcelInfo parcelTest;
                            if (scene.Parcels.TryGetValue(agent.GlobalPosition, out parcelTest) &&
                                parcelInfo.ID == parcelTest.ID)
                            {
                                agent.SendMessageIfRootAgent(pmu, scene.ID);
                            }
                        }
                        else
                        {
                            parcelInfo.MediaAutoScale = pmu.MediaAutoScale;
                            parcelInfo.MediaDescription = pmu.MediaDesc;
                            parcelInfo.MediaHeight = pmu.MediaHeight;
                            parcelInfo.MediaID = pmu.MediaID;
                            parcelInfo.MediaType = pmu.MediaType;
                            parcelInfo.MediaLoop = pmu.MediaLoop;
                            parcelInfo.MediaURI = new URI(pmu.MediaURL);
                            parcelInfo.MediaWidth = pmu.MediaWidth;
                            scene.TriggerParcelUpdate(parcelInfo);

                            foreach(IAgent rootAgent in scene.RootAgents)
                            {
                                ParcelInfo parcelTest;
                                if (scene.Parcels.TryGetValue(rootAgent.GlobalPosition, out parcelTest) &&
                                    parcelInfo.ID == parcelTest.ID)
                                {
                                    rootAgent.SendMessageIfRootAgent(pmu, scene.ID);
                                }
                            }
                        }

                        if (sendCommand >= 0)
                        {
                            ParcelMediaCommandMessage pmc = new ParcelMediaCommandMessage();
                            pmc.Flags = (uint)1 << sendCommand; /* Flags is a bit set of the commands to do */
                            pmc.Command = (uint)sendCommand;
                            if (mediaStartTime >= 0)
                            {
                                pmc.Flags |= 1 << PARCEL_MEDIA_COMMAND_TIME;
                                pmc.Time = mediaStartTime;
                            }

                            if(agent != null)
                            {
                                ParcelInfo parcelTest;
                                if (scene.Parcels.TryGetValue(agent.GlobalPosition, out parcelTest) &&
                                    parcelInfo.ID == parcelTest.ID)
                                {
                                    agent.SendMessageIfRootAgent(pmc, scene.ID);
                                }
                            }
                            else
                            {
                                foreach (IAgent rootAgent in scene.RootAgents)
                                {
                                    ParcelInfo parcelTest;
                                    if (scene.Parcels.TryGetValue(rootAgent.GlobalPosition, out parcelTest) &&
                                        parcelInfo.ID == parcelTest.ID)
                                    {
                                        rootAgent.SendMessageIfRootAgent(pmc, scene.ID);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llParcelMediaQuery")]
        [ForcedSleep(2)]
        public AnArray ParcelMediaQuery(ScriptInstance instance, AnArray query)
        {
            AnArray res = new AnArray();
            lock(instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                ParcelInfo parcelInfo;
                if(scene.Parcels.TryGetValue(grp.GlobalPosition, out parcelInfo))
                {
                    foreach(IValue iv in query)
                    {
                        switch(iv.AsInt)
                        {
                            case PARCEL_MEDIA_COMMAND_URL:
                                if (null != parcelInfo.MediaURI && !parcelInfo.ObscureMedia)
                                {
                                    res.Add(parcelInfo.MediaURI.ToString());
                                }
                                else
                                {
                                    res.Add(string.Empty);
                                }
                                break;

                            case PARCEL_MEDIA_COMMAND_DESC:
                                res.Add(parcelInfo.MediaDescription);
                                break;

                            case PARCEL_MEDIA_COMMAND_TEXTURE:
                                res.Add(instance.FindInventoryName(AssetType.Texture, parcelInfo.MediaID));
                                break;

                            case PARCEL_MEDIA_COMMAND_TYPE:
                                res.Add(parcelInfo.MediaType);
                                break;

                            case PARCEL_MEDIA_COMMAND_SIZE:
                                res.Add(parcelInfo.MediaWidth);
                                res.Add(parcelInfo.MediaHeight);
                                break;

                            case PARCEL_MEDIA_COMMAND_AUTO_ALIGN:
                                res.Add(parcelInfo.MediaAutoScale ? 1 : 0);
                                break;
                        }
                    }
                }
            }
            return res;
        }

        [APILevel(APIFlags.OSSL, "osSetParcelMediaURL")]
        public void SetParcelMediaURL(ScriptInstance instance, string url)
        {
            lock (instance)
            {
                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                ParcelInfo parcelInfo;
                if (scene.Parcels.TryGetValue(grp.GlobalPosition, out parcelInfo))
                {
                    if (!scene.CanEditParcelDetails(grp.Owner, parcelInfo))
                    {
                        return;
                    }
                    parcelInfo.MediaURI = new URI(url);
                    ParcelMediaUpdate pmu = new ParcelMediaUpdate();
                    pmu.MediaAutoScale = parcelInfo.MediaAutoScale;
                    pmu.MediaDesc = parcelInfo.MediaDescription;
                    pmu.MediaHeight = parcelInfo.MediaHeight;
                    pmu.MediaID = parcelInfo.MediaID;
                    pmu.MediaLoop = parcelInfo.MediaLoop;
                    pmu.MediaType = parcelInfo.MediaType;
                    pmu.MediaURL = url;
                    pmu.MediaWidth = parcelInfo.MediaWidth;

                    parcelInfo.MediaAutoScale = pmu.MediaAutoScale;
                    parcelInfo.MediaDescription = pmu.MediaDesc;
                    parcelInfo.MediaHeight = pmu.MediaHeight;
                    parcelInfo.MediaID = pmu.MediaID;
                    parcelInfo.MediaType = pmu.MediaType;
                    parcelInfo.MediaLoop = pmu.MediaLoop;
                    parcelInfo.MediaURI = new URI(pmu.MediaURL);
                    parcelInfo.MediaWidth = pmu.MediaWidth;
                    scene.TriggerParcelUpdate(parcelInfo);

                    foreach (IAgent rootAgent in scene.RootAgents)
                    {
                        ParcelInfo parcelTest;
                        if (scene.Parcels.TryGetValue(rootAgent.GlobalPosition, out parcelTest) &&
                            parcelInfo.ID == parcelTest.ID)
                        {
                            rootAgent.SendMessageIfRootAgent(pmu, scene.ID);
                        }
                    }
                }
            }
        }
    }
}
