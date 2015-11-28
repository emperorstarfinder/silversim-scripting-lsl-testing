// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    [ScriptApiName("Agents")]
    [LSLImplementation]
    public class Agents_API : IScriptApi, IPlugin
    {
        public Agents_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_FLYING = 0x0001;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_ATTACHMENTS = 0x0002;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_SCRIPTED = 0x0004;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_MOUSELOOK = 0x0008;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_SITTING = 0x0010;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_ON_OBJECT = 0x0020;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_AWAY = 0x0040;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_WALKING = 0x0080;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_IN_AIR = 0x0100;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_TYPING = 0x0200;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_CROUCHING = 0x0400;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_BUSY = 0x0800;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_ALWAYS_RUN = 0x1000;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_AUTOPILOT = 0x2000;

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_LIST_PARCEL = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_LIST_PARCEL_OWNER = 2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int AGENT_LIST_REGION = 4;

        [APILevel(APIFlags.LSL, "llGetAgentList")]
        public AnArray GetAgentList(ScriptInstance instance, int scope, AnArray options)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetAgentInfo")]
        public int GetAgentInfo(ScriptInstance instance, LSLKey id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int DATA_ONLINE = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int DATA_NAME = 2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int DATA_BORN = 3;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int DATA_RATING = 4;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int DATA_PAYINFO = 8;

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PAYMENT_INFO_ON_FILE = 0x1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PAYMENT_INFO_USED = 0x2;

        [APILevel(APIFlags.LSL, "llRequestAgentData")]
        public LSLKey RequestAgentData(ScriptInstance instance, LSLKey id, int data)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llRequestDisplayName")]
        public LSLKey RequestDisplayName(ScriptInstance instance, LSLKey id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetUsername")]
        public string GetUsername(ScriptInstance instance, LSLKey id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llRequestUsername")]
        public LSLKey RequestUsername(ScriptInstance instance, LSLKey id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetDisplayName")]
        public string GetDisplayName(ScriptInstance instance, LSLKey id)
        {
            throw new NotImplementedException();
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

        [APILevel(APIFlags.LSL, "llTeleportAgentHome")]
        public void llTeleportAgentHome(ScriptInstance instance, LSLKey avatar)
        {
            throw new NotImplementedException();
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
    }
}
