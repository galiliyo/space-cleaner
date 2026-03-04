using UnityEngine;
using SpaceCleaner.Core;

namespace SpaceCleaner.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class VacuumCollector : MonoBehaviour
    {
        [Header("Collection")]
        [SerializeField] private float collectRadius = 5f;
        [SerializeField] private float lerpSpeed = 15f;
        [SerializeField] private LayerMask trashLayer;

        private PlayerController playerController;
        private SphereCollider vacuumTrigger;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();

            // Add sphere trigger for vacuum collection
            vacuumTrigger = gameObject.AddComponent<SphereCollider>();
            vacuumTrigger.isTrigger = true;
            vacuumTrigger.radius = collectRadius;
        }

        private void OnTriggerStay(Collider other)
        {
            if (((1 << other.gameObject.layer) & trashLayer) == 0) return;

            var trash = other.GetComponent<TrashPickup>();
            if (trash != null && !trash.IsBeingCollected)
            {
                trash.StartCollection(transform, lerpSpeed);
            }
        }
    }
}
