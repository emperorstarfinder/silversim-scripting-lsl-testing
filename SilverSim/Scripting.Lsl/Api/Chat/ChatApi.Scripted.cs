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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scripting.Lsl.Api.Chat
{
    public partial class ChatApi
    {
        [APILevel(APIFlags.LSL, "listen")]
        [StateEventDelegate]
        public delegate void State_listen(int channel, string name, LSLKey id, string message);

        [APILevel(APIFlags.LSL, "llShout")]
        [AllowExplicitTypecastsBeImplicitToString(2)]
        public void Shout(ScriptInstance instance, int channel, string message)
        {
            var ev = new ListenEvent
            {
                Channel = channel,
                Type = channel == DEBUG_CHANNEL ? ListenEvent.ChatType.DebugChannel : ListenEvent.ChatType.Shout,
                Message = message,
                SourceType = ListenEvent.ChatSourceType.Object,
                OwnerID = GetOwner(instance),
                Group = GetGroup(instance)
            };
            SendChat(instance, ev);
        }

        [APILevel(APIFlags.OSSL, "llShout")]
        [AllowExplicitTypecastsBeImplicitToString(1)]
        public void Shout(ScriptInstance instance, string message) => Shout(instance, PUBLIC_CHANNEL, message);

        [APILevel(APIFlags.LSL, "llSay")]
        [AllowExplicitTypecastsBeImplicitToString(2)]
        public void Say(ScriptInstance instance, int channel, string message)
        {
            var ev = new ListenEvent
            {
                Channel = channel,
                Type = channel == DEBUG_CHANNEL ? ListenEvent.ChatType.DebugChannel : ListenEvent.ChatType.Say,
                Message = message,
                SourceType = ListenEvent.ChatSourceType.Object,
                OwnerID = GetOwner(instance),
                Group = GetGroup(instance)
            };
            SendChat(instance, ev);
        }

        [APILevel(APIFlags.OSSL, "llSay")]
        [AllowExplicitTypecastsBeImplicitToString(1)]
        public void Say(ScriptInstance instance, string message) => Say(instance, PUBLIC_CHANNEL, message);

        [APILevel(APIFlags.LSL, "llWhisper")]
        [AllowExplicitTypecastsBeImplicitToString(2)]
        public void Whisper(ScriptInstance instance, int channel, string message)
        {
            var ev = new ListenEvent
            {
                Channel = channel,
                Type = channel == DEBUG_CHANNEL ? ListenEvent.ChatType.DebugChannel : ListenEvent.ChatType.Whisper,
                Message = message,
                SourceType = ListenEvent.ChatSourceType.Object,
                OwnerID = GetOwner(instance),
                Group = GetGroup(instance)
            };
            SendChat(instance, ev);
        }

        [APILevel(APIFlags.OSSL, "llWhisper")]
        [AllowExplicitTypecastsBeImplicitToString(1)]
        public void Whisper(ScriptInstance instance, string message) => Whisper(instance, PUBLIC_CHANNEL, message);

        [APILevel(APIFlags.LSL, "llOwnerSay")]
        [AllowExplicitTypecastsBeImplicitToString(1)]
        public void OwnerSay(ScriptInstance instance, string message)
        {
            lock (instance)
            {
                var ev = new ListenEvent
                {
                    Channel = PUBLIC_CHANNEL,
                    Type = ListenEvent.ChatType.OwnerSay,
                    Message = message,
                    TargetID = instance.Part.ObjectGroup.Owner.ID,
                    SourceType = ListenEvent.ChatSourceType.Object,
                    OwnerID = GetOwner(instance),
                    Group = GetGroup(instance)
                };
                SendChat(instance, ev);
            }
        }

        [APILevel(APIFlags.LSL, "llRegionSay")]
        [AllowExplicitTypecastsBeImplicitToString(2)]
        public void RegionSay(ScriptInstance instance, int channel, string message)
        {
            if (channel != PUBLIC_CHANNEL)
            {
                var ev = new ListenEvent
                {
                    Type = channel == DEBUG_CHANNEL ? ListenEvent.ChatType.DebugChannel : ListenEvent.ChatType.Region,
                    Channel = channel,
                    Message = message,
                    OwnerID = GetOwner(instance),
                    SourceType = ListenEvent.ChatSourceType.Object,
                    Group = GetGroup(instance)
                };
                SendChat(instance, ev);
            }
        }

        [APILevel(APIFlags.LSL, "llRegionSayTo")]
        [AllowExplicitTypecastsBeImplicitToString(3)]
        public void RegionSayTo(ScriptInstance instance, LSLKey target, int channel, string message)
        {
            var ev = new ListenEvent
            {
                Channel = channel,
                Type = channel == DEBUG_CHANNEL ? ListenEvent.ChatType.DebugChannel : ListenEvent.ChatType.Region,
                Message = message,
                TargetID = target,
                OwnerID = GetOwner(instance),
                SourceType = ListenEvent.ChatSourceType.Object,
                Group = GetGroup(instance)
            };
            SendChat(instance, ev);
        }

        [APILevel(APIFlags.OSSL, "llRegionSayTo")]
        [AllowExplicitTypecastsBeImplicitToString(2)]
        public void RegionSayTo(ScriptInstance instance, LSLKey target, string message) => RegionSayTo(instance, target, PUBLIC_CHANNEL, message);

        [APILevel(APIFlags.LSL, "llListen")]
        public int Listen(ScriptInstance instance, int channel, string name, LSLKey id, string msg) =>
            Listen(instance, channel, name, id, msg, LISTEN_FLAG_ENABLE);

        [APILevel(APIFlags.ASSL, "asListen")]
        public int Listen(ScriptInstance instance, int channel, string name, LSLKey id, string msg, int flags)
        {
            var script = (Script)instance;
            lock (script)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (script.m_Listeners.Count >= GetMaxListenerHandles(scene.ID))
                {
                    return new Integer(-1);
                }
                ChatServiceInterface chatservice = scene.GetService<ChatServiceInterface>();

                /* LSL matches on repeating llListen with the already created listen */
                foreach (KeyValuePair<int, ChatServiceInterface.Listener> kvp in script.m_Listeners)
                {
                    if (kvp.Value.IsMatching(name, id.AsUUID, msg, 0) && kvp.Value.Channel == channel)
                    {
                        return kvp.Key;
                    }
                }

                ChatServiceInterface.Listener l;
                for (int newhandle = 0; newhandle < GetMaxListenerHandles(scene.ID); ++newhandle)
                {
                    if (!script.m_Listeners.TryGetValue(newhandle, out l))
                    {
                        l = chatservice.AddListen(
                            channel,
                            name,
                            id,
                            msg,
                            () => instance.Part.ID,
                            () => instance.Part.Group,
                            () => instance.Part.GlobalPosition,
                            () => instance.Part.ObjectGroup.IsAttached ? instance.Part.ID : UUID.Zero,
                            script.OnListen);
                        if((flags & LISTEN_FLAG_ENABLE) != 0)
                        {
                            l.IsActive = true;
                        }
                        else
                        {
                            l.IsActive = false;
                        }
                        if((flags & LISTEN_FLAG_LIMIT_TO_SAME_OWNER) != 0)
                        {
                            l.LimitToSameOwner = true;
                        }
                        if((flags & LISTEN_FLAG_LIMIT_TO_SAME_GROUP) != 0)
                        {
                            l.LimitToSameGroup = true;
                        }
                        try
                        {
                            script.m_Listeners.Add(newhandle, l);
                            return new Integer(newhandle);
                        }
                        catch
                        {
                            l.Remove();
                            return new Integer(-1);
                        }
                    }
                }
                return new Integer(-1);
            }
        }

        [APILevel(APIFlags.LSL, "llListenRemove")]
        public void ListenRemove(ScriptInstance instance, int handle)
        {
            var script = (Script)instance;
            ChatServiceInterface.Listener l;
            lock (script)
            {
                if (script.m_Listeners.Remove(handle, out l))
                {
                    l.Remove();
                }
            }
        }

        [APILevel(APIFlags.LSL, "llListenControl")]
        public void ListenControl(ScriptInstance instance, int handle, int active)
        {
            var script = (Script)instance;
            ChatServiceInterface.Listener l;
            lock (script)
            {
                if (script.m_Listeners.TryGetValue(handle, out l))
                {
                    l.IsActive = active != 0;
                }
            }
        }

        [APILevel(APIFlags.ASSL)]
        public const int LISTEN_FLAG_ENABLE = 1;
        [APILevel(APIFlags.ASSL)]
        public const int LISTEN_FLAG_LIMIT_TO_SAME_OWNER = 2;
        [APILevel(APIFlags.ASSL)]
        public const int LISTEN_FLAG_LIMIT_TO_SAME_GROUP = 4;

        [APILevel(APIFlags.ASSL, "asListenControl")]
        public void ListenControl(ScriptInstance instance, int handle, int enableflags, int disableflags)
        {
            var script = (Script)instance;
            ChatServiceInterface.Listener l;
            int changeflags = enableflags | disableflags;
            lock (script)
            {
                if (script.m_Listeners.TryGetValue(handle, out l))
                {
                    if ((changeflags & LISTEN_FLAG_ENABLE) != 0)
                    {
                        l.IsActive = (disableflags & LISTEN_FLAG_ENABLE) == 0;
                    }
                    if((changeflags & LISTEN_FLAG_LIMIT_TO_SAME_OWNER) != 0)
                    {
                        l.LimitToSameOwner = (disableflags & LISTEN_FLAG_LIMIT_TO_SAME_OWNER) == 0;
                    }
                    if((changeflags & LISTEN_FLAG_LIMIT_TO_SAME_GROUP) != 0)
                    {
                        l.LimitToSameGroup = (disableflags & LISTEN_FLAG_LIMIT_TO_SAME_GROUP) == 0;
                    }
                }
            }
        }

        #region osListenRegex
        [APILevel(APIFlags.OSSL, "osListenRegex")]
        public int ListenRegex(ScriptInstance instance, int channel, string name, LSLKey id, string msg, int regexBitfield)
        {
            var script = (Script)instance;
            lock (script)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                if (script.m_Listeners.Count >= GetMaxListenerHandles(scene.ID))
                {
                    return -1;
                }
                ChatServiceInterface chatservice = scene.GetService<ChatServiceInterface>();

                /* LSL matches on repeating llListen with the already created listen */
                foreach (KeyValuePair<int, ChatServiceInterface.Listener> kvp in script.m_Listeners)
                {
                    if (kvp.Value.IsMatching(name, id.AsUUID, msg, regexBitfield) && kvp.Value.Channel == channel)
                    {
                        return kvp.Key;
                    }
                }

                ChatServiceInterface.Listener l;
                for (int newhandle = 0; newhandle < GetMaxListenerHandles(scene.ID); ++newhandle)
                {
                    if (!script.m_Listeners.TryGetValue(newhandle, out l))
                    {
                        l = chatservice.AddListenRegex(
                            channel,
                            name,
                            id,
                            msg,
                            regexBitfield,
                            () => instance.Part.ID,
                            () => instance.Part.Group,
                            () => instance.Part.GlobalPosition,
                            () => instance.Part.ObjectGroup.IsAttached ? instance.Part.ID : UUID.Zero,
                            script.OnListen);
                        try
                        {
                            script.m_Listeners.Add(newhandle, l);
                            return newhandle;
                        }
                        catch
                        {
                            l.Remove();
                            return -1;
                        }
                    }
                }
            }
            return -1;
        }
        #endregion

        [ExecutedOnStateChange]
        [ExecutedOnScriptRemove]
        public static void ResetListeners(ScriptInstance instance)
        {
            var script = (Script)instance;
            lock (script)
            {
                ICollection<ChatServiceInterface.Listener> coll = script.m_Listeners.Values;
                script.m_Listeners.Clear();
                foreach (ChatServiceInterface.Listener l in coll)
                {
                    l.Remove();
                }
            }
        }

        [ExecutedOnDeserialization("listen")]
        public void Deserialize(ScriptInstance instance, List<object> args)
        {
            var script = (Script)instance;
            lock(script)
            {
                ChatServiceInterface chatservice = instance.Part.ObjectGroup.Scene.GetService<ChatServiceInterface>();
                int lstart = 0;
                int argsCount = args.Count;
                while(lstart + 6 <= argsCount)
                {
                    var isActive = (bool)args[lstart++];
                    var handle = (int)args[lstart++];
                    var channel = (int)args[lstart++];
                    var name = (string)args[lstart++];
                    var key = (UUID)args[lstart++];
                    var message = (string)args[lstart++];
                    int regexBitfield = (lstart == argsCount || args[lstart] is bool) ?
                        0 :
                        (int)args[lstart++];

                    if(!script.m_Listeners.ContainsKey(handle))
                    {
                        ChatServiceInterface.Listener l = regexBitfield == 0 ?
                            chatservice.AddListen(
                                channel,
                                name,
                                key,
                                message,
                                () => instance.Part.ID,
                                () => instance.Part.Group,
                                () => instance.Part.GlobalPosition,
                                () => instance.Part.ObjectGroup.IsAttached ? instance.Part.ID : UUID.Zero,
                                script.OnListen) :
                            chatservice.AddListenRegex(
                                channel,
                                name,
                                key,
                                message,
                                regexBitfield,
                                () => instance.Part.ID,
                                () => instance.Part.Group,
                                () => instance.Part.GlobalPosition,
                                () => instance.Part.ObjectGroup.IsAttached ? instance.Part.ID : UUID.Zero,
                                script.OnListen);
                        l.IsActive = isActive;

                        script.m_Listeners.Add(handle, l);
                    }
                }
            }
        }

        [ExecutedOnSerialization("listen")]
        public void Serialize(ScriptInstance instance, List<object> res)
        {
            var script = (Script)instance;
            lock(script)
            {
                res.Add("listen");
                int idx = res.Count;
                res.Add("0");
                int countofs = res.Count;
                foreach (KeyValuePair<int, ChatServiceInterface.Listener> kvp in script.m_Listeners)
                {
                    kvp.Value.Serialize(res, kvp.Key);
                }
                res[idx] = res.Count - countofs;
            }
        }
    }
}
