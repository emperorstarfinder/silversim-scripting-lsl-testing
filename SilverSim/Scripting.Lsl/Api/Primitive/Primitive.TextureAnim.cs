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
using SilverSim.Types.Primitive;

namespace SilverSim.Scripting.Lsl.Api.Primitive
{
    public partial class PrimitiveApi
    {
        #region Texture Animation
        [APILevel(APIFlags.LSL)]
        public const int ANIM_ON = 0x01;
        [APILevel(APIFlags.LSL)]
        public const int LOOP = 0x02;
        [APILevel(APIFlags.LSL)]
        public const int REVERSE = 0x04;
        [APILevel(APIFlags.LSL)]
        public const int PING_PONG = 0x08;
        [APILevel(APIFlags.LSL)]
        public const int SMOOTH = 0x10;
        [APILevel(APIFlags.LSL)]
        public const int ROTATE = 0x20;
        [APILevel(APIFlags.LSL)]
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
