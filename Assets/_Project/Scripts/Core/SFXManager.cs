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
            Register(SFXType.ProjectileImpact, 0.55f, 0f,
                "impactMetal_medium_000", "impactMetal_medium_001",
                "impactMetal_medium_002", "impactMetal_medium_003",
                "impactMetal_medium_004");
            Register(SFXType.TrashCollected, 0.5f, 0f, "pepSound3");
            Register(SFXType.VacuumStart, 0.3f, 0f, "phaserUp4");
            Register(SFXType.VacuumStop, 0.25f, 0f, "phaserDown1");
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
            Register(SFXType.AICollectTrash, 0.3f, 0f, "spaceTrash2");
            Register(SFXType.LevelComplete, 0.8f, 0f, "threeTone2");
            Register(SFXType.UIClick, 0.6f, 0.1f, "click_003");
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
