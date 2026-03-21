using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace SpaceCleaner.UI
{
    /// <summary>
    /// Aim/fire joystick with a full-size transparent touch area, visible handle,
    /// and directional aim line. The base stays fixed; the handle slides on drag.
    /// </summary>
    [AddComponentMenu("Input/Fire Button")]
    public class FireButton : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Fire Button Settings")]
        [SerializeField] private float movementRange = 75f;

        [Header("Fire Button Visuals")]
        [SerializeField] private float touchAreaRadius = 75f;
        [SerializeField] private float handleRadius = 28f;
        [SerializeField] private Color touchAreaColor = new Color(0.1f, 0.1f, 0.15f, 0.55f);
        [SerializeField] private Color borderColor = new Color(0.9f, 0.4f, 0.2f, 0.4f);
        [SerializeField] private Color handleColor = new Color(1f, 0.4f, 0.15f, 0.9f);
        [SerializeField] private Color aimLineColor = new Color(1f, 0.5f, 0.2f, 0.6f);

        [Header("Aim Line")]
        [SerializeField] private float aimLineLength = 50f;
        [SerializeField] private float aimLineWidth = 4f;

        [InputControl(layout = "Vector2")]
        [SerializeField]
        private string m_ControlPath;

        private RectTransform baseRT;
        private RectTransform handleRect;
        private RectTransform backgroundRect;
        private RectTransform borderRect;
        private RectTransform aimLineRect;
        private Image aimLineImage;
        private Canvas parentCanvas;
        private UnityEngine.Camera canvasCamera;

        public Vector2 AimDirection { get; private set; }

        protected override string controlPathInternal
        {
            get => m_ControlPath;
            set => m_ControlPath = value;
        }

        private void Start()
        {
            baseRT = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                canvasCamera = parentCanvas.worldCamera;

            EnsureVisuals();
        }

        public void EnsureVisuals()
        {
            if (baseRT == null) baseRT = GetComponent<RectTransform>();

            // Base covers the full touch area
            baseRT.sizeDelta = new Vector2(touchAreaRadius * 2f, touchAreaRadius * 2f);

            // Base image: transparent but catches all pointer events
            var baseImage = GetComponent<Image>();
            if (baseImage == null) baseImage = gameObject.AddComponent<Image>();
            baseImage.color = new Color(0, 0, 0, 0);
            baseImage.raycastTarget = true;

            // --- Outer border ring ---
            if (borderRect == null)
            {
                var existing = transform.Find("FireBorder");
                if (existing != null)
                {
                    borderRect = existing.GetComponent<RectTransform>();
                }
                else
                {
                    var borderGo = new GameObject("FireBorder", typeof(RectTransform), typeof(Image));
                    borderGo.transform.SetParent(transform, false);
                    borderRect = borderGo.GetComponent<RectTransform>();
                    var borderImage = borderGo.GetComponent<Image>();

                    float borderSize = (touchAreaRadius + 3f) * 2f;
                    borderRect.anchoredPosition = Vector2.zero;
                    borderRect.sizeDelta = new Vector2(borderSize, borderSize);

                    borderImage.color = borderColor;
                    borderImage.raycastTarget = false;
                    borderImage.sprite = VirtualJoystick.CreateRingSprite();
                    borderImage.type = Image.Type.Simple;
                }
            }

            // --- Background touch area (dark filled circle) ---
            if (backgroundRect == null)
            {
                var existing = transform.Find("FireBackground");
                if (existing != null)
                {
                    backgroundRect = existing.GetComponent<RectTransform>();
                }
                else
                {
                    var bgGo = new GameObject("FireBackground", typeof(RectTransform), typeof(Image));
                    bgGo.transform.SetParent(transform, false);
                    backgroundRect = bgGo.GetComponent<RectTransform>();
                    var bgImage = bgGo.GetComponent<Image>();

                    backgroundRect.anchoredPosition = Vector2.zero;
                    backgroundRect.sizeDelta = new Vector2(touchAreaRadius * 2f, touchAreaRadius * 2f);

                    bgImage.color = touchAreaColor;
                    bgImage.raycastTarget = false;
                    bgImage.sprite = VirtualJoystick.CreateCircleSprite();
                    bgImage.type = Image.Type.Simple;
                }
            }

            // --- Aim line ---
            if (aimLineRect == null)
            {
                var existing = transform.Find("AimLine");
                if (existing != null)
                {
                    aimLineRect = existing.GetComponent<RectTransform>();
                    aimLineImage = existing.GetComponent<Image>();
                }
                else
                {
                    var lineGo = new GameObject("AimLine", typeof(RectTransform), typeof(Image));
                    lineGo.transform.SetParent(transform, false);
                    aimLineRect = lineGo.GetComponent<RectTransform>();
                    aimLineImage = lineGo.GetComponent<Image>();

                    aimLineRect.pivot = new Vector2(0.5f, 0f);
                    aimLineRect.sizeDelta = new Vector2(aimLineWidth, aimLineLength);
                    aimLineRect.anchoredPosition = Vector2.zero;

                    aimLineImage.color = aimLineColor;
                    aimLineImage.raycastTarget = false;
                }
            }

            if (aimLineRect != null)
                aimLineRect.gameObject.SetActive(false);

            // --- Handle knob (child that moves on drag) ---
            if (handleRect == null)
            {
                var existing = transform.Find("FireHandle");
                if (existing != null)
                {
                    handleRect = existing.GetComponent<RectTransform>();
                }
                else
                {
                    var handleGo = new GameObject("FireHandle", typeof(RectTransform), typeof(Image));
                    handleGo.transform.SetParent(transform, false);
                    handleRect = handleGo.GetComponent<RectTransform>();
                    var handleImage = handleGo.GetComponent<Image>();

                    handleRect.anchoredPosition = Vector2.zero;
                    handleRect.sizeDelta = new Vector2(handleRadius * 2f, handleRadius * 2f);

                    handleImage.color = handleColor;
                    handleImage.sprite = VirtualJoystick.CreateGradientCircleSprite();
                    handleImage.type = Image.Type.Simple;
                    handleImage.raycastTarget = false;
                }
            }

            // Z-order: border, bg, aim line, handle on top
            if (borderRect != null) borderRect.SetAsFirstSibling();
            if (backgroundRect != null) backgroundRect.SetSiblingIndex(1);
            if (handleRect != null) handleRect.SetAsLastSibling();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (handleRect == null || baseRT == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                baseRT,
                eventData.position,
                canvasCamera,
                out Vector2 localPoint
            );

            localPoint = Vector2.ClampMagnitude(localPoint, movementRange);
            handleRect.anchoredPosition = localPoint;

            AimDirection = localPoint / movementRange;
            SendValueToControl(AimDirection);

            UpdateAimVisuals();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (handleRect != null)
                handleRect.anchoredPosition = Vector2.zero;

            AimDirection = Vector2.zero;
            SendValueToControl(Vector2.zero);

            if (aimLineRect != null)
                aimLineRect.gameObject.SetActive(false);
        }

        private void UpdateAimVisuals()
        {
            if (aimLineRect == null) return;

            bool showLine = AimDirection.sqrMagnitude > 0.01f;
            aimLineRect.gameObject.SetActive(showLine);

            if (showLine)
            {
                float angle = Mathf.Atan2(AimDirection.x, AimDirection.y) * Mathf.Rad2Deg;
                aimLineRect.localRotation = Quaternion.Euler(0f, 0f, -angle);
                aimLineRect.sizeDelta = new Vector2(aimLineWidth, aimLineLength * AimDirection.magnitude);
            }
        }

        public static FireButton Create(RectTransform parent, string controlPath, Vector2 anchorPosition)
        {
            var go = new GameObject("AimFireButton", typeof(RectTransform));
            go.SetActive(false);
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchorPosition;

            var fireButton = go.AddComponent<FireButton>();
            fireButton.m_ControlPath = controlPath;
            fireButton.movementRange = 75f;

            go.SetActive(true);
            fireButton.EnsureVisuals();

            return fireButton;
        }
    }
}
