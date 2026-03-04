using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceCleaner.Core;
using SpaceCleaner.Player;
using SpaceCleaner.Enemies;

namespace SpaceCleaner.UI
{
    public class GameplayHUD : MonoBehaviour
    {
        [Header("Ammo")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private Image ammoOverflowIndicator;

        [Header("Cleanup")]
        [SerializeField] private Slider cleanupBar;
        [SerializeField] private TextMeshProUGUI cleanupPercentText;

        [Header("Opponent")]
        [SerializeField] private Slider opponentHealthBar;
        [SerializeField] private GameObject opponentHealthPanel;

        [Header("Level Complete")]
        [SerializeField] private GameObject levelCompletePanel;

        private PlayerController player;
        private Health opponentHealth;

        private void Start()
        {
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);

            // Find references
            player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                player.OnAmmoChanged += UpdateAmmoDisplay;

            var opponent = FindAnyObjectByType<AIOpponent>();
            if (opponent != null)
            {
                opponentHealth = opponent.GetComponent<Health>();
                if (opponentHealth != null)
                    opponentHealth.OnHealthChanged += UpdateOpponentHealth;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCleanupChanged += UpdateCleanupDisplay;
                GameManager.Instance.OnLevelComplete += ShowLevelComplete;
            }

            // Initialize displays
            UpdateAmmoDisplay(player != null ? player.AmmoCount : 0);
            UpdateCleanupDisplay(0f);
        }

        private void UpdateAmmoDisplay(int ammo)
        {
            if (ammoText != null)
                ammoText.text = ammo.ToString();

            if (ammoOverflowIndicator != null && player != null)
                ammoOverflowIndicator.enabled = ammo > player.SoftCap;
        }

        private void UpdateCleanupDisplay(float percentage)
        {
            if (cleanupBar != null)
                cleanupBar.value = percentage;

            if (cleanupPercentText != null)
                cleanupPercentText.text = $"{Mathf.RoundToInt(percentage * 100)}%";
        }

        private void UpdateOpponentHealth(int current, int max)
        {
            if (opponentHealthBar != null)
                opponentHealthBar.value = (float)current / max;

            if (opponentHealthPanel != null && current <= 0)
                opponentHealthPanel.SetActive(false);
        }

        private void ShowLevelComplete()
        {
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(true);
        }

        private void OnDestroy()
        {
            if (player != null)
                player.OnAmmoChanged -= UpdateAmmoDisplay;

            if (opponentHealth != null)
                opponentHealth.OnHealthChanged -= UpdateOpponentHealth;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCleanupChanged -= UpdateCleanupDisplay;
                GameManager.Instance.OnLevelComplete -= ShowLevelComplete;
            }
        }
    }
}
