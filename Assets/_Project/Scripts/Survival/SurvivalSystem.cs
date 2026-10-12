using System;
using UnityEngine;

namespace SurvivalIsland.Survival
{
    /// <summary>
    /// Gère les besoins vitaux du joueur pour un jeu de survie réaliste :
    /// Faim, Soif, Santé et Endurance (Stamina).
    /// Gère la dégradation naturelle du temps, les dégâts d'inanition/déshydratation,
    /// la régénération passive et expose tous les événements pour l'UI.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival Island/Survival/Survival System")]
    public class SurvivalSystem : MonoBehaviour
    {
        #region Singleton
        public static SurvivalSystem Instance { get; private set; }
        #endregion

        #region Santé (Health)
        [Header("--- Santé (Health) ---")]
        [Tooltip("Santé maximale du joueur.")]
        [SerializeField]
        private float maxHealth = 100f;

        [Tooltip("Santé actuelle du joueur.")]
        [SerializeField]
        private float currentHealth = 100f;

        [Tooltip("Régénération de santé par seconde si la faim et la soif sont à un niveau élevé (>70%).")]
        [SerializeField]
        private float passiveHealthRegenRate = 1.0f;

        [Tooltip("Seuil de faim/soif (en %) nécessaire pour déclencher la régénération de santé passive.")]
        [SerializeField, Range(0f, 1f)]
        private float regenVitalThreshold = 0.70f;
        #endregion

        #region Faim (Hunger)
        [Header("--- Faim (Hunger) ---")]
        [Tooltip("Faim maximale (100 = rassasié, 0 = famine totale).")]
        [SerializeField]
        private float maxHunger = 100f;

        [Tooltip("Faim actuelle.")]
        [SerializeField]
        private float currentHunger = 100f;

        [Tooltip("Points de faim perdus par minute au repos sur l'île.")]
        [SerializeField]
        private float hungerDepletionPerMinute = 2.5f;

        [Tooltip("Multiplicateur de perte de faim pendant la course ou effort soutenu.")]
        [SerializeField]
        private float runningHungerMultiplier = 1.8f;

        [Tooltip("Dégâts de santé infligés par seconde lorsque la faim est à 0 (famine).")]
        [SerializeField]
        private float starvationDamagePerSecond = 1.2f;
        #endregion

        #region Soif (Thirst)
        [Header("--- Soif (Thirst) ---")]
        [Tooltip("Soif maximale (100 = hydraté, 0 = déshydratation totale).")]
        [SerializeField]
        private float maxThirst = 100f;

        [Tooltip("Soif actuelle.")]
        [SerializeField]
        private float currentThirst = 100f;

        [Tooltip("Points de soif perdus par minute (la soif chute plus vite sous le climat tropical).")]
        [SerializeField]
        private float thirstDepletionPerMinute = 4.0f;

        [Tooltip("Multiplicateur de perte de soif pendant la course.")]
        [SerializeField]
        private float runningThirstMultiplier = 2.2f;

        [Tooltip("Dégâts de santé infligés par seconde lorsque la soif est à 0 (déshydratation).")]
        [SerializeField]
        private float dehydrationDamagePerSecond = 2.5f;
        #endregion

        #region Endurance (Stamina)
        [Header("--- Endurance (Stamina) ---")]
        [Tooltip("Endurance maximale.")]
        [SerializeField]
        private float maxStamina = 100f;

        [Tooltip("Endurance actuelle.")]
        [SerializeField]
        private float currentStamina = 100f;

        [Tooltip("Endurance consommée par seconde de sprint.")]
        [SerializeField]
        private float sprintStaminaCostPerSecond = 15f;

        [Tooltip("Endurance consommée par saut.")]
        [SerializeField]
        private float jumpStaminaCost = 10f;

        [Tooltip("Régénération d'endurance par seconde au repos.")]
        [SerializeField]
        private float staminaRegenPerSecond = 20f;

        [Tooltip("Délai en secondes avant que l'endurance commence à se régénérer après un effort.")]
        [SerializeField]
        private float staminaRegenDelay = 1.0f;
        #endregion

        #region Événements C# (Liaison UI & Systèmes)
        /// <summary>
        /// Événement déclenché quand la santé change. (Valeur actuelle, Valeur Max)
        /// </summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>
        /// Événement déclenché quand la faim change. (Valeur actuelle, Valeur Max)
        /// </summary>
        public event Action<float, float> OnHungerChanged;

        /// <summary>
        /// Événement déclenché quand la soif change. (Valeur actuelle, Valeur Max)
        /// </summary>
        public event Action<float, float> OnThirstChanged;

        /// <summary>
        /// Événement déclenché quand l'endurance change. (Valeur actuelle, Valeur Max)
        /// </summary>
        public event Action<float, float> OnStaminaChanged;

        /// <summary>
        /// Événement déclenché lorsque le joueur meurt (Santé = 0).
        /// </summary>
        public event Action OnPlayerDeath;

        /// <summary>
        /// Événement déclenché lors de la réapparition du joueur.
        /// </summary>
        public event Action OnPlayerRespawn;
        #endregion

        #region Variables Internes
        private bool isDead = false;
        private bool isRunning = false;
        private float staminaTimer = 0f;
        #endregion

        #region Propriétés Publiques
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercent => Mathf.Clamp01(currentHealth / maxHealth);

        public float CurrentHunger => currentHunger;
        public float MaxHunger => maxHunger;
        public float HungerPercent => Mathf.Clamp01(currentHunger / maxHunger);

        public float CurrentThirst => currentThirst;
        public float MaxThirst => maxThirst;
        public float ThirstPercent => Mathf.Clamp01(currentThirst / maxThirst);

        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public float StaminaPercent => Mathf.Clamp01(currentStamina / maxStamina);

        public bool IsDead => isDead;
        public bool HasStamina => currentStamina > 2f;
        #endregion

        #region Cycle de Vie Unity
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            currentHealth = maxHealth;
            currentHunger = maxHunger;
            currentThirst = maxThirst;
            currentStamina = maxStamina;
        }

        private void Start()
        {
            // Diffusion initiale de l'état vers l'UI
            NotifyAllVitalStats();
        }

        private void Update()
        {
            if (isDead) return;

            UpdateHungerAndThirst(Time.deltaTime);
            UpdateHealth(Time.deltaTime);
            UpdateStamina(Time.deltaTime);
        }
        #endregion

        #region Mise à Jour des Besoins Vitaux
        private void UpdateHungerAndThirst(float deltaTime)
        {
            // Calcul du taux de faim par seconde
            float hungerLossRate = (hungerDepletionPerMinute / 60f);
            if (isRunning) hungerLossRate *= runningHungerMultiplier;

            currentHunger = Mathf.Max(0f, currentHunger - hungerLossRate * deltaTime);
            OnHungerChanged?.Invoke(currentHunger, maxHunger);

            // Calcul du taux de soif par seconde
            float thirstLossRate = (thirstDepletionPerMinute / 60f);
            if (isRunning) thirstLossRate *= runningThirstMultiplier;

            currentThirst = Mathf.Max(0f, currentThirst - thirstLossRate * deltaTime);
            OnThirstChanged?.Invoke(currentThirst, maxThirst);
        }

        private void UpdateHealth(float deltaTime)
        {
            // 1. Dégâts d'inanition si faim vide
            if (currentHunger <= 0f)
            {
                TakeDamage(starvationDamagePerSecond * deltaTime, "Famine");
            }

            // 2. Dégâts de déshydratation si soif vide
            if (currentThirst <= 0f)
            {
                TakeDamage(dehydrationDamagePerSecond * deltaTime, "Déshydratation");
            }

            // 3. Régénération passive de santé si bien nourri et hydraté
            bool isWellFed = (currentHunger / maxHunger) >= regenVitalThreshold;
            bool isHydrated = (currentThirst / maxThirst) >= regenVitalThreshold;

            if (isWellFed && isHydrated && currentHealth < maxHealth && currentHunger > 0 && currentThirst > 0)
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth + passiveHealthRegenRate * deltaTime);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }

        private void UpdateStamina(float deltaTime)
        {
            if (isRunning)
            {
                // Consomme l'endurance en courant
                currentStamina = Mathf.Max(0f, currentStamina - sprintStaminaCostPerSecond * deltaTime);
                staminaTimer = 0f;
                OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            }
            else
            {
                // Régénération d'endurance après un temps de repos
                staminaTimer += deltaTime;
                if (staminaTimer >= staminaRegenDelay && currentStamina < maxStamina)
                {
                    currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenPerSecond * deltaTime);
                    OnStaminaChanged?.Invoke(currentStamina, maxStamina);
                }
            }
        }
        #endregion

        #region Actions de Gameplay (Manger, Boire, Soin, Dégâts)
        /// <summary>
        /// Consomme de la nourriture pour restaurer la jauge de faim.
        /// (Appelé par exemple lorsqu'on mange de la viande de gibier ou des fruits).
        /// </summary>
        /// <param name="nutritionAmount">Quantité de faim restaurée</param>
        public void Eat(float nutritionAmount)
        {
            if (isDead) return;

            currentHunger = Mathf.Min(maxHunger, currentHunger + nutritionAmount);
            OnHungerChanged?.Invoke(currentHunger, maxHunger);

            // Joue un son de dégustation ou notifie
            Debug.Log($"[SurvivalSystem] Nourriture consommée : +{nutritionAmount} Faim. Faim actuelle : {currentHunger:F1}/{maxHunger}");
        }

        /// <summary>
        /// Consomme de l'eau potable pour restaurer la jauge de soif.
        /// </summary>
        /// <param name="hydrationAmount">Quantité de soif restaurée</param>
        public void Drink(float hydrationAmount)
        {
            if (isDead) return;

            currentThirst = Mathf.Min(maxThirst, currentThirst + hydrationAmount);
            OnThirstChanged?.Invoke(currentThirst, maxThirst);

            Debug.Log($"[SurvivalSystem] Eau consommée : +{hydrationAmount} Soif. Soif actuelle : {currentThirst:F1}/{maxThirst}");
        }

        /// <summary>
        /// Soigne le joueur d'un montant de points de vie.
        /// </summary>
        public void Heal(float healthAmount)
        {
            if (isDead) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + healthAmount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Inflige des dégâts au joueur (attaques d'animaux, faim, soif, chutes).
        /// </summary>
        public void TakeDamage(float damageAmount, string source = "Dégâts")
        {
            if (isDead) return;

            currentHealth = Mathf.Max(0f, currentHealth - damageAmount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                Die(source);
            }
        }

        /// <summary>
        /// Déduit de l'endurance (ex: lors d'un saut ou attaque).
        /// </summary>
        /// <returns>True si le joueur avait assez d'endurance, False sinon.</returns>
        public bool ConsumeStamina(float amount)
        {
            if (currentStamina >= amount)
            {
                currentStamina -= amount;
                staminaTimer = 0f;
                OnStaminaChanged?.Invoke(currentStamina, maxStamina);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Déduit l'endurance d'un saut si disponible.
        /// </summary>
        public bool TryConsumeJumpStamina()
        {
            return ConsumeStamina(jumpStaminaCost);
        }

        /// <summary>
        /// Notifie le système si le joueur est en train de sprinter.
        /// </summary>
        public void SetRunningState(bool running)
        {
            isRunning = running && currentStamina > 1f;
        }

        private void Die(string cause)
        {
            isDead = true;
            currentHealth = 0f;
            Debug.LogWarning($"<color=red><b>[SurvivalSystem] Mort du joueur ! Cause : {cause}</b></color>");
            OnPlayerDeath?.Invoke();
        }

        /// <summary>
        /// Réinitialise les constantes de survie lors d'une réapparition.
        /// </summary>
        public void Respawn()
        {
            isDead = false;
            currentHealth = maxHealth;
            currentHunger = maxHunger * 0.75f;
            currentThirst = maxThirst * 0.75f;
            currentStamina = maxStamina;

            NotifyAllVitalStats();
            OnPlayerRespawn?.Invoke();
        }

        /// <summary>
        /// Propage toutes les statistiques vers les abonnés (notamment l'UI).
        /// </summary>
        public void NotifyAllVitalStats()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnHungerChanged?.Invoke(currentHunger, maxHunger);
            OnThirstChanged?.Invoke(currentThirst, maxThirst);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
        #endregion
    }
}
