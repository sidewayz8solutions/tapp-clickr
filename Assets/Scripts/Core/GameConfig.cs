using UnityEngine;

namespace TappBird
{
    /// <summary>All balance values and layout constants.</summary>
    public static class GameCfg
    {
        public const float WorldWidth = 8.2f;          // design width in world units (portrait anchored)
        public const int DesignHeight = 1920;
        public const int DesignWidth = 1080;

        // layout
        public const float GroundLine = -3.1f;             // apparent surface the tree/bird stand on
        public static readonly Vector2 TreePos = new Vector2(2.3f, 0f);   // x is final; y filled at runtime (centered over base)
        public static readonly Vector2 BirdPos = new Vector2(-2.6f, -2.55f);
        public const float TreeWorldWidth = 2.6f;          // desired on-screen width of the tree sprite
        public const float BirdWorldWidth = 1.5f;          // desired on-screen width of the bird sprite
        public static float PeckHoleOffsetY => -1.9f;      // world-units below tree center where the peck hole sits

        // progression
        public static float TreeHealth(int n) => 10 + 8 * n;
        public static int CoinsForTree(int n) => 4 + 3 * n;
        public const float TreePopDuration = 0.45f;

        // upgrades
        public enum UpgradeType { Peck, Auto, Boost }
        public const int MaxUpgradeLevel = 30;

        public static float PeckPower(int level) => 1 + level;
        public static float AutoPeckInterval(int level) => Mathf.Max(0.45f, 1.6f - level * 0.13f);
        public static float CoinBoost(int level) => 1 + level * 0.5f;

        public static int UpgradeCost(UpgradeType t, int level)
        {
            float baseCost = t == UpgradeType.Peck ? 25f : (t == UpgradeType.Auto ? 60f : 40f);
            return Mathf.CeilToInt(baseCost * Mathf.Pow(1.6f, level));
        }

        public static string UpgradeName(UpgradeType t) =>
            t == UpgradeType.Peck ? "Sharp Beak" : (t == UpgradeType.Auto ? "Auto-Peck" : "Coin Magnet");

        public static string UpgradeDescription(UpgradeType t)
        {
            switch (t)
            {
                case UpgradeType.Peck: return "Peck harder. +1 damage per level.";
                case UpgradeType.Auto: return "Peck by yourself. Faster every level.";
                default: return "Find extra coins on every tree.";
            }
        }
    }
}