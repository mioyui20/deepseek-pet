using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace DaFeiYu {
    sealed class MaidSleeve : IDisposable {
        readonly Bitmap cuffAndHand;
        readonly int outward;

        public MaidSleeve(Bitmap atlas, int x, int outward) {
            cuffAndHand = atlas.Clone(new Rectangle(x, 799, 143, 123), PixelFormat.Format32bppPArgb);
            this.outward = outward;
        }

        public void DrawSleeve(Graphics g, MaidArmPose arm) {
            PointF upper = Direction(arm.Shoulder, arm.Elbow);
            PointF lower = Direction(arm.Elbow, arm.Wrist);
            PointF fingers = MaidPose.Unit(arm.Fingers);
            PointF joint = MaidPose.Unit(new PointF(upper.X + lower.X, upper.Y + lower.Y));
            PointF shoulder = arm.Shoulder;
            float lowerLength = (arm.Wrist.X - arm.Elbow.X) * lower.X + (arm.Wrist.Y - arm.Elbow.Y) * lower.Y;
            float bend = Math.Min(10, lowerLength * .4f);
            PointF cuff = Along(arm.Wrist, lower, -Math.Min(3.5f, lowerLength * .22f));
            PointF cuffAxis = MaidPose.Unit(MaidPose.Lerp(lower, fingers, .3f));
            PointF shoulderOuter = FromShoulder(shoulder, 6, -8), shoulderInner = FromShoulder(shoulder, -9, -7);
            float elbowWidth = 10.5f * outward, cuffWidth = 7.3f * outward;
            PointF elbowOuter = Across(arm.Elbow, joint, elbowWidth), elbowInner = Across(arm.Elbow, joint, -elbowWidth);
            PointF cuffOuter = Across(cuff, cuffAxis, cuffWidth), cuffInner = Across(cuff, cuffAxis, -cuffWidth);

            using (var sleeve = new GraphicsPath()) {
                sleeve.AddBezier(shoulderOuter, Across(Along(shoulder, upper, 8), upper, 23 * outward),
                    Across(Along(elbowOuter, joint, -10), upper, 4 * outward), elbowOuter);
                sleeve.AddBezier(elbowOuter, Along(elbowOuter, joint, bend), Along(cuffOuter, cuffAxis, -bend), cuffOuter);
                sleeve.AddLine(cuffOuter, cuffInner);
                sleeve.AddBezier(cuffInner, Along(cuffInner, cuffAxis, -bend), Along(elbowInner, joint, bend), elbowInner);
                sleeve.AddBezier(elbowInner, Along(elbowInner, joint, -12),
                    FromShoulder(shoulder, -12, 10), shoulderInner);
                sleeve.AddBezier(shoulderInner, FromShoulder(shoulder, -11, -13), FromShoulder(shoulder, 1, -13), shoulderOuter);
                sleeve.CloseFigure();
                using (var cloth = new LinearGradientBrush(FromShoulder(shoulder, 7, -9), cuff,
                    Color.FromArgb(59, 76, 138), Color.FromArgb(28, 37, 83)))
                using (var outline = new Pen(Color.FromArgb(21, 28, 60), 1.15f) { LineJoin = LineJoin.Round }) {
                    g.FillPath(cloth, sleeve);
                    PaintFolds(g, sleeve, shoulder, upper, arm.Elbow, joint);
                    g.DrawPath(outline, sleeve);
                }
            }
        }

        void PaintFolds(Graphics g, GraphicsPath sleeve, PointF shoulder, PointF upper, PointF elbow, PointF joint) {
            var saved = g.Save();
            g.SetClip(sleeve, CombineMode.Intersect);

            // Paint one soft highlight across the puff and a fold along its underside.
            PointF top = FromShoulder(shoulder, 4, -8);
            PointF middle = MaidPose.Lerp(shoulder, elbow, .72f);
            PointF tip = Across(middle, upper, 10 * outward);
            using (var highlight = new GraphicsPath()) {
                highlight.AddBezier(top, Across(Along(shoulder, upper, 4), upper, 19 * outward),
                    Across(Along(middle, upper, -4), upper, 16 * outward), tip);
                highlight.AddBezier(tip, Across(middle, upper, 4 * outward), FromShoulder(shoulder, -7, 0), top);
                highlight.CloseFigure();
                using (var light = new LinearGradientBrush(top, tip, Color.FromArgb(108, 94, 119, 183),
                    Color.FromArgb(0, 94, 119, 183))) g.FillPath(light, highlight);
            }

            PointF fold = Across(Along(elbow, joint, -5), joint, -8 * outward);
            using (var shade = new Pen(Color.FromArgb(106, 21, 29, 66), 1.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round }) {
                g.DrawBezier(shade, FromShoulder(shoulder, -7, 2), FromShoulder(shoulder, -3, 8),
                    Across(middle, upper, -9 * outward), fold);
                g.DrawBezier(shade, fold, Along(fold, joint, 7), Across(elbow, joint, -1 * outward),
                    Across(Along(elbow, joint, 2), joint, 4 * outward));
            }
            g.Restore(saved);
        }

        PointF FromShoulder(PointF shoulder, float x, float y) { return new PointF(shoulder.X + x * outward, shoulder.Y + y); }

        public void DrawHand(Graphics g, MaidArmPose arm) {
            // The lace and fingers keep their dimensions as the forearm folds.
            PointF fingers = MaidPose.Unit(arm.Fingers);
            DrawAlong(g, cuffAndHand, Along(arm.Wrist, fingers, -8.9f), fingers, 27, 20);
        }

        static void DrawAlong(Graphics g, Bitmap art, PointF top, PointF forward, float width, float length) {
            PointF left = Across(top, forward, -width / 2);
            g.DrawImage(art, new[] { left, Across(top, forward, width / 2), Along(left, forward, length) });
        }

        static PointF Direction(PointF a, PointF b) {
            return MaidPose.Unit(new PointF(b.X - a.X, b.Y - a.Y));
        }

        static PointF Across(PointF p, PointF direction, float distance) {
            return new PointF(p.X + direction.Y * distance, p.Y - direction.X * distance);
        }

        static PointF Along(PointF p, PointF direction, float distance) {
            return new PointF(p.X + direction.X * distance, p.Y + direction.Y * distance);
        }

        public void Dispose() { cuffAndHand.Dispose(); }
    }
}
