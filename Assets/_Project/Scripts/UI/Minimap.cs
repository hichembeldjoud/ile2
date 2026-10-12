using UnityEngine;
using UnityEngine.UI;

namespace SurvivalIsland.UI
{
    /// <summary>
    /// Contrôleur de la Minimap avec caméra orthographique pointant verticalement sur le joueur.
    /// Génère ou projette une RenderTexture sur un composant UI RawImage,
    /// gère la rotation par rapport au joueur ou au Nord, et permet de zoomer.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival Island/UI/Minimap")]
    public class Minimap : MonoBehaviour
    {
        #region Cible & Caméra
        [Header("--- Cible & Caméra Orthographique ---")]
        [Tooltip("Cible suivie par la minimap (par défaut le Joueur).")]
        [SerializeField]
        private Transform target;

        [Tooltip("Caméra orthographique de la minimap.")]
        [SerializeField]
        private Camera minimapCamera;

        [Tooltip("Hauteur de la caméra au-dessus du joueur en mètres.")]
        [SerializeField]
        private float cameraHeight = 50f;

        [Tooltip("Taille orthographique de la caméra (niveau de zoom).")]
        [SerializeField]
        private float orthographicSize = 30f;

        [Tooltip("La minimap doit-elle tourner avec la rotation du joueur ? (False = Nord en haut).")]
        [SerializeField]
        private bool rotateWithPlayer = true;
        #endregion

        #region Rendu & Interface UI
        [Header("--- Rendu & Affichage UI ---")]
        [Tooltip("Composant RawImage affichant la RenderTexture de la minimap dans le HUD.")]
        [SerializeField]
        private RawImage minimapRawImage;

        [Tooltip("Résolution de la RenderTexture de la minimap (ex: 512 pour 512x512).")]
        [SerializeField]
        private int renderTextureResolution = 512;

        [Tooltip("Calques visibles sur la caméra de minimap.")]
        [SerializeField]
        private LayerMask minimapLayers = ~0;
        #endregion

        #region Variables Internes
        private RenderTexture minimapRenderTexture;
        #endregion

        #region Cycle de Vie Unity
        private void Awake()
        {
            EnsureMinimapCameraSetup();
        }

        private void Start()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null || minimapCamera == null) return;

            // 1. Positionnement vertical au-dessus du joueur
            Vector3 newPosition = target.position;
            newPosition.y += cameraHeight;
            minimapCamera.transform.position = newPosition;

            // 2. Orientation (Orientation joueur ou Nord fixé)
            if (rotateWithPlayer)
            {
                minimapCamera.transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
            }
            else
            {
                minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        private void OnDestroy()
        {
            // Libération propre de la mémoire de la RenderTexture créée dynamiquement
            if (minimapRenderTexture != null)
            {
                minimapRenderTexture.Release();
                Destroy(minimapRenderTexture);
            }
        }
        #endregion

        #region Configuration & Helpers
        private void EnsureMinimapCameraSetup()
        {
            // Recherche ou crée la caméra dédiée à la minimap
            if (minimapCamera == null)
            {
                Transform camTransform = transform.Find("Minimap_Camera");
                if (camTransform != null)
                {
                    minimapCamera = camTransform.GetComponent<Camera>();
                }
                else
                {
                    GameObject camObj = new GameObject("Minimap_Camera");
                    camObj.transform.SetParent(transform);
                    minimapCamera = camObj.AddComponent<Camera>();
                }
            }

            // Réglages orthographiques optimaux
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = orthographicSize;
            minimapCamera.nearClipPlane = 0.5f;
            minimapCamera.farClipPlane = cameraHeight + 100f;
            minimapCamera.cullingMask = minimapLayers;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.1f, 0.25f, 0.2f, 1f); // Vert île tropicale

            // Création automatique de la RenderTexture si aucune n'est assignée
            if (minimapCamera.targetTexture == null)
            {
                minimapRenderTexture = new RenderTexture(renderTextureResolution, renderTextureResolution, 16, RenderTextureFormat.ARGB32)
                {
                    name = "Minimap_RenderTexture",
                    filterMode = FilterMode.Bilinear
                };
                minimapRenderTexture.Create();

                minimapCamera.targetTexture = minimapRenderTexture;
            }

            // Liaison automatique avec le RawImage du HUD
            if (minimapRawImage != null && minimapCamera.targetTexture != null)
            {
                minimapRawImage.texture = minimapCamera.targetTexture;
            }
        }

        /// <summary>
        /// Ajuste le niveau de zoom de la minimap.
        /// </summary>
        public void SetZoom(float size)
        {
            orthographicSize = Mathf.Clamp(size, 10f, 80f);
            if (minimapCamera != null)
            {
                minimapCamera.orthographicSize = orthographicSize;
            }
        }

        /// <summary>
        /// Définit la cible suivie par la minimap.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// Assigne le composant RawImage d'affichage.
        /// </summary>
        public void SetRawImage(RawImage rawImage)
        {
            minimapRawImage = rawImage;
            if (minimapCamera != null && minimapCamera.targetTexture != null)
            {
                minimapRawImage.texture = minimapCamera.targetTexture;
            }
        }
        #endregion
    }
}
