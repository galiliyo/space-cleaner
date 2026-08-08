using UnityEngine;

namespace SpaceCleaner.Core
{
    /// <summary>
    /// Haptic feedback manager. Provides vibrations for key gameplay moments.
    /// Wraps Unity's Handheld.Vibrate with duration control via patterns.
    /// </summary>
    public class HapticManager : MonoBehaviour
    {
        public static HapticManager Instance { get; private set; }
        
        [Header("Haptic Settings")]
        [SerializeField] private bool enableHaptics = true;
        [SerializeField] private bool enableOnAndroid = true;
        [SerializeField] private bool enableOniOS = true;
        
        [Header("Patterns (in milliseconds)")]
        [SerializeField] private int shootDuration = 15;
        [SerializeField] private int collectDuration = 30;
        [SerializeField] private int damageDuration = 100;
        [SerializeField] private int levelCompleteDuration = 200;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        /// <summary>
        /// Triggers a haptic pulse of specified intensity.
        /// </summary>
        public void Trigger(HapticType type)
        {
            if (!enableHaptics) return;
            if (!IsMobilePlatform()) return;
            
            int duration = GetDuration(type);
            
#if UNITY_ANDROID && !UNITY_EDITOR
            if (enableOnAndroid)
                AndroidVibrate(duration);
#elif UNITY_IOS && !UNITY_EDITOR
            if (enableOniOS)
                Handheld.Vibrate();
#else
            // Editor - log only
            // Debug.Log($"[Haptic] {type} ({duration}ms)");
#endif
        }
        
        /// <summary>
        /// Android-specific vibration with duration control using native interface.
        /// </summary>
        private void AndroidVibrate(int milliseconds)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null)
                    {
                        // Vibrate for milliseconds (API 26+ pattern)
                        vibrator.Call("vibrate", (long)milliseconds);
                    }
                }
            }
            catch
            {
                // Fallback to simple vibrate
                Handheld.Vibrate();
            }
#else
            Handheld.Vibrate();
#endif
        }
        
        private int GetDuration(HapticType type)
        {
            switch (type)
            {
                case HapticType.Shoot: return shootDuration;
                case HapticType.Collect: return collectDuration;
                case HapticType.Damage: return damageDuration;
                case HapticType.LevelComplete: return levelCompleteDuration;
                default: return 50;
            }
        }
        
        private bool IsMobilePlatform()
        {
#if UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return false;
#endif
        }
    }
    
    public enum HapticType
    {
        Shoot,
        Collect,
        Damage,
        LevelComplete
    }
}
