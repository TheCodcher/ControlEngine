using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using ControlEngine.Extended;
using System.IO;
using ControlEngine;

namespace ControlEngine
{
    namespace Graphic
    {
        interface ICanBeScreen
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public Color BackColor { get; set; }
            public Graphics GetGraphics();
        }
        class GraphicsEngine : BeTimed //содержит инструкции для графического интерфейса
        {
            private Bitmap grCanvas;
            private Graphics formGraph;
            private List<PlacedImage>[] SpriteLayers;
            private const int LAYERS_MAX = 3;
            private const int MATRIX_CELL_SIZE = 20;
            private const int BIG_DATE_ACTIVE_PER = 3; //0-100% //2%? //до 72 изображений в один кадр?
            private Rectangle[][] DisplayMatrix;
            private List<Rectangle> ActiveDisplayCell;
            private bool bigDate = false;
            private int bigDateActiveCount; //количество активных клеток для перерисовки всего экрана
                                            //float Xscale = 1; //UpScale не работает
                                            //float Yscale = 1; //
            public GraphicsEngine(ICanBeScreen Display)
            {
                formGraph = Display.GetGraphics();
                grCanvas = new Bitmap(Display.Width, Display.Height);
                Graphics grCanvas_graphic = Graphics.FromImage(grCanvas);  //<-- Начало костыля
                grCanvas_graphic.Clear(Display.BackColor);
                grCanvas_graphic.Dispose(); //<-- конец костыля
                SpriteLayers = new List<PlacedImage>[LAYERS_MAX];

                int MatrixWidth = Display.Width / MATRIX_CELL_SIZE + (Display.Width % MATRIX_CELL_SIZE > 0 ? 1 : 0);
                int MatrixHeigth = Display.Height / MATRIX_CELL_SIZE + (Display.Height % MATRIX_CELL_SIZE > 0 ? 1 : 0);
                DisplayMatrix = new Rectangle[MatrixWidth][];
                for (int i = 0; i < MatrixWidth; i++)
                    DisplayMatrix[i] = new Rectangle[MatrixWidth];
                for (int i = 0; i < MatrixWidth; i++)
                    for (int j = 0; j < MatrixHeigth; j++)
                    {
                        DisplayMatrix[i][j] = new Rectangle(i * MATRIX_CELL_SIZE, j * MATRIX_CELL_SIZE, MATRIX_CELL_SIZE, MATRIX_CELL_SIZE);
                        //grCanvas_graphic.DrawRectangle(new Pen(Color.Black, 1), DisplayMatrix[i][j]); //
                    }
                ActiveDisplayCell = new List<Rectangle>();

                //AddFrame(0, new PlacedImage(this, grCanvas, new Point(0, 0)));
                SpriteLayers[0] = new List<PlacedImage>();
                SpriteLayers[0].Add(new PlacedImage(this, grCanvas, new Point(0, 0)));
                bigDateActiveCount = MatrixWidth * MatrixHeigth * BIG_DATE_ACTIVE_PER / 100;
                //grCanvas_graphic.Dispose(); //
            }
            public override void TickUpdate()
            {
                UpdateFrame();
            }
            public void UpdateFrame()
            {
                Bitmap tempCanvas = grCanvas.Clone(new Rectangle(new Point(0, 0), grCanvas.Size), grCanvas.PixelFormat);
                Graphics tempG = Graphics.FromImage(tempCanvas); //готовить кадр в другом потоке и загружать конечный результат в графический интерфейс

                for (int i = 0; i < LAYERS_MAX; i++) //отрисовка внутреннего кадра
                    if (SpriteLayers[i] != null)
                        foreach (var sprite in SpriteLayers[i])
                            tempG.DrawImage(sprite.Image, sprite.Location);
                //Parallel.ForEach(SpriteLayers[i], (sprite) => tempG.DrawImage(sprite.Image, sprite.Location));
                if (bigDate)
                    formGraph.DrawImage(tempCanvas, 0, 0);
                else
                    foreach (var rect in ActiveDisplayCell)
                        formGraph.DrawImage(tempCanvas, rect, rect, GraphicsUnit.Pixel);

                ActiveDisplayCell.Clear();
                bigDate = false;
                tempCanvas.Dispose();
                tempG.Dispose();
            }
            /// <summary>
            /// Метод, позволяющий добавить объект, необходимый к отрисовке в следующем кадре
            /// </summary>
            /// <param name="layer">Слой, в который будет добавлен объект</param>
            /// <param name="image">Необходимый к отрисовке спрайт</param>
            public void AddFrame(int layer, PlacedImage image)
            {
                if (SpriteLayers[layer] == null) SpriteLayers[layer] = new List<PlacedImage>();
                PlacedImage temp = SpriteLayers[layer].Find((PlacedImage item) => item.Sender.Equals(image.Sender));
                if (temp != null)
                {
                    SpriteLayers[layer].Remove(temp);
                    if (!bigDate) UpdateGraphicMatrix(temp.Location, temp.Image.Size);
                }
                SpriteLayers[layer].Add(image);
                if (!bigDate) UpdateGraphicMatrix(image.Location, image.Image.Size);
            }
            public async void AddFrameAsync(int layer, PlacedImage image)
            {
                await Task.Run(() => AddFrame(layer, image));
            }
            public void AddFrame(int layer, PlacedImage image, bool AllRefresh)
            {
                bigDate = AllRefresh;
                AddFrame(layer, image);
            }
            private void UpdateGraphicMatrix(Point Location, Size rectSize)
            {
                EPoint UpLeft = Location;
                EPoint DownRight = UpLeft + (EPoint)rectSize;
                Point UpLeftIndx = new Point(0, 0);
                Point DownRightIndx = new Point(0, 0);

                void findMatrixIdex(EPoint pToFind, ref Point result)
                {
                    for (int i = 0; i < DisplayMatrix.Length; i++) //
                    {
                        int tempHeigthIndx = Array.FindIndex(DisplayMatrix[i], (cell) => pToFind.Include(cell));
                        if (tempHeigthIndx != -1)
                        {
                            result = new Point(i, tempHeigthIndx);
                            break;
                        }
                    }
                }
                Parallel.Invoke(() => findMatrixIdex(UpLeft, ref UpLeftIndx), () => findMatrixIdex(DownRight, ref DownRightIndx));

                for (int i = UpLeftIndx.X; i <= DownRightIndx.X; i++)
                    for (int j = UpLeftIndx.Y; j <= DownRightIndx.Y; j++)
                    {
                        var temp = DisplayMatrix[i][j];
                        if (!ActiveDisplayCell.Contains(temp))
                        {
                            lock (ActiveDisplayCell)
                            {
                                ActiveDisplayCell.Add(temp);
                            }
                        }
                    }
                if (ActiveDisplayCell.Count >= bigDateActiveCount) bigDate = true;
            }
        }
    }
}

