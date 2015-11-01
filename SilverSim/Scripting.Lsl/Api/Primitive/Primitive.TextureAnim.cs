// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        #region Texture Animation
        [APILevel(APIFlags.LSL, "llSetLinkTextureAnim")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void SetLinkTextureAnim(ScriptInstance instance, int link, int mode, int face, int sizeX, int sizeY, double start, double length, double rate)
        {
            TextureAnimationEntry te = new TextureAnimationEntry();
            te.Flags = (TextureAnimationEntry.TextureAnimMode)mode;
            te.Face = (sbyte)face;
            te.SizeX = (byte)sizeX;
            te.SizeY = (byte)sizeY;
            te.Start = (float)start;
            te.Length = (float)length;
            te.Rate = (float)rate;

            foreach(ObjectPart part in GetLinkTargets(instance, link))
            {
                part.TextureAnimation = te;
            }
        }

        [APILevel(APIFlags.LSL, "llSetTextureAnim")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void SetTextureAnim(ScriptInstance instance, int mode, int face, int sizeX, int sizeY, double start, double length, double rate)
        {
            SetLinkTextureAnim(instance, LINK_THIS, mode, face, sizeX, sizeY, start, length, rate);
        }
        #endregion
    }
}
