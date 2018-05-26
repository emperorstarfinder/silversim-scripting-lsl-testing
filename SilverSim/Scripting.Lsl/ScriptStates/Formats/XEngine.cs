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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using EventParams = SilverSim.Scripting.Lsl.ScriptStates.ScriptState.EventParams;

namespace SilverSim.Scripting.Lsl.ScriptStates.Formats
{
    public static class XEngine
    {
        #region Serialization entry
        public static void Serialize(XmlTextWriter writer, ScriptState state)
        {
            writer.WriteStartElement("State");
            writer.WriteAttributeString("UUID", state.ItemID.ToString());
            writer.WriteAttributeString("Asset", state.AssetID.ToString());
            writer.WriteAttributeString("Engine", "XEngine");
            {
                writer.WriteStartElement("ScriptState");
                {
                    writer.WriteStartElement("State");
                    {
                        writer.WriteValue(state.CurrentState);
                    }
                    writer.WriteEndElement();
                    writer.WriteStartElement("Running");
                    {
                        writer.WriteValue(state.IsRunning);
                    }
                    writer.WriteEndElement();
                    writer.WriteStartElement("StartParameter");
                    {
                        writer.WriteValue(state.StartParameter);
                    }
                    writer.WriteEndElement();
                    writer.WriteStartElement("Variables");
                    foreach (KeyValuePair<string, object> kvp in state.Variables)
                    {
                        string varName = kvp.Key;
                        object varValue = kvp.Value;
                        Type varType = varValue.GetType();
                        if (varType == typeof(int))
                        {
                            writer.WriteStartElement("Variable");
                            writer.WriteAttributeString("name", varName);
                            writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger");
                            writer.WriteValue(varValue.ToString());
                        }
                        else if (varType == typeof(char))
                        {
                            writer.WriteStartElement("Variable");
                            writer.WriteAttributeString("name", varName);
                            writer.WriteAttributeString("type", "char");
                            writer.WriteValue(((int)varValue).ToString());
                        }
                        else if (varType == typeof(long))
                        {
                            writer.WriteStartElement("Variable");
                            writer.WriteAttributeString("name", varName);
                            writer.WriteAttributeString("type", "long");
                            writer.WriteValue(varValue.ToString());
                        }
                        else if (varType == typeof(double))
                        {
                            writer.WriteStartElement("Variable");
                            writer.WriteAttributeString("name", varName);
                            writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat");
                            writer.WriteValue(LSLCompiler.TypecastDoubleToString((double)varValue));
                        }
                        else if (varType == typeof(Vector3))
                        {
                            writer.WriteStartElement("Variable");
                            writer.WriteAttributeString("name", varName);
                            writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector3");
                            writer.WriteValue(LSLCompiler.TypecastVectorToString6Places((Vector3)varValue));
                        }
                        else if (varType == typeof(Quaternion))
                        {
                            writer.WriteStartElement("Variable");
                            writer.WriteAttributeString("name", varName);
                            writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion");
                            writer.WriteValue(LSLCompiler.TypecastRotationToString6Places((Quaternion)varValue));
                        }
                        else if (varType == typeof(UUID) || varType == typeof(LSLKey) || varType == typeof(string))
                        {
                            writer.WriteStartElement("Variable");
                            writer.WriteAttributeString("name", varName);
                            writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLString");
                            writer.WriteValue(varValue.ToString());
                        }
                        else if (varType == typeof(AnArray))
                        {
                            writer.ListToXml("Variable", varName, (AnArray)varValue);
                            continue;
                        }
                        else if (Attribute.GetCustomAttribute(varType, typeof(SerializableAttribute)) != null)
                        {
                            byte[] data;
                            try
                            {
                                using (var ms = new MemoryStream())
                                {
                                    using (XmlTextWriter innerWriter = ms.UTF8XmlTextWriter())
                                    {
                                        var formatter = new XmlSerializer(varValue.GetType());
                                        formatter.Serialize(innerWriter, varValue);
                                    }
                                    data = ms.ToArray();
                                }
                            }
                            catch
                            {
                                continue;
                            }
                            writer.WriteStartElement("Variable");
                            writer.WriteAttributeString("name", varName);
                            writer.WriteAttributeString("type", varType.FullName);
                            writer.WriteValue(Convert.ToBase64String(data));
                        }
                        else
                        {
                            continue;
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteStartElement("Queue");
                    foreach (IScriptEvent ev in state.EventData)
                    {
                        Type evType = ev.GetType();
                        ScriptState.EventParams eventParams = null;
                        if (evType == typeof(MessageObjectEvent))
                        {
                            if (state.UseMessageObjectEvent)
                            {
                                eventParams = ScriptState.ObjectMessageSerializer(ev);
                            }
                            else
                            {
                                eventParams = ScriptState.ObjectMessageDataserverSerializer(ev);
                            }
                        }
                        else
                        {
                            ScriptState.TryTranslateEvent(ev, out eventParams);
                        }
                        if(eventParams != null)
                        {
                            writer.WriteStartElement("Item");
                            writer.WriteAttributeString("event", eventParams.EventName);
                            writer.WriteStartElement("Params");
                            foreach(object o in eventParams.Params)
                            {
                                writer.WriteTypedValue("Param", o);
                            }
                            writer.WriteEndElement();
                            writer.WriteStartElement("Detected");
                            foreach (DetectInfo d in eventParams.Detected)
                            {
                                writer.WriteStartElement("Object");
                                writer.WriteAttributeString("pos", d.GrabOffset.ToString());
                                writer.WriteAttributeString("linkNum", d.LinkNumber.ToString());
                                writer.WriteAttributeString("group", d.Group.ToString());
                                writer.WriteAttributeString("name", d.Name);
                                writer.WriteAttributeString("owner", d.Owner.ToString());
                                writer.WriteAttributeString("position", d.Position.ToString());
                                writer.WriteAttributeString("rotation", d.Rotation.ToString());
                                writer.WriteAttributeString("type", ((int)d.ObjType).ToString());
                                writer.WriteAttributeString("velocity", d.Velocity.ToString());

                                /* for whatever reason, OpenSim does not serialize the following */
                                writer.WriteAttributeString("touchst", d.TouchST.ToString());
                                writer.WriteAttributeString("touchuv", d.TouchUV.ToString());
                                writer.WriteAttributeString("touchbinormal", d.TouchBinormal.ToString());
                                writer.WriteAttributeString("touchpos", d.TouchPosition.ToString());
                                writer.WriteAttributeString("touchface", d.TouchFace.ToString());
                                writer.WriteValue(d.Key.ToString());
                                writer.WriteEndElement();
                            }
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteEndElement();
                    {
                        ObjectPartInventoryItem.PermsGranterInfo grantInfo = state.PermsGranter;
                        if (grantInfo.PermsGranter.ID != UUID.Zero)
                        {
                            writer.WriteStartElement("Permissions");
                            writer.WriteAttributeString("mask", ((uint)grantInfo.PermsMask).ToString());
                            writer.WriteAttributeString("granter", grantInfo.PermsGranter.ID.ToString());
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteStartElement("Plugins");
                    foreach (object o in state.PluginData)
                    {
                        writer.WriteTypedValue("ListItem", o);
                    }
                    writer.WriteEndElement();
                    writer.WriteNamedValue("MinEventDelay", state.MinEventDelay);
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        #endregion

        #region Serialization
        private static void ListToXml(this XmlTextWriter writer, string tagname, string name, AnArray array)
        {
            writer.WriteStartElement(tagname);
            if (name?.Length != 0)
            {
                writer.WriteAttributeString("name", name);
            }
            writer.WriteAttributeString("type", "list");
            foreach (IValue val in array)
            {
                Type valtype = val.GetType();
                if (valtype == typeof(Integer))
                {
                    writer.WriteStartElement("ListItem");
                    writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger");
                    writer.WriteValue(val.AsInt);
                    writer.WriteEndElement();
                }
                else if (valtype == typeof(Real))
                {
                    writer.WriteStartElement("ListItem");
                    writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat");
                    double v = (Real)val;
                    writer.WriteValue(LSLCompiler.TypecastDoubleToString(v));
                    writer.WriteEndElement();
                }
                else if (valtype == typeof(Quaternion))
                {
                    writer.WriteStartElement("ListItem");
                    writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion");
                    writer.WriteValue(LSLCompiler.TypecastRotationToString6Places((Quaternion)val));
                    writer.WriteEndElement();
                }
                else if (valtype == typeof(Vector3))
                {
                    writer.WriteStartElement("ListItem");
                    writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector3");
                    writer.WriteValue(LSLCompiler.TypecastVectorToString6Places((Vector3)val));
                    writer.WriteEndElement();
                }
                else if (valtype == typeof(AString))
                {
                    writer.WriteStartElement("ListItem");
                    writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLString");
                    writer.WriteValue(val.ToString());
                    writer.WriteEndElement();
                }
                else if (valtype == typeof(UUID) || valtype == typeof(LSLKey))
                {
                    writer.WriteStartElement("ListItem");
                    writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+key");
                    writer.WriteValue(val.ToString());
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement();
        }

        private static void WriteTypedValue(this XmlTextWriter writer, string tagname, object o)
        {
            Type type = o.GetType();
            if (type == typeof(bool))
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", "System.Boolean");
                writer.WriteValue(o.ToString());
            }
            else if (type == typeof(double))
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", "System.Single");
                writer.WriteValue(((float)(double)o).ToString(CultureInfo.InvariantCulture));
            }
            else if (type == typeof(string))
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", "System.String");
                writer.WriteValue(o.ToString());
            }
            else if (type == typeof(int))
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", "System.Int32");
                writer.WriteValue(o.ToString());
            }
            else if (type == typeof(long))
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", "System.Int64");
                writer.WriteValue(o.ToString());
            }
            else if (type == typeof(Vector3))
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector3");
                writer.WriteValue(o.ToString());
            }
            else if (type == typeof(Quaternion))
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion");
                writer.WriteValue(o.ToString());
            }
            else if (type == typeof(LSLKey))
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", "OpenSim.Region.ScriptEngine.Shared.LSL_Types+key");
                writer.WriteValue(o.ToString());
            }
            else if (type == typeof(UUID))
            {
                writer.WriteStartElement(tagname);
                /* people want compatibility with their creations so we have to understand to what
                 * these types map in the OpenSim serialization.
                 */
                writer.WriteAttributeString("type", "OpenMetaverse.UUID");
                writer.WriteValue(o.ToString());
            }
            else if (type == typeof(AnArray))
            {
                writer.ListToXml(tagname, null, (AnArray)o);
            }
            else if (LSLCompiler.KnownSerializationTypes.ContainsValue(type) &&
                Attribute.GetCustomAttribute(type, typeof(SerializableAttribute)) != null)
            {
                writer.WriteStartElement(tagname);
                writer.WriteAttributeString("type", type.FullName);
                using (var ms = new MemoryStream())
                {
                    var formatter = new XmlSerializer(type);
                    formatter.Serialize(ms, o);
                    writer.WriteValue(Convert.ToBase64String(ms.ToArray()));
                }
                writer.WriteEndElement();
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(o));
            }
            writer.WriteEndElement();
        }
        #endregion

        #region Deserialization entry
        public static ScriptState Deserialize(XmlTextReader reader, Dictionary<string, string> attrs)
        {
            var state = new ScriptState();
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
                                state = ScriptStateFromXML(reader, attrs);
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
                }
            }
        }
        #endregion

        #region Deserialization
        private static object ReadTypedValue(this XmlTextReader reader)
        {
            string type = string.Empty;
            string tagName = reader.Name;
            bool isEmptyElement = reader.IsEmptyElement;
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    switch (reader.Name)
                    {
                        case "type":
                            type = reader.Value;
                            break;

                        default:
                            break;
                    }
                } while (reader.MoveToNextAttribute());
            }

            if(type == "list")
            {
                return ListFromXml(reader, "Param", isEmptyElement);
            }
            string data = string.Empty;
            if (!isEmptyElement)
            {
                data = reader.ReadElementValueAsString(tagName);
            }
            switch (type)
            {
                case "System.Boolean":
                    return bool.Parse(data);

                case "System.Single":
                case "System.Double":
                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat":
                    return double.Parse(data, NumberStyles.Float, CultureInfo.InvariantCulture);

                case "System.String":
                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLString":
                    return data;

                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+key":
                    return new LSLKey(data);

                case "System.Int32":
                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger":
                    return int.Parse(data);

                case "System.Int64":
                    return long.Parse(data);

                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector":
                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector3":
                    return Vector3.Parse(data);

                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion":
                    return Quaternion.Parse(data);

                case "OpenMetaverse.UUID":
                    return UUID.Parse(data);

                default:
                    Type customType;
                    if (LSLCompiler.KnownSerializationTypes.TryGetValue(type, out customType) &&
                        Attribute.GetCustomAttribute(customType, typeof(SerializableAttribute)) != null)
                    {
                        try
                        {
                            using (var ms = new MemoryStream(Convert.FromBase64String(reader.ReadElementValueAsString())))
                            {
                                var formatter = new XmlSerializer(customType);
                                return formatter.Deserialize(ms);
                            }
                        }
                        catch
                        {
                            /* deserialization not possible */
                        }
                    }
                    throw new ArgumentException("Unknown type \"" + type + "\" in serialization");
            }
        }

        private static void ScriptPermissionsFromXML(ScriptState state, XmlTextReader reader)
        {
            string attrname = string.Empty;
            bool isEmptyElement = reader.IsEmptyElement;
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
                            case "granter":
                                state.PermsGranter.PermsGranter.ID = reader.ReadContentAsUUID();
                                break;

                            case "mask":
                                var mask = (uint)reader.ReadElementValueAsLong();
                                state.PermsGranter.PermsMask = (ScriptPermissions)mask;
                                break;
                        }
                        break;
                }
            }

            if (isEmptyElement)
            {
                return;
            }

            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        reader.ReadToEndElement();
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "Permissions")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return;
                }
            }
        }

        private static void ListItemFromXml(XmlTextReader reader, AnArray array, bool isEmptyElement)
        {
            string type = string.Empty;
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    switch (reader.Name)
                    {
                        case "type":
                            type = reader.Value;
                            break;
                    }
                }
                while (reader.MoveToNextAttribute());
            }

            if (type.Length == 0)
            {
                throw new InvalidObjectXmlException("Missing type for element of list variable");
            }

            switch (type)
            {
                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion":
                    array.Add(LSLCompiler.ParseStringToQuaternion(reader.ReadElementValueAsString("ListItem")));
                    break;

                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector":
                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector3":
                    array.Add(LSLCompiler.ParseStringToVector(reader.ReadElementValueAsString("ListItem")));
                    break;

                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger":
                case "System.Int32":
                    array.Add(reader.ReadElementValueAsInt("ListItem"));
                    break;

                case "System.Int64":
                    array.Add((IValue)new LongInteger(reader.ReadElementValueAsLong("ListItem")));
                    break;

                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat":
                case "System.Double":
                case "System.Single":
                    array.Add(reader.ReadElementValueAsDouble("ListItem"));
                    break;

                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLString":
                case "System.String":
                    array.Add(isEmptyElement ? string.Empty : reader.ReadElementValueAsString("ListItem"));
                    break;

                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+key":
                case "OpenMetaverse.UUID":
                    array.Add(isEmptyElement ? new LSLKey(string.Empty) : new LSLKey(reader.ReadElementValueAsString("ListItem")));
                    break;

                case "long":
                    array.Add((IValue)new LongInteger(isEmptyElement ? 0 : long.Parse(reader.ReadElementValueAsString("ListItem"))));
                    break;

                default:
                    throw new InvalidObjectXmlException("Unknown type \"" + type + "\" encountered");
            }
        }

        private static AnArray ListFromXml(XmlTextReader reader, string tagname, bool isEmptyElement)
        {
            var array = new AnArray();
            if (isEmptyElement)
            {
                return array;
            }
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException("Error at parsing list data");
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "ListItem":
                                ListItemFromXml(reader, array, reader.IsEmptyElement);
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != tagname)
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return array;

                    default:
                        break;
                }
            }
        }
        
        private static void VariableFromXml(ScriptState state, XmlTextReader reader)
        {
            string type = string.Empty;
            string varname = string.Empty;
            bool isEmptyElement = reader.IsEmptyElement;
            if (reader.MoveToFirstAttribute())
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

            if (varname.Length == 0 || type.Length == 0)
            {
                throw new InvalidObjectXmlException();
            }
            string vardata;

            switch (type)
            {
                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Quaternion":
                    vardata = reader.ReadElementValueAsString("Variable");
                    state.Variables[varname] = Quaternion.Parse(vardata);
                    break;

                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector3":
                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+Vector":
                    vardata = reader.ReadElementValueAsString("Variable");
                    state.Variables[varname] = Vector3.Parse(vardata);
                    break;

                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLInteger":
                    state.Variables[varname] = reader.ReadElementValueAsInt("Variable");
                    break;

                case "long":
                    state.Variables[varname] = reader.ReadElementValueAsLong("Variable");
                    break;

                case "char":
                    state.Variables[varname] = (char)reader.ReadElementValueAsInt("Variable");
                    break;

                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLFloat":
                    state.Variables[varname] = reader.ReadElementValueAsDouble("Variable");
                    break;

                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+LSLString":
                    state.Variables[varname] = isEmptyElement ? string.Empty : reader.ReadElementValueAsString("Variable");
                    break;

                case "OpenSim.Region.ScriptEngine.Shared.LSL_Types+key":
                case "OpenMetaverse.UUID":
                    state.Variables[varname] = isEmptyElement ? new LSLKey(string.Empty) : new LSLKey(reader.ReadElementValueAsString("Variable"));
                    break;

                case "list":
                    state.Variables[varname] = ListFromXml(reader, "Variable", isEmptyElement);
                    break;

                default:
                    Type customType;
                    if (LSLCompiler.KnownSerializationTypes.TryGetValue(type, out customType) &&
                        Attribute.GetCustomAttribute(customType, typeof(SerializableAttribute)) != null)
                    {
                        try
                        {
                            using (var ms = new MemoryStream(Convert.FromBase64String(reader.ReadElementValueAsString())))
                            {
                                var formatter = new XmlSerializer(customType);
                                state.Variables[varname] = formatter.Deserialize(ms);
                            }
                        }
                        catch
                        {
                            /* deserialization not possible */
                        }
                    }
                    else
                    {
                        throw new InvalidObjectXmlException();
                    }
                    break;
            }
        }

        private static void VariablesFromXml(ScriptState state, XmlTextReader reader)
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

                        if (reader.Name == "Variable")
                        {
                            VariableFromXml(state, reader);
                        }
                        else
                        {
                            reader.ReadToEndElement();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "Variables")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return;

                    default:
                        break;
                }
            }
        }

        private static EventParams EventFromXml(XmlTextReader reader)
        {
            if (reader.IsEmptyElement)
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
            var ev = new EventParams
            {
                EventName = eventName
            };

            for (; ; )
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
                                if (!reader.IsEmptyElement)
                                {
                                    ev.Params = ParamsFromXml(reader);
                                }
                                break;

                            case "Detected":
                                if (!reader.IsEmptyElement)
                                {
                                    ev.Detected = DetectedFromXml(reader);
                                }
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
                }
            }
        }

        private static DetectInfo DetectedObjectFromXml(XmlTextReader reader)
        {
            var di = new DetectInfo();
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
                            di.Owner = new UGUI(reader.Value);
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
                    }
                } while (reader.MoveToNextAttribute());
            }

            di.Key = UUID.Parse(reader.ReadElementValueAsString("Object"));
            return di;
        }

        private static List<DetectInfo> DetectedFromXml(XmlTextReader reader)
        {
            var res = new List<DetectInfo>();
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
                }
            }
        }

        private static List<object> ParamsFromXml(XmlTextReader reader)
        {
            var res = new List<object>();
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
                }
            }
        }

        private static IScriptEvent[] EventsFromXml(XmlTextReader reader)
        {
            var events = new List<IScriptEvent>();
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
                            case "Item":
                                IScriptEvent ev;
                                if (ScriptState.TryTranslateEventParams(EventFromXml(reader), out ev))
                                {
                                    events.Add(ev);
                                }
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
                        return events.ToArray();
                }
            }
        }

        private static List<object> PluginsFromXml(XmlTextReader reader)
        {
            var res = new List<object>();
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
                            if (reader.Name == "ListItem")
                            {
                                res.Add(reader.ReadTypedValue());
                            }
                            break;
                        }

                        switch (reader.Name)
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
                }
            }
        }

        private static ScriptState ScriptStateFromXML(XmlTextReader reader, Dictionary<string, string> attrs)
        {
            var state = new ScriptState();
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
                            case "State":
                                state.CurrentState = reader.ReadElementValueAsString();
                                break;

                            case "Running":
                                state.IsRunning = reader.ReadElementValueAsBoolean();
                                break;

                            case "StartParameter":
                                state.StartParameter = reader.ReadElementValueAsInt();
                                break;

                            case "Variables":
                                VariablesFromXml(state, reader);
                                break;

                            case "Queue":
                                state.EventData = EventsFromXml(reader);
                                break;

                            case "Plugins":
                                state.PluginData = PluginsFromXml(reader);
                                break;

                            case "Permissions":
                                ScriptPermissionsFromXML(state, reader);
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
                        if (reader.Name != "ScriptState")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return state;
                }
            }
        }

        #endregion
    }
}
