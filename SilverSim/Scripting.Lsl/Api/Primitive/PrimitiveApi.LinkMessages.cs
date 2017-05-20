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
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types.Asset;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        private void EnqueueToScripts(ObjectPart part, LinkMessageEvent ev)
        {
            /* check whether we actually have scripts in prim */
            if (part.IsScripted)
            {
                foreach (ObjectPartInventoryItem item in part.Inventory.Values)
                {
                    if (item.AssetType == AssetType.LSLText || item.AssetType == AssetType.LSLBytecode)
                    {
                        ScriptInstance si = item.ScriptInstance;

                        if (si?.IsLinkMessageReceiver == true)
                        {
                            si.PostEvent(ev);
                        }
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL, "llMessageLinked")]
        public void MessageLinked(ScriptInstance instance, int link, int num, string str, LSLKey id)
        {
            lock (instance)
            {
                var ev = new LinkMessageEvent()
                {
                    SenderNumber = instance.Part.LinkNumber,
                    TargetNumber = link,
                    Number = num,
                    Data = str,
                    Id = id.ToString()
                };
                foreach (ObjectPart part in GetLinkTargets(instance, link))
                {
                    EnqueueToScripts(part, ev);
                }
            }
        }
    }
}
