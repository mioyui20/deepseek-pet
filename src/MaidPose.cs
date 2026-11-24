using System;
using System.Drawing;

namespace DaFeiYu {
    struct MaidArmPose {
        public readonly PointF Shoulder, Elbow, Wrist, Fingers;

        public MaidArmPose(PointF shoulder, PointF elbow, PointF wrist, PointF fingers) {
            Shoulder = shoulder;
            Elbow = elbow;
            Wrist = wrist;
            Fingers = fingers;
        }
    }

    struct MaidPose {
        public const float TableHeightOffset = -10;
        // This edge of the cheeks in the atlas stays fixed against the wrist at rest.
        public static readonly PointF UprightCheek = new PointF(196, 171);
        public readonly MaidArmPose MouseArm, KeyArm;
        public readonly PointF Cheek;
        public readonly float Lean, Roll, ShoulderDrop, BodiceShift, BodiceHeight, Contact;

        public MaidPose(PointF mouseCenter, PointF keyContact, float press, MotionFrame motion) {
            float fold = Ease(motion.Rest / .69f);
            Lean = Ease((motion.Rest - .08f) / .74f);
            Contact = Ease((motion.Rest - .38f) / .60f);

            float keyReach = keyContact.X - 250;
            float reach = Ease((Math.Abs(keyReach) - 20) / 55) * (1 - fold);
            float shoulderShift = reach * keyReach * .20f;
            BodiceShift = shoulderShift - Lean * 7;
            BodiceHeight = 1 - Lean * .19f;
            // The body leans toward distant keys while the sleeping cheek stays supported.
            ShoulderDrop = Lean * (22 + TableHeightOffset) + reach * 13 + motion.Bob * (.75f - .47f * Lean);
            var mouseForward = new PointF(-.45f, .89f);
            var mouseWrist = new PointF(mouseCenter.X - mouseForward.X * 8.5f,
                mouseCenter.Y - mouseForward.Y * 8.5f + press);
            var mouseElbow = new PointF(151 + (mouseWrist.X - 129) * .39f,
                221 + (mouseWrist.Y - 237) * .46f + motion.Bob * .35f);
            var restingMouseWrist = new PointF(194, 251 + TableHeightOffset);
            MouseArm = new MaidArmPose(
                new PointF(177 + BodiceShift, 164 + 25 * BodiceHeight + ShoulderDrop),
                Lerp(mouseElbow, new PointF(153, 242 + TableHeightOffset), fold),
                Lerp(mouseWrist, restingMouseWrist, fold),
                Lerp(mouseForward, Unit(new PointF(1, .17f)), fold));

            var keyElbow = new PointF(255 + (keyContact.X - 248) * .48f + shoulderShift * .45f,
                218 + (keyContact.Y - 244) * .24f + reach * 10 + motion.Bob * .35f);
            var keyForward = Unit(new PointF((keyContact.X - keyElbow.X) * .83f,
                Math.Max(19, keyContact.Y - keyElbow.Y)));
            var keyWrist = new PointF(keyContact.X - keyForward.X * 11, keyContact.Y - keyForward.Y * 11);
            KeyArm = new MaidArmPose(
                new PointF(240 + BodiceShift, 164 + 25 * BodiceHeight + ShoulderDrop),
                Lerp(keyElbow, new PointF(249, 256 + TableHeightOffset), fold),
                Lerp(keyWrist, new PointF(215, 266 + TableHeightOffset), fold),
                Lerp(keyForward, Unit(new PointF(-1, .19f)), fold));

            float nod = motion.Nod * Lean * (1 - Contact);
            Cheek = Lerp(new PointF(UprightCheek.X - 3 * Lean + shoulderShift, UprightCheek.Y + ShoulderDrop + nod * 5),
                new PointF(restingMouseWrist.X + 3, restingMouseWrist.Y - 4), Contact);
            Roll = motion.Tilt * .7f * (1 - Lean) + shoulderShift * .25f - Lean * 9 - nod * 3;
        }

        public static PointF Unit(PointF vector) {
            float length = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            return length > .001f ? new PointF(vector.X / length, vector.Y / length) : new PointF(0, 1);
        }

        public static PointF Lerp(PointF a, PointF b, float amount) {
            return new PointF(a.X + (b.X - a.X) * amount, a.Y + (b.Y - a.Y) * amount);
        }

        static float Ease(float value) {
            float t = Math.Max(0, Math.Min(1, value));
            return t * t * (3 - 2 * t);
        }
    }
}
