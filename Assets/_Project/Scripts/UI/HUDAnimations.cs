using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceCleaner.Core;
using SpaceCleaner.Player;

namespace SpaceCleaner.UI
{
    /// <summary>
    /// HUD micro-animations: bar pulses, number pops, damage flashes.
    /// Like Brawl Stars' snappy UI feedback.
    /// </summary>
    public class HUDAnimations : MonoBehaviour
    {
        [Header("Health Bar")]
        [SerializeField] private float damageFlashDuration = 0.2f;
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private float healthPulseScale = 1.05f;
        
        [Header("Ammo Counter")]
        [SerializeField] private float ammoBumpScale = 1.3f;
        [SerializeField] private float ammoBumpDuration = 0.15f;
        
        [Header("Cleanup Bar")]
        [SerializeField] private float fillSmoothSpeed = 8f;
        [SerializeField] private bool animateFill = true;
        
        private GameplayHUD hud;
        private PlayerController player;
        private Health playerHealth;
        private int lastHealth = -1;
        private int lastAmmo = -1;
        private float displayedCleanupPercent;
        
        // Animation state
        private float ammoBumpTimer;
        private bool isAmmoBumping;
        private TextMeshProUGUI ammoText;
        private Vector3 ammoTextOriginalScale;
        
        private void Awake()
        {
            hud = GetComponent<GameplayHUD>();
            
            // Find references through reflection or public methods
            player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
                player.OnAmmoChanged += OnAmmoChanged;
            }
            
            if (GameManager.Instance != null)
                GameManager.Instance.OnCleanupChanged += OnCleanupChanged;
                
            if (playerHealth != null)
                playerHealth.OnHealthChanged += OnHealthChanged;
        }
        
        private void Start()
        {
            // Find UI elements
            FindUIElements();
        }
        
        private void FindUIElements()
        {
            // Find ammo text
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var tmpComponents = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmpComponents)
                {
                    if (tmp.name == "AmmoText" || tmp.name.Contains("Ammo"))
                    {
                        ammoText = tmp;
                        ammoTextOriginalScale = tmp.transform.localScale;
                        break;
                    }
                }
            }
        }
        
        private void OnDestroy()
        {
            if (player != null)
                player.OnAmmoChanged -= OnAmmoChanged;
            if (GameManager.Instance != null)
                GameManager.Instance.OnCleanupChanged -= OnCleanupChanged;
            if (playerHealth != null)
                playerHealth.OnHealthChanged -= OnHealthChanged;
        }
        
        private void OnAmmoChanged(int newAmmo)
        {
            if (newAmmo > lastAmmo && lastAmmo >= 0)
            {
                // Collected - bump
                TriggerAmmoBump();
            }
            lastAmmo = newAmmo;
        }
        
        private void OnHealthChanged(int current, int max)
        {
            if (current < lastHealth && lastHealth >= 0)
            {
                // Took damage - flash
                TriggerDamageFlash();
            }
            lastHealth = current;
        }
        
        private void OnCleanupChanged(float percentage)
        {
            // Target value is set
        }
        
        private void TriggerAmmoBump()
        {
            isAmmoBumping = true;
            ammoBumpTimer = 0f;
        }
        
        private void TriggerDamageFlash()
        {
            // Find health bar fill and flash it
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var images = canvas.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (img.name == "Fill" && img.transform.parent?.parent?.name == "PlayerHealthContainer")
                    {
                        StartCoroutine(FlashImage(img));
                        break;
                    }
                }
            }
        }
        
        private System.Collections.IEnumerator FlashImage(Image image)
        {
            Color originalColor = image.color;
            image.color = damageFlashColor;
            yield return new WaitForSeconds(damageFlashDuration);
            if (image != null)
                image.color = originalColor;
        }
        
        private void Update()
        {
            UpdateAmmoBump();
            UpdateCleanupBar();
        }
        
        private void UpdateAmmoBump()
        {
            if (ammoText == null) return;
            
            if (isAmmoBumping)
            {
                ammoBumpTimer += Time.deltaTime;
                float t = ammoBumpTimer / ammoBumpDuration;
                
                if (t >= 1f)
                {
                    ammoText.transform.localScale = ammoTextOriginalScale;
                    isAmmoBumping = false;
                }
                else
                {
                    // Scale up then back down
                    float scale = 1f + (ammoBumpScale - 1f) * Mathf.Sin(t * Mathf.PI);
                    ammoText.transform.localScale = ammoTextOriginalScale * scale;
                }
            }
        }
        
        private void UpdateCleanupBar()
        {
            if (!animateFill) return;
            
            // Smoothly interpolate displayed value
            float targetPercent = GameManager.Instance?.CleanupPercentage ?? 0f;
            displayedCleanupPercent = Mathf.MoveTowards(displayedCleanupPercent, targetPercent, 
                fillSmoothSpeed * Time.deltaTime);
        }
    }
}
