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
using SilverSim.Types;
using System.Xml.Serialization;

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

            [XmlIgnore]
            public int IsDebug => (Number == DEBUG_CHANNEL).ToLSLBoolean();
            [XmlIgnore]
            public int IsPublic => (Number == PUBLIC_CHANNEL).ToLSLBoolean();
        }

        [APIExtension(APIExtension.Properties, "listenhandle")]
        [APIDisplayName("listenhandle")]
        [APIIsVariableType]
        [APICloneOnAssignment]
        [ImplementsCustomTypecasts]
        public class ListenHandle
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

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Listen")]
        public ListenHandle Listen(ScriptInstance instance, ScriptChannel channel) => new ListenHandle
        {
            Channel = channel.Number,
            Handle = Listen(instance, channel.Number, string.Empty, UUID.Zero, string.Empty)
        };

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Remove")]
        public void ListenRemove(ScriptInstance instance, ListenHandle handle)
        {
            ListenRemove(instance, handle.Handle);
            handle.Handle = -1;
        }

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
