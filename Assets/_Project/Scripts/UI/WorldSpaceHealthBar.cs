using UnityEngine;
using UnityEngine.UI;
using SpaceCleaner.Core;

namespace SpaceCleaner.UI
{
    public class WorldSpaceHealthBar : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
        [SerializeField] private float visibleRange = 30f;
        [SerializeField] private float barWidth = 1.5f;
        [SerializeField] private float barHeight = 0.15f;

        private Health health;
        private Transform cam;
        private GameObject barCanvas;
        private Image fillImage;

        private void Start()
        {
            health = GetComponent<Health>();
            cam = UnityEngine.Camera.main?.transform;

            CreateHealthBar();

            if (health != null)
                health.OnHealthChanged += OnHealthChanged;
        }

        private void CreateHealthBar()
        {
            // World-space canvas
            barCanvas = new GameObject("HealthBarCanvas");
            barCanvas.transform.SetParent(transform, false);
            barCanvas.transform.localPosition = offset;

            var canvas = barCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            var rt = barCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(barWidth, barHeight);
            rt.localScale = Vector3.one;

            var scaler = barCanvas.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            // Background
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(rt, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

            // Fill
            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(rt, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillRt.pivot = new Vector2(0, 0.5f);
            fillImage = fillGo.GetComponent<Image>();
            fillImage.color = new Color(0.9f, 0.2f, 0.2f, 0.8f);
        }

        private void LateUpdate()
        {
            if (cam == null || barCanvas == null) return;

            float dist = Vector3.Distance(transform.position, cam.position);
            bool visible = dist < visibleRange && health != null && !health.IsDead;
            barCanvas.SetActive(visible);

            if (!visible) return;

            // Billboard — face camera
            barCanvas.transform.rotation = Quaternion.LookRotation(
                barCanvas.transform.position - cam.position, cam.up);
        }

        private void OnHealthChanged(int current, int max)
        {
            if (fillImage == null) return;

            float normalized = (float)current / max;
            fillImage.rectTransform.anchorMax = new Vector2(normalized, 1f);

            // Color shift: green → yellow → red
            if (normalized > 0.5f)
                fillImage.color = Color.Lerp(Color.yellow, Color.green, (normalized - 0.5f) * 2f);
            else
                fillImage.color = Color.Lerp(Color.red, Color.yellow, normalized * 2f);
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnHealthChanged -= OnHealthChanged;
        }
    }
}
