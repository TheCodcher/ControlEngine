using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using ControlEngine.Extended;
using ControlEngine.Graphic;
using ControlEngine.Collisions;
using ControlEngine;
using ControlEngine.Interactive;

namespace ControlEngine
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            LoadResurce.InitializeJSONResurce("Resurce");
            //LoadResurce.InitializeImageResurce("Sprites");
            Form1 screen = new Form1();
            GraphicsEngine g = new GraphicsEngine(screen);
            var first = new SomeShitForTest(screen);
            var second = new SomeShitForTest2();
            first.UpdateFrame += g.AddFrameAsync;
            second.UpdateFrame += g.AddFrameAsync;
            g.Update += first.TickUpdate;
            g.Update += second.TickUpdate;
            CollisionEngine c = new CollisionEngine(screen.Size);
            g.Update += c.TickUpdate;
            c.AddMaterialObject(first);
            c.AddMaterialObject(second);
            g.TickUpdateStart(60);
            Application.Run(screen);
        }
    }
    class SomeShitForTest : MaterialObject
    {
        Bitmap myImage;
        Bitmap myImage2;
        sbyte chanjeflag = 1;
        int dir = 0;
        ObjectResurce<Vector[]> date = new ObjectResurce<Vector[]>("SSFT2");
        ObjectResurce<Vector[]> date2 = new ObjectResurce<Vector[]>("SSFT2_1");
        public SomeShitForTest(ICanBeInteractive z)
        {
            myImage = new Bitmap(140, 140);
            var g = Graphics.FromImage(myImage);
            g.Clear(Color.Black);
            g.FillRectangle(new SolidBrush(Color.Aqua), new Rectangle(0, 0, 10, 10));
            g.FillRectangle(new SolidBrush(Color.Blue), new Rectangle(130, 130, 10, 10));
            g.Dispose();

            myImage2 = new Bitmap(50, 50);
            var g2 = Graphics.FromImage(myImage2);
            g2.Clear(Color.Black);
            g2.FillRectangle(new SolidBrush(Color.Aqua), new Rectangle(0, 0, 10, 10));
            g2.FillRectangle(new SolidBrush(Color.Blue), new Rectangle(130, 130, 10, 10));
            g2.Dispose();

            SelecteImage(myImage);
            Location = new Point(50, 50);

            //myImage = (Bitmap)Rotate(myImage, 0);
            CollisionBufferSize = 361;
            SmoothAccuracyRate = 5;

            InitializeCollisionBuffer(new MaterialObjectResurce(myImage, date.GetLoadedObject()), new MaterialObjectResurce(myImage2, date2.GetLoadedObject()));
            //date2.SaveResurce("Resurce", CreateCollision(myImage2));
            //var c = date.GetLoadedObject();
            new MyInteractive(z, this);
        }
        public override void TickUpdate()
        {
            //Direction = Vector.Up.Rotate(dir*5);
            //SelecteImage(myImage);//Rotate(0);
            dir++;
            if (dir == 72)
            {
                dir = 0;
                if (chanjeflag > 0)
                    SelecteImage(myImage);
                else
                    SelecteImage(myImage2);
                chanjeflag *= -1;
            }
            Direction = Vector.Up.Rotate(dir * 5);
            var tempc = Rotate(dir * 5);
            var MyCollisionL = GetCollisionModel().Select((p) => (Point)(p - Position + (EPoint)tempc.Size / 2)).ToArray();
            var MyCollision = GetCollisionModel().Select((p) => new Rectangle(p - Position + (EPoint)tempc.Size / 2, new Size(3, 3))).ToArray();
            var MySize = GetGeneralSize();
            var g = Graphics.FromImage(tempc);
            g.FillRectangles(new SolidBrush(Color.Red), MyCollision);
            g.DrawLines(new Pen(Color.Red), MyCollisionL);
            g.DrawRectangle(new Pen(Color.Green), new Rectangle((EPoint)0, MySize.Size));
            NeedUpdateFrame(tempc);
            g.Dispose();
        }
        public override void CollisionWith(MaterialObject sender)
        {
            try
            {
                CollisionWith2((dynamic)sender);
            }
            catch
            {
                
            }
        }
        private void CollisionWith2(SomeShitForTest2 SecondObj)
        {
            
        }

        public override void OutOfSize()
        {
            throw new NotImplementedException();
        }
        class MyInteractive : InteractiveObject
        {
            public Action<MouseButtons> Activity;
            public readonly MaterialObject ezozoz;
            public MyInteractive(ICanBeInteractive ICBI, MaterialObject ez) : base(ICBI) 
            {
                ezozoz = ez;
                Subscriptions.Add(MouseButtons.Right);
            }
            protected override void Active(MouseButtons PressedKey)
            {
                throw new Exception();
            }
            protected override Rectangle GetBoard()
            {
                return new Rectangle(ezozoz.Location, ezozoz.GetImage().NowSize);
            }
            protected override EPoint[] GetBoarderPoints()
            {
                return ezozoz.GetCollisionModel();
            }
        }
    }
    class SomeShitForTest2 : MaterialObject
    {
        Bitmap myImage;
        EPoint vel = EPoint.Left;
        double scaleflag = 1.5;
        double scaleV = 180;
        int dir = 1;
        ObjectResurce<Vector[]> date = new ObjectResurce<Vector[]>("SSFT4");
        public SomeShitForTest2()
        {
            myImage = new Bitmap(140, 140);
            SelecteImage(myImage);
            Location = new Point(500, 150);
            var g = Graphics.FromImage(myImage);
            g.Clear(Color.Black);
            g.FillRectangle(new SolidBrush(Color.Aqua), new Rectangle(0, 0, 10, 10));
            g.FillRectangle(new SolidBrush(Color.Blue), new Rectangle(130, 130, 10, 10));
            g.Dispose();
            //myImage = (Bitmap)Rotate(myImage, 0);
            CollisionBufferSize = 361;
            SmoothAccuracyRate = 5;
            InitializeCollisionBuffer(new[] { new MaterialObjectResurce(myImage, date.GetLoadedObject()) });
            //date.SaveResurce("Resurce", CreateCollision(myImage));
        }
        public override void TickUpdate()
        {
            Direction = Vector.Up.Rotate(dir);
            Position = new PointF(Position.X + vel.X, Position.Y + vel.Y);
            Rotate(dir);
            ScaleImage(scaleV / 360);
            var tempc = GetImage().NowImage;
            var MyCollisionL = GetCollisionModel().Select((p) => (Point)(p - Position + (EPoint)tempc.Size / 2)).ToArray();
            var MyCollision = GetCollisionModel().Select((p) => new Rectangle(p - Position + (EPoint)tempc.Size / 2, new Size(3, 3))).ToArray();
            var MySize = GetGeneralSize();
            var g = Graphics.FromImage(tempc);
            g.FillRectangles(new SolidBrush(Color.Red), MyCollision);
            g.DrawLines(new Pen(Color.Red), MyCollisionL);
            g.DrawRectangle(new Pen(Color.Green), new Rectangle((EPoint)0, MySize.Size));

            scaleV += scaleflag;
            dir++;
            if (dir == 360)
            {
                dir = 1;
                scaleflag *= -1;
            }
            NeedUpdateFrame(tempc);
            g.Dispose();
        }
        public override void CollisionWith(MaterialObject sender)
        {
            try
            {
                CollisionWith2((dynamic)sender);
            }
            catch
            {
                
            }
        }
        private void CollisionWith2(SomeShitForTest SecondObj)
        {
            vel = -vel;
        }

        public override void OutOfSize()
        {
            vel = -vel;
        }
    }
}
