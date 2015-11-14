// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types.Primitive;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        #region Texture Animation
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int ANIM_ON = 0x01;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int LOOP = 0x02;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int REVERSE = 0x04;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int PING_PONG = 0x08;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int SMOOTH = 0x10;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int ROTATE = 0x20;
        [APILevel(APIFlags.LSL, APILevel.KeepCsName)]
        public const int SCALE = 0x40;

        [APILevel(APIFlags.LSL, "llSetLinkTextureAnim")]
        public void SetLinkTextureAnim(ScriptInstance instance, int link, int mode, int face, int sizeX, int sizeY, double start, double length, double rate)
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
        public void SetTextureAnim(ScriptInstance instance, int mode, int face, int sizeX, int sizeY, double start, double length, double rate)
        {
            SetLinkTextureAnim(instance, LINK_THIS, mode, face, sizeX, sizeY, start, length, rate);
        }
        #endregion
    }
}
