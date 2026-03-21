using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace SpaceCleaner.UI
{
    /// <summary>
    /// Virtual joystick with a full-size transparent touch area.
    /// The base stays fixed and receives all pointer events; the handle (knob) is a
    /// child that slides within it on drag.
    /// </summary>
    [AddComponentMenu("Input/Virtual Joystick")]
    public class VirtualJoystick : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Joystick Settings")]
        [SerializeField] private float movementRange = 75f;

        [Header("Joystick Visuals")]
        [SerializeField] private float backgroundRadius = 75f;
        [SerializeField] private float handleRadius = 30f;
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.55f);
        [SerializeField] private Color borderColor = new Color(0.3f, 0.6f, 0.9f, 0.4f);
        [SerializeField] private Color handleColor = new Color(0.3f, 0.7f, 1f, 0.9f);

        [InputControl(layout = "Vector2")]
        [SerializeField]
        private string m_ControlPath;

        private RectTransform baseRT;
        private RectTransform handleRect;
        private RectTransform backgroundRect;
        private RectTransform borderRect;
        private Canvas parentCanvas;
        private UnityEngine.Camera canvasCamera;

        public Vector2 Direction { get; private set; }

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
            baseRT.sizeDelta = new Vector2(backgroundRadius * 2f, backgroundRadius * 2f);

            // Base image: transparent but catches all pointer events in the area
            var baseImage = GetComponent<Image>();
            if (baseImage == null) baseImage = gameObject.AddComponent<Image>();
            baseImage.color = new Color(0, 0, 0, 0);
            baseImage.raycastTarget = true;

            // --- Outer border ring ---
            if (borderRect == null)
            {
                var existing = transform.Find("JoystickBorder");
                if (existing != null)
                {
                    borderRect = existing.GetComponent<RectTransform>();
                }
                else
                {
                    var borderGo = new GameObject("JoystickBorder", typeof(RectTransform), typeof(Image));
                    borderGo.transform.SetParent(transform, false);
                    borderRect = borderGo.GetComponent<RectTransform>();
                    var borderImage = borderGo.GetComponent<Image>();

                    float borderSize = (backgroundRadius + 3f) * 2f;
                    borderRect.anchoredPosition = Vector2.zero;
                    borderRect.sizeDelta = new Vector2(borderSize, borderSize);

                    borderImage.color = borderColor;
                    borderImage.raycastTarget = false;
                    borderImage.sprite = CreateRingSprite();
                    borderImage.type = Image.Type.Simple;
                }
            }

            // --- Background (dark filled circle) ---
            if (backgroundRect == null)
            {
                var existing = transform.Find("JoystickBackground");
                if (existing != null)
                {
                    backgroundRect = existing.GetComponent<RectTransform>();
                }
                else
                {
                    var bgGo = new GameObject("JoystickBackground", typeof(RectTransform), typeof(Image));
                    bgGo.transform.SetParent(transform, false);
                    backgroundRect = bgGo.GetComponent<RectTransform>();
                    var bgImage = bgGo.GetComponent<Image>();

                    backgroundRect.anchoredPosition = Vector2.zero;
                    backgroundRect.sizeDelta = new Vector2(backgroundRadius * 2f, backgroundRadius * 2f);

                    bgImage.color = backgroundColor;
                    bgImage.raycastTarget = false;
                    bgImage.sprite = CreateCircleSprite();
                    bgImage.type = Image.Type.Simple;
                }
            }

            // --- Handle knob (child that moves on drag) ---
            if (handleRect == null)
            {
                var existing = transform.Find("JoystickHandle");
                if (existing != null)
                {
                    handleRect = existing.GetComponent<RectTransform>();
                }
                else
                {
                    var handleGo = new GameObject("JoystickHandle", typeof(RectTransform), typeof(Image));
                    handleGo.transform.SetParent(transform, false);
                    handleRect = handleGo.GetComponent<RectTransform>();
                    var handleImage = handleGo.GetComponent<Image>();

                    handleRect.anchoredPosition = Vector2.zero;
                    handleRect.sizeDelta = new Vector2(handleRadius * 2f, handleRadius * 2f);

                    handleImage.color = handleColor;
                    handleImage.sprite = CreateGradientCircleSprite();
                    handleImage.type = Image.Type.Simple;
                    handleImage.raycastTarget = false;
                }
            }

            // Z-order: border behind bg behind handle
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

            Direction = localPoint / movementRange;
            SendValueToControl(Direction);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (handleRect != null)
                handleRect.anchoredPosition = Vector2.zero;

            Direction = Vector2.zero;
            SendValueToControl(Vector2.zero);
        }

        // --- Sprite generators (cached, shared) ---

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

        private static Sprite s_RingSprite;
        internal static Sprite CreateRingSprite()
        {
            if (s_RingSprite != null) return s_RingSprite;

            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float center = size / 2f;
            float outerR = center;
            float innerR = center - 3f;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist >= innerR && dist <= outerR)
                    {
                        float edgeFade = 1f;
                        if (dist > outerR - 1f) edgeFade = outerR - dist;
                        if (dist < innerR + 1f) edgeFade = Mathf.Min(edgeFade, dist - innerR);
                        edgeFade = Mathf.Clamp01(edgeFade);
                        byte a = (byte)(edgeFade * 255);
                        pixels[y * size + x] = new Color32(255, 255, 255, a);
                    }
                    else
                        pixels[y * size + x] = new Color32(255, 255, 255, 0);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            s_RingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            s_RingSprite.name = "RuntimeRing";
            return s_RingSprite;
        }

        private static Sprite s_GradientCircleSprite;
        internal static Sprite CreateGradientCircleSprite()
        {
            if (s_GradientCircleSprite != null) return s_GradientCircleSprite;

            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float center = size / 2f;
            float radius = center;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= radius)
                    {
                        float t = dist / radius;
                        byte brightness = (byte)(255 - (int)(t * 80));
                        float highlight = Mathf.Clamp01(1f - ((dx + dy) / radius + 1f) * 0.3f);
                        brightness = (byte)Mathf.Min(255, brightness + (int)(highlight * 30));

                        float edgeFade = Mathf.Clamp01((radius - dist) * 2f);
                        byte a = (byte)(edgeFade * 255);

                        pixels[y * size + x] = new Color32(brightness, brightness, brightness, a);
                    }
                    else
                        pixels[y * size + x] = new Color32(255, 255, 255, 0);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            s_GradientCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            s_GradientCircleSprite.name = "RuntimeGradientCircle";
            return s_GradientCircleSprite;
        }

        public static VirtualJoystick Create(RectTransform parent, string controlPath, Vector2 anchorPosition)
        {
            var go = new GameObject("MoveJoystick", typeof(RectTransform));
            go.SetActive(false);
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchorPosition;

            var joystick = go.AddComponent<VirtualJoystick>();
            joystick.m_ControlPath = controlPath;
            joystick.movementRange = 75f;

            go.SetActive(true);
            joystick.EnsureVisuals();

            return joystick;
        }
    }
}
