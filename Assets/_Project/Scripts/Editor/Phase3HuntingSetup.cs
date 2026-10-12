#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using SurvivalIsland.AI;
using SurvivalIsland.Combat;
using SurvivalIsland.Items;

namespace SurvivalIsland.Editor
{
    /// <summary>
    /// Outil d'automatisation Editor pour configurer l'écosystème animal,
    /// le système d'armes (Arc/Fusil) et le loot de viande pour la Phase 3.
    /// </summary>
    public static class Phase3HuntingSetup
    {
        [MenuItem("Survival Island/Phase 3 - Configurer la Chasse, Animaux et Armes", false, 31)]
        public static void SetupPhase3Scene()
        {
            Undo.SetCurrentGroupName("Setup Phase 3 Survival Island");
            int group = Undo.GetCurrentGroup();

            Debug.Log("<color=#4CAF50><b>[Survival Island]</b> Démarrage de la configuration automatique de la Phase 3...</color>");

            // 1. Génération de l'Animator Controller pour les animaux
            AnimatorController animalAnimController = AnimalAnimatorControllerBuilder.GenerateAnimalAnimatorController();

            // 2. Création du Préfabriqué de Viande (Meat Item)
            GameObject meatPrefab = CreateOrGetMeatPrefab();

            // 3. Configuration du Joueur avec le WeaponSystem
            SetupPlayerWeapons();

            // 4. Création des Animaux Sauvages & de l'Écosystème
            AnimalPackManager.PopulateIslandWithAnimals();

            Undo.CollapseUndoOperations(group);
            Debug.Log("<color=#4CAF50><b>[Survival Island]</b> Configuration Phase 3 terminée ! Animaux sur NavMesh, Arme équipée et Viande configurés.</color>");
        }

        private static void SetupPlayerWeapons()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[Setup Phase 3] Aucun GameObject avec le tag 'Player' trouvé. Veuillez d'abord exécuter la Phase 2.");
                return;
            }

            WeaponSystem weaponSystem = player.GetComponent<WeaponSystem>();
            if (weaponSystem == null)
            {
                weaponSystem = Undo.AddComponent<WeaponSystem>(player);
            }

            // Création du FirePoint si inexistant
            Transform firePoint = player.transform.Find("FirePoint");
            if (firePoint == null)
            {
                GameObject fp = new GameObject("FirePoint");
                fp.transform.SetParent(player.transform);
                fp.transform.localPosition = new Vector3(0.35f, 1.45f, 0.65f);
                Undo.RegisterCreatedObjectUndo(fp, "Create FirePoint");
                firePoint = fp.transform;
            }

            // Assignation par réflexion pour l'inspecteur
            var fpField = typeof(WeaponSystem).GetField("firePoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fpField?.SetValue(weaponSystem, firePoint);
        }

        private static GameObject CreateOrGetMeatPrefab()
        {
            string prefabPath = "Assets/_Project/Prefabs/Items/Meat_Loot.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null) return existingPrefab;

            // Création programmatique propre du Prefab de viande
            GameObject tempMeat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tempMeat.name = "Meat_Loot";
            tempMeat.transform.localScale = new Vector3(0.45f, 0.18f, 0.45f);

            // Trigger Collider
            Collider col = tempMeat.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // Composant MeatItem
            tempMeat.AddComponent<MeatItem>();

            // Création et sauvegarde de l'asset Prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempMeat, prefabPath);
            Object.DestroyImmediate(tempMeat);

            Debug.Log($"<color=#4CAF50><b>[Prefab Builder]</b> Prefab de Viande créé à : {prefabPath}</color>");
            return prefab;
        }

        private static void SetupWildBoars(AnimatorController animController, GameObject meatPrefab)
        {
            GameObject faunaRoot = GameObject.Find("--- WILDLIFE & ANIMALS ---");
            if (faunaRoot == null)
            {
                faunaRoot = new GameObject("--- WILDLIFE & ANIMALS ---");
                Undo.RegisterCreatedObjectUndo(faunaRoot, "Create Fauna Root");
            }

            // Emplacements d'exemple sur l'île
            Vector3[] spawnPositions = new Vector3[]
            {
                new Vector3(15f, 0.5f, 25f),
                new Vector3(-20f, 0.5f, 30f),
                new Vector3(5f, 0.5f, 50f)
            };

            for (int i = 0; i < spawnPositions.Length; i++)
            {
                string boarName = $"Tropical_Boar_{i + 1}";
                Transform existing = faunaRoot.transform.Find(boarName);
                if (existing != null) continue;

                GameObject boarObj = new GameObject(boarName);
                boarObj.transform.SetParent(faunaRoot.transform);
                boarObj.transform.position = spawnPositions[i];
                Undo.RegisterCreatedObjectUndo(boarObj, "Create " + boarName);

                // CapsuleCollider pour les dégâts et la détection
                CapsuleCollider capsule = boarObj.AddComponent<CapsuleCollider>();
                capsule.height = 1.2f;
                capsule.radius = 0.5f;
                capsule.direction = 2; // Axe Z (horizontal)
                capsule.center = new Vector3(0, 0.6f, 0);

                // NavMeshAgent
                NavMeshAgent agent = boarObj.AddComponent<NavMeshAgent>();
                agent.speed = 2.2f;
                agent.acceleration = 12f;
                agent.angularSpeed = 240f;
                agent.stoppingDistance = 0.5f;

                // Modèle visuel / Placeholder
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Boar_VisualModel";
                visual.transform.SetParent(boarObj.transform);
                visual.transform.localPosition = new Vector3(0, 0.5f, 0);
                visual.transform.localScale = new Vector3(0.8f, 0.8f, 1.4f);

                // Supprime le BoxCollider du visuel (le CapsuleCollider parent gère les collisions)
                Collider visCol = visual.GetComponent<Collider>();
                if (visCol != null) Object.DestroyImmediate(visCol);

                Animator anim = visual.AddComponent<Animator>();
                anim.runtimeAnimatorController = animController;

                // Script AnimalAI
                AnimalAI ai = boarObj.AddComponent<AnimalAI>();

                // Assignation du prefab de viande et de l'animator par réflexion
                var meatField = typeof(AnimalAI).GetField("meatPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                meatField?.SetValue(ai, meatPrefab);

                var animField = typeof(AnimalAI).GetField("animator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                animField?.SetValue(ai, anim);
            }
        }
    }
}
#endif
