using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SpaceCleaner.Player;

namespace SpaceCleaner.Core
{
    public class TrashPickup : MonoBehaviour
    {
        private static readonly List<TrashPickup> _activeInstances = new List<TrashPickup>(256);
        public static IReadOnlyList<TrashPickup> ActiveInstances => _activeInstances;

        /// <summary>Max seconds a trash item can chase a collector before giving up.</summary>
        private const float MaxCollectionTime = 4f;

        public bool IsBeingCollected { get; private set; }

        /// <summary>
        /// When false, collecting this trash does not count toward level cleanup progress.
        /// Used for trash converted from missed player projectiles.
        /// </summary>
        public bool CountsForProgress { get; set; } = true;

        [SerializeField] private int ammoValue = 1;
        public int AmmoValue { get => ammoValue; set => ammoValue = value; }

        private Transform target;
        private float moveSpeed;
        private float collectionTimer;
        private int _registryIndex = -1;

        public void StartCollection(Transform collector, float speed)
        {
            IsBeingCollected = true;
            target = collector;
            moveSpeed = speed;
            collectionTimer = 0f;
        }

        private void Update()
        {
            if (!IsBeingCollected || target == null) return;

            collectionTimer += Time.deltaTime;

            // If the trash has been chasing too long, cancel so it can be re-collected
            if (collectionTimer > MaxCollectionTime)
            {
                CancelCollection();
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.5f)
            {
                CompleteCollection();
            }
        }

        private void CancelCollection()
        {
            IsBeingCollected = false;
            target = null;
            moveSpeed = 0f;
        }

        private void CompleteCollection()
        {
            IsBeingCollected = false;
            var player = target.GetComponent<PlayerController>();
            if (player != null)
            {
                player.AddAmmo(ammoValue);
            }

            SFXManager.Instance?.Play(SFXType.TrashCollected);
            if (CountsForProgress)
                GameManager.Instance?.RegisterTrashCollected();
            ObjectPool.ReturnOrDestroy(gameObject);
        }

        /// <summary>
        /// Resets state when retrieved from the pool so the object behaves as freshly spawned.
        /// Called automatically via OnEnable (pool sets active after positioning).
        /// </summary>
        private void OnEnable()
        {
            // Disable shadows on trash (same pattern as AIOpponent/LevelSetup)
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = ShadowCastingMode.Off;

            IsBeingCollected = false;
            target = null;
            moveSpeed = 0f;
            collectionTimer = 0f;
            _registryIndex = _activeInstances.Count;
            _activeInstances.Add(this);
            CountsForProgress = true;
            ammoValue = 1;
        }

        private void OnDisable()
        {
            if (_registryIndex >= 0 && _registryIndex < _activeInstances.Count && _activeInstances[_registryIndex] == this)
            {
                int last = _activeInstances.Count - 1;
                if (_registryIndex != last)
                {
                    _activeInstances[_registryIndex] = _activeInstances[last];
                    _activeInstances[_registryIndex]._registryIndex = _registryIndex;
                }
                _activeInstances.RemoveAt(last);
            }
            _registryIndex = -1;
        }
    }
}
