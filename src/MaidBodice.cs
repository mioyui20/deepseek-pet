using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace DaFeiYu {
    static class MaidBodice {
        public static Bitmap PaintShoulders(Bitmap coat, Point origin) {
            var foreground = new Bitmap(coat.Width, coat.Height, PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(foreground))
            using (var original = new TextureBrush(coat, WrapMode.Clamp))
            using (var trim = new GraphicsPath())
            using (var toBitmap = new Matrix(1, 0, 0, 1, -origin.X, -origin.Y)) {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // Trace the lace and the two locks that hang in front of the shoulders.
                trim.AddPolygon(new[] { P(182,172), P(234,172), P(238,182), P(235,195), P(229,211),
                    P(187,211), P(183,199), P(180,190), P(181,179) });
                AddHairLock(trim, false);
                AddHairLock(trim, true);
                trim.Transform(toBitmap);
                g.FillPath(original, trim);
            }

            using (var g = Graphics.FromImage(coat)) {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TranslateTransform(-origin.X, -origin.Y);
                PaintHairBehindSleeve(g);
                g.TranslateTransform(416, 0);
                g.ScaleTransform(-1, 1);
                PaintHairBehindSleeve(g);
            }
            return foreground;
        }

        static void AddHairLock(GraphicsPath trim, bool mirror) {
            using (var lockPath = new GraphicsPath()) {
                lockPath.AddBezier(P(158,160), P(163,162), P(172,166), P(179,170));
                lockPath.AddBezier(P(179,170), P(182,181), P(174,191), P(163,192));
                lockPath.AddBezier(P(163,192), P(156,192), P(150,190), P(147,184));
                lockPath.AddBezier(P(147,184), P(154,187), P(162,184), P(163,179));
                lockPath.AddBezier(P(163,179), P(159,172), P(159,166), P(158,160));
                lockPath.CloseFigure();
                if (mirror) using (var reflection = new Matrix(-1, 0, 0, 1, 416, 0)) lockPath.Transform(reflection);
                trim.AddPath(lockPath, false);
            }
        }

        static void PaintHairBehindSleeve(Graphics g) {
            using (var strand = new GraphicsPath()) {
                // Cover the original sleeve, gold cuff and hand back into the blue hair.
                strand.AddBezier(P(175,177), P(164,181), P(160,189), P(156,197));
                strand.AddBezier(P(156,197), P(150,208), P(162,220), P(160,230));
                strand.AddBezier(P(160,230), P(167,226), P(176,221), P(177,216));
                strand.AddBezier(P(177,216), P(181,212), P(184,212), P(187,210));
                strand.AddBezier(P(187,210), P(182,203), P(183,185), P(186,178));
                strand.CloseFigure();
                using (var blue = new LinearGradientBrush(P(171,179), P(157,226),
                    Color.FromArgb(43, 54, 118), Color.FromArgb(59, 106, 183))) g.FillPath(blue, strand);

                var saved = g.Save();
                g.SetClip(strand, CombineMode.Intersect);
                using (var light = new Pen(Color.FromArgb(83, 97, 153, 222), 3) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawBezier(light, P(165,188), P(157,206), P(174,211), P(161,227));
                g.Restore(saved);
            }
        }

        static PointF P(float x, float y) { return new PointF(x, y); }
    }
}
