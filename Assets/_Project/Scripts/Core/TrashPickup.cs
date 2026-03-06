using UnityEngine;
using SpaceCleaner.Player;

namespace SpaceCleaner.Core
{
    public class TrashPickup : MonoBehaviour
    {
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
            Destroy(gameObject); // TODO: Return to pool instead
        }
    }
}
