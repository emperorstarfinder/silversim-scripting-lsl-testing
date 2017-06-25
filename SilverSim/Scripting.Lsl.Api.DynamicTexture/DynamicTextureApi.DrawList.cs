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
using System.Globalization;
using System.Text;

namespace SilverSim.Scripting.Lsl.Api.DynamicTexture
{
    public partial class DynamicTextureApi
    {
        private string DrawResetTransform() => "ResetTransf;";
        private string DrawScaleTransform(double x, double y) => "ScaleTransf " + x.ToString(CultureInfo.InvariantCulture) + "," + y.ToString(CultureInfo.InvariantCulture) + ";";
        private string DrawTranslationTransform(double x, double y) => "TransTransf " + x.ToString(CultureInfo.InvariantCulture) + "," + y.ToString(CultureInfo.InvariantCulture) + ";";
        private string DrawRotationTransform(double x) => "RotTransf " + x.ToString(CultureInfo.InvariantCulture) + ";";
        private string MovePen(int x, int y) => string.Format("MoveTo {0},{1};", x, y);
        private string DrawLine(int endX, int endY) => string.Format("LineTo {0},{1};", endX, endY);
        private string DrawText(string text) => string.Format("Text {0};", text);
        private string DrawEllipse(int width, int height) => string.Format("Ellipse {0},{1};", width, height);
        private string DrawFilledEllipse(int width, int height) => string.Format("FillEllipse {0},{1};", width, height);
        private string DrawRectangle(int width, int height) => string.Format("Rectangle {0},{1};", width, height);
        private string DrawFilledRectangle(int width, int height) => string.Format("FillRectangle {0},{1};", width, height);

        private string DrawFilledPolygon(AnArray x, AnArray y)
        {
            int xCount = x.Count;
            if (xCount != y.Count || xCount < 3)
            {
                return "";
            }
            var drawBuild = new StringBuilder();
            drawBuild.AppendFormat("FillPolygon {0},{1}", x[0].AsReal.ToString(), y[0].AsReal.ToString());
            for (int i = 1; i < xCount; i++)
            {
                drawBuild.AppendFormat(",{0},{1}", x[i].AsReal.ToString(), y[i].AsReal.ToString());
            }
            drawBuild.Append(";");
            return drawBuild.ToString();
        }

        private string DrawPolygon(AnArray x, AnArray y)
        {
            int xCount = x.Count;
            if (xCount != y.Count || xCount < 3)
            {
                return "";
            }
            var drawBuild = new StringBuilder();
            drawBuild.AppendFormat("Polygon {0},{1}", x[0].AsReal.ToString(), y[0].AsReal.ToString());
            for (int i = 1; i < xCount; i++)
            {
                drawBuild.AppendFormat(",{0},{1}", x[i].AsReal.ToString(), y[i].AsReal.ToString());
            }
            drawBuild.AppendFormat(";");
            return drawBuild.ToString();
        }


        private string SetFontSize(int fontSize) => "FontSize " + fontSize.ToString() + ";";
        private string SetFontName(string fontName) => "FontName " + fontName + ";";
        private string SetPenSize(int penSize) => "PenSize " + penSize.ToString() + ";";
        private string SetPenColor(string color) => "PenColor " + color + ";";
        private string SetPenCap(string direction, string type) => "PenCap " + direction + "," + type + ";";
        private string DrawImage(int width, int height, string imageUrl) => width.ToString() + "," + height.ToString() + "," + imageUrl + ";";

        [APILevel(APIFlags.OSSL, "osDrawResetTransform")]
        public string DrawResetTransform(ScriptInstance instance, string drawList) => drawList + DrawResetTransform();
        [APILevel(APIFlags.OSSL, "osDrawScaleTransform")]
        public string DrawScaleTransform(ScriptInstance instance, string drawlist, double x, double y) => drawlist + DrawScaleTransform(x, y);
        [APILevel(APIFlags.OSSL, "osDrawTranslationTransform")]
        public string DrawTranslationTransform(ScriptInstance instance, string drawlist, double x, double y) => drawlist + DrawTranslationTransform(x, y);
        [APILevel(APIFlags.OSSL, "osDrawRotationTransform")]
        public string DrawRotationTransform(ScriptInstance instance, string drawlist, double f) => drawlist + DrawRotationTransform(f);
        [APILevel(APIFlags.OSSL, "osMovePen")]
        public string MovePen(ScriptInstance instance, string drawList, int x, int y) => drawList + MovePen(x, y);
        [APILevel(APIFlags.OSSL, "osDrawLine")]
        public string DrawLine(ScriptInstance instance, string drawList, int startX, int startY, int endX, int endY) =>
            drawList + MovePen(startX, startY) + DrawLine(endX, endY);
        [APILevel(APIFlags.OSSL, "osDrawLine")]
        public string DrawLine(ScriptInstance instance, string drawList, int endX, int endY) => drawList + DrawLine(endX, endY);
        [APILevel(APIFlags.OSSL, "osDrawText")]
        public string DrawText(ScriptInstance instance, string drawList, string text) => drawList + DrawText(text);
        [APILevel(APIFlags.OSSL, "osDrawEllipse")]
        public string DrawEllipse(ScriptInstance instance, string drawList, int width, int height) => drawList + DrawEllipse(width, height);
        [APILevel(APIFlags.OSSL, "osDrawFilledEllipse")]
        public string DrawFilledEllipse(ScriptInstance instance, string drawList, int width, int height) => drawList + DrawFilledEllipse(width, height);
        [APILevel(APIFlags.OSSL, "osDrawRectangle")]
        public string DrawRectangle(ScriptInstance instance, string drawList, int width, int height) => drawList + DrawRectangle(width, height);
        [APILevel(APIFlags.OSSL, "osDrawFilledRectangle")]
        public string DrawFilledRectangle(ScriptInstance instance, string drawList, int width, int height) => drawList + DrawFilledRectangle(width, height);
        [APILevel(APIFlags.OSSL, "osDrawFilledPolygon")]
        public string DrawFilledPolygon(ScriptInstance instance, string drawList, AnArray x, AnArray y) =>
            drawList + DrawFilledPolygon(x, y);
        [APILevel(APIFlags.OSSL, "osDrawPolygon")]
        public string DrawPolygon(ScriptInstance instance, string drawList, AnArray x, AnArray y) =>
            drawList + DrawPolygon(x, y);
        [APILevel(APIFlags.OSSL, "osSetFontSize")]
        public string SetFontSize(ScriptInstance instance, string drawList, int fontSize) => drawList + SetFontSize(fontSize);
        [APILevel(APIFlags.OSSL, "osSetFontName")]
        public string SetFontName(ScriptInstance instance, string drawList, string fontName) => drawList + SetFontName(fontName);
        [APILevel(APIFlags.OSSL, "osSetPenSize")]
        public string SetPenSize(ScriptInstance instance, string drawList, int penSize) => drawList + SetPenSize(penSize);
        [APILevel(APIFlags.OSSL, "osSetPenColor")]
        [APILevel(APIFlags.OSSL, "osSetPenColour")]
        public string SetPenColor(ScriptInstance instance, string drawList, string color) => drawList + SetPenColor(color);
        [APILevel(APIFlags.OSSL, "osSetPenCap")]
        public string SetPenCap(ScriptInstance instance, string drawList, string direction, string type) => drawList + SetPenCap(direction, type);
        [APILevel(APIFlags.OSSL, "osDrawImage")]
        public string DrawImage(ScriptInstance instance, string drawList, int width, int height, string imageUrl) => drawList + DrawImage(width, height, imageUrl);
    }
}
