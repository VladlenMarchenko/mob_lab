using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RoadEditor
{
    public static class TileGenerator
    {
        private const int Size = 100;

        private static readonly SolidColorBrush GrassBrush = new SolidColorBrush(Color.FromRgb(105, 180, 89));
        private static readonly SolidColorBrush RoadBrush = new SolidColorBrush(Color.FromRgb(95, 99, 104));
        private static readonly SolidColorBrush LineBrush = Brushes.White;

        public static BitmapSource CreateGrass()
        {
            return DrawTile(ctx => {
                ctx.DrawRectangle(GrassBrush, null, new Rect(0, 0, Size, Size));
            });
        }

        // НОВОЕ: Рисуем кустик на траве
        public static BitmapSource CreateGrassWithBush()
        {
            return DrawTile(ctx => {
                // Сначала рисуем обычную траву
                ctx.DrawRectangle(GrassBrush, null, new Rect(0, 0, Size, Size));

                // Настройки кистей для куста
                SolidColorBrush bushBrush = new SolidColorBrush(Color.FromRgb(40, 140, 40)); // Темно-зеленый
                SolidColorBrush bushOutline = new SolidColorBrush(Color.FromRgb(20, 100, 20)); // Обводка
                Pen outlinePen = new Pen(bushOutline, 2);

                // Рисуем кустик из трех пересекающихся кругов
                ctx.DrawEllipse(bushBrush, outlinePen, new Point(Size / 2, Size / 2), 25, 25);
                ctx.DrawEllipse(bushBrush, outlinePen, new Point(Size / 2 + 15, Size / 2 + 10), 20, 20);
                ctx.DrawEllipse(bushBrush, outlinePen, new Point(Size / 2 - 15, Size / 2 + 5), 18, 18);
            });
        }

        public static BitmapSource CreateIntersection()
        {
            return DrawTile(ctx => {
                ctx.DrawRectangle(RoadBrush, null, new Rect(0, 0, Size, Size));
            });
        }

        public static BitmapSource CreateRoadHorizontal()
        {
            return DrawTile(ctx => {
                ctx.DrawRectangle(RoadBrush, null, new Rect(0, 0, Size, Size));
                Pen dashedPen = new Pen(LineBrush, 4) { DashStyle = DashStyles.Dash };
                ctx.DrawLine(dashedPen, new Point(0, Size / 2), new Point(Size, Size / 2));
            });
        }

        public static BitmapSource CreateRoadVertical()
        {
            return DrawTile(ctx => {
                ctx.DrawRectangle(RoadBrush, null, new Rect(0, 0, Size, Size));
                Pen dashedPen = new Pen(LineBrush, 4) { DashStyle = DashStyles.Dash };
                ctx.DrawLine(dashedPen, new Point(Size / 2, 0), new Point(Size / 2, Size));
            });
        }


        public static BitmapSource CreateCrosswalkHorizontal()
        {
            return DrawTile(ctx => {
                ctx.DrawRectangle(RoadBrush, null, new Rect(0, 0, Size, Size));
                for (int y = 15; y <= 85; y += 15)
                {
                    ctx.DrawRectangle(LineBrush, null, new Rect(20, y, 60, 10));
                }
            });
        }

        // ИСПРАВЛЕНО: Зебра для вертикальной дороги (полосы идут вертикально)
        public static BitmapSource CreateCrosswalkVertical()
        {
            return DrawTile(ctx => {
                ctx.DrawRectangle(RoadBrush, null, new Rect(0, 0, Size, Size));
                for (int x = 15; x <= 85; x += 15)
                {
                    ctx.DrawRectangle(LineBrush, null, new Rect(x, 20, 10, 60));
                }
            });
        }

        private static BitmapSource DrawTile(Action<DrawingContext> drawAction)
        {
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext ctx = visual.RenderOpen())
            {
                drawAction(ctx);
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(Size, Size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            return rtb;
        }
    }
}
