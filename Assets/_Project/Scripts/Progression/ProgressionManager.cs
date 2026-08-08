using UnityEngine;
using SpaceCleaner.Core;

namespace SpaceCleaner.Progression
{
    /// <summary>
    /// Owns the run's progression state: current solar system / planet, save data,
    /// and the citizen reward calculation at level completion.
    /// Created automatically on startup; persists across scenes.
    /// </summary>
    public class ProgressionManager : MonoBehaviour
    {
        public static ProgressionManager Instance { get; private set; }

        [Header("Citizen Reward Tuning")]
        [Tooltip("Base currency for cleaning a planet, before bonuses.")]
        [SerializeField] private int baseReward = 100;
        [Tooltip("Currency per point of best combo reached during the level.")]
        [SerializeField] private int perComboPoint = 10;
        [Tooltip("Currency per percent of cleanup achieved.")]
        [SerializeField] private int perCleanupPercent = 2;

        public SaveData Data { get; private set; }

        /// <summary>Fired when a citizen reward is granted. Arg: currency amount.</summary>
        public event System.Action<int> OnCitizenReward;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("ProgressionManager");
            go.AddComponent<ProgressionManager>();
            // CurrencyManager rides on the same object
            if (go.GetComponent<CurrencyManager>() == null)
                go.AddComponent<CurrencyManager>();
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

            Data = SaveSystem.Load();
            CurrencyManager.Instance?.Initialize(Data);
        }

        private bool gameManagerHooked;

        private void OnEnable()
        {
            TryHookGameManager();
        }

        private void Start()
        {
            // Scene objects (GameManager) run Awake before our Start, so this is the
            // reliable hookup point. Update() below covers later scene loads.
            TryHookGameManager();
        }

        private void Update()
        {
            // GameManager is scene-scoped and may be created after us on a new level;
            // keep trying until hooked, then stop checking.
            if (!gameManagerHooked)
                TryHookGameManager();
        }

        private void TryHookGameManager()
        {
            if (GameManager.Instance != null && !gameManagerHooked)
            {
                GameManager.Instance.OnLevelComplete += HandleLevelComplete;
                gameManagerHooked = true;
            }
        }

        /// <summary>Citizen reward: base + cleanup% + combo bonus (GDD §3.3).</summary>
        public int CalculateCitizenReward(float cleanupPercent, int bestCombo)
        {
            int cleanupBonus = Mathf.RoundToInt(cleanupPercent * 100f * perCleanupPercent);
            int comboBonus = bestCombo * perComboPoint;
            return baseReward + cleanupBonus + comboBonus;
        }

        private void HandleLevelComplete()
        {
            int bestCombo = ComboManager.Instance != null ? ComboManager.Instance.BestCombo : 0;
            float cleanup = GameManager.Instance != null ? GameManager.Instance.CleanupPercentage : 0f;

            int reward = CalculateCitizenReward(cleanup, bestCombo);

            // Record stats
            Data.totalTrashCollected += GameManager.Instance != null ? GameManager.Instance.CollectedTrashCount : 0;
            if (bestCombo > Data.bestCombo)
                Data.bestCombo = bestCombo;

            CurrencyManager.Instance?.Add(reward);
            OnCitizenReward?.Invoke(reward);

            // Auto-save on planet completion (GDD §10.1)
            SaveSystem.Save(Data);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Progression] Citizen reward: {reward} (cleanup {cleanup:P0}, bestCombo {bestCombo})");
#endif
        }

        /// <summary>Records a defeated opponent for boss carry-over and saves.</summary>
        public void RecordOpponentDefeated(string name, int ammo)
        {
            Data.carryOver.Add(new SaveData.CarryOverEntry(name, ammo));
            SaveSystem.Save(Data);
        }

        /// <summary>Called on boss defeat: clears carry-over, bumps boss count, saves.</summary>
        public void RecordBossDefeated()
        {
            Data.totalBossesDefeated++;
            Data.carryOver.Clear();
            SaveSystem.Save(Data);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null && gameManagerHooked)
                GameManager.Instance.OnLevelComplete -= HandleLevelComplete;
            if (Instance == this)
                Instance = null;
        }
    }
}
