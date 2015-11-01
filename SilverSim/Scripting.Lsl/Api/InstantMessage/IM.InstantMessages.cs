// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.IM;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.IM;
using System.Text;
using System;
using System.Diagnostics.CodeAnalysis;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;

namespace SilverSim.Scripting.Lsl.Api.IM
{
    public partial class InstantMessageApi
    {
        [APILevel(APIFlags.LSL, "llInstantMessage")]
        [ForcedSleep(2)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void InstantMessage(ScriptInstance instance, LSLKey user, string message)
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
                im.BinaryBucket = UTF8NoBOM.GetBytes(binBuck);
                im.OnResult = delegate(GridInstantMessage imret, bool success) { };

                imservice.Send(im);
            }
        }

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}
