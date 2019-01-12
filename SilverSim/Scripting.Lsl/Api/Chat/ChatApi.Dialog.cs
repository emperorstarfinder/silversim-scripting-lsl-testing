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

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Viewer.Messages.Script;

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
                UGUIWithName groupOwner = thisGroup.Scene.AvatarNameService.ResolveName(thisGroup.Owner);
                if (message.Length > 511)
                {
                    throw new LocalizedScriptErrorException(this, "MessageMoreThan511Characters", "Message more than 511 characters");
                }
                else if(message.Length == 0)
                {
                    throw new LocalizedScriptErrorException(this, "MessageIsEmpty", "Message is empty");
                }
                else if(buttons.Count > 12)
                {
                    throw new LocalizedScriptErrorException(this, "TooManyButtons", "Too many buttons");
                }
                else if(buttons.Count == 0)
                {
                    if (((Script)instance).AllowEmptyDialogList)
                    {
                        buttons = new AnArray { { "OK" } };
                    }
                    else
                    {
                        throw new LocalizedScriptErrorException(this, "AtLeastOneButtonMustBeDefined", "At least one button must be defined");
                    }
                }

                SceneInterface thisScene = thisGroup.Scene;
                IAgent agent;
                if (thisScene.Agents.TryGetValue(avatar, out agent))
                {
                    var m = new ScriptDialog
                    {
                        Message = message.TrimToMaxLength(256),
                        ObjectID = thisGroup.ID,
                        ImageID = UUID.Zero,
                        ObjectName = thisGroup.Name,
                        FirstName = groupOwner.FirstName,
                        LastName = groupOwner.LastName,
                        ChatChannel = channel
                    };
                    for (int c = 0; c < buttons.Count && c < 12; ++c)
                    {
                        string buttontext = buttons[c].ToString();
                        if (buttontext.Length == 0)
                        {
                            throw new LocalizedScriptErrorException(this, "ButtonLabelCannotBeBlank", "button label cannot be blank");
                        }
                        else if (buttontext.Length > 24)
                        {
                            buttontext = buttontext.Substring(0, 24);
                        }
                        m.Buttons.Add(buttontext);
                    }

                    m.OwnerData.Add(groupOwner.ID);

                    agent.SendMessageAlways(m, thisScene.ID);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llTextBox")]
        [ForcedSleep(1)]
        public void TextBox(ScriptInstance instance, LSLKey avatar, string message, int channel)
        {
            var buttons = new AnArray
            {
                "!!llTextBox!!"
            };
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
                IAgent agent;
                if(thisScene.Agents.TryGetValue(avatar, out agent))
                {
                    agent.SendMessageAlways(new LoadURL
                    {
                        ObjectName = thisGroup.Name,
                        ObjectID = thisGroup.ID,
                        OwnerID = thisGroup.Owner.ID,
                        Message = message.TrimToMaxLength(256),
                        URL = url
                    }, thisScene.ID);
                }
            }
        }

        [APILevel(APIFlags.LSL, "llMapDestination")]
        [ForcedSleep(1)]
        public void MapDestination(ScriptInstance instance, string simname, Vector3 pos, Vector3 look_at)
        {
            lock(instance)
            {
                var script = (Script)instance;
                ObjectGroup thisGroup = instance.Part.ObjectGroup;
                SceneInterface thisScene = thisGroup.Scene;

                foreach (DetectInfo detinfo in script.m_Detected)
                {
                    IAgent agent;
                    if (thisScene.Agents.TryGetValue(detinfo.Key, out agent))
                    {
                        agent.SendMessageAlways(new ScriptTeleportRequest
                        {
                            ObjectName = thisGroup.Name,
                            SimName = simname,
                            SimPosition = pos,
                            LookAt = look_at
                        }, thisScene.ID);
                    }
                }
            }
        }
    }
}
