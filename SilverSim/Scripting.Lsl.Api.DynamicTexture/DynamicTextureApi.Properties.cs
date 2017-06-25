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

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Text;

namespace SilverSim.Scripting.Lsl.Api.DynamicTexture
{
    public partial class DynamicTextureApi
    {
        [APIExtension(APIExtension.Properties, "vectordrawlist")]
        [APIDisplayName("vectordrawlist")]
        [APIIsVariableType]
        [ImplementsCustomTypecasts]
        [APICloneOnAssignment]
        [Serializable]
        public class DynamicTextureDrawList
        {
            private readonly StringBuilder m_DrawList = new StringBuilder();

            public DynamicTextureDrawList()
            {
            }

            public DynamicTextureDrawList(DynamicTextureDrawList c)
            {
                m_DrawList = new StringBuilder(c.m_DrawList.ToString());
            }

            public static implicit operator string(DynamicTextureDrawList d) => d.m_DrawList.ToString();

            public static explicit operator DynamicTextureDrawList(string s)
            {
                DynamicTextureDrawList dl = new DynamicTextureDrawList();
                dl.Append(s);
                return dl;
            }

            public void Append(string cmd)
            {
                m_DrawList.Append(cmd);
            }
        }

        [APIExtension(APIExtension.Properties, "osDrawResetTransform")]
        public void DrawResetTransform(ScriptInstance instance, DynamicTextureDrawList drawList)
        {
            drawList.Append(DrawResetTransform());
        }

        [APIExtension(APIExtension.Properties, "osDrawScaleTransform")]
        public void DrawScaleTransform(ScriptInstance instance, DynamicTextureDrawList drawlist, double x, double y)
        {
            drawlist.Append(DrawScaleTransform(x, y));
        }

        [APIExtension(APIExtension.Properties, "osDrawTranslationTransform")]
        public void DrawTranslationTransform(ScriptInstance instance, DynamicTextureDrawList drawlist, double x, double y)
        {
            drawlist.Append(DrawTranslationTransform(x, y));
        }

        [APIExtension(APIExtension.Properties, "osDrawRotationTransform")]
        public void DrawRotationTransform(ScriptInstance instance, DynamicTextureDrawList drawlist, double f)
        {
             drawlist.Append(DrawRotationTransform(f));
        }

        [APIExtension(APIExtension.Properties, "osMovePen")]
        public void MovePen(ScriptInstance instance, DynamicTextureDrawList drawList, int x, int y)
        {
            drawList.Append(MovePen(x, y));
        }

        [APIExtension(APIExtension.Properties, "osDrawLine")]
        public void DrawLine(ScriptInstance instance, DynamicTextureDrawList drawList, int startX, int startY, int endX, int endY)
        {
            drawList.Append(MovePen(startX, startY) + DrawLine(endX, endY));
        }

        [APIExtension(APIExtension.Properties, "osDrawLine")]
        public void DrawLine(ScriptInstance instance, DynamicTextureDrawList drawList, int endX, int endY)
        {
            drawList.Append(DrawLine(endX, endY));
        }

        [APIExtension(APIExtension.Properties, "osDrawText")]
        public void DrawText(ScriptInstance instance, DynamicTextureDrawList drawList, string text)
        {
            drawList.Append(DrawText(text));
        }

        [APIExtension(APIExtension.Properties, "osDrawEllipse")]
        public void DrawEllipse(ScriptInstance instance, DynamicTextureDrawList drawList, int width, int height)
        {
            drawList.Append(DrawEllipse(width, height));
        }

        [APIExtension(APIExtension.Properties, "osDrawFilledEllipse")]
        public void DrawFilledEllipse(ScriptInstance instance, DynamicTextureDrawList drawList, int width, int height)
        {
            drawList.Append(DrawFilledEllipse(width, height));
        }

        [APIExtension(APIExtension.Properties, "osDrawRectangle")]
        public void DrawRectangle(ScriptInstance instance, DynamicTextureDrawList drawList, int width, int height)
        {
            drawList.Append(DrawRectangle(width, height));
        }

        [APIExtension(APIExtension.Properties, "osDrawFilledRectangle")]
        public void DrawFilledRectangle(ScriptInstance instance, DynamicTextureDrawList drawList, int width, int height)
        {
            drawList.Append(DrawFilledRectangle(width, height));
        }

        [APIExtension(APIExtension.Properties, "osDrawFilledPolygon")]
        public void DrawFilledPolygon(ScriptInstance instance, DynamicTextureDrawList drawList, AnArray x, AnArray y)
        {
            drawList.Append(DrawFilledPolygon(x, y));
        }

        [APIExtension(APIExtension.Properties, "osDrawPolygon")]
        public void DrawPolygon(ScriptInstance instance, DynamicTextureDrawList drawList, AnArray x, AnArray y)
        {
            drawList.Append(DrawPolygon(x, y));
        }

        [APIExtension(APIExtension.Properties, "osSetFontSize")]
        public void SetFontSize(ScriptInstance instance, DynamicTextureDrawList drawList, int fontSize)
        {
            drawList.Append(SetFontSize(fontSize));
        }

        [APIExtension(APIExtension.Properties, "osSetFontName")]
        public void SetFontName(ScriptInstance instance, DynamicTextureDrawList drawList, string fontName)
        {
            drawList.Append(SetFontName(fontName));
        }

        [APIExtension(APIExtension.Properties, "osSetPenSize")]
        public void SetPenSize(ScriptInstance instance, DynamicTextureDrawList drawList, int penSize)
        {
            drawList.Append(SetPenSize(penSize));
        }

        [APIExtension(APIExtension.Properties, "osSetPenColor")]
        [APIExtension(APIExtension.Properties, "osSetPenColour")]
        public void SetPenColor(ScriptInstance instance, DynamicTextureDrawList drawList, string color)
        {
            drawList.Append(SetPenColor(color));
        }

        [APIExtension(APIExtension.Properties, "osSetPenCap")]
        public void SetPenCap(ScriptInstance instance, DynamicTextureDrawList drawList, string direction, string type)
        {
            drawList.Append(SetPenCap(direction, type));
        }

        [APIExtension(APIExtension.Properties, "osDrawImage")]
        public void DrawImage(ScriptInstance instance, DynamicTextureDrawList drawList, int width, int height, string imageUrl)
        {
            drawList.Append(DrawImage(width, height, imageUrl));
        }
    }
}
