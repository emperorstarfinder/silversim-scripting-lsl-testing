using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilverSim.Scripting.Lsl.Api.Chat
{
    public partial class ChatApi
    {
        [APIExtension(APIExtension.Properties, "scriptchannel")]
        [APIDisplayName("scriptchannel")]
        [APIIsVariableType]
        [APIAccessibleMembers("IsDebug", "IsPublic")]
        [ImplementsCustomTypecasts]
        public struct ScriptChannel
        {
            public int Number;

            [APIExtension(APIExtension.Properties)]
            public static implicit operator int(ScriptChannel c) => c.Number;

            public int IsDebug => (Number == DEBUG_CHANNEL).ToLSLBoolean();
            public int IsPublic => (Number == PUBLIC_CHANNEL).ToLSLBoolean();
        }

        [APIExtension(APIExtension.Properties, "listenhandle")]
        [APIDisplayName("listenhandle")]
        [APIIsVariableType]
        [ImplementsCustomTypecasts]
        public struct ListenHandle
        {
            public int Channel;
            public int Handle;

            [APIExtension(APIExtension.Properties)]
            public static implicit operator int(ListenHandle c) => c.Handle;
            [APIExtension(APIExtension.Properties)]
            public static implicit operator bool(ListenHandle c) => c.Handle >= 0;
        }

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "Channel")]
        public ScriptChannel GetChannel(int channel) => new ScriptChannel { Number = channel };

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "PublicChannel")]
        public ScriptChannel GetPublicChannel(int channel) => new ScriptChannel { Number = PUBLIC_CHANNEL };

        [APIExtension(APIExtension.Properties, APIUseAsEnum.Getter, "DebugChannel")]
        public ScriptChannel GetDebugChannel(int channel) => new ScriptChannel { Number = DEBUG_CHANNEL };

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Shout")]
        public void Shout(ScriptInstance instance, ScriptChannel channel, string message) =>
            Shout(instance, channel.Number, message);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Shout")]
        public void Shout(ScriptInstance instance, ListenHandle handle, string message) =>
            Shout(instance, handle.Channel, message);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Say")]
        public void Say(ScriptInstance instance, ScriptChannel channel, string message) =>
            Say(instance, channel.Number, message);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Say")]
        public void Say(ScriptInstance instance, ListenHandle handle, string message) =>
            Say(instance, handle.Channel, message);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Whisper")]
        public void Whisper(ScriptInstance instance, ScriptChannel channel, string message) =>
            Say(instance, channel.Number, message);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Whisper")]
        public void Whisper(ScriptInstance instance, ListenHandle handle, string message) =>
            Say(instance, handle.Channel, message);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "RegionSay")]
        public void RegionSay(ScriptInstance instance, ScriptChannel channel, string message) =>
            RegionSay(instance, channel.Number, message);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "RegionSay")]
        public void RegionSay(ScriptInstance instance, ListenHandle handle, string message) =>
            RegionSay(instance, handle.Channel, message);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "RegionSayTo")]
        public void RegionSayTo(ScriptInstance instance, ScriptChannel channel, LSLKey target, string message) =>
            RegionSayTo(instance, target, channel.Number, message);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "RegionSayTo")]
        public void RegionSayTo(ScriptInstance instance, ListenHandle handle, LSLKey target, string message) =>
            RegionSayTo(instance, target, handle.Channel, message);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Listen")]
        public ListenHandle Listen(ScriptInstance instance, ScriptChannel channel, string name, LSLKey id, string msg) => new ListenHandle
        {
            Channel = channel.Number,
            Handle = Listen(instance, channel.Number, name, id, msg)
        };

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Remove")]
        public void ListenRemove(ScriptInstance instance, ListenHandle handle) =>
            ListenRemove(instance, handle.Handle);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Enable")]
        public void ListenEnable(ScriptInstance instance, ListenHandle handle) =>
            ListenControl(instance, handle.Handle, 1);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Disable")]
        public void ListenDisable(ScriptInstance instance, ListenHandle handle) =>
            ListenControl(instance, handle.Handle, 0);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "ListenRegex")]
        public ListenHandle ListenRegex(ScriptInstance instance, ScriptChannel channel, string name, LSLKey id, string msg, int regexBitfield) => new ListenHandle
        {
            Channel = channel.Number,
            Handle = ListenRegex(instance, channel.Number, name, id, msg, regexBitfield)
        };

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Dialog")]
        [ForcedSleep(1)]
        public void Dialog(ScriptInstance instance, ScriptChannel channel, LSLKey avatar, string message, AnArray buttons) =>
            Dialog(instance, avatar, message, buttons, channel.Number);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Dialog")]
        [ForcedSleep(1)]
        public void Dialog(ScriptInstance instance, ListenHandle handle, LSLKey avatar, string message, AnArray buttons) =>
            Dialog(instance, avatar, message, buttons, handle.Channel);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "TextBox")]
        [ForcedSleep(1)]
        void TextBox(ScriptInstance instance, ScriptChannel channel, LSLKey avatar, string message) =>
            TextBox(instance, avatar, message, channel.Number);

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "TextBox")]
        [ForcedSleep(1)]
        void TextBox(ScriptInstance instance, ListenHandle handle, LSLKey avatar, string message) =>
            TextBox(instance, avatar, message, handle.Channel);
    }
}
