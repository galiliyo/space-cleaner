using UnityEngine;
using SpaceCleaner.Player;
using SpaceCleaner.UI;

namespace SpaceCleaner.Core
{
    /// <summary>
    /// Auto-setup for all polish components. Add to GameManager or scene root.
    /// </summary>
    public class PolishSetup : MonoBehaviour
    {
        [Header("Auto-Setup")]
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private bool logSetup = true;
        
        [Header("Optional References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private GameplayHUD hud;
        
        void Start()
        {
            if (setupOnStart)
                SetupAll();
        }
        
        [ContextMenu("Setup All Polish")]
        public void SetupAll()
        {
            SetupPlayerPolish();
            SetupHUDPolish();
            
            if (logSetup)
                Debug.Log("[PolishSetup] All polish components configured.");
        }
        
        void SetupPlayerPolish()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
                
            if (player == null)
            {
                Debug.LogWarning("[PolishSetup] No PlayerController found!");
                return;
            }
            
            var playerGO = player.gameObject;
            
            // ShipFeedback
            if (playerGO.GetComponent<ShipFeedback>() == null)
            {
                playerGO.AddComponent<ShipFeedback>();
                if (logSetup) Debug.Log("[PolishSetup] Added ShipFeedback");
            }
            
            // VacuumJuice
            if (playerGO.GetComponent<VacuumJuice>() == null)
            {
                playerGO.AddComponent<VacuumJuice>();
                if (logSetup) Debug.Log("[PolishSetup] Added VacuumJuice");
            }
            
            // ShootingJuice
            if (playerGO.GetComponent<ShootingJuice>() == null)
            {
                playerGO.AddComponent<ShootingJuice>();
                if (logSetup) Debug.Log("[PolishSetup] Added ShootingJuice");
            }
        }
        
        void SetupHUDPolish()
        {
            if (hud == null)
                hud = FindAnyObjectByType<GameplayHUD>();
                
            if (hud == null)
            {
                Debug.LogWarning("[PolishSetup] No GameplayHUD found!");
                return;
            }
            
            var hudGO = hud.gameObject;
            
            // HUDAnimations
            if (hudGO.GetComponent<HUDAnimations>() == null)
            {
                hudGO.AddComponent<HUDAnimations>();
                if (logSetup) Debug.Log("[PolishSetup] Added HUDAnimations");
            }
            
            // ComboUI
            if (hudGO.GetComponent<ComboUI>() == null)
            {
                hudGO.AddComponent<ComboUI>();
                if (logSetup) Debug.Log("[PolishSetup] Added ComboUI");
            }
        }
    }
}
