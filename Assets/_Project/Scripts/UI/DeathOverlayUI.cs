using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceCleaner.Core;
using SpaceCleaner.Player;

namespace SpaceCleaner.UI
{
    /// <summary>
    /// Full-screen death lobby overlay. Shows cleanup stat and retry/menu buttons.
    /// Uses CanvasGroup + WaitForSecondsRealtime so fades work while timeScale=0.
    /// </summary>
    public class DeathOverlayUI : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI statText;
        private PlayerDeathHandler deathHandler;

        public void Initialize(PlayerDeathHandler handler)
        {
            deathHandler = handler;
            BuildUI();
            // Start hidden
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void BuildUI()
        {
            // Full-screen RectTransform
            var rt = gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // CanvasGroup for fading
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Dark background
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.9f);

            // --- Content container (centered) ---
            var contentGO = new GameObject("Content");
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.SetParent(rt, false);
            contentRT.anchorMin = new Vector2(0.5f, 0.5f);
            contentRT.anchorMax = new Vector2(0.5f, 0.5f);
            contentRT.pivot = new Vector2(0.5f, 0.5f);
            contentRT.sizeDelta = new Vector2(400f, 300f);

            // --- Ship placeholder (tinted/darkened) ---
            var shipGO = new GameObject("ShipIcon");
            var shipRT = shipGO.AddComponent<RectTransform>();
            shipRT.SetParent(contentRT, false);
            shipRT.anchorMin = new Vector2(0.5f, 0.7f);
            shipRT.anchorMax = new Vector2(0.5f, 0.7f);
            shipRT.pivot = new Vector2(0.5f, 0.5f);
            shipRT.sizeDelta = new Vector2(80f, 80f);

            var shipImg = shipGO.AddComponent<Image>();
            shipImg.color = new Color(0.4f, 0.3f, 0.3f, 0.8f);

            // Bobbing animation driven by Update
            shipGO.AddComponent<DeathShipBobber>();

            // --- Stat line ---
            var statGO = new GameObject("StatText");
            var statRT = statGO.AddComponent<RectTransform>();
            statRT.SetParent(contentRT, false);
            statRT.anchorMin = new Vector2(0f, 0.45f);
            statRT.anchorMax = new Vector2(1f, 0.55f);
            statRT.sizeDelta = Vector2.zero;

            statText = statGO.AddComponent<TextMeshProUGUI>();
            statText.fontSize = 22f;
            statText.color = new Color(0.8f, 0.85f, 0.9f, 1f);
            statText.alignment = TextAlignmentOptions.Center;

            // --- Retry button ---
            var retryGO = new GameObject("RetryButton");
            var retryRT = retryGO.AddComponent<RectTransform>();
            retryRT.SetParent(contentRT, false);
            retryRT.anchorMin = new Vector2(0.5f, 0.2f);
            retryRT.anchorMax = new Vector2(0.5f, 0.2f);
            retryRT.pivot = new Vector2(0.5f, 0.5f);
            retryRT.sizeDelta = new Vector2(200f, 56f);

            var retryBg = retryGO.AddComponent<Image>();
            retryBg.color = new Color(0.15f, 0.6f, 0.9f, 1f);

            var retryBtn = retryGO.AddComponent<Button>();
            retryBtn.targetGraphic = retryBg;
            retryBtn.onClick.AddListener(OnRetryClicked);

            var retryTextGO = new GameObject("Text");
            var retryTextRT = retryTextGO.AddComponent<RectTransform>();
            retryTextRT.SetParent(retryRT, false);
            retryTextRT.anchorMin = Vector2.zero;
            retryTextRT.anchorMax = Vector2.one;
            retryTextRT.sizeDelta = Vector2.zero;

            var retryTMP = retryTextGO.AddComponent<TextMeshProUGUI>();
            retryTMP.text = "RETRY";
            retryTMP.fontSize = 28f;
            retryTMP.color = Color.white;
            retryTMP.alignment = TextAlignmentOptions.Center;
            retryTMP.fontStyle = FontStyles.Bold;

            // --- Main Menu stub ---
            var menuGO = new GameObject("MenuButton");
            var menuRT = menuGO.AddComponent<RectTransform>();
            menuRT.SetParent(contentRT, false);
            menuRT.anchorMin = new Vector2(0.5f, 0.07f);
            menuRT.anchorMax = new Vector2(0.5f, 0.07f);
            menuRT.pivot = new Vector2(0.5f, 0.5f);
            menuRT.sizeDelta = new Vector2(160f, 36f);

            var menuBtn = menuGO.AddComponent<Button>();
            menuBtn.onClick.AddListener(OnMenuClicked);

            var menuTextGO = new GameObject("Text");
            var menuTextRT = menuTextGO.AddComponent<RectTransform>();
            menuTextRT.SetParent(menuRT, false);
            menuTextRT.anchorMin = Vector2.zero;
            menuTextRT.anchorMax = Vector2.one;
            menuTextRT.sizeDelta = Vector2.zero;

            var menuTMP = menuTextGO.AddComponent<TextMeshProUGUI>();
            menuTMP.text = "Main Menu";
            menuTMP.fontSize = 16f;
            menuTMP.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            menuTMP.alignment = TextAlignmentOptions.Center;

            // Menu button needs a graphic for raycasting
            var menuBg = menuGO.AddComponent<Image>();
            menuBg.color = new Color(0f, 0f, 0f, 0f); // invisible but raycastable
            menuBtn.targetGraphic = menuBg;
        }

        private void OnRetryClicked()
        {
            if (deathHandler != null)
                deathHandler.OnRetry();
        }

        private static void OnMenuClicked()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("[DeathOverlayUI] Main menu not yet implemented.");
#endif
        }

        public Coroutine FadeIn(float duration)
        {
            // Update stat text
            if (GameManager.Instance != null)
            {
                int pct = Mathf.RoundToInt(GameManager.Instance.CleanupPercentage * 100f);
                statText.text = $"Planet: {pct}% Clean";
            }

            gameObject.SetActive(true);
            return StartCoroutine(FadeCoroutine(0f, 1f, duration));
        }

        public Coroutine FadeOut(float duration)
        {
            return StartCoroutine(FadeOutCoroutine(duration));
        }

        private IEnumerator FadeOutCoroutine(float duration)
        {
            yield return FadeCoroutine(1f, 0f, duration);
            gameObject.SetActive(false);
        }

        private IEnumerator FadeCoroutine(float from, float to, float duration)
        {
            float elapsed = 0f;
            canvasGroup.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            canvasGroup.alpha = to;
        }
    }

    /// <summary>Simple bobbing animation for the damaged ship icon using Mathf.Sin.</summary>
    internal class DeathShipBobber : MonoBehaviour
    {
        private RectTransform rt;
        private Vector2 basePos;

        private void Start()
        {
            rt = GetComponent<RectTransform>();
            basePos = rt.anchoredPosition;
        }

        private void Update()
        {
            if (rt == null) return;
            float bob = Mathf.Sin(Time.unscaledTime * 1.5f) * 4f;
            rt.anchoredPosition = basePos + new Vector2(0f, bob);
        }
    }
}
