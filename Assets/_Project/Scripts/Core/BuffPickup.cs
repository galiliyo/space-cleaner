using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceCleaner.Core
{
    /// <summary>
    /// A large spinning ring that floats at orbit height. Any entity with a BuffReceiver
    /// that moves within CollectRadius of the ring center picks up the buff.
    /// Spawned and managed by BuffManager.
    /// </summary>
    public class BuffPickup : MonoBehaviour
    {
        // Active-pickup registry so consumers (radar, etc.) can iterate without FindObjectsByType.
        private static readonly List<BuffPickup> s_Active = new List<BuffPickup>(3);
        public static IReadOnlyList<BuffPickup> ActiveInstances => s_Active;
        public BuffType Type => _type;

        // ── Visual config ──────────────────────────────────────────────────
        private const float RingRadius    = 3.5f;  // 70% of original 5
        private const float RingTubeWidth = 0.3f;
        private const int   RingSegments  = 48;
        private const float RotateSpeed   = 25f;   // deg/s around surface normal
        private const float IconSize      = 2.0f;  // world units across

        // ── Gameplay config ────────────────────────────────────────────────
        private const float ActiveDuration = 30f;
        private const float CollectRadius  = 3.5f; // matches ring edge — fly through to pick up

        private static readonly Color[] s_Colors =
        {
            new Color(0.2f, 0.8f,  1.0f), // Speed       — cyan
            new Color(0.2f, 1.0f,  0.4f), // VacuumRadius — green
            new Color(1.0f, 0.45f, 0.1f), // AmmoStrength — orange
        };

        // ── State ─────────────────────────────────────────────────────────
        private BuffType  _type;
        private Vector3   _surfaceNormal;
        private float     _timer;
        private Transform _iconTransform; // child quad — billboarded each frame

        public void Initialize(BuffType type, Vector3 position, Vector3 surfaceNormal)
        {
            _type          = type;
            _surfaceNormal = surfaceNormal;
            _timer         = ActiveDuration;

            transform.position = position;

            // Orient so ring stands upright — local XY plane perpendicular to a random tangent
            Vector3 tangent = Vector3.Cross(surfaceNormal, Random.onUnitSphere).normalized;
            if (tangent.sqrMagnitude < 0.01f)
                tangent = Vector3.Cross(surfaceNormal, Vector3.right).normalized;
            transform.rotation = Quaternion.LookRotation(tangent, surfaceNormal);

            BuildRing(s_Colors[(int)type]);
            BuildIcon(type, s_Colors[(int)type]);
        }

        private void BuildIcon(BuffType type, Color color)
        {
            var iconGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            iconGO.name = "Icon";
            // Strip the auto-added MeshCollider — we don't want physics on the icon
            var col = iconGO.GetComponent<MeshCollider>();
            if (col != null) Destroy(col);

            iconGO.transform.SetParent(transform, false);
            iconGO.transform.localPosition = Vector3.zero;
            iconGO.transform.localScale    = Vector3.one * IconSize;

            var mr = iconGO.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows    = false;

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetTexture("_BaseMap", BuffIcons.GetTexture(type));
                mat.SetColor("_BaseColor", color * 2f);
                mat.SetFloat("_Surface", 1f); // transparent
                mat.SetFloat("_Blend",   1f); // additive
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_ZWrite",   0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3001; // above ring
                mr.sharedMaterial = mat;
            }

            _iconTransform = iconGO.transform;
        }

        private void BuildRing(Color color)
        {
            var lr = gameObject.AddComponent<LineRenderer>();
            lr.loop           = true;
            lr.positionCount  = RingSegments;
            lr.startWidth     = RingTubeWidth;
            lr.endWidth       = RingTubeWidth;
            lr.useWorldSpace  = false; // local space — rotates for free with the transform
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;

            // Additive emissive material so the ring glows
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetColor("_BaseColor", color * 2f); // boost brightness
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend",   1f); // additive
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_ZWrite",   0f);
                mat.renderQueue = 3000;
                lr.sharedMaterial = mat;
            }

            // Circle in local XY plane (ring stands perpendicular to local Z)
            for (int i = 0; i < RingSegments; i++)
            {
                float a = i * 2f * Mathf.PI / RingSegments;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * RingRadius, Mathf.Sin(a) * RingRadius, 0f));
            }
        }

        private void Update()
        {
            // Spin around the surface normal
            transform.RotateAround(transform.position, _surfaceNormal, RotateSpeed * Time.deltaTime);

            // Billboard the icon so it always faces the camera, regardless of ring spin
            if (_iconTransform != null)
            {
                var cam = UnityEngine.Camera.main;
                if (cam != null)
                {
                    Vector3 toCam = cam.transform.position - _iconTransform.position;
                    if (toCam.sqrMagnitude > 0.001f)
                        _iconTransform.rotation = Quaternion.LookRotation(-toCam, cam.transform.up);
                }
            }

            // Auto-despawn when timer expires
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                Despawn(collected: false);
                return;
            }

            // Distance-based collection check (avoids Rigidbody requirement for trigger/trigger overlap)
            float rSq = CollectRadius * CollectRadius;
            foreach (var receiver in BuffReceiver.All)
            {
                if ((receiver.transform.position - transform.position).sqrMagnitude <= rSq)
                {
                    receiver.ApplyBuff(_type);
                    SFXManager.Instance?.Play(SFXType.BuffCollected);
                    Despawn(collected: true);
                    return;
                }
            }
        }

        private void Despawn(bool collected)
        {
            BuffManager.Instance?.OnBuffDespawned(_type);
            Destroy(gameObject);
        }

        private void OnEnable()  { if (!s_Active.Contains(this)) s_Active.Add(this); }
        private void OnDisable() { s_Active.Remove(this); }
    }
}
