using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceCleaner.Core
{
    /// <summary>
    /// Handles mobile-specific concerns: frame rate, app lifecycle, memory pressure, and idle throttling.
    /// Auto-creates via [RuntimeInitializeOnLoadMethod] — no manual scene wiring needed.
    /// Lifecycle and idle throttling only activate on mobile platforms (not in the Editor).
    /// </summary>
    public class MobileManager : MonoBehaviour
    {
        private const int TargetFrameRate = 60;
        private const int IdleFrameRate = 15;
        private const float IdleTimeout = 5f;

        private static MobileManager _instance;

        private float _savedTimeScale = 1f;
        private bool _isPaused;
        private float _lastInputTime;
        private bool _isIdle;
        private bool _isMobilePlatform;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (_instance != null) return;

            var go = new GameObject("[MobileManager]");
            _instance = go.AddComponent<MobileManager>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID || UNITY_IOS
            _isMobilePlatform = true;
#else
            _isMobilePlatform = false;
#endif
        }

        private void Start()
        {
            if (_isMobilePlatform)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = TargetFrameRate;
            }

            Application.lowMemory += OnLowMemory;
            _lastInputTime = Time.unscaledTime;
        }

        private void OnDestroy()
        {
            Application.lowMemory -= OnLowMemory;
        }

        private void Update()
        {
            // Idle throttling only on mobile
            if (!_isMobilePlatform) return;

            // Use New Input System (legacy Input is disabled in this project)
            bool hasInput = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
            if (!hasInput && Gamepad.current != null)
                hasInput = Gamepad.current.wasUpdatedThisFrame;
            if (!hasInput && Keyboard.current != null)
                hasInput = Keyboard.current.anyKey.isPressed;

            if (hasInput)
            {
                _lastInputTime = Time.unscaledTime;

                if (_isIdle)
                {
                    _isIdle = false;
                    Application.targetFrameRate = TargetFrameRate;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log("[MobileManager] Input detected, restoring frame rate to " + TargetFrameRate);
#endif
                }
            }
            else if (!_isIdle && Time.unscaledTime - _lastInputTime >= IdleTimeout)
            {
                _isIdle = true;
                Application.targetFrameRate = IdleFrameRate;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[MobileManager] Idle detected, throttling frame rate to " + IdleFrameRate);
#endif
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // Only handle lifecycle on actual mobile devices
            if (!_isMobilePlatform) return;

            if (pauseStatus)
            {
                _savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                AudioListener.pause = true;
                _isPaused = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[MobileManager] Application paused. TimeScale saved: " + _savedTimeScale);
#endif
            }
            else
            {
                Time.timeScale = _savedTimeScale;
                AudioListener.pause = false;
                _isPaused = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[MobileManager] Application resumed. TimeScale restored: " + _savedTimeScale);
#endif
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!_isMobilePlatform) return;

            if (!hasFocus)
            {
                AudioListener.pause = true;
            }
            else
            {
                if (!_isPaused)
                {
                    AudioListener.pause = false;
                }
            }
        }

        private void OnLowMemory()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("[MobileManager] Low memory warning! Unloading unused assets and collecting garbage.");
#endif

            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
    }
}
