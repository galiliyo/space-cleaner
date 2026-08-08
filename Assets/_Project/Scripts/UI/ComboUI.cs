using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceCleaner.Core;

namespace SpaceCleaner.UI
{
    /// <summary>
    /// On-screen combo multiplier display (GDD §2.4, HUD center).
    /// Escalating visual flair: bigger font + hotter colors at higher tiers,
    /// pop animation on each pickup, radial timer showing the 2s window.
    /// Creates its own UI programmatically on the HUD canvas — no scene setup needed.
    /// </summary>
    public class ComboUI : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float popScale = 1.4f;
        [SerializeField] private float popDuration = 0.18f;
        [SerializeField] private float tierUpPopScale = 1.8f;
        [SerializeField] private float idlePulseSpeed = 3f;
        [SerializeField] private float idlePulseAmount = 0.06f;

        [Header("Layout")]
        [SerializeField] private Vector2 position = new Vector2(0f, 120f); // center, above crosshair
        [SerializeField] private int baseFontSize = 40;

        // Tier escalation: multiplier -> (color, font scale)
        private static readonly (Color color, float scale)[] tierStyles =
        {
            (new Color(0.5f, 0.9f, 1.0f), 1.00f), // x1 (unused, baseline cyan)
            (new Color(0.5f, 1.0f, 0.5f), 1.00f), // x2 green
            (new Color(1.0f, 0.95f, 0.3f), 1.10f), // x3 yellow
            (new Color(1.0f, 0.7f, 0.2f), 1.20f), // x5 orange
            (new Color(1.0f, 0.45f, 0.2f), 1.32f), // x8 deep orange
            (new Color(1.0f, 0.2f, 0.4f), 1.45f), // x10 hot red-pink
        };

        private GameObject container;
        private TextMeshProUGUI multiplierText;
        private TextMeshProUGUI comboCountText;
        private Image timerRing;
        private RectTransform containerRT;
        private CanvasGroup canvasGroup;

        private Vector3 baseScale = Vector3.one;
        private float popTimer = -1f;
        private float popMaxScale = 1.4f;
        private int currentMultiplier = 1;
        private float fadeTarget;
        private float fadeSpeed = 6f;

        private void Awake()
        {
            BuildUI();
        }

        private void Start()
        {
            var combo = ComboManager.Instance;
            if (combo != null)
            {
                combo.OnComboIncreased += HandleComboIncreased;
                combo.OnComboLost += HandleComboLost;
                combo.OnComboTimerChanged += HandleTimerChanged;
            }
            fadeTarget = 0f;
            canvasGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            var combo = ComboManager.Instance;
            if (combo != null)
            {
                combo.OnComboIncreased -= HandleComboIncreased;
                combo.OnComboLost -= HandleComboLost;
                combo.OnComboTimerChanged -= HandleTimerChanged;
            }
        }

        private void BuildUI()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[ComboUI] No parent Canvas found.");
#endif
                return;
            }

            var parentRT = GetComponent<RectTransform>() ?? canvas.GetComponent<RectTransform>();

            // Container
            container = new GameObject("ComboUI");
            containerRT = container.AddComponent<RectTransform>();
            containerRT.SetParent(parentRT, false);
            containerRT.anchorMin = new Vector2(0.5f, 0.5f);
            containerRT.anchorMax = new Vector2(0.5f, 0.5f);
            containerRT.pivot = new Vector2(0.5f, 0.5f);
            containerRT.anchoredPosition = position;
            containerRT.sizeDelta = new Vector2(200f, 90f);
            canvasGroup = container.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Multiplier text "x5"
            var multGO = new GameObject("MultiplierText");
            var multRT = multGO.AddComponent<RectTransform>();
            multRT.SetParent(containerRT, false);
            multRT.anchorMin = new Vector2(0f, 0.35f);
            multRT.anchorMax = new Vector2(1f, 1f);
            multRT.offsetMin = Vector2.zero;
            multRT.offsetMax = Vector2.zero;
            multiplierText = multGO.AddComponent<TextMeshProUGUI>();
            multiplierText.text = "x2";
            multiplierText.fontSize = baseFontSize;
            multiplierText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            multiplierText.alignment = TextAlignmentOptions.Center;
            multiplierText.color = tierStyles[1].color;
            // Outline for readability against planet
            multiplierText.outlineWidth = 0.25f;
            multiplierText.outlineColor = new Color(0f, 0f, 0f, 0.7f);

            // Combo count "5 COMBO"
            var countGO = new GameObject("ComboCountText");
            var countRT = countGO.AddComponent<RectTransform>();
            countRT.SetParent(containerRT, false);
            countRT.anchorMin = new Vector2(0f, 0f);
            countRT.anchorMax = new Vector2(1f, 0.35f);
            countRT.offsetMin = Vector2.zero;
            countRT.offsetMax = Vector2.zero;
            comboCountText = countGO.AddComponent<TextMeshProUGUI>();
            comboCountText.text = "";
            comboCountText.fontSize = 16f;
            comboCountText.fontStyle = FontStyles.Bold;
            comboCountText.alignment = TextAlignmentOptions.Center;
            comboCountText.color = new Color(1f, 1f, 1f, 0.85f);
            comboCountText.outlineWidth = 0.2f;
            comboCountText.outlineColor = new Color(0f, 0f, 0f, 0.6f);

            // Radial timer ring behind the multiplier text
            var ringGO = new GameObject("TimerRing");
            var ringRT = ringGO.AddComponent<RectTransform>();
            ringRT.SetParent(containerRT, false);
            ringRT.SetSiblingIndex(0); // behind text
            ringRT.anchorMin = new Vector2(0.5f, 0.65f);
            ringRT.anchorMax = new Vector2(0.5f, 0.65f);
            ringRT.pivot = new Vector2(0.5f, 0.5f);
            ringRT.anchoredPosition = Vector2.zero;
            ringRT.sizeDelta = new Vector2(110f, 110f);
            timerRing = ringGO.AddComponent<Image>();
            timerRing.color = new Color(1f, 1f, 1f, 0.35f);
            timerRing.type = Image.Type.Filled;
            timerRing.fillMethod = Image.FillMethod.Radial360;
            timerRing.fillOrigin = (int)Image.Origin360.Top;
            timerRing.fillClockwise = false;
            timerRing.fillAmount = 1f;
            timerRing.raycastTarget = false;
        }

        private void HandleComboIncreased(int comboCount, int multiplier)
        {
            bool tierUp = multiplier != currentMultiplier;
            currentMultiplier = multiplier;

            var style = StyleFor(multiplier);
            multiplierText.text = $"x{multiplier}";
            multiplierText.color = style.color;
            multiplierText.fontSize = baseFontSize * style.scale;
            comboCountText.text = $"{comboCount} COMBO";
            timerRing.color = new Color(style.color.r, style.color.g, style.color.b, 0.35f);

            // Pop animation — bigger on tier-up
            popTimer = 0f;
            popMaxScale = tierUp ? tierUpPopScale : popScale;

            fadeTarget = 1f;
        }

        private void HandleComboLost(int finalCount)
        {
            currentMultiplier = 1;
            fadeTarget = 0f;
        }

        private void HandleTimerChanged(float normalized)
        {
            if (timerRing != null)
                timerRing.fillAmount = normalized;
        }

        private static (Color color, float scale) StyleFor(int multiplier)
        {
            // Map multiplier value to the closest defined tier
            return multiplier switch
            {
                <= 1 => tierStyles[0],
                2 => tierStyles[1],
                <= 4 => tierStyles[2],
                <= 7 => tierStyles[3],
                <= 9 => tierStyles[4],
                _ => tierStyles[5],
            };
        }

        private void Update()
        {
            if (canvasGroup == null) return;

            // Fade in/out
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, fadeTarget, fadeSpeed * Time.deltaTime);
            if (canvasGroup.alpha <= 0f) return;

            // Pop animation
            float scale = 1f;
            if (popTimer >= 0f)
            {
                popTimer += Time.deltaTime;
                float t = Mathf.Clamp01(popTimer / popDuration);
                scale = 1f + (popMaxScale - 1f) * Mathf.Sin(t * Mathf.PI);
                if (t >= 1f) popTimer = -1f;
            }
            else
            {
                // Subtle idle pulse so the combo feels alive
                scale = 1f + Mathf.Sin(Time.unscaledTime * idlePulseSpeed) * idlePulseAmount;
            }
            containerRT.localScale = baseScale * scale;
        }
    }
}
