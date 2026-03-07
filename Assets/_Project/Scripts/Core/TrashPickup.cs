using System.Collections.Generic;
using UnityEngine;
using SpaceCleaner.Player;

namespace SpaceCleaner.Core
{
    public class TrashPickup : MonoBehaviour
    {
        private static readonly List<TrashPickup> _activeInstances = new List<TrashPickup>(256);
        public static IReadOnlyList<TrashPickup> ActiveInstances => _activeInstances;

        public bool IsBeingCollected { get; private set; }

        private Transform target;
        private float moveSpeed;
        private int _registryIndex = -1;

        private void Awake() { enabled = false; }

        public void StartCollection(Transform collector, float speed)
        {
            IsBeingCollected = true;
            target = collector;
            moveSpeed = speed;
            enabled = true;
        }

        private void Update()
        {
            if (!IsBeingCollected || target == null) return;

            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.5f)
            {
                CompleteCollection();
            }
        }

        private void CompleteCollection()
        {
            enabled = false;
            var player = target.GetComponent<PlayerController>();
            if (player != null)
            {
                player.AddAmmo(1);
            }

            GameManager.Instance?.RegisterTrashCollected();
            ObjectPool.ReturnOrDestroy(gameObject);
        }

        /// <summary>
        /// Resets state when retrieved from the pool so the object behaves as freshly spawned.
        /// Called automatically via OnEnable (pool sets active after positioning).
        /// </summary>
        private void OnEnable()
        {
            IsBeingCollected = false;
            target = null;
            moveSpeed = 0f;
            enabled = false;
            _registryIndex = _activeInstances.Count;
            _activeInstances.Add(this);
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
