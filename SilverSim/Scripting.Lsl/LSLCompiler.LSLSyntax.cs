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
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace SilverSim.Scripting.Lsl
{
    public partial class LSLCompiler
    {
        private void WriteSyntaxElement(XmlTextWriter writer, string elem, string tooltip)
        {
            writer.WriteNamedValue("key", elem);
            writer.WriteStartElement("map");
            writer.WriteNamedValue("key", "tooltip");
            writer.WriteNamedValue("string", tooltip);
            writer.WriteEndElement();
        }

        public UUID GetLSLSyntaxId()
        {
            return LSLSyntaxId;
        }

        public void WriteLSLSyntaxFile(Stream outstream)
        {
            outstream.Write(LSLSyntaxFile, 0, LSLSyntaxFile.Length);
        }

        private byte[] LSLSyntaxFile = new byte[0];
        private UUID LSLSyntaxId = UUID.Zero;

        [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
        public void GenerateLSLSyntaxFile()
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new XmlTextWriter(ms, new UTF8Encoding(false)))
                {
                    writer.WriteStartElement("map");
                    {
                        #region Controls
                        writer.WriteNamedValue("key", "controls");
                        writer.WriteStartElement("map");
                        {
                            WriteSyntaxElement(writer, "do", "do / while loop\ndo {\n...\n} while (<condition>);");
                            WriteSyntaxElement(writer, "else", "if / else block\nif (<condition>) {\n...\n[} else [if (<condition>) {\n...]]\n}");
                            WriteSyntaxElement(writer, "for", "for loop\nfor (<initializer>; <condition>; <post-iteration-statement>)\n{ ...\n}");
                            WriteSyntaxElement(writer, "if", "if / else block\nif (<condition>) {\n...\n[} else [if (<condition>) {\n...]]\n}");
                            WriteSyntaxElement(writer, "jump", "jump statement\njump <label>");
                            WriteSyntaxElement(writer, "return", "Leave current event or function.\nreturn [<variable>];\nOptionally pass back a variable's value, from a function.");
                            WriteSyntaxElement(writer, "state", "state <target>\nIf the target state is not the same as the current one, change to the target state.");
                            WriteSyntaxElement(writer, "while", "while loop\nwhile (<condition>) {\n,,,\n}");
                        }
                        writer.WriteEndElement();
                        #endregion

                        #region Types
                        writer.WriteNamedValue("key", "types");
                        writer.WriteStartElement("map");
                        {
                            WriteSyntaxElement(writer, "float", "32 bit floating point value.\nThe range is 1.175494351E-38 to 3.402823466E+38.");
                            WriteSyntaxElement(writer, "integer", "32 bit integer value.\n-2,147,483,648 and +2,147,483,647 (that is 0x80000000 to 0x7FFFFFFF in hex).");
                            WriteSyntaxElement(writer, "key", "A 128 bit unique identifier (UUID).\nThe key is represented as hexidecimal characters (A-F and 0-9), grouped into sections (8,4,4,4,12 characters) and separated by hyphens (for a total of 36 characters). e.g. \"A822FF2B-FF02-461D-B45D-DCD10A2DE0C2\".");
                            WriteSyntaxElement(writer, "list", "A collection of other data types.\nLists are signified by square brackets surrounding their elements; the elements inside are separated by commas. e.g. [0, 1, 2, 3, 4] or [\"Yes\", \"No\", \"Perhaps\"].");
                            WriteSyntaxElement(writer, "quaternion", "The quaternion type is a left over from way back when LSL was created. It was later renamed to &lt;rotation&gt; to make it more user friendly, but it appears someone forgot to remove it ;-)");
                            WriteSyntaxElement(writer, "rotation", "The rotation type is one of several ways to represent an orientation in 3D.\nIt is a mathematical object called a quaternion. You can think of a quaternion as four numbers (x, y, z, w), three of which represent the direction an object is facing and a fourth that represents the object's banking left or right around that direction.");
                            WriteSyntaxElement(writer, "string", "Text data.\nThe editor accepts UTF-8 encoded text.");
                            WriteSyntaxElement(writer, "vector", "A vector is a data type that contains a set of three float values.\nVectors are used to represent colors (RGB), positions, and directions/velocities.");
                        }
                        writer.WriteEndElement();
                        #endregion

                        #region Constants
                        writer.WriteNamedValue("key", "constants");
                        writer.WriteStartElement("map");
                        {
                            foreach (IScriptApi api in m_Apis)
                            {
                                foreach (FieldInfo fi in api.GetType().GetFields())
                                {
                                    if (IsValidType(fi.FieldType))
                                    {
                                        var apiLevelAttrs = Attribute.GetCustomAttributes(fi, typeof(APILevelAttribute)) as APILevelAttribute[];
                                        var apiExtensionAttrs = Attribute.GetCustomAttributes(fi, typeof(APIExtensionAttribute)) as APIExtensionAttribute[];
                                        foreach(APILevelAttribute level in apiLevelAttrs)
                                        {
                                            if (string.IsNullOrEmpty(level.Name))
                                            {
                                                writer.WriteNamedValue("key", fi.Name);
                                            }
                                            else
                                            {
                                                writer.WriteNamedValue("key", level.Name);
                                            }
                                            writer.WriteStartElement("map");
                                            {
                                                writer.WriteNamedValue("key", "type");
                                                writer.WriteNamedValue("string", MapType(fi.FieldType));
                                                writer.WriteNamedValue("key", "value");
                                                writer.WriteNamedValue("string", fi.GetValue(null).ToString());
                                                var tooltip = (DescriptionAttribute)System.Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));
                                                writer.WriteNamedValue("key", "tooltip");
                                                string avail = "Supported for";
                                                if ((level.Flags & APIFlags.LSL) != APIFlags.None)
                                                {
                                                    avail += " LSL";
                                                }
                                                if ((level.Flags & APIFlags.ASSL) != APIFlags.None)
                                                {
                                                    avail += " ASSL";
                                                }
                                                if ((level.Flags & APIFlags.OSSL) != APIFlags.None)
                                                {
                                                    avail += " OSSL";
                                                }

                                                if (tooltip != null)
                                                {
                                                    writer.WriteNamedValue("string", tooltip.Description.Replace("\n", "\\n") + "\\n" + avail);
                                                }
                                                else
                                                {
                                                    writer.WriteNamedValue("string", fi.Name + "\\n" + avail);
                                                }
                                            }
                                            writer.WriteEndElement();
                                        }

                                        foreach (APIExtensionAttribute level in apiExtensionAttrs)
                                        {
                                            if (string.IsNullOrEmpty(level.Name))
                                            {
                                                writer.WriteNamedValue("key", fi.Name);
                                            }
                                            else
                                            {
                                                writer.WriteNamedValue("key", level.Name);
                                            }
                                            writer.WriteStartElement("map");
                                            {
                                                writer.WriteNamedValue("key", "type");
                                                writer.WriteNamedValue("string", MapType(fi.FieldType));
                                                writer.WriteNamedValue("key", "value");
                                                writer.WriteNamedValue("string", fi.GetValue(null).ToString());
                                                var tooltip = (DescriptionAttribute)Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));
                                                writer.WriteNamedValue("key", "tooltip");
                                                string avail = "Supported for " + level.Extension;

                                                if (tooltip != null)
                                                {
                                                    writer.WriteNamedValue("string", tooltip.Description.Replace("\n", "\\n") + "\\n" + avail);
                                                }
                                                else
                                                {
                                                    writer.WriteNamedValue("string", fi.Name + "\\n" + avail);
                                                }
                                            }
                                            writer.WriteEndElement();
                                        }
                                    }
                                }
                            }
                        }
                        writer.WriteEndElement();
                        #endregion

                        #region Events
                        writer.WriteNamedValue("key", "events");
                        writer.WriteStartElement("map");
                        {
                            foreach (IScriptApi api in m_Apis)
                            {
                                foreach (Type t in api.GetType().GetNestedTypes(BindingFlags.Public).Where(t => t.BaseType == typeof(MulticastDelegate)))
                                {
                                    var apiLevelAttrs = Attribute.GetCustomAttributes(t, typeof(APILevelAttribute)) as APILevelAttribute[];
                                    var apiExtensionAttrs = Attribute.GetCustomAttributes(t, typeof(APIExtensionAttribute)) as APIExtensionAttribute[];
                                    var stateEventDel = (StateEventDelegateAttribute)Attribute.GetCustomAttribute(t, typeof(StateEventDelegateAttribute));
                                    MethodInfo mi = t.GetMethod("Invoke");
                                    if ((apiLevelAttrs.Length != 0 || apiExtensionAttrs.Length != 0) && stateEventDel != null)
                                    {
                                        foreach (APILevelAttribute level in apiLevelAttrs)
                                        {
                                            if (string.IsNullOrEmpty(level.Name))
                                            {
                                                writer.WriteNamedValue("key", mi.Name);
                                            }
                                            else
                                            {
                                                writer.WriteNamedValue("key", level.Name);
                                            }
                                            writer.WriteStartElement("map");
                                            {
                                                writer.WriteNamedValue("key", "arguments");
                                                writer.WriteStartElement("map");
                                                foreach (ParameterInfo pi in mi.GetParameters())
                                                {
                                                    writer.WriteNamedValue("key", pi.Name);
                                                    writer.WriteNamedValue("string", MapType(pi.ParameterType));
                                                    var ptooltip = (DescriptionAttribute)Attribute.GetCustomAttribute(pi, typeof(DescriptionAttribute));
                                                    writer.WriteNamedValue("key", "tooltip");
                                                    if (ptooltip != null)
                                                    {
                                                        writer.WriteNamedValue("string", ptooltip.Description.Replace("\n", "\\n"));
                                                    }
                                                    else
                                                    {
                                                        writer.WriteStartElement("string");
                                                        writer.WriteEndElement();
                                                    }
                                                }
                                                writer.WriteEndElement();

                                                var tooltip = (DescriptionAttribute)Attribute.GetCustomAttribute(mi, typeof(DescriptionAttribute));
                                                writer.WriteNamedValue("key", "tooltip");
                                                var avail = new StringBuilder("Supported for");
                                                if ((level.Flags & APIFlags.LSL) != APIFlags.None)
                                                {
                                                    avail.Append(" LSL");
                                                }
                                                if ((level.Flags & APIFlags.ASSL) != APIFlags.None)
                                                {
                                                    avail.Append(" ASSL");
                                                }
                                                if ((level.Flags & APIFlags.OSSL) != APIFlags.None)
                                                {
                                                    avail.Append(" OSSL");
                                                }
                                                foreach(APIExtensionAttribute ext in apiExtensionAttrs)
                                                {
                                                    avail.Append(" ");
                                                    avail.Append(ext.Extension);
                                                }

                                                if (tooltip != null)
                                                {
                                                    writer.WriteNamedValue("string", tooltip.Description.Replace("\n", "\\n") + "\\n" + avail.ToString());
                                                }
                                                else
                                                {
                                                    writer.WriteNamedValue("string", t.Name + "\\n" + avail.ToString());
                                                }
                                            }
                                            writer.WriteEndElement();
                                        }
                                    }
                                }
                            }
                        }
                        writer.WriteEndElement();
                        #endregion

                        #region Functions
                        writer.WriteNamedValue("key", "functions");
                        writer.WriteStartElement("map");
                        {
                            foreach (IScriptApi api in m_Apis)
                            {
                                foreach (MethodInfo mi in api.GetType().GetMethods())
                                {
                                    foreach(APILevelAttribute level in (APILevelAttribute[])Attribute.GetCustomAttributes(mi, typeof(APILevelAttribute)))
                                    {
                                        if (string.IsNullOrEmpty(level.Name))
                                        {
                                            writer.WriteNamedValue("key", mi.Name);
                                        }
                                        else
                                        {
                                            writer.WriteNamedValue("key", level.Name);
                                        }
                                        writer.WriteStartElement("map");
                                        {
                                            var energy = (EnergyUsageAttribute)Attribute.GetCustomAttribute(mi, typeof(EnergyUsageAttribute));
                                            var forcedSleep = (ForcedSleepAttribute)Attribute.GetCustomAttribute(mi, typeof(ForcedSleepAttribute));
                                            writer.WriteNamedValue("key", "energy");
                                            if (energy != null)
                                            {
                                                writer.WriteNamedValue("real", energy.Energy);
                                            }
                                            else
                                            {
                                                writer.WriteNamedValue("real", 10f);
                                            }
                                            writer.WriteNamedValue("key", "sleep");
                                            if (forcedSleep != null)
                                            {
                                                writer.WriteNamedValue("real", forcedSleep.Seconds);
                                            }
                                            else
                                            {
                                                writer.WriteNamedValue("real", "0.0");
                                            }
                                            writer.WriteNamedValue("key", "return");
                                            writer.WriteNamedValue("string", MapType(mi.ReturnType));

                                            writer.WriteNamedValue("key", "arguments");
                                            writer.WriteStartElement("map");
                                            foreach (ParameterInfo pi in mi.GetParameters())
                                            {
                                                writer.WriteNamedValue("key", pi.Name);
                                                writer.WriteNamedValue("string", MapType(pi.ParameterType));
                                                var ptooltip = (DescriptionAttribute)Attribute.GetCustomAttribute(pi, typeof(DescriptionAttribute));
                                                writer.WriteNamedValue("key", "tooltip");
                                                if (ptooltip != null)
                                                {
                                                    writer.WriteNamedValue("string", ptooltip.Description.Replace("\n", "\\n"));
                                                }
                                                else
                                                {
                                                    writer.WriteStartElement("string");
                                                    writer.WriteEndElement();
                                                }
                                            }
                                            writer.WriteEndElement();

                                            var tooltip = (DescriptionAttribute)Attribute.GetCustomAttribute(mi, typeof(DescriptionAttribute));
                                            writer.WriteNamedValue("key", "tooltip");
                                            string avail = "Supported for";
                                            if ((level.Flags & APIFlags.LSL) != APIFlags.None)
                                            {
                                                avail += " LSL";
                                            }
                                            if ((level.Flags & APIFlags.ASSL) != APIFlags.None)
                                            {
                                                avail += " ASSL";
                                            }
                                            if ((level.Flags & APIFlags.OSSL) != APIFlags.None)
                                            {
                                                avail += " OSSL";
                                            }

                                            if (tooltip != null)
                                            {
                                                writer.WriteNamedValue("string", tooltip.Description.Replace("\n", "\\n") + "\\n" + avail);
                                            }
                                            else
                                            {
                                                writer.WriteNamedValue("string", mi.Name + "\\n" + avail);
                                            }
                                        }
                                        writer.WriteEndElement();
                                    }

                                    foreach (APIExtensionAttribute level in (APIExtensionAttribute[])Attribute.GetCustomAttributes(mi, typeof(APIExtensionAttribute)))
                                    {
                                        if (string.IsNullOrEmpty(level.Name))
                                        {
                                            writer.WriteNamedValue("key", mi.Name);
                                        }
                                        else
                                        {
                                            writer.WriteNamedValue("key", level.Name);
                                        }
                                        writer.WriteStartElement("map");
                                        {
                                            var energy = (EnergyUsageAttribute)Attribute.GetCustomAttribute(mi, typeof(EnergyUsageAttribute));
                                            var forcedSleep = (ForcedSleepAttribute)Attribute.GetCustomAttribute(mi, typeof(ForcedSleepAttribute));
                                            writer.WriteNamedValue("key", "energy");
                                            if (energy != null)
                                            {
                                                writer.WriteNamedValue("real", energy.Energy);
                                            }
                                            else
                                            {
                                                writer.WriteNamedValue("real", 10f);
                                            }
                                            writer.WriteNamedValue("key", "sleep");
                                            if (forcedSleep != null)
                                            {
                                                writer.WriteNamedValue("real", forcedSleep.Seconds);
                                            }
                                            else
                                            {
                                                writer.WriteNamedValue("real", "0.0");
                                            }
                                            writer.WriteNamedValue("key", "return");
                                            writer.WriteNamedValue("string", MapType(mi.ReturnType));

                                            writer.WriteNamedValue("key", "arguments");
                                            writer.WriteStartElement("map");
                                            foreach (ParameterInfo pi in mi.GetParameters())
                                            {
                                                writer.WriteNamedValue("key", pi.Name);
                                                writer.WriteNamedValue("string", MapType(pi.ParameterType));
                                                var ptooltip = (DescriptionAttribute)Attribute.GetCustomAttribute(pi, typeof(DescriptionAttribute));
                                                writer.WriteNamedValue("key", "tooltip");
                                                if (ptooltip != null)
                                                {
                                                    writer.WriteNamedValue("string", ptooltip.Description.Replace("\n", "\\n"));
                                                }
                                                else
                                                {
                                                    writer.WriteStartElement("string");
                                                    writer.WriteEndElement();
                                                }
                                            }
                                            writer.WriteEndElement();

                                            var tooltip = (DescriptionAttribute)Attribute.GetCustomAttribute(mi, typeof(DescriptionAttribute));
                                            writer.WriteNamedValue("key", "tooltip");
                                            string avail = "Supported for "+ level.Extension;

                                            if (tooltip != null)
                                            {
                                                writer.WriteNamedValue("string", tooltip.Description.Replace("\n", "\\n") + "\\n" + avail);
                                            }
                                            else
                                            {
                                                writer.WriteNamedValue("string", mi.Name + "\\n" + avail);
                                            }
                                        }
                                        writer.WriteEndElement();
                                    }
                                }
                            }
                        }
                        writer.WriteEndElement();
                        #endregion

                        writer.WriteNamedValue("key", "llsd-lsl-syntax-version");
                        writer.WriteNamedValue("integer", 2);
                    }
                    writer.WriteEndElement();

                    using (var md5 = MD5.Create())
                    {
                        LSLSyntaxFile = ms.ToArray();
                        byte[] hash = md5.ComputeHash(LSLSyntaxFile);
                        LSLSyntaxId = new UUID(hash, 0);
                    }
                }
            }
        }
    }
}
