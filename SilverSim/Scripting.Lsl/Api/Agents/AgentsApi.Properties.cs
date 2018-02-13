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

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using System.Collections;
using System.Collections.Generic;

namespace SilverSim.Scripting.Lsl.Api.Agents
{
    public partial class AgentsApi
    {
        [APIExtension(APIExtension.Properties, "agentdata")]
        [APIDisplayName("agentdata")]
        [APIAccessibleMembers]
        [ImplementsCustomTypecasts]
        public class AgentData
        {
            public int IsNpc { get; }
            public LSLKey Key { get; }
            public string Name { get; }
            public Vector3 Position { get; }

            public AgentData()
            {
                Key = UUID.Zero;
            }

            public AgentData(IAgent agent)
            {
                IsNpc = agent.IsNpc.ToLSLBoolean();
                Key = agent.Owner.ID;
                Name = agent.Owner.FullName;
                Position = agent.GlobalPosition;
            }

            public static implicit operator bool(AgentData d) => d.Key.AsBoolean;
        }

        public class AgentEnumerator : IEnumerator<AgentData>
        {
            private readonly AgentData[] m_List;
            private int m_Position = -1;

            public AgentEnumerator(AgentData[] list)
            {
                m_List = list;
            }

            public AgentData Current => m_List[m_Position];

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext() => ++m_Position < m_List.Length;

            public void Reset() => m_Position = -1;
        }

        public class AgentEnumAccessor
        {
            protected readonly ScriptInstance m_ScriptInstance;
            protected readonly int m_FilterFlags;

            public AgentEnumAccessor(ScriptInstance instance, int filterflags)
            {
                m_ScriptInstance = instance;
                m_FilterFlags = filterflags;
            }

            protected AgentData[] GetList()
            {
                var agentList = new List<AgentData>();
                lock (m_ScriptInstance)
                {
                    ObjectGroup grp = m_ScriptInstance.Part.ObjectGroup;
                    SceneInterface scene = grp.Scene;
                    if ((m_FilterFlags & AGENT_LIST_PARCEL) != 0)
                    {
                        ParcelInfo thisParcel;
                        if (scene.Parcels.TryGetValue(grp.GlobalPosition, out thisParcel))
                        {
                            foreach (IAgent agent in scene.RootAgents)
                            {
                                ParcelInfo pInfo;
                                if (scene.Parcels.TryGetValue(agent.GlobalPosition, out pInfo) &&
                                    pInfo.ID == thisParcel.ID &&
                                    (agent.IsNpc && (m_FilterFlags & AGENT_LIST_EXCLUDENPC) == 0))
                                {
                                    agentList.Add(new AgentData(agent));
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (IAgent agent in scene.RootAgents)
                        {
                            if (agent.IsNpc && (m_FilterFlags & AGENT_LIST_EXCLUDENPC) != 0)
                            {
                                continue;
                            }

                            if ((m_FilterFlags & AGENT_LIST_PARCEL_OWNER) != 0)
                            {
                                ParcelInfo pInfo;
                                if (scene.Parcels.TryGetValue(agent.GlobalPosition, out pInfo) ||
                                    agent.Owner.EqualsGrid(pInfo.Owner))
                                {
                                    agentList.Add(new AgentData(agent));
                                }
                            }
                            else if ((m_FilterFlags & AGENT_LIST_REGION) != 0)
                            {
                                agentList.Add(new AgentData(agent));
                            }
                        }
                    }
                }
                return agentList.ToArray();
            }
        }

        [APIExtension(APIExtension.Properties, "agentfilteraccessor")]
        [APIDisplayName("agentfilteraccessor")]
        public class AgentFilterAccessor : AgentEnumAccessor
        {
            public AgentFilterAccessor(ScriptInstance instance, int filterflags)
                : base(instance, filterflags)
            {
            }

            public AgentEnumerator GetLslForeachEnumerator()
            {
                lock(m_ScriptInstance)
                {
                    return new AgentEnumerator(GetList());
                }
            }

            public AgentFilterAccessor WithoutNpc => new AgentFilterAccessor(m_ScriptInstance, AGENT_LIST_EXCLUDENPC | AGENT_LIST_REGION);
        }

        [APIExtension(APIExtension.Properties, "agentaccessor")]
        [APIDisplayName("agentaccessor")]
        [APIAccessibleMembers]
        public class AgentAccessor : AgentEnumAccessor
        {
            public AgentAccessor(ScriptInstance instance)
                : base(instance, 0)
            {
            }

            public AgentFilterAccessor InParcelOnly => new AgentFilterAccessor(m_ScriptInstance, AGENT_LIST_PARCEL);

            public AgentFilterAccessor WithoutNpc => new AgentFilterAccessor(m_ScriptInstance, AGENT_LIST_EXCLUDENPC);

            public AgentFilterAccessor RegionWide => new AgentFilterAccessor(m_ScriptInstance, AGENT_LIST_REGION);

            public AgentData this[LSLKey key]
            {
                get
                {
                    lock(m_ScriptInstance)
                    {
                        IAgent agent;
                        return m_ScriptInstance.Part.ObjectGroup.Scene.Agents.TryGetValue(key, out agent) ? new AgentData(agent) : new AgentData();
                    }
                }
            }

            public AgentEnumerator GetLslForeachEnumerator()
            {
                lock (m_ScriptInstance)
                {
                    return new AgentEnumerator(GetList());
                }
            }
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "Agents")]
        public AgentAccessor GetAgentsList(ScriptInstance instance) => new AgentAccessor(instance);
    }
}
