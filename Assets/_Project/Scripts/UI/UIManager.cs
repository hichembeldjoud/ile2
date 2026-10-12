using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SurvivalIsland.Survival;
using SurvivalIsland.Quests;
using SurvivalIsland.Environment;

namespace SurvivalIsland.UI
{
    /// <summary>
    /// Gestionnaire principal de l'interface utilisateur (HUD complet).
    /// Centralise, lie et actualise :
    /// - Les barres de vie, faim, soif et endurance.
    /// - La minimap orthographique.
    /// - Les textes de la quête active et de sa progression.
    /// - L'horloge du cycle jour/nuit et l'état météo.
    /// - L'écran de mort et réapparition.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival Island/UI/UI Manager")]
    public class UIManager : MonoBehaviour
    {
        #region Singleton
        public static UIManager Instance { get; private set; }
        #endregion

        #region Jauges de Survie
        [Header("--- Jauges de Survie ---")]
        [Tooltip("Slider pour la santé.")]
        [SerializeField]
        private Slider healthSlider;
        [SerializeField]
        private TextMeshProUGUI healthText;

        [Tooltip("Slider pour la faim.")]
        [SerializeField]
        private Slider hungerSlider;
        [SerializeField]
        private TextMeshProUGUI hungerText;

        [Tooltip("Slider pour la soif.")]
        [SerializeField]
        private Slider thirstSlider;
        [SerializeField]
        private TextMeshProUGUI thirstText;

        [Tooltip("Slider pour l'endurance.")]
        [SerializeField]
        private Slider staminaSlider;
        [SerializeField]
        private TextMeshProUGUI staminaText;

        [Tooltip("Vitesse d'interpolation visuelle des jauges.")]
        [SerializeField]
        private float barLerpSpeed = 7f;
        #endregion

        #region Interface de la Minimap
        [Header("--- Minimap ---")]
        [Tooltip("Composant RawImage affichant la vue caméra orthographique de la minimap.")]
        [SerializeField]
        private RawImage minimapRawImage;

        [Tooltip("Contrôleur Minimap.")]
        [SerializeField]
        private Minimap minimapController;
        #endregion

        #region Quêtes et Objectifs
        [Header("--- Quête Active ---")]
        [Tooltip("Titre de la quête en cours.")]
        [SerializeField]
        private TextMeshProUGUI questTitleText;

        [Tooltip("Description de l'objectif.")]
        [SerializeField]
        private TextMeshProUGUI questDescriptionText;

        [Tooltip("Texte de progression (ex: '0 / 1').")]
        [SerializeField]
        private TextMeshProUGUI questProgressText;

        [Tooltip("Bannière pop-up lors d'une quête réussie.")]
        [SerializeField]
        private GameObject questCompletedBanner;
        #endregion

        #region Environnement & Temps
        [Header("--- Environnement & Horloge ---")]
        [Tooltip("Texte affichant l'heure in-game (ex: '14:20').")]
        [SerializeField]
        private TextMeshProUGUI clockText;

        [Tooltip("Texte affichant la météo actuelle.")]
        [SerializeField]
        private TextMeshProUGUI weatherText;
        #endregion

        #region Écran de Mort (Game Over)
        [Header("--- Écran de Fin de Partie (Mort) ---")]
        [Tooltip("Panneau apparaissant lorsque le joueur meurt.")]
        [SerializeField]
        private GameObject deathPanel;
        #endregion

        #region Variables Internes
        private float targetHealth = 1f;
        private float targetHunger = 1f;
        private float targetThirst = 1f;
        private float targetStamina = 1f;

        private float currentHealth = 1f;
        private float currentHunger = 1f;
        private float currentThirst = 1f;
        private float currentStamina = 1f;
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

            // Nettoyage immédiat des textes pour éviter les caractères Unicode non supportés par la police
            if (clockText != null) clockText.text = "08:00";
            if (weatherText != null) weatherText.text = "Ciel Degage";
            if (questTitleText != null) questTitleText.text = "OBJECTIF";
        }

        private void Start()
        {
            SubscribeToSystems();
            UpdateInitialDisplays();

            if (deathPanel != null)
            {
                deathPanel.SetActive(false);
            }

            if (questCompletedBanner != null)
            {
                questCompletedBanner.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromSystems();
        }

        private void Update()
        {
            // Lissage visuel des barres
            currentHealth = Mathf.Lerp(currentHealth, targetHealth, Time.deltaTime * barLerpSpeed);
            currentHunger = Mathf.Lerp(currentHunger, targetHunger, Time.deltaTime * barLerpSpeed);
            currentThirst = Mathf.Lerp(currentThirst, targetThirst, Time.deltaTime * barLerpSpeed);
            currentStamina = Mathf.Lerp(currentStamina, targetStamina, Time.deltaTime * barLerpSpeed);

            ApplyBarFills();
            UpdateEnvironmentHUD();
        }
        #endregion

        #region Connexion aux Systèmes
        private void SubscribeToSystems()
        {
            // 1. Survie
            if (SurvivalSystem.Instance != null)
            {
                SurvivalSystem.Instance.OnHealthChanged += HandleHealthChanged;
                SurvivalSystem.Instance.OnHungerChanged += HandleHungerChanged;
                SurvivalSystem.Instance.OnThirstChanged += HandleThirstChanged;
                SurvivalSystem.Instance.OnStaminaChanged += HandleStaminaChanged;
                SurvivalSystem.Instance.OnPlayerDeath += HandlePlayerDeath;
                SurvivalSystem.Instance.OnPlayerRespawn += HandlePlayerRespawn;
            }

            // 2. Quêtes
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted += HandleQuestUpdated;
                QuestManager.Instance.OnQuestUpdated += HandleQuestUpdated;
                QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
            }

            // 3. Météo
            if (WeatherSystem.Instance != null)
            {
                WeatherSystem.Instance.OnWeatherChanged += HandleWeatherChanged;
            }
        }

        private void UnsubscribeFromSystems()
        {
            if (SurvivalSystem.Instance != null)
            {
                SurvivalSystem.Instance.OnHealthChanged -= HandleHealthChanged;
                SurvivalSystem.Instance.OnHungerChanged -= HandleHungerChanged;
                SurvivalSystem.Instance.OnThirstChanged -= HandleThirstChanged;
                SurvivalSystem.Instance.OnStaminaChanged -= HandleStaminaChanged;
                SurvivalSystem.Instance.OnPlayerDeath -= HandlePlayerDeath;
                SurvivalSystem.Instance.OnPlayerRespawn -= HandlePlayerRespawn;
            }

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted -= HandleQuestUpdated;
                QuestManager.Instance.OnQuestUpdated -= HandleQuestUpdated;
                QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
            }

            if (WeatherSystem.Instance != null)
            {
                WeatherSystem.Instance.OnWeatherChanged -= HandleWeatherChanged;
            }
        }

        private void UpdateInitialDisplays()
        {
            if (SurvivalSystem.Instance != null)
            {
                HandleHealthChanged(SurvivalSystem.Instance.CurrentHealth, SurvivalSystem.Instance.MaxHealth);
                HandleHungerChanged(SurvivalSystem.Instance.CurrentHunger, SurvivalSystem.Instance.MaxHunger);
                HandleThirstChanged(SurvivalSystem.Instance.CurrentThirst, SurvivalSystem.Instance.MaxThirst);
                HandleStaminaChanged(SurvivalSystem.Instance.CurrentStamina, SurvivalSystem.Instance.MaxStamina);
            }

            if (QuestManager.Instance != null)
            {
                Quest active = QuestManager.Instance.GetActiveQuest();
                if (active != null)
                {
                    HandleQuestUpdated(active);
                }
            }

            if (WeatherSystem.Instance != null)
            {
                HandleWeatherChanged(WeatherSystem.Instance.CurrentWeather);
            }
        }
        #endregion

        #region Mises à Jour Graphiques
        private void HandleHealthChanged(float current, float max)
        {
            targetHealth = Mathf.Clamp01(current / max);
            if (healthText != null) healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void HandleHungerChanged(float current, float max)
        {
            targetHunger = Mathf.Clamp01(current / max);
            if (hungerText != null) hungerText.text = $"{Mathf.CeilToInt(current)}%";
        }

        private void HandleThirstChanged(float current, float max)
        {
            targetThirst = Mathf.Clamp01(current / max);
            if (thirstText != null) thirstText.text = $"{Mathf.CeilToInt(current)}%";
        }

        private void HandleStaminaChanged(float current, float max)
        {
            targetStamina = Mathf.Clamp01(current / max);
            if (staminaText != null) staminaText.text = $"{Mathf.CeilToInt(current)}%";
        }

        private void ApplyBarFills()
        {
            if (healthSlider != null) healthSlider.value = currentHealth;
            if (hungerSlider != null) hungerSlider.value = currentHunger;
            if (thirstSlider != null) thirstSlider.value = currentThirst;
            if (staminaSlider != null) staminaSlider.value = currentStamina;
        }

        private void UpdateEnvironmentHUD()
        {
            if (clockText != null && DayNightCycle.Instance != null)
            {
                clockText.text = $"{DayNightCycle.Instance.GetFormattedTime()}";
            }
        }

        private void HandleWeatherChanged(WeatherType weather)
        {
            if (weatherText == null) return;

            switch (weather)
            {
                case WeatherType.Clear:
                    weatherText.text = "Ciel Degage";
                    break;
                case WeatherType.Cloudy:
                    weatherText.text = "Nuageux";
                    break;
                case WeatherType.TropicalRain:
                    weatherText.text = "Pluie Tropicale";
                    break;
                case WeatherType.Thunderstorm:
                    weatherText.text = "Orage Violent";
                    break;
            }
        }
        #endregion

        #region Gestion Quêtes UI
        private void HandleQuestUpdated(Quest quest)
        {
            if (quest == null) return;

            if (questTitleText != null)
            {
                questTitleText.text = $"OBJECTIF : {quest.title}";
            }

            if (questDescriptionText != null)
            {
                questDescriptionText.text = quest.description;
            }

            if (questProgressText != null)
            {
                questProgressText.text = $"Objectif : {quest.GetProgressString()}";
            }
        }

        private void HandleQuestCompleted(Quest quest)
        {
            if (questCompletedBanner != null)
            {
                StartCoroutine(ShowQuestCompletedRoutine());
            }
        }

        private IEnumerator ShowQuestCompletedRoutine()
        {
            questCompletedBanner.SetActive(true);
            yield return new WaitForSeconds(3.5f);
            questCompletedBanner.SetActive(false);
        }
        #endregion

        #region Mort & Réapparition
        private void HandlePlayerDeath()
        {
            if (deathPanel != null)
            {
                deathPanel.SetActive(true);
            }
        }

        private void HandlePlayerRespawn()
        {
            if (deathPanel != null)
            {
                deathPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Appelé par un bouton UI 'Réapparaître' pour relancer la partie.
        /// </summary>
        public void OnClickRespawnButton()
        {
            if (SurvivalSystem.Instance != null)
            {
                SurvivalSystem.Instance.Respawn();
            }
        }
        #endregion

        #region Helpers pour Outils Éditeur
        public void AssignReferences(Slider health, Slider hunger, Slider thirst, Slider stamina,
                                     RawImage minimap, Minimap minimapComp,
                                     TextMeshProUGUI qTitle, TextMeshProUGUI qDesc, TextMeshProUGUI qProg,
                                     TextMeshProUGUI clock, TextMeshProUGUI weather, GameObject death)
        {
            healthSlider = health;
            hungerSlider = hunger;
            thirstSlider = thirst;
            staminaSlider = stamina;
            minimapRawImage = minimap;
            minimapController = minimapComp;
            questTitleText = qTitle;
            questDescriptionText = qDesc;
            questProgressText = qProg;
            clockText = clock;
            weatherText = weather;
            deathPanel = death;
        }
        #endregion
    }
}
