using System;
using System.Drawing;

namespace DaFeiYu {
    sealed class PointerMotion {
        PointF lead;
        public PointF Position { get; private set; }

        public void Advance(PointF target, double elapsed) {
            // Two critically damped stages preserve velocity when the cursor reverses.
            float step = (float)(27 * Math.Max(0, Math.Min(.1, elapsed)));
            float decay = (float)Math.Exp(-step);
            Position = new PointF(
                target.X + (Position.X - target.X + step * (lead.X - target.X)) * decay,
                target.Y + (Position.Y - target.Y + step * (lead.Y - target.Y)) * decay);
            lead = MaidPose.Lerp(target, lead, decay);
        }
    }
}
