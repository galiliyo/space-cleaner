using UnityEngine;

namespace SpaceCleaner.Core
{
    /// <summary>
    /// Combo system per GDD §2.4:
    /// - Collecting trash within 2s of the previous pickup increases the combo counter
    /// - Going 2+ seconds without collecting resets the combo
    /// - Combo count drives the multiplier shown on-screen (x2, x5, x10...)
    /// - Best combo is tracked for end-of-level score/currency bonuses
    /// </summary>
    public class ComboManager : MonoBehaviour
    {
        public static ComboManager Instance { get; private set; }

        [Header("Timing")]
        [Tooltip("Seconds allowed between pickups before the combo resets (GDD: 2s).")]
        [SerializeField] private float comboWindow = 2f;

        [Header("Multiplier Tiers")]
        [Tooltip("Combo counts at which the displayed multiplier escalates.")]
        [SerializeField] private int[] multiplierTiers = { 3, 6, 10, 15, 20 };
        [SerializeField] private int[] multiplierValues = { 2, 3, 5, 8, 10 };

        public int ComboCount { get; private set; }
        public int BestCombo { get; private set; }
        public int Multiplier { get; private set; } = 1;
        public float TimeRemaining { get; private set; }
        public bool IsActive => ComboCount > 0;

        /// <summary>Fired when combo increments. Args: (comboCount, multiplier).</summary>
        public event System.Action<int, int> OnComboIncreased;
        /// <summary>Fired when the combo timer runs out. Args: final combo count.</summary>
        public event System.Action<int> OnComboLost;
        /// <summary>Fired every frame while a combo is active. Arg: normalized time remaining (0-1).</summary>
        public event System.Action<float> OnComboTimerChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>Called when the player collects a trash item.</summary>
        public void RegisterPickup()
        {
            ComboCount++;
            TimeRemaining = comboWindow;

            int newMultiplier = EvaluateMultiplier(ComboCount);
            bool tierUp = newMultiplier != Multiplier;
            Multiplier = newMultiplier;

            if (ComboCount > BestCombo)
                BestCombo = ComboCount;

            OnComboIncreased?.Invoke(ComboCount, Multiplier);

            if (tierUp)
                SFXManager.Instance?.Play(SFXType.ComboUp);
        }

        private void Update()
        {
            if (!IsActive) return;

            TimeRemaining -= Time.deltaTime;
            OnComboTimerChanged?.Invoke(Mathf.Clamp01(TimeRemaining / comboWindow));

            if (TimeRemaining <= 0f)
            {
                int finalCount = ComboCount;
                ComboCount = 0;
                Multiplier = 1;
                TimeRemaining = 0f;
                OnComboLost?.Invoke(finalCount);
            }
        }

        private int EvaluateMultiplier(int combo)
        {
            int result = 1;
            for (int i = 0; i < multiplierTiers.Length && i < multiplierValues.Length; i++)
            {
                if (combo >= multiplierTiers[i])
                    result = multiplierValues[i];
            }
            return result;
        }

        /// <summary>Resets combo state. Called on level init / player death.</summary>
        public void ResetCombo()
        {
            int finalCount = ComboCount;
            ComboCount = 0;
            Multiplier = 1;
            TimeRemaining = 0f;
            if (finalCount > 0)
                OnComboLost?.Invoke(finalCount);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
