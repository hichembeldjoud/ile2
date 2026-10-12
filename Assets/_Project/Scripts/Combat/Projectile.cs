using UnityEngine;
using SurvivalIsland.AI;
using SurvivalIsland.Audio;

namespace SurvivalIsland.Combat
{
    /// <summary>
    /// Projectile réaliste (Flèche ou Balle) avec balistique physique,
    /// détection de collision continue (anti-tunneling), dégâts aux animaux et impact dans le décor.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival Island/Combat/Projectile")]
    public class Projectile : MonoBehaviour
    {
        #region Paramètres Balistiques
        [Header("--- Balistique & Dégâts ---")]
        [Tooltip("Dégâts infligés à la cible.")]
        [SerializeField]
        private float damage = 40f;

        [Tooltip("Vitesse initiale du projectile en m/s.")]
        [SerializeField]
        private float launchSpeed = 45f;

        [Tooltip("Activer la courbure balistique due à la gravité (idéal pour un arc).")]
        [SerializeField]
        private bool useGravity = true;

        [Tooltip("Force de la gravité appliquée au projectile.")]
        [SerializeField]
        private float gravityScale = 1.2f;

        [Tooltip("Durée de vie maximale avant disparition si aucun impact.")]
        [SerializeField]
        private float maxLifetime = 7f;

        [Tooltip("Calque des objets pouvant être touchés (Animaux, Décor, Terrain).")]
        [SerializeField]
        private LayerMask hitLayers = ~0;
        #endregion

        #region Audio & Effets Visuels
        [Header("--- Impacts (VFX & SFX) ---")]
        [Tooltip("Préfabriqué d'effet de particules lors de l'impact.")]
        [SerializeField]
        private GameObject impactVfxPrefab;

        [Tooltip("Clip audio d'impact sur de la chair (animal).")]
        [SerializeField]
        private AudioClip fleshImpactAudio;

        [Tooltip("Clip audio d'impact sur une surface solide (pierre, bois, sable).")]
        [SerializeField]
        private AudioClip solidImpactAudio;
        #endregion

        #region Variables Internes
        private Vector3 velocity;
        private Vector3 previousPosition;
        private bool hasHit = false;
        private float lifeTimer = 0f;
        #endregion

        #region Cycle de Vie Unity
        public void Launch(Vector3 direction, float speedMultiplier = 1f, float damageMultiplier = 1f)
        {
            velocity = direction.normalized * (launchSpeed * speedMultiplier);
            damage *= damageMultiplier;
            previousPosition = transform.position;
            hasHit = false;
        }

        private void Start()
        {
            if (velocity == Vector3.zero)
            {
                // Vitesse par défaut si lancé sans méthode Launch explicite
                velocity = transform.forward * launchSpeed;
                previousPosition = transform.position;
            }
        }

        private void Update()
        {
            if (hasHit) return;

            lifeTimer += Time.deltaTime;
            if (lifeTimer >= maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Application de la gravité
            if (useGravity)
            {
                velocity += Physics.gravity * (gravityScale * Time.deltaTime);
            }

            // Calcul de la nouvelle position
            Vector3 currentPosition = transform.position;
            Vector3 nextPosition = currentPosition + (velocity * Time.deltaTime);
            Vector3 travelVector = nextPosition - currentPosition;
            float travelDistance = travelVector.magnitude;

            // Détection continue par SphereCast pour éviter le tunneling et garantir de toucher les animaux
            RaycastHit[] hits = Physics.SphereCastAll(currentPosition, 0.22f, travelVector.normalized, travelDistance, hitLayers, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var h in hits)
                {
                    // Ignore le joueur et ses composants
                    if (h.collider.CompareTag("Player") || h.collider.GetComponentInParent<SurvivalIsland.Player.PlayerController>() != null)
                    {
                        continue;
                    }

                    // Impact détecté sur un animal ou le décor
                    transform.position = h.point;
                    HandleImpact(h);
                    return;
                }
            }

            // Déplacement effectif
            transform.position = nextPosition;
            if (velocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(velocity);
            }

            previousPosition = currentPosition;
        }
        #endregion

        #region Gestion de l'Impact
        private void HandleImpact(RaycastHit hit)
        {
            hasHit = true;

            // 1. Dégâts infligés aux animaux
            AnimalAI animal = hit.collider.GetComponentInParent<AnimalAI>();
            if (animal != null)
            {
                animal.TakeDamage(damage, transform.position);

                if (fleshImpactAudio != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFXAtPosition(fleshImpactAudio, hit.point, 0.9f);
                }

                // Plante la flèche sur l'animal pour un réalisme accru
                transform.SetParent(hit.collider.transform);
                Destroy(this); // Désactive le script projectile mais garde le mesh planté
                Destroy(gameObject, 15f);
                return;
            }

            // 2. Impact sur le décor (Rocher, Arbre, Sol)
            if (solidImpactAudio != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(solidImpactAudio, hit.point, 0.8f);
            }

            // Instanciation de l'effet de particules
            if (impactVfxPrefab != null)
            {
                GameObject vfx = Instantiate(impactVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(vfx, 2f);
            }

            // Plante la flèche dans le décor ou la détruit
            transform.SetParent(hit.collider.transform);
            Destroy(this);
            Destroy(gameObject, 20f);
        }
        #endregion
    }
}
