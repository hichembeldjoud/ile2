using System;
using UnityEngine;

namespace SurvivalIsland.Environment
{
    /// <summary>
    /// Gère le cycle jour/nuit réaliste pour une île tropicale.
    /// Contrôle la rotation du soleil et de la lune, l'éclairage dynamique,
    /// l'ambiance atmosphérique, le brouillard et l'exposition du ciel.
    /// Conçu pour fonctionner à l'exécution et dans l'éditeur Unity (ExecuteAlways).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival Island/Environment/Day-Night Cycle")]
    public class DayNightCycle : MonoBehaviour
    {
        #region Singleton Pattern
        public static DayNightCycle Instance { get; private set; }
        #endregion

        #region Paramètres du Temps
        [Header("--- Paramètres Temporels ---")]
        [Tooltip("Durée d'une journée complète (24h in-game) en minutes de temps réel.")]
        [SerializeField, Range(1f, 60f)]
        private float dayDurationInMinutes = 15f;

        [Tooltip("Heure actuelle de la journée (0 = minuit, 6 = lever, 12 = midi, 18 = coucher).")]
        [SerializeField, Range(0f, 24f)]
        private float currentTime = 8f;

        [Tooltip("Active ou met en pause l'écoulement du temps.")]
        [SerializeField]
        private bool isTimeProgressing = true;

        [Tooltip("Multiplicateur de vitesse temporelle (ex: 2 pour accélérer, 0.5 pour ralentir).")]
        [SerializeField, Range(0f, 20f)]
        private float timeScale = 1f;

        [Tooltip("Latitude de l'île tropicale en degrés (définit l'angle et l'élévation du soleil).")]
        [SerializeField, Range(-90f, 90f)]
        private float islandLatitude = 15f;
        #endregion

        #region Références Lumineuses
        [Header("--- Sources Lumineuses ---")]
        [Tooltip("Lumière directionnelle représentant le Soleil.")]
        [SerializeField]
        private Light sunLight;

        [Tooltip("Lumière directionnelle représentant la Lune.")]
        [SerializeField]
        private Light moonLight;
        #endregion

        #region Courbes & Gradients Solaires et Lunaires
        [Header("--- Éclairage Solaire & Lunaire ---")]
        [Tooltip("Intensité maximale de la lumière du soleil.")]
        [SerializeField]
        private float maxSunIntensity = 1.3f;

        [Tooltip("Intensité maximale de la lumière de la lune.")]
        [SerializeField]
        private float maxMoonIntensity = 0.25f;

        [Tooltip("Variation de l'intensité du soleil sur 24h.")]
        [SerializeField]
        private AnimationCurve sunIntensityCurve = new AnimationCurve();

        [Tooltip("Variation de l'intensité de la lune sur 24h.")]
        [SerializeField]
        private AnimationCurve moonIntensityCurve = new AnimationCurve();

        [Tooltip("Gradient de couleur de la lumière solaire (aube dorée, zénith tropical blanc-chaud, coucher carmin).")]
        [SerializeField]
        private Gradient sunColorGradient = new Gradient();

        [Tooltip("Gradient de couleur de la lumière lunaire (nuit bleutée/argentée).")]
        [SerializeField]
        private Gradient moonColorGradient = new Gradient();
        #endregion

        #region Ambiance Atmosphérique & Brouillard
        [Header("--- Atmosphère & Brouillard (Fog) ---")]
        [Tooltip("Gradient de couleur de la lumière ambiante globale.")]
        [SerializeField]
        private Gradient ambientLightGradient = new Gradient();

        [Tooltip("Activer la gestion dynamique du brouillard.")]
        [SerializeField]
        private bool controlFog = true;

        [Tooltip("Gradient de couleur du brouillard atmosphérique.")]
        [SerializeField]
        private Gradient fogColorGradient = new Gradient();

        [Tooltip("Densité du brouillard au cours des 24 heures.")]
        [SerializeField]
        private AnimationCurve fogDensityCurve = new AnimationCurve();

        [Tooltip("Activer le contrôle de l'exposition du Skybox.")]
        [SerializeField]
        private bool controlSkybox = true;

        [Tooltip("Facteur d'exposition du matériau Skybox au cours des 24h.")]
        [SerializeField]
        private AnimationCurve skyboxExposureCurve = new AnimationCurve();

        [Header("--- Matériaux Skybox HD (Fantasy Skybox) ---")]
        [Tooltip("Skybox de jour (plein soleil).")]
        [SerializeField]
        private Material daySkybox;

        [Tooltip("Skybox de l'aube / lever de soleil.")]
        [SerializeField]
        private Material sunriseSkybox;

        [Tooltip("Skybox du coucher de soleil.")]
        [SerializeField]
        private Material sunsetSkybox;

        [Tooltip("Skybox de nuit étoilée.")]
        [SerializeField]
        private Material nightSkybox;
        #endregion

        #region Événements C#
        /// <summary>
        /// Déclenché à chaque mise à jour du temps. Paramètre 1 = Heure (0-24), Paramètre 2 = Progression journalière (0-1).
        /// </summary>
        public event Action<float, float> OnTimeUpdated;

        /// <summary>
        /// Déclenché lorsque l'heure entière change (ex: passage à 13h, 14h...).
        /// </summary>
        public event Action<int> OnHourChanged;

        /// <summary>
        /// Déclenché au lever du soleil (par défaut 06h00).
        /// </summary>
        public event Action OnSunrise;

        /// <summary>
        /// Déclenché au coucher du soleil (par défaut 18h30).
        /// </summary>
        public event Action OnSunset;
        #endregion

        #region Variables Internes
        private int lastRecordedHour = -1;
        private bool wasDay = true;
        private Material skyboxInstance;
        private static readonly int ExposurePropertyId = Shader.PropertyToID("_Exposure");
        private static readonly int SkyTintPropertyId = Shader.PropertyToID("_SkyTint");
        #endregion

        #region Propriétés Publiques
        public float CurrentTime => currentTime;
        public float DayDurationInMinutes => dayDurationInMinutes;
        public bool IsDay => currentTime >= 6f && currentTime < 18.5f;
        public bool IsNight => !IsDay;
        public float NormalizedTime => currentTime / 24f;
        public Light SunLight => sunLight;
        public Light MoonLight => moonLight;
        #endregion

        #region Cycle de Vie Unity
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this && Application.isPlaying)
            {
                Destroy(gameObject);
                return;
            }

            InitializeDefaultsIfEmpty();
            CacheSkyboxMaterial();
        }

        private void OnEnable()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            lastRecordedHour = Mathf.FloorToInt(currentTime);
            wasDay = IsDay;
            UpdateEnvironment(currentTime);
        }

        private void Update()
        {
            if (Application.isPlaying && isTimeProgressing)
            {
                // Avance le temps basé sur le deltaTime réel et la durée configurée
                // 24 heures / (dayDurationInMinutes * 60 secondes) = progression par seconde
                float timeAdvance = (24f / (dayDurationInMinutes * 60f)) * Time.deltaTime * timeScale;
                currentTime = (currentTime + timeAdvance) % 24f;

                CheckTimeEvents();
            }

            // Met à jour l'orientation des astres et l'éclairage
            UpdateEnvironment(currentTime);
        }

        private void OnValidate()
        {
            // Permet de rafraîchir immédiatement la vue Scène lors de modifications dans l'Inspecteur
            if (!Application.isPlaying)
            {
                currentTime = Mathf.Clamp(currentTime, 0f, 24f);
                UpdateEnvironment(currentTime);
            }
        }
        #endregion

        #region Logique Principale d'Éclairage
        /// <summary>
        /// Met à jour la rotation du soleil/lune, les couleurs, intensités et le ciel.
        /// </summary>
        /// <param name="hour">Heure actuelle (0 à 24)</param>
        public void UpdateEnvironment(float hour)
        {
            float time01 = hour / 24f;

            // 1. Calcul de la rotation du Soleil et de la Lune
            // À 6h (lever) angle ~ 0°, à 12h (midi) angle ~ 90°, à 18h (coucher) angle ~ 180°
            float sunAngle = (hour / 24f) * 360f - 90f;
            Quaternion sunRotation = Quaternion.Euler(sunAngle, islandLatitude, 0f);

            // La lune est à l'opposé du soleil (180 degrés de décalage)
            Quaternion moonRotation = Quaternion.Euler(sunAngle + 180f, islandLatitude, 0f);

            // 2. Mise à jour du Soleil
            if (sunLight != null)
            {
                sunLight.transform.rotation = sunRotation;
                float sunIntensityEval = sunIntensityCurve.Evaluate(time01) * maxSunIntensity;

                // Atténuation lors des averses tropicales
                if (WeatherSystem.Instance != null && WeatherSystem.Instance.RainIntensity > 0.01f)
                {
                    sunIntensityEval *= Mathf.Lerp(1f, 0.35f, WeatherSystem.Instance.RainIntensity);
                }

                sunLight.intensity = sunIntensityEval;
                sunLight.color = sunColorGradient.Evaluate(time01);

                // Désactive les ombres sous l'horizon pour optimiser les performances de rendu
                sunLight.shadows = (sunIntensityEval > 0.05f) ? LightShadows.Soft : LightShadows.None;
                sunLight.enabled = sunIntensityEval > 0.01f;
            }

            // 3. Mise à jour de la Lune
            if (moonLight != null)
            {
                moonLight.transform.rotation = moonRotation;
                float moonIntensityEval = moonIntensityCurve.Evaluate(time01) * maxMoonIntensity;
                moonLight.intensity = moonIntensityEval;
                moonLight.color = moonColorGradient.Evaluate(time01);

                moonLight.shadows = (moonIntensityEval > 0.05f) ? LightShadows.Soft : LightShadows.None;
                moonLight.enabled = moonIntensityEval > 0.01f;
            }

            // 4. Lumière Ambiante Globale
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientLightGradient.Evaluate(time01);

            // 5. Brouillard Atmosphérique (Fog)
            if (controlFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = fogColorGradient.Evaluate(time01);
                RenderSettings.fogDensity = fogDensityCurve.Evaluate(time01);
            }

            // 6. Skybox dynamique
            UpdateDynamicSkybox(hour);

            if (controlSkybox && RenderSettings.skybox != null)
            {
                float exposure = skyboxExposureCurve.Evaluate(time01);
                if (RenderSettings.skybox.HasProperty(ExposurePropertyId))
                {
                    RenderSettings.skybox.SetFloat(ExposurePropertyId, exposure);
                }
            }

            // Notification d'événement
            OnTimeUpdated?.Invoke(hour, time01);
        }

        private void UpdateDynamicSkybox(float hour)
        {
            if (WeatherSystem.Instance != null && (WeatherSystem.Instance.CurrentWeather == WeatherType.TropicalRain || WeatherSystem.Instance.CurrentWeather == WeatherType.Thunderstorm))
            {
                return;
            }

            Material targetSky = null;

            if (hour >= 5.5f && hour < 8.0f)
            {
                targetSky = sunriseSkybox ?? daySkybox;
            }
            else if (hour >= 8.0f && hour < 17.5f)
            {
                targetSky = daySkybox;
            }
            else if (hour >= 17.5f && hour < 19.5f)
            {
                targetSky = sunsetSkybox ?? daySkybox;
            }
            else
            {
                targetSky = nightSkybox ?? daySkybox;
            }

            if (targetSky != null && RenderSettings.skybox != targetSky)
            {
                RenderSettings.skybox = targetSky;
            }
        }

        public void ConfigureSkyboxes(Material day, Material sunrise, Material sunset, Material night)
        {
            daySkybox = day;
            sunriseSkybox = sunrise;
            sunsetSkybox = sunset;
            nightSkybox = night;
            UpdateDynamicSkybox(currentTime);
        }

        private void CheckTimeEvents()
        {
            int currentHourInt = Mathf.FloorToInt(currentTime);
            if (currentHourInt != lastRecordedHour)
            {
                lastRecordedHour = currentHourInt;
                OnHourChanged?.Invoke(currentHourInt);
            }

            bool currentlyDay = IsDay;
            if (currentlyDay != wasDay)
            {
                wasDay = currentlyDay;
                if (currentlyDay)
                {
                    OnSunrise?.Invoke();
                }
                else
                {
                    OnSunset?.Invoke();
                }
            }
        }
        #endregion

        #region Initialisation des Courbes et Gradients par Défaut
        /// <summary>
        /// Initialise les courbes et dégradés avec des valeurs réalistes d'île tropicale
        /// si l'utilisateur n'en a pas encore configuré dans l'Inspecteur.
        /// </summary>
        private void InitializeDefaultsIfEmpty()
        {
            // Initialisation de la courbe du soleil si vide
            if (sunIntensityCurve == null || sunIntensityCurve.length == 0)
            {
                sunIntensityCurve = new AnimationCurve(
                    new Keyframe(0.0f, 0.0f),     // 00h00 : Nuit noire
                    new Keyframe(0.23f, 0.0f),    // 05h30 : Pré-aube
                    new Keyframe(0.27f, 0.3f),    // 06h30 : Aube dorée
                    new Keyframe(0.5f, 1.0f),     // 12h00 : Zénith maximal
                    new Keyframe(0.75f, 0.5f),    // 18h00 : Coucher chaud
                    new Keyframe(0.80f, 0.0f),    // 19h15 : Crépuscule achevé
                    new Keyframe(1.0f, 0.0f)      // 24h00 : Nuit noire
                );
            }

            // Initialisation de la courbe de la lune si vide
            if (moonIntensityCurve == null || moonIntensityCurve.length == 0)
            {
                moonIntensityCurve = new AnimationCurve(
                    new Keyframe(0.0f, 0.9f),     // 00h00 : Pleine nuit
                    new Keyframe(0.22f, 0.7f),    // 05h15 : La lune faiblit
                    new Keyframe(0.28f, 0.0f),    // 06h45 : Disparition de la lune
                    new Keyframe(0.73f, 0.0f),    // 17h30 : Pas de lune en journée
                    new Keyframe(0.82f, 0.7f),    // 19h40 : Apparition de la lune
                    new Keyframe(1.0f, 0.9f)      // 24h00 : Pleine nuit
                );
            }

            // Initialisation du gradient solaire si vide
            if (sunColorGradient == null || sunColorGradient.colorKeys.Length <= 1)
            {
                sunColorGradient = new Gradient();
                sunColorGradient.colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.1f, 0.15f, 0.3f), 0.0f),       // Nuit bleutée
                    new GradientColorKey(new Color(0.98f, 0.45f, 0.22f), 0.25f),    // Aube orangée/pêche
                    new GradientColorKey(new Color(1.0f, 0.96f, 0.88f), 0.50f),     // Midi tropical éclatant
                    new GradientColorKey(new Color(0.95f, 0.38f, 0.15f), 0.76f),    // Coucher flamboyant
                    new GradientColorKey(new Color(0.4f, 0.2f, 0.45f), 0.82f),      // Crépuscule pourpre
                    new GradientColorKey(new Color(0.1f, 0.15f, 0.3f), 1.0f)        // Nuit
                };
                sunColorGradient.alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                };
            }

            // Initialisation du gradient lunaire si vide
            if (moonColorGradient == null || moonColorGradient.colorKeys.Length <= 1)
            {
                moonColorGradient = new Gradient();
                moonColorGradient.colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.6f, 0.75f, 0.95f), 0.0f),
                    new GradientColorKey(new Color(0.5f, 0.65f, 0.9f), 1.0f)
                };
            }

            // Initialisation de l'ambiance lumineuse
            if (ambientLightGradient == null || ambientLightGradient.colorKeys.Length <= 1)
            {
                ambientLightGradient = new Gradient();
                ambientLightGradient.colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.04f, 0.06f, 0.12f), 0.0f),     // Nuit obscure
                    new GradientColorKey(new Color(0.35f, 0.25f, 0.3f), 0.25f),     // Aube
                    new GradientColorKey(new Color(0.52f, 0.57f, 0.62f), 0.5f),     // Jour tropical
                    new GradientColorKey(new Color(0.42f, 0.25f, 0.25f), 0.75f),    // Soir
                    new GradientColorKey(new Color(0.04f, 0.06f, 0.12f), 1.0f)      // Nuit
                };
            }

            // Initialisation du fog
            if (fogColorGradient == null || fogColorGradient.colorKeys.Length <= 1)
            {
                fogColorGradient = new Gradient();
                fogColorGradient.colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.05f, 0.08f, 0.15f), 0.0f),
                    new GradientColorKey(new Color(0.75f, 0.55f, 0.45f), 0.25f),
                    new GradientColorKey(new Color(0.7f, 0.82f, 0.92f), 0.5f),
                    new GradientColorKey(new Color(0.72f, 0.42f, 0.32f), 0.75f),
                    new GradientColorKey(new Color(0.05f, 0.08f, 0.15f), 1.0f)
                };
            }

            if (fogDensityCurve == null || fogDensityCurve.length == 0)
            {
                fogDensityCurve = new AnimationCurve(
                    new Keyframe(0.0f, 0.005f),
                    new Keyframe(0.24f, 0.015f), // Brume matinale tropicale
                    new Keyframe(0.35f, 0.003f),
                    new Keyframe(0.50f, 0.002f), // Atmosphère dégagée à midi
                    new Keyframe(0.80f, 0.006f),
                    new Keyframe(1.0f, 0.005f)
                );
            }

            if (skyboxExposureCurve == null || skyboxExposureCurve.length == 0)
            {
                skyboxExposureCurve = new AnimationCurve(
                    new Keyframe(0.0f, 0.15f),
                    new Keyframe(0.25f, 0.5f),
                    new Keyframe(0.5f, 1.2f),
                    new Keyframe(0.75f, 0.6f),
                    new Keyframe(1.0f, 0.15f)
                );
            }
        }

        private void CacheSkyboxMaterial()
        {
            if (RenderSettings.skybox != null)
            {
                // Instantie une copie pour éviter de modifier l'asset originel sur le disque
                skyboxInstance = new Material(RenderSettings.skybox);
                RenderSettings.skybox = skyboxInstance;
            }
        }
        #endregion

        #region Méthodes Publiques Utilitaires
        /// <summary>
        /// Définit directement l'heure du jeu (ex: pour dormir ou pour les quêtes).
        /// </summary>
        /// <param name="hour">Heure entre 0 et 24</param>
        public void SetTime(float hour)
        {
            currentTime = Mathf.Clamp(hour, 0f, 24f);
            UpdateEnvironment(currentTime);
            CheckTimeEvents();
        }

        /// <summary>
        /// Active ou met en pause la progression automatique du temps.
        /// </summary>
        public void SetTimeProgression(bool active)
        {
            isTimeProgressing = active;
        }

        /// <summary>
        /// Renvoie l'heure formatée au format HH:MM (ex: "14:35").
        /// </summary>
        public string GetFormattedTime()
        {
            int hours = Mathf.FloorToInt(currentTime);
            int minutes = Mathf.FloorToInt((currentTime - hours) * 60f);
            return $"{hours:00}:{minutes:00}";
        }

        /// <summary>
        /// Associe automatiquement les lumières si elles ne sont pas assignées.
        /// </summary>
        public void AutoAssignLights(Light sun, Light moon)
        {
            sunLight = sun;
            moonLight = moon;
            UpdateEnvironment(currentTime);
        }
        #endregion
    }
}
