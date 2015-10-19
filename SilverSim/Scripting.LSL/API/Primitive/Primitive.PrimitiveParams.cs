﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using System;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        #region Primitives

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetKey")]
        public LSLKey GetKey(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ID;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llAllowInventoryDrop")]
        public void AllowInventoryDrop(ScriptInstance instance, int add)
        {
            lock (instance)
            {
                instance.Part.IsAllowedDrop = add != 0;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetLinkPrimitiveParams")]
        public AnArray GetLinkPrimitiveParams(ScriptInstance instance, int link, AnArray param)
        {
            AnArray parout = new AnArray();
            lock (instance)
            {
                instance.Part.ObjectGroup.GetPrimitiveParams(instance.Part.LinkNumber, link, param.GetEnumerator(), ref parout);
            }
            return parout;
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(0.2)]
        [ScriptFunctionName("llGetPrimitiveParams")]
        public AnArray GetPrimitiveParams(ScriptInstance instance, AnArray param)
        {
            AnArray parout = new AnArray();
            lock (instance)
            {
                instance.Part.ObjectGroup.GetPrimitiveParams(instance.Part.LinkNumber, LINK_THIS, param.GetEnumerator(), ref parout);
            }
            return parout;
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetLocalPos")]
        public Vector3 GetLocalPos(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.LocalPosition;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetPos")]
        public Vector3 GetPos(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.Position;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetRootPosition")]
        public Vector3 GetRootPosition(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.Position;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetRootRotation")]
        public Quaternion GetRootRotation(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.ObjectGroup.RootPart.Rotation;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetRot")]
        public Quaternion GetRot(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.Rotation;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llGetScale")]
        public Vector3 GetScale(ScriptInstance instance)
        {
            lock (instance)
            {
                return instance.Part.Size;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llPassCollisions")]
        public void PassCollisions(ScriptInstance instance, int pass)
        {
            lock (instance)
            {
                instance.Part.IsPassCollisions = pass != 0;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llPassTouches")]
        public void PassTouches(ScriptInstance instance, int pass)
        {
            lock (instance)
            {
                instance.Part.IsPassTouches = pass != 0;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetClickAction")]
        public void SetClickAction(ScriptInstance instance, int action)
        {
            lock (instance)
            {
                instance.Part.ClickAction = (ClickActionType)action;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetPayPrice")]
        public void SetPayPrice(ScriptInstance instance, int price, AnArray quick_pay_buttons)
        {
            throw new NotImplementedException("llSetPayPrice(int, list)");
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetPos")]
        public void SetPos(ScriptInstance instance, Vector3 pos)
        {
            lock (instance)
            {
                instance.Part.Position = pos;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(0.2)]
        [ScriptFunctionName("llSetPrimitiveParams")]
        public void SetPrimitiveParams(ScriptInstance instance, AnArray rules)
        {
            lock (instance)
            {
                instance.Part.ObjectGroup.SetPrimitiveParams(instance.Part.LinkNumber, LINK_THIS, rules.GetMarkEnumerator());
            }
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(0.2)]
        [ScriptFunctionName("llSetLinkPrimitiveParams")]
        public void SetLinkPrimitiveParams(ScriptInstance instance, int linkTarget, AnArray rules)
        {
            SetLinkPrimitiveParamsFast(instance, linkTarget, rules);
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetLinkPrimitiveParamsFast")]
        public void SetLinkPrimitiveParamsFast(ScriptInstance instance, int linkTarget, AnArray rules)
        {
            lock (instance)
            {
                instance.Part.ObjectGroup.SetPrimitiveParams(instance.Part.LinkNumber, linkTarget, rules.GetMarkEnumerator());
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetScale")]
        public void SetScale(ScriptInstance instance, Vector3 size)
        {
            lock (instance)
            {
                instance.Part.Size = size;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetSitText")]
        public void SetSitText(ScriptInstance instance, string text)
        {
            lock (instance)
            {
                instance.Part.ObjectGroup.RootPart.SitText = text;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetText")]
        public void SetText(ScriptInstance instance, string text, Vector3 color, double alpha)
        {
            ObjectPart.TextParam tp = new ObjectPart.TextParam();
            tp.Text = text;
            tp.TextColor = new ColorAlpha(color, alpha);
            lock (instance)
            {
                instance.Part.Text = tp;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llSetTouchText")]
        public void SetTouchText(ScriptInstance instance, string text)
        {
            lock (instance)
            {
                instance.Part.ObjectGroup.RootPart.TouchText = text;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ScriptFunctionName("llTargetOmega")]
        public void TargetOmega(ScriptInstance instance, Vector3 axis, double spinrate, double gain)
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
