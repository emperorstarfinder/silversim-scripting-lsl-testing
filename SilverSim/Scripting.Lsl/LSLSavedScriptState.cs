// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace SilverSim.Scripting.Lsl
{
    public partial class Script
    {
        public class SavedScriptState : IScriptState
        {
            public Dictionary<string, object> Variables = new Dictionary<string, object>();
            public List<object> PluginData = new List<object>();
            public List<EventParams> EventData = new List<EventParams>();
            public bool IsRunning;
            public string CurrentState = "default";
            public double MinEventDelay;

            static void ScriptPermissionsFromXML(XmlTextReader reader, ObjectPartInventoryItem item)
            {
                for (; ; )
                {
                    if (!reader.Read())
                    {
                        throw new InvalidObjectXmlException();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.IsEmptyElement)
                            {
                                break;
                            }

                            switch (reader.Name)
                            {
                                case "mask":
                                    uint mask = (uint)reader.ReadContentAsLong();
                                    item.PermsGranter.PermsMask = (ScriptPermissions)mask;
                                    break;

                                case "granter":
                                    item.PermsGranter.PermsGranter.ID = reader.ReadContentAsUUID();
                                    break;

                                default:
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (reader.Name != "Permissions")
                            {
                                throw new InvalidObjectXmlException();
                            }
                            return;

                        default:
                            break;
                    }
                }
            }

            [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
            static void ListItemFromXml(XmlTextReader reader, AnArray array)
            {
                string type = string.Empty;
                string attrname = string.Empty;
                while (reader.ReadAttributeValue())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Attribute:
                            attrname = reader.Value;
                            break;

                        case XmlNodeType.Text:
                            switch (attrname)
                            {
                                case "type":
                                    type = reader.Value;
                                    break;

                                default:
                                    break;
                            }
                            break;

                        default:
                            break;
                    }
                }

                if (type.Length == 0)
                {
                    throw new InvalidObjectXmlException();
                }
                string vardata;

                switch (type)
                {
                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion":
                        vardata = reader.ReadElementValueAsString();
                        array.Add(Quaternion.Parse(vardata));
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector":
                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector3":
                        vardata = reader.ReadElementValueAsString();
                        array.Add(Vector3.Parse(vardata));
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger":
                        array.Add(reader.ReadElementValueAsInt());
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat":
                        array.Add(reader.ReadElementValueAsFloat());
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLString":
                        array.Add(reader.ReadElementValueAsString());
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+key":
                    case "OpenMetaverse.UUID":
                        array.Add(new LSLKey(reader.ReadElementValueAsString()));
                        break;

                    default:
                        throw new InvalidObjectXmlException();
                }
            }

            static AnArray ListFromXml(XmlTextReader reader)
            {
                AnArray array = new AnArray();
                for(;;)
                {
                    if(!reader.Read())
                    {
                        throw new InvalidObjectXmlException();
                    }

                    switch(reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if(reader.IsEmptyElement)
                            {
                                break;
                            }

                            switch(reader.Name)
                            {
                                case "ListItem":
                                    ListItemFromXml(reader, array);
                                    break;

                                default:
                                    reader.ReadToEndElement();
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if(reader.Name != "Variable")
                            {
                                throw new InvalidObjectXmlException();
                            }
                            return array;

                        default:
                            break;
                    }
                }
            }

            [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
            static void VariableFromXml(XmlTextReader reader, SavedScriptState state)
            {
                string type = string.Empty;
                string varname = string.Empty;
                if(reader.MoveToFirstAttribute())
                {
                    do
                    {
                        switch (reader.Name)
                        {
                            case "type":
                                type = reader.Value;
                                break;

                            case "name":
                                varname = reader.Value;
                                break;

                            default:
                                break;
                        }
                    } while (reader.MoveToNextAttribute());
                }

                if(varname.Length == 0 || type.Length == 0)
                {
                    throw new InvalidObjectXmlException();
                }
                string vardata;

                switch(type)
                {
                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion":
                        vardata = reader.ReadElementValueAsString();
                        state.Variables[varname] = Quaternion.Parse(vardata);
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector3":
                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector":
                        vardata = reader.ReadElementValueAsString();
                        state.Variables[varname] = Vector3.Parse(vardata);
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger":
                        state.Variables[varname] = reader.ReadElementValueAsInt();
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat":
                        state.Variables[varname] = reader.ReadElementValueAsFloat();
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLString":
                        state.Variables[varname] = reader.ReadElementValueAsString();
                        break;

                    case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+key":
                    case "OpenMetaverse.UUID":
                        state.Variables[varname] = new LSLKey(reader.ReadElementValueAsString());
                        break;

                    case "list":
                        state.Variables[varname] = ListFromXml(reader);
                        break;

                    default:
                        throw new InvalidObjectXmlException();
                }
            }
            static void VariablesFromXml(XmlTextReader reader, SavedScriptState state)
            {
                for(;;)
                {
                    if(!reader.Read())
                    {
                        throw new InvalidObjectXmlException();
                    }

                    switch(reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if(reader.IsEmptyElement)
                            {
                                break;
                            }

                            if (reader.Name == "Variable")
                            {
                                VariableFromXml(reader, state);
                            }
                            else
                            {
                                reader.ReadToEndElement();
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if(reader.Name != "Variables")
                            {
                                throw new InvalidObjectXmlException();
                            }
                            return;
                            
                        default:
                            break;
                    }
                }
            }

            public class EventParams
            {
                public string EventName = string.Empty;
                public List<object> Params = new List<object>();
                public List<DetectInfo> Detected = new List<DetectInfo>();

                public EventParams()
                {

                }
            }

            static EventParams EventFromXml(XmlTextReader reader)
            {
                if(reader.IsEmptyElement)
                {
                    throw new InvalidObjectXmlException();
                }
                string eventName = string.Empty;
                if (reader.MoveToFirstAttribute())
                {
                    do
                    {
                        switch (reader.Name)
                        {
                            case "event":
                                eventName = reader.Value;
                                break;

                            default:
                                break;
                        }
                    } while (reader.MoveToNextAttribute());
                }

                if (eventName.Length == 0)
                {
                    throw new InvalidObjectXmlException();
                }
                EventParams ev = new EventParams();

                for (;;)
                {
                    if (!reader.Read())
                    {
                        throw new InvalidObjectXmlException();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.Name)
                            {
                                case "Params":
                                    if (reader.IsEmptyElement)
                                    {
                                        throw new InvalidObjectXmlException();
                                    }
                                    ev.Params = ParamsFromXml(reader);
                                    break;

                                case "Detected":
                                    if (reader.IsEmptyElement)
                                    {
                                        break;
                                    }
                                    ev.Detected = DetectedFromXml(reader);
                                    break;

                                default:
                                    reader.ReadToEndElement();
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (reader.Name != "Item")
                            {
                                throw new InvalidObjectXmlException();
                            }
                            return ev;

                        default:
                            break;
                    }
                }
            }

            static DetectInfo DetectedObjectFromXml(XmlTextReader reader)
            {
                DetectInfo di = new DetectInfo();
                if (reader.MoveToFirstAttribute())
                {
                    do
                    {
                        switch (reader.Name)
                        {
                            case "pos":
                                di.GrabOffset = Vector3.Parse(reader.Value);
                                break;

                            case "linkNum":
                                di.LinkNumber = int.Parse(reader.Value);
                                break;

                            case "group":
                                di.Group = new UGI(reader.Value);
                                break;

                            case "name":
                                di.Name = reader.Value;
                                break;

                            case "owner":
                                di.Owner = new UUI(reader.Value);
                                break;

                            case "position":
                                di.Position = Vector3.Parse(reader.Value);
                                break;

                            case "rotation":
                                di.Rotation = Quaternion.Parse(reader.Value);
                                break;

                            case "type":
                                di.ObjType = (DetectedTypeFlags)int.Parse(reader.Value);
                                break;

                            case "velocity":
                                di.Velocity = Vector3.Parse(reader.Value);
                                break;

                                /* for whatever reason, OpenSim does not serialize the following */
                            case "touchst":
                                di.TouchST = Vector3.Parse(reader.Value);
                                break;

                            case "touchuv":
                                di.TouchUV = Vector3.Parse(reader.Value);
                                break;

                            case "touchbinormal":
                                di.TouchBinormal = Vector3.Parse(reader.Value);
                                break;

                            case "touchpos":
                                di.TouchPosition = Vector3.Parse(reader.Value);
                                break;

                            case "touchface":
                                di.TouchFace = int.Parse(reader.Value);
                                break;

                            default:
                                break;
                        }
                    } while (reader.MoveToNextAttribute());
                }

                di.Key = UUID.Parse(reader.ReadElementValueAsString("Object"));
                return di;
            }

            static List<DetectInfo> DetectedFromXml(XmlTextReader reader)
            {
                List<DetectInfo> res = new List<DetectInfo>();
                for (;;)
                {
                    if (!reader.Read())
                    {
                        throw new InvalidObjectXmlException();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.IsEmptyElement)
                            {
                                break;
                            }

                            switch (reader.Name)
                            {
                                case "Object":
                                    res.Add(DetectedObjectFromXml(reader));
                                    break;

                                default:
                                    reader.Skip();
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (reader.Name != "Detected")
                            {
                                throw new InvalidObjectXmlException();
                            }
                            return res;

                        default:
                            break;
                    }
                }
            }

            static List<object> ParamsFromXml(XmlTextReader reader)
            {
                List<object> res = new List<object>();
                for (;;)
                {
                    if (!reader.Read())
                    {
                        throw new InvalidObjectXmlException();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.IsEmptyElement)
                            {
                                break;
                            }

                            switch (reader.Name)
                            {
                                case "Param":
                                    res.Add(reader.ReadTypedValue());
                                    break;

                                default:
                                    reader.Skip();
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (reader.Name != "Params")
                            {
                                throw new InvalidObjectXmlException();
                            }
                            return res;

                        default:
                            break;
                    }
                }
            }

            static List<EventParams> EventsFromXml(XmlTextReader reader)
            {
                List<EventParams> events = new List<EventParams>();
                for (;;)
                {
                    if (!reader.Read())
                    {
                        throw new InvalidObjectXmlException();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.IsEmptyElement)
                            {
                                break;
                            }

                            switch (reader.Name)
                            {
                                case "Item":
                                    events.Add(EventFromXml(reader));
                                    break;

                                default:
                                    reader.ReadToEndElement();
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (reader.Name != "Queue")
                            {
                                throw new InvalidObjectXmlException();
                            }
                            return events;

                        default:
                            break;
                    }
                }
            }

            static List<object> PluginsFromXml(XmlTextReader reader)
            {
                List<object> res = new List<object>();
                for (;;)
                {
                    if (!reader.Read())
                    {
                        throw new InvalidObjectXmlException();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.IsEmptyElement)
                            {
                                break;
                            }

                            switch(reader.Name)
                            {
                                case "ListItem":
                                    res.Add(reader.ReadTypedValue());
                                    break;

                                default:
                                    reader.Skip();
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (reader.Name != "Plugins")
                            {
                                throw new InvalidObjectXmlException();
                            }
                            return res;

                        default:
                            break;
                    }
                }
            }

            static SavedScriptState ScriptStateFromXML(XmlTextReader reader, Dictionary<string, string> attrs, ObjectPartInventoryItem item)
            {
                SavedScriptState state = new SavedScriptState();
                for (; ;)
                {
                    if(!reader.Read())
                    {
                        throw new InvalidObjectXmlException();
                    }

                    switch(reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if(reader.IsEmptyElement)
                            {
                                break;
                            }

                            switch(reader.Name)
                            {
                                case "State":
                                    state.CurrentState = reader.ReadElementValueAsString();
                                    break;

                                case "Running":
                                    state.IsRunning = reader.ReadElementValueAsBoolean();
                                    break;

                                case "Variables":
                                    VariablesFromXml(reader, state);
                                    break;

                                case "Queue":
                                    state.EventData = EventsFromXml(reader);
                                    break;

                                case "Plugins":
                                    state.PluginData = PluginsFromXml(reader);
                                    break;

                                case "Permissions":
                                    ScriptPermissionsFromXML(reader, item);
                                    break;

                                case "MinEventDelay":
                                    state.MinEventDelay = reader.ReadElementValueAsDouble();
                                    break;

                                default:
                                    reader.ReadToEndElement();
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if(reader.Name != "ScriptState")
                            {
                                throw new InvalidObjectXmlException();
                            }
                            return state;

                        default:
                            break;
                    }
                }
            }

            public static SavedScriptState FromXML(XmlTextReader reader, Dictionary<string, string> attrs, ObjectPartInventoryItem item)
            {
                SavedScriptState state = new SavedScriptState();
                for (; ; )
                {
                    if (!reader.Read())
                    {
                        throw new InvalidObjectXmlException();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.IsEmptyElement)
                            {
                                break;
                            }

                            switch (reader.Name)
                            {
                                case "ScriptState":
                                    state = ScriptStateFromXML(reader, attrs, item);
                                    break;

                                default:
                                    reader.ReadToEndElement();
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (reader.Name != "State")
                            {
                                throw new InvalidObjectXmlException();
                            }
                            return state;

                        default:
                            break;
                    }
                }
            }

            public static SavedScriptState FromXML(XmlTextReader reader, ObjectPartInventoryItem item)
            {
                SavedScriptState state = new SavedScriptState();
                for (;;)
                {
                    if (!reader.Read())
                    {
                        throw new InvalidObjectXmlException();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.IsEmptyElement)
                            {
                                break;
                            }

                            switch (reader.Name)
                            {
                                case "State":
                                    Dictionary<string, string> attrs = new Dictionary<string, string>();
                                    if (reader.MoveToFirstAttribute())
                                    {
                                        do
                                        {
                                            attrs[reader.Name] = reader.Value;
                                        } while (reader.MoveToNextAttribute());
                                    }
                                    if(reader.IsEmptyElement)
                                    {
                                        break;
                                    }

                                    state = ScriptStateFromXML(reader, attrs, item);
                                    break;

                                default:
                                    reader.ReadToEndElement();
                                    break;
                            }
                            break;

                        default:
                            break;
                    }
                }
            }

            public void ToXml(XmlTextWriter writer)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
