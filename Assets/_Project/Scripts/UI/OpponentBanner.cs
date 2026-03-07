using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceCleaner.Core;
using SpaceCleaner.Enemies;

namespace SpaceCleaner.UI
{
    public class OpponentBanner : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Vector3 bannerOffset = new Vector3(2f, 3f, 0f);
        [SerializeField] private float lineThickness = 0.04f;

        [Header("Visibility")]
        [SerializeField] private float visibleRange = 80f;

        [Header("Health Bar Size")]
        [SerializeField] private float barWidth = 1.8f;
        [SerializeField] private float barHeight = 0.18f;

        private Health health;
        private Transform cam;
        private GameObject bannerCanvas;
        private Image fillImage;
        private TextMeshProUGUI nameText;

        private void Start()
        {
            health = GetComponent<Health>();
            cam = Camera.main?.transform;

            string name = "Opponent";
            var ai = GetComponent<AIOpponent>();
            if (ai != null)
                name = ai.OpponentName;

            CreateBanner(name);

            if (health != null)
                health.OnHealthChanged += OnHealthChanged;
        }

        private void CreateBanner(string name)
        {
            // World-space canvas
            bannerCanvas = new GameObject("BannerCanvas");
            bannerCanvas.transform.SetParent(transform, false);
            bannerCanvas.transform.localPosition = Vector3.zero;

            var canvas = bannerCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            var canvasRt = bannerCanvas.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(6f, 5f);
            canvasRt.localScale = Vector3.one;

            var scaler = bannerCanvas.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            // Diagonal line from origin to bannerOffset
            CreateDiagonalLine(canvasRt);

            // Banner panel at offset position
            CreateBannerPanel(canvasRt, name);
        }

        private void CreateDiagonalLine(RectTransform parent)
        {
            var lineGo = new GameObject("DiagonalLine", typeof(RectTransform), typeof(Image));
            lineGo.transform.SetParent(parent, false);

            var lineRt = lineGo.GetComponent<RectTransform>();
            float length = bannerOffset.magnitude;
            float angle = Mathf.Atan2(bannerOffset.y, bannerOffset.x) * Mathf.Rad2Deg;

            lineRt.pivot = new Vector2(0f, 0.5f);
            lineRt.anchorMin = new Vector2(0.5f, 0.5f);
            lineRt.anchorMax = new Vector2(0.5f, 0.5f);
            lineRt.anchoredPosition = Vector2.zero;
            lineRt.sizeDelta = new Vector2(length, lineThickness);
            lineRt.localRotation = Quaternion.Euler(0, 0, angle);

            var lineImg = lineGo.GetComponent<Image>();
            lineImg.color = new Color(1f, 1f, 1f, 0.7f);
        }

        private void CreateBannerPanel(RectTransform parent, string name)
        {
            var panelGo = new GameObject("BannerPanel", typeof(RectTransform));
            panelGo.transform.SetParent(parent, false);

            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = new Vector2(bannerOffset.x, bannerOffset.y);
            panelRt.sizeDelta = new Vector2(barWidth, barHeight + 0.25f);

            // Name text
            var nameGo = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGo.transform.SetParent(panelRt, false);

            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.5f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(0.05f, 0f);
            nameRt.offsetMax = Vector2.zero;

            nameText = nameGo.GetComponent<TextMeshProUGUI>();
            nameText.text = name;
            nameText.fontSize = 0.8f;
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Truncate;

            // Health bar background
            var bgGo = new GameObject("HealthBarBG", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(panelRt, false);

            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0f);
            bgRt.anchorMax = new Vector2(1f, 0.5f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);

            // Health bar fill
            var fillGo = new GameObject("HealthBarFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(bgRt, false);

            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillRt.pivot = new Vector2(0f, 0.5f);

            fillImage = fillGo.GetComponent<Image>();
            fillImage.color = new Color(0.2f, 0.9f, 0.2f, 0.9f);
        }

        private void LateUpdate()
        {
            if (cam == null || bannerCanvas == null) return;

            float dist = Vector3.Distance(transform.position, cam.position);
            bool visible = dist < visibleRange && health != null && !health.IsDead;
            bannerCanvas.SetActive(visible);

            if (!visible) return;

            // Billboard — face camera
            bannerCanvas.transform.rotation = Quaternion.LookRotation(
                bannerCanvas.transform.position - cam.position, cam.up);
        }

        private void OnHealthChanged(int current, int max)
        {
            if (fillImage == null) return;

            float normalized = (float)current / max;
            fillImage.rectTransform.anchorMax = new Vector2(normalized, 1f);

            // Green when healthy, lerps to red below 30%
            if (normalized > 0.3f)
                fillImage.color = new Color(0.2f, 0.9f, 0.2f, 0.9f);
            else
                fillImage.color = Color.Lerp(Color.red, new Color(0.2f, 0.9f, 0.2f, 0.9f), normalized / 0.3f);
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnHealthChanged -= OnHealthChanged;
        }
    }
}
