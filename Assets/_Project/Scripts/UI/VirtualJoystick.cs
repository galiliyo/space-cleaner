using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace SpaceCleaner.UI
{
    /// <summary>
    /// On-screen virtual joystick for mobile touch controls.
    /// Extends OnScreenControl directly and implements pointer/drag interfaces
    /// to inject a Vector2 into the Input System (e.g. as Gamepad leftStick).
    /// Renders a circular background with a draggable handle that follows the finger.
    /// </summary>
    [AddComponentMenu("Input/Virtual Joystick")]
    public class VirtualJoystick : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Joystick Settings")]
        [SerializeField] private float movementRange = 60f;

        [Header("Joystick Visuals")]
        [SerializeField] private float backgroundRadius = 60f;
        [SerializeField] private float handleRadius = 25f;
        [SerializeField] private Color backgroundColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color handleColor = new Color(1f, 1f, 1f, 0.6f);

        [InputControl(layout = "Vector2")]
        [SerializeField]
        private string m_ControlPath;

        private RectTransform handleRect;
        private RectTransform backgroundRect;
        private Image backgroundImage;
        private Image handleImage;
        private Vector2 startPos;
        private Canvas parentCanvas;
        private UnityEngine.Camera canvasCamera;

        /// <summary>
        /// Current joystick direction normalized to [-1, 1] on each axis.
        /// Can be read directly if needed outside the Input System pipeline.
        /// </summary>
        public Vector2 Direction { get; private set; }

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
        /// Creates the background and handle UI images if they don't already exist.
        /// </summary>
        public void EnsureVisuals()
        {
            if (handleRect == null)
                handleRect = GetComponent<RectTransform>();

            // --- Background circle ---
            if (backgroundRect == null)
            {
                var existing = transform.Find("JoystickBackground");
                if (existing != null)
                {
                    backgroundRect = existing.GetComponent<RectTransform>();
                    backgroundImage = existing.GetComponent<Image>();
                }
                else
                {
                    var bgGo = new GameObject("JoystickBackground", typeof(RectTransform), typeof(Image));
                    bgGo.transform.SetParent(transform, false);
                    backgroundRect = bgGo.GetComponent<RectTransform>();
                    backgroundImage = bgGo.GetComponent<Image>();

                    backgroundRect.anchoredPosition = Vector2.zero;
                    backgroundRect.sizeDelta = new Vector2(backgroundRadius * 2f, backgroundRadius * 2f);

                    backgroundImage.color = backgroundColor;
                    backgroundImage.raycastTarget = false;
                    backgroundImage.sprite = CreateCircleSprite();
                    backgroundImage.type = Image.Type.Simple;
                }
            }

            // --- Handle (this GameObject's own Image) ---
            handleImage = GetComponent<Image>();
            if (handleImage == null)
                handleImage = gameObject.AddComponent<Image>();

            handleImage.color = handleColor;
            handleImage.sprite = CreateCircleSprite();
            handleImage.type = Image.Type.Simple;
            handleImage.raycastTarget = true;

            handleRect.sizeDelta = new Vector2(handleRadius * 2f, handleRadius * 2f);

            // Ensure the background renders behind the handle
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

            Direction = delta / movementRange;
            SendValueToControl(Direction);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (handleRect != null)
                handleRect.anchoredPosition = startPos;

            Direction = Vector2.zero;
            SendValueToControl(Vector2.zero);
        }

        /// <summary>
        /// Creates a simple white filled circle sprite at runtime.
        /// </summary>
        private static Sprite s_CircleSprite;
        internal static Sprite CreateCircleSprite()
        {
            if (s_CircleSprite != null) return s_CircleSprite;

            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float center = size / 2f;
            float radiusSq = center * center;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float distSq = dx * dx + dy * dy;

                    if (distSq <= radiusSq - center)
                        pixels[y * size + x] = new Color32(255, 255, 255, 255);
                    else if (distSq <= radiusSq)
                    {
                        float t = 1f - (distSq - (radiusSq - center)) / center;
                        byte a = (byte)(Mathf.Clamp01(t) * 255);
                        pixels[y * size + x] = new Color32(255, 255, 255, a);
                    }
                    else
                        pixels[y * size + x] = new Color32(255, 255, 255, 0);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            s_CircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            s_CircleSprite.name = "RuntimeCircle";
            return s_CircleSprite;
        }

        /// <summary>
        /// Factory method to create a fully configured VirtualJoystick on a Canvas.
        /// </summary>
        /// <param name="parent">Parent RectTransform (should be on a Canvas).</param>
        /// <param name="controlPath">Input System control path, e.g. "&lt;Gamepad&gt;/leftStick".</param>
        /// <param name="anchorPosition">Anchored position relative to bottom-left.</param>
        /// <returns>The created VirtualJoystick component.</returns>
        public static VirtualJoystick Create(RectTransform parent, string controlPath, Vector2 anchorPosition)
        {
            var go = new GameObject("MoveJoystick", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchorPosition;

            var joystick = go.AddComponent<VirtualJoystick>();
            joystick.m_ControlPath = controlPath;
            joystick.movementRange = 60f;

            joystick.EnsureVisuals();

            return joystick;
        }
    }
}
