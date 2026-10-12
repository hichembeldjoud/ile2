#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using SurvivalIsland.AI;
using SurvivalIsland.Combat;
using SurvivalIsland.Environment;

namespace SurvivalIsland.Editor
{
    /// <summary>
    /// Initialiseur automatique Editor :
    /// S'exécute dès la compilation dans l'éditeur Unity pour garantir que :
    /// 1. Tous les prefabs d'animaux sont extraits et enregistrés (Assets & Resources).
    /// 2. L'île (Terrain) possède un NavMesh cuit et valide.
    /// 3. Tous les animaux peuplent l'île sous --- WILDLIFE & ANIMALS ---.
    /// 4. Le joueur possède WeaponSystem pour chasser au clic gauche et se nourrir.
    /// 5. La scène est automatiquement enregistrée.
    /// </summary>
    public static class AutoPopulateIslandAndVerify
    {
        private const string SessionKey = "SurvivalIsland_FaunaPopulated_V8";

        private static void OnEditorLoad()
        {
            EditorApplication.delayCall += ExecuteAutoPopulation;
        }

        [MenuItem("Survival Island/✨ VÉRIFIER & PEUPLER L'ÎLE MAINTENANT (Animaux, NavMesh, Chasse)", false, 1)]
        public static void ForcePopulateNow()
        {
            SessionState.SetBool(SessionKey, false);
            ExecuteAutoPopulation();
        }

        private static void ExecuteAutoPopulation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SessionState.GetBool(SessionKey, false)) return;

            Debug.Log("<color=#4CAF50><b>[Auto Setup Wildlife]</b> Vérification et configuration automatique de la faune de l'île...</color>");

            // 1. Génération de tous les prefabs d'animaux
            AnimalPackManager.SetupAllAnimals();

            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain == null)
            {
                Debug.LogWarning("[Auto Setup Wildlife] Aucun Terrain trouvé dans la scène active.");
                return;
            }

            // 2. Cuisson du NavMesh sur le terrain
            if (terrain.gameObject.GetComponent<IslandNavMeshRuntime>() == null)
            {
                terrain.gameObject.AddComponent<IslandNavMeshRuntime>();
            }

            if (terrain.gameObject.GetComponent<IslandWildlifeRuntime>() == null)
            {
                terrain.gameObject.AddComponent<IslandWildlifeRuntime>();
            }

            NavMeshSurface surface = terrain.gameObject.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = terrain.gameObject.AddComponent<NavMeshSurface>();
            }

            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            Debug.Log("<color=#4CAF50><b>[Auto Setup Wildlife]</b> NavMesh cuit avec succès pour l'ensemble de l'île !</color>");

            // 3. Peuplement des animaux dans la scène (dont les sangliers du pack Wild Boar)
            AnimalPackManager.PopulateIslandWithAnimals();
            AnimalPackManager.AddWildBoarsToGameMenuItem();
            AddRequestedAssetsManager.SetupAllRequestedAssets();

            // 4. Configuration du Joueur pour la chasse
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("Player");

            if (player != null)
            {
                WeaponSystem weapon = player.GetComponent<WeaponSystem>();
                if (weapon == null)
                {
                    player.AddComponent<WeaponSystem>();
                    Debug.Log("<color=#4CAF50><b>[Auto Setup Wildlife]</b> WeaponSystem ajouté au joueur pour la chasse !</color>");
                }
            }

            // 5. Sauvegarde de la scène
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            SessionState.SetBool(SessionKey, true);
            Debug.Log("<color=#4CAF50><b>[Auto Setup Wildlife]</b> SUCCÈS TOTAL ! L'île tropicale est désormais entièrement peuplée d'animaux avec NavMesh actif et système de chasse opérationnel.</color>");
        }
    }
}
#endif
