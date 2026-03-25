using UnityEngine;

namespace SpaceCleaner.Core
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 20;
        private int currentHealth;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public float HealthNormalized => (float)currentHealth / maxHealth;
        public bool IsDead => currentHealth <= 0;

        public event System.Action<int, int> OnHealthChanged; // current, max
        public event System.Action OnDeath;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead) return;
            currentHealth = Mathf.Max(0, currentHealth - amount);
            if (gameObject.layer == 6) // Player layer
                SFXManager.Instance?.Play(SFXType.PlayerDamage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (IsDead)
                OnDeath?.Invoke();
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Resets health to max after death. Bypasses the IsDead guard that blocks Heal().
        /// </summary>
        public void Revive()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
