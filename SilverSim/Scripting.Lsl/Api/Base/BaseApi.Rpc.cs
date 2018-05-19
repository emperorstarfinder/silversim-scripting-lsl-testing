using SilverSim.Scene.Types.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
