using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace SpaceCleaner.UI
{
    /// <summary>
    /// Brawl Stars-style aim/fire joystick: dark base ring with red/orange knob
    /// and directional aim line.
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

        private RectTransform handleRect;
        private RectTransform backgroundRect;
        private RectTransform borderRect;
        private Image backgroundImage;
        private Image borderImage;
        private Image handleImage;
        private RectTransform aimLineRect;
        private Image aimLineImage;
        private Vector2 startPos;
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
            handleRect = GetComponent<RectTransform>();

            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                canvasCamera = parentCanvas.worldCamera;

            EnsureVisuals();
            startPos = handleRect.anchoredPosition;
        }

        public void EnsureVisuals()
        {
            if (handleRect == null)
                handleRect = GetComponent<RectTransform>();

            // --- Outer border ring ---
            if (borderRect == null)
            {
                var existing = transform.Find("FireBorder");
                if (existing != null)
                {
                    borderRect = existing.GetComponent<RectTransform>();
                    borderImage = existing.GetComponent<Image>();
                }
                else
                {
                    var borderGo = new GameObject("FireBorder", typeof(RectTransform), typeof(Image));
                    borderGo.transform.SetParent(transform, false);
                    borderRect = borderGo.GetComponent<RectTransform>();
                    borderImage = borderGo.GetComponent<Image>();

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
                    backgroundImage = existing.GetComponent<Image>();
                }
                else
                {
                    var bgGo = new GameObject("FireBackground", typeof(RectTransform), typeof(Image));
                    bgGo.transform.SetParent(transform, false);
                    backgroundRect = bgGo.GetComponent<RectTransform>();
                    backgroundImage = bgGo.GetComponent<Image>();

                    backgroundRect.anchoredPosition = Vector2.zero;
                    backgroundRect.sizeDelta = new Vector2(touchAreaRadius * 2f, touchAreaRadius * 2f);

                    backgroundImage.color = touchAreaColor;
                    backgroundImage.raycastTarget = false;
                    backgroundImage.sprite = VirtualJoystick.CreateCircleSprite();
                    backgroundImage.type = Image.Type.Simple;
                }
            }

            // --- Handle knob (red/orange gradient circle) ---
            handleImage = GetComponent<Image>();
            if (handleImage == null)
                handleImage = gameObject.AddComponent<Image>();

            handleImage.color = handleColor;
            handleImage.sprite = VirtualJoystick.CreateGradientCircleSprite();
            handleImage.type = Image.Type.Simple;
            handleImage.raycastTarget = true;

            handleRect.sizeDelta = new Vector2(handleRadius * 2f, handleRadius * 2f);

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

            // Z-order: border, bg, aim line, handle on top
            if (borderRect != null)
                borderRect.SetAsFirstSibling();
            if (backgroundRect != null)
                backgroundRect.SetSiblingIndex(1);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (handleRect == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                eventData.position,
                canvasCamera,
                out Vector2 localPoint
            );

            Vector2 delta = localPoint - startPos;
            delta = Vector2.ClampMagnitude(delta, movementRange);

            handleRect.anchoredPosition = startPos + delta;

            AimDirection = delta / movementRange;
            SendValueToControl(AimDirection);

            UpdateAimVisuals();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (handleRect != null)
                handleRect.anchoredPosition = startPos;

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
            rt.sizeDelta = new Vector2(56f, 56f);

            var fireButton = go.AddComponent<FireButton>();
            fireButton.m_ControlPath = controlPath;
            fireButton.movementRange = 75f;

            go.SetActive(true);
            fireButton.EnsureVisuals();

            return fireButton;
        }
    }
}
