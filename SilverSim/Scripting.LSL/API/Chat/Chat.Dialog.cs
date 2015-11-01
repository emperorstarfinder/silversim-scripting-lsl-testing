// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Chat
{
    public partial class ChatApi
    {
        [APILevel(APIFlags.LSL, "llDialog")]
        [ForcedSleep(1)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void Dialog(ScriptInstance instance, LSLKey avatar, string message, AnArray buttons, int channel)
        {
            lock (instance)
            {
                if (message.Length > 511)
                {
                    throw new ArgumentException("Message more than 511 characters");
                }
                else if(message.Length == 0)
                {
                    throw new ArgumentException("Message is empty");
                }
                else if(buttons.Count > 12)
                {
                    throw new ArgumentException("Too many buttons");
                }
                else if(buttons.Count == 0)
                {
                    throw new ArgumentException("At least one button must be defined");
                }
                SilverSim.Viewer.Messages.Script.ScriptDialog m = new SilverSim.Viewer.Messages.Script.ScriptDialog();
                m.Message = message.Substring(0, 256);
                m.ObjectID = instance.Part.ObjectGroup.ID;
                m.ImageID = UUID.Zero;
                m.ObjectName = instance.Part.ObjectGroup.Name;
                m.FirstName = instance.Part.ObjectGroup.Owner.FirstName;
                m.LastName = instance.Part.ObjectGroup.Owner.LastName;
                m.ChatChannel = channel;
                for (int c = 0; c < buttons.Count && c < 12; ++c )
                {
                    string buttontext = buttons[c].ToString();
                    if (buttontext.Length == 0)
                    {
                        throw new ArgumentException("button label cannot be blank");
                    }
                    else if (buttontext.Length > 24)
                    {
                        throw new ArgumentException("button label cannot be more than 24 characters");
                    }
                    m.Buttons.Add(buttontext);
                }

                m.OwnerData.Add(instance.Part.ObjectGroup.Owner.ID);

                try
                {
                    instance.Part.ObjectGroup.Scene.Agents[avatar].SendMessageAlways(m, instance.Part.ObjectGroup.Scene.ID);
                }
                catch
                {

                }
            }
        }

        [APILevel(APIFlags.LSL, "llTextBox")]
        [ForcedSleep(1)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void TextBox(ScriptInstance instance, LSLKey avatar, string message, int channel)
        {
            AnArray buttons = new AnArray();
            buttons.Add("!!llTextBox!!");
            Dialog(instance, avatar, message, buttons, channel);
        }

        [APILevel(APIFlags.LSL, "llLoadURL")]
        [ForcedSleep(10)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void LoadURL(ScriptInstance instance, LSLKey avatar, string message, string url)
        {
            lock (instance)
            {
                SilverSim.Viewer.Messages.Script.LoadURL m = new Viewer.Messages.Script.LoadURL();
                m.ObjectName = instance.Part.ObjectGroup.Name;
                m.ObjectID = instance.Part.ObjectGroup.ID;
                m.OwnerID = instance.Part.ObjectGroup.Owner.ID;
                m.Message = message;
                m.URL = url;

                try
                {
                    instance.Part.ObjectGroup.Scene.Agents[avatar].SendMessageAlways(m, instance.Part.ObjectGroup.Scene.ID);
                }
                catch
                {

                }
            }
        }

        [APILevel(APIFlags.LSL, "llMapDestination")]
        [ForcedSleep(1)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void MapDestination(ScriptInstance instance, string simname, Vector3 pos, Vector3 look_at)
        {
            lock(instance)
            {
                Script script = (Script)instance;

                foreach (DetectInfo detinfo in script.m_Detected)
                {
                    try
                    {
                        SilverSim.Viewer.Messages.Script.ScriptTeleportRequest m = new Viewer.Messages.Script.ScriptTeleportRequest();
                        m.ObjectName = instance.Part.ObjectGroup.Name;
                        m.SimName = simname;
                        m.SimPosition = pos;
                        m.LookAt = look_at;

                        instance.Part.ObjectGroup.Scene.Agents[detinfo.Object.ID].SendMessageAlways(m, instance.Part.ObjectGroup.Scene.ID);
                    }
                    catch
                    {

                    }
                }
            }
        }
    }
}
