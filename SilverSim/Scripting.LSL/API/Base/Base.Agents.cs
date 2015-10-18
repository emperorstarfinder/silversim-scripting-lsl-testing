// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetRegionAgentCount")]
        public int LlGetRegionAgentCount(ScriptInstance instance)
        {
            return instance.Part.ObjectGroup.Scene.Agents.Count;
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llRequestAgentData")]
        public LSLKey RequestAgentData(ScriptInstance instance, LSLKey id, int data)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llRequestDisplayName")]
        public LSLKey RequestDisplayName(ScriptInstance instance, LSLKey id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llRequestUsername")]
        public LSLKey RequestUsername(ScriptInstance instance, LSLKey id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetDisplayName")]
        public string GetDisplayName(ScriptInstance instance, LSLKey id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llKey2Name")]
        public string Key2Name(ScriptInstance instance, LSLKey id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetAgentSize")]
        public Vector3 GetAgentSize(ScriptInstance instance, LSLKey id)
        {
            lock (instance)
            {
                IAgent agent;
                try
                {
                    agent = instance.Part.ObjectGroup.Scene.Agents[id];
                }
                catch
                {
                    return Vector3.Zero;
                }

                if (agent.IsInScene(instance.Part.ObjectGroup.Scene))
                {
                    return agent.Size;
                }
                return Vector3.Zero;
            }
        }

        #region osGetAvatarList
        [APILevel(APIFlags.OSSL)]
        [ScriptFunctionName("osGetAvatarList")]
        public AnArray GetAvatarList(ScriptInstance instance)
        {
            AnArray res = new AnArray();

            lock (instance)
            {
                foreach (IAgent agent in instance.Part.ObjectGroup.Scene.Agents)
                {
                    if (agent.ID == instance.Part.ObjectGroup.Scene.Owner.ID)
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
        [APILevel(APIFlags.OSSL)]
        [ScriptFunctionName("osGetAgents")]
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
