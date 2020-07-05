using System;
using System.Collections.Generic;
using System.Drawing;
using ControlEngine.Extended;

namespace ControlEngine
{
    namespace Graphic
    {
        class PlacedImage
        {
            public Image Image { get; set; }
            public Point Location { get; set; }
            public object Sender { get; set; }
            public PlacedImage(Image Image, Point Location)
            {
                this.Image = Image;
                this.Location = Location;
                Sender = null;
            }
            public PlacedImage(object Sender, Image Image, Point Location)
            {
                this.Image = Image;
                this.Location = Location;
                this.Sender = Sender;
            }
        }
        abstract class  GraphicObject : BeTimed
        {
            public delegate void UpdateFrameHendler(int Layer, PlacedImage Sprite);
            public event UpdateFrameHendler UpdateFrame;
            /// <summary>
            /// Базовый слой для отображения (по умолчанию 1) - 0 пустой слой  заднего фона
            /// </summary>
            protected int BaseLayer = 1;
            private PointF position = new PointF(0, 0);
            private Point location = new Point(0, 0);
            /// <summary>
            /// Позиция центра изображения в относительных координатах формы (по умолчанию 0)
            /// </summary>
            public virtual PointF Position
            {
                get => position;
                set
                {
                    position = value;
                    Size imgSize = GetImage().NowSize;
                    location = new Point((int)Math.Round(position.X - imgSize.Width / 2), (int)Math.Round(position.Y - imgSize.Height) / 2);
                }
            }
            /// <summary>
            /// Позиция верхнего левого угла изображения в относительных координатах формы (по умолчанию 0)
            /// </summary>
            public virtual Point Location
            {
                get => location;
                set
                {
                    location = value;
                    Size imgSize = GetImage().NowSize;
                    position = new PointF(location.X + imgSize.Width / 2, location.Y + imgSize.Height / 2);
                }
            }
            protected GraphicObject()
            {
                //тут что-то определенно было, когда я пришел... (нет)
            }
            protected void NeedUpdateFrame()
            {
                UpdateFrame(BaseLayer, new PlacedImage(this, NowStateImage.NowImage, Location));
            }
            protected void NeedUpdateFrame(Image Sprite)
            {
                UpdateFrame(BaseLayer, new PlacedImage(this, Sprite, Location));
            }
            protected void NeedUpdateFrame(Image Sprite, Point Location)
            {
                UpdateFrame(BaseLayer, new PlacedImage(this, Sprite, Location));
            }
            protected void NeedUpdateFrame(int BaseLayer, Image Sprite, Point Location)
            {
                UpdateFrame(BaseLayer, new PlacedImage(this, Sprite, Location));
            }

            /// <summary>
            /// Возвращает повернутое изображение
            /// </summary>
            /// <param name="Image">Исходное изображение</param>
            /// <param name="Angle">Угол поворота в градусах</param>
            /// <returns></returns>
            public static Image Rotate(Image Image, double Angle)
            {
                double rAngle = MathExtended.ToRadians(Angle);
                if (rAngle == 0) return Image;
                double sin = Math.Abs(Math.Sin(rAngle));
                double cos = Math.Abs(Math.Cos(rAngle));
                double tempW = Image.Height * sin + Image.Width * cos;
                double tempH = Image.Height * cos + Image.Width * sin;
                Bitmap tempCanvas = new Bitmap((int)Math.Ceiling(tempW), (int)Math.Ceiling(tempH));
                Graphics tempG = Graphics.FromImage(tempCanvas);

                tempG.TranslateTransform(tempCanvas.Width / 2, tempCanvas.Height / 2);
                tempG.RotateTransform((float)Angle);
                tempG.TranslateTransform(-Image.Width / 2, -Image.Height / 2);

                tempG.DrawImage(Image, 0, 0);
                tempG.Dispose();
                return tempCanvas;
            }
            public Image Rotate(double Angle, Image Select = null)
            {
                Select = Select == null ? NowStateImage.KeyImage : Select;
                var img = Rotate(Select, Angle);
                NowStateImage = new StateImage(Select, img, Angle);
                return img;
            }
            //записывать старый frame, чтобы все работало корректно.
            private StateImage _stateImage = new StateImage();
            public StateImage NowStateImage
            {
                get => _stateImage;
                private set
                {
                    _stateImage = value;
                    location = new Point((int)Math.Round(position.X - value.NowSize.Width / 2), (int)Math.Round(position.Y - value.NowSize.Height / 2));
                }
            }
            public static Image ScaleImage(Image KeyImage, double ScaleValue)
            {
                if (ScaleValue == 0) throw new Exception("Try scaled on zero");
                var tempCanv = new Bitmap((int)Math.Ceiling(KeyImage.Width * ScaleValue), (int)Math.Ceiling(KeyImage.Height * ScaleValue));
                using (var gTemp = Graphics.FromImage(tempCanv))
                {
                    gTemp.ScaleTransform((float)ScaleValue, (float)ScaleValue);
                    gTemp.DrawImage(KeyImage, 0, 0);  
                }
                return tempCanv;
            }
            public Image ScaleImage(double ScaleValue, bool UseKeyImage = false)
            {
                Image Select = UseKeyImage ? GetImage().KeyImage : GetImage().NowImage;
                Select = ScaleImage(Select, ScaleValue);
                NowStateImage = new StateImage(NowStateImage.KeyImage, Select, NowStateImage.NowAngle, ScaleValue);
                return Select;
            }
            public StateImage GetImage()
            {
                if (NowStateImage.KeyImage == null) throw new NullReferenceException();
                return NowStateImage;
            }
            protected void SelecteImage(Image Select)
            {
                NowStateImage = new StateImage(Select, Select);
            }

            public class StateImage
            {
                public StateImage(Image KeyImage = null, Image NowImage = null, double NowAngle = 0, double Scale = 1)
                {
                    this.KeyImage = KeyImage;
                    this.NowImage = NowImage;
                    this.NowAngle = NowAngle;
                    this.Scale = Scale;
                }
                public Image KeyImage { get; private set; }
                public Image NowImage { get; private set; }
                public double NowAngle { get; private set; }
                public double Scale { get; private set; }
                public Size NowSize { get => NowImage == null ? Size.Empty : NowImage.Size; }
            }
        }
    }

    namespace Extended
    {
        public class EPoint
        {
            public static readonly EPoint Up = new EPoint(0, -1);
            public static readonly EPoint Left = new EPoint(-1, 0);
            public static readonly EPoint Right = new EPoint(1, 0);
            public static readonly EPoint Down = new EPoint(0, 1);
            public int X { get; set; }
            public int Y { get; set; }
            public EPoint()
            {
                X = 0;
                Y = 0;
            }
            public EPoint(int X, int Y)
            {
                this.X = X;
                this.Y = Y;
            }

            public static EPoint operator +(EPoint A, EPoint B) => new EPoint(A.X + B.X, A.Y + B.Y);
            public static EPoint operator +(EPoint A, int b) => new EPoint(A.X + b, A.Y + b);
            public static EPoint operator *(EPoint A, EPoint B) => new EPoint(A.X * B.X, A.Y * B.Y);
            public static EPoint operator *(EPoint A, int b) => new EPoint(A.X * b, A.Y * b);
            public static EPoint operator /(EPoint A, EPoint B) => new EPoint(A.X / B.X, A.Y / B.Y);
            public static EPoint operator /(EPoint A, int b) => new EPoint(A.X / b, A.Y / b);
            public static EPoint operator *(EPoint A, double b) => new EPoint((int)Math.Round(A.X * b), (int)Math.Round(A.Y * b));
            public static EPoint operator -(EPoint A, EPoint B) => new EPoint(A.X - B.X, A.Y - B.Y);
            public static EPoint operator -(EPoint A, int b) => new EPoint(A.X - b, A.Y - b);
            public static EPoint operator -(EPoint A) => new EPoint(-A.X, -A.Y);
            public static EPoint operator ++(EPoint A) => new EPoint(A.X + 1, A.Y + 1);
            public static EPoint operator --(EPoint A) => new EPoint(A.X - 1, A.Y - 1);
            public static bool operator ==(EPoint A, EPoint B) => (A.X == B.X) && (A.Y == B.Y) ? true : false;
            public static bool operator ==(EPoint A, int b) => (A.X == b) && (A.Y == b) ? true : false;
            public static bool operator !=(EPoint A, EPoint B) => (A.X != B.X) || (A.Y != B.Y) ? true : false;
            public static bool operator !=(EPoint A, int b) => (A.X != b) || (A.Y != b) ? true : false;
            //.///.////

            //public static bool operator <=(EPoint A, int b)
            //{
            //    return (A.X <= b) && (A.Y <= b) ? true : false;
            //}
            //public static bool operator >=(EPoint A, int b)
            //{
            //    return (A.X >= b) || (A.Y >= b) ? true : false;
            //}
            //public static bool operator <=(EPoint A, EPoint B)
            //{
            //    return (A.X <= B.X) && (A.Y <= B.Y) ? true : false;
            //}
            //public static bool operator >=(EPoint A, EPoint B)
            //{
            //    return (A.X >= B.X) || (A.Y >= B.Y) ? true : false;
            //}

            ////.///.////

            //public static bool operator <(EPoint A, int b)
            //{
            //    return (A.X < b) && (A.Y < b) ? true : false;
            //}
            //public static bool operator >(EPoint A, int b)
            //{
            //    return (A.X > b) || (A.Y > b) ? true : false;
            //}
            //public static bool operator <(EPoint A, EPoint B)
            //{
            //    return (A.X < B.X) && (A.Y < B.Y) ? true : false;
            //}
            //public static bool operator >(EPoint A, EPoint B)
            //{
            //    return (A.X > B.X) || (A.Y < B.Y) ? true : false;
            //}

            //.///.////
            public override bool Equals(object obj)
            {
                if (!(obj is EPoint))
                {
                    return false;
                }
                var A = (EPoint)obj;
                return (X == A.X) && (Y == A.Y);
            }

            public override int GetHashCode()
            {
                return Tuple.Create(X, Y).GetHashCode();
            }
            public double Length()
            {
                return Math.Sqrt(X * X + Y * Y);
            }
            public double SquareLength()
            {
                return X * X + Y * Y;
            }
            /// <summary>
            /// Возвращает true, если точка лежит в области, ограниченной прямоугольником, заданным диагональю с концами в точках A и B
            /// </summary>
            /// <param name="A">Верхняя левая точка прямоугольника</param>
            /// <param name="B">Нижняя правая точка прямоугольника</param>
            /// <returns></returns>
            public bool Include(EPoint A, EPoint B)
            {
                return (X >= A.X) && (Y >= A.Y) && (X <= B.X) && (Y <= B.Y);
            }
            public bool Include(Rectangle rect)
            {
                EPoint A = new EPoint(rect.X, rect.Y);
                EPoint B = A + new EPoint(rect.Width, rect.Height);
                return Include(A, B);
            }
            public bool IncludeStrictly(EPoint A, EPoint B)
            {
                EPoint k = new EPoint(X, Y);
                return (k.X > A.X) && (k.Y > A.Y) && (k.X < B.X) && (k.Y < B.Y);
            }
            public bool IncludeStrictly(Rectangle rect)
            {
                EPoint k = new EPoint(X, Y);
                EPoint A = new EPoint(rect.X, rect.Y);
                EPoint B = A + new EPoint(rect.Width, rect.Height);
                return (k.X > A.X) && (k.Y > A.Y) && (k.X < B.X) && (k.Y < B.Y);
            }
            

            public static implicit operator System.Drawing.Point(EPoint A) => new System.Drawing.Point(A.X, A.Y);
            public static implicit operator EPoint(System.Drawing.Point A) => new EPoint(A.X, A.Y);
            public static implicit operator EPoint(Size A) => new EPoint(A.Width, A.Height);
            public static implicit operator Size(EPoint A) => new Size(A.X, A.Y);
            public static implicit operator EPoint(int a) => new EPoint(a, a);
            public static implicit operator PointF(EPoint A) => new PointF(A.X, A.Y);
            public static implicit operator EPoint(PointF A) => new EPoint((int)Math.Round(A.X), (int)Math.Round(A.Y));
            public static implicit operator Vector(EPoint A) => new PointF(A.X, A.Y);
            public static implicit operator EPoint(Vector A) => new EPoint((int)Math.Round(A.X), (int)Math.Round(A.Y));
        }

        class Animation
        {
            private List<Image> AnimationList = new List<Image>();
            private List<Image> OpenAnimationList = new List<Image>();
            private List<Image> EndAnimationList = new List<Image>();
            private Queue<Image> AnimationQueue = new Queue<Image>();
            private Image NowFrame;
            public bool UseOpenAnimation { get; private set; }
            public bool OpenAnimationStarted { get; private set; } = false;
            public bool OpenAnimationAreOver { get; private set; } = false;
            public bool EndAnimationStarted { get; private set; } = false;
            public bool EndAnimationAreOver { get; private set; } = false;

            public int FPSUpdateRate { get; set; } = 1; //в каждый какой кадр будет возвращен новый спрайт
            private int FPSCounter = 1;
            #region .ctor
            public Animation(List<Image> AnimationList)
            {
                this.AnimationList = AnimationList;
                StartOver(false);
            }
            public Animation(int FPSUpdateRate, List<Image> AnimationList)
            {
                this.AnimationList = AnimationList;
                this.FPSUpdateRate = FPSUpdateRate;
                StartOver(false);
            }
            public Animation(int FPSUpdateRate, List<Image> AnimationList, bool UseOpenAnimation, List<Image> OpenAnimationList)
            {
                this.OpenAnimationList = OpenAnimationList;
                this.AnimationList = AnimationList;
                this.FPSUpdateRate = FPSUpdateRate;
                StartOver(UseOpenAnimation);
            }
            public Animation(int FPSUpdateRate, List<Image> AnimationList, bool UseOpenAnimation, List<Image> OpenAnimationList, List<Image> EndAnimationList)
            {
                this.OpenAnimationList = OpenAnimationList;
                this.EndAnimationList = EndAnimationList;
                this.AnimationList = AnimationList;
                this.FPSUpdateRate = FPSUpdateRate;
                StartOver(UseOpenAnimation);
            }
            public Animation(int FPSUpdateRate, List<Image> AnimationList, List<Image> EndAnimationList)
            {
                this.AnimationList = AnimationList;
                this.EndAnimationList = EndAnimationList;
                this.FPSUpdateRate = FPSUpdateRate;
                StartOver(false);
            }
            #endregion
            public Image PlayAnimationFrame()
            {
                if (!EndAnimationAreOver)
                {
                    if (AnimationQueue.Count == 0)
                    {
                        if (OpenAnimationStarted && !OpenAnimationAreOver)
                        {
                            OpenAnimationAreOver = true;
                        }
                        if (EndAnimationStarted)
                        {
                            EndAnimationAreOver = true;
                            return NowFrame;
                        }
                        AnimationQueue = ConvertToQueue(AnimationList);
                    }
                    if (FPSCounter % FPSUpdateRate == 0)
                    {
                        NowFrame = AnimationQueue.Dequeue();
                        FPSCounter = 1;
                    }
                    else
                    {
                        FPSCounter++;
                    }
                }
                return NowFrame;
            }
            public Image GetNowAnimationFrame()
            {
                return NowFrame;
            }
            public Image AnimationQueueFrame(int Index)
            {
                if (Index > AnimationQueue.Count) Index = AnimationQueue.Count - 1;
                if (Index < 0) Index = 0;
                return AnimationQueue.ToArray()[Index];
            }
            /// <summary>
            /// Если в листе анимаций нет ни одного объекта возвращает null
            /// </summary>
            public Image GetFirstSprite()
            {
                return AnimationList.Count > 0 ? AnimationList[0] : null;
            }
            public void StartOver(bool UseOpenAnimation)
            {
                if (AnimationList.Count == 0) throw new Exception("Animation list clear");
                this.UseOpenAnimation = UseOpenAnimation;
                OpenAnimationStarted = false;
                if (UseOpenAnimation)
                {
                    AnimationQueue = ConvertToQueue(OpenAnimationList);
                    OpenAnimationStarted = true;
                }
                else AnimationQueue = ConvertToQueue(AnimationList);
                OpenAnimationAreOver = false;
                EndAnimationStarted = false;
                EndAnimationAreOver = false;
                FPSCounter = FPSUpdateRate;
                NowFrame = AnimationQueue.Peek();
            }
            private Queue<Image> ConvertToQueue(List<Image> list)
            {
                Queue<Image> temp = new Queue<Image>(list);
                //foreach (var t in list)
                //    temp.Enqueue(t);
                return temp;
            }
            public void BeginCompletion()
            {
                if (!EndAnimationStarted)
                {
                    EndAnimationStarted = true;
                    AnimationQueue = ConvertToQueue(EndAnimationList);
                    FPSCounter = FPSUpdateRate;
                    NowFrame = AnimationQueue.Peek();
                }
            }
        }
        static class MathExtended
        {
            public static double ToRadians(double Degrees)
            {
                if (Math.Abs(Degrees) >= 360) Degrees = Degrees - (int)Degrees / 360 * 360;
                if (Degrees == 0) return 0;
                if (Degrees < 0) Degrees = 360 - Degrees;
                return Degrees / 180 * Math.PI;
            }
            public static double ToDegrees(double Radians)
            {
                throw new NotImplementedException();
            }
        }
    }
}
