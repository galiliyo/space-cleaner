using UnityEngine;

namespace SpaceCleaner.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Level State")]
        [SerializeField] private int totalTrashCount;
        [SerializeField] private int collectedTrashCount;
        [SerializeField] private bool opponentAlive = true;

        [Header("Completion")]
        [Tooltip("Fraction of trash that must be collected to win (0-1). Default 0.8 = 80%.")]
        [SerializeField, Range(0.5f, 1f)] private float cleanupThreshold = 0.8f;

        public int TotalTrashCount => totalTrashCount;
        public int CollectedTrashCount => collectedTrashCount;
        public float CleanupPercentage => totalTrashCount > 0 ? (float)collectedTrashCount / totalTrashCount : 0f;
        public bool OpponentAlive => opponentAlive;
        public bool IsLevelComplete => CleanupPercentage >= cleanupThreshold && !opponentAlive;

        public float CleanupThreshold => cleanupThreshold;

        public event System.Action<float> OnCleanupChanged;
        public event System.Action OnLevelComplete;
        public event System.Action<bool> OnOpponentStateChanged;
        public event System.Action OnPlayerDeath;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void InitializeLevel(int trashCount)
        {
            totalTrashCount = trashCount;
            collectedTrashCount = 0;
            opponentAlive = true;
            OnCleanupChanged?.Invoke(0f);
        }

        public void RegisterTrashCollected()
        {
            collectedTrashCount++;
            OnCleanupChanged?.Invoke(CleanupPercentage);
            CheckLevelComplete();
        }

        public void RegisterOpponentKilled()
        {
            opponentAlive = false;
            OnOpponentStateChanged?.Invoke(false);
            CheckLevelComplete();
        }

        private void CheckLevelComplete()
        {
            if (IsLevelComplete)
            {
                SFXManager.Instance?.Play(SFXType.LevelComplete);
                OnLevelComplete?.Invoke();
            }
        }

        public void NotifyPlayerDeath()
        {
            OnPlayerDeath?.Invoke();
        }

        public void PauseGame()
        {
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
        }
    }
}
