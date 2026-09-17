using UnityEngine;

namespace TappBird
{
    /// <summary>
    /// Draws every sprite in the game at runtime onto Textures via Canvas.
    /// All art is deterministic (seeded RNG) so it regenerates identically each launch.
    /// </summary>
    public class ArtGen
    {
        // sprite references (world sprites + UI textures)
        public Sprite Sky, Sun, Cloud, HillsFar, ForestMid, Ground, Tree, Bird, Chip, Coin;
        public Texture2D BtnRect, PanelRect, BtnGreenRect;
        public Sprite SoundOn, SoundOff;
        public Texture2D ParticleDot;

        readonly System.Random rng = new System.Random(1337);

        public static ArtGen Build()
        {
            var a = new ArtGen();
            a.BuildAll();
            return a;
        }

        void BuildAll()
        {
            Sky = BakePx("sky", 8, 256, (c, w, h) =>
                c.FillRoundRectGradientVert(-w / 2f, -h / 2f, w / 2f, h / 2f, 0f, Palette.SkyTop, Palette.SkyHorizon));
            Sun = BakePx("sun", 256, 256, DrawSun);
            Cloud = BakePx("cloud", 300, 150, DrawCloud);
            HillsFar = BakePx("hills_far", 900, 260, DrawHillsFar);
            ForestMid = BakePx("forest_mid", 900, 280, DrawForestMid);
            Ground = BakePx("ground", 900, 250, DrawGround);
            Tree = BakePx("tree", 560, 1280, DrawTree);
            Bird = BakePx("bird", 440, 440, DrawBird);
            Chip = BakePx("chip", 48, 48, DrawChip);
            Coin = BakePx("coin", 96, 96, DrawCoin);
            BtnRect = BakeTex("btn_rect", 260, 110, DrawBtnRect);
            BtnGreenRect = BakeTex("btn_green", 260, 110, DrawBtnGreenRect);
            PanelRect = BakeTex("panel_rect", 620, 760, DrawPanelRect);
            SoundOn = BakePx("sound_on", 96, 96, cw => DrawSpeaker(cw, true));
            SoundOff = BakePx("sound_off", 96, 96, cw => DrawSpeaker(cw, false));
            ParticleDot = BakeTex("dot", 32, 32, (c, w, h) => c.FillCircleSoft(Vector2.zero, 15f, Color.white, 3f));
        }

        // ---- baking helpers --------------------------------------------------

        Sprite BakePx(string name, int w, int h, System.Action<Canvas, int, int> draw)
        {
            var c = new Canvas(w, h);
            draw(c, w, h);
            return Canvas.ToSprite(c.Build(), 200f, name);
        }

        Texture2D BakeTex(string name, int w, int h, System.Action<Canvas, int, int> draw)
        {
            var c = new Canvas(w, h);
            draw(c, w, h);
            var t = c.Build();
            t.name = name;
            return t;
        }

        Sprite BakePx(string name, int w, int h, System.Action<Canvas> draw)
        {
            var c = new Canvas(w, h);
            draw(c);
            return Canvas.ToSprite(c.Build(), 200f, name);
        }

        // ---- individual drawings ----------------------------------------------

        void DrawSun(Canvas c, int w, int h)
        {
            c.FillCircleSoft(Vector2.zero, w * 0.46f, Palette.SunGlow, w * 0.18f);
            c.FillCircle(Vector2.zero, w * 0.20f, Palette.SunDisc);
            c.FillCircleSoft(Vector2.zero, w * 0.16f, new Color(1f, 1f, 1f, 0.5f), w * 0.05f);
        }

        void DrawCloud(Canvas c, int w, int h)
        {
            var puff = Palette.Cloud;
            puff.a = 0.92f;
            c.FillCircleSoft(new Vector2(-80, 10), 46, puff, 12);
            c.FillCircleSoft(new Vector2(-30, 34), 58, puff, 14);
            c.FillCircleSoft(new Vector2(28, 12), 62, puff, 14);
            c.FillCircleSoft(new Vector2(82, 6), 44, puff, 12);
            c.FillCircleSoft(new Vector2(2, -6), 50, puff, 12);
        }

        void DrawHillsFar(Canvas c, int w, int h)
        {
            int n = 64;
            var poly = new Vector2[n + 3];
            poly[0] = new Vector2(-w / 2f, -h / 2f - 2);
            for (int i = 0; i <= n; i++)
            {
                float x = Mathf.Lerp(-w / 2f, w / 2f, i / (float)n);
                float roll = Mathf.Sin(i * 0.7f) * 0.45f + Mathf.Sin(i * 1.9f) * 0.25f;
                poly[i + 1] = new Vector2(x, -h * 0.10f + roll * h * 0.30f);
            }
            poly[n + 2] = new Vector2(w / 2f, -h / 2f - 2);
            c.FillPolygonGradient(poly, Palette.HillsFar, Palette.HillsFarDeep);
        }

        void DrawForestMid(Canvas c, int w, int h)
        {
            int n = 40;
            var poly = new Vector2[n + 3];
            poly[0] = new Vector2(-w / 2f, -h / 2f - 2);
            for (int i = 0; i <= n; i++)
            {
                float step = 2.25f;
                float x = Mathf.Lerp(-w / 2f, w / 2f, i / (float)n);
                float cy = -h * 0.16f;
                float xf = x / step;
                float bump = Mathf.Sin(xf) * Mathf.Sin(xf * 0.37f);
                poly[i + 1] = new Vector2(x, cy - Mathf.Abs(bump) * h * 0.34f);
            }
            poly[n + 2] = new Vector2(w / 2f, -h / 2f - 2);
            c.FillPolygonGradient(poly, Palette.ForestMid, Palette.ForestMidDeep);
        }

        void DrawGround(Canvas c, int w, int h)
        {
            // dirt base with two gentle mounds (bird stand left, tree right)
            int n = 64;
            var poly = new Vector2[n + 3];
            poly[0] = new Vector2(-w / 2f, -h / 2f - 2);
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float x = Mathf.Lerp(-w / 2f, w / 2f, t);
                float mound = Mathf.Sin(Mathf.PI * t);
                float ridge = -h * 0.24f - mound * h * 0.10f;
                poly[i + 1] = new Vector2(x, ridge);
            }
            poly[n + 2] = new Vector2(w / 2f, -h / 2f - 2);
            c.FillPolygonGradient(poly, Palette.GroundTop, Palette.GroundDark);

            // grass fringe spikes along the top ridge
            for (int x = -w / 2 + 8; x < w / 2 - 4; x += 26)
            {
                float t = Mathf.Clamp01((x + w / 2f) / (float)w);
                float mound = Mathf.Sin(Mathf.PI * t);
                float baseY = -h * 0.24f - mound * h * 0.10f;
                float lean = (x % 52 < 26) ? -6f : 6f;
                var spike = new[]
                {
                    new Vector2(x, baseY),
                    new Vector2(x + 12 + lean * 0.4f, baseY + 26),
                    new Vector2(x + 24, baseY + 4)
                };
                c.FillPolygon(spike, Palette.GroundTop);
            }
        }

        void DrawTree(Canvas c, int w, int h)
        {
            // --- trunk (bottom) ---
            float topY = -h * 0.30f, baseY = -h / 2f - 4;
            float topW = w * 0.14f, botW = w * 0.27f;
            var trunk = new[]
            {
                new Vector2(-topW / 2f, topY),
                new Vector2(-botW / 2f, baseY),
                new Vector2(botW / 2f, baseY),
                new Vector2(topW / 2f, topY)
            };
            c.FillPolygon(trunk, Palette.Trunk);
            c.StrokePolygon(trunk, Palette.TrunkDark, 8);

            // bark streak lines
            for (int i = 0; i < 7; i++)
            {
                float y = RandomIn(topY, baseY);
                float ww = TrunkWidthAt(y, topY, baseY, topW, botW);
                float x = RandomIn(-ww / 2f + 6, ww / 2f - 6);
                c.FillEllipse(new Vector2(x, y), 6, RandomIn(10f, 34f), Palette.Barkline, 0f);
            }

            // wood rings
            float ringY = topY - 8;
            for (int i = 0; i < 2; i++)
            {
                float ww = TrunkWidthAt(ringY, topY, baseY, topW, botW);
                c.FillRect(-ww / 2f + 6, ringY - 1, ww / 2f - 6, ringY + 1, Palette.Barkline);
                ringY -= 150;
            }

            // root flare
            c.FillEllipse(new Vector2(-botW * 0.36f, baseY + 6), botW * 0.34f, 34, Palette.Trunk);
            c.FillEllipse(new Vector2(botW * 0.36f, baseY + 6), botW * 0.34f, 34, Palette.Trunk);

            // pecking hole
            var hole = new Vector2(0, topY - 26);
            c.FillCircle(hole, 24, Palette.HoleDark);
            c.FillCircle(hole + new Vector2(-5, 4), 8, Palette.HoleDark);

            // --- canopy (top) ---
            Vector2 canopyC = new Vector2(0, h * 0.32f);
            float crx = w * 0.46f, cry = h * 0.30f;

            // mottled leaf fill
            int spots = 90;
            for (int i = 0; i < spots; i++)
            {
                float a = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float rr = Mathf.Sqrt((float)rng.NextDouble());
                float x = canopyC.x + Mathf.Cos(a) * crx * rr;
                float y = canopyC.y + Mathf.Sin(a) * cry * rr * 1.08f;
                float jx = RandomIn(-38, 38), jy = RandomIn(-38, 38);
                float r = RandomIn(16f, 44f);
                Color leaf = RandomFrom(new[] { Palette.LeafBase, Palette.LeafBase, Palette.LeafDark, Palette.LeafLight });
                c.FillCircle(new Vector2(x + jx, y + jy), r, leaf);
            }

            // big leaf clumps
            var clumps = new Vector2[] { canopyC, canopyC + new Vector2(-170, -40), canopyC + new Vector2(160, -60), canopyC + new Vector2(-60, 100), canopyC + new Vector2(70, 110) };
            for (int i = 0; i < clumps.Length; i++)
            {
                float rad = (i == 0) ? 250f : 150f;
                c.FillEllipse(clumps[i], rad * 0.85f, rad * 0.72f, Palette.LeafBase);
                c.FillEllipse(clumps[i] + new Vector2(0, rad * 0.55f), rad * 0.72f, rad * 0.42f, Palette.LeafLight);
            }

            // outline the canopy silhouette
            for (int i = 0; i < clumps.Length; i++)
            {
                float rad = (i == 0) ? 250f : 145f;
                c.StrokeEllipse(clumps[i], rad * 0.85f, rad * 0.72f, Palette.LeafOutline, 12);
            }

            // fruit beads scattered in canopy
            for (int i = 0; i < 10; i++)
            {
                Vector2 f = FruitSpot(canopyC, crx, cry);
                c.FillCircle(f, 36, Palette.Fruit);
                c.FillCircle(f + new Vector2(-10, 10), 12, Palette.FruitHighlight);
                c.FillCircle(f + new Vector2(-2, 40), 4, Palette.FruitStem);
            }
        }

        static float TrunkWidthAt(float y, float topY, float baseY, float topW, float botW)
        {
            float t = Mathf.InverseLerp(topY, baseY, y);
            return Mathf.Lerp(topW, botW, t);
        }

        Vector2 FruitSpot(Vector2 center, float crx, float cry)
        {
            float a = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float rr = 0.35f + (float)rng.NextDouble() * 0.55f;
            return center + new Vector2(Mathf.Cos(a) * crx * rr, Mathf.Sin(a) * cry * rr);
        }

        void DrawBird(Canvas c, int w, int h)
        {
            // tail
            var tail = new[]
            {
                new Vector2(-95, -58), new Vector2(-165, -108), new Vector2(-148, -128), new Vector2(-88, -88)
            };
            c.FillPolygon(tail, Palette.Tail);
            c.StrokePolygon(tail, Palette.BirdWingDark, 9);

            // body
            Vector2 bodyC = new Vector2(-6, 6);
            c.FillEllipse(bodyC, 152, 118, Palette.BirdBody, -6f);

            // belly
            c.FillEllipse(new Vector2(52, -52), 96, 74, Palette.Belly, -8f);
            c.StrokeEllipse(new Vector2(52, -52), 96, 74, Palette.BirdDark, 10, -8f);

            // wing
            var wingC = new Vector2(6, 18);
            c.FillEllipse(wingC, 104, 74, Palette.BirdWing, -18f);
            c.StrokeEllipse(wingC, 104, 74, Palette.BirdWingDark, 9, -18f);
            // wing feather scallops
            c.FillCircle(new Vector2(-46, -38), 12, Palette.Belly);
            c.FillCircle(new Vector2(-10, -54), 12, Palette.Belly);
            c.FillCircle(new Vector2(30, -58), 12, Palette.Belly);

            // head
            var headC = new Vector2(104, 92);
            c.FillCircle(headC, 86, Palette.BirdBody);

            // crest
            var crest = new[]
            {
                new Vector2(148, 138), new Vector2(196, 196), new Vector2(142, 208), new Vector2(112, 162)
            };
            c.FillPolygon(crest, Palette.Crest);
            c.StrokePolygon(crest, Palette.BirdDark, 9);

            // beak
            var beak = new[]
            {
                new Vector2(184, 100), new Vector2(262, 84), new Vector2(258, 116), new Vector2(186, 120)
            };
            c.FillPolygon(beak, Palette.Beak);
            c.StrokePolygon(beak, Palette.BeakTip, 7);
            var beakTip = new[]
            {
                new Vector2(252, 86), new Vector2(266, 84), new Vector2(262, 112), new Vector2(250, 112)
            };
            c.FillPolygon(beakTip, Palette.BeakTip);

            // eye
            c.FillCircle(new Vector2(132, 122), 28, Palette.Eye);
            c.FillCircle(new Vector2(138, 132), 9, Palette.EyeGlint);

            // cheek
            c.FillCircle(new Vector2(104, 66), 20, Palette.Cheek);

            // head outline
            c.StrokeCircle(headC, 86, Palette.BirdDark, 10);

            // feet
            Vector2 f1 = new Vector2(24, -116), f2 = new Vector2(76, -120);
            DrawFoot(c, f1);
            DrawFoot(c, f2);
        }

        void DrawFoot(Canvas c, Vector2 p)
        {
            c.FillEllipse(p, 10, 22, Palette.Foot, 8f);
            c.FillEllipse(p + new Vector2(-16, -6), 7, 10, Palette.Foot, -30f);
            c.FillEllipse(p + new Vector2(16, -8), 7, 10, Palette.Foot, 30f);
        }

        void DrawChip(Canvas c, int w, int h)
        {
            var p = new[]
            {
                new Vector2(-16, 10), new Vector2(-10, -12), new Vector2(6, -18), new Vector2(16, -4), new Vector2(8, 14)
            };
            var face = new[]
            {
                new Vector2(-10, 6), new Vector2(-4, -8), new Vector2(8, -12), new Vector2(12, -2), new Vector2(4, 8)
            };
            c.FillPolygon(p, Palette.Chip);
            c.StrokePolygon(p, Palette.ChipDark, 6);
            c.FillPolygon(face, Palette.ChipDark);
        }

        void DrawCoin(Canvas c, int w, int h)
        {
            c.FillCircle(Vector2.zero, 42, Palette.Coin);
            c.StrokeCircle(Vector2.zero, 42, Palette.CoinEdge, 7);
            c.StrokeCircle(Vector2.zero, 28, new Color(Palette.CoinDark.r, Palette.CoinDark.g, Palette.CoinDark.b, 0.6f), 5);
            c.FillEllipse(new Vector2(-8, 6), 24, 14, Palette.CoinGloss, -18f);
            c.FillCircle(Vector2.zero, 7, Palette.CoinEdge);
        }

        void DrawBtnRect(Canvas c, int w, int h)
        {
            c.FillRoundRectGradientVert(-w / 2f, -h / 2f, w / 2f, h / 2f, 42, Palette.BtnTop, Palette.BtnBottom);
            DrawBtnEdge(c, w, h, 42, Palette.BtnEdge);
            c.FillRoundRect(-w * 0.40f, h * 0.28f, w * 0.40f, h * 0.36f, 18, new Color(1f, 1f, 1f, 0.22f));
        }

        void DrawBtnGreenRect(Canvas c, int w, int h)
        {
            c.FillRoundRectGradientVert(-w / 2f, -h / 2f, w / 2f, h / 2f, 42, Palette.BtnGreen, Palette.BtnGreenEdge);
            DrawBtnEdge(c, w, h, 42, Palette.BtnGreenEdge);
            c.FillRoundRect(-w * 0.40f, h * 0.28f, w * 0.40f, h * 0.36f, 18, new Color(1f, 1f, 1f, 0.22f));
        }

        static void DrawBtnEdge(Canvas c, int w, int h, float radius, Color edge)
        {
            c.StrokePolygon(new[]
            {
                new Vector2(-w / 2f + radius, -h / 2f + 6), new Vector2(w / 2f - radius, -h / 2f + 6),
                new Vector2(w / 2f - 6, -h / 2f + radius), new Vector2(w / 2f - 6, h / 2f - radius),
                new Vector2(w / 2f - radius, h / 2f - 6), new Vector2(-w / 2f + radius, h / 2f - 6),
                new Vector2(-w / 2f + 6, h / 2f - radius), new Vector2(-w / 2f + 6, -h / 2f + radius)
            }, edge, 8);
        }

        void DrawPanelRect(Canvas c, int w, int h)
        {
            c.FillRoundRectGradientVert(-w / 2f, -h / 2f, w / 2f, h / 2f, 46, Palette.UiPanel, new Color(Palette.UiPanel.r * 0.96f, Palette.UiPanel.g * 0.96f, Palette.UiPanel.b * 0.96f, 0.98f));
            DrawBtnEdge(c, w, h, 46, Palette.UiBrownSoft);
        }

        void DrawSpeaker(Canvas c, bool on)
        {
            // circle backplate
            c.FillCircle(Vector2.zero, 44, Palette.BtnGreen);
            c.StrokeCircle(Vector2.zero, 44, Palette.BtnGreenEdge, 6);
            // speaker body
            var body = new[]
            {
                new Vector2(-18, -14), new Vector2(2, -14), new Vector2(16, -27), new Vector2(16, 27), new Vector2(2, 14), new Vector2(-18, 14)
            };
            c.FillPolygon(body, Palette.UiCream);
            // sound waves
            var glow = Palette.UiCream; glow.a = 0.7f;
            c.StrokeCircle(new Vector2(24, 0), 16, glow, 4);
            c.StrokeCircle(new Vector2(33, 0), 28, glow, 4);
            c.StrokeCircle(new Vector2(8, 0), 26, glow, 3);
            if (!on)
            {
                var x = Palette.BtnGreenEdge;
                c.FillRect(-2, -32, 4, 32, new Color(0.9f, 0.35f, 0.3f, 0.9f));
            }
        }

        // ---- utility --------------------------------------------------------

        float RandomIn(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        T RandomFrom<T>(T[] arr) => arr[rng.Next(arr.Length)];
    }
}