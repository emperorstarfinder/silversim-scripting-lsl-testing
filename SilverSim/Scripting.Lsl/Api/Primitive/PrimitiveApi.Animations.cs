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

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        [APILevel(APIFlags.LSL, "llStartObjectAnimation")]
        public void StartObjectAnimation(
            ScriptInstance instance,
            [Description("animation to be played")]
            string animation)
        {
            lock (instance)
            {
                UUID animID = instance.GetAnimationAssetID(animation);

                ObjectGroup grp = instance.Part.ObjectGroup;
                SceneInterface scene = grp.Scene;
                AssetServiceInterface assetService = scene.AssetService;
                AssetMetadata metadata;
                AssetData data;
                if (!assetService.Metadata.TryGetValue(animID, out metadata))
                {
                    if (grp.IsAttached) /* on attachments, we have to fetch from agent eventually */
                    {
                        IAgent owner;
                        if (!grp.Scene.RootAgents.TryGetValue(grp.Owner.ID, out owner))
                        {
                            return;
                        }
                        if (!owner.AssetService.TryGetValue(animID, out data))
                        {
                            /* not found */
                            return;
                        }
                        assetService.Store(data);
                        if (data.Type != AssetType.Animation)
                        {
                            /* ignore wrong asset here */
                            return;
                        }
                    }
                    else
                    {
                        /* ignore missing asset here */
                        return;
                    }
                }
                else if (metadata.Type != AssetType.Animation)
                {
                    /* ignore wrong asset here */
                    return;
                }

                instance.Part.AnimationController.PlayAnimation(animID);
            }
        }

        [APILevel(APIFlags.LSL, "llStopObjectAnimation")]
        public void StopObjectAnimation(
            ScriptInstance instance,
            [Description("animation to be stopped")]
            string animation)
        {
            lock (instance)
            {
                UUID animID = instance.GetAnimationAssetID(animation);
                instance.Part.AnimationController.StopAnimation(animID);
            }
        }

        [APILevel(APIFlags.LSL, "llGetObjectAnimationNames")]
        public AnArray GetObjectAnimationNames(
            ScriptInstance instance)
        {
            var anims = new List<UUID>();
            var animinvs = new Dictionary<UUID, ObjectPartInventoryItem>();
            lock(instance)
            {
                anims = instance.Part.AnimationController.GetPlayingAnimations();
                foreach(ObjectPartInventoryItem item in instance.Part.Inventory.Values)
                {
                    if(item.AssetType == Types.Asset.AssetType.Animation &&
                        !animinvs.ContainsKey(item.AssetID))
                    {
                        animinvs.Add(item.AssetID, item);
                    }
                }
            }

            /* no need to do the following in the lock */
            var res = new AnArray();
            foreach(UUID animid in anims)
            {
                ObjectPartInventoryItem item;
                if(animinvs.TryGetValue(animid, out item))
                {
                    res.Add(item.Name);
                }
                else
                {
                    res.Add(animid);
                }
            }
            return res;
        }
    }
}
