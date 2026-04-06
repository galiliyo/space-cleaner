using System.Collections.Generic;

namespace SpaceCleaner.Boss
{
    /// <summary>
    /// Accumulates defeated opponent data during a planet run.
    /// BossFightManager retrieves and clears entries at boss arena start.
    /// No persistence — data lives in memory for the duration of a solar system run.
    /// </summary>
    public static class CarryOverData
    {
        private static readonly List<(string name, int ammo)> s_Entries = new();

        public static void Record(string name, int ammo)
        {
            s_Entries.Add((name, ammo));
        }

        /// <summary>
        /// Returns all stored entries and clears the list.
        /// </summary>
        public static List<(string name, int ammo)> GetAndClear()
        {
            var result = new List<(string name, int ammo)>(s_Entries);
            s_Entries.Clear();
            return result;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearStaticState()
        {
            s_Entries.Clear();
        }
    }
}
