using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace SpaceCleaner.UI
{
    /// <summary>
    /// On-screen aim/fire control for the right side of the screen.
    /// Extends OnScreenControl directly and implements pointer/drag interfaces
    /// to inject a Vector2 into the Input System (e.g. as Gamepad rightStick).
    ///
    /// The ShootingSystem already handles the flick-vs-hold logic based on
    /// the Aim input magnitude:
    ///   - Aim magnitude > 0.01 for less than flickThreshold => single shot on release
    ///   - Aim magnitude > 0.01 for more than flickThreshold => auto-fire while held
    ///
    /// This component adds an aim line visual that shows the current aim direction.
    /// </summary>
    [AddComponentMenu("Input/Fire Button")]
    public class FireButton : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Fire Button Settings")]
        [SerializeField] private float movementRange = 60f;

        [Header("Fire Button Visuals")]
        [SerializeField] private float touchAreaRadius = 60f;
        [SerializeField] private float handleRadius = 22f;
        [SerializeField] private Color touchAreaColor = new Color(1f, 0.3f, 0.3f, 0.2f);
        [SerializeField] private Color handleColor = new Color(1f, 0.4f, 0.4f, 0.6f);
        [SerializeField] private Color aimLineColor = new Color(1f, 0.5f, 0.5f, 0.5f);

        [Header("Aim Line")]
        [SerializeField] private float aimLineLength = 50f;
        [SerializeField] private float aimLineWidth = 4f;

        [InputControl(layout = "Vector2")]
        [SerializeField]
        private string m_ControlPath;

        private RectTransform handleRect;
        private RectTransform backgroundRect;
        private Image backgroundImage;
        private Image handleImage;
        private RectTransform aimLineRect;
        private Image aimLineImage;
        private Vector2 startPos;
        private Canvas parentCanvas;
        private UnityEngine.Camera canvasCamera;

        /// <summary>
        /// Current aim direction normalized to [-1, 1] on each axis.
        /// </summary>
        public Vector2 AimDirection { get; private set; }

        protected override string controlPathInternal
        {
            get => m_ControlPath;
            set => m_ControlPath = value;
        }

        private void Start()
        {
            handleRect = GetComponent<RectTransform>();
            startPos = handleRect.anchoredPosition;

            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                canvasCamera = parentCanvas.worldCamera;

            EnsureVisuals();
        }

        /// <summary>
        /// Creates the touch area background, handle, and aim line visuals.
        /// </summary>
        public void EnsureVisuals()
        {
            if (handleRect == null)
                handleRect = GetComponent<RectTransform>();

            // --- Background touch area ---
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

            // --- Handle (this object's Image) ---
            handleImage = GetComponent<Image>();
            if (handleImage == null)
                handleImage = gameObject.AddComponent<Image>();

            handleImage.color = handleColor;
            handleImage.sprite = VirtualJoystick.CreateCircleSprite();
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

            // Initially hidden
            if (aimLineRect != null)
                aimLineRect.gameObject.SetActive(false);

            // Ensure background renders behind handle
            if (backgroundRect != null)
                backgroundRect.SetAsFirstSibling();
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

        /// <summary>
        /// Factory method to create a fully configured FireButton on a Canvas.
        /// </summary>
        /// <param name="parent">Parent RectTransform (should be on a Canvas).</param>
        /// <param name="controlPath">Input System control path, e.g. "&lt;Gamepad&gt;/rightStick".</param>
        /// <param name="anchorPosition">Anchored position relative to bottom-right.</param>
        /// <returns>The created FireButton component.</returns>
        public static FireButton Create(RectTransform parent, string controlPath, Vector2 anchorPosition)
        {
            var go = new GameObject("AimFireButton", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchorPosition;

            var fireButton = go.AddComponent<FireButton>();
            fireButton.m_ControlPath = controlPath;
            fireButton.movementRange = 60f;

            fireButton.EnsureVisuals();

            return fireButton;
        }
    }
}
