using System;
using UnityEngine;

namespace TappBird
{
    [Serializable]
    public class SaveData
    {
        public int coins;
        public int treeLevel;
        public int peckLevel, autoLevel, boostLevel;
        public int totalTrees, totalPecks;
        public bool soundOn = true;
    }

    /// <summary>Holds the persistent player state; single instance created by GameBootstrap.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Inst { get; private set; }

        public SaveData Data { get; private set; }

        public event Action OnCoinsChanged;
        public event Action<int> OnTreeCleared;  // trees cleared so far (0-indexed)
        public event Action<GameCfg.UpgradeType, int> OnUpgradeBought; // type, new level

        public int TreeIndex => Data.treeLevel;
        public bool SoundOn { get => Data.soundOn; }

        void Awake()
        {
            if (Inst != null && Inst != this) { Destroy(gameObject); return; }
            Inst = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void AddCoins(int n)
        {
            Data.coins += n;
            Save();
            OnCoinsChanged?.Invoke();
        }

        public bool TrySpend(int cost)
        {
            if (Data.coins < cost) return false;
            Data.coins -= cost;
            Save();
            OnCoinsChanged?.Invoke();
            return true;
        }

        public void RecordPeck()
        {
            Data.totalPecks++;
        }

        public void ClearTree()
        {
            Data.totalTrees++;
            Data.treeLevel++;
            Save();
            OnTreeCleared?.Invoke(Data.treeLevel);
        }

        public bool BuyUpgrade(GameCfg.UpgradeType type)
        {
            int lvl = GetLevel(type);
            if (lvl >= GameCfg.MaxUpgradeLevel) return false;
            int cost = GameCfg.UpgradeCost(type, lvl);
            if (!TrySpend(cost)) return false;
            SetLevel(type, lvl + 1);
            Save();
            OnUpgradeBought?.Invoke(type, lvl + 1);
            return true;
        }

        public int GetLevel(GameCfg.UpgradeType t) =>
            t == GameCfg.UpgradeType.Peck ? Data.peckLevel :
            t == GameCfg.UpgradeType.Auto ? Data.autoLevel : Data.boostLevel;

        public void SetLevel(GameCfg.UpgradeType t, int v)
        {
            if (t == GameCfg.UpgradeType.Peck) Data.peckLevel = v;
            else if (t == GameCfg.UpgradeType.Auto) Data.autoLevel = v;
            else Data.boostLevel = v;
        }

        public float CurrentPeckPower => GameCfg.PeckPower(Data.peckLevel);
        public float CurrentCoinBoost => GameCfg.CoinBoost(Data.boostLevel);
        public float CurrentAutoInterval => GameCfg.AutoPeckInterval(Data.autoLevel);

        public void ToggleSound()
        {
            Data.soundOn = !Data.soundOn;
            Sfx.SetMuted(!Data.soundOn);
            Save();
        }

        public void ClearSave()
        {
            PlayerPrefs.DeleteKey("tapp_save");
            Load();
        }

        void Load()
        {
            string json = PlayerPrefs.GetString("tapp_save", "");
            if (string.IsNullOrEmpty(json))
            {
                Data = new SaveData();
                return;
            }
            try { Data = JsonUtility.FromJson<SaveData>(json); }
            catch { Data = new SaveData(); }
        }

        void Save()
        {
            PlayerPrefs.SetString("tapp_save", JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }
    }
}