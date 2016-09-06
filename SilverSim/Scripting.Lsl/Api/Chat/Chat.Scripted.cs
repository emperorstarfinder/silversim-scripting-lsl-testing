// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
        public void Shout(ScriptInstance instance, int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Shout;
            ev.Message = message;
            ev.SourceType = ListenEvent.ChatSourceType.Object;
            ev.OwnerID = GetOwner(instance);
            SendChat(instance, ev);
        }

        [APILevel(APIFlags.LSL, "llSay")]
        public void Say(ScriptInstance instance, int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Say;
            ev.Message = message;
            ev.SourceType = ListenEvent.ChatSourceType.Object;
            ev.OwnerID = GetOwner(instance);
            SendChat(instance, ev);
        }

        [APILevel(APIFlags.LSL, "llWhisper")]
        public void Whisper(ScriptInstance instance, int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Whisper;
            ev.Message = message;
            ev.SourceType = ListenEvent.ChatSourceType.Object;
            ev.OwnerID = GetOwner(instance);
            SendChat(instance, ev);
        }

        [APILevel(APIFlags.LSL, "llOwnerSay")]
        public void OwnerSay(ScriptInstance instance, string message)
        {
            lock (instance)
            {
                ListenEvent ev = new ListenEvent();
                ev.Channel = PUBLIC_CHANNEL;
                ev.Type = ListenEvent.ChatType.OwnerSay;
                ev.Message = message;
                ev.TargetID = instance.Part.ObjectGroup.Owner.ID;
                ev.SourceType = ListenEvent.ChatSourceType.Object;
                ev.OwnerID = GetOwner(instance);
                SendChat(instance, ev);
            }
        }

        [APILevel(APIFlags.LSL, "llRegionSay")]
        public void RegionSay(ScriptInstance instance, int channel, string message)
        {
            if (channel != PUBLIC_CHANNEL)
            {
                ListenEvent ev = new ListenEvent();
                ev.Type = ListenEvent.ChatType.Region;
                ev.Channel = channel;
                ev.Message = message;
                ev.OwnerID = GetOwner(instance);
                ev.SourceType = ListenEvent.ChatSourceType.Object;
                SendChat(instance, ev);
            }
        }

        [APILevel(APIFlags.LSL, "llRegionSayTo")]
        public void RegionSayTo(ScriptInstance instance, LSLKey target, int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Region;
            ev.Message = message;
            ev.TargetID = target;
            ev.OwnerID = GetOwner(instance);
            ev.SourceType = ListenEvent.ChatSourceType.Object;
            SendChat(instance, ev);
        }

        [APILevel(APIFlags.LSL, "llListen")]
        public int Listen(ScriptInstance instance, int channel, string name, LSLKey id, string msg)
        {
            Script script = (Script)instance;
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
                    if (kvp.Value.IsMatching(name, id.AsUUID, msg, 0))
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
                            delegate() { return instance.Part.ID; },
                            delegate() { return instance.Part.GlobalPosition; },
                            script.OnListen);
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
            Script script = (Script)instance;
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
            Script script = (Script)instance;
            ChatServiceInterface.Listener l;
            lock (script)
            {
                if (script.m_Listeners.TryGetValue(handle, out l))
                {
                    l.IsActive = active != 0;
                }
            }
        }

        #region osListenRegex
        [APILevel(APIFlags.OSSL, "osListenRegex")]
        public int ListenRegex(ScriptInstance instance, int channel, string name, LSLKey id, string msg, int regexBitfield)
        {
            Script script = (Script)instance;
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
                    if (kvp.Value.IsMatching(name, id.AsUUID, msg, regexBitfield))
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
                            delegate() { return instance.Part.ID; },
                            delegate() { return instance.Part.GlobalPosition; },
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
            Script script = (Script)instance;
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

        [ExecutedOnDeserialization("listener")]
        public void Deserialize(ScriptInstance instance, List<object> args)
        {
            Script script = (Script)instance;
            lock(script)
            {
                ChatServiceInterface chatservice = instance.Part.ObjectGroup.Scene.GetService<ChatServiceInterface>();
                int lstart = 0;
                int argsCount = args.Count;
                while(lstart < argsCount)
                {
                    if(lstart < 6)
                    {
                        break;
                    }

                    bool isActive = (bool)args[lstart++];
                    int handle = (int)args[lstart++];
                    int channel = (int)args[lstart++];
                    string name = (string)args[lstart++];
                    UUID key = (UUID)args[lstart++];
                    string message = (string)args[lstart++];
                    int regexBitfield = (lstart == argsCount || args[lstart] is bool) ?
                        0 :
                        (int)args[lstart++];

                    if(!script.m_Listeners.ContainsKey(handle))
                    {
                        ChatServiceInterface.Listener l;
                        l = regexBitfield == 0 ?
                            chatservice.AddListen(
                                channel,
                                name,
                                key,
                                message,
                                delegate () { return instance.Part.ID; },
                                delegate () { return instance.Part.GlobalPosition; },
                                script.OnListen) :
                            chatservice.AddListenRegex(
                                channel,
                                name,
                                key,
                                message,
                                regexBitfield,
                                delegate () { return instance.Part.ID; },
                                delegate () { return instance.Part.GlobalPosition; },
                                script.OnListen);
                        l.IsActive = isActive;

                        script.m_Listeners.Add(handle, l);
                    }
                }
            }
        }

        [ExecutedOnSerialization("listener")]
        public void Serialize(ScriptInstance instance, List<object> res)
        {
            Script script = (Script)instance;
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
                res[idx] = (res.Count - countofs);
            }
        }
    }
}
