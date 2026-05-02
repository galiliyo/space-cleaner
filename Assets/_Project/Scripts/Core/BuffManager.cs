using UnityEngine;

namespace SpaceCleaner.Core
{
    /// <summary>
    /// Manages one instance of each buff type: spawns them at random orbit positions,
    /// tracks their active state, and enforces the 30-second cooldown after despawn.
    /// Add this component to any persistent GameObject in the Gameplay scene.
    /// Planet is auto-discovered via the "Planet" tag if not assigned in the Inspector.
    /// </summary>
    public class BuffManager : MonoBehaviour
    {
        [SerializeField] private Transform planet;
        [SerializeField] private float orbitRadius = 52f;

        private const float Cooldown = 30f;

        // Per buff-type state
        private readonly float[] _cooldowns = { 0f, 10f, 20f }; // stagger initial spawns
        private readonly bool[]  _active    = { false, false, false };

        public static BuffManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("BuffManager");
            go.AddComponent<BuffManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (planet == null)
            {
                var go = GameObject.FindGameObjectWithTag("Planet");
                if (go != null) planet = go.transform;
            }
        }

        private void Update()
        {
            for (int i = 0; i < 3; i++)
            {
                if (_active[i]) continue;
                _cooldowns[i] -= Time.deltaTime;
                if (_cooldowns[i] <= 0f)
                    Spawn((BuffType)i);
            }
        }

        public void OnBuffDespawned(BuffType type)
        {
            _active[(int)type]    = false;
            _cooldowns[(int)type] = Cooldown;
        }

        private void Spawn(BuffType type)
        {
            if (planet == null) return;
            _active[(int)type]    = true;
            _cooldowns[(int)type] = 0f;

            Vector3 dir = Random.onUnitSphere;
            Vector3 pos = planet.position + dir * orbitRadius;

            var go     = new GameObject($"Buff_{type}");
            var pickup = go.AddComponent<BuffPickup>();
            pickup.Initialize(type, pos, dir);
        }
    }
}
