// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.LSL.Api.Primitive
{
    public partial class PrimitiveApi
    {
        void EnqueueToScripts(ObjectPart part, LinkMessageEvent ev)
        {
            foreach(ObjectPartInventoryItem item in part.Inventory.Values)
            {
                if(item.AssetType == AssetType.LSLText || item.AssetType == AssetType.LSLBytecode)
                {
                    ScriptInstance si = item.ScriptInstance;

                    if(si != null)
                    {
                        si.PostEvent(ev);
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llMessageLinked")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void MessageLinked(ScriptInstance instance, int link, int num, string str, LSLKey id)
        {
            lock (instance)
            {
                LinkMessageEvent ev = new LinkMessageEvent();
                ev.SenderNumber = instance.Part.LinkNumber;
                ev.TargetNumber = link;
                ev.Number = num;
                ev.Data = str;
                ev.Id = id.ToString();

                foreach (ObjectPart part in GetLinkTargets(instance, link))
                {
                    EnqueueToScripts(part, ev);
                }
            }
        }
    }
}
