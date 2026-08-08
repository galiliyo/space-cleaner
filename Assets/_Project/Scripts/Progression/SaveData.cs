using System;
using System.Collections.Generic;

namespace SpaceCleaner.Progression
{
    /// <summary>
    /// Serializable save data. Persisted as JSON via SaveSystem.
    /// Plain data only — no UnityEngine references so it stays testable and version-proof.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;

        // Currency
        public int currency;

        // Progression position
        public int currentSolarSystem;
        public int currentPlanetIndex;

        // Lifetime stats (for achievements later)
        public int totalTrashCollected;
        public int totalBossesDefeated;
        public int bestCombo;

        // Unlocks (ship colors, achievements) — filled in by later milestones
        public List<string> unlockedItems = new List<string>();
        public List<string> earnedAchievements = new List<string>();

        // Defeated opponents carried into the boss fight (name + ammo at time of defeat)
        public List<CarryOverEntry> carryOver = new List<CarryOverEntry>();

        [Serializable]
        public class CarryOverEntry
        {
            public string name;
            public int ammo;

            public CarryOverEntry(string name, int ammo)
            {
                this.name = name;
                this.ammo = ammo;
            }
        }

        public static SaveData CreateDefault() => new SaveData
        {
            version = 1,
            currency = 0,
            currentSolarSystem = 0,
            currentPlanetIndex = 0,
            totalTrashCollected = 0,
            totalBossesDefeated = 0,
            bestCombo = 0,
        };
    }
}
