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

using SilverSim.Scene.Management.IM;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Types;
using SilverSim.Types.IM;
using System;

namespace SilverSim.Scripting.Lsl.Api.IM
{
    public partial class InstantMessageApi
    {
        [APILevel(APIFlags.LSL, "llInstantMessage")]
        [ForcedSleep(2)]
        public void InstantMessage(ScriptInstance instance, LSLKey user, string message)
        {
            lock(instance)
            {
                ObjectPart thisPart = instance.Part;
                ObjectGroup thisGroup = thisPart.ObjectGroup;
                SceneInterface thisScene = thisGroup.Scene;
                IMServiceInterface imservice = instance.Part.ObjectGroup.Scene.GetService<IMServiceInterface>();
                GridInstantMessage im = new GridInstantMessage();
                im.FromAgent.ID = thisPart.Owner.ID;
                im.FromAgent.FullName = thisGroup.Name;
                im.IMSessionID = thisGroup.ID;
                im.ToAgent.ID = user;
                im.Position = thisGroup.GlobalPosition;
                im.RegionID = thisScene.ID;
                im.Message = message;
                im.Dialog = GridInstantMessageDialog.MessageFromObject;
                string binBuck = string.Format("{0}/{1}/{2}/{3}\0",
                    thisScene.Name,
                    (int)Math.Floor(im.Position.X),
                    (int)Math.Floor(im.Position.Y),
                    (int)Math.Floor(im.Position.Z));
                im.BinaryBucket = binBuck.ToUTF8Bytes();
                im.OnResult = delegate(GridInstantMessage imret, bool success) { };

                imservice.Send(im);
            }
        }

        [APILevel(APIFlags.OSSL, "osMessageBox")]
        public void MessageBox(ScriptInstance instance, LSLKey user, string message)
        {
            lock (instance)
            {
                ObjectPart thisPart = instance.Part;
                ObjectGroup thisGroup = thisPart.ObjectGroup;
                SceneInterface thisScene = thisGroup.Scene;
                IMServiceInterface imservice = instance.Part.ObjectGroup.Scene.GetService<IMServiceInterface>();
                GridInstantMessage im = new GridInstantMessage();
                im.FromAgent.ID = thisPart.Owner.ID;
                im.FromAgent.FullName = thisGroup.Name;
                im.IMSessionID = thisGroup.ID;
                im.ToAgent.ID = user;
                im.Position = thisGroup.GlobalPosition;
                im.RegionID = thisScene.ID;
                im.Message = message;
                im.Dialog = GridInstantMessageDialog.MessageBox;
                im.OnResult = delegate (GridInstantMessage imret, bool success) { };

                imservice.Send(im);
            }
        }

        [APILevel(APIFlags.OSSL, "osGotoUrl")]
        public void GotoUrl(ScriptInstance instance, LSLKey user, string message, string url)
        {
            lock (instance)
            {
                ObjectPart thisPart = instance.Part;
                ObjectGroup thisGroup = thisPart.ObjectGroup;
                SceneInterface thisScene = thisGroup.Scene;
                IMServiceInterface imservice = instance.Part.ObjectGroup.Scene.GetService<IMServiceInterface>();
                GridInstantMessage im = new GridInstantMessage();
                im.FromAgent.ID = thisPart.Owner.ID;
                im.FromAgent.FullName = thisGroup.Name;
                im.IMSessionID = thisGroup.ID;
                im.ToAgent.ID = user;
                im.Position = thisGroup.GlobalPosition;
                im.RegionID = thisScene.ID;
                im.Message = message;
                im.BinaryBucket = (url + "\0").ToUTF8Bytes();
                im.Dialog = GridInstantMessageDialog.GotoUrl;
                im.OnResult = delegate (GridInstantMessage imret, bool success) { };

                imservice.Send(im);
            }
        }
    }
}
