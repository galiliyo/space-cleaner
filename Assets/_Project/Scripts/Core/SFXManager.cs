using System.Collections.Generic;
using UnityEngine;

namespace SpaceCleaner.Core
{
    public class SFXManager : MonoBehaviour
    {
        public static SFXManager Instance { get; private set; }

        private const int SourcePoolSize = 6;
        private AudioSource[] sourcePool;
        private int nextSourceIndex;

        private Dictionary<SFXType, SFXEntry> entries;
        private Dictionary<SFXType, float> lastPlayTime;

        private struct SFXEntry
        {
            public AudioClip[] clips;
            public float volume;
            public float pitchMin;
            public float pitchMax;
            public float cooldown;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("SFXManager");
            DontDestroyOnLoad(go);
            go.AddComponent<SFXManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateSourcePool();
            LoadClips();
        }

        private void CreateSourcePool()
        {
            sourcePool = new AudioSource[SourcePoolSize];
            for (int i = 0; i < SourcePoolSize; i++)
            {
                sourcePool[i] = gameObject.AddComponent<AudioSource>();
                sourcePool[i].playOnAwake = false;
                sourcePool[i].spatialBlend = 0f; // 2D by default
            }
        }

        private void LoadClips()
        {
            entries = new Dictionary<SFXType, SFXEntry>();
            lastPlayTime = new Dictionary<SFXType, float>();

            Register(SFXType.PlayerShoot, 0.45f, 0.05f, "laser5");
            Register(SFXType.ProjectileImpact, 0.55f, 0.1f,
                "impactPlate_light_000", "impactPlate_light_001", "impactPlate_light_002",
                "impactPlate_light_003", "impactPlate_light_004",
                "impactPlate_medium_000", "impactPlate_medium_001", "impactPlate_medium_002",
                "impactPlate_medium_003", "impactPlate_medium_004",
                "impactPlate_heavy_000", "impactPlate_heavy_001", "impactPlate_heavy_002",
                "impactPlate_heavy_003", "impactPlate_heavy_004");
            Register(SFXType.TrashCollected, 0.5f, 0.08f, "pepSound3");
            Register(SFXType.VacuumStart, 0.3f, 0.5f, "phaserUp4");
            Register(SFXType.VacuumStop, 0.25f, 0.5f, "phaserDown1");
            Register(SFXType.PlayerDamage, 0.7f, 0.1f,
                "impactPunch_heavy_000", "impactPunch_heavy_001",
                "impactPunch_heavy_002", "impactPunch_heavy_003",
                "impactPunch_heavy_004");
            Register(SFXType.AIShoot, 0.35f, 0f, "laser7");
            Register(SFXType.AIDeath, 0.65f, 0f, "phaserDown2");
            Register(SFXType.AIPlayerBounce, 0.5f, 0.2f,
                "impactSoft_medium_000", "impactSoft_medium_001",
                "impactSoft_medium_002", "impactSoft_medium_003",
                "impactSoft_medium_004");
            Register(SFXType.AICollectTrash, 0.15f, 0.8f, "spaceTrash2");
            Register(SFXType.LevelComplete, 0.8f, 0f, "threeTone2");
            Register(SFXType.UIClick, 0.6f, 0.1f, "click_003");

            // Procedural buff SFX — generated at startup so they play without needing asset imports.
            RegisterClip(SFXType.BuffCollected, 0.55f, 0f, GenerateBuffCollectedClip());
            RegisterClip(SFXType.BuffExpiring, 0.40f, 0.5f, GenerateBuffExpiringClip());
            RegisterClip(SFXType.BuffExpired,  0.45f, 0f, GenerateBuffExpiredClip());
        }

        private void RegisterClip(SFXType type, float volume, float cooldown, AudioClip clip)
        {
            entries[type] = new SFXEntry
            {
                clips = clip != null ? new[] { clip } : System.Array.Empty<AudioClip>(),
                volume = volume,
                pitchMin = 0.98f,
                pitchMax = 1.02f,
                cooldown = cooldown
            };
            lastPlayTime[type] = -999f;
        }

        // ── Procedural buff clips ─────────────────────────────────────────
        // Each is a short stand-alone synth tone composed of sine waves with an envelope.

        private static AudioClip GenerateBuffCollectedClip()
        {
            // Bright rising arpeggio: A5 → C#6 → E6, ~0.3s total. Triumphant pickup feel.
            const int rate = 22050;
            float duration = 0.3f;
            int n = Mathf.RoundToInt(rate * duration);
            float[] s = new float[n];
            float[] notes = { 880f, 1108f, 1318f }; // A5, C#6, E6
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                int seg = Mathf.Clamp((int)(t / (duration / notes.Length)), 0, notes.Length - 1);
                float local = (t - seg * (duration / notes.Length)) / (duration / notes.Length);
                float env = Mathf.Sin(local * Mathf.PI); // attack-release envelope per note
                float wave = Mathf.Sin(2f * Mathf.PI * notes[seg] * t)
                           + 0.4f * Mathf.Sin(2f * Mathf.PI * notes[seg] * 2f * t); // slight harmonic
                s[i] = wave * env * 0.55f;
            }
            return ClipFromSamples("BuffCollected", s, rate);
        }

        private static AudioClip GenerateBuffExpiringClip()
        {
            // Short two-tone warning chirp at high pitch — "tick tick" feel.
            const int rate = 22050;
            float duration = 0.18f;
            int n = Mathf.RoundToInt(rate * duration);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Exp(-t * 14f); // fast decay
                float freq = t < duration * 0.5f ? 1500f : 1200f;
                s[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.6f;
            }
            return ClipFromSamples("BuffExpiring", s, rate);
        }

        private static AudioClip GenerateBuffExpiredClip()
        {
            // Descending power-down sweep, ~0.45s.
            const int rate = 22050;
            float duration = 0.45f;
            int n = Mathf.RoundToInt(rate * duration);
            float[] s = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float freq = Mathf.Lerp(700f, 180f, t / duration);
                phase += 2f * Mathf.PI * freq / rate;
                float env = 1f - (t / duration); // linear fade-out
                float wave = Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 0.5f);
                s[i] = wave * env * 0.55f;
            }
            return ClipFromSamples("BuffExpired", s, rate);
        }

        private static AudioClip ClipFromSamples(string name, float[] samples, int sampleRate)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void Register(SFXType type, float volume, float cooldown, params string[] clipNames)
        {
            var clips = new List<AudioClip>();
            foreach (var name in clipNames)
            {
                var clip = Resources.Load<AudioClip>("SFX/" + name);
                if (clip != null)
                    clips.Add(clip);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                else
                    Debug.LogWarning($"[SFXManager] Missing clip: SFX/{name}");
#endif
            }

            entries[type] = new SFXEntry
            {
                clips = clips.ToArray(),
                volume = volume,
                pitchMin = 0.95f,
                pitchMax = 1.05f,
                cooldown = cooldown
            };
            lastPlayTime[type] = -999f;
        }

        public void Play(SFXType type)
        {
            if (!entries.TryGetValue(type, out var entry)) return;
            if (entry.clips.Length == 0) return;

            if (entry.cooldown > 0f && Time.unscaledTime - lastPlayTime[type] < entry.cooldown)
                return;
            lastPlayTime[type] = Time.unscaledTime;

            var clip = entry.clips[Random.Range(0, entry.clips.Length)];
            var source = GetNextSource();
            source.spatialBlend = 0f;
            source.pitch = Random.Range(entry.pitchMin, entry.pitchMax);
            source.PlayOneShot(clip, entry.volume);
        }

        public void PlayAtPosition(SFXType type, Vector3 position)
        {
            if (!entries.TryGetValue(type, out var entry)) return;
            if (entry.clips.Length == 0) return;

            if (entry.cooldown > 0f && Time.unscaledTime - lastPlayTime[type] < entry.cooldown)
                return;
            lastPlayTime[type] = Time.unscaledTime;

            var clip = entry.clips[Random.Range(0, entry.clips.Length)];
            var source = GetNextSource();
            source.transform.position = position;
            source.spatialBlend = 0.5f; // partial 3D for positional hint
            source.pitch = Random.Range(entry.pitchMin, entry.pitchMax);
            source.PlayOneShot(clip, entry.volume);
        }

        private AudioSource GetNextSource()
        {
            var source = sourcePool[nextSourceIndex];
            nextSourceIndex = (nextSourceIndex + 1) % SourcePoolSize;
            return source;
        }
    }
}
