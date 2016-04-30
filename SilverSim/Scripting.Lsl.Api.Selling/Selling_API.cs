// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Selling
{
    [ScriptApiName("Selling")]
    [Description("LSL Selling API")]
    [LSLImplementation]
    public class SellingApi : IScriptApi, IPlugin
    {
        public SellingApi()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [APIExtension(APIExtension.Selling, "item_sold")]
        [StateEventDelegate]
        public delegate void State_item_sold(LSLKey id, string agentname, LSLKey agentid, string objectname, LSLKey objectid);

        [APIExtension(APIExtension.Selling, "sellListen")]
        public void SellListen(ScriptInstance instance)
        {
            lock (instance)
            {
                ObjectPart part = instance.Part;
                if (null == part)
                {
                    return;
                }
                ObjectGroup group = part.ObjectGroup;
                if (group == null)
                {
                    return;
                }
                SceneInterface scene = group.Scene;
                if (scene != null)
                {
                    scene.AddObjectBuyListen(instance);
                }
            }
        }

        [APIExtension(APIExtension.Selling, "sellListenRemove")]
        [ExecutedOnScriptReset]
        [ExecutedOnScriptRemove]
        public void SellListenRemove(ScriptInstance instance)
        {
            lock (instance)
            {
                ObjectPart part = instance.Part;
                if (null == part)
                {
                    return;
                }
                ObjectGroup group = part.ObjectGroup;
                if (group == null)
                {
                    return;
                }
                SceneInterface scene = group.Scene;
                if (scene != null)
                {
                    scene.RemoveObjectBuyListen(instance);
                }
            }
        }
    }
}
