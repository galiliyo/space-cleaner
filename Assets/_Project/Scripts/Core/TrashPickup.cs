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

        public void StartCollection(Transform collector, float speed)
        {
            IsBeingCollected = true;
            target = collector;
            moveSpeed = speed;
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
            _activeInstances.Add(this);
        }

        private void OnDisable()
        {
            _activeInstances.Remove(this);
        }
    }
}
