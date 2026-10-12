using System;
using System.Collections;
using UnityEngine;

namespace SurvivalIsland.Environment
{
    /// <summary>
    /// Type d'état météorologique sur l'île.
    /// </summary>
    public enum WeatherType
    {
        Clear,          // Ciel tropical ensoleillé
        Cloudy,         // Partiellement couvert
        TropicalRain,   // Averse tropicale dense
        Thunderstorm    // Orage violent tropical (pluie, éclairs, tonnerre)
    }

    /// <summary>
    /// Gère la météo dynamique de l'île tropicale.
    /// Alterne entre ciel dégagé et pluie tropicale, pilote les particules de pluie,
    /// assombrit l'éclairage en synergie avec DayNightCycle, génère les éclairs et contrôle le son de pluie.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival Island/Environment/Weather System")]
    public class WeatherSystem : MonoBehaviour
    {
        #region Singleton
        public static WeatherSystem Instance { get; private set; }
        #endregion

        #region Configuration Météo
        [Header("--- État Météorologique ---")]
        [Tooltip("Météo actuelle active.")]
        [SerializeField]
        private WeatherType currentWeather = WeatherType.Clear;

        [Tooltip("Active le cycle météo automatique aléatoire.")]
        [SerializeField]
        private bool autoWeatherCycle = true;

        [Tooltip("Durée minimale d'un épisode météo en secondes.")]
        [SerializeField]
        private float minWeatherDuration = 180f; // 3 minutes

        [Tooltip("Durée maximale d'un épisode météo en secondes.")]
        [SerializeField]
        private float maxWeatherDuration = 480f; // 8 minutes

        [Tooltip("Durée de transition fluide entre deux météos en secondes.")]
        [SerializeField, Range(1f, 15f)]
        private float transitionDuration = 5f;
        #endregion

        #region Effets Visuels (VFX)
        [Header("--- Effets Visuels de Pluie (VFX) ---")]
        [Tooltip("Système de particules émettant les gouttes de pluie tropicale.")]
        [SerializeField]
        private ParticleSystem rainParticleSystem;

        [Tooltip("Taux d'émission de particules maximal pour une averse tropicale (particules/seconde).")]
        [SerializeField]
        private float maxRainEmissionRate = 450f;

        [Tooltip("Cible que le système de pluie doit suivre (par défaut la Caméra Principale).")]
        [SerializeField]
        private Transform rainFollowTarget;

        [Tooltip("Hauteur au-dessus de la cible où le système de particules flotte.")]
        [SerializeField]
        private float rainHeightOffset = 18f;
        #endregion

        #region Impact sur l'Éclairage & Atmosphère
        [Header("--- Impact Atmosphérique & Éclairage ---")]
        [Tooltip("Multiplicateur d'intensité solaire par temps de pluie (assombrissement sous les nuages tropicaux).")]
        [SerializeField, Range(0.1f, 1f)]
        private float rainSunMultiplier = 0.35f;

        [Tooltip("Multiplicateur d'augmentation du brouillard par temps de pluie.")]
        [SerializeField, Range(1f, 5f)]
        private float rainFogDensityMultiplier = 2.5f;

        [Tooltip("Couleur du brouillard pendant les averses tropicales.")]
        [SerializeField]
        private Color rainFogColor = new Color(0.45f, 0.48f, 0.52f, 1f);

        [Header("--- Matériaux Skybox Tempête / Pluie (Fantasy Skybox) ---")]
        [Tooltip("Skybox sombre et orageux utilisé pendant la pluie tropicale et l'orage.")]
        [SerializeField]
        private Material rainSkybox;

        [Tooltip("Skybox clair de repli.")]
        [SerializeField]
        private Material clearSkybox;
        #endregion

        #region Orage & Éclairs
        [Header("--- Orage & Éclairs (Thunderstorm) ---")]
        [Tooltip("Lumière utilisée pour simuler le flash d'un éclair.")]
        [SerializeField]
        private Light lightningLight;

        [Tooltip("Intervalle minimal entre deux éclairs en secondes.")]
        [SerializeField]
        private float minLightningInterval = 6f;

        [Tooltip("Intervalle maximal entre deux éclairs en secondes.")]
        [SerializeField]
        private float maxLightningInterval = 18f;

        [Tooltip("Clips audio de tonnerre joués lors des éclairs.")]
        [SerializeField]
        private AudioClip[] thunderClips;
        #endregion

        #region Audio Météo
        [Header("--- Audio Météo ---")]
        [Tooltip("Source audio en boucle pour le bruit de pluie tropicale.")]
        [SerializeField]
        private AudioSource rainAudioSource;

        [Tooltip("Source audio pour les coups de tonnerre.")]
        [SerializeField]
        private AudioSource thunderAudioSource;

        [Tooltip("Volume maximal du son de pluie.")]
        [SerializeField, Range(0f, 1f)]
        private float maxRainVolume = 0.85f;
        #endregion

        #region Événements C#
        /// <summary>
        /// Déclenché lorsqu'un changement de météo débute.
        /// </summary>
        public event Action<WeatherType> OnWeatherChanged;

        /// <summary>
        /// Déclenché pendant la transition météo. Paramètre = intensité de pluie normalisée (0 à 1).
        /// </summary>
        public event Action<float> OnRainIntensityChanged;
        #endregion

        #region Variables Internes
        private float targetRainIntensity = 0f;
        private float currentRainIntensity = 0f;
        private float weatherTimer = 0f;
        private float nextWeatherChangeTime = 0f;
        private Coroutine lightningCoroutine;
        private Coroutine transitionCoroutine;
        #endregion

        #region Propriétés Publiques
        public WeatherType CurrentWeather => currentWeather;
        public float RainIntensity => currentRainIntensity;
        public bool IsRaining => currentRainIntensity > 0.05f;
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

            EnsureComponentsReady();
        }

        private void Start()
        {
            ScheduleNextWeatherChange();
            ApplyInstantWeather(currentWeather);
        }

        private void Update()
        {
            // Met à jour la position du système de pluie au-dessus de la caméra ou de la cible
            UpdateRainPosition();

            // Gestion du cycle météo aléatoire automatique
            if (autoWeatherCycle && Application.isPlaying)
            {
                weatherTimer += Time.deltaTime;
                if (weatherTimer >= nextWeatherChangeTime)
                {
                    weatherTimer = 0f;
                    PickRandomNextWeather();
                    ScheduleNextWeatherChange();
                }
            }
        }
        #endregion

        #region Gestion et Transitions Météo
        /// <summary>
        /// Change la météo actuelle avec une transition douce.
        /// </summary>
        /// <param name="newWeather">Nouveau type de météo</param>
        /// <param name="duration">Durée de la transition en secondes</param>
        public void SetWeather(WeatherType newWeather, float duration = -1f)
        {
            if (duration < 0f)
            {
                duration = transitionDuration;
            }

            currentWeather = newWeather;
            OnWeatherChanged?.Invoke(newWeather);

            switch (newWeather)
            {
                case WeatherType.Clear:
                    targetRainIntensity = 0f;
                    StopLightning();
                    break;

                case WeatherType.Cloudy:
                    targetRainIntensity = 0.05f;
                    StopLightning();
                    break;

                case WeatherType.TropicalRain:
                    targetRainIntensity = 0.85f;
                    StopLightning();
                    break;

                case WeatherType.Thunderstorm:
                    targetRainIntensity = 1f;
                    StartLightning();
                    break;
            }

            if (newWeather == WeatherType.TropicalRain || newWeather == WeatherType.Thunderstorm)
            {
                if (rainSkybox != null) RenderSettings.skybox = rainSkybox;
            }
            else
            {
                if (clearSkybox != null && DayNightCycle.Instance == null)
                {
                    RenderSettings.skybox = clearSkybox;
                }
            }

            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            transitionCoroutine = StartCoroutine(TransitionWeatherRoutine(targetRainIntensity, duration));
        }

        public void ConfigureRainSkybox(Material rainMat, Material clearMat = null)
        {
            rainSkybox = rainMat;
            if (clearMat != null) clearSkybox = clearMat;
            if (currentWeather == WeatherType.TropicalRain || currentWeather == WeatherType.Thunderstorm)
            {
                if (rainSkybox != null) RenderSettings.skybox = rainSkybox;
            }
        }

        private IEnumerator TransitionWeatherRoutine(float targetIntensity, float duration)
        {
            float initialIntensity = currentRainIntensity;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                currentRainIntensity = Mathf.Lerp(initialIntensity, targetIntensity, smoothT);
                ApplyRainIntensity(currentRainIntensity);

                yield return null;
            }

            currentRainIntensity = targetIntensity;
            ApplyRainIntensity(currentRainIntensity);
        }

        private void ApplyInstantWeather(WeatherType weather)
        {
            currentWeather = weather;
            targetRainIntensity = (weather == WeatherType.TropicalRain) ? 0.85f : (weather == WeatherType.Thunderstorm ? 1f : 0f);
            currentRainIntensity = targetRainIntensity;
            ApplyRainIntensity(currentRainIntensity);

            if (weather == WeatherType.TropicalRain || weather == WeatherType.Thunderstorm)
            {
                if (rainSkybox != null) RenderSettings.skybox = rainSkybox;
            }
            else
            {
                if (clearSkybox != null && DayNightCycle.Instance == null)
                {
                    RenderSettings.skybox = clearSkybox;
                }
            }

            if (weather == WeatherType.Thunderstorm)
            {
                StartLightning();
            }
            else
            {
                StopLightning();
            }
        }

        private void ApplyRainIntensity(float intensity)
        {
            // 1. Particules de pluie
            if (rainParticleSystem != null)
            {
                var emission = rainParticleSystem.emission;
                emission.rateOverTime = maxRainEmissionRate * intensity;

                if (intensity > 0.01f && !rainParticleSystem.isPlaying)
                {
                    rainParticleSystem.Play();
                }
                else if (intensity <= 0.01f && rainParticleSystem.isPlaying)
                {
                    rainParticleSystem.Stop();
                }
            }

            // 2. Audio de pluie
            if (rainAudioSource != null)
            {
                rainAudioSource.volume = maxRainVolume * intensity;
                if (intensity > 0.01f && !rainAudioSource.isPlaying)
                {
                    rainAudioSource.Play();
                }
                else if (intensity <= 0.01f && rainAudioSource.isPlaying)
                {
                    rainAudioSource.Stop();
                }
            }

            // 3. Impact sur le brouillard et la lumière solaire
            if (RenderSettings.fog)
            {
                RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, RenderSettings.fogDensity * rainFogDensityMultiplier, intensity * 0.1f);
            }

            if (RenderSettings.sun != null)
            {
                RenderSettings.sun.intensity = Mathf.Lerp(RenderSettings.sun.intensity, RenderSettings.sun.intensity * rainSunMultiplier, intensity * 0.05f);
            }

            OnRainIntensityChanged?.Invoke(intensity);
        }

        private void PickRandomNextWeather()
        {
            // Probabilités tropicales réalistes :
            // Majoritairement du beau temps (65%), avec averses tropicales régulières (25%) et orages occasionnels (10%)
            float roll = UnityEngine.Random.value;
            WeatherType nextWeather;

            if (roll < 0.65f)
            {
                nextWeather = WeatherType.Clear;
            }
            else if (roll < 0.85f)
            {
                nextWeather = WeatherType.TropicalRain;
            }
            else
            {
                nextWeather = WeatherType.Thunderstorm;
            }

            SetWeather(nextWeather, transitionDuration);
        }

        private void ScheduleNextWeatherChange()
        {
            nextWeatherChangeTime = UnityEngine.Random.Range(minWeatherDuration, maxWeatherDuration);
            weatherTimer = 0f;
        }

        private void UpdateRainPosition()
        {
            if (rainParticleSystem == null) return;

            if (rainFollowTarget == null)
            {
                if (Camera.main != null)
                {
                    rainFollowTarget = Camera.main.transform;
                }
            }

            if (rainFollowTarget != null)
            {
                Vector3 targetPos = rainFollowTarget.position + Vector3.up * rainHeightOffset;
                rainParticleSystem.transform.position = targetPos;
            }
        }
        #endregion

        #region Éclairs & Tonnerre
        private void StartLightning()
        {
            if (lightningCoroutine == null)
            {
                lightningCoroutine = StartCoroutine(LightningRoutine());
            }
        }

        private void StopLightning()
        {
            if (lightningCoroutine != null)
            {
                StopCoroutine(lightningCoroutine);
                lightningCoroutine = null;
            }

            if (lightningLight != null)
            {
                lightningLight.enabled = false;
            }
        }

        private IEnumerator LightningRoutine()
        {
            while (currentWeather == WeatherType.Thunderstorm)
            {
                float wait = UnityEngine.Random.Range(minLightningInterval, maxLightningInterval);
                yield return new WaitForSeconds(wait);

                // Simulation d'un double éclair réaliste
                yield return StartCoroutine(FlashLightning());

                // Décalage réaliste entre l'éclair et le bruit du tonnerre (vitesse du son)
                float soundDelay = UnityEngine.Random.Range(0.4f, 1.8f);
                yield return new WaitForSeconds(soundDelay);

                PlayThunderSound();
            }
        }

        private IEnumerator FlashLightning()
        {
            if (lightningLight == null) yield break;

            lightningLight.enabled = true;
            lightningLight.intensity = UnityEngine.Random.Range(2.5f, 4f);
            yield return new WaitForSeconds(0.08f);

            lightningLight.enabled = false;
            yield return new WaitForSeconds(0.05f);

            lightningLight.enabled = true;
            lightningLight.intensity = UnityEngine.Random.Range(1.8f, 3f);
            yield return new WaitForSeconds(0.12f);

            lightningLight.enabled = false;
        }

        private void PlayThunderSound()
        {
            if (thunderAudioSource == null || thunderClips == null || thunderClips.Length == 0) return;

            AudioClip clip = thunderClips[UnityEngine.Random.Range(0, thunderClips.Length)];
            if (clip != null)
            {
                thunderAudioSource.pitch = UnityEngine.Random.Range(0.85f, 1.15f);
                thunderAudioSource.PlayOneShot(clip, UnityEngine.Random.Range(0.7f, 1f));
            }
        }
        #endregion

        #region Initialisation Automatique & Helpers
        private void EnsureComponentsReady()
        {
            // Initialisation de la source audio de pluie si manquante
            if (rainAudioSource == null)
            {
                rainAudioSource = gameObject.AddComponent<AudioSource>();
                rainAudioSource.loop = true;
                rainAudioSource.playOnAwake = false;
                rainAudioSource.spatialBlend = 0f; // 2D ambiant
            }

            // Initialisation de la source audio de tonnerre si manquante
            if (thunderAudioSource == null)
            {
                thunderAudioSource = gameObject.AddComponent<AudioSource>();
                thunderAudioSource.loop = false;
                thunderAudioSource.playOnAwake = false;
                thunderAudioSource.spatialBlend = 0f;
            }

            // Configuration ou création du flash de lumière si manquant
            if (lightningLight == null)
            {
                GameObject lightObj = new GameObject("LightningLight");
                lightObj.transform.SetParent(transform);
                lightningLight = lightObj.AddComponent<Light>();
                lightningLight.type = LightType.Directional;
                lightningLight.color = new Color(0.85f, 0.92f, 1f);
                lightningLight.intensity = 0f;
                lightningLight.enabled = false;
            }
        }
        #endregion
    }
}
