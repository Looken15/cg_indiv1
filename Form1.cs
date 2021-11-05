using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Indiv1
{
    public partial class Form1 : Form
    {
        Bitmap bitmap;
        Graphics g;
        const int point_radius = 5;
        Polygon prim_polygon;
        bool first_point = true;
        const int locate_radius = 20;
        Color point_color = Color.Blue;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            pictureBox1.Image = bitmap;
            prim_polygon = new Polygon();
        }

        private void DrawPolygon()
        {
            PolygonPoint p = prim_polygon.start;
            if (prim_polygon.size == 1)
            {
                DrawPoint((int)p.x, (int)p.y, point_radius);
                return;
            }
            PolygonPoint p1 = new PolygonPoint();
            for (int i = 0; i < prim_polygon.size - 1; ++i)
            {
                DrawPoint((int)p.x, (int)p.y, point_radius);
                p1 = p.next;
                g.DrawLine(new Pen(point_color, 3), p.ToPoint(), p1.ToPoint());
                DrawPoint((int)p1.x, (int)p1.y, point_radius);
                p = p1;
            }
            if (prim_polygon.done)
                g.DrawLine(new Pen(point_color, 3), p.ToPoint(), prim_polygon.start.ToPoint());
            else
                g.DrawLine(new Pen(point_color, 3), p.ToPoint(), p1.ToPoint());
            pictureBox1.Refresh();
        }

        private void DrawPoint(int x, int y, int r)
        {
            g.FillEllipse(new SolidBrush(point_color), x - r, y - r, 2 * r, 2 * r);
            pictureBox1.Refresh();
        }

        public class PolygonPoint
        {
            public float x;
            public float y;
            public PolygonPoint prev;
            public PolygonPoint next;

            public PolygonPoint()
            {
                x = -1;
                y = -1;
                prev = null;
                next = null;
            }
            public PolygonPoint(PointF _p, PolygonPoint _prev, PolygonPoint _next)
            {
                x = _p.X;
                y = _p.Y;
                prev = _prev;
                next = _next;
            }
            public PolygonPoint(PointF _p)
            {
                x = _p.X;
                y = _p.Y;
                prev = null;
                next = null;
            }
            public PolygonPoint(float _x, float _y, PolygonPoint _prev, PolygonPoint _next)
            {
                x = _x;
                y = _y;
                prev = _prev;
                next = _next;
            }
            public Point ToPoint()
            {
                return new Point((int)x, (int)y);
            }
        }

        public class Polygon
        {
            public PolygonPoint start;
            public int size;
            public bool done;

            public Polygon()
            {
                start = null;
                size = 0;
                done = false;
            }

            public Polygon(PointF p)
            {
                start = new PolygonPoint(p);
                size = 1;
                done = false;
            }

            public PolygonPoint getlast()
            {
                if (done) return start;
                PolygonPoint pp = start;
                while (pp.next != null)
                {
                    pp = pp.next;
                }
                return pp;
            }

            public void addPoint(PointF p)
            {
                if (done) return;
                PolygonPoint pp = start;
                if (pp == null)
                {
                    start = new PolygonPoint(p);
                    size = 1;
                    return;
                }
                Point pPoint = pp.ToPoint();
                if (pPoint == p)
                {
                    PolygonPoint last = getlast();
                    last.next = start;
                    start.prev = last;
                    done = true;
                    return;
                }
                while (pp.next != null)
                {
                    pp = pp.next;
                }
                pp.next = new PolygonPoint(p);
                PolygonPoint ppp = pp.next;
                ppp.prev = pp;
                size++;
            }
        }

        public class Edge
        {
            public Point p1;
            public Point p2;
            public Edge()
            {
                p1 = new Point(-1, -1);
                p2 = new Point(-1, -1);
            }
            public Edge(Point _p1, Point _p2)
            {
                p1 = _p1;
                p2 = _p2;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var me = (MouseEventArgs)e;
            if (prim_polygon.done)
                prim_polygon = new Polygon();
            if (first_point)
            {
                first_point = false;
                prim_polygon.addPoint(me.Location);
            }
            else if (LocatePoint(me.Location))
            {
                prim_polygon.addPoint(prim_polygon.start.ToPoint());
                first_point = true;
                RedrawScene();
                PolygonFragmentation();
            }
            else { prim_polygon.addPoint(me.Location); }

            if (!prim_polygon.done) RedrawScene();
        }

        private void RedrawScene()
        {
            g.Clear(Color.White);

            if (!first_point || prim_polygon.done)
            {
                DrawPolygon();
            }

            pictureBox1.Refresh();
        }
        private bool LocatePoint(Point p)
        {
            return Math.Abs(p.X - prim_polygon.start.x) < locate_radius && Math.Abs(p.Y - prim_polygon.start.y) < locate_radius;
        }

        private void PolygonFragmentation()
        {
            var edges = PolygonToListEdges();

            //находим самую левую точку
            var points = new List<PolygonPoint>();
            var f = prim_polygon.start;
            points.Add(f);
            var c = prim_polygon.start.next;
            while (c != f)
            {
                points.Add(c);
                c = c.next;
            }
            var first = points.OrderBy(x => x.x).First();

            //двигаемся по часовой и ищем изгибы
            var p = f;
            for (int i = 0; i < prim_polygon.size; ++i)
            {
                if (IsLeftBend(p))
                {
                    g.FillRectangle(new SolidBrush(Color.Red), p.x - point_radius, p.y - point_radius, 2 * point_radius, 2 * point_radius);

                    var y1 = (int)p.y + 1;
                    var y2 = (int)p.y - 1;
                    while (bitmap.GetPixel((int)p.x, y1).ToArgb() != Color.Blue.ToArgb())
                        y1++;
                    while (bitmap.GetPixel((int)p.x, y2).ToArgb() != Color.Blue.ToArgb())
                        y2--;

                    var p1 = new Point((int)p.x, y1);
                    var p2 = new Point((int)p.x, y2);

                    var e1 = new Edge();
                    var e2 = new Edge();
                    foreach (var edge in edges)
                    {
                        if (PointPositionToEdge(p1, edge.p1, edge.p2) == 0)
                            e1 = edge;
                        if (PointPositionToEdge(p2, edge.p1, edge.p2) == 0)
                            e2 = edge;
                    }

                    var l1 = e1.p1.X < e1.p2.X ? e1.p1 : e1.p2;
                    var l2 = e2.p1.X < e2.p2.X ? e2.p1 : e2.p2;

                    var pl1 = points.Find(a => (a.x == l1.X && a.y == l1.Y));
                    var pl2 = points.Find(a => (a.x == l2.X && a.y == l2.Y));

                    if (pl1 == pl2)
                    {
                        g.DrawLine(new Pen(Color.Black), pl2.ToPoint(), p.ToPoint());
                    }
                    else
                    {
                        var min_r = Math.Min(Math.Abs(p.x - pl1.x), Math.Abs(p.x - pl2.x));
                        var min_p = Math.Abs(p.x - pl1.x) < Math.Abs(p.x - pl2.x) ? pl1 : pl2;
                        while (pl2 != pl1)
                        {
                            if (Math.Abs(p.x - pl2.x) < min_r)
                            {
                                min_r = Math.Abs(p.x - pl2.x);
                                min_p = pl2;
                            }
                            pl2 = pl2.next;
                        }
                        g.DrawLine(new Pen(Color.Black), pl2.ToPoint(), p.ToPoint());
                    }

                    g.DrawLine(new Pen(Color.Pink), e1.p1, e1.p2);
                    g.DrawLine(new Pen(Color.Pink), e2.p1, e2.p2);
                    g.FillRectangle(new SolidBrush(Color.Pink), p.x - point_radius, y1 - point_radius, 2 * point_radius, 2 * point_radius);
                    g.FillRectangle(new SolidBrush(Color.Pink), p.x - point_radius, y2 - point_radius, 2 * point_radius, 2 * point_radius);
                }
                p = p.next;
            }

            for (int i = 0; i < prim_polygon.size; ++i)
            {
                if (IsRightBend(p))
                {
                    g.FillRectangle(new SolidBrush(Color.Green), p.x - point_radius, p.y - point_radius, 2 * point_radius, 2 * point_radius);

                    var y1 = (int)p.y + 1;
                    var y2 = (int)p.y - 1;
                    while (bitmap.GetPixel((int)p.x, y1).ToArgb() != Color.Blue.ToArgb())
                        y1++;
                    while (bitmap.GetPixel((int)p.x, y2).ToArgb() != Color.Blue.ToArgb())
                        y2--;

                    var p1 = new Point((int)p.x, y1);
                    var p2 = new Point((int)p.x, y2);

                    var e1 = new Edge();
                    var e2 = new Edge();
                    foreach (var edge in edges)
                    {
                        if (PointPositionToEdge(p1, edge.p1, edge.p2) == 0)
                            e1 = edge;
                        if (PointPositionToEdge(p2, edge.p1, edge.p2) == 0)
                            e2 = edge;
                    }

                    var l1 = e1.p1.X > e1.p2.X ? e1.p1 : e1.p2;
                    var l2 = e2.p1.X > e2.p2.X ? e2.p1 : e2.p2;

                    var pl1 = points.Find(a => (a.x == l1.X && a.y == l1.Y));
                    var pl2 = points.Find(a => (a.x == l2.X && a.y == l2.Y));

                    if (pl1 == pl2)
                    {
                        g.DrawLine(new Pen(Color.Black), pl1.ToPoint(), p.ToPoint());
                    }
                    else
                    {
                        var min_r = Math.Min(Math.Abs(p.x - pl1.x), Math.Abs(p.x - pl2.x));
                        var min_p = Math.Abs(p.x - pl1.x) < Math.Abs(p.x - pl2.x) ? pl1 : pl2;
                        while (pl1 != pl2)
                        {
                            if (Math.Abs(p.x - pl2.x) < min_r)
                            {
                                min_r = Math.Abs(p.x - pl2.x);
                                min_p = pl2;
                            }
                            pl1 = pl1.prev;
                        }
                        g.DrawLine(new Pen(Color.Black), pl1.ToPoint(), p.ToPoint());
                    }

                    g.DrawLine(new Pen(Color.Yellow), e1.p1, e1.p2);
                    g.DrawLine(new Pen(Color.Yellow), e2.p1, e2.p2);
                    g.FillRectangle(new SolidBrush(Color.Yellow), p.x - point_radius, y1 - point_radius, 2 * point_radius, 2 * point_radius);
                    g.FillRectangle(new SolidBrush(Color.Yellow), p.x - point_radius, y2 - point_radius, 2 * point_radius, 2 * point_radius);
                }
                p = p.prev;
            }
            pictureBox1.Refresh();
        }

        private List<Edge> PolygonToListEdges()
        {
            var res = new List<Edge>();
            var f = prim_polygon.start;
            var c = prim_polygon.start.next;
            while (c != f)
            {
                res.Add(new Edge(c.prev.ToPoint(), c.ToPoint()));
                c = c.next;
            }
            res.Add(new Edge(c.prev.ToPoint(), c.ToPoint()));
            return res;
        }

        //-1 - left, 0 - on edge, 1 - right
        private int PointPositionToEdge(Point p, Point p1, Point p2)
        {
            double yp = p2.Y - p.Y; double yp1 = p2.Y - p1.Y;
            double xp = p.X - p2.X; double xp1 = p1.X - p2.X;

            var r = yp * xp1 - xp * yp1;

            if (Math.Abs(r) < 1000)
                return 0;
            if (r > 0)
                return -1;
            if (r < 0)
                return 1;
            return 0;
        }

        double AngleInPoint(PolygonPoint p)
        {
            var a = (p.x - p.prev.x, p.y - p.prev.y);
            var b = (p.x - p.next.x, p.y - p.next.y);

            var num = a.Item1 * b.Item1 + a.Item2 * b.Item2;
            var den = Math.Sqrt(a.Item1 * a.Item1 + a.Item2 * a.Item2) * Math.Sqrt(b.Item1 * b.Item1 + b.Item2 * b.Item2);

            var cos = num / den;

            var angle = Math.Acos(cos);

            return angle * 180 / Math.PI;
        }

        private bool IsLeftBend(PolygonPoint p)
        {
            return p.next.x > p.x && p.prev.x > p.x && PointPositionToEdge(p.next.ToPoint(), p.prev.ToPoint(), p.ToPoint()) == 1;
        }
        private bool IsRightBend(PolygonPoint p)
        {
            return p.next.x < p.x && p.prev.x < p.x && PointPositionToEdge(p.next.ToPoint(), p.prev.ToPoint(), p.ToPoint()) == 1;
        }
    }
}
