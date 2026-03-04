using UnityEngine;
using SpaceCleaner.Player;
using SpaceCleaner.Camera;
using SpaceCleaner.Enemies;

namespace SpaceCleaner.Core
{
    public class LevelSetup : MonoBehaviour
    {
        [Header("Planet")]
        [SerializeField] private Transform planet;
        [SerializeField] private float planetRadius = 50f;
        [SerializeField] private float hoverHeight = 2f;

        [Header("References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private SphericalCamera sphericalCamera;
        [SerializeField] private AIOpponent aiOpponent;

        private void Start()
        {
            float orbitRadius = planetRadius + hoverHeight;

            // Setup player
            if (player != null)
            {
                var movement = player.GetComponent<SphericalMovement>();
                if (movement != null)
                    movement.SetPlanet(planet, orbitRadius);

                // Position player on top of planet
                player.transform.position = planet.position + Vector3.up * orbitRadius;
                player.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            }

            // Setup camera
            if (sphericalCamera != null && player != null)
            {
                sphericalCamera.SetTarget(player.transform, planet);
            }

            // Setup AI opponent on opposite side of planet
            if (aiOpponent != null)
            {
                aiOpponent.transform.position = planet.position + Vector3.down * orbitRadius;
                aiOpponent.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.down);
            }
        }
    }
}
