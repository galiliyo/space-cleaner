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
        private ShootingSystem shootingSystem;
        private Image burstCooldownImage;
        private static Shader s_ParticleShader;

        private void Start()
        {
            // Auto-find child UI elements if not serialized
            if (ammoText == null)
                ammoText = transform.Find("AmmoText")?.GetComponent<TextMeshProUGUI>();
            if (cleanupPercentText == null)
                cleanupPercentText = transform.Find("CleanupPercentText")?.GetComponent<TextMeshProUGUI>();
            if (cleanupBar == null)
                cleanupBar = transform.Find("CleanupBar")?.GetComponent<Slider>();

            // Auto-wire Slider fillRect if missing
            if (cleanupBar != null && cleanupBar.fillRect == null)
            {
                var fill = cleanupBar.transform.Find("Fill Area/Fill");
                if (fill != null)
                    cleanupBar.fillRect = fill.GetComponent<RectTransform>();
            }

            if (levelCompletePanel != null)
                levelCompletePanel.SetActive(false);

            if (opponentDefeatedPanel != null)
                opponentDefeatedPanel.SetActive(false);

            player = FindAnyObjectByType<PlayerController>();
            shootingSystem = player != null ? player.GetComponent<ShootingSystem>() : null;
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

            // Create burst cooldown indicator
            CreateBurstCooldownIndicator();

            // Apply safe area for notched devices
            if (GetComponent<SafeAreaHandler>() == null)
                gameObject.AddComponent<SafeAreaHandler>();
        }

        private void Update()
        {
            // Update burst cooldown indicator
            if (burstCooldownImage != null && shootingSystem != null)
            {
                if (shootingSystem.BurstOnCooldown)
                {
                    burstCooldownImage.enabled = true;
                    burstCooldownImage.fillAmount = shootingSystem.BurstCooldownNormalized;
                }
                else
                {
                    burstCooldownImage.enabled = false;
                }
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[GameplayHUD] No parent Canvas found — cannot create touch controls.");
#endif
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[GameplayHUD] Created MoveJoystick (touch control).");
#endif
            }

            if (fireButton == null)
            {
                // Bottom-right, offset inward from the corner
                fireButton = FireButton.Create(
                    parentRT,
                    "<Gamepad>/rightStick",
                    new Vector2(-100f, 100f)
                );
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[GameplayHUD] Created AimFireButton (touch control).");
#endif
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
            StartCoroutine(LevelCompleteCelebration());
        }

        private IEnumerator LevelCompleteCelebration()
        {
            // 1. Spawn confetti particle effect
            SpawnConfettiEffect();

            // 2. Create or show the level complete panel with animated text
            if (levelCompletePanel == null)
                CreateLevelCompletePanel();

            // Grab references to the text elements before showing
            var mainText = levelCompletePanel.transform.Find("MainText")?.GetComponent<TextMeshProUGUI>();
            var subText = levelCompletePanel.transform.Find("SubText")?.GetComponent<TextMeshProUGUI>();
            var continueText = levelCompletePanel.transform.Find("ContinueText")?.GetComponent<TextMeshProUGUI>();

            // Hide sub-elements initially
            if (subText != null) subText.gameObject.SetActive(false);
            if (continueText != null) continueText.gameObject.SetActive(false);

            levelCompletePanel.SetActive(true);

            // Animate panel scale from 0 to 1 with ease-out bounce
            var panelRT = levelCompletePanel.GetComponent<RectTransform>();
            float duration = 0.5f;
            float elapsed = 0f;
            panelRT.localScale = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease-out back for a bouncy pop-in
                float scale = 1f + 0.15f * Mathf.Sin(t * Mathf.PI);
                if (t >= 1f) scale = 1f;
                panelRT.localScale = Vector3.one * scale;
                yield return null;
            }
            panelRT.localScale = Vector3.one;

            // 3. Show sub-text after a short delay
            yield return new WaitForSeconds(0.5f);
            if (subText != null) subText.gameObject.SetActive(true);

            // 4. After 2 seconds, show continue text
            yield return new WaitForSeconds(2f);
            if (continueText != null) continueText.gameObject.SetActive(true);
        }

        private void CreateLevelCompletePanel()
        {
            var canvasRT = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
            if (canvasRT == null) return;

            var parentRT = GetComponent<RectTransform>() ?? canvasRT;

            levelCompletePanel = new GameObject("LevelCompletePanel");
            var panelRT = levelCompletePanel.AddComponent<RectTransform>();
            panelRT.SetParent(parentRT, false);
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(500f, 200f);

            var bg = levelCompletePanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            // Main text: "PLANET CLEANED!"
            var mainTextGO = new GameObject("MainText");
            var mainTextRT = mainTextGO.AddComponent<RectTransform>();
            mainTextRT.SetParent(panelRT, false);
            mainTextRT.anchorMin = new Vector2(0f, 0.5f);
            mainTextRT.anchorMax = new Vector2(1f, 1f);
            mainTextRT.sizeDelta = Vector2.zero;
            mainTextRT.anchoredPosition = new Vector2(0f, 10f);

            var mainTMP = mainTextGO.AddComponent<TextMeshProUGUI>();
            mainTMP.text = "PLANET CLEANED!";
            mainTMP.fontSize = 36f;
            mainTMP.color = new Color(0.3f, 1f, 0.3f, 1f);
            mainTMP.alignment = TextAlignmentOptions.Center;
            mainTMP.fontStyle = FontStyles.Bold;

            // Sub-text: "Citizens celebrate!"
            var subTextGO = new GameObject("SubText");
            var subTextRT = subTextGO.AddComponent<RectTransform>();
            subTextRT.SetParent(panelRT, false);
            subTextRT.anchorMin = new Vector2(0f, 0.25f);
            subTextRT.anchorMax = new Vector2(1f, 0.55f);
            subTextRT.sizeDelta = Vector2.zero;
            subTextRT.anchoredPosition = Vector2.zero;

            var subTMP = subTextGO.AddComponent<TextMeshProUGUI>();
            subTMP.text = "Citizens celebrate!";
            subTMP.fontSize = 22f;
            subTMP.color = new Color(1f, 0.95f, 0.6f, 1f);
            subTMP.alignment = TextAlignmentOptions.Center;

            // Continue text: "Next Planet >>"
            var continueTextGO = new GameObject("ContinueText");
            var continueTextRT = continueTextGO.AddComponent<RectTransform>();
            continueTextRT.SetParent(panelRT, false);
            continueTextRT.anchorMin = new Vector2(0f, 0f);
            continueTextRT.anchorMax = new Vector2(1f, 0.3f);
            continueTextRT.sizeDelta = Vector2.zero;
            continueTextRT.anchoredPosition = Vector2.zero;

            var continueTMP = continueTextGO.AddComponent<TextMeshProUGUI>();
            continueTMP.text = "Next Planet >>";
            continueTMP.fontSize = 18f;
            continueTMP.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            continueTMP.alignment = TextAlignmentOptions.Center;
            continueTMP.fontStyle = FontStyles.Italic;

            levelCompletePanel.SetActive(false);
        }

        private void SpawnConfettiEffect()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            var confettiGO = new GameObject("CelebrationConfetti");
            confettiGO.transform.position = cam.transform.position
                + cam.transform.forward * 10f
                + cam.transform.up * 8f;
            confettiGO.transform.rotation = Quaternion.LookRotation(Vector3.down);

            var ps = confettiGO.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 4f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.3f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            // Random start color from confetti palette
            Gradient colorGradient = new Gradient();
            colorGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1f, 0.2f, 0.2f), 0f),     // Red
                    new GradientColorKey(new Color(1f, 0.9f, 0.1f), 0.25f),  // Yellow
                    new GradientColorKey(new Color(0.2f, 1f, 0.3f), 0.5f),   // Green
                    new GradientColorKey(new Color(0.2f, 0.5f, 1f), 0.75f),  // Blue
                    new GradientColorKey(new Color(1f, 0.4f, 0.8f), 1f)      // Pink
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            main.startColor = new ParticleSystem.MinMaxGradient(colorGradient);

            // Fade out over lifetime
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fadeGradient = new Gradient();
            fadeGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = fadeGradient;

            // Emission: single burst of 80 particles
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 80)
            });

            // Cone shape spreading outward
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 3f;

            // Tumbling rotation
            var rotOverLifetime = ps.rotationOverLifetime;
            rotOverLifetime.enabled = true;
            rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-180f, 180f);

            // URP-compatible particle material
            var renderer = confettiGO.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (s_ParticleShader == null)
            {
                s_ParticleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (s_ParticleShader == null)
                    s_ParticleShader = Shader.Find("Particles/Standard Unlit");
            }
            var mat = new Material(s_ParticleShader);
            renderer.material = mat;

            ps.Play();

            Destroy(confettiGO, 5f);
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

        private void CreateBurstCooldownIndicator()
        {
            var canvasRT = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
            if (canvasRT == null) return;

            var parentRT = GetComponent<RectTransform>() ?? canvasRT;

            var go = new GameObject("BurstCooldownIndicator");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parentRT, false);
            // Bottom-right area, near fire button
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-30f, 180f);
            rt.sizeDelta = new Vector2(64f, 64f);

            burstCooldownImage = go.AddComponent<Image>();
            burstCooldownImage.color = new Color(1f, 0.45f, 0.15f, 0.6f); // Warm orange, semi-transparent
            burstCooldownImage.type = Image.Type.Filled;
            burstCooldownImage.fillMethod = Image.FillMethod.Radial360;
            burstCooldownImage.fillOrigin = (int)Image.Origin360.Top;
            burstCooldownImage.fillClockwise = false;
            burstCooldownImage.fillAmount = 1f;
            burstCooldownImage.enabled = false; // Hidden when not on cooldown
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
