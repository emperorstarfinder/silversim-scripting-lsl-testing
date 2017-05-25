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

using OpenJp2.Net;
using SilverSim.Http.Client;
using SilverSim.Types.Asset;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

namespace SilverSim.Scripting.Lsl.Api.DynamicTexture
{
    public static class RenderDynamicTexture
    {
        private const string DefaultFontName = "Arial";

        public static AssetData ToTexture(this Bitmap bmp, bool lossless = false) => new AssetData()
        {
            Type = AssetType.Texture,
            Name = "Dynamic Texture",
            Local = true,
            Flags = AssetFlags.Collectable,
            Data = J2cEncoder.Encode(bmp, lossless)
        };

        public static Bitmap BlendTextures(Bitmap front, Bitmap back, bool setNewAlpha, byte newAlpha)
        {
            if(setNewAlpha)
            {
                for (int w = 0; w < back.Width; w++)
                {
                    for (int h = 0; h < back.Height; h++)
                    {
                        back.SetPixel(w, h, Color.FromArgb(newAlpha, back.GetPixel(w, h)));
                    }
                }
            }

            return MergeBitmaps(front, back);
        }

        public static Bitmap MergeBitmaps(Bitmap front, Bitmap back)
        {
            var joint = new Bitmap(back.Width, back.Height, PixelFormat.Format32bppArgb);
            using (Graphics gfx = Graphics.FromImage(joint))
            {
                gfx.DrawImage(back, 0, 0, back.Width, back.Height);
                gfx.DrawImage(front, 0, 0, back.Width, back.Height);
            }
            return joint;
        }

        public static Bitmap LoadImage(
            string data,
            string extraParams)
        {
            int width = 256;
            int height = 256;

            extraParams = extraParams.Trim().ToLower();

            string[] extraParamPairs = extraParams.Split(',');

            int temp = -1;
            foreach (string pair in extraParamPairs)
            {
                string[] pairset = pair.Split(':');
                string name = pairset[0];
                string value = string.Empty;

                if (pairset.Length > 1)
                {
                    value = pairset[1];
                }

                switch (name)
                {
                    case "width":
                        if (int.TryParse(value, out temp))
                        {
                            width = Types.TypeExtensionMethods.Clamp(temp, 1, 2048);
                        }
                        break;

                    case "height":
                        if (int.TryParse(value, out temp))
                        {
                            height = Types.TypeExtensionMethods.Clamp(temp, 1, 2048);
                        }
                        break;

                    case "":
                        break;

                    default:
                        if (Int32.TryParse(name, out temp))
                        {
                            height = SilverSim.Types.TypeExtensionMethods.Clamp(temp, 128, 1024);
                            width = height;
                        }
                        break;
                }
            }

            using (Stream imgStream = HttpClient.DoStreamGetRequest(data, null, 10000))
            {
                using (Bitmap img = new Bitmap(imgStream))
                {
                    return new Bitmap(img, new Size(width, height));
                }
            }
        }

        public static Bitmap RenderTexture(
            string data,
            string extraParams)
        {
            int width = 256;
            int height = 256;
            int alpha = 255;
            Color backgroundColor = Color.White;
            char altDataDelim = ';';

            extraParams = extraParams.Trim().ToLower();

            int temp = -1;

            foreach(string pair in extraParams.Split(','))
            {
                string[] pairset = pair.Split(':');
                string name = pairset[0];
                string value = string.Empty;

                if(pairset.Length > 1)
                {
                    value = pairset[1];
                }

                switch(name)
                {
                    case "width":
                        if(int.TryParse(value, out temp))
                        {
                            width = SilverSim.Types.TypeExtensionMethods.Clamp(temp, 1, 2048);
                        }
                        break;

                    case "height":
                        if(int.TryParse(value, out temp))
                        {
                            height = SilverSim.Types.TypeExtensionMethods.Clamp(temp, 1, 2048);
                        }
                        break;

                    case "alpha":
                        if(value == "false")
                        {
                            alpha = 256; /* no alpha channel */
                        }
                        else if(int.TryParse(value, out temp))
                        {
                            alpha = SilverSim.Types.TypeExtensionMethods.Clamp(temp, 0, 255);
                        }
                        break;

                    case "bgcolor":
                    case "bgcolour":
                        int hex = 0;
                        backgroundColor = (Int32.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hex)) ?
                            Color.FromArgb(hex) :
                            Color.FromName(value);
                        break;

                    case "altdatadelim":
                        altDataDelim = value[0];
                        break;

                    case "":
                        break;

                    default:
                        if(name == "setalpha")
                        {
                            alpha = 0;
                        }
                        else if(Int32.TryParse(name, out temp))
                        {
                            height = SilverSim.Types.TypeExtensionMethods.Clamp(temp, 128, 1024);
                            width = height;
                        }
                        break;
                }
            }

            /* no dispose of the bitmap we still need it */
            var bitmap = new Bitmap(width, height,
                alpha == 256 ? PixelFormat.Format32bppRgb : PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                /* fully opaque dynamic texture are filled here keeping scripts from doing the same */
                if(alpha >= 255)
                {
                    using (SolidBrush fillBrush = new SolidBrush(backgroundColor))
                    {
                        graphics.FillRectangle(fillBrush, 0, 0, width, height);
                    }
                }
                else
                {
                    Color alphaColor = Color.FromArgb(alpha, backgroundColor);
                    using (SolidBrush fillBrush = new SolidBrush(alphaColor))
                    {
                        graphics.FillRectangle(fillBrush, 0, 0, width, height);
                    }
                }
                DrawToTexture(graphics, data, altDataDelim);
            }
            return bitmap;
        }

        public static void RenderTexture(Bitmap bitmap, string data, string extraParams)
        {
            char altDataDelim = ';';

            extraParams = extraParams.Trim().ToLower();

            foreach (string pair in extraParams.Split(','))
            {
                string[] pairset = pair.Split(':');
                string name = pairset[0];
                string value = string.Empty;

                if (pairset.Length > 1)
                {
                    value = pairset[1];
                }

                if (name == "altdatadelim")
                {
                    altDataDelim = value[0];
                }
            }

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                DrawToTexture(graphics, data, altDataDelim);
            }
        }

        private const char PartsDelimiter = ',';

        private static void GetParams(string line, int startLength, out Point p)
        {
            p = new Point();
            line = line.Remove(0, startLength);
            string[] parts = line.Split(PartsDelimiter);

            if (parts.Length >= 2)
            {
                string xVal = parts[0].Trim();
                string yVal = parts[1].Trim();
                p.X = (int)Convert.ToSingle(xVal, CultureInfo.InvariantCulture);
                p.Y = (int)Convert.ToSingle(yVal, CultureInfo.InvariantCulture);
            }
        }

        private static void GetParams(string line, int startLength, out float x, out float y, out string imgurl)
        {
            x = 0;
            y = 0;
            imgurl = string.Empty;
            line = line.Remove(0, startLength);
            string[] parts = line.Split(new char[] { PartsDelimiter }, 3);

            if (parts.Length >= 2)
            {
                string xVal = parts[0].Trim();
                string yVal = parts[1].Trim();
                x = Convert.ToSingle(xVal, CultureInfo.InvariantCulture);
                y = Convert.ToSingle(yVal, CultureInfo.InvariantCulture);
            }

            if (parts.Length > 2)
            {
                imgurl = parts[2];
            }
        }

        private static void GetParams(string line, int startLength, out PointF[] points)
        {
            line = line.Remove(0, startLength);
            string[] parts = line.Split(PartsDelimiter);

            if (parts.Length > 1 && parts.Length % 2 == 0)
            {
                points = new PointF[parts.Length / 2];
                for (int i = 0; i < parts.Length; i += 2)
                {
                    string xVal = parts[i].Trim();
                    string yVal = parts[i + 1].Trim();
                    float x = Convert.ToSingle(xVal, CultureInfo.InvariantCulture);
                    float y = Convert.ToSingle(yVal, CultureInfo.InvariantCulture);
                    points[i / 2] = new PointF(x, y);
                }
            }
            else
            {
                points = default(PointF[]);
            }
        }

        private static void DrawToTexture(Graphics gfx, string graphdata, char dataDelim)
        {
            var startPoint = new Point(0, 0);
            var endPoint = new Point(0, 0);
            Pen drawPen = null;
            Font drawFont = null;
            SolidBrush drawBrush = null;

            try
            {
                drawPen = new Pen(Color.Black, 7);
                string fontName = DefaultFontName;
                float fontSize = 14;
                drawFont = new Font(fontName, fontSize);
                drawBrush = new SolidBrush(Color.Black);

                foreach(string line in graphdata.Split(dataDelim))
                {
                    string cmdLine = line.Trim();

                    if(cmdLine.StartsWith("MoveTo"))
                    {
                        GetParams(cmdLine, 6, out startPoint);
                    }
                    else if(cmdLine.StartsWith("LineTo"))
                    {
                        GetParams(cmdLine, 6, out endPoint);
                        gfx.DrawLine(drawPen, startPoint, endPoint);
                        startPoint = endPoint;
                    }
                    else if(cmdLine.StartsWith("Text"))
                    {
                        cmdLine = cmdLine.Remove(0, 4).Trim();
                        gfx.DrawString(cmdLine, drawFont, drawBrush, startPoint);
                    }
                    else if(cmdLine.StartsWith("Image"))
                    {
                        string imgurl;
                        float x;
                        float y;
                        GetParams(cmdLine, 5, out x, out y, out imgurl);
                        endPoint.X = (int)x;
                        endPoint.Y = (int)y;
                        try
                        {
                            using (Stream imgStream = HttpClient.DoStreamGetRequest(imgurl, null, 20000))
                            {
                                using (var image = new Bitmap(imgStream))
                                {
                                    gfx.DrawImage(image, (float)startPoint.X, (float)startPoint.Y, (float)endPoint.X, (float)endPoint.Y);
                                }
                            }
                        }
                        catch
                        {
                            using (var errorFont = new Font(DefaultFontName, 6))
                            {
                                gfx.DrawString("URL couldn't be resolved or is", errorFont,
                                                 drawBrush, startPoint);
                                gfx.DrawString("not an image. Please check URL.", errorFont,
                                                 drawBrush, new Point(startPoint.X, 12 + startPoint.Y));
                            }

                            gfx.DrawRectangle(drawPen, startPoint.X, startPoint.Y, endPoint.X, endPoint.Y);
                        }

                        startPoint.X += endPoint.X;
                        startPoint.Y += endPoint.Y;
                    }
                    else if(cmdLine.StartsWith("Rectangle"))
                    {
                        GetParams(cmdLine, 9, out endPoint);
                        gfx.DrawRectangle(drawPen, startPoint.X, startPoint.Y, endPoint.X, endPoint.Y);
                        startPoint.X += endPoint.X;
                        startPoint.Y += endPoint.Y;
                    }
                    else if (cmdLine.StartsWith("FillRectangle"))
                    {
                        GetParams(cmdLine, 13, out endPoint);
                        gfx.FillRectangle(drawBrush, startPoint.X, startPoint.Y, endPoint.X, endPoint.Y);
                        startPoint.X += endPoint.X;
                        startPoint.Y += endPoint.Y;
                    }
                    else if (cmdLine.StartsWith("FillPolygon"))
                    {
                        PointF[] points;
                        GetParams(cmdLine, 11, out points);
                        gfx.FillPolygon(drawBrush, points);
                    }
                    else if (cmdLine.StartsWith("Polygon"))
                    {
                        PointF[] points;
                        GetParams(cmdLine, 7, out points);
                        gfx.DrawPolygon(drawPen, points);
                    }
                    else if (cmdLine.StartsWith("Ellipse"))
                    {
                        GetParams(cmdLine, 7, out endPoint);
                        gfx.DrawEllipse(drawPen, startPoint.X, startPoint.Y, endPoint.X, endPoint.Y);
                        startPoint.X += endPoint.X;
                        startPoint.Y += endPoint.Y;
                    }
                    else if (cmdLine.StartsWith("FontSize"))
                    {
                        cmdLine = cmdLine.Remove(0, 8).Trim();
                        float size;
                        if (float.TryParse(cmdLine, NumberStyles.Float, CultureInfo.InvariantCulture, out size))
                        {
                            fontSize = size;

                            drawFont.Dispose();
                            drawFont = new Font(fontName, fontSize);
                        }
                    }
                    else if (cmdLine.StartsWith("FontProp"))
                    {
                        cmdLine = cmdLine.Remove(0, 8).Trim();

                        foreach (string prop in cmdLine.Split(PartsDelimiter))
                        {
                            switch (prop)
                            {
                                case "B":
                                    if (!(drawFont.Bold))
                                    {
                                        Font newFont = new Font(drawFont, drawFont.Style | FontStyle.Bold);
                                        drawFont.Dispose();
                                        drawFont = newFont;
                                    }
                                    break;

                                case "I":
                                    if (!(drawFont.Italic))
                                    {
                                        Font newFont = new Font(drawFont, drawFont.Style | FontStyle.Italic);
                                        drawFont.Dispose();
                                        drawFont = newFont;
                                    }
                                    break;

                                case "U":
                                    if (!(drawFont.Underline))
                                    {
                                        Font newFont = new Font(drawFont, drawFont.Style | FontStyle.Underline);
                                        drawFont.Dispose();
                                        drawFont = newFont;
                                    }
                                    break;

                                case "S":
                                    if (!(drawFont.Strikeout))
                                    {
                                        Font newFont = new Font(drawFont, drawFont.Style | FontStyle.Strikeout);
                                        drawFont.Dispose();
                                        drawFont = newFont;
                                    }
                                    break;

                                case "R":
                                    if(drawFont.Style != FontStyle.Regular)
                                    {
                                        Font newFont = new Font(drawFont, FontStyle.Regular);
                                        drawFont.Dispose();
                                        drawFont = newFont;
                                    }
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                    else if (cmdLine.StartsWith("FontName"))
                    {
                        cmdLine = cmdLine.Remove(0, 8).Trim();
                        drawFont.Dispose();
                        fontName = cmdLine;
                        drawFont = new Font(fontName, fontSize);
                    }
                    else if (cmdLine.StartsWith("PenSize"))
                    {
                        cmdLine = cmdLine.Remove(0, 7).Trim();
                        float size;
                        if (float.TryParse(cmdLine, NumberStyles.Float, CultureInfo.InvariantCulture, out size))
                        {
                            drawPen.Width = size;
                        }
                    }
                    else if (cmdLine.StartsWith("PenCap"))
                    {
                        PenCap(drawPen, cmdLine);
                    }
                    else if (cmdLine.StartsWith("PenColor") || cmdLine.StartsWith("PenColour"))
                    {
                        cmdLine = cmdLine.Remove(0, 9).Trim();
                        int hex = 0;

                        Color newColor = (Int32.TryParse(cmdLine, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hex)) ?
                            Color.FromArgb(hex) :
                            Color.FromName(cmdLine);

                        drawBrush.Color = newColor;
                        drawPen.Color = newColor;
                    }
                }
            }
            finally
            {
                drawFont?.Dispose();
                drawBrush?.Dispose();
                drawPen?.Dispose();
            }
        }

        private static void PenCap(Pen drawPen, string cmdLine)
        {
            bool start = true;
            bool end = true;

            cmdLine = cmdLine.Remove(0, 6).Trim();
            string[] cap = cmdLine.Split(PartsDelimiter);
            if(cap.Length < 2)
            {
                return;
            }
            if (cap[0].ToLower() == "start")
            {
                end = false;
            }
            else if (cap[0].ToLower() == "end")
            {
                start = false;
            }
            else if (cap[0].ToLower() != "both")
            {
                return;
            }
            string type = cap[1].ToLower();

            if (end)
            {
                switch (type)
                {
                    case "arrow":
                        drawPen.EndCap = LineCap.ArrowAnchor;
                        break;
                    case "round":
                        drawPen.EndCap = LineCap.RoundAnchor;
                        break;
                    case "diamond":
                        drawPen.EndCap = LineCap.DiamondAnchor;
                        break;
                    case "flat":
                        drawPen.EndCap = LineCap.Flat;
                        break;

                    default:
                        break;
                }
            }
            if (start)
            {
                switch (type)
                {
                    case "arrow":
                        drawPen.StartCap = LineCap.ArrowAnchor;
                        break;
                    case "round":
                        drawPen.StartCap = LineCap.RoundAnchor;
                        break;
                    case "diamond":
                        drawPen.StartCap = LineCap.DiamondAnchor;
                        break;
                    case "flat":
                        drawPen.StartCap = LineCap.Flat;
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
