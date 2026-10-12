using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SurvivalIsland.Survival;

namespace SurvivalIsland.UI
{
    /// <summary>
    /// Gestionnaire d'affichage de l'interface utilisateur (HUD) pour les jauges de survie.
    /// Lie en temps réel les jauges de Santé, Faim, Soif et Endurance du SurvivalSystem.
    /// Supporte les Sliders UGUI et les Images en FillAmount avec lissage visuel.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival Island/UI/Survival UI")]
    public class SurvivalUI : MonoBehaviour
    {
        #region Référence au Système
        [Header("--- Système Source ---")]
        [Tooltip("Référence au SurvivalSystem (laissée vide = utilise le Singleton Instance).")]
        [SerializeField]
        private SurvivalSystem survivalSystem;
        #endregion

        #region Jauge de Santé
        [Header("--- Jauge de Santé (Health) ---")]
        [Tooltip("Slider UGUI représentant la santé.")]
        [SerializeField]
        private Slider healthSlider;

        [Tooltip("Image avec ImageType.Filled représentant la santé (optionnel).")]
        [SerializeField]
        private Image healthFillImage;

        [Tooltip("Texte affichant les PV (ex: '85 / 100').")]
        [SerializeField]
        private TextMeshProUGUI healthText;
        #endregion

        #region Jauge de Faim
        [Header("--- Jauge de Faim (Hunger) ---")]
        [Tooltip("Slider UGUI représentant la faim.")]
        [SerializeField]
        private Slider hungerSlider;

        [Tooltip("Image Filled représentant la faim (optionnel).")]
        [SerializeField]
        private Image hungerFillImage;

        [Tooltip("Texte affichant la faim.")]
        [SerializeField]
        private TextMeshProUGUI hungerText;
        #endregion

        #region Jauge de Soif
        [Header("--- Jauge de Soif (Thirst) ---")]
        [Tooltip("Slider UGUI représentant la soif.")]
        [SerializeField]
        private Slider thirstSlider;

        [Tooltip("Image Filled représentant la soif (optionnel).")]
        [SerializeField]
        private Image thirstFillImage;

        [Tooltip("Texte affichant la soif.")]
        [SerializeField]
        private TextMeshProUGUI thirstText;
        #endregion

        #region Jauge d'Endurance
        [Header("--- Jauge d'Endurance (Stamina) ---")]
        [Tooltip("Slider UGUI représentant l'endurance.")]
        [SerializeField]
        private Slider staminaSlider;

        [Tooltip("Image Filled représentant l'endurance (optionnel).")]
        [SerializeField]
        private Image staminaFillImage;

        [Tooltip("Texte affichant l'endurance.")]
        [SerializeField]
        private TextMeshProUGUI staminaText;
        #endregion

        #region Paramètres Graphiques
        [Header("--- Lissage & Rendu Visuel ---")]
        [Tooltip("Vitesse d'interpolation douce des jauges.")]
        [SerializeField]
        private float barLerpSpeed = 8f;

        [Tooltip("Masquer la barre d'endurance lorsqu'elle est pleine.")]
        [SerializeField]
        private bool hideStaminaWhenFull = false;

        [Tooltip("Conteneur ou CanvasGroup de la barre d'endurance.")]
        [SerializeField]
        private CanvasGroup staminaCanvasGroup;
        #endregion

        #region Variables Internes
        private float targetHealth = 1f;
        private float targetHunger = 1f;
        private float targetThirst = 1f;
        private float targetStamina = 1f;

        private float currentHealthFill = 1f;
        private float currentHungerFill = 1f;
        private float currentThirstFill = 1f;
        private float currentStaminaFill = 1f;
        #endregion

        #region Cycle de Vie Unity
        private void Start()
        {
            if (survivalSystem == null)
            {
                survivalSystem = SurvivalSystem.Instance != null ? SurvivalSystem.Instance : FindFirstObjectByType<SurvivalSystem>();
            }

            if (survivalSystem != null)
            {
                SubscribeToEvents();
                // Initialisation immédiate des valeurs
                UpdateHealthDisplay(survivalSystem.CurrentHealth, survivalSystem.MaxHealth);
                UpdateHungerDisplay(survivalSystem.CurrentHunger, survivalSystem.MaxHunger);
                UpdateThirstDisplay(survivalSystem.CurrentThirst, survivalSystem.MaxThirst);
                UpdateStaminaDisplay(survivalSystem.CurrentStamina, survivalSystem.MaxStamina);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            // Lissage visuel des barres
            currentHealthFill = Mathf.Lerp(currentHealthFill, targetHealth, Time.deltaTime * barLerpSpeed);
            currentHungerFill = Mathf.Lerp(currentHungerFill, targetHunger, Time.deltaTime * barLerpSpeed);
            currentThirstFill = Mathf.Lerp(currentThirstFill, targetThirst, Time.deltaTime * barLerpSpeed);
            currentStaminaFill = Mathf.Lerp(currentStaminaFill, targetStamina, Time.deltaTime * barLerpSpeed);

            ApplyFillToElements();
        }
        #endregion

        #region Inscription aux Événements
        private void SubscribeToEvents()
        {
            if (survivalSystem == null) return;

            survivalSystem.OnHealthChanged += UpdateHealthDisplay;
            survivalSystem.OnHungerChanged += UpdateHungerDisplay;
            survivalSystem.OnThirstChanged += UpdateThirstDisplay;
            survivalSystem.OnStaminaChanged += UpdateStaminaDisplay;
        }

        private void UnsubscribeFromEvents()
        {
            if (survivalSystem == null) return;

            survivalSystem.OnHealthChanged -= UpdateHealthDisplay;
            survivalSystem.OnHungerChanged -= UpdateHungerDisplay;
            survivalSystem.OnThirstChanged -= UpdateThirstDisplay;
            survivalSystem.OnStaminaChanged -= UpdateStaminaDisplay;
        }
        #endregion

        #region Mises à Jour des Affichages
        private void UpdateHealthDisplay(float current, float max)
        {
            targetHealth = Mathf.Clamp01(current / max);
            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        private void UpdateHungerDisplay(float current, float max)
        {
            targetHunger = Mathf.Clamp01(current / max);
            if (hungerText != null)
            {
                hungerText.text = $"{Mathf.CeilToInt(current)}%";
            }
        }

        private void UpdateThirstDisplay(float current, float max)
        {
            targetThirst = Mathf.Clamp01(current / max);
            if (thirstText != null)
            {
                thirstText.text = $"{Mathf.CeilToInt(current)}%";
            }
        }

        private void UpdateStaminaDisplay(float current, float max)
        {
            targetStamina = Mathf.Clamp01(current / max);
            if (staminaText != null)
            {
                staminaText.text = $"{Mathf.CeilToInt(current)}%";
            }

            if (hideStaminaWhenFull && staminaCanvasGroup != null)
            {
                staminaCanvasGroup.alpha = (targetStamina >= 0.99f) ? 0f : 1f;
            }
        }

        private void ApplyFillToElements()
        {
            // Santé
            if (healthSlider != null) healthSlider.value = currentHealthFill;
            if (healthFillImage != null) healthFillImage.fillAmount = currentHealthFill;

            // Faim
            if (hungerSlider != null) hungerSlider.value = currentHungerFill;
            if (hungerFillImage != null) hungerFillImage.fillAmount = currentHungerFill;

            // Soif
            if (thirstSlider != null) thirstSlider.value = currentThirstFill;
            if (thirstFillImage != null) thirstFillImage.fillAmount = currentThirstFill;

            // Endurance
            if (staminaSlider != null) staminaSlider.value = currentStaminaFill;
            if (staminaFillImage != null) staminaFillImage.fillAmount = currentStaminaFill;
        }
        #endregion
    }
}
