using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace labTSISA
{
    public partial class Form1 : Form
    {
        private int zValue = 10; // Значение целевой функции Z
        private int offsetX = 50; // Смещение по оси X
        private int offsetY = 150; // Смещение по оси Y
        private List<PointF> points; // Список точек для построения выпуклой оболочки

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true; // Убираем мерцание графики

            // Создаем трекбар для изменения Z
            TrackBar zSlider = new TrackBar
            {
                Dock = DockStyle.Bottom,
                Minimum = 0,
                Maximum = 30,
                TickFrequency = 1,
                Value = zValue
            };
            zSlider.Scroll += (s, e) =>
            {
                zValue = zSlider.Value;
                this.Invalidate(); // Перерисовка формы
            };
            Controls.Add(zSlider);

            // Инициализация списка точек
            points = new List<PointF>
            {
                new PointF(2, 3),
                new PointF(3, 6),
                new PointF(4, 4),
                new PointF(5, 5),
                new PointF(7, 2),
                new PointF(8, 7),
                new PointF(9, 1)
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            int width = this.ClientSize.Width;
            int height = this.ClientSize.Height;

            // Настройка координатной системы
            float scaleX = (width - offsetX) / 10f;
            float scaleY = (height - offsetY) / 10f;

            // Ось X и ось Y с учетом смещения
            Pen axisPen = new Pen(Color.Black, 2);
            g.DrawLine(axisPen, offsetX, height - offsetY, width, height - offsetY); // Ось X
            g.DrawLine(axisPen, offsetX, 0, offsetX, height); // Ось Y

            // Рисуем насечки и подписи на осях
            DrawAxisTicks(g, scaleX, scaleY, width, height);

            // Рисуем ограничения
            DrawConstraint(g, x => 8 - x, scaleX, scaleY, width, height, Color.Blue, "x1 + x2 <= 8");
            DrawConstraint(g, x => 6 - 3 * x, scaleX, scaleY, width, height, Color.Green, "3x1 + x2 <= 6");
            DrawConstraint(g, x => (3 - 2 * x) / 3, scaleX, scaleY, width, height, Color.Orange, "2x1 + 3x2 >= 3");

            // Рисуем целевую функцию
            DrawObjectiveFunction(g, zValue, scaleX, scaleY, width, height, Color.Red);

            // Рисуем точки
            foreach (var point in points)
            {
                g.FillEllipse(Brushes.Black, offsetX + point.X * scaleX - 3, height - offsetY - point.Y * scaleY - 3, 6, 6);
            }

            // Рисуем выпуклую оболочку с использованием алгоритма Джарвиса
            List<PointF> convexHull = JarvisMarch(points);
            for (int i = 0; i < convexHull.Count; i++)
            {
                PointF start = convexHull[i];
                PointF end = convexHull[(i + 1) % convexHull.Count]; // Следующая точка, замкнем оболочку
                g.DrawLine(Pens.Red, offsetX + start.X * scaleX, height - offsetY - start.Y * scaleY,
                    offsetX + end.X * scaleX, height - offsetY - end.Y * scaleY);
            }
        }

        // Функция алгоритма Джарвиса для нахождения выпуклой оболочки
        private List<PointF> JarvisMarch(List<PointF> points)
        {
            // Находим точку с наименьшей X-координатой (или наименьшей Y, если X одинаковы)
            PointF leftMost = points.OrderBy(p => p.X).ThenBy(p => p.Y).First();
            List<PointF> hull = new List<PointF>();
            PointF currentPoint = leftMost;

            do
            {
                hull.Add(currentPoint);
                PointF nextPoint = points[0];

                foreach (var candidate in points)
                {
                    // Проверяем, является ли кандидат более левым по отношению к текущей точке
                    float crossProduct = (nextPoint.X - currentPoint.X) * (candidate.Y - currentPoint.Y) -
                                         (nextPoint.Y - currentPoint.Y) * (candidate.X - currentPoint.X);
                    if (crossProduct > 0 || (crossProduct == 0 && Distance(currentPoint, candidate) > Distance(currentPoint, nextPoint)))
                    {
                        nextPoint = candidate;
                    }
                }

                currentPoint = nextPoint;

            } while (currentPoint != leftMost);

            return hull;
        }

        // Функция для вычисления расстояния между двумя точками
        private float Distance(PointF p1, PointF p2)
        {
            return (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        private void DrawConstraint(Graphics g, Func<double, double> constraintFunc, float scaleX, float scaleY, int width, int height, Color color, string label)
        {
            Pen constraintPen = new Pen(color, 2);

            PointF? prevPoint = null;

            for (float x = 0; x <= 10; x += 0.1f)
            {
                float y = (float)constraintFunc(x);
                if (y >= 0 && y <= 10)
                {
                    PointF currentPoint = new PointF(offsetX + x * scaleX, height - offsetY - y * scaleY);
                    if (prevPoint.HasValue)
                    {
                        g.DrawLine(constraintPen, prevPoint.Value, currentPoint);
                    }
                    prevPoint = currentPoint;
                }
            }

            // Отображение метки
            g.DrawString(label, new Font("Arial", 10), new SolidBrush(color), 10, height - 30);
        }

        private void DrawObjectiveFunction(Graphics g, int z, float scaleX, float scaleY, int width, int height, Color color)
        {
            Pen objectivePen = new Pen(color, 2);
            PointF? prevPoint = null;

            for (float x = 0; x <= 10; x += 0.1f)
            {
                float y = (float)((z - x) / 3.0); // Целевая функция: z = x1 + 3x2 -> x2 = (z - x1) / 3
                if (y >= 0 && y <= 10)
                {
                    PointF currentPoint = new PointF(offsetX + x * scaleX, height - offsetY - y * scaleY);
                    if (prevPoint.HasValue)
                    {
                        g.DrawLine(objectivePen, prevPoint.Value, currentPoint);
                    }
                    prevPoint = currentPoint;
                }
            }

            // Отображение метки
            g.DrawString($"z = {z}", new Font("Arial", 10), new SolidBrush(color), 10, height - 50);
        }

        private void DrawAxisTicks(Graphics g, float scaleX, float scaleY, int width, int height)
        {
            // Настройка пера для насечек
            Pen tickPen = new Pen(Color.Black, 1);
            Font font = new Font("Arial", 8);
            SolidBrush brush = new SolidBrush(Color.Black);

            // Рисуем насечки на оси X
            for (int i = 0; i <= 10; i++)
            {
                float x = offsetX + i * scaleX;
                g.DrawLine(tickPen, x, height - offsetY - 5, x, height - offsetY + 5); // вертикальная насечка
                g.DrawString(i.ToString(), font, brush, x - 10, height - offsetY + 10); // подпись
            }

            // Рисуем насечки на оси Y
            for (int i = 0; i <= 10; i++)
            {
                float y = height - offsetY - i * scaleY;
                g.DrawLine(tickPen, offsetX - 5, y, offsetX + 5, y); // горизонтальная насечка
                g.DrawString(i.ToString(), font, brush, offsetX - 20, y - 10); // подпись
            }
        }
    }
}
