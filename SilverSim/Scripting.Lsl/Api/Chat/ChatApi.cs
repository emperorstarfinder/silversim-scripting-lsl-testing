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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Chat
{
    [LSLImplementation]
    [ScriptApiName("Chat")]
    [Description("LSL/OSSL Chat API")]
    [ServerParam("LSL.MaxListenersPerScript", ParameterType = typeof(uint), DefaultValue = 1000)]
    public partial class ChatApi : IScriptApi, IPlugin, IServerParamListener
    {
        [APILevel(APIFlags.LSL)]
        public const int PUBLIC_CHANNEL = 0;
        [APILevel(APIFlags.LSL)]
        public const int DEBUG_CHANNEL = 0x7FFFFFFF;

        private UUID GetOwner(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Owner.ID;
            }
        }

        private UGI GetGroup(ScriptInstance instance)
        {
            lock(instance)
            {
                return instance.Part.ObjectGroup.Group;
            }
        }

        private void SendChat(ScriptInstance instance, ListenEvent ev)
        {
            lock (instance)
            {
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                ev.ID = thisGroup.ID;
                ev.GlobalPosition = instance.Part.GlobalPosition;
                ev.Name = thisGroup.Name;
                thisGroup.Scene.GetService<ChatServiceInterface>().Send(ev);
            }
        }

        [APILevel(APIFlags.OSSL)]
        public const int OS_LISTEN_REGEX_NAME = 0x1;
        [APILevel(APIFlags.OSSL)]
        public const int OS_LISTEN_REGEX_MESSAGE = 0x2;

        public void Startup(ConfigurationLoader loader)
        {
            /* nothing to do */
        }

        private int GetMaxListenerHandles(UUID regionID)
        {
            int value;
            if(m_MaxListenerHandleParams.TryGetValue(regionID, out value) ||
                m_MaxListenerHandleParams.TryGetValue(UUID.Zero, out value))
            {
                return value;
            }
            return 1000;
        }

        private readonly RwLockedDictionary<UUID, int> m_MaxListenerHandleParams = new RwLockedDictionary<UUID, int>();

        [ServerParam("LSL.MaxListenersPerScript")]
        public void MaxListenersPerScriptUpdated(UUID regionID, string value)
        {
            int intval;
            if (value.Length == 0)
            {
                m_MaxListenerHandleParams.Remove(regionID);
            }
            else if (int.TryParse(value, out intval))
            {
                m_MaxListenerHandleParams[regionID] = intval;
            }
        }
    }
}
