using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceCleaner.Core;
using SpaceCleaner.Player;

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

        [Header("Level Complete")]
        [SerializeField] private GameObject levelCompletePanel;

        private PlayerController player;

        private void Start()
        {
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);

            player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                player.OnAmmoChanged += UpdateAmmoDisplay;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCleanupChanged += UpdateCleanupDisplay;
                GameManager.Instance.OnLevelComplete += ShowLevelComplete;
            }

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

        private void ShowLevelComplete()
        {
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(true);
        }

        private void OnDestroy()
        {
            if (player != null)
                player.OnAmmoChanged -= UpdateAmmoDisplay;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCleanupChanged -= UpdateCleanupDisplay;
                GameManager.Instance.OnLevelComplete -= ShowLevelComplete;
            }
        }
    }
}
