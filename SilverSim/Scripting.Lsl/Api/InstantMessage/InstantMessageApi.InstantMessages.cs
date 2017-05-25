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
                var imservice = instance.Part.ObjectGroup.Scene.GetService<IMServiceInterface>();
                Vector3 globPos = thisGroup.GlobalPosition;
                string binBuck = string.Format("{0}/{1}/{2}/{3}\0",
                    thisScene.Name,
                    (int)Math.Floor(globPos.X),
                    (int)Math.Floor(globPos.Y),
                    (int)Math.Floor(globPos.Z));
                imservice.Send(new GridInstantMessage()
                {
                    FromAgent = new UUI { ID = thisPart.Owner.ID, FullName = thisGroup.Name },
                    IMSessionID = thisGroup.ID,
                    ToAgent = new UUI(user),
                    Position = globPos,
                    RegionID = thisScene.ID,
                    Message = message,
                    Dialog = GridInstantMessageDialog.MessageFromObject,
                    BinaryBucket = binBuck.ToUTF8Bytes(),
                    OnResult = (GridInstantMessage imret, bool success) => { }
                });
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
                var imservice = instance.Part.ObjectGroup.Scene.GetService<IMServiceInterface>();
                imservice.Send(new GridInstantMessage()
                {
                    FromAgent = new UUI { ID = thisPart.Owner.ID, FullName = thisGroup.Name },
                    IMSessionID = thisGroup.ID,
                    ToAgent = new UUI(user),
                    Position = thisGroup.GlobalPosition,
                    RegionID = thisScene.ID,
                    Message = message,
                    Dialog = GridInstantMessageDialog.MessageBox,
                    OnResult = (GridInstantMessage imret, bool success) => { }
                });
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
                imservice.Send(new GridInstantMessage()
                {
                    FromAgent = new UUI { ID = thisPart.Owner.ID, FullName = thisGroup.Name },
                    IMSessionID = thisGroup.ID,
                    ToAgent = new UUI(user),
                    Position = thisGroup.GlobalPosition,
                    RegionID = thisScene.ID,
                    Message = message,
                    BinaryBucket = (url + "\0").ToUTF8Bytes(),
                    Dialog = GridInstantMessageDialog.GotoUrl,
                    OnResult = (GridInstantMessage imret, bool success) => { }
                });
            }
        }
    }
}
