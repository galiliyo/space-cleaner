using System.Collections.Generic;
using UnityEngine;

namespace SpaceCleaner.Core
{
    /// <summary>
    /// Placed on any entity (player or AI) that can pick up buff rings.
    /// Exposes multipliers consumed by SphericalMovement, VacuumCollector, ShootingSystem, and AIOpponent.
    /// </summary>
    public class BuffReceiver : MonoBehaviour
    {
        private const float SpeedMult       = 1.5f;
        private const float VacuumMult      = 3f;
        private const float DamageMult      = 2f;
        private const float AmmoGainMult    = 2f;
        public  const float BuffDuration    = 15f;

        private readonly float[] _timers = new float[3];

        public float SpeedMultiplier        { get; private set; } = 1f;
        public float VacuumRadiusMultiplier { get; private set; } = 1f;
        public float DamageMultiplier       { get; private set; } = 1f;
        public float AmmoGainMultiplier     { get; private set; } = 1f;

        public bool IsActive(BuffType type) => _timers[(int)type] > 0f;
        public float GetRemaining(BuffType type) => _timers[(int)type];

        // ── Static registry so BuffPickup can iterate without FindObjectsByType every frame ──
        private static readonly List<BuffReceiver> s_All = new();
        public static IReadOnlyList<BuffReceiver> All => s_All;

        private void OnEnable()  => s_All.Add(this);
        private void OnDisable() => s_All.Remove(this);

        public void ApplyBuff(BuffType type)
        {
            _timers[(int)type] = BuffDuration;
            Recalculate();
        }

        private void Update()
        {
            bool changed = false;
            for (int i = 0; i < 3; i++)
            {
                if (_timers[i] <= 0f) continue;
                _timers[i] -= Time.deltaTime;
                if (_timers[i] < 0f) _timers[i] = 0f;
                changed = true;
            }
            if (changed) Recalculate();
        }

        private void Recalculate()
        {
            SpeedMultiplier        = _timers[(int)BuffType.Speed]        > 0f ? SpeedMult    : 1f;
            VacuumRadiusMultiplier = _timers[(int)BuffType.VacuumRadius] > 0f ? VacuumMult   : 1f;
            DamageMultiplier       = _timers[(int)BuffType.AmmoStrength] > 0f ? DamageMult   : 1f;
            AmmoGainMultiplier     = _timers[(int)BuffType.AmmoStrength] > 0f ? AmmoGainMult : 1f;
        }
    }
}
