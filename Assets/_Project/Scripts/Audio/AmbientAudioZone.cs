using UnityEngine;

namespace SurvivalIsland.Audio
{
    /// <summary>
    /// Type d'ambiance géographique sur l'île tropicale.
    /// </summary>
    public enum AmbientZoneType
    {
        Beach,          // Plage : bruit des vagues, ressac, brise marine
        Jungle,         // Jungle : insectes tropicaux, canopée, oiseaux exotiques
        Mountain,       // Altitude : vent en rafales, sensation de hauteur
        Cave,           // Grotte : réverbération sourde, gouttes d'eau
        Custom          // Zone personnalisée
    }

    /// <summary>
    /// Zone de déclenchement (Trigger) pour modifier l'ambiance sonore selon l'emplacement du joueur.
    /// À placer sur un GameObject avec un Collider en "Is Trigger".
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Survival Island/Audio/Ambient Audio Zone")]
    public class AmbientAudioZone : MonoBehaviour
    {
        #region Configuration de la Zone
        [Header("--- Profil de la Zone ---")]
        [Tooltip("Type géographique de la zone sonore.")]
        [SerializeField]
        private AmbientZoneType zoneType = AmbientZoneType.Beach;

        [Tooltip("Nom descriptif de la zone (ex: 'Plage Sud', 'Cœur de la Jungle').")]
        [SerializeField]
        private string zoneName = "Nouvelle Zone";

        [Tooltip("Piste audio d'ambiance en boucle pour cette zone.")]
        [SerializeField]
        private AudioClip ambientClip;

        [Tooltip("Volume cible d'écoute de l'ambiance (entre 0 et 1).")]
        [SerializeField, Range(0f, 1f)]
        private float targetVolume = 0.8f;

        [Tooltip("Durée de la transition sonore (crossfade) en secondes lors de l'entrée/sortie.")]
        [SerializeField, Range(0.5f, 10f)]
        private float transitionDuration = 3f;

        [Tooltip("Priorité de la zone si plusieurs zones se chevauchent (valeur plus haute = prioritaire).")]
        [SerializeField]
        private int priority = 0;
        #endregion

        #region Gizmos & Affichage Scène
        [Header("--- Visualisation dans l'Éditeur ---")]
        [Tooltip("Couleur du Gizmo dans la vue Scène de Unity.")]
        [SerializeField]
        private Color gizmoColor = new Color(0.95f, 0.85f, 0.3f, 0.35f);

        [Tooltip("Dessiner le volume en fil de fer ou en solide.")]
        [SerializeField]
        private bool drawSolidGizmo = true;
        #endregion

        #region Propriétés Publiques
        public AmbientZoneType ZoneType => zoneType;
        public string ZoneName => zoneName;
        public AudioClip AmbientClip => ambientClip;
        public float TargetVolume => targetVolume;
        public float TransitionDuration => transitionDuration;
        public int Priority => priority;
        #endregion

        #region Cycle de Vie Unity
        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[AmbientAudioZone] Le collider sur '{gameObject.name}' a été automatiquement défini en 'Is Trigger'.", this);
            }
        }

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Attribue une couleur par défaut adaptée au type de zone
            SetDefaultColorByZoneType();
        }

        private void OnValidate()
        {
            SetDefaultColorByZoneType();
        }
        #endregion

        #region Détection du Joueur
        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other))
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.EnterAmbientZone(this);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other))
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.ExitAmbientZone(this);
                }
            }
        }

        private bool IsPlayer(Collider col)
        {
            // Vérifie le tag Player ou la présence de la caméra principale
            if (col.CompareTag("Player")) return true;
            if (col.GetComponent<Camera>() != null && col.GetComponent<Camera>() == Camera.main) return true;
            return false;
        }
        #endregion

        #region Helpers & Visualisation Gizmos
        private void SetDefaultColorByZoneType()
        {
            switch (zoneType)
            {
                case AmbientZoneType.Beach:
                    gizmoColor = new Color(1.0f, 0.84f, 0.0f, 0.3f); // Jaune sable / Océan doré
                    break;
                case AmbientZoneType.Jungle:
                    gizmoColor = new Color(0.13f, 0.75f, 0.25f, 0.3f); // Vert tropical luxuriant
                    break;
                case AmbientZoneType.Mountain:
                    gizmoColor = new Color(0.35f, 0.7f, 0.95f, 0.3f); // Bleu altitude / vent frais
                    break;
                case AmbientZoneType.Cave:
                    gizmoColor = new Color(0.4f, 0.2f, 0.5f, 0.3f); // Pourpre / ombre souterraine
                    break;
            }
        }

        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = gizmoColor;

            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                if (drawSolidGizmo)
                {
                    Gizmos.DrawCube(box.center, box.size);
                }
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.9f);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                if (drawSolidGizmo)
                {
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                }
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.9f);
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
        }

        /// <summary>
        /// Configure programmatiquement la zone (utilisé notamment par les outils éditeurs).
        /// </summary>
        public void ConfigureZone(AmbientZoneType type, string name, int zonePriority, float vol = 0.8f)
        {
            zoneType = type;
            zoneName = name;
            priority = zonePriority;
            targetVolume = vol;
            SetDefaultColorByZoneType();
        }
        #endregion
    }
}
