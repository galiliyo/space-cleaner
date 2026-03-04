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
        private InputAction singleShotAction;
        private InputAction burstShotAction;
        private float overflowDecayTimer;

        public int AmmoCount => ammoCount;
        public int SoftCap => softCap;

        public event System.Action<int> OnAmmoChanged;

        private void Awake()
        {
            movement = GetComponent<SphericalMovement>();
            shooting = GetComponent<ShootingSystem>();

            var playerMap = inputActions.FindActionMap("Player");
            moveAction = playerMap.FindAction("Move");
            aimAction = playerMap.FindAction("Aim");
            singleShotAction = playerMap.FindAction("SingleShot");
            burstShotAction = playerMap.FindAction("BurstShot");
        }

        private void OnEnable()
        {
            inputActions.Enable();
            singleShotAction.performed += OnSingleShot;
            burstShotAction.performed += OnBurstShot;
        }

        private void OnDisable()
        {
            singleShotAction.performed -= OnSingleShot;
            burstShotAction.performed -= OnBurstShot;
            inputActions.Disable();
        }

        private void Update()
        {
            // Feed movement input
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            movement.SetMoveInput(moveInput);

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

        private void OnSingleShot(InputAction.CallbackContext ctx)
        {
            if (shooting != null)
                shooting.FireSingle();
        }

        private void OnBurstShot(InputAction.CallbackContext ctx)
        {
            if (shooting != null)
                shooting.FireBurst();
        }
    }
}
