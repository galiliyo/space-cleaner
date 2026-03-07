using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceCleaner.Player
{
    [RequireComponent(typeof(SphericalMovement))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Ammo")]
        [SerializeField] private int ammoCount;
        [SerializeField] private int softCap = 50;
        [SerializeField] private float overflowDecayRate = 2f; // ammo lost per second when over cap

        private SphericalMovement movement;
        private ShootingSystem shooting;
        private InputAction moveAction;
        private InputAction aimAction;
        private float overflowDecayTimer;

        public int AmmoCount => ammoCount;
        public int SoftCap => softCap;

        public event System.Action<int> OnAmmoChanged;

        private void Awake()
        {
            movement = GetComponent<SphericalMovement>();
            shooting = GetComponent<ShootingSystem>();

            if (inputActions == null)
            {
                // Try to find the asset at runtime (in case scene reference broke)
                inputActions = Resources.Load<InputActionAsset>("SpaceCleaner_Actions");
                if (inputActions == null)
                {
                    // Last resort: search all loaded assets
                    var allAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
                    foreach (var asset in allAssets)
                    {
                        if (asset.name.Contains("SpaceCleaner"))
                        {
                            inputActions = asset;
                            break;
                        }
                    }
                }
                if (inputActions == null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError("[PlayerController] InputActions asset is NULL! Reassign it in the Inspector.");
#endif
                    return;
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[PlayerController] InputActions was null — found it via fallback. Please reassign in Inspector.");
#endif
            }

            var playerMap = inputActions.FindActionMap("Player");
            if (playerMap == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[PlayerController] 'Player' action map not found in InputActions asset!");
#endif
                return;
            }

            moveAction = playerMap.FindAction("Move");
            aimAction = playerMap.FindAction("Aim");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (moveAction == null) Debug.LogError("[PlayerController] 'Move' action not found!");
            if (aimAction == null) Debug.LogError("[PlayerController] 'Aim' action not found!");
#endif
        }

        private void OnEnable()
        {
            if (inputActions != null) inputActions.Enable();
        }

        private void OnDisable()
        {
            if (inputActions != null) inputActions.Disable();
        }

        private void Update()
        {
            if (moveAction == null || aimAction == null) return;

            // Feed movement input
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            movement.SetMoveInput(moveInput);

            // Feed aim input to shooting system
            Vector2 aimInput = aimAction.ReadValue<Vector2>();
            if (shooting != null)
                shooting.UpdateAim(aimInput);

            // Overflow decay
            if (ammoCount > softCap)
            {
                overflowDecayTimer += Time.deltaTime;
                if (overflowDecayTimer >= 1f / overflowDecayRate)
                {
                    overflowDecayTimer = 0f;
                    ammoCount--;
                    OnAmmoChanged?.Invoke(ammoCount);
                }
            }
        }

        public void AddAmmo(int amount)
        {
            ammoCount += amount;
            OnAmmoChanged?.Invoke(ammoCount);
        }

        public bool TryConsumeAmmo(int amount = 1)
        {
            if (ammoCount < amount) return false;
            ammoCount -= amount;
            OnAmmoChanged?.Invoke(ammoCount);
            return true;
        }
    }
}
