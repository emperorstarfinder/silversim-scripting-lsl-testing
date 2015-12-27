// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.Lsl.Api.Chat
{
    public partial class ChatApi
    {
        [APILevel(APIFlags.LSL, "llDialog")]
        [ForcedSleep(1)]
        public void Dialog(ScriptInstance instance, LSLKey avatar, string message, AnArray buttons, int channel)
        {
            lock (instance)
            {
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                UUI groupOwner = thisGroup.Owner;
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
                m.ObjectID = thisGroup.ID;
                m.ImageID = UUID.Zero;
                m.ObjectName = thisGroup.Name;
                m.FirstName = groupOwner.FirstName;
                m.LastName = groupOwner.LastName;
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
                        buttontext = buttontext.Substring(0, 24);
                    }
                    m.Buttons.Add(buttontext);
                }

                m.OwnerData.Add(groupOwner.ID);

                SceneInterface thisScene = thisGroup.Scene;
                IAgent agent;
                if(thisScene.Agents.TryGetValue(avatar, out agent))
                {
                    agent.SendMessageAlways(m, thisScene.ID);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llTextBox")]
        [ForcedSleep(1)]
        public void TextBox(ScriptInstance instance, LSLKey avatar, string message, int channel)
        {
            AnArray buttons = new AnArray();
            buttons.Add("!!llTextBox!!");
            Dialog(instance, avatar, message, buttons, channel);
        }

        [APILevel(APIFlags.LSL, "llLoadURL")]
        [ForcedSleep(10)]
        public void LoadURL(ScriptInstance instance, LSLKey avatar, string message, string url)
        {
            lock (instance)
            {
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                SceneInterface thisScene = thisGroup.Scene;
                SilverSim.Viewer.Messages.Script.LoadURL m = new Viewer.Messages.Script.LoadURL();
                m.ObjectName = thisGroup.Name;
                m.ObjectID = thisGroup.ID;
                m.OwnerID = thisGroup.Owner.ID;
                m.Message = message;
                m.URL = url;

                try
                {
                    thisScene.Agents[avatar].SendMessageAlways(m, thisScene.ID);
                }
                catch
                {

                }
            }
        }

        [APILevel(APIFlags.LSL, "llMapDestination")]
        [ForcedSleep(1)]
        public void MapDestination(ScriptInstance instance, string simname, Vector3 pos, Vector3 look_at)
        {
            lock(instance)
            {
                Script script = (Script)instance;
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                SceneInterface thisScene = thisGroup.Scene;

                foreach (DetectInfo detinfo in script.m_Detected)
                {
                    try
                    {
                        Viewer.Messages.Script.ScriptTeleportRequest m = new Viewer.Messages.Script.ScriptTeleportRequest();
                        m.ObjectName = thisGroup.Name;
                        m.SimName = simname;
                        m.SimPosition = pos;
                        m.LookAt = look_at;

                        thisScene.Agents[detinfo.Key].SendMessageAlways(m, thisScene.ID);
                    }
                    catch
                    {

                    }
                }
            }
        }
    }
}
