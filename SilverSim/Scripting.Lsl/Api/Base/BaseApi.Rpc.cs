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

using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.Lsl.Api.Base
{
    public partial class BaseApi
    {
        [APIExtension(APIExtension.Extern, "rpcGetSenderScriptName")]
        public string GetSenderScriptName(ScriptInstance instance)
        {
            lock(instance)
            {
                return ((Script)instance).RpcRemoteScriptName;
            }
        }

        [APIExtension(APIExtension.Extern, "rpcGetSenderScriptKey")]
        public LSLKey GetSenderScriptKey(ScriptInstance instance)
        {
            lock (instance)
            {
                return ((Script)instance).RpcRemoteScriptKey;
            }
        }

        [APIExtension(APIExtension.Extern, "rpcGetRemoteLinkNumber")]
        public int GetSenderLinkNumber(ScriptInstance instance)
        {
            lock (instance)
            {
                return ((Script)instance).RpcRemoteLinkNumber;
            }
        }

        [APIExtension(APIExtension.Extern, "rpcGetRemoteKey")]
        public LSLKey GetSenderLinkKey(ScriptInstance instance)
        {
            lock (instance)
            {
                return ((Script)instance).RpcRemoteKey;
            }
        }

        [APIExtension(APIExtension.Extern, "rpc_access")]
        [APIDisplayName("rpc_access")]
        [APIAccessibleMembers]
        public class RpcAccessor
        {
            private readonly Script m_Script;

            public RpcAccessor(Script instance)
            {
                m_Script = instance;
            }

            public string ScriptName
            {
                get
                {
                    lock(m_Script)
                    {
                        return m_Script.RpcRemoteScriptName;
                    }
                }
            }

            public LSLKey ScriptKey
            {
                get
                {
                    lock(m_Script)
                    {
                        return m_Script.RpcRemoteScriptKey;
                    }
                }
            }

            public int LinkNumber
            {
                get
                {
                    lock(m_Script)
                    {
                        return m_Script.RpcRemoteLinkNumber;
                    }
                }
            }

            public LSLKey Key
            {
                get
                {
                    lock(m_Script)
                    {
                        return m_Script.RpcRemoteKey;
                    }
                }
            }
        }

        [APIExtension(APIExtension.Extern, APIUseAsEnum.Getter, "RpcSender")]
        public RpcAccessor GetRpcAccessor(ScriptInstance instance) => new RpcAccessor((Script)instance);
    }
}
