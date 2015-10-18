// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scripting.LSL.API.Chat
{
    public partial class Chat_API
    {
        public static int MaxListenerHandles = 64;

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llShout")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSay")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llWhisper")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llOwnerSay")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llRegionSay")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llRegionSayTo")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llListen")]
        public int Listen(ScriptInstance instance, int channel, string name, LSLKey id, string msg)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Listeners.Count >= MaxListenerHandles)
                {
                    return new Integer(-1);
                }
                ChatServiceInterface chatservice = instance.Part.ObjectGroup.Scene.GetService<ChatServiceInterface>();

                int newhandle = 0;
                ChatServiceInterface.Listener l;
                for (newhandle = 0; newhandle < MaxListenerHandles; ++newhandle)
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llListenRemove")]
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

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llListenControl")]
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
        [APILevel(APIFlags.OSSL)]
        [ScriptFunctionName("osListenRegex")]
        public int ListenRegex(ScriptInstance instance, int channel, string name, LSLKey id, string msg, int regexBitfield)
        {
            Script script = (Script)instance;
            lock (script)
            {
                if (script.m_Listeners.Count >= MaxListenerHandles)
                {
                    return -1;
                }
                ChatServiceInterface chatservice = instance.Part.ObjectGroup.Scene.GetService<ChatServiceInterface>();

                int newhandle = 0;
                ChatServiceInterface.Listener l;
                for (newhandle = 0; newhandle < MaxListenerHandles; ++newhandle)
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
    }
}
