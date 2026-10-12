using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using SurvivalIsland.AI;
using SurvivalIsland.Combat;
using SurvivalIsland.Player;

namespace SurvivalIsland.Environment
{
    /// <summary>
    /// Gestionnaire d'exécution autonome de la faune sauvage de l'île.
    /// Garantit qu'à tout moment en cours de jeu (Play Mode) :
    /// 1. Des animaux de multiples espèces peuplent l'île, même si la scène a été ouverte sans eux.
    /// 2. Tous les animaux sont alignés au sol et raccordés au NavMesh.
    /// 3. Le joueur dispose du WeaponSystem pour pouvoir chasser au clic gauche ou à l'arc/fusil.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    [AddComponentMenu("Survival Island/Environment/Island Wildlife Runtime")]
    public class IslandWildlifeRuntime : MonoBehaviour
    {
        [Header("--- Paramètres de Spawn Runtime ---")]
        [Tooltip("Repeupler automatiquement si aucun animal n'est présent dans la scène.")]
        [SerializeField]
        private bool autoPopulateIfEmpty = true;

        private void Awake()
        {
            EnsurePlayerReadyForHunting();
        }

        private void Start()
        {
            if (autoPopulateIfEmpty)
            {
                EnsureIslandFaunaActive();
            }
            EnsureTree01Vegetation();
        }

        /// <summary>
        /// S'assure que le joueur a le composant WeaponSystem pour attaquer et chasser.
        /// </summary>
        public static void EnsurePlayerReadyForHunting()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
                if (pc != null) player = pc.gameObject;
            }

            if (player != null)
            {
                WeaponSystem weapon = player.GetComponent<WeaponSystem>();
                if (weapon == null)
                {
                    weapon = player.AddComponent<WeaponSystem>();
                    Debug.Log("<color=#4CAF50><b>[Chasse & Combat]</b> WeaponSystem attaché au joueur avec succès ! Prêt pour la chasse.</color>");
                }
            }
        }

        /// <summary>
        /// Vérifie le nombre d'animaux vivants et génère une faune variée si aucun animal n'est présent.
        /// </summary>
        public void EnsureIslandFaunaActive()
        {
            AnimalAI[] existing = Object.FindObjectsByType<AnimalAI>(FindObjectsSortMode.None);
            if (existing != null && existing.Length > 0)
            {
                foreach (var animal in existing)
                {
                    if (animal != null)
                    {
                        animal.EnsureVisualGrounded();
                        animal.EnsureMaterialsURPLit();
                    }
                }
                Debug.Log($"<color=#4CAF50><b>[Faune Sauvage]</b> {existing.Length} animaux actifs détectés et vérifiés au sol sur l'île.</color>");
                return;
            }

            Debug.Log("<color=#FF9800><b>[Faune Sauvage]</b> Aucun animal dans la scène. Peuplement d'urgence de l'île en cours...</color>");

            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain == null) return;

            // S'assure que le NavMesh existe
            IslandNavMeshRuntime.EnsureNavMeshExists();

            GameObject faunaRoot = GameObject.Find("--- WILDLIFE & ANIMALS ---");
            if (faunaRoot == null)
            {
                faunaRoot = new GameObject("--- WILDLIFE & ANIMALS ---");
            }

            // Espèces et coordonnées de spawn
            var runtimeSpawns = new List<(string speciesName, Vector3[] coords)>()
            {
                ("Wild_Boar", new [] { new Vector3(15f, 0, -145f), new Vector3(-45f, 0, -100f), new Vector3(35f, 0, -70f), new Vector3(20f, 0, -135f), new Vector3(-30f, 0, -115f) }),
                ("Boar", new [] { new Vector3(45f, 0, -90f), new Vector3(-15f, 0, -140f) }),
                ("Rabbit", new [] { new Vector3(25f, 0, -150f), new Vector3(-15f, 0, -130f), new Vector3(50f, 0, -110f), new Vector3(-25f, 0, -85f) }),
                ("Deer", new [] { new Vector3(60f, 0, -40f), new Vector3(-60f, 0, 10f), new Vector3(20f, 0, 45f) }),
                ("Fox", new [] { new Vector3(80f, 0, -20f), new Vector3(-70f, 0, 30f) }),
                ("Wolf", new [] { new Vector3(-55f, 0, -30f), new Vector3(45f, 0, 40f), new Vector3(-85f, 0, -55f) }),
                ("Bear", new [] { new Vector3(10f, 0, 75f), new Vector3(-75f, 0, 65f) }),
                ("MountainGoat", new [] { new Vector3(0f, 0, 105f), new Vector3(-35f, 0, 95f) }),
                ("Alligator", new [] { new Vector3(-95f, 0, -110f), new Vector3(90f, 0, -80f) }),
                ("Moose", new [] { new Vector3(45f, 0, -15f), new Vector3(-35f, 0, 15f) }),
                ("Buffalo", new [] { new Vector3(65f, 0, 20f), new Vector3(-55f, 0, -5f) })
            };

            int spawnedCount = 0;
            foreach (var group in runtimeSpawns)
            {
                GameObject prefab = Resources.Load<GameObject>($"Animals/{group.speciesName}");
                if (prefab == null && group.speciesName == "Wolf")
                {
                    prefab = Resources.Load<GameObject>("Animals/Wolf_Dedicated");
                }
                if (prefab == null && (group.speciesName == "Boar" || group.speciesName == "Wild_Boar"))
                {
                    prefab = Resources.Load<GameObject>("Animals/Wild_Boar");
                    if (prefab == null) prefab = Resources.Load<GameObject>("Animals/Boar");
                }

                if (prefab == null && (group.speciesName == "Deer" || group.speciesName == "Realistic_Deer"))
                {
                    prefab = Resources.Load<GameObject>("Animals/Realistic_Deer");
                    if (prefab == null) prefab = Resources.Load<GameObject>("Animals/Deer");
                }

                if (prefab == null) continue;

                for (int i = 0; i < group.coords.Length; i++)
                {
                    Vector3 pos = group.coords[i];
                    float y = terrain.SampleHeight(pos);
                    Vector3 spawnPos = new Vector3(pos.x, y + 0.15f, pos.z);

                    GameObject inst = Instantiate(prefab, spawnPos, Quaternion.Euler(0, Random.Range(0, 360), 0), faunaRoot.transform);
                    inst.name = $"{group.speciesName}_{i + 1}";

                    NavMeshAgent agent = inst.GetComponent<NavMeshAgent>();
                    if (agent != null && NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 15f, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                    }

                    AnimalAI ai = inst.GetComponent<AnimalAI>();
                    if (ai != null)
                    {
                        ai.EnsureVisualGrounded();
                        ai.EnsureMaterialsURPLit();
                    }

                    spawnedCount++;
                }
            }

            Debug.Log($"<color=#4CAF50><b>[Faune Sauvage]</b> {spawnedCount} animaux de multiples espèces créés avec succès sur l'île !</color>");
        }

        /// <summary>
        /// Vérifie et place les arbres 59-Tree01 sur l'île.
        /// </summary>
        public void EnsureTree01Vegetation()
        {
            GameObject treeFolder = GameObject.Find("--- 59-TREE01 VEGETATION ---");
            if (treeFolder != null && treeFolder.transform.childCount > 0) return;

            GameObject treePrefab = Resources.Load<GameObject>("Environment/Tree01");
            if (treePrefab == null) return;

            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain == null) return;

            if (treeFolder == null) treeFolder = new GameObject("--- 59-TREE01 VEGETATION ---");

            Vector3[] spawnCoords = new Vector3[]
            {
                new Vector3(25f, 0, -110f),
                new Vector3(-35f, 0, -80f),
                new Vector3(50f, 0, -50f),
                new Vector3(-60f, 0, -20f),
                new Vector3(10f, 0, 30f),
                new Vector3(-20f, 0, 60f),
                new Vector3(40f, 0, 70f),
                new Vector3(-50f, 0, 90f),
                new Vector3(0f, 0, 100f),
                new Vector3(65f, 0, -85f),
                new Vector3(-45f, 0, -125f),
                new Vector3(30f, 0, 10f)
            };

            for (int i = 0; i < spawnCoords.Length; i++)
            {
                Vector3 pt = spawnCoords[i];
                float y = terrain.SampleHeight(pt);
                Vector3 pos = new Vector3(pt.x, y, pt.z);

                GameObject tree = Instantiate(treePrefab, pos, Quaternion.Euler(0, Random.Range(0, 360f), 0), treeFolder.transform);
                float scale = Random.Range(0.85f, 1.25f);
                tree.transform.localScale = new Vector3(scale, scale, scale);
                tree.name = $"Tree01_{i + 1}";

                // Nettoyage et optimisation des ombres
                Camera strayCam = tree.GetComponentInChildren<Camera>(true);
                if (strayCam != null) Destroy(strayCam.gameObject);

                Light strayLight = tree.GetComponentInChildren<Light>(true);
                if (strayLight != null) Destroy(strayLight.gameObject);

                Renderer[] renderers = tree.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r != null && r.gameObject.name.ToLower().Contains("leaves"))
                    {
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
            }

            Debug.Log("<color=#4CAF50><b>[Arbres 59-Tree01]</b> Végétation Tree01 vérifiée et active sur l'île !</color>");
        }
    }
}
