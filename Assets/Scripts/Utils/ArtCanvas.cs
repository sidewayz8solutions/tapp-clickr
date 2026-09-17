using UnityEngine;

namespace TappBird
{
    /// <summary>
    /// Tiny software rasterizer used to generate all game art at runtime.
    /// Draws into a premultiplied-alpha buffer at 2x supersample, then downsamples
    /// to the final texture for smooth anti-aliased edges.
    /// </summary>
    public class Canvas
    {
        public readonly int W, H;          // final dimensions
        readonly int sw, sh;               // supersampled dimensions
        readonly int SS = 2;
        readonly float[] r, g, b, a;

        public Canvas(int w, int h)
        {
            W = w; H = h;
            sw = w * SS; sh = h * SS;
            r = new float[sw * sh]; g = new float[sw * sh];
            b = new float[sw * sh]; a = new float[sw * sh];
        }

        // ---- helpers ----------------------------------------------------

        int Idx(int x, int y) => y * sw + x;

        void Blend(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= sw || y >= sh) return;
            int i = Idx(x, y);
            float sa = Mathf.Clamp01(c.a);
            if (sa <= 0f) return;
            float om = 1f - sa;
            r[i] = c.r * sa + r[i] * om;
            g[i] = c.g * sa + g[i] * om;
            b[i] = c.b * sa + b[i] * om;
            a[i] = sa + a[i] * om;
        }

        public void Clear(Color c)
        {
            for (int i = 0; i < r.Length; i++)
            {
                r[i] = c.r; g[i] = c.g; b[i] = c.b; a[i] = 1f;
            }
        }

        // ---- shapes ------------------------------------------------------

        public void FillRect(float x0, float y0, float x1, float y1, Color c)
        {
            int xa = Mathf.FloorToInt(x0 * SS), xb = Mathf.CeilToInt(x1 * SS);
            int ya = Mathf.FloorToInt(y0 * SS), yb = Mathf.CeilToInt(y1 * SS);
            for (int y = ya; y < yb; y++)
                for (int x = xa; x < xb; x++)
                    Blend(x, y, c);
        }

        public void FillEllipse(Vector2 center, float rx, float ry, Color c, float rotDeg = 0f)
        {
            var pts = EllipsePoints(center, rx, ry, rotDeg, 28);
            FillPolygon(pts, c);
        }

        public void StrokeEllipse(Vector2 center, float rx, float ry, Color c, float thickness, float rotDeg = 0f)
        {
            var pts = EllipsePoints(center, rx, ry, rotDeg, 40);
            StrokePolygon(pts, c, thickness);
        }

        static Vector2[] EllipsePoints(Vector2 center, float rx, float ry, float rotDeg, int n)
        {
            var pts = new Vector2[n];
            float ra = rotDeg * Mathf.Deg2Rad;
            float ca = Mathf.Cos(ra), sa = Mathf.Sin(ra);
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                float x = Mathf.Cos(a) * rx, y = Mathf.Sin(a) * ry;
                pts[i] = new Vector2(center.x + x * ca - y * sa, center.y + x * sa + y * ca);
            }
            return pts;
        }

        public void FillCircle(Vector2 center, float radius, Color c)
        {
            int xa = Mathf.FloorToInt((center.x - radius) * SS), xb = Mathf.CeilToInt((center.x + radius) * SS);
            int ya = Mathf.FloorToInt((center.y - radius) * SS), yb = Mathf.CeilToInt((center.y + radius) * SS);
            float rs = radius * SS;
            for (int y = ya; y < yb; y++)
            {
                for (int x = xa; x < xb; x++)
                {
                    float px = x / (float)SS, py = y / (float)SS;
                    float d = (px - center.x) * (px - center.x) + (py - center.y) * (py - center.y);
                    if (d <= radius * radius)
                        Blend(x, y, c);
                }
            }
        }

        /// <summary>Circle with a soft radial falloff (used for glows).</summary>
        public void FillCircleSoft(Vector2 center, float radius, Color c, float softEdge = 1f)
        {
            int xa = Mathf.FloorToInt((center.x - radius) * SS), xb = Mathf.CeilToInt((center.x + radius) * SS);
            int ya = Mathf.FloorToInt((center.y - radius) * SS), yb = Mathf.CeilToInt((center.y + radius) * SS);
            float r2 = radius * radius;
            float inner = Mathf.Max(0.001f, radius - softEdge);
            float inner2 = inner * inner;
            for (int y = ya; y < yb; y++)
            {
                for (int x = xa; x < xb; x++)
                {
                    float px = x / (float)SS, py = y / (float)SS;
                    float d = (px - center.x) * (px - center.x) + (py - center.y) * (py - center.y);
                    if (d >= r2) continue;
                    float t = 1f;
                    if (d > inner2)
                        t = Mathf.Sqrt(Mathf.Clamp01((r2 - d) / (r2 - inner2)));
                    Color cc = c; cc.a = c.a * t;
                    Blend(x, y, cc);
                }
            }
        }

        public void StrokeCircle(Vector2 center, float radius, Color c, float thickness)
        {
            int xa = Mathf.FloorToInt((center.x - radius - thickness) * SS), xb = Mathf.CeilToInt((center.x + radius + thickness) * SS);
            int ya = Mathf.FloorToInt((center.y - radius - thickness) * SS), yb = Mathf.CeilToInt((center.y + radius + thickness) * SS);
            float ro = (radius + thickness) * (radius + thickness);
            float ri = Mathf.Max(0f, radius - thickness) * (radius - thickness);
            for (int y = ya; y < yb; y++)
            {
                for (int x = xa; x < xb; x++)
                {
                    float px = x / (float)SS, py = y / (float)SS;
                    float d = (px - center.x) * (px - center.x) + (py - center.y) * (py - center.y);
                    if (d <= ro && d >= ri)
                        Blend(x, y, c);
                }
            }
        }

        public void FillRoundRect(float x0, float y0, float x1, float y1, float radius, Color c)
        {
            int xa = Mathf.FloorToInt(x0 * SS), xb = Mathf.CeilToInt(x1 * SS);
            int ya = Mathf.FloorToInt(y0 * SS), yb = Mathf.CeilToInt(y1 * SS);
            for (int y = ya; y < yb; y++)
            {
                for (int x = xa; x < xb; x++)
                {
                    float px = x / (float)SS, py = y / (float)SS;
                    if (InRoundRect(px, py, x0, y0, x1, y1, radius))
                        Blend(x, y, c);
                }
            }
        }

        static bool InRoundRect(float px, float py, float x0, float y0, float x1, float y1, float r)
        {
            if (px < x0 || px > x1 || py < y0 || py > y1) return false;
            float cx = Mathf.Clamp(px, x0 + r, x1 - r);
            float cy = Mathf.Clamp(py, y0 + r, y1 - r);
            float dx = px - cx, dy = py - cy;
            return dx * dx + dy * dy <= r * r;
        }

        public void FillRoundRectGradientVert(float x0, float y0, float x1, float y1, float radius, Color top, Color bottom)
        {
            int xa = Mathf.FloorToInt(x0 * SS), xb = Mathf.CeilToInt(x1 * SS);
            int ya = Mathf.FloorToInt(y0 * SS), yb = Mathf.CeilToInt(y1 * SS);
            float hgt = y1 - y0;
            for (int y = ya; y < yb; y++)
            {
                float py = y / (float)SS;
                float t = hgt <= 0f ? 0f : Mathf.Clamp01((py - y0) / hgt);
                Color c = Color.Lerp(top, bottom, t);
                for (int x = xa; x < xb; x++)
                {
                    float px = x / (float)SS;
                    if (InRoundRect(px, py, x0, y0, x1, y1, radius))
                        Blend(x, y, c);
                }
            }
        }

        /// <summary>Even-odd polygon fill (points in local texture space, y up).</summary>
        public void FillPolygon(Vector2[] pts, Color c)
        {
            int n = pts.Length;
            if (n < 3) return;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                minX = Mathf.Min(minX, pts[i].x); maxX = Mathf.Max(maxX, pts[i].x);
                minY = Mathf.Min(minY, pts[i].y); maxY = Mathf.Max(maxY, pts[i].y);
            }
            int y0 = Mathf.FloorToInt(minY * SS), y1 = Mathf.CeilToInt(maxY * SS);
            float[] xs = new float[n];
            for (int y = y0; y <= y1; y++)
            {
                float py = y / (float)SS;
                int count = 0;
                for (int i = 0; i < n; i++)
                {
                    Vector2 p0 = pts[i], p1 = pts[(i + 1) % n];
                    if ((p0.y <= py && p1.y > py) || (p1.y <= py && p0.y > py))
                    {
                        float t = (py - p0.y) / (p1.y - p0.y);
                        xs[count++] = Mathf.Lerp(p0.x, p1.x, t);
                    }
                }
                System.Array.Sort(xs, 0, count);
                for (int s = 0; s + 1 < count; s += 2)
                {
                    int xa = Mathf.FloorToInt(xs[s] * SS), xb = Mathf.CeilToInt(xs[s + 1] * SS);
                    for (int x = xa; x <= xb; x++)
                        Blend(x, y, c);
                }
            }
        }

        /// <summary>Vertical gradient polygon fill.</summary>
        public void FillPolygonGradient(Vector2[] pts, Color top, Color bottom)
        {
            int n = pts.Length;
            if (n < 3) return;
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                minY = Mathf.Min(minY, pts[i].y); maxY = Mathf.Max(maxY, pts[i].y);
            }
            int y0 = Mathf.FloorToInt(minY * SS), y1 = Mathf.CeilToInt(maxY * SS);
            float hgt = maxY - minY;
            float[] xsA = new float[n], xsB = new float[n];
            for (int y = y0; y <= y1; y++)
            {
                float py = y / (float)SS;
                float t = hgt <= 0f ? 0f : Mathf.Clamp01((py - minY) / hgt);
                Color c = Color.Lerp(top, bottom, t);
                int count = 0;
                for (int i = 0; i < n; i++)
                {
                    Vector2 p0 = pts[i], p1 = pts[(i + 1) % n];
                    if ((p0.y <= py && p1.y > py) || (p1.y <= py && p0.y > py))
                    {
                        float tt = (py - p0.y) / (p1.y - p0.y);
                        xsA[count] = Mathf.Lerp(p0.x, p1.x, tt);
                        count++;
                    }
                }
                System.Array.Copy(xsA, xsB, count);
                System.Array.Sort(xsB, 0, count);
                for (int s = 0; s + 1 < count; s += 2)
                {
                    int xa = Mathf.FloorToInt(xsB[s] * SS), xb = Mathf.CeilToInt(xsB[s + 1] * SS);
                    for (int x = xa; x <= xb; x++)
                        Blend(x, y, c);
                }
            }
        }

        /// <summary>Cartoon outline: stamps discs along the polygon perimeter.</summary>
        public void StrokePolygon(Vector2[] pts, Color c, float thickness)
        {
            int n = pts.Length;
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = pts[i], p1 = pts[(i + 1) % n];
                int steps = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(p0, p1) * 2f));
                for (int s = 0; s <= steps; s++)
                {
                    Vector2 p = Vector2.Lerp(p0, p1, s / (float)steps);
                    FillCircle(p, thickness, c);
                }
            }
        }

        // ---- output -------------------------------------------------------

        public Texture2D Build()
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var colors = new Color[W * H];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float ar = 0, ag = 0, ab = 0, aa = 0;
                    for (int sy = 0; sy < SS; sy++)
                        for (int sx = 0; sx < SS; sx++)
                        {
                            int i = (y * SS + sy) * sw + (x * SS + sx);
                            ar += r[i]; ag += g[i]; ab += b[i]; aa += a[i];
                        }
                    float inv = 1f / (SS * SS);
                    ar *= inv; ag *= inv; ab *= inv; aa *= inv;
                    if (aa > 0.0001f && aa < 1f)
                    {
                        ar /= aa; ag /= aa; ab /= aa;
                    }
                    colors[y * W + x] = new Color(ar, ag, ab, aa);
                }
            }
            tex.SetPixels(colors);
            tex.Apply(false, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        public static Sprite ToSprite(Texture2D tex, float pixelsPerUnit, string name)
        {
            var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
            spr.name = name;
            return spr;
        }
    }
}