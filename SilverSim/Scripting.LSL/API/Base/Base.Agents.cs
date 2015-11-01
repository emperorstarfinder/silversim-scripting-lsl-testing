// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APILevel(APIFlags.LSL, "llGetRegionAgentCount")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal int GetRegionAgentCount(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Scene.Agents.Count;
            }
        }

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int DATA_ONLINE = 1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int DATA_NAME = 2;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int DATA_BORN = 3;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int DATA_RATING = 4;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int DATA_PAYINFO = 8;

        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int PAYMENT_INFO_ON_FILE = 0x1;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        internal const int PAYMENT_INFO_USED = 0x2;

        [APILevel(APIFlags.LSL, "llRequestAgentData")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal LSLKey RequestAgentData(ScriptInstance instance, LSLKey id, int data)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llRequestDisplayName")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal LSLKey RequestDisplayName(ScriptInstance instance, LSLKey id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llRequestUsername")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal LSLKey RequestUsername(ScriptInstance instance, LSLKey id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llGetDisplayName")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal string GetDisplayName(ScriptInstance instance, LSLKey id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL, "llKey2Name")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal string Key2Name(ScriptInstance instance, LSLKey id)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal Vector3 GetAgentSize(ScriptInstance instance, LSLKey id)
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

        #region osGetAvatarList
        [APILevel(APIFlags.OSSL, "osGetAvatarList")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal AnArray GetAvatarList(ScriptInstance instance)
        {
            AnArray res = new AnArray();

            lock (instance)
            {
                UUID ownerID = instance.Part.ObjectGroup.Scene.Owner.ID;
                foreach (IAgent agent in instance.Part.ObjectGroup.Scene.Agents)
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal AnArray GetAgents(ScriptInstance instance)
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
