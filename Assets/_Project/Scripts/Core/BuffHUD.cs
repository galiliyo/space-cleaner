using UnityEngine;
using UnityEngine.UI;
using SpaceCleaner.Player;

namespace SpaceCleaner.Core
{
    /// <summary>
    /// Auto-spawned screen-space overlay that shows three buff slots in the top-right corner.
    /// Each slot dims when inactive, lights up with a radial countdown when active,
    /// and pulses + plays SFX when expiring or just expired.
    /// </summary>
    public class BuffHUD : MonoBehaviour
    {
        // ── Layout (pixels at 1080-wide reference) ────────────────────────
        private const float SlotSize    = 96f;
        private const float SlotSpacing = 12f;
        private const float MarginRight = 24f;
        private const float MarginTop   = 24f;
        private const float ExpiringWindow = 3f;   // seconds before expiry → flash + chime

        // ── Per-buff colors (must match BuffPickup) ───────────────────────
        private static readonly Color[] s_Colors =
        {
            new Color(0.2f, 0.8f,  1.0f),
            new Color(0.2f, 1.0f,  0.4f),
            new Color(1.0f, 0.45f, 0.1f),
        };
        private static readonly Color s_InactiveTint = new Color(0.45f, 0.45f, 0.45f, 0.45f);

        private BuffReceiver _receiver;
        private Slot[] _slots = new Slot[3];

        private struct Slot
        {
            public Image Icon;          // tinted icon, full color when active, grey when inactive
            public Image Countdown;     // radial fill ring overlay
            public bool  WasActive;     // tracks transitions (active → inactive) for SFX
            public bool  WarnedExpiring;// chime fired this cycle
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (FindAnyObjectByType<BuffHUD>() != null) return;
            var go = new GameObject("BuffHUD");
            DontDestroyOnLoad(go);
            go.AddComponent<BuffHUD>();
        }

        private void Start()
        {
            BuildCanvas();
        }

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("BuffHUDCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Container anchored top-right
            var container = new GameObject("Slots", typeof(RectTransform));
            container.transform.SetParent(canvasGO.transform, false);
            var rt = (RectTransform)container.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-MarginRight, -MarginTop);
            float totalW = SlotSize * 3 + SlotSpacing * 2;
            rt.sizeDelta = new Vector2(totalW, SlotSize);

            for (int i = 0; i < 3; i++)
                _slots[i] = BuildSlot((BuffType)i, container.transform, i);
        }

        private Slot BuildSlot(BuffType type, Transform parent, int slotIndex)
        {
            var slotGO = new GameObject($"Slot_{type}", typeof(RectTransform));
            slotGO.transform.SetParent(parent, false);
            var srt = (RectTransform)slotGO.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(0f, 0.5f);
            srt.pivot     = new Vector2(0f, 0.5f);
            float x = slotIndex * (SlotSize + SlotSpacing);
            srt.anchoredPosition = new Vector2(x, 0f);
            srt.sizeDelta = new Vector2(SlotSize, SlotSize);

            // Icon
            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(slotGO.transform, false);
            var iconRT = (RectTransform)iconGO.transform;
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.sprite = BuffIcons.GetSprite(type);
            iconImg.color  = s_InactiveTint;
            iconImg.raycastTarget = false;

            // Countdown radial overlay (uses the same icon sprite, draws on top with fillAmount)
            var countGO = new GameObject("Countdown", typeof(RectTransform));
            countGO.transform.SetParent(slotGO.transform, false);
            var countRT = (RectTransform)countGO.transform;
            countRT.anchorMin = Vector2.zero;
            countRT.anchorMax = Vector2.one;
            countRT.offsetMin = countRT.offsetMax = Vector2.zero;
            var countImg = countGO.AddComponent<Image>();
            countImg.sprite     = BuffIcons.GetSprite(type);
            countImg.color      = s_Colors[(int)type];
            countImg.type       = Image.Type.Filled;
            countImg.fillMethod = Image.FillMethod.Radial360;
            countImg.fillOrigin = (int)Image.Origin360.Top;
            countImg.fillClockwise = false;
            countImg.fillAmount = 0f;
            countImg.raycastTarget = false;

            return new Slot { Icon = iconImg, Countdown = countImg };
        }

        private void Update()
        {
            if (_receiver == null) AcquireReceiver();
            if (_receiver == null) return;

            for (int i = 0; i < 3; i++)
            {
                var type      = (BuffType)i;
                float remain  = _receiver.GetRemaining(type);
                bool  active  = remain > 0f;
                ref var slot  = ref _slots[i];

                if (active)
                {
                    // Pickup transition (was inactive last frame)
                    if (!slot.WasActive)
                        slot.WarnedExpiring = false;

                    // Steady active state
                    slot.Icon.color      = s_Colors[i];
                    slot.Countdown.fillAmount = remain / BuffReceiver.BuffDuration;

                    // Expiring flash (last 3s) — pulse alpha at ~3 Hz
                    if (remain <= ExpiringWindow)
                    {
                        if (!slot.WarnedExpiring)
                        {
                            SFXManager.Instance?.Play(SFXType.BuffExpiring);
                            slot.WarnedExpiring = true;
                        }
                        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 18f);
                        var c = s_Colors[i];
                        c.a *= Mathf.Lerp(0.4f, 1f, pulse);
                        slot.Icon.color = c;
                    }
                }
                else
                {
                    // Expiry transition
                    if (slot.WasActive)
                        SFXManager.Instance?.Play(SFXType.BuffExpired);

                    slot.Icon.color           = s_InactiveTint;
                    slot.Countdown.fillAmount = 0f;
                    slot.WarnedExpiring       = false;
                }

                slot.WasActive = active;
            }
        }

        private void AcquireReceiver()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            _receiver = player.GetComponent<BuffReceiver>();
        }
    }
}
