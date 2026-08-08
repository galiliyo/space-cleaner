using System.IO;
using UnityEngine;

namespace SpaceCleaner.Progression
{
    /// <summary>
    /// JSON save/load to Application.persistentDataPath. Static, no scene object needed.
    /// GDD §10.1: JSON serialization. Auto-save hooks live in ProgressionManager.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "savegame.json";

        public static string SavePath =>
            Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSave => File.Exists(SavePath);

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                // Write to temp then move — avoids a corrupted save if the app dies mid-write.
                string tmp = SavePath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(SavePath))
                    File.Delete(SavePath);
                File.Move(tmp, SavePath);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[SaveSystem] Saved to {SavePath}");
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
            }
        }

        public static SaveData Load()
        {
            if (!HasSave)
                return SaveData.CreateDefault();

            try
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                return data ?? SaveData.CreateDefault();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed, starting fresh: {e.Message}");
                return SaveData.CreateDefault();
            }
        }

        public static void Delete()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
    }
}
