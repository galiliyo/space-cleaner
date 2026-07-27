using UnityEngine;

namespace SpaceCleaner.Core
{
    /// <summary>
    /// Generates and caches procedural 64x64 white-on-transparent icon textures and sprites
    /// for each buff type. Icons are tinted at draw-time by the consumer (BuffPickup, BuffHUD).
    /// </summary>
    public static class BuffIcons
    {
        private const int Size = 64;
        private static readonly Texture2D[] s_Textures = new Texture2D[3];
        private static readonly Sprite[]    s_Sprites  = new Sprite[3];

        public static Texture2D GetTexture(BuffType type)
        {
            int i = (int)type;
            if (s_Textures[i] == null) s_Textures[i] = Build(type);
            return s_Textures[i];
        }

        public static Sprite GetSprite(BuffType type)
        {
            int i = (int)type;
            if (s_Sprites[i] == null)
            {
                var tex = GetTexture(type);
                s_Sprites[i] = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 64f);
            }
            return s_Sprites[i];
        }

        private static Texture2D Build(BuffType type)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp
            };

            var px = new Color[Size * Size];
            // start fully transparent
            for (int k = 0; k < px.Length; k++) px[k] = new Color(1, 1, 1, 0);

            switch (type)
            {
                case BuffType.Speed:        DrawLightning(px); break;
                case BuffType.VacuumRadius: DrawConcentricRings(px); break;
                case BuffType.AmmoStrength: DrawStarburst(px); break;
            }

            tex.SetPixels(px);
            tex.Apply(false, false);
            return tex;
        }

        // ── Icon shapes ───────────────────────────────────────────────────

        // Lightning bolt: zigzag stroke through the middle
        private static void DrawLightning(Color[] px)
        {
            // Three control points form the zigzag (top-right → middle-left → bottom-right)
            var p0 = new Vector2(40, 56);
            var p1 = new Vector2(24, 36);
            var p2 = new Vector2(40, 30);
            var p3 = new Vector2(20,  8);
            StrokeLine(px, p0, p1, 5f);
            StrokeLine(px, p1, p2, 5f);
            StrokeLine(px, p2, p3, 5f);
        }

        // Two concentric ring outlines
        private static void DrawConcentricRings(Color[] px)
        {
            DrawRing(px, 32, 32, outer: 28, inner: 24);
            DrawRing(px, 32, 32, outer: 16, inner: 12);
        }

        // 4-pointed star burst
        private static void DrawStarburst(Color[] px)
        {
            // Cross spokes (vertical + horizontal)
            StrokeLine(px, new Vector2(32, 6),  new Vector2(32, 58), 4f);
            StrokeLine(px, new Vector2(6,  32), new Vector2(58, 32), 4f);
            // Diagonal shorter spokes
            StrokeLine(px, new Vector2(14, 14), new Vector2(50, 50), 3f);
            StrokeLine(px, new Vector2(50, 14), new Vector2(14, 50), 3f);
            // Center filled dot
            DrawDisc(px, 32, 32, radius: 6f);
        }

        // ── Drawing primitives ────────────────────────────────────────────

        private static void StrokeLine(Color[] px, Vector2 a, Vector2 b, float width)
        {
            float halfW = width * 0.5f;
            int xMin = Mathf.Max(0, (int)(Mathf.Min(a.x, b.x) - halfW - 1));
            int xMax = Mathf.Min(Size - 1, (int)(Mathf.Max(a.x, b.x) + halfW + 1));
            int yMin = Mathf.Max(0, (int)(Mathf.Min(a.y, b.y) - halfW - 1));
            int yMax = Mathf.Min(Size - 1, (int)(Mathf.Max(a.y, b.y) + halfW + 1));

            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    Vector2 ap = new Vector2(x - a.x, y - a.y);
                    float t = lenSq > 0f ? Mathf.Clamp01(Vector2.Dot(ap, ab) / lenSq) : 0f;
                    Vector2 closest = a + ab * t;
                    float d = Vector2.Distance(new Vector2(x, y), closest);
                    float alpha = Mathf.Clamp01(1f - (d - halfW + 0.5f));
                    if (alpha <= 0f) continue;
                    int idx = y * Size + x;
                    if (alpha > px[idx].a) px[idx] = new Color(1, 1, 1, alpha);
                }
            }
        }

        private static void DrawRing(Color[] px, int cx, int cy, float outer, float inner)
        {
            int xMin = Mathf.Max(0, (int)(cx - outer - 1));
            int xMax = Mathf.Min(Size - 1, (int)(cx + outer + 1));
            int yMin = Mathf.Max(0, (int)(cy - outer - 1));
            int yMax = Mathf.Min(Size - 1, (int)(cy + outer + 1));
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    // alpha = 1 inside [inner, outer], antialias on both edges
                    float a = Mathf.Clamp01(d - inner + 0.5f) * Mathf.Clamp01(outer - d + 0.5f);
                    if (a <= 0f) continue;
                    int idx = y * Size + x;
                    if (a > px[idx].a) px[idx] = new Color(1, 1, 1, a);
                }
            }
        }

        private static void DrawDisc(Color[] px, int cx, int cy, float radius)
        {
            int xMin = Mathf.Max(0, (int)(cx - radius - 1));
            int xMax = Mathf.Min(Size - 1, (int)(cx + radius + 1));
            int yMin = Mathf.Max(0, (int)(cy - radius - 1));
            int yMax = Mathf.Min(Size - 1, (int)(cy + radius + 1));
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    if (a <= 0f) continue;
                    int idx = y * Size + x;
                    if (a > px[idx].a) px[idx] = new Color(1, 1, 1, a);
                }
            }
        }
    }
}
