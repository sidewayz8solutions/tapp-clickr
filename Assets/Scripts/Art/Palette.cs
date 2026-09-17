using UnityEngine;

namespace TappBird
{
    /// <summary>Warm pastel cartoon palette for the whole game.</summary>
    public static class Palette
    {
        // sky & environment
        public static readonly Color SkyTop = Hex("7EC8FF");
        public static readonly Color SkyMid = Hex("A9DEFF");
        public static readonly Color SkyHorizon = Hex("FFE7B6");
        public static readonly Color SunGlow = Hex("FFF3C4");
        public static readonly Color SunDisc = Hex("FFF8D6");
        public static readonly Color Cloud = Hex("FFFFFF");
        public static readonly Color HillsFar = Hex("BFE3C8");
        public static readonly Color HillsFarDeep = Hex("9AD3AE");
        public static readonly Color ForestMid = Hex("69B98A");
        public static readonly Color ForestMidDeep = Hex("4E9E72");
        public static readonly Color GroundTop = Hex("8FDC9C");
        public static readonly Color GroundBase = Hex("54B272");
        public static readonly Color GroundDark = Hex("3F9A5F");
        public static readonly Color Dirt = Hex("9A6234");

        // tree
        public static readonly Color Trunk = Hex("9A6234");
        public static readonly Color TrunkDark = Hex("70451F");
        public static readonly Color TrunkLight = Hex("B97B45");
        public static readonly Color Barkline = Hex("7C4E26");
        public static readonly Color LeafBase = Hex("57B36F");
        public static readonly Color LeafDark = Hex("3C8F57");
        public static readonly Color LeafLight = Hex("74CD85");
        public static readonly Color LeafOutline = Hex("2C6B43");
        public static readonly Color Fruit = Hex("FF7E6E");
        public static readonly Color FruitHighlight = Hex("FFB4A4");
        public static readonly Color FruitStem = Hex("7A4A26");
        public static readonly Color AppleLeaf = Hex("4FA85F");
        public static readonly Color HoleDark = Hex("3A2416");

        // bird
        public static readonly Color BirdBody = Hex("FF6B62");
        public static readonly Color BirdDark = Hex("D84A41");
        public static readonly Color BirdWing = Hex("C93F36");
        public static readonly Color BirdWingDark = Hex("A52E27");
        public static readonly Color Belly = Hex("FFF3D8");
        public static readonly Color Crest = Hex("E8453A");
        public static readonly Color Beak = Hex("FFB545");
        public static readonly Color BeakTip = Hex("FF8C2B");
        public static readonly Color Eye = Hex("2B2233");
        public static readonly Color EyeGlint = Hex("FFFFFF");
        public static readonly Color Cheek = Hex("FFB9A8");
        public static readonly Color Tail = Hex("5C2E3A");
        public static readonly Color Foot = Hex("FF9A3C");

        // coins / rewards
        public static readonly Color Coin = Hex("FFCB45");
        public static readonly Color CoinDark = Hex("E8A51B");
        public static readonly Color CoinEdge = Hex("B97E12");
        public static readonly Color CoinGloss = Hex("FFF2C0");

        // wood chips
        public static readonly Color Chip = Hex("B97B45");
        public static readonly Color ChipDark = Hex("7C4E26");

        // UI
        public static readonly Color UiCream = Hex("FFF7E3");
        public static readonly Color UiPanel = Hex("FFEBB8");
        public static readonly Color UiBrown = Hex("6B4226");
        public static readonly Color UiBrownSoft = Hex("8A5A33");
        public static readonly Color BtnTop = Hex("FFD98C");
        public static readonly Color BtnBottom = Hex("F5B14D");
        public static readonly Color BtnEdge = Hex("C5701F");
        public static readonly Color BtnGreen = Hex("5CB875");
        public static readonly Color BtnGreenEdge = Hex("31723F");
        public static readonly Color PriceGold = Hex("A86A1F");

        static Color Hex(string h)
        {
            int r = System.Convert.ToInt32(h.Substring(0, 2), 16);
            int g = System.Convert.ToInt32(h.Substring(2, 2), 16);
            int b = System.Convert.ToInt32(h.Substring(4, 2), 16);
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        public static Color Darken(Color c, float amt) =>
            new Color(c.r * (1f - amt), c.g * (1f - amt), c.b * (1f - amt), c.a);
    }
}