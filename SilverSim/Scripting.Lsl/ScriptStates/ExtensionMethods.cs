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

using SilverSim.Types;
using System;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SilverSim.Scripting.Lsl.ScriptStates
{
    public static class ExtensionMethods
    {
        public static void ListToXml(this XmlTextWriter writer, string name, AnArray array)
        {
            writer.WriteStartElement("Variable");
            writer.WriteAttributeString("name", name);
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

        public static object ReadTypedValue(this XmlTextReader reader)
        {
            string type = string.Empty;
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

            string data = string.Empty;
            if (!isEmptyElement)
            {
                data = reader.ReadElementValueAsString("ListItem");
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

        public static void WriteTypedValue(this XmlTextWriter writer, string tagname, object o)
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
                writer.ListToXml(tagname, (AnArray)o);
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

    }
}
