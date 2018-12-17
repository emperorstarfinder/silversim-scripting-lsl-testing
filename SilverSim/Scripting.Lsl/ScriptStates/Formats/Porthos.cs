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
using SilverSim.Types.StructuredData.TLV;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using EventParams = SilverSim.Scripting.Lsl.ScriptStates.ScriptState.EventParams;

namespace SilverSim.Scripting.Lsl.ScriptStates.Formats
{
    public static class Porthos
    {
        private enum TlvIds : ushort
        {
            /* do not remove elements here */
            CurrentState = 1,
            IsRunning = 2,
            StartParameter = 3,
            Queue = 4,
            EventName = 5,
            EventParam = 6,
            DetectedInfo = 7,
            DetectedInfo_GrabOffset = 8,
            DetectedInfo_LinkNum = 9,
            DetectedInfo_Group = 10,
            DetectedInfo_Name = 11,
            DetectedInfo_Owner = 12,
            DetectedInfo_Position = 13,
            DetectedInfo_Rotation = 14,
            DetectedInfo_Type = 15,
            DetectedInfo_Velocity = 16,
            DetectedInfo_TouchST = 17,
            DetectedInfo_TouchUV = 18,
            DetectedInfo_TouchBinormal = 19,
            DetectedInfo_TouchPos = 20,
            DetectedInfo_TouchFace = 21,
            DetectedInfo_Key = 22,
            PermsGranter_Mask = 23,
            PermsGranter_Granter = 24,
            PluginData = 25,
            MinEventDelay = 26,
            VariableName = 27,
            DataValue = 29,
            Array = 30,
            XmlSerialization = 31,
            LSLKeyValue = 32,
        }

        #region Serialization entry
        public static void Serialize(XmlTextWriter writer, ScriptState state)
        {
            writer.WriteStartElement("State");
            writer.WriteAttributeString("UUID", state.ItemID.ToString());
            writer.WriteAttributeString("Asset", state.AssetID.ToString());
            writer.WriteAttributeString("Engine", "Porthos");
            writer.WriteStartElement("ScriptState");
            byte[] serialize = SerializeState(state);
            writer.WriteBase64(serialize, 0, serialize.Length);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private static byte[] SerializeState(ScriptState state)
        {
            using (var ms = new MemoryStream())
            {
                using (var tlv = new TLV(ms))
                {
                    tlv.Write((ushort)TlvIds.CurrentState, state.CurrentState);
                    tlv.Write((ushort)TlvIds.IsRunning, state.IsRunning);
                    tlv.Write((ushort)TlvIds.StartParameter, state.StartParameter);
                    tlv.SerializeEvents(state);
                    {
                        ObjectPartInventoryItem.PermsGranterInfo grantInfo = state.PermsGranter;
                        if (grantInfo.PermsGranter.IsSet)
                        {
                            tlv.Write((ushort)TlvIds.PermsGranter_Mask, (uint)grantInfo.PermsMask);
                            tlv.Write((ushort)TlvIds.PermsGranter_Granter, grantInfo.PermsGranter);
                        }
                    }
                    using (TLV plugintlv = tlv.WriteInner((ushort)TlvIds.PluginData))
                    {
                        foreach (object o in state.PluginData)
                        {
                            tlv.SerializeTypedValue(o);
                        }
                    }
                    tlv.Write((ushort)TlvIds.MinEventDelay, state.MinEventDelay);
                    tlv.WriteEnd();
                }
                return ms.ToArray();
            }
        }

        private static void SerializeTypedValue(this TLV tlv, object o)
        {
            Type varType = o.GetType();

            if (varType == typeof(Integer))
            {
                tlv.Write((ushort)TlvIds.DataValue, ((Integer)o).AsInt);
            }
            else if (varType == typeof(LongInteger))
            {
                tlv.Write((ushort)TlvIds.DataValue, ((LongInteger)o).AsLong);
            }
            else if (varType == typeof(Real))
            {
                double v = (Real)o;
                tlv.Write((ushort)TlvIds.DataValue, v);
            }
            else if (varType == typeof(AString))
            {
                tlv.Write((ushort)TlvIds.DataValue, o.ToString());
            }
            else if (varType == typeof(int))
            {
                tlv.Write((ushort)TlvIds.DataValue, (int)o);
            }
            else if (varType == typeof(char))
            {
                tlv.Write((ushort)TlvIds.DataValue, (char)o);
            }
            else if (varType == typeof(long))
            {
                tlv.Write((ushort)TlvIds.DataValue, (long)o);
            }
            else if (varType == typeof(double))
            {
                tlv.Write((ushort)TlvIds.DataValue, (double)o);
            }
            else if (varType == typeof(Vector3))
            {
                tlv.Write((ushort)TlvIds.DataValue, (Vector3)o);
            }
            else if (varType == typeof(Quaternion))
            {
                tlv.Write((ushort)TlvIds.DataValue, (Quaternion)o);
            }
            else if (varType == typeof(UUID))
            {
                tlv.Write((ushort)TlvIds.DataValue, (UUID)o);
            }
            else if (varType == typeof(UUID) || varType == typeof(LSLKey) || varType == typeof(string))
            {
                tlv.Write((ushort)TlvIds.DataValue, o.ToString());
            }
            else if (varType == typeof(AnArray))
            {
                using (TLV arraytlv = tlv.WriteInner((ushort)TlvIds.Array))
                {
                    foreach (IValue val in (AnArray)o)
                    {
                        SerializeTypedValue(tlv, val);
                    }
                }
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
                            var formatter = new XmlSerializer(varType);
                            formatter.Serialize(innerWriter, o);
                        }
                        data = ms.ToArray();
                    }
                }
                catch
                {
                    return;
                }
                tlv.Write((ushort)TlvIds.XmlSerialization, data);
            }
        }

        private static void SerializeEvents(this TLV tlv, ScriptState state)
        {
            foreach (IScriptEvent ev in state.EventData)
            {
                Type evType = ev.GetType();
                EventParams eventParams = null;
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
                if (eventParams != null)
                {
                    using (TLV innertlv = tlv.WriteInner((ushort)TlvIds.Queue))
                    {
                        innertlv.Write((ushort)TlvIds.EventName, eventParams.EventName);
                        foreach (KeyValuePair<string, object> kvp in state.Variables)
                        {
                            tlv.Write((ushort)TlvIds.VariableName, kvp.Key);
                            tlv.SerializeTypedValue(kvp.Value);
                        }

                        using (TLV paramtlv = innertlv.WriteInner((ushort)TlvIds.EventParam))
                        {
                            foreach (object o in eventParams.Params)
                            {
                                innertlv.SerializeTypedValue(o);
                            }
                        }

                        foreach (DetectInfo d in eventParams.Detected)
                        {
                            using (TLV detectedTlv = innertlv.WriteInner((ushort)TlvIds.DetectedInfo))
                            {
                                detectedTlv.Write((ushort)TlvIds.DetectedInfo_GrabOffset, d.GrabOffset);
                                detectedTlv.Write((ushort)TlvIds.DetectedInfo_LinkNum, d.LinkNumber);
                                detectedTlv.Write((ushort)TlvIds.DetectedInfo_Group, d.Group);
                                detectedTlv.Write((ushort)TlvIds.DetectedInfo_Name, d.Name);
                                detectedTlv.Write((ushort)TlvIds.DetectedInfo_Owner, d.Owner);
                                detectedTlv.Write((ushort)TlvIds.DetectedInfo_Position, d.Position);
                                detectedTlv.Write((ushort)TlvIds.DetectedInfo_Rotation, d.Rotation);
                                detectedTlv.Write((ushort)TlvIds.DetectedInfo_Type, (int)d.ObjType);
                                detectedTlv.Write((ushort)TlvIds.DetectedInfo_Velocity, d.Velocity);
                                detectedTlv.Write((ushort)TlvIds.DetectedInfo_TouchST, d.TouchST);
                                detectedTlv.Write((ushort)TlvIds.DetectedInfo_TouchUV, d.TouchUV);
                                detectedTlv.Write((ushort)TlvIds.DetectedInfo_TouchBinormal, d.TouchBinormal);
                                detectedTlv.Write((ushort)TlvIds.DetectedInfo_TouchPos, d.TouchPosition);
                                detectedTlv.Write((ushort)TlvIds.DetectedInfo_TouchFace, d.TouchFace);
                                detectedTlv.Write((ushort)TlvIds.DetectedInfo_Key, d.Key);
                            }
                        }
                    }
                }
            }
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
                                using (var ms = new MemoryStream(Convert.FromBase64String(reader.ReadElementValueAsString())))
                                {
                                    using (var tlv = new TLV(ms))
                                    {
                                        state = ScriptStateFromData(tlv);
                                    }
                                }
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
        private static ScriptState ScriptStateFromData(TLV tlv)
        {
            TLV.Header header;
            var state = new ScriptState();
            string s;
            bool b;
            int i;
            double d;
            var events = new List<IScriptEvent>();

            for (; ; )
            {
                if (!tlv.TryReadHeader(out header))
                {
                    throw new InvalidObjectXmlException();
                }

                if (header.Type == TLV.EntryType.End)
                {
                    break;
                }

                switch ((TlvIds)header.ID)
                {
                    case TlvIds.CurrentState:
                        if (!tlv.TryReadTypedValue(header, out s))
                        {
                            throw new InvalidObjectXmlException();
                        }
                        state.CurrentState = s;
                        break;

                    case TlvIds.IsRunning:
                        if (!tlv.TryReadTypedValue(header, out b))
                        {
                            throw new InvalidObjectXmlException();
                        }
                        state.IsRunning = b;
                        break;

                    case TlvIds.StartParameter:
                        if (!tlv.TryReadTypedValue(header, out i))
                        {
                            throw new InvalidObjectXmlException();
                        }
                        state.StartParameter = i;
                        break;

                    case TlvIds.Queue:
                        using (TLV queuetlv = tlv.ReadInner(header))
                        {
                            IScriptEvent ev;
                            if (ScriptState.TryTranslateEventParams(DeserializeQueue(queuetlv), out ev))
                            {
                                events.Add(ev);
                            }
                        }
                        break;

                    case TlvIds.PermsGranter_Mask:
                        uint ui;
                        if (!tlv.TryReadTypedValue(header, out ui))
                        {
                            throw new InvalidObjectXmlException();
                        }
                        state.PermsGranter.PermsMask = (ScriptPermissions)ui;
                        break;

                    case TlvIds.PermsGranter_Granter:
                        UGUI granter;
                        if (!tlv.TryReadTypedValue(header, out granter))
                        {
                            throw new InvalidObjectXmlException();
                        }
                        state.PermsGranter.PermsGranter = granter;
                        break;

                    case TlvIds.PluginData:
                        using (TLV plugintlv = tlv.ReadInner(header))
                        {
                            while (plugintlv.TryReadHeader(out header))
                            {
                                object pluginValue;
                                if (TryReadTypedValue(tlv, header, out pluginValue))
                                {
                                    state.PluginData.Add(pluginValue);
                                }
                                else
                                {
                                    throw new InvalidObjectXmlException();
                                }
                            }
                        }
                        break;

                    case TlvIds.MinEventDelay:
                        if (!tlv.TryReadTypedValue(header, out d))
                        {
                            throw new InvalidObjectXmlException();
                        }
                        state.MinEventDelay = d;
                        break;

                    case TlvIds.VariableName:
                        string varName;
                        if (!tlv.TryReadTypedValue(header, out varName))
                        {
                            throw new InvalidObjectXmlException();
                        }
                        if (!tlv.TryReadHeader(out header))
                        {
                            throw new InvalidObjectXmlException();
                        }
                        object varValue;
                        if (TryReadTypedValue(tlv, header, out varValue))
                        {
                            state.Variables[varName] = varValue;
                        }
                        break;

                    default:
                        object obj;
                        if (!tlv.TryReadValue(header, out obj))
                        {
                            throw new InvalidObjectXmlException();
                        }
                        break;
                }
            }

            state.EventData = events.ToArray();

            return state;
        }

        private static EventParams DeserializeQueue(TLV inner)
        {
            var ev = new EventParams();
            TLV.Header header;
            string s;
            while(inner.TryReadHeader(out header))
            {
                switch((TlvIds)header.ID)
                {
                    case TlvIds.EventName:
                        if(!inner.TryReadTypedValue(header, out s))
                        {
                            throw new InvalidObjectXmlException();
                        }
                        ev.EventName = s;
                        break;

                    case TlvIds.EventParam:
                        using (TLV paramtlv = inner.ReadInner(header))
                        {
                            while(paramtlv.TryReadHeader(out header))
                            {
                                object para;
                                if(!TryReadTypedValue(paramtlv, header, out para))
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                ev.Params.Add(para);
                            }
                        }
                        break;

                    case TlvIds.DetectedInfo:
                        using (TLV detecttlv = inner.ReadInner(header))
                        {
                            var di = new DetectInfo();
                            Vector3 vec;
                            Quaternion q;
                            int i;
                            UGI group;
                            UUID id;
                            UGUI owner;

                            while(detecttlv.TryReadHeader(out header))
                            {
                                switch((TlvIds)header.ID)
                                {
                                    case TlvIds.DetectedInfo_GrabOffset:
                                        if(!detecttlv.TryReadTypedValue(header, out vec))
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                        di.GrabOffset = vec;
                                        break;

                                    case TlvIds.DetectedInfo_LinkNum:
                                        if(!detecttlv.TryReadTypedValue(header, out i))
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                        di.LinkNumber = i;
                                        break;

                                    case TlvIds.DetectedInfo_Group:
                                        if(!detecttlv.TryReadTypedValue(header, out group))
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                        di.Group = group;
                                        break;

                                    case TlvIds.DetectedInfo_Name:
                                        if(!detecttlv.TryReadTypedValue(header, out s))
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                        di.Name = s;
                                        break;

                                    case TlvIds.DetectedInfo_Owner:
                                        if(!detecttlv.TryReadTypedValue(header, out owner))
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                        di.Owner = owner;
                                        break;

                                    case TlvIds.DetectedInfo_Position:
                                        if(!detecttlv.TryReadTypedValue(header, out vec))
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                        di.Position = vec;
                                        break;

                                    case TlvIds.DetectedInfo_Rotation:
                                        if(!detecttlv.TryReadTypedValue(header, out q))
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                        di.Rotation = q;
                                        break;

                                    case TlvIds.DetectedInfo_Type:
                                        if(!detecttlv.TryReadTypedValue(header, out i))
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                        di.ObjType = (DetectedTypeFlags)i;
                                        break;

                                    case TlvIds.DetectedInfo_Velocity:
                                        if(!detecttlv.TryReadTypedValue(header, out vec))
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                        di.Velocity = vec;
                                        break;

                                    case TlvIds.DetectedInfo_TouchST:
                                        if (!detecttlv.TryReadTypedValue(header, out vec))
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                        di.TouchST = vec;
                                        break;

                                    case TlvIds.DetectedInfo_TouchUV:
                                        if (!detecttlv.TryReadTypedValue(header, out vec))
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                        di.TouchUV = vec;
                                        break;

                                    case TlvIds.DetectedInfo_TouchBinormal:
                                        if (!detecttlv.TryReadTypedValue(header, out vec))
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                        di.TouchBinormal = vec;
                                        break;

                                    case TlvIds.DetectedInfo_TouchPos:
                                        if (!detecttlv.TryReadTypedValue(header, out vec))
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                        di.TouchPosition = vec;
                                        break;

                                    case TlvIds.DetectedInfo_TouchFace:
                                        if (!detecttlv.TryReadTypedValue(header, out i))
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                        di.TouchFace = i;
                                        break;

                                    case TlvIds.DetectedInfo_Key:
                                        if (!detecttlv.TryReadTypedValue(header, out id))
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                        di.Key = id;
                                        break;

                                    default:
                                        throw new InvalidObjectXmlException();
                                }
                            }
                            ev.Detected.Add(di);
                        }
                        break;

                    default:
                        throw new InvalidObjectXmlException();
                }
            }
            return ev;
        }

        private static bool TryReadTypedValue(TLV inner, TLV.Header header, out object res)
        {
            res = null;
            switch ((TlvIds)header.ID)
            {
                case TlvIds.Array:
                    using (TLV arraytlv = inner.ReadInner(header))
                    {
                        AnArray array = new AnArray();
                        while (arraytlv.TryReadHeader(out header))
                        {
                            object arrayVal;
                            switch ((TlvIds)header.ID)
                            {
                                case TlvIds.LSLKeyValue:
                                    string k;
                                    if (!arraytlv.TryReadTypedValue(header, out k))
                                    {
                                        throw new InvalidObjectXmlException();
                                    }
                                    array.Add(new LSLKey(k));
                                    break;

                                case TlvIds.DataValue:
                                    if (TryReadTypedValue(arraytlv, header, out arrayVal))
                                    {
                                        Type t = arrayVal.GetType();
                                        if (t == typeof(int))
                                        {
                                            array.Add((int)arrayVal);
                                        }
                                        else if (t == typeof(long))
                                        {
                                            array.Add((long)arrayVal);
                                        }
                                        else if (t == typeof(double))
                                        {
                                            array.Add((double)arrayVal);
                                        }
                                        else if (t == typeof(float))
                                        {
                                            array.Add((float)arrayVal);
                                        }
                                        else if (t == typeof(string))
                                        {
                                            array.Add((string)arrayVal);
                                        }
                                        else if (t == typeof(Quaternion))
                                        {
                                            array.Add((Quaternion)arrayVal);
                                        }
                                        else if (t == typeof(Vector3))
                                        {
                                            array.Add((Vector3)arrayVal);
                                        }
                                        else
                                        {
                                            throw new InvalidObjectXmlException();
                                        }
                                    }
                                    break;

                                default:
                                    throw new InvalidObjectXmlException();
                            }
                        }
                        res = array;
                        return true;
                    }

                case TlvIds.DataValue:
                    if (header.Type == TLV.EntryType.TLV)
                    {
                        throw new NotImplementedException();
                    }
                    else if (inner.TryReadTypedValue(header, out res))
                    {
                        return true;
                    }
                    else
                    {
                        throw new InvalidObjectXmlException();
                    }

                case TlvIds.LSLKeyValue:
                    string s;
                    if (!inner.TryReadTypedValue(header, out s))
                    {
                        throw new InvalidObjectXmlException();
                    }
                    res = new LSLKey(s);
                    return true;

                case TlvIds.XmlSerialization:
                    Type customType;
                    string type;
                    byte[] data;
                    if (!inner.TryReadTypedValue(header, out type))
                    {
                        throw new InvalidObjectXmlException();
                    }
                    if (!inner.TryReadHeader(out header))
                    {
                        throw new InvalidObjectXmlException();
                    }
                    if (!inner.TryReadTypedValue(header, out data))
                    {
                        throw new InvalidObjectXmlException();
                    }
                    if (LSLCompiler.KnownSerializationTypes.TryGetValue(type, out customType) &&
                        Attribute.GetCustomAttribute(customType, typeof(SerializableAttribute)) != null)
                    {
                        try
                        {
                            using (var ms = new MemoryStream(data))
                            {
                                var formatter = new XmlSerializer(customType);
                                res = formatter.Deserialize(ms);
                                return true;
                            }
                        }
                        catch
                        {
                            /* deserialization not possible */
                            return false;
                        }
                    }
                    else
                    {
                        throw new InvalidObjectXmlException();
                    }

                default:
                    throw new InvalidObjectXmlException();
            }
        }

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

            if (type == "list")
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
