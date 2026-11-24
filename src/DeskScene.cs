using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DaFeiYu {
    sealed class DeskScene : IDisposable {

        readonly Bitmap coat, bodiceForeground;

        readonly Bitmap[] heads=new Bitmap[6];
        readonly MaidSleeve keySleeve, mouseSleeve;
        readonly List<KeyCap> keys = new List<KeyCap>();
        PointF leftTip = new PointF(251,262);
        float mousePress;
        double lastFrame = -1;
        sealed class KeyCap {
            public int Vk; public string Label; public RectangleF Rect;
            public KeyCap(int vk, string label, float x, float y, float w) { Vk=vk; Label=label; Rect=new RectangleF(x,y,w,12); }
        }
        public DeskScene() {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DaFeiYu.Art"))
            using (var atlas = new Bitmap(stream)) {
                CleanAtlasAlpha(atlas);
                int[] cells = { 0, 1, 2, 3, 4, 0 };
                for (int i = 0; i < heads.Length; i++) {
                    using (var figure = DrawFigure(atlas, cells[i])) {
                        if (i == 0)
                            coat = figure.Clone(new Rectangle(0, NeckLine, figure.Width, figure.Height - NeckLine), PixelFormat.Format32bppPArgb);
                        heads[i] = figure.Clone(new Rectangle(0, 0, figure.Width, HeadHeight), PixelFormat.Format32bppPArgb);
                        FeatherBottom(heads[i], NeckLine);
                    }
                }
                bodiceForeground = MaidBodice.PaintShoulders(coat, new Point(FigureX, FigureY + NeckLine));
                mouseSleeve = new MaidSleeve(atlas, 1102, -1);
                keySleeve = new MaidSleeve(atlas, 1309, 1);
            }
            AddKeys();
        }
        const int FigureX = 88, FigureY = 24, FigureWidth = 240, FigureHeight = 232;
        const int NeckLine = 140, HeadHeight = 163;
        const float PadU = 8, PadV = 2, PadWidth = 138, PadDepth = 90;
        const float MouseCenterU = 109, MouseCenterV = 43, MouseHalfWidth = 17, MouseHalfDepth = 23;
        const float KeyboardU = 146, KeyboardV = 6, KeyboardWidth = 190, KeyboardDepth = 70;

        static Bitmap DrawFigure(Bitmap atlas, int cell) {
            var figure = new Bitmap(FigureWidth, FigureHeight, PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(figure)) {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(atlas, new Rectangle(0, 0, FigureWidth, FigureHeight),
                    new Rectangle(cell % 3 * 512, cell / 3 * 512, 512, 512), GraphicsUnit.Pixel);
            }
            return figure;
        }
        static void CleanAtlasAlpha(Bitmap atlas) {
            // The generated image has a faint translucent backdrop. Its opacity
            // is lower than that of the art, so remove it before resampling.
            var bounds = new Rectangle(0, 0, atlas.Width, atlas.Height);
            var data = atlas.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try {
                var row = new int[atlas.Width];
                for (int y = 0; y < atlas.Height; y++) {
                    IntPtr address = IntPtr.Add(data.Scan0, y * data.Stride);
                    Marshal.Copy(address, row, 0, row.Length);
                    for (int x = 0; x < row.Length; x++) {
                        int alpha = (int)((uint)row[x] >> 24);
                        int opacity = (int)(255 * SmoothStep((alpha - 205) / 43f));
                        row[x] = opacity == 0 ? 0 : (row[x] & 0x00ffffff) | (opacity << 24);
                    }
                    Marshal.Copy(row, 0, address, row.Length);
                }
            } finally { atlas.UnlockBits(data); }
        }
        static void FeatherBottom(Bitmap sprite, int start) {
            var data = sprite.LockBits(new Rectangle(0, 0, sprite.Width, sprite.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
            try {
                var row = new int[sprite.Width];
                for (int y = start; y < sprite.Height; y++) {
                    IntPtr address = IntPtr.Add(data.Scan0, y * data.Stride);
                    Marshal.Copy(address, row, 0, row.Length);
                    float opacity = 1 - SmoothStep((y - start) / (float)(sprite.Height - start - 1));
                    for (int x = 0; x < row.Length; x++) row[x] = ScalePixel(row[x], opacity);
                    Marshal.Copy(row, 0, address, row.Length);
                }
            } finally { sprite.UnlockBits(data); }
        }
        public Bitmap CreateThumbnail() {
            var thumbnail = new Bitmap(48, 48, PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(thumbnail)) {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(heads[0], new Rectangle(0, 0, 48, 48),
                    new Rectangle(43, 17, 153, 153), GraphicsUnit.Pixel);
            }
            return thumbnail;
        }
        void AddKeys() {
            AddRow(244, new int[]{192,49,50,51,52,53,54,55,56,57,48,189,187,8}, new string[]{"`","1","2","3","4","5","6","7","8","9","0","-","=","←"});
            AddRow(259, new int[]{9,81,87,69,82,84,89,85,73,79,80,219,221,220}, new string[]{"Tab","Q","W","E","R","T","Y","U","I","O","P","[","]","\\"});
            AddRow(274, new int[]{20,65,83,68,70,71,72,74,75,76,186,222,13}, new string[]{"Caps","A","S","D","F","G","H","J","K","L",";","'","↵"});
            AddRow(289, new int[]{160,90,88,67,86,66,78,77,188,190,191,161}, new string[]{"Shift","Z","X","C","V","B","N","M",",",".","/","Shift"});
            keys.Add(new KeyCap(162,"Ctrl",48,304,28)); keys.Add(new KeyCap(164,"Alt",79,304,24)); keys.Add(new KeyCap(32,"",106,304,102));
            keys.Add(new KeyCap(165,"Alt",211,304,24)); keys.Add(new KeyCap(163,"Ctrl",238,304,27));
        }
        void AddRow(float y, int[] vks, string[] names) {
            float x=48, step=217f / vks.Length;
            for(int i=0;i<vks.Length;i++) keys.Add(new KeyCap(vks[i], names[i], x+i*step, y, step-2));
        }
        static GraphicsPath Round(RectangleF r, float rad) {
            var p=new GraphicsPath(); float d=rad*2;
            p.AddArc(r.X,r.Y,d,d,180,90); p.AddArc(r.Right-d,r.Y,d,d,270,90); p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); p.AddArc(r.X,r.Bottom-d,d,d,90,90); p.CloseFigure(); return p;
        }
        static void Box(Graphics g, RectangleF r, Color fill, Color line, float rad) {
            using(var p=Round(r,rad)) using(var b=new SolidBrush(fill)) using(var pen=new Pen(line,1)) { g.FillPath(b,p); g.DrawPath(pen,p); }
        }
        static float SmoothStep(float value) {
            float t = Math.Max(0, Math.Min(1, value));
            return t * t * (3 - 2 * t);
        }
        public Bitmap Render(int width,int height,double time,bool sleep,bool paused,bool effects,float mx,float my,bool typing,int activeKey,bool leftButton,bool rightButton,bool wheel,Func<int,bool> held,bool preview,MotionFrame motion) {
            var output=new Bitmap(width,height,PixelFormat.Format32bppPArgb);
            using(var g=Graphics.FromImage(output)) {
                g.SmoothingMode=SmoothingMode.AntiAlias; g.InterpolationMode=InterpolationMode.HighQualityBicubic; g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                g.ScaleTransform(width/400f,height/328f);
                float rest=motion.Rest;
                PointF target = Plane(248,38); target.Y+=typing?1:-9;
                if(typing) foreach(var key in keys) if(key.Vk==activeKey) { target=KeyboardPoint(key.Rect.X+key.Rect.Width/2,key.Rect.Y+6); target.Y+=2; break; }
                PointF mouse = MousePadPosition(mx, my);
                PointF mouseCenter = Plane(mouse.X, mouse.Y);
                double frameTime = Math.Max(0, Math.Min(.1, time - lastFrame));
                float blend = lastFrame < 0 ? 1 : (float)(1 - Math.Exp(-frameTime * 25));
                float clickDepth = leftButton || rightButton ? 1.6f : 0;
                float press = preview ? clickDepth : mousePress + (clickDepth - mousePress) * blend;
                PointF keyContact = preview ? target : MaidPose.Lerp(leftTip, target, blend);
                if (!preview) { leftTip = keyContact; mousePress = press; lastFrame = time; }
                var pose = new MaidPose(mouseCenter, keyContact, press, motion);
                var bodiceRect = new RectangleF(FigureX + pose.BodiceShift, FigureY + NeckLine + pose.ShoulderDrop,
                    coat.Width, coat.Height * pose.BodiceHeight);
                g.DrawImage(coat, bodiceRect);
                Surface(g,0,0,358,95,Color.FromArgb(243,46,53,63),Color.FromArgb(170,130,146,161),0);
                Surface(g,PadU,PadV,PadWidth,PadDepth,Color.FromArgb(255,68,79,93),Color.FromArgb(160,116,136,155),1);
                DrawKeyboard(g,typing,activeKey,paused,held);
                if(effects && !sleep) {
                    using(var pen=new Pen(Color.FromArgb(40,125,211,225),1)) g.DrawLine(pen,Plane(MouseCenterU,MouseCenterV),mouseCenter);
                }
                DrawMouse(g,mouse.X,mouse.Y,press,leftButton,rightButton,wheel);
                if (pose.Lean > 0) {
                    DrawRestShadow(g, pose.MouseArm, pose.Lean);
                    DrawRestShadow(g, pose.KeyArm, pose.Lean);
                }
                mouseSleeve.DrawSleeve(g, pose.MouseArm);
                keySleeve.DrawSleeve(g, pose.KeyArm);
                g.DrawImage(bodiceForeground, bodiceRect);
                var save=g.Save();
                g.TranslateTransform(pose.Cheek.X, pose.Cheek.Y);
                g.RotateTransform(pose.Roll);
                g.ScaleTransform(1 + pose.Lean * .035f, 1 - pose.Lean * .1f);
                g.TranslateTransform(-MaidPose.UprightCheek.X, -MaidPose.UprightCheek.Y);
                var headRect = new Rectangle(FigureX, FigureY, FigureWidth, HeadHeight);
                if(motion.FaceBlend<1) DrawHead(g,heads[motion.PreviousFace],headRect,1);
                DrawHead(g,heads[motion.Face],headRect,motion.FaceBlend);
                if(effects&&sleep&&!paused&&rest>.5f) {
                    float radius=4+(motion.Breathing+1)*2.1f;
                    var bubble=new RectangleF(217-radius,153-radius,radius*2,radius*2);
                    using(var fill=new SolidBrush(Color.FromArgb((int)(rest*90),172,222,241))) using(var pen=new Pen(Color.FromArgb((int)(rest*130),218,243,250),.9f)) { g.FillEllipse(fill,bubble); g.DrawEllipse(pen,bubble); }
                    using(var shine=new SolidBrush(Color.FromArgb((int)(rest*170),255,255,255))) g.FillEllipse(shine,bubble.X+radius*.35f,bubble.Y+radius*.3f,radius*.5f,radius*.35f);
                }
                g.Restore(save);
                // The desk-side cuffs sit in front of the lowered head's atlas fringe.
                mouseSleeve.DrawHand(g, pose.MouseArm);
                keySleeve.DrawHand(g, pose.KeyArm);
                // Keep click feedback visible above the fingertips.
                if(leftButton||rightButton) {
                    using(var pen=new Pen(leftButton?Color.FromArgb(102,231,242):Color.FromArgb(255,197,142),1.5f))
                        g.DrawArc(pen,mouseCenter.X-18,mouseCenter.Y-11+press,36,24,0,180);
                }
                if(sleep&&effects&&!paused) for(int i=0;i<3;i++) {
                    float phase=(float)((time/3.2+i/3.0)%1),alpha=(float)Math.Sin(phase*Math.PI)*pose.Contact;
                    using(var font=new Font("Segoe UI",10+phase*6,FontStyle.Bold)) using(var brush=new SolidBrush(Color.FromArgb((int)(alpha*200),180,212,238))) g.DrawString("z",font,brush,290+phase*20,164+rest*15-phase*39);
                }
                if(paused) using(var font=new Font("Microsoft YaHei UI",9)) using(var brush=new SolidBrush(Color.FromArgb(230,217,230,239))) g.DrawString(Ui.Text("已暂停"),font,brush,294,300);
            }
            return output;
        }
        static void DrawRestShadow(Graphics g, MaidArmPose arm, float rest) {
            PointF center = MaidPose.Lerp(arm.Elbow, arm.Wrist, .64f);
            var state = g.Save();
            g.TranslateTransform(center.X, center.Y + 5);
            g.RotateTransform((float)(Math.Atan2(arm.Wrist.Y - arm.Elbow.Y, arm.Wrist.X - arm.Elbow.X) * 180 / Math.PI));
            using (var outline = new GraphicsPath()) {
                outline.AddEllipse(-26, -7, 52, 15);
                using (var shadow = new PathGradientBrush(outline)) {
                    shadow.CenterColor = Color.FromArgb((int)(100 * rest), 20, 17, 23);
                    shadow.SurroundColors = new[] { Color.FromArgb(0, 20, 17, 23) };
                    g.FillPath(shadow, outline);
                }
            }
            g.Restore(state);
        }
        // One affine desktop plane is shared by the pad, mouse, keyboard and
        // hand contact points. Device textures face the character (180 degrees).
        static PointF Plane(float u,float v) { return new PointF(38+.9f*u-.28f*v,214+MaidPose.TableHeightOffset+.15f*u+.55f*v); }
        static PointF KeyboardPoint(float x,float y) {
            return Plane(KeyboardU + KeyboardWidth * (1 - (x-38)/240), KeyboardV + KeyboardDepth * (1 - (y-237)/90));
        }
        internal static PointF MousePadPosition(float x, float y) {
            float u = Math.Max(PadU + MouseHalfWidth, Math.Min(PadU + PadWidth - MouseHalfWidth, MouseCenterU + 19.5f * x));
            float v = Math.Max(PadV + MouseHalfDepth, Math.Min(PadV + PadDepth - MouseHalfDepth, MouseCenterV + 16 * y));
            return new PointF(u, v);
        }
        static void Surface(Graphics g,float u,float v,float w,float h,Color color,Color edge,float thickness) {
            var top=new PointF[]{Plane(u,v),Plane(u+w,v),Plane(u+w,v+h),Plane(u,v+h)};
            if(thickness>0) { var basePoints=(PointF[])top.Clone(); for(int i=0;i<4;i++) basePoints[i].Y+=thickness; using(var b=new SolidBrush(Color.FromArgb(255,35,43,53))) g.FillPolygon(b,basePoints); }
            using(var b=new SolidBrush(color)) using(var p=new Pen(edge,1)) { g.FillPolygon(b,top); g.DrawPolygon(p,top); }
        }
        void DrawKeyboard(Graphics g,bool typing,int activeKey,bool paused,Func<int,bool> held) {
            Surface(g,KeyboardU,KeyboardV,KeyboardWidth,KeyboardDepth,Color.FromArgb(255,104,121,134),Color.FromArgb(190,155,174,185),3);
            using(var keyboard=new Bitmap(240,90,PixelFormat.Format32bppPArgb)) {
                using(var kg=Graphics.FromImage(keyboard)) {
                    kg.SmoothingMode=SmoothingMode.AntiAlias; kg.TranslateTransform(-38,-237);
                    Box(kg,new RectangleF(38,237,240,90),Color.FromArgb(255,207,218,225),Color.FromArgb(255,146,163,177),5);
                    using(var font=new Font("Segoe UI",6.2f,FontStyle.Bold)) using(var text=new SolidBrush(Color.FromArgb(83,103,116))) using(var downText=new SolidBrush(Color.White)) {
                        foreach(var key in keys) {
                            bool down=!paused&&(held(key.Vk)||(typing&&key.Vk==activeKey)); var r=key.Rect;
                            if(down) r.Y+=1;
                            Box(kg,r,down?Color.FromArgb(46,170,189):Color.FromArgb(248,251,253),Color.FromArgb(163,181,194),2);
                            using(var fmt=new StringFormat {Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center}) kg.DrawString(key.Label,font,down?downText:text,r,fmt);
                        }
                    }
                }
                g.DrawImage(keyboard,new PointF[]{KeyboardPoint(38,237),KeyboardPoint(278,237),KeyboardPoint(38,327)});
            }
        }
        static void DrawMouse(Graphics g,float u,float v,float press,bool left,bool right,bool wheel) {
            // Nose/buttons point away from the pet; its wrist rests on the back.
            var nose=Plane(u,v+MouseHalfDepth-1); nose.Y += press;
            using(var p=new Pen(Color.FromArgb(166,182,194),1.2f))
                g.DrawBezier(p,nose,Plane(u+5,v+MouseHalfDepth+10),Plane(u+23,v+15),Plane(143,89));
            using(var image=new Bitmap(44,68,PixelFormat.Format32bppPArgb)) {
                using(var mg=Graphics.FromImage(image)) {
                    mg.SmoothingMode=SmoothingMode.AntiAlias;
                    Box(mg,new RectangleF(3,2,38,62),Color.FromArgb(244,249,251),Color.FromArgb(130,153,171),17);
                    Box(mg,new RectangleF(6,5,14,25),left?Color.FromArgb(61,188,205):Color.FromArgb(221,233,239),Color.FromArgb(151,177,194),5);
                    Box(mg,new RectangleF(24,5,14,25),right?Color.FromArgb(244,174,115):Color.FromArgb(221,233,239),Color.FromArgb(151,177,194),5);
                    using(var b=new SolidBrush(wheel?Color.FromArgb(247,184,93):Color.FromArgb(109,140,159))) mg.FillRectangle(b,20,9,4,14);
                }
                var points=new PointF[]{Plane(u+MouseHalfWidth,v+MouseHalfDepth),Plane(u-MouseHalfWidth,v+MouseHalfDepth),Plane(u+MouseHalfWidth,v-MouseHalfDepth)};
                for(int i=0;i<3;i++) points[i].Y+=press;
                g.DrawImage(image,points);
            }
        }
        static void DrawHead(Graphics g,Bitmap head,Rectangle rect,float alpha) {
            using(var attributes=new ImageAttributes()) {
                var matrix=new ColorMatrix(); matrix.Matrix33=alpha; attributes.SetColorMatrix(matrix);
                g.DrawImage(head,rect,0,0,head.Width,head.Height,GraphicsUnit.Pixel,attributes);
            }
        }
        public void Dispose() {
            coat.Dispose();
            bodiceForeground.Dispose();
            foreach (var head in heads) head.Dispose();
            mouseSleeve.Dispose();
            keySleeve.Dispose();
        }
        static int ScalePixel(int pixel, float opacity) {
            int result = 0;
            for (int shift = 0; shift < 32; shift += 8)
                result |= (int)(((pixel >> shift) & 255) * opacity) << shift;
            return result;
        }
    }
}
