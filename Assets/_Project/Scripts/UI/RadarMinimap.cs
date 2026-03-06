using UnityEngine;
using UnityEngine.UI;
using SpaceCleaner.Player;
using SpaceCleaner.Enemies;

namespace SpaceCleaner.UI
{
    public class RadarMinimap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform planet;
        [SerializeField] private Transform player;
        [SerializeField] private RectTransform radarRect; // the circular radar area
        [SerializeField] private Image backgroundImage;

        [Header("Settings")]
        [SerializeField] private float radarRangeAngle = 90f; // degrees of spherical coverage
        [SerializeField] private float updateInterval = 0.1f; // seconds between updates
        [SerializeField] private int maxTrashDots = 30; // max visible trash dots on radar
        [SerializeField] private Color trashColor = new Color(0.2f, 0.9f, 0.3f, 0.8f);
        [SerializeField] private Color opponentColor = new Color(1f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color playerColor = new Color(1f, 1f, 1f, 0.9f);

        [Header("Dot Sizes")]
        [SerializeField] private float trashDotSize = 4f;
        [SerializeField] private float opponentDotSize = 8f;
        [SerializeField] private float playerDotSize = 6f;

        private Image[] trashDots;
        private Image opponentDot;
        private RectTransform playerDot;
        private float updateTimer;
        private Transform opponentTransform;
        private float radarRadius;

        public void SetReferences(Transform planetRef, Transform playerRef, RectTransform rect, Image bgImg)
        {
            planet = planetRef;
            player = playerRef;
            radarRect = rect;
            backgroundImage = bgImg;
        }

        private void Start()
        {
            radarRadius = radarRect.rect.width * 0.45f; // slightly inside edge

            // Find opponent
            var ai = FindAnyObjectByType<AIOpponent>();
            if (ai != null) opponentTransform = ai.transform;

            // Find player if not assigned
            if (player == null)
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null) player = pc.transform;
            }

            CreateDots();

            if (backgroundImage != null)
            {
                var c = backgroundImage.color;
                c.a = 0.3f;
                backgroundImage.color = c;
            }
        }

        private void CreateDots()
        {
            // Player direction indicator (triangle-ish via small rotated square)
            var playerGo = CreateDotObject("PlayerDot", playerColor, playerDotSize);
            playerDot = playerGo.GetComponent<RectTransform>();
            playerDot.anchoredPosition = Vector2.zero; // always center

            // Opponent dot
            var oppGo = CreateDotObject("OpponentDot", opponentColor, opponentDotSize);
            opponentDot = oppGo.GetComponent<Image>();
            opponentDot.enabled = false;

            // Trash dots pool
            trashDots = new Image[maxTrashDots];
            for (int i = 0; i < maxTrashDots; i++)
            {
                var go = CreateDotObject($"TrashDot_{i}", trashColor, trashDotSize);
                trashDots[i] = go.GetComponent<Image>();
                trashDots[i].enabled = false;
            }
        }

        private GameObject CreateDotObject(string name, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(radarRect, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            var img = go.GetComponent<Image>();
            img.color = color;
            return go;
        }

        private void Update()
        {
            if (player == null || planet == null) return;

            updateTimer -= Time.deltaTime;
            if (updateTimer > 0f) return;
            updateTimer = updateInterval;

            UpdateRadar();
        }

        private void UpdateRadar()
        {
            Vector3 playerUp = (player.position - planet.position).normalized;

            // Build a local coordinate frame on the sphere at the player's position
            // "north" is an arbitrary but stable reference direction
            Vector3 playerForward = Vector3.ProjectOnPlane(player.forward, playerUp).normalized;
            if (playerForward.sqrMagnitude < 0.001f)
                playerForward = Vector3.ProjectOnPlane(Vector3.forward, playerUp).normalized;
            Vector3 playerRight = Vector3.Cross(playerUp, playerForward).normalized;

            float cosRange = Mathf.Cos(radarRangeAngle * Mathf.Deg2Rad);

            // Update opponent
            if (opponentTransform != null && opponentDot != null)
            {
                Vector2 oppPos = WorldToRadar(opponentTransform.position, playerUp, playerForward, playerRight, cosRange);
                if (oppPos.sqrMagnitude <= 1.01f) // within range (normalized to 0-1)
                {
                    opponentDot.enabled = true;
                    opponentDot.rectTransform.anchoredPosition = oppPos * radarRadius;
                }
                else
                {
                    // Clamp to edge if out of range
                    opponentDot.enabled = true;
                    opponentDot.rectTransform.anchoredPosition = oppPos.normalized * radarRadius;
                    var c = opponentColor;
                    c.a = 0.4f;
                    opponentDot.color = c;
                }
            }

            // Update trash dots
            var trashObjects = FindObjectsByType<Core.TrashPickup>(FindObjectsSortMode.None);
            int dotIndex = 0;

            for (int i = 0; i < trashObjects.Length && dotIndex < maxTrashDots; i++)
            {
                if (trashObjects[i].IsBeingCollected) continue;

                Vector2 pos = WorldToRadar(trashObjects[i].transform.position, playerUp, playerForward, playerRight, cosRange);
                if (pos.sqrMagnitude > 1.01f) continue; // out of radar range

                trashDots[dotIndex].enabled = true;
                trashDots[dotIndex].rectTransform.anchoredPosition = pos * radarRadius;
                dotIndex++;
            }

            // Hide unused dots
            for (int i = dotIndex; i < maxTrashDots; i++)
            {
                trashDots[i].enabled = false;
            }
        }

        /// <summary>
        /// Projects a world position onto the 2D radar plane.
        /// Returns normalized position (-1 to 1 range) relative to radar center.
        /// </summary>
        private Vector2 WorldToRadar(Vector3 worldPos, Vector3 playerUp, Vector3 playerFwd, Vector3 playerRight, float cosRange)
        {
            Vector3 dir = (worldPos - planet.position).normalized;
            float dot = Vector3.Dot(dir, playerUp);

            // Angle from player on sphere surface
            float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f));
            float maxAngle = radarRangeAngle * Mathf.Deg2Rad;

            if (angle > maxAngle)
            {
                // Project direction but return magnitude > 1 to indicate out of range
                Vector3 projected = Vector3.ProjectOnPlane(dir, playerUp).normalized;
                float x = Vector3.Dot(projected, playerRight);
                float y = Vector3.Dot(projected, playerFwd);
                return new Vector2(x, y) * 1.5f;
            }

            // Normalize distance: 0 at player, 1 at radar edge
            float normalizedDist = angle / maxAngle;

            // Project onto tangent plane to get 2D direction
            Vector3 tangentDir = Vector3.ProjectOnPlane(dir, playerUp).normalized;
            float px = Vector3.Dot(tangentDir, playerRight);
            float py = Vector3.Dot(tangentDir, playerFwd);

            return new Vector2(px, py) * normalizedDist;
        }
    }
}
