using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalIsland.Player
{
    /// <summary>
    /// Caméra orbitale à la 3ème personne fluide avec détection d'obstacles et zoom.
    /// Suit le joueur en douceur et gère les collisions avec le décor tropical.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival Island/Player/Third Person Camera")]
    public class ThirdPersonCamera : MonoBehaviour
    {
        #region Cible & Décalages
        [Header("--- Cible & Positionnement ---")]
        [Tooltip("Cible suivie par la caméra (généralement le GameObject du Joueur).")]
        [SerializeField]
        private Transform target;

        [Tooltip("Décalage de la cible (hauteur de visée vers les épaules/tête du joueur).")]
        [SerializeField]
        private Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);

        [Tooltip("Distance par défaut de la caméra par rapport au joueur.")]
        [SerializeField]
        private float distance = 3.8f;

        [Tooltip("Distance minimale (zoom maximal).")]
        [SerializeField]
        private float minDistance = 1.2f;

        [Tooltip("Distance maximale.")]
        [SerializeField]
        private float maxDistance = 7.0f;
        #endregion

        #region Rotation & Sensibilité
        [Header("--- Contrôles & Sensibilité ---")]
        [Tooltip("Sensibilité horizontale de la souris.")]
        [SerializeField]
        private float mouseSensitivityX = 0.15f;

        [Tooltip("Sensibilité verticale de la souris.")]
        [SerializeField]
        private float mouseSensitivityY = 0.15f;

        [Tooltip("Limite minimale de l'angle vertical (regard vers le haut).")]
        [SerializeField]
        private float minPitch = -35f;

        [Tooltip("Limite maximale de l'angle vertical (regard vers le bas).")]
        [SerializeField]
        private float maxPitch = 70f;

        [Tooltip("Lissage de la rotation de la caméra.")]
        [SerializeField]
        private float rotationSmoothTime = 0.05f;
        #endregion

        #region Collision & Évitement
        [Header("--- Collision Décor ---")]
        [Tooltip("Calque des objets bloquant la vue de la caméra (Terrain, Rochers, Arbres).")]
        [SerializeField]
        private LayerMask collisionLayers = ~0;

        [Tooltip("Rayon du test d'évitement de collision.")]
        [SerializeField]
        private float collisionRadius = 0.22f;

        [Tooltip("Vitesse d'ajustement lors d'une collision.")]
        [SerializeField]
        private float collisionSmoothSpeed = 12f;
        #endregion

        #region Variables Internes
        private float yaw = 0f;
        private float pitch = 15f;
        private float targetYaw = 0f;
        private float targetPitch = 15f;
        private float currentDistance;
        private float targetDistance;
        private Vector2 rotationVelocity;
        #endregion

        #region Cycle de Vie Unity
        private void Start()
        {
            if (target == null)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    target = player.transform;
                }
            }

            currentDistance = distance;
            targetDistance = distance;

            yaw = transform.eulerAngles.y;
            targetYaw = yaw;
            pitch = transform.eulerAngles.x;
            targetPitch = pitch;

            // Verrouille le curseur pour le confort de jeu
            LockCursor(true);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            ProcessCameraInputs();
            CalculatePositionAndRotation();
        }
        #endregion

        #region Entrées Caméra
        private void ProcessCameraInputs()
        {
            Vector2 lookDelta = Vector2.zero;

            // 1. Souris
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                lookDelta = mouse.delta.ReadValue();

                // Zoom molette
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    targetDistance = Mathf.Clamp(targetDistance - (scroll * 0.005f), minDistance, maxDistance);
                }
            }

            // 2. Manette (Stick droit)
            Gamepad gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 stickRight = gp.rightStick.ReadValue();
                if (stickRight.sqrMagnitude > 0.02f)
                {
                    lookDelta += stickRight * 12f;
                }
            }

            targetYaw += lookDelta.x * mouseSensitivityX;
            targetPitch -= lookDelta.y * mouseSensitivityY;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

            // Déverrouille la souris avec Échap si nécessaire
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                LockCursor(Cursor.lockState != CursorLockMode.Locked);
            }
        }
        #endregion

        #region Calcul Position & Collision
        private void CalculatePositionAndRotation()
        {
            yaw = Mathf.SmoothDamp(yaw, targetYaw, ref rotationVelocity.x, rotationSmoothTime);
            pitch = Mathf.SmoothDamp(pitch, targetPitch, ref rotationVelocity.y, rotationSmoothTime);

            Quaternion currentRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focusPoint = target.position + targetOffset;

            // Test de collision avec le relief ou obstacles pour éviter les traversées de murs/arbres
            Vector3 desiredCameraPos = focusPoint - (currentRotation * Vector3.forward * targetDistance);
            Vector3 rayDirection = (desiredCameraPos - focusPoint).normalized;

            float adjustedDistance = targetDistance;
            if (Physics.SphereCast(focusPoint, collisionRadius, rayDirection, out RaycastHit hit, targetDistance, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                adjustedDistance = Mathf.Clamp(hit.distance - collisionRadius, minDistance, targetDistance);
            }

            currentDistance = Mathf.Lerp(currentDistance, adjustedDistance, Time.deltaTime * collisionSmoothSpeed);

            transform.position = focusPoint - (currentRotation * Vector3.forward * currentDistance);
            transform.rotation = currentRotation;
        }

        private void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
        #endregion

        #region Méthode Publique
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
        #endregion
    }
}
