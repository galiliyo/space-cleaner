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

        public int TotalTrashCount => totalTrashCount;
        public int CollectedTrashCount => collectedTrashCount;
        public float CleanupPercentage => totalTrashCount > 0 ? (float)collectedTrashCount / totalTrashCount : 0f;
        public bool OpponentAlive => opponentAlive;
        public bool IsLevelComplete => CleanupPercentage >= 1f && !opponentAlive;

        public event System.Action<float> OnCleanupChanged;
        public event System.Action OnLevelComplete;
        public event System.Action<bool> OnOpponentStateChanged;

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
                OnLevelComplete?.Invoke();
            }
        }
    }
}
