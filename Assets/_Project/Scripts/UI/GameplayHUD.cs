using System.Collections;
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

        [Header("Player Health")]
        [SerializeField] private Slider playerHealthBar;
        [SerializeField] private TextMeshProUGUI playerHealthText;

        [Header("Opponent")]
        [SerializeField] private GameObject opponentDefeatedPanel;

        [Header("Level Complete")]
        [SerializeField] private GameObject levelCompletePanel;

        [Header("Touch Controls")]
        [Tooltip("Virtual joystick for movement (bottom-left). Created automatically if null.")]
        [SerializeField] private VirtualJoystick moveJoystick;
        [Tooltip("Fire/aim button (bottom-right). Created automatically if null.")]
        [SerializeField] private FireButton fireButton;
        [Tooltip("If true, touch controls are created on Start when running on a touch device or in the Editor.")]
        [SerializeField] private bool autoCreateTouchControls = true;

        private PlayerController player;
        private Health playerHealth;

        private void Start()
        {
            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);

            if (opponentDefeatedPanel != null)
                opponentDefeatedPanel.SetActive(false);

            player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.OnAmmoChanged += UpdateAmmoDisplay;

                // Subscribe to player health
                playerHealth = player.GetComponent<Health>();
                if (playerHealth != null)
                {
                    playerHealth.OnHealthChanged += UpdatePlayerHealth;
                }
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCleanupChanged += UpdateCleanupDisplay;
                GameManager.Instance.OnLevelComplete += ShowLevelComplete;
                GameManager.Instance.OnOpponentStateChanged += HandleOpponentStateChanged;
            }

            // Build health bar programmatically if not assigned
            if (playerHealthBar == null)
                CreatePlayerHealthBar();

            // Initialize displays
            UpdateAmmoDisplay(player != null ? player.AmmoCount : 0);
            UpdateCleanupDisplay(0f);

            if (playerHealth != null)
                UpdatePlayerHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            else
                UpdatePlayerHealth(0, 1);

            // Create touch controls if needed
            if (autoCreateTouchControls && ShouldShowTouchControls())
            {
                SetupTouchControls();
            }
        }

        /// <summary>
        /// Determines whether touch controls should be shown.
        /// Returns true on mobile platforms, or in the Editor for testing.
        /// </summary>
        private bool ShouldShowTouchControls()
        {
#if UNITY_EDITOR
            // Always show in editor for testing; disable via the inspector toggle if unwanted
            return true;
#elif UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return UnityEngine.Input.touchSupported;
#endif
        }

        /// <summary>
        /// Creates the move joystick and fire button if they are not already assigned.
        /// Attaches them to the same Canvas as this HUD.
        /// </summary>
        private void SetupTouchControls()
        {
            var canvasRT = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
            if (canvasRT == null)
            {
                Debug.LogWarning("[GameplayHUD] No parent Canvas found — cannot create touch controls.");
                return;
            }

            // Use this transform as parent so controls are grouped under the HUD
            var parentRT = GetComponent<RectTransform>() ?? canvasRT;

            if (moveJoystick == null)
            {
                // Bottom-left, offset inward from the corner
                moveJoystick = VirtualJoystick.Create(
                    parentRT,
                    "<Gamepad>/leftStick",
                    new Vector2(100f, 100f)
                );
                Debug.Log("[GameplayHUD] Created MoveJoystick (touch control).");
            }

            if (fireButton == null)
            {
                // Bottom-right, offset inward from the corner
                fireButton = FireButton.Create(
                    parentRT,
                    "<Gamepad>/rightStick",
                    new Vector2(-100f, 100f)
                );
                Debug.Log("[GameplayHUD] Created AimFireButton (touch control).");
            }
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

        private void UpdatePlayerHealth(int current, int max)
        {
            if (playerHealthBar != null)
                playerHealthBar.value = max > 0 ? (float)current / max : 0f;

            if (playerHealthText != null)
                playerHealthText.text = $"HP: {current}/{max}";
        }

        private void HandleOpponentStateChanged(bool alive)
        {
            if (!alive)
                StartCoroutine(ShowOpponentDefeated());
        }

        private IEnumerator ShowOpponentDefeated()
        {
            if (opponentDefeatedPanel == null)
                CreateOpponentDefeatedPanel();

            if (opponentDefeatedPanel != null)
            {
                opponentDefeatedPanel.SetActive(true);
                yield return new WaitForSeconds(3f);
                opponentDefeatedPanel.SetActive(false);
            }
        }

        private void CreatePlayerHealthBar()
        {
            var canvasRT = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
            if (canvasRT == null) return;

            var parentRT = GetComponent<RectTransform>() ?? canvasRT;

            // Root object for the health bar
            var barGO = new GameObject("PlayerHealthBar");
            var barRT = barGO.AddComponent<RectTransform>();
            barRT.SetParent(parentRT, false);
            barRT.anchorMin = new Vector2(0f, 1f);
            barRT.anchorMax = new Vector2(0f, 1f);
            barRT.pivot = new Vector2(0f, 1f);
            barRT.anchoredPosition = new Vector2(10f, -10f);
            barRT.sizeDelta = new Vector2(200f, 20f);

            // Slider component
            playerHealthBar = barGO.AddComponent<Slider>();
            playerHealthBar.minValue = 0f;
            playerHealthBar.maxValue = 1f;
            playerHealthBar.wholeNumbers = false;
            playerHealthBar.interactable = false;

            // Background
            var bgGO = new GameObject("Background");
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.SetParent(barRT, false);
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Fill area
            var fillAreaGO = new GameObject("Fill Area");
            var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRT.SetParent(barRT, false);
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.sizeDelta = Vector2.zero;

            // Fill
            var fillGO = new GameObject("Fill");
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.SetParent(fillAreaRT, false);
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.sizeDelta = Vector2.zero;
            var fillImage = fillGO.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.9f, 0.2f, 1f); // Green

            playerHealthBar.fillRect = fillRT;
            playerHealthBar.targetGraphic = fillImage;

            // Health text
            var textGO = new GameObject("PlayerHealthText");
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.SetParent(parentRT, false);
            textRT.anchorMin = new Vector2(0f, 1f);
            textRT.anchorMax = new Vector2(0f, 1f);
            textRT.pivot = new Vector2(0f, 1f);
            textRT.anchoredPosition = new Vector2(215f, -10f);
            textRT.sizeDelta = new Vector2(120f, 20f);

            playerHealthText = textGO.AddComponent<TextMeshProUGUI>();
            playerHealthText.fontSize = 14f;
            playerHealthText.color = Color.white;
            playerHealthText.alignment = TextAlignmentOptions.MidlineLeft;
            playerHealthText.text = "HP: 0/0";
        }

        private void CreateOpponentDefeatedPanel()
        {
            var canvasRT = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
            if (canvasRT == null) return;

            var parentRT = GetComponent<RectTransform>() ?? canvasRT;

            opponentDefeatedPanel = new GameObject("OpponentDefeatedPanel");
            var panelRT = opponentDefeatedPanel.AddComponent<RectTransform>();
            panelRT.SetParent(parentRT, false);
            panelRT.anchorMin = new Vector2(0.5f, 0.6f);
            panelRT.anchorMax = new Vector2(0.5f, 0.6f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(400f, 60f);

            var bg = opponentDefeatedPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.7f);

            var textGO = new GameObject("Text");
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.SetParent(panelRT, false);
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "OPPONENT DEFEATED!";
            tmp.fontSize = 28f;
            tmp.color = new Color(1f, 0.85f, 0.2f, 1f); // Gold
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            opponentDefeatedPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (player != null)
                player.OnAmmoChanged -= UpdateAmmoDisplay;

            if (playerHealth != null)
                playerHealth.OnHealthChanged -= UpdatePlayerHealth;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCleanupChanged -= UpdateCleanupDisplay;
                GameManager.Instance.OnLevelComplete -= ShowLevelComplete;
                GameManager.Instance.OnOpponentStateChanged -= HandleOpponentStateChanged;
            }
        }
    }
}
