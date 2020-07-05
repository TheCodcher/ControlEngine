using System;
using System.Collections.Generic;
using System.Text;
using ControlEngine.Extended;
using ControlEngine.Graphic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ControlEngine
{
    namespace Collisions
    {
        class CollisionEngine : BeTimed
        {
            public readonly Size ActiveFieldSize;
            //List<Collision> AllObjCollisions = new List<Collision>(); //кто такие столкновения
            public readonly Dictionary<MaterialObjectState, List<MaterialObject>> MaterialObjectsLists;
            public CollisionEngine(Size FieldSize)
            {
                ActiveFieldSize = FieldSize;
                MaterialObjectsLists = new Dictionary<MaterialObjectState, List<MaterialObject>>(((IEnumerable<MaterialObjectState>)Enum.GetValues(typeof(MaterialObjectState))).Select((s) => new KeyValuePair<MaterialObjectState, List<MaterialObject>>(s, new List<MaterialObject>())));
            }

            public override void TickUpdate()
            {
                var objs = MaterialObjectsLists[MaterialObjectState.isMaterial].ToArray();
                var mPoints = MaterialObjectsLists[MaterialObjectState.isPoint].ToArray();
                for (int i = 0; i < objs.Length; i++)
                {
                    var IobjsRect = objs[i].GetGeneralSize();
                    var loc = (EPoint)IobjsRect.Location;
                    var eloc = loc + (EPoint)IobjsRect.Size;
                    if (!loc.Include(0, ActiveFieldSize) || !eloc.Include(0, ActiveFieldSize)) 
                        objs[i].OutOfSize();
                    for (int j = i + 1; j < objs.Length; j++)
                        if (isCollision(objs[i], objs[j]))
                        {
                            objs[i].CollisionWith(objs[j]);
                            objs[j].CollisionWith(objs[i]);
                        }
                }
                for (int i = 0; i < mPoints.Length; i++)
                {
                    var center = (EPoint)mPoints[i].Position;
                    if (!center.Include(0, ActiveFieldSize)) mPoints[i].OutOfSize();
                    for (int j = 0; j < objs.Length; j++)
                    {
                        var gArea = objs[j].GetGeneralSize();
                        if (center.Include(gArea))
                        {
                            center = center - gArea.Location;
                            var Areas = new[]
                            {
                                new Rectangle(gArea.Location, center - gArea.Location),
                                new Rectangle(center.X,gArea.Location.Y, gArea.Size.Width - center.X, center.Y - gArea.Location.Y),
                                new Rectangle(gArea.Location.X, center.Y, center.X - gArea.Location.X, gArea.Size.Height - center.Y),
                                new Rectangle(center, (EPoint)gArea.Size - center)
                            };
                            var shellPoints = objs[j].GetCollisionModel();
                            var flags = new bool[4];
                            foreach (var p in shellPoints)
                            {
                                bool breaker = false;
                                for (int k = 0; k < 4; k++)
                                {
                                    if (!flags[k])
                                    {
                                        flags[k] = p.Include(Areas[k]);
                                        if (flags[k]) break;
                                    }
                                }
                                if (flags[0] && flags[1] && flags[2] && flags[3])
                                {
                                    mPoints[i].CollisionWith(objs[j]);
                                    objs[j].CollisionWith(mPoints[i]);
                                    breaker = true;
                                }
                                if (breaker) break;
                            }
                        }
                    }
                }
            }
            private bool isCollision(MaterialObject A, MaterialObject B)
            {
                //алгоритм GJK первая ступень - боксы, 2ая - коллизия //так же к стенам относится size
                //проверка каждой точки?
                //БООООКСЫЫЫЫЫЫЫ

                var Arect = A.GetGeneralSize();
                var Brect = B.GetGeneralSize();
                return Arect.IntersectsWith(Brect);

                //throw new NotImplementedException();
            }
            public void AddMaterialObject(MaterialObject obj)
            {
                if (obj.NowState == MaterialObjectState.isDispose) return;
                MaterialObjectsLists[obj.NowState].Add(obj);
                obj.UpdateState += ObjUpdateState;
            }
            public void RemoveMaterialObject(MaterialObject obj) //вызывать ли ошибку или не вызывать ли ошибку, вот в чем вопрос.
                //с одной стороны, пользователь должен быть вкурсе, что делает херню. 
                //c другой стороны, будет пользователь, творящий херню, следить за своей хенрней?
            {
                 MaterialObjectsLists[obj.NowState].Remove(obj);
                 obj.UpdateState -= ObjUpdateState;
            }
            private void ObjUpdateState(MaterialObjectState Last, MaterialObjectState Now, MaterialObject sender)
            {
                MaterialObjectsLists[Last].Remove(sender);
                if (Now == MaterialObjectState.isDispose)
                {
                    sender.UpdateState -= ObjUpdateState;
                }
                else
                {
                    MaterialObjectsLists[Now].Add(sender);
                }
            }

            //class Collision //задумывалось как нечно классное
            //{
            //    public readonly MaterialObject First;
            //    public readonly MaterialObject Second;
            //    public readonly object Simplex; //и как это вообще работать должно?
            //    public Collision(MaterialObject First, MaterialObject Second)
            //    {
            //        this.First = First;
            //        this.Second = Second;
            //    }
            //}
        }

        enum MaterialObjectState
        {
            isMaterial,
            isPoint,
            isNotMaterial,
            isDispose
        }
        class MaterialObjectResurce
        {
            public MaterialObjectResurce(Image Sprite, Vector[] DefaultCollision = null)
            {
                this.DefaultCollision = DefaultCollision;
                this.Sprite = Sprite;
            }
            public readonly Vector[] DefaultCollision;
            public readonly Image Sprite;
        }
        abstract class MaterialObject : GraphicObject
        {
            private MaterialObjectState _state = MaterialObjectState.isMaterial;
            public event Action<MaterialObjectState, MaterialObjectState, MaterialObject> UpdateState; 

            #region Public Properties
            /// <summary>
            /// Количество колизий, которое будет сохранено в буфер для каждого объекта (по умолчанию 2)
            /// </summary>
            public int CollisionBufferSize { get; protected set; } = 5;
            /// <summary>
            /// Сколько раз будет просеиваться количество точек модели коллизии, то есть будет удалаться каждая 2-ая (по умолчанию 3)
            /// </summary>
            public int SmoothAccuracyRate { get; protected set; } = 3;

            public MaterialObjectState NowState
            {
                get => _state;
                set
                {
                    if (value == _state) return;
                    UpdateState?.Invoke(_state, value, this);
                    _state = value;
                }
            }

            Vector _dir = Vector.Empty;
            public virtual Vector Direction
            {
                get => _dir;
                set => _dir = value.Normalize();
            }
            #endregion
            #region Previous Staff
            private PointF previousPosition = PointF.Empty;
            private Image previousImage = null;
            private Vector previousDirection = Vector.Empty;
            private EPoint[] LastStateColissionPoints = null;
            #endregion
            private Dictionary<Image, CollisionBuffer> CollisionDate = new Dictionary<Image, CollisionBuffer>();

            #region Constractors
            public MaterialObject() { }
            public MaterialObject(MaterialObjectState StartState, int CollisionBufferSize, int SmoothAccuracyRate)
            {
                this.CollisionBufferSize = CollisionBufferSize;
                this.SmoothAccuracyRate = SmoothAccuracyRate;
                _state = StartState;
            }
            public MaterialObject(MaterialObjectState StartState)
            {
                _state = StartState;
            }
            #endregion
            public Rectangle GetGeneralSize()
            {
                return new Rectangle(Location, GetImage().NowSize);
                //var NowImage = GetImage().KeyImage;
                //try
                //{
                //    return new Rectangle(Location, GetImage().NowSize);
                //    //return CollisionDate[NowImage].GetGeneralSize();
                //}
                //catch
                //{
                //    //CollisionDate.Add(NowImage, new CollisionBuffer(NowImage, this));
                //    //return CollisionDate[NowImage].GetGeneralSize();
                //}
            }
            public EPoint[] GetCollisionModel()
            {
                var tempNowState = GetImage();
                Image NowImage = tempNowState.KeyImage;
                if (previousPosition == Position && previousDirection == Direction && previousImage == NowImage) return LastStateColissionPoints;
                try
                {
                    LastStateColissionPoints = CollisionDate[NowImage].GetCollision().Select((p) => (EPoint)(p * tempNowState.Scale + Position)).ToArray();
                }
                catch
                {
                    CollisionDate.Add(NowImage, new CollisionBuffer(NowImage, this));
                    LastStateColissionPoints = CollisionDate[NowImage].GetCollision().Select((p) => (EPoint)(p + Position)).ToArray();
                }
                previousDirection = Direction;
                previousImage = NowImage;
                previousPosition = Position;

                return LastStateColissionPoints;
            }
            public void InitializeCollisionBuffer(params MaterialObjectResurce[] res)
            {
                foreach (var r in res)
                {
                    if (CollisionDate.ContainsKey(r.Sprite)) continue;
                    if (r.DefaultCollision == null)
                        CollisionDate.Add(r.Sprite, new CollisionBuffer(r.Sprite, this));
                    else
                        CollisionDate.Add(r.Sprite, new CollisionBuffer(r.Sprite, r.DefaultCollision, this));
                }
            }
            public MaterialObjectResurce[] SaveDate()
            {
                return CollisionDate.Select((k) => new MaterialObjectResurce(k.Key, k.Value.DefaultCollision)).ToArray();
            }
            public Vector[] CreateCollision(Bitmap SelectedImage)
            {
                List<EPoint> resultCollision = new List<EPoint>();
                Vector imgCenter = SelectedImage.Size / 2;

                var processing = new Stack<EPoint>();
                bool isExtremePixel(int i, int j)
                {
                    try
                    {
                        if (SelectedImage.GetPixel(i, j) == Color.Transparent) return false;
                    }
                    catch
                    {
                        return false;
                    }
                    try
                    {

                        if (SelectedImage.GetPixel(i - 1, j) == Color.Transparent) return true;
                        if (SelectedImage.GetPixel(i + 1, j) == Color.Transparent) return true;
                        if (SelectedImage.GetPixel(i, j - 1) == Color.Transparent) return true;
                        if (SelectedImage.GetPixel(i, j + 1) == Color.Transparent) return true;
                    }
                    catch
                    {
                        return true;
                    }
                    return false;
                }

                for (int i = 0; i < SelectedImage.Width; i++)
                {
                    bool breakPoint = false;
                    for (int j = 0; j < SelectedImage.Height; j++)
                    {

                        if (isExtremePixel(i, j))
                        {
                            processing.Push(new EPoint(i, j));
                            breakPoint = true;
                            break;
                        }
                    }
                    if (breakPoint) break;
                }
                while (processing.Count != 0)
                {
                    var pixel = processing.Pop();
                    if (resultCollision.Contains(pixel)) continue;
                    resultCollision.Add(pixel);
                    for (int i = pixel.X - 1; i <= pixel.X + 1; i++)
                        for (int j = pixel.Y - 1; j <= pixel.Y + 1; j++)
                        {
                            if (i == pixel.X && j == pixel.Y) continue;
                            if (isExtremePixel(i, j))
                                processing.Push(new EPoint(i, j));
                        }
                }
                int ActualyKey = (int)Math.Pow(2, SmoothAccuracyRate);
                var tempArray = resultCollision.ToArray();
                var resultArray = new List<Vector>();
                for (int i = 0; i < tempArray.Length; i += ActualyKey)
                    resultArray.Add((Vector)tempArray[i] - imgCenter);
                return resultArray.ToArray();
            }
            public virtual void CollisionWith(MaterialObject SecondObj) { }
            public virtual void OutOfSize() => Dispose();
            public void Dispose()
            {
                NowState = MaterialObjectState.isDispose;
            }
            class CollisionBuffer
            {
                private MaterialObject BaseObj;
                public readonly Vector[] DefaultCollision;
                //public readonly EPoint DefaultGeneralSize;
                private Queue<BufferItem<Vector[]>> CBuffer = new Queue<BufferItem<Vector[]>>();
                //private Queue<BufferItem<EPoint>> GSBuffer = new Queue<BufferItem<EPoint>>();
                public CollisionBuffer(Image KeyImage, Vector[] DefaultCollision, MaterialObject Sender)
                {
                    BaseObj = Sender;
                    this.DefaultCollision = DefaultCollision;
                    //DefaultGeneralSize = KeyImage.Size;
                }
                public CollisionBuffer(Image KeyImage, MaterialObject Sender)
                {
                    BaseObj = Sender;
                    DefaultCollision = BaseObj.CreateCollision((Bitmap)KeyImage);
                    //DefaultGeneralSize = KeyImage.Size;
                }
                //public Rectangle GetGeneralSize() //какая-то дичь, которую можно посчитать просто (loc-pos)*2
                //{
                //    var norm = BaseObj.Direction.Normalize();
                //    var Canfind = GSBuffer.FirstOrDefault((p) => p.Key == norm);
                //    if (Canfind != null) return new Rectangle(BaseObj.Location, Canfind.Value);

                //    EPoint NowSize;
                //    double Angle = Vector.CalcAngleNormalVect(Vector.Up, norm);
                //    if (Angle == 0) NowSize = DefaultGeneralSize;
                //    else
                //    {
                //        double rAngle = MathExtended.ToRadians(Angle);
                //        double sin = Math.Abs(Math.Sin(rAngle));
                //        double cos = Math.Abs(Math.Cos(rAngle));
                //        double tempW = DefaultGeneralSize.Y * sin + DefaultGeneralSize.X * cos;
                //        double tempH = DefaultGeneralSize.Y * cos + DefaultGeneralSize.X * sin;
                //        NowSize = new EPoint((int)Math.Ceiling(tempW), (int)Math.Ceiling(tempH));
                //    }
                //    GSBuffer.Enqueue(new BufferItem<EPoint>(norm, NowSize));
                //    if (GSBuffer.Count > BaseObj.CollisionBufferSize)
                //        GSBuffer.Dequeue();
                //    return new Rectangle(BaseObj.Location, NowSize);
                //}
                public Vector[] GetCollision()
                {
                    var norm = BaseObj.Direction;
                    var Canfind = CBuffer.FirstOrDefault((p) => p.Key == norm);
                    if (Canfind != null) return Canfind.Value;

                    double Angle = Vector.CalcAngleNormalVect(Vector.Up, norm);
                    Canfind = new BufferItem<Vector[]>(norm, DefaultCollision.Select((p) => p.Rotate(Angle)).ToArray());
                    CBuffer.Enqueue(Canfind);
                    if (CBuffer.Count > BaseObj.CollisionBufferSize)
                        CBuffer.Dequeue();
                    return Canfind.Value;
                }
                class BufferItem<T>
                {
                    public readonly Vector Key = Vector.Empty;
                    public readonly T Value;
                    public BufferItem(Vector Key, T Value)
                    {
                        this.Key = Key;
                        this.Value = Value;
                    }
                }
            }
        }
    }
}
