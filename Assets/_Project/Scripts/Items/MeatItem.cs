using UnityEngine;
using UnityEngine.InputSystem;
using SurvivalIsland.Survival;
using SurvivalIsland.Audio;

namespace SurvivalIsland.Items
{
    /// <summary>
    /// Objet consommable 'Viande' obtenu après la chasse d'un animal.
    /// Permet au joueur de manger pour restaurer sa jauge de faim dans le SurvivalSystem.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival Island/Items/Meat Item")]
    public class MeatItem : MonoBehaviour
    {
        #region Propriétés de Survie
        [Header("--- Valeurs Nutritionnelles ---")]
        [Tooltip("Quantité de faim restaurée en mangeant cette viande.")]
        [SerializeField]
        private float hungerRestoration = 45f;

        [Tooltip("Bonus de santé accordé (nourriture revigorante).")]
        [SerializeField]
        private float healthBonus = 15f;

        [Tooltip("Distance maximale pour ramasser/manger la viande.")]
        [SerializeField]
        private float pickupDistance = 2.8f;

        [Tooltip("Manger automatiquement au contact sans attendre la touche E.")]
        [SerializeField]
        private bool autoEatOnContact = true;
        #endregion

        #region Audio & Effets Visuels
        [Header("--- Audio & Effets Visuels ---")]
        [Tooltip("Clip audio joué lorsque le joueur mange la viande.")]
        [SerializeField]
        private AudioClip eatSoundClip;

        [Tooltip("Faire flotter et tourner légèrement l'objet pour attirer l'œil.")]
        [SerializeField]
        private bool gentleFloatingEffect = true;

        [Tooltip("Vitesse de rotation sur l'axe Y.")]
        [SerializeField]
        private float rotationSpeed = 45f;
        #endregion

        #region Variables Internes
        private Transform playerTransform;
        private bool isPlayerInRange = false;
        private Vector3 initialPosition;
        #endregion

        #region Cycle de Vie Unity
        private void Start()
        {
            initialPosition = transform.position;

            // S'assure que le collider est un trigger pour la détection
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        private void Update()
        {
            // Effet visuel d'objet au sol
            if (gentleFloatingEffect)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
                float newY = initialPosition.y + Mathf.Sin(Time.time * 2.5f) * 0.08f;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }

            // Recherche automatique du joueur
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
            }

            // Détection de proximité physique
            if (playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= 1.8f && autoEatOnContact)
                {
                    ConsumeMeat();
                    return;
                }

                isPlayerInRange = (dist <= pickupDistance);
            }

            // Détection de la touche d'interaction (Touche E ou Manette)
            if (isPlayerInRange)
            {
                bool interactPressed = false;

                // 1. New Input System
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.eKey.wasPressedThisFrame)
                {
                    interactPressed = true;
                }

                Gamepad gp = Gamepad.current;
                if (gp != null && gp.buttonWest.wasPressedThisFrame)
                {
                    interactPressed = true;
                }

                #if ENABLE_LEGACY_INPUT_MANAGER
                // 2. Legacy Input fallback
                try
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        interactPressed = true;
                    }
                }
                catch { }
                #endif

                if (interactPressed)
                {
                    ConsumeMeat();
                }
            }
        }
        #endregion

        #region Triggers & Consommation
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInRange = true;
                if (playerTransform == null) playerTransform = other.transform;

                if (autoEatOnContact)
                {
                    ConsumeMeat();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerInRange = false;
            }
        }

        /// <summary>
        /// Consomme la viande et restaure la faim dans SurvivalSystem.
        /// </summary>
        public void ConsumeMeat()
        {
            SurvivalSystem survival = SurvivalSystem.Instance;
            if (survival == null && playerTransform != null)
            {
                survival = playerTransform.GetComponent<SurvivalSystem>();
            }
            if (survival == null)
            {
                survival = Object.FindFirstObjectByType<SurvivalSystem>();
            }

            if (survival != null)
            {
                // Restaure la faim et la santé
                survival.Eat(hungerRestoration);
                if (healthBonus > 0f)
                {
                    survival.Heal(healthBonus);
                }

                // Joue le son de mastication / repas
                if (eatSoundClip != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(eatSoundClip, 0.9f);
                }

                // Notification du système de quêtes
                if (SurvivalIsland.Quests.QuestManager.Instance != null)
                {
                    SurvivalIsland.Quests.QuestManager.Instance.NotifyFoodEaten();
                }

                Debug.Log($"<color=#8BC34A><b>[Loot]</b> Viande consommée avec succès ! +{hungerRestoration} Faim restaurée.</color>");

                // Destruction de l'objet après consommation
                Destroy(gameObject);
            }
        }
        #endregion

        #region Affichage Gizmos & UI
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.3f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, pickupDistance);
        }
        #endregion
    }
}
