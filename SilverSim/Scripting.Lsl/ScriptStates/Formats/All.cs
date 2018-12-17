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
using SilverSim.Types;
using System.Collections.Generic;
using System.Xml;

namespace SilverSim.Scripting.Lsl.ScriptStates.Formats
{
    public static class All
    {
        public static ScriptState Deserialize(XmlTextReader reader, Dictionary<string, string> attrs)
        {
            string engine;
            if (!attrs.TryGetValue("Engine", out engine))
            {
                throw new InvalidObjectXmlException();
            }
            switch (engine)
            {
                case "XEngine":
                    return XEngine.Deserialize(reader, attrs);

                case "Porthos":
                    return Porthos.Deserialize(reader, attrs);

                default:
                    throw new InvalidObjectXmlException();
            }
        }

        public static ScriptState Deserialize(XmlTextReader reader)
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
                            case "State":
                                Dictionary<string, string> attrs = new Dictionary<string, string>();
                                bool isEmptyElement = reader.IsEmptyElement;
                                if (reader.MoveToFirstAttribute())
                                {
                                    do
                                    {
                                        attrs[reader.Name] = reader.Value;
                                    } while (reader.MoveToNextAttribute());
                                }
                                if (isEmptyElement)
                                {
                                    return null;
                                }

                                return Deserialize(reader, attrs);

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;
                }
            }
        }
    }
}
