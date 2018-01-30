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

        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "Append")]
        public void Append(DynamicTextureDrawList drawlist, DynamicTextureDrawList src) => drawlist.Append(src);

        [APIExtension(APIExtension.Properties, "osDrawResetTransform")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "ResetTransform")]
        public void DrawResetTransform(DynamicTextureDrawList drawList)
        {
            drawList.Append(DrawResetTransform());
        }

        [APIExtension(APIExtension.Properties, "osDrawScaleTransform")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "ScaleTransform")]
        public void DrawScaleTransform(DynamicTextureDrawList drawlist, double x, double y)
        {
            drawlist.Append(DrawScaleTransform(x, y));
        }

        [APIExtension(APIExtension.Properties, "osDrawTranslationTransform")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "TranslateTransform")]
        public void DrawTranslationTransform(DynamicTextureDrawList drawlist, double x, double y)
        {
            drawlist.Append(DrawTranslationTransform(x, y));
        }

        [APIExtension(APIExtension.Properties, "osDrawRotationTransform")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "RotateTransform")]
        public void DrawRotationTransform(DynamicTextureDrawList drawlist, double f)
        {
             drawlist.Append(DrawRotationTransform(f));
        }

        [APIExtension(APIExtension.Properties, "osMovePen")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "MovePen")]
        public void MovePen(DynamicTextureDrawList drawList, int x, int y)
        {
            drawList.Append(MovePen(x, y));
        }

        [APIExtension(APIExtension.Properties, "osDrawLine")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "DrawLine")]
        public void DrawLine(DynamicTextureDrawList drawList, int startX, int startY, int endX, int endY)
        {
            drawList.Append(MovePen(startX, startY) + DrawLine(endX, endY));
        }

        [APIExtension(APIExtension.Properties, "osDrawLine")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "DrawLine")]
        public void DrawLine(DynamicTextureDrawList drawList, int endX, int endY)
        {
            drawList.Append(DrawLine(endX, endY));
        }

        [APIExtension(APIExtension.Properties, "osDrawText")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "DrawText")]
        public void DrawText(DynamicTextureDrawList drawList, string text)
        {
            drawList.Append(DrawText(text));
        }

        [APIExtension(APIExtension.Properties, "osDrawEllipse")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "DrawEllipse")]
        public void DrawEllipse(DynamicTextureDrawList drawList, int width, int height)
        {
            drawList.Append(DrawEllipse(width, height));
        }

        [APIExtension(APIExtension.Properties, "osDrawFilledEllipse")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "DrawFilledEllipse")]
        public void DrawFilledEllipse(DynamicTextureDrawList drawList, int width, int height)
        {
            drawList.Append(DrawFilledEllipse(width, height));
        }

        [APIExtension(APIExtension.Properties, "osDrawRectangle")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "DrawRectangle")]
        public void DrawRectangle(DynamicTextureDrawList drawList, int width, int height)
        {
            drawList.Append(DrawRectangle(width, height));
        }

        [APIExtension(APIExtension.Properties, "osDrawFilledRectangle")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "DrawFilledRectangle")]
        public void DrawFilledRectangle(DynamicTextureDrawList drawList, int width, int height)
        {
            drawList.Append(DrawFilledRectangle(width, height));
        }

        [APIExtension(APIExtension.Properties, "osDrawFilledPolygon")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "DrawFilledPolygon")]
        public void DrawFilledPolygon(DynamicTextureDrawList drawList, AnArray x, AnArray y)
        {
            drawList.Append(DrawFilledPolygon(x, y));
        }

        [APIExtension(APIExtension.Properties, "osDrawPolygon")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "DrawPolygon")]
        public void DrawPolygon(DynamicTextureDrawList drawList, AnArray x, AnArray y)
        {
            drawList.Append(DrawPolygon(x, y));
        }

        [APIExtension(APIExtension.Properties, "osSetFontSize")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "SetFontSize")]
        public void SetFontSize(DynamicTextureDrawList drawList, int fontSize)
        {
            drawList.Append(SetFontSize(fontSize));
        }

        [APIExtension(APIExtension.Properties, "osSetFontName")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "SetFontName")]
        public void SetFontName(DynamicTextureDrawList drawList, string fontName)
        {
            drawList.Append(SetFontName(fontName));
        }

        [APIExtension(APIExtension.Properties, "osSetPenSize")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "SetPenSize")]
        public void SetPenSize(DynamicTextureDrawList drawList, int penSize)
        {
            drawList.Append(SetPenSize(penSize));
        }

        [APIExtension(APIExtension.Properties, "osSetPenColor")]
        [APIExtension(APIExtension.Properties, "osSetPenColour")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "SetPenColor")]
        public void SetPenColor(DynamicTextureDrawList drawList, string color)
        {
            drawList.Append(SetPenColor(color));
        }

        [APIExtension(APIExtension.Properties, "osSetPenCap")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "SetPenCap")]
        public void SetPenCap(DynamicTextureDrawList drawList, string direction, string type)
        {
            drawList.Append(SetPenCap(direction, type));
        }

        [APIExtension(APIExtension.Properties, "osDrawImage")]
        [APIExtension(APIExtension.MemberFunctions, APIUseAsEnum.MemberFunction, "DrawImage")]
        public void DrawImage(DynamicTextureDrawList drawList, int width, int height, string imageUrl)
        {
            drawList.Append(DrawImage(width, height, imageUrl));
        }
    }
}
