using UnityEngine;

namespace SpaceCleaner.Progression
{
    /// <summary>
    /// Tracks the player's currency. Backed by SaveData; changes auto-save.
    /// Singleton that lives across scenes.
    /// </summary>
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        public int Currency { get; private set; }

        /// <summary>Fired whenever the balance changes. Arg: new balance.</summary>
        public event System.Action<int> OnCurrencyChanged;

        private SaveData saveData;

        /// <summary>Initializes from loaded save data. Called by ProgressionManager on startup.</summary>
        public void Initialize(SaveData data)
        {
            saveData = data;
            Currency = data.currency;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Currency += amount;
            Persist();
            OnCurrencyChanged?.Invoke(Currency);
        }

        /// <summary>Attempts to spend. Returns false if insufficient funds.</summary>
        public bool TrySpend(int amount)
        {
            if (amount < 0 || Currency < amount) return false;
            Currency -= amount;
            Persist();
            OnCurrencyChanged?.Invoke(Currency);
            return true;
        }

        private void Persist()
        {
            if (saveData == null) return;
            saveData.currency = Currency;
            SaveSystem.Save(saveData);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
