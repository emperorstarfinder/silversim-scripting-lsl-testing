// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using System;
using SilverSim.Scene.Types.Scene;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.LSL.Api.Primitive
{
    public partial class PrimitiveApi
    {
        #region Primitives

        [APILevel(APIFlags.LSL, "llGetKey")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        LSLKey GetKey(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ID;
            }
        }

        [APILevel(APIFlags.LSL, "llAllowInventoryDrop")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void AllowInventoryDrop(ScriptInstance instance, int add)
        {
            lock (instance)
            {
                instance.Part.IsAllowedDrop = add != 0;
            }
        }

        [APILevel(APIFlags.LSL, "llGetLinkPrimitiveParams")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        AnArray GetLinkPrimitiveParams(ScriptInstance instance, int link, AnArray param)
        {
            AnArray parout = new AnArray();
            lock (instance)
            {
                instance.Part.ObjectGroup.GetPrimitiveParams(instance.Part.LinkNumber, link, param.GetEnumerator(), ref parout);
            }
            return parout;
        }

        [APILevel(APIFlags.LSL, "llGetPrimitiveParams")]
        [ForcedSleep(0.2)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        AnArray GetPrimitiveParams(ScriptInstance instance, AnArray param)
        {
            AnArray parout = new AnArray();
            lock (instance)
            {
                instance.Part.ObjectGroup.GetPrimitiveParams(instance.Part.LinkNumber, LINK_THIS, param.GetEnumerator(), ref parout);
            }
            return parout;
        }

        [APILevel(APIFlags.LSL, "llGetLocalPos")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        Vector3 GetLocalPos(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.LocalPosition;
            }
        }

        [APILevel(APIFlags.LSL, "llGetPos")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        Vector3 GetPos(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.Position;
            }
        }

        [APILevel(APIFlags.LSL, "llGetRootPosition")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        Vector3 GetRootPosition(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Position;
            }
        }

        [APILevel(APIFlags.LSL, "llGetRootRotation")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        Quaternion GetRootRotation(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.RootPart.Rotation;
            }
        }

        [APILevel(APIFlags.LSL, "llGetRot")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        Quaternion GetRot(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.Rotation;
            }
        }

        [APILevel(APIFlags.LSL, "llGetScale")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        Vector3 GetScale(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.Size;
            }
        }

        [APILevel(APIFlags.LSL, "llPassCollisions")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void PassCollisions(ScriptInstance instance, int pass)
        {
            lock (instance)
            {
                instance.Part.IsPassCollisions = pass != 0;
            }
        }

        [APILevel(APIFlags.LSL, "llPassTouches")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void PassTouches(ScriptInstance instance, int pass)
        {
            lock (instance)
            {
                instance.Part.IsPassTouches = pass != 0;
            }
        }

        [APILevel(APIFlags.LSL, "llSetClickAction")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetClickAction(ScriptInstance instance, int action)
        {
            lock (instance)
            {
                instance.Part.ClickAction = (ClickActionType)action;
            }
        }

        [APILevel(APIFlags.LSL, "llSetPayPrice")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetPayPrice(ScriptInstance instance, int price, AnArray quick_pay_buttons)
        {
            throw new NotImplementedException("llSetPayPrice(int, list)");
        }

        [APILevel(APIFlags.LSL, "llSetPos")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetPos(ScriptInstance instance, Vector3 pos)
        {
            lock (instance)
            {
                instance.Part.Position = pos;
            }
        }

        [APILevel(APIFlags.OSSL, "osSetPrimitiveParams")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetPrimitiveParams(ScriptInstance instance, LSLKey key, AnArray rules)
        {
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                ObjectPart part;
                try
                {
                    part = scene.Primitives[key.AsUUID];
                }
                catch
                {
                    return;
                }
                if(part.Owner != instance.Part.Owner)
                {
                    return;
                }
                part.SetPrimitiveParams(rules.GetMarkEnumerator());
            }
        }

        [APILevel(APIFlags.OSSL, "osGetPrimitiveParams")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        AnArray GetPrimitiveParams(ScriptInstance instance, LSLKey key, AnArray paramList)
        {
            lock (instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                ObjectPart part;
                AnArray res = new AnArray();
                try
                {
                    part = scene.Primitives[key.AsUUID];
                }
                catch
                {
                    return res;
                }
                if (part.Owner != instance.Part.Owner)
                {
                    return res;
                }
                part.GetPrimitiveParams(paramList.GetEnumerator(), ref res);
                return res;
            }
        }

        [APILevel(APIFlags.LSL, "llSetPrimitiveParams")]
        [ForcedSleep(0.2)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetPrimitiveParams(ScriptInstance instance, AnArray rules)
        {
            lock (instance)
            {
                instance.Part.ObjectGroup.SetPrimitiveParams(instance.Part.LinkNumber, LINK_THIS, rules.GetMarkEnumerator());
            }
        }

        [APILevel(APIFlags.LSL, "llSetLinkPrimitiveParams")]
        [ForcedSleep(0.2)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetLinkPrimitiveParams(ScriptInstance instance, int linkTarget, AnArray rules)
        {
            SetLinkPrimitiveParamsFast(instance, linkTarget, rules);
        }

        [APILevel(APIFlags.LSL, "llSetLinkPrimitiveParamsFast")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetLinkPrimitiveParamsFast(ScriptInstance instance, int linkTarget, AnArray rules)
        {
            lock (instance)
            {
                instance.Part.ObjectGroup.SetPrimitiveParams(instance.Part.LinkNumber, linkTarget, rules.GetMarkEnumerator());
            }
        }

        [APILevel(APIFlags.LSL, "llSetScale")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetScale(ScriptInstance instance, Vector3 size)
        {
            lock (instance)
            {
                instance.Part.Size = size;
            }
        }

        [APILevel(APIFlags.LSL, "llSetSitText")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetSitText(ScriptInstance instance, string text)
        {
            lock (instance)
            {
                instance.Part.ObjectGroup.RootPart.SitText = text;
            }
        }

        [APILevel(APIFlags.LSL, "llSetText")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetText(ScriptInstance instance, string text, Vector3 color, double alpha)
        {
            ObjectPart.TextParam tp = new ObjectPart.TextParam();
            tp.Text = text;
            tp.TextColor = new ColorAlpha(color, alpha);
            lock (instance)
            {
                instance.Part.Text = tp;
            }
        }

        [APILevel(APIFlags.LSL, "llSetTouchText")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void SetTouchText(ScriptInstance instance, string text)
        {
            lock (instance)
            {
                instance.Part.ObjectGroup.RootPart.TouchText = text;
            }
        }

        [APILevel(APIFlags.LSL, "llTargetOmega")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void TargetOmega(ScriptInstance instance, Vector3 axis, double spinrate, double gain)
        {
            ObjectPart.OmegaParam op = new ObjectPart.OmegaParam();
            op.Axis = axis;
            op.Spinrate = spinrate;
            op.Gain = gain;
            lock (instance)
            {
                instance.Part.Omega = op;
            }
        }
        #endregion
    }
}
