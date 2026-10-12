using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using SurvivalIsland.AI;

namespace SurvivalIsland.Environment
{
    /// <summary>
    /// Assure à l'exécution (Play mode) que l'île possède un NavMesh valide et actif.
    /// Si aucun NavMesh n'a été cuit dans l'éditeur, ce composant cuit automatiquement le NavMesh
    /// du terrain en temps réel et connecte tous les animaux de l'île.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)] // S'exécute avant tous les scripts d'animaux
    [AddComponentMenu("Survival Island/Environment/Island NavMesh Runtime")]
    public class IslandNavMeshRuntime : MonoBehaviour
    {
        private void Awake()
        {
            OptimizeTerrainPerformance();
            EnsureNavMeshExists();
        }

        private static void OptimizeTerrainPerformance()
        {
            Application.targetFrameRate = 60;

            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain != null)
            {
                terrain.heightmapPixelError = 5f;
                terrain.treeDistance = 250f;
                terrain.treeBillboardDistance = 50f;
                terrain.detailObjectDistance = 60f;
                terrain.drawInstanced = true;
            }

            // 1. Suppression des caméras parasites exportées par mégarde dans les modèles 3D
            Camera[] allCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in allCams)
            {
                if (cam == null) continue;
                if (cam.CompareTag("MainCamera")) continue;
                if (cam.name.Contains("vThirdPersonCamera") || cam.name.Contains("ThirdPersonCamera") || cam.name.Contains("Minimap")) continue;
                if (cam.transform.parent != null && (cam.transform.parent.name.Contains("Tree") || cam.transform.parent.name.Contains("VisualModel")))
                {
                    Destroy(cam.gameObject);
                }
            }

            // 2. Suppression des lumières parasites et désactivation des ombres ponctuelles
            Light[] allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in allLights)
            {
                if (l == null) continue;
                if (l.type != LightType.Directional)
                {
                    if (l.transform.parent != null && (l.transform.parent.name.Contains("Tree") || l.transform.parent.name.Contains("VisualModel")))
                    {
                        Destroy(l.gameObject);
                    }
                    else
                    {
                        l.shadows = LightShadows.None;
                    }
                }
            }

            // 3. Optimisation des MeshRenderers de feuillage (seul le tronc projette l'ombre pour un gain FPS massif)
            Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in allRenderers)
            {
                if (r == null) continue;
                if (r.gameObject.name.ToLower().Contains("leaves"))
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        private void Start()
        {
            // Vérification et ajustement au démarrage
            SnapAnimalsToNavMesh();
        }

        /// <summary>
        /// Vérifie la présence d'un NavMesh et le génère en temps réel si manquant.
        /// </summary>
        public static void EnsureNavMeshExists()
        {
            // Teste si un point sur l'île est couvert par un NavMesh
            bool hasNavMesh = NavMesh.SamplePosition(new Vector3(0, 10f, 0), out _, 150f, NavMesh.AllAreas);

            if (!hasNavMesh)
            {
                Debug.Log("<color=#FF9800><b>[NavMesh Runtime]</b> Aucun NavMesh détecté. Cuisson automatique du NavMesh de l'île en cours...</color>");

                Terrain terrain = Object.FindFirstObjectByType<Terrain>();
                GameObject targetObj = terrain != null ? terrain.gameObject : null;

                if (targetObj == null)
                {
                    targetObj = GameObject.Find("--- TROPICAL ISLAND TERRAIN ---");
                }

                if (targetObj != null)
                {
                    NavMeshSurface surface = targetObj.GetComponent<NavMeshSurface>();
                    if (surface == null)
                    {
                        surface = targetObj.AddComponent<NavMeshSurface>();
                    }
                    surface.collectObjects = CollectObjects.All;
                    surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                    surface.BuildNavMesh();

                    Debug.Log("<color=#4CAF50><b>[NavMesh Runtime]</b> NavMesh cuit avec succès pour l'ensemble de l'île !</color>");
                }
                else
                {
                    Debug.LogWarning("[NavMesh Runtime] Aucun terrain trouvé pour générer le NavMesh.");
                }
            }
        }

        /// <summary>
        /// Raccorde tous les NavMeshAgents de la scène au NavMesh fraîchement créé.
        /// </summary>
        public static void SnapAnimalsToNavMesh()
        {
            AnimalAI[] animals = Object.FindObjectsByType<AnimalAI>(FindObjectsSortMode.None);
            int snapped = 0;

            foreach (var a in animals)
            {
                NavMeshAgent agent = a.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    if (!agent.isOnNavMesh)
                    {
                        if (NavMesh.SamplePosition(a.transform.position, out NavMeshHit hit, 15f, NavMesh.AllAreas))
                        {
                            agent.Warp(hit.position);
                            snapped++;
                        }
                    }
                }
            }

            if (snapped > 0)
            {
                Debug.Log($"<color=#4CAF50><b>[NavMesh Runtime]</b> {snapped} animaux connectés et alignés sur le NavMesh !</color>");
            }
        }
    }
}
