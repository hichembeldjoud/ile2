using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalIsland.Audio
{
    /// <summary>
    /// Gestionnaire audio centralisé pour le jeu de survie.
    /// Gère les ambiances géographiques avec crossfade fluide (Dual-Source),
    /// les effets sonores (SFX), la météo et les musiques.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival Island/Audio/Audio Manager")]
    public class AudioManager : MonoBehaviour
    {
        #region Singleton
        public static AudioManager Instance { get; private set; }
        #endregion

        #region Volumes Globaux
        [Header("--- Volumes Maîtres ---")]
        [Tooltip("Volume général de tout le jeu.")]
        [SerializeField, Range(0f, 1f)]
        private float masterVolume = 1f;

        [Tooltip("Volume dédié aux ambiances naturelles (plage, jungle, montagne).")]
        [SerializeField, Range(0f, 1f)]
        private float ambienceVolume = 0.85f;

        [Tooltip("Volume dédié aux effets sonores (bruits de pas, tir, impact, survie).")]
        [SerializeField, Range(0f, 1f)]
        private float sfxVolume = 1f;

        [Tooltip("Volume dédié à la musique et aux mélodies.")]
        [SerializeField, Range(0f, 1f)]
        private float musicVolume = 0.7f;
        #endregion

        #region Pistes par Défaut
        [Header("--- Ambiance par Défaut ---")]
        [Tooltip("Clip d'ambiance joué quand le joueur n'est dans aucune zone spécifique.")]
        [SerializeField]
        private AudioClip defaultIslandAmbientClip;

        [Tooltip("Volume cible pour l'ambiance par défaut.")]
        [SerializeField, Range(0f, 1f)]
        private float defaultAmbientVolume = 0.6f;
        #endregion

        #region Références AudioSources
        [Header("--- Canaux Audio Internes ---")]
        [Tooltip("Première source du crossfader d'ambiance.")]
        [SerializeField]
        private AudioSource ambientSourceA;

        [Tooltip("Deuxième source du crossfader d'ambiance.")]
        [SerializeField]
        private AudioSource ambientSourceB;

        [Tooltip("Source dédiée à la musique thématique.")]
        [SerializeField]
        private AudioSource musicSource;

        [Tooltip("Source dédiée aux effets sonores 2D (UI, notifications).")]
        [SerializeField]
        private AudioSource sfxSource;
        #endregion

        #region Variables Internes
        private bool isUsingSourceA = true;
        private Coroutine crossfadeCoroutine;
        private readonly List<AmbientAudioZone> activeZones = new List<AmbientAudioZone>();
        private AudioClip currentAmbientClip;
        #endregion

        #region Propriétés Publiques
        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Mathf.Clamp01(value);
                AudioListener.volume = masterVolume;
            }
        }

        public float AmbienceVolume
        {
            get => ambienceVolume;
            set
            {
                ambienceVolume = Mathf.Clamp01(value);
                UpdateActiveAmbientVolumes();
            }
        }

        public float SFXVolume
        {
            get => sfxVolume;
            set => sfxVolume = Mathf.Clamp01(value);
        }

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                if (musicSource != null) musicSource.volume = musicVolume;
            }
        }
        #endregion

        #region Cycle de Vie Unity
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            SetupInternalAudioSources();
            AudioListener.volume = masterVolume;
        }

        private void Start()
        {
            // Lance l'ambiance par défaut au démarrage
            if (defaultIslandAmbientClip != null)
            {
                PlayAmbient(defaultIslandAmbientClip, defaultAmbientVolume, 2f);
            }
        }
        #endregion

        #region Gestion des Zones d'Ambiance (Triggers)
        /// <summary>
        /// Appelé par un AmbientAudioZone lorsqu'un joueur entre dans son trigger.
        /// </summary>
        public void EnterAmbientZone(AmbientAudioZone zone)
        {
            if (!activeZones.Contains(zone))
            {
                activeZones.Add(zone);
                // Trie par priorité décroissante (la priorité la plus haute en tête)
                activeZones.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }

            EvaluateCurrentAmbientZone();
        }

        /// <summary>
        /// Appelé par un AmbientAudioZone lorsqu'un joueur sort de son trigger.
        /// </summary>
        public void ExitAmbientZone(AmbientAudioZone zone)
        {
            if (activeZones.Contains(zone))
            {
                activeZones.Remove(zone);
            }

            EvaluateCurrentAmbientZone();
        }

        private void EvaluateCurrentAmbientZone()
        {
            if (activeZones.Count > 0)
            {
                // La zone prioritaire est le premier élément
                AmbientAudioZone highestPriorityZone = activeZones[0];
                if (highestPriorityZone.AmbientClip != null && highestPriorityZone.AmbientClip != currentAmbientClip)
                {
                    PlayAmbient(highestPriorityZone.AmbientClip, highestPriorityZone.TargetVolume, highestPriorityZone.TransitionDuration);
                }
            }
            else
            {
                // Retour à l'ambiance par défaut de l'île
                if (defaultIslandAmbientClip != null && defaultIslandAmbientClip != currentAmbientClip)
                {
                    PlayAmbient(defaultIslandAmbientClip, defaultAmbientVolume, 3f);
                }
            }
        }
        #endregion

        #region Crossfade d'Ambiance Fluide
        /// <summary>
        /// Enclenche un fondu enchaîné vers un nouveau clip d'ambiance.
        /// </summary>
        public void PlayAmbient(AudioClip clip, float targetVol = 0.8f, float fadeDuration = 3f)
        {
            if (clip == null) return;
            currentAmbientClip = clip;

            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
            }

            crossfadeCoroutine = StartCoroutine(CrossfadeAmbientRoutine(clip, targetVol, fadeDuration));
        }

        private IEnumerator CrossfadeAmbientRoutine(AudioClip nextClip, float targetVol, float duration)
        {
            AudioSource currentSource = isUsingSourceA ? ambientSourceA : ambientSourceB;
            AudioSource newSource = isUsingSourceA ? ambientSourceB : ambientSourceA;

            newSource.clip = nextClip;
            newSource.volume = 0f;
            newSource.Play();

            float startVol = currentSource.volume;
            float finalTargetVol = targetVol * ambienceVolume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Courbes douces de fondu croisé
                currentSource.volume = Mathf.Lerp(startVol, 0f, progress);
                newSource.volume = Mathf.Lerp(0f, finalTargetVol, progress);

                yield return null;
            }

            currentSource.volume = 0f;
            currentSource.Stop();
            newSource.volume = finalTargetVol;

            // Alterne la source active
            isUsingSourceA = !isUsingSourceA;
        }

        private void UpdateActiveAmbientVolumes()
        {
            AudioSource activeSource = isUsingSourceA ? ambientSourceA : ambientSourceB;
            if (activeSource != null && activeSource.isPlaying)
            {
                // Met à jour proportionnellement le volume si le joueur ajuste les options
                activeSource.volume = defaultAmbientVolume * ambienceVolume;
            }
        }
        #endregion

        #region Effets Sonores (SFX)
        /// <summary>
        /// Joue un effet sonore 2D instantané avec variation aléatoire de pitch (bruits de survie, clics, etc.).
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1f, float pitchVariation = 0.05f)
        {
            if (clip == null || sfxSource == null) return;

            sfxSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            sfxSource.PlayOneShot(clip, volume * sfxVolume);
        }

        /// <summary>
        /// Joue un effet sonore 3D spatialisé dans le monde (ex: impact de flèche, cri d'animal).
        /// </summary>
        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f, float minDistance = 2f, float maxDistance = 35f)
        {
            if (clip == null) return;

            GameObject tempAudioObj = new GameObject($"SFX_{clip.name}");
            tempAudioObj.transform.position = position;

            AudioSource source = tempAudioObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume * sfxVolume;
            source.spatialBlend = 1f; // Son 3D pur
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.Play();

            Destroy(tempAudioObj, clip.length + 0.2f);
        }
        #endregion

        #region Musique Thématique
        /// <summary>
        /// Joue une musique avec fondu d'entrée.
        /// </summary>
        public void PlayMusic(AudioClip clip, float fadeDuration = 2f)
        {
            if (clip == null || musicSource == null) return;
            StartCoroutine(FadeMusicRoutine(clip, fadeDuration));
        }

        private IEnumerator FadeMusicRoutine(AudioClip clip, float duration)
        {
            if (musicSource.isPlaying)
            {
                float initialVol = musicSource.volume;
                for (float t = 0; t < duration * 0.5f; t += Time.deltaTime)
                {
                    musicSource.volume = Mathf.Lerp(initialVol, 0f, t / (duration * 0.5f));
                    yield return null;
                }
                musicSource.Stop();
            }

            musicSource.clip = clip;
            musicSource.Play();

            for (float t = 0; t < duration * 0.5f; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(0f, musicVolume, t / (duration * 0.5f));
                yield return null;
            }
            musicSource.volume = musicVolume;
        }
        #endregion

        #region Initialisation des Sources Audio
        private void SetupInternalAudioSources()
        {
            if (ambientSourceA == null)
            {
                ambientSourceA = gameObject.AddComponent<AudioSource>();
                ambientSourceA.loop = true;
                ambientSourceA.playOnAwake = false;
                ambientSourceA.spatialBlend = 0f;
            }

            if (ambientSourceB == null)
            {
                ambientSourceB = gameObject.AddComponent<AudioSource>();
                ambientSourceB.loop = true;
                ambientSourceB.playOnAwake = false;
                ambientSourceB.spatialBlend = 0f;
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.spatialBlend = 0f;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f;
            }
        }
        #endregion
    }
}
