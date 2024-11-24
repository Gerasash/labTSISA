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
        private List<PointF> intersectionPoints = new List<PointF>(); // Список точек пересечения

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
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            int width = this.ClientSize.Width;
            int height = this.ClientSize.Height;

            // Настройка координатной системы
            float scaleX = width / 10f;
            float scaleY = height / 10f;

            // Ось X и ось Y
            Pen axisPen = new Pen(Color.Black, 2);
            g.DrawLine(axisPen, 0, height, width, height); // Ось X
            g.DrawLine(axisPen, 0, 0, 0, height); // Ось Y

            // Рисуем ограничения
            intersectionPoints.Clear(); // Очищаем список точек пересечения перед перерисовкой
            DrawConstraint(g, x => 8 - x, scaleX, scaleY, width, height, Color.Blue, "x1 + x2 <= 8");
            DrawConstraint(g, x => 6 - 3 * x, scaleX, scaleY, width, height, Color.Green, "3x1 + x2 <= 6");
            DrawConstraint(g, x => (3 - 2 * x) / 3, scaleX, scaleY, width, height, Color.Orange, "2x1 + 3x2 >= 3");

            // Находим точки пересечения
            FindIntersectionPoints();

            // Рисуем многоугольник с использованием алгоритма Джарвиса
            List<PointF> convexHull = JarvisAlgorithm(intersectionPoints);
            DrawConvexHull(g, convexHull, scaleX, scaleY, height);

            // Рисуем целевую функцию
            DrawObjectiveFunction(g, zValue, scaleX, scaleY, width, height, Color.Red);
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
                    PointF currentPoint = new PointF(x * scaleX, height - y * scaleY);
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

        private void FindIntersectionPoints()
        {
            // Решаем систему уравнений для каждой пары ограничений
            // Ограничения:
            // x1 + x2 <= 8
            // 3x1 + x2 <= 6
            // 2x1 + 3x2 >= 3

            // Первая линия: x1 + x2 = 8
            // Вторая линия: 3x1 + x2 = 6
            intersectionPoints.Add(SolveIntersection(1, 1, 3, 1, 8, 6)); // Пересечение первых двух линий

            // Пересечение второй и третьей линии:
            intersectionPoints.Add(SolveIntersection(3, 1, 2, 3, 6, 3)); // Пересечение второй и третьей

            // Пересечение первой и третьей линии:
            intersectionPoints.Add(SolveIntersection(1, 1, 2, 3, 8, 3)); // Пересечение первой и третьей
        }

        private PointF SolveIntersection(double a1, double b1, double a2, double b2, double c1, double c2)
        {
            // Решаем систему уравнений:
            // a1*x + b1*y = c1
            // a2*x + b2*y = c2

            double det = a1 * b2 - a2 * b1;
            if (Math.Abs(det) < 1e-10) return new PointF(-1, -1); // Если определитель равен нулю, то линии параллельны

            double x = (c1 * b2 - c2 * b1) / det;
            double y = (a1 * c2 - a2 * c1) / det;

            return new PointF((float)x, (float)y);
        }

        private List<PointF> JarvisAlgorithm(List<PointF> points)
        {
            if (points.Count <= 1) return points;

            // Находим самую левую точку
            PointF start = points.OrderBy(p => p.X).ThenBy(p => p.Y).First();
            List<PointF> hull = new List<PointF>();
            PointF current = start;

            do
            {
                hull.Add(current);
                PointF next = points[0];

                foreach (var point in points)
                {
                    if (point.Equals(current)) continue;
                    float crossProduct = (next.X - current.X) * (point.Y - current.Y) - (next.Y - current.Y) * (point.X - current.X);
                    if (crossProduct > 0 || (crossProduct == 0 && Distance(current, point) > Distance(current, next)))
                    {
                        next = point;
                    }
                }

                current = next;
            }
            while (!current.Equals(start));

            return hull;
        }

        private float Distance(PointF a, PointF b)
        {
            return (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        private void DrawConvexHull(Graphics g, List<PointF> convexHull, float scaleX, float scaleY, int height)
        {
            if (convexHull.Count < 3) return;

            Pen hullPen = new Pen(Color.Purple, 2);
            for (int i = 0; i < convexHull.Count - 1; i++)
            {
                PointF start = convexHull[i];
                PointF end = convexHull[i + 1];
                g.DrawLine(hullPen, start.X * scaleX, height - start.Y * scaleY, end.X * scaleX, height - end.Y * scaleY);
            }

            // Замкнуть многоугольник
            PointF first = convexHull[0];
            PointF last = convexHull[convexHull.Count - 1];
            g.DrawLine(hullPen, last.X * scaleX, height - last.Y * scaleY, first.X * scaleX, height - first.Y * scaleY);
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
                    PointF currentPoint = new PointF(x * scaleX, height - y * scaleY);
                    if (prevPoint.HasValue)
                    {
                        g.DrawLine(objectivePen, prevPoint.Value, currentPoint);
                    }
                    prevPoint = currentPoint;
                }
            }
        }
    }
}