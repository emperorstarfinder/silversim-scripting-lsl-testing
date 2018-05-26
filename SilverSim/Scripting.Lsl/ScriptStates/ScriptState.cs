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
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SilverSim.Scripting.Lsl.ScriptStates
{
    public partial class ScriptState : ILSLScriptState
    {
        public class EventParams
        {
            public string EventName = string.Empty;
            public List<object> Params = new List<object>();
            public List<DetectInfo> Detected = new List<DetectInfo>();
        }

        public UUID ItemID { get; set; }
        public UUID AssetID { get; set; }
        public Dictionary<string, object> Variables { get; } = new Dictionary<string, object>();
        public List<object> PluginData { get; set; } = new List<object>();
        public IScriptEvent[] EventData { get; set; } = new IScriptEvent[0];
        public bool IsRunning { get; set; }
        public string CurrentState { get; set; } = "default";
        public double MinEventDelay { get; set; }
        public int StartParameter { get; set; }
        public ObjectPartInventoryItem.PermsGranterInfo PermsGranter { get; set; } = new ObjectPartInventoryItem.PermsGranterInfo();
        public bool UseMessageObjectEvent { get; set; }

        public void ToXml(XmlTextWriter writer)
        {
            Formats.XEngine.Serialize(writer, this);
        }

        public byte[] ToDbSerializedState()
        {
            using (var ms = new MemoryStream())
            {
                using (XmlTextWriter writer = ms.UTF8XmlTextWriter())
                {
                    Formats.XEngine.Serialize(writer, this);
                }
                return ms.ToArray();
            }
        }
    }
}