#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using SurvivalIsland.AI;
using SurvivalIsland.Items;

namespace SurvivalIsland.Editor
{
    /// <summary>
    /// Intégration complète et automatique des 3 assets demandés :
    /// 1. realistic_deer_3d_model_2.0_demo_free_download : Animal réaliste (Cerf majestueux avec IA, fuite et loot).
    /// 2. 59-tree01 : Arbre réaliste avec matériaux URP, collider de tronc et disposition insulaire.
    /// 3. meat : Véritable modèle 3D de viande déposé au sol quand un animal est abattu.
    /// </summary>
    public static class AddRequestedAssetsManager
    {
        private const string SessionKey = "SurvivalIsland_RequestedAssets_Setup_V8";

        private static void OnEditorLoad()
        {
            EditorApplication.delayCall += ExecuteSetupIfFirstTime;
        }

        private static void ExecuteSetupIfFirstTime()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SessionState.GetBool(SessionKey, false)) return;

            SetupAllRequestedAssets();
            SessionState.SetBool(SessionKey, true);
        }

        [MenuItem("Survival Island/🌟 INTÉGRER : Cerf Réaliste, Arbre 59-Tree01 & Viande Réaliste", false, 4)]
        public static void SetupAllRequestedAssets()
        {
            Undo.SetCurrentGroupName("Setup Requested Assets (Deer, Tree01, Meat)");
            int group = Undo.GetCurrentGroup();

            Debug.Log("<color=#4CAF50><b>[Assets Manager]</b> Début de l'intégration du Cerf, de l'Arbre 59-Tree01 et de la Viande...</color>");

            EnsureDirectories();

            // 1. Configuration de la Viande Réaliste (Dossier 'meat')
            GameObject meatPrefab = SetupRealisticMeatPrefab();

            // 2. Configuration de l'Arbre Réaliste (Dossier '59-tree01')
            GameObject treePrefab = SetupTree01Prefab();

            // 3. Configuration du Cerf Réaliste (Asset 'realistic_deer_3d_model_2.0_demo_free_download')
            GameObject deerPrefab = SetupRealisticDeerPrefab(meatPrefab);

            // 4. Peuplement sur l'Île
            Terrain terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
            if (terrain != null)
            {
                SpawnTreesOnIsland(terrain, treePrefab);
                SpawnRealisticDeersOnIsland(terrain, deerPrefab);
            }

            // 5. Sauvegarde de la scène
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(group);

            Debug.Log("<color=#4CAF50><b>[Assets Manager]</b> SUCCÈS TOTAL ! Cerf réaliste, arbres 59-Tree01 et viande 3D intégrés et sauvegardés dans la scène !</color>");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = new string[]
            {
                "Assets/_Project/Prefabs/Items",
                "Assets/_Project/Prefabs/Environment",
                "Assets/_Project/Prefabs/Animals",
                "Assets/_Project/Prefabs/Animals/Herbivores",
                "Assets/Resources/Items",
                "Assets/Resources/Animals",
                "Assets/Resources/Environment",
                "Assets/RealisticDeer/Materials",
                "Assets/59-tree01/Materials",
                "Assets/meat/Materials"
            };

            foreach (var d in dirs)
            {
                if (!Directory.Exists(d))
                {
                    Directory.CreateDirectory(d);
                }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        #region 1. Viande Réaliste (Dossier meat)
        public static GameObject SetupRealisticMeatPrefab()
        {
            string matPath = "Assets/meat/Materials/Mat_Meat_Realistic.mat";
            Material meatMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");

            if (meatMat == null)
            {
                meatMat = new Material(urpLit);
                meatMat.name = "Mat_Meat_Realistic";

                Texture2D meatTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/meat/textures/color.jpeg");
                if (meatTex == null) meatTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/meat/source/meat/color.jpg");

                if (meatTex != null)
                {
                    meatMat.SetTexture("_BaseMap", meatTex);
                    meatMat.mainTexture = meatTex;
                }
                meatMat.SetColor("_BaseColor", Color.white);
                meatMat.SetFloat("_Smoothness", 0.35f);

                AssetDatabase.CreateAsset(meatMat, matPath);
            }

            // Recherche du modèle OBJ de viande dans Assets/meat
            GameObject meatModelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/meat/meat_piece.obj");
            if (meatModelAsset == null)
            {
                meatModelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/meat/source/meat/쪠.obj");
            }
            if (meatModelAsset == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/meat" });
                if (guids != null && guids.Length > 0)
                {
                    meatModelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            GameObject meatRoot = new GameObject("Meat_Loot");
            meatRoot.tag = "Untagged";

            if (meatModelAsset != null)
            {
                GameObject visual = UnityEngine.Object.Instantiate(meatModelAsset, meatRoot.transform);
                visual.name = "VisualModel";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(-90f, 0, 0); // Ajuste l'orientation au sol
                visual.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            }
            else
            {
                // Fallback visuel avec le matériau réaliste
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                fallback.name = "VisualModel";
                fallback.transform.SetParent(meatRoot.transform);
                fallback.transform.localPosition = Vector3.zero;
                fallback.transform.localScale = new Vector3(0.35f, 0.12f, 0.35f);
                fallback.GetComponent<Renderer>().sharedMaterial = meatMat;
                UnityEngine.Object.DestroyImmediate(fallback.GetComponent<Collider>());
            }

            // Collider trigger pour ramasser / manger
            CapsuleCollider col = meatRoot.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0, 0.12f, 0);
            col.radius = 0.25f;
            col.height = 0.5f;
            col.direction = 1; // Axe Y

            // Composant MeatItem (survie & faim)
            MeatItem item = meatRoot.AddComponent<MeatItem>();

            // Enregistrement dans les dossiers de Prefabs du jeu
            string targetPath = "Assets/_Project/Prefabs/Items/Meat_Loot.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(meatRoot, targetPath);
            PrefabUtility.SaveAsPrefabAsset(meatRoot, "Assets/Resources/Items/Meat_Loot.prefab");
            PrefabUtility.SaveAsPrefabAsset(meatRoot, "Assets/Resources/Meat_Loot.prefab");
            PrefabUtility.SaveAsPrefabAsset(meatRoot, "Assets/meat/Meat_Loot.prefab");

            UnityEngine.Object.DestroyImmediate(meatRoot);
            Debug.Log("<color=#4CAF50><b>[Viande Réaliste]</b> Préfabriqué de viande 3D configuré avec succès !</color>");
            return savedPrefab;
        }
        #endregion

        #region 2. Arbre 59-Tree01 (Dossier 59-tree01)
        public static GameObject SetupTree01Prefab()
        {
            string fbxPath = "Assets/59-tree01/Tree01/3dmodel/tree01.fbx";
            GameObject treeModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (treeModel == null)
            {
                string[] guids = AssetDatabase.FindAssets("tree01 t:Model", new[] { "Assets/59-tree01" });
                if (guids != null && guids.Length > 0)
                {
                    treeModel = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            if (treeModel == null)
            {
                Debug.LogWarning("[Assets Manager] Modèle tree01.fbx introuvable dans Assets/59-tree01.");
                return null;
            }

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");

            // Matériau pour les Feuilles (Material.001)
            string leavesMatPath = "Assets/59-tree01/Materials/Mat_Tree01_Leaves.mat";
            Material leavesMat = AssetDatabase.LoadAssetAtPath<Material>(leavesMatPath);
            if (leavesMat == null)
            {
                leavesMat = new Material(urpLit) { name = "Mat_Tree01_Leaves" };
                Color leafColor = new Color(0.18f, 0.42f, 0.16f); // Vert feuillage naturel dense
                leavesMat.SetColor("_BaseColor", leafColor);
                leavesMat.color = leafColor;
                leavesMat.SetFloat("_Smoothness", 0.15f);
                AssetDatabase.CreateAsset(leavesMat, leavesMatPath);
            }

            // Matériau pour le Tronc (Material.002)
            string barkMatPath = "Assets/59-tree01/Materials/Mat_Tree01_Bark.mat";
            Material barkMat = AssetDatabase.LoadAssetAtPath<Material>(barkMatPath);
            if (barkMat == null)
            {
                barkMat = new Material(urpLit) { name = "Mat_Tree01_Bark" };
                Color barkColor = new Color(0.32f, 0.22f, 0.15f); // Écorce de bois sombre
                barkMat.SetColor("_BaseColor", barkColor);
                barkMat.color = barkColor;
                barkMat.SetFloat("_Smoothness", 0.1f);
                AssetDatabase.CreateAsset(barkMat, barkMatPath);
            }

            // Instanciation et configuration du Prefab d'Arbre
            GameObject treeRoot = new GameObject("Tree01");
            GameObject treeInst = UnityEngine.Object.Instantiate(treeModel, treeRoot.transform);
            treeInst.name = "VisualModel";
            treeInst.transform.localPosition = Vector3.zero;
            treeInst.transform.localRotation = Quaternion.identity;
            treeInst.transform.localScale = Vector3.one;

            // Suppression des composants et GameObjects parasites exportés par défaut depuis Blender (Camera et Light)
            Camera[] strayCams = treeInst.GetComponentsInChildren<Camera>(true);
            foreach (var cam in strayCams)
            {
                if (cam != null) UnityEngine.Object.DestroyImmediate(cam.gameObject);
            }
            Light[] strayLights = treeInst.GetComponentsInChildren<Light>(true);
            foreach (var l in strayLights)
            {
                if (l != null) UnityEngine.Object.DestroyImmediate(l.gameObject);
            }

            // Application des matériaux aux sous-renderers et optimisation des ombres
            Renderer[] renderers = treeInst.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                Material[] sharedMats = r.sharedMaterials;
                for (int i = 0; i < sharedMats.Length; i++)
                {
                    if (sharedMats[i] != null && sharedMats[i].name.Contains("001"))
                    {
                        sharedMats[i] = leavesMat;
                    }
                    else if (sharedMats[i] != null && sharedMats[i].name.Contains("002"))
                    {
                        sharedMats[i] = barkMat;
                    }
                    else
                    {
                        // Fallback par défaut selon l'index
                        sharedMats[i] = (i == 0) ? leavesMat : barkMat;
                    }
                }
                r.sharedMaterials = sharedMats;

                // Optimisation ombres : le tronc projette l'ombre au sol, le feuillage dense reçoit la lumière sans surcharger les shadowmaps
                if (r.gameObject.name.ToLower().Contains("leaves"))
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                else
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
            }

            // Collider physique sur le tronc pour bloquer le joueur et les animaux de manière réaliste
            CapsuleCollider trunkCollider = treeRoot.AddComponent<CapsuleCollider>();
            trunkCollider.center = new Vector3(0, 3.5f, 0);
            trunkCollider.radius = 0.55f;
            trunkCollider.height = 7.0f;
            trunkCollider.direction = 1; // Axe Y

            string targetPath = "Assets/_Project/Prefabs/Environment/Tree01.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(treeRoot, targetPath);
            PrefabUtility.SaveAsPrefabAsset(treeRoot, "Assets/59-tree01/Tree01_Prefab.prefab");
            PrefabUtility.SaveAsPrefabAsset(treeRoot, "Assets/Resources/Environment/Tree01.prefab");

            UnityEngine.Object.DestroyImmediate(treeRoot);
            Debug.Log("<color=#4CAF50><b>[Arbre 59-Tree01]</b> Préfabriqué d'arbre configuré et optimisé (sans caméra ni lumière parasite) !</color>");
            return savedPrefab;
        }

        private static void SpawnTreesOnIsland(Terrain terrain, GameObject treePrefab)
        {
            if (treePrefab == null || terrain == null) return;

            GameObject treeFolder = GameObject.Find("--- 59-TREE01 VEGETATION ---");
            if (treeFolder == null)
            {
                treeFolder = new GameObject("--- 59-TREE01 VEGETATION ---");
                Undo.RegisterCreatedObjectUndo(treeFolder, "Create 59-Tree01 Folder");
            }
            else
            {
                // Nettoyage des anciens arbres potentiellement porteurs de lumières/caméras parasites
                for (int i = treeFolder.transform.childCount - 1; i >= 0; i--)
                {
                    Undo.DestroyObjectImmediate(treeFolder.transform.GetChild(i).gameObject);
                }
            }

            // Coordonnées stratégiques de plantation à travers l'île
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

            int spawned = 0;
            for (int i = 0; i < spawnCoords.Length; i++)
            {
                Vector3 pt = spawnCoords[i];
                float y = terrain.SampleHeight(pt);
                Vector3 pos = new Vector3(pt.x, y, pt.z);

                // Instanciation avec légères variations naturelles de taille et d'orientation
                GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, treeFolder.transform);
                tree.transform.position = pos;
                tree.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0);
                float scale = UnityEngine.Random.Range(0.85f, 1.25f);
                tree.transform.localScale = new Vector3(scale, scale, scale);
                tree.name = $"Tree01_{i + 1}";

                Undo.RegisterCreatedObjectUndo(tree, "Spawn " + tree.name);
                spawned++;
            }

            Debug.Log($"<color=#4CAF50><b>[Arbre 59-Tree01]</b> {spawned} arbres Tree01 plantés avec succès sur l'île !</color>");
        }
        #endregion

        #region 3. Cerf Réaliste (realistic_deer_3d_model_2.0_demo_free_download)
        public static GameObject SetupRealisticDeerPrefab(GameObject meatPrefab)
        {
            string deerObjPath = "Assets/RealisticDeer/realistic_deer.obj";
            GameObject deerModel = AssetDatabase.LoadAssetAtPath<GameObject>(deerObjPath);
            if (deerModel == null)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                deerModel = AssetDatabase.LoadAssetAtPath<GameObject>(deerObjPath);
            }

            if (deerModel == null)
            {
                Debug.LogWarning("[Assets Manager] Modèle realistic_deer.obj introuvable dans Assets/RealisticDeer.");
                return null;
            }

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");

            // Matériau pour le Corps du Cerf (Deer_M)
            string bodyMatPath = "Assets/RealisticDeer/Materials/Mat_Deer_Body.mat";
            Material bodyMat = AssetDatabase.LoadAssetAtPath<Material>(bodyMatPath);
            if (bodyMat == null)
            {
                bodyMat = new Material(urpLit) { name = "Mat_Deer_Body" };
                Texture2D bodyTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RealisticDeer/Textures/Deer_Body_BaseColor.png");
                if (bodyTex == null) bodyTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RealisticDeer/Textures/deer_texture_2.png");
                if (bodyTex != null)
                {
                    bodyMat.SetTexture("_BaseMap", bodyTex);
                    bodyMat.mainTexture = bodyTex;
                }
                bodyMat.SetColor("_BaseColor", Color.white);
                bodyMat.SetFloat("_Smoothness", 0.15f);
                AssetDatabase.CreateAsset(bodyMat, bodyMatPath);
            }

            // Matériau pour les Bois du Cerf (Deer_Antler)
            string antlerMatPath = "Assets/RealisticDeer/Materials/Mat_Deer_Antler.mat";
            Material antlerMat = AssetDatabase.LoadAssetAtPath<Material>(antlerMatPath);
            if (antlerMat == null)
            {
                antlerMat = new Material(urpLit) { name = "Mat_Deer_Antler" };
                Texture2D antlerTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RealisticDeer/Textures/Deer_Antler_BaseColor.png");
                if (antlerTex == null) antlerTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RealisticDeer/Textures/deer_texture_0.png");
                if (antlerTex != null)
                {
                    antlerMat.SetTexture("_BaseMap", antlerTex);
                    antlerMat.mainTexture = antlerTex;
                }

                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RealisticDeer/Textures/Deer_Antler_Normal.png");
                if (normalTex == null) normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RealisticDeer/Textures/deer_texture_1.png");
                if (normalTex != null)
                {
                    antlerMat.SetTexture("_BumpMap", normalTex);
                    antlerMat.EnableKeyword("_NORMALMAP");
                }

                antlerMat.SetColor("_BaseColor", Color.white);
                antlerMat.SetFloat("_Smoothness", 0.25f);
                AssetDatabase.CreateAsset(antlerMat, antlerMatPath);
            }

            // Racine du Cerf
            GameObject deerRoot = new GameObject("Cerf Majestueux");

            GameObject deerVisual = UnityEngine.Object.Instantiate(deerModel, deerRoot.transform);
            deerVisual.name = "VisualModel";
            deerVisual.transform.localPosition = Vector3.zero;
            deerVisual.transform.localRotation = Quaternion.identity;
            deerVisual.transform.localScale = Vector3.one;

            // Application des textures et matériaux
            Renderer[] renderers = deerVisual.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                string rName = r.gameObject.name.ToLower();
                if (rName.Contains("antler"))
                {
                    r.sharedMaterial = antlerMat;
                }
                else
                {
                    r.sharedMaterial = bodyMat;
                }
            }

            // 1. Collider physique
            CapsuleCollider col = deerRoot.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.85f, 0.38f);
            col.radius = 0.5f;
            col.height = 1.8f;
            col.direction = 2; // Axe Z

            // 2. NavMeshAgent
            NavMeshAgent agent = deerRoot.AddComponent<NavMeshAgent>();
            agent.speed = 2.4f;
            agent.angularSpeed = 240f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 1.0f;
            agent.radius = 0.55f;
            agent.height = 1.8f;

            // 3. AnimalAI (Comportement de cerf craintif gibier)
            AnimalAI ai = deerRoot.AddComponent<AnimalAI>();
            ai.ConfigureSpecies("Cerf Majestueux", false, 50f, 2.4f, 7.5f, 0f, 2.0f, 3);

            var meatField = typeof(AnimalAI).GetField("meatPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            meatField?.SetValue(ai, meatPrefab);

            // Enregistrement des préfabriqués
            string dstPath = "Assets/_Project/Prefabs/Animals/Realistic_Deer.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(deerRoot, dstPath);
            PrefabUtility.SaveAsPrefabAsset(deerRoot, "Assets/_Project/Prefabs/Animals/Herbivores/Deer.prefab");
            PrefabUtility.SaveAsPrefabAsset(deerRoot, "Assets/_Project/Prefabs/Animals/Deer.prefab");
            PrefabUtility.SaveAsPrefabAsset(deerRoot, "Assets/Resources/Animals/Deer.prefab");
            PrefabUtility.SaveAsPrefabAsset(deerRoot, "Assets/Resources/Animals/Realistic_Deer.prefab");
            PrefabUtility.SaveAsPrefabAsset(deerRoot, "Assets/Animals/Deer.prefab");

            UnityEngine.Object.DestroyImmediate(deerRoot);
            Debug.Log("<color=#4CAF50><b>[Cerf Réaliste]</b> Préfabriqué du cerf configuré avec succès !</color>");
            return savedPrefab;
        }

        private static void SpawnRealisticDeersOnIsland(Terrain terrain, GameObject deerPrefab)
        {
            if (deerPrefab == null || terrain == null) return;

            GameObject faunaRoot = GameObject.Find("--- WILDLIFE & ANIMALS ---");
            if (faunaRoot == null)
            {
                faunaRoot = new GameObject("--- WILDLIFE & ANIMALS ---");
                Undo.RegisterCreatedObjectUndo(faunaRoot, "Create Fauna Root");
            }

            Vector3[] deerSpawns = new Vector3[]
            {
                new Vector3(30f, 0, -120f),
                new Vector3(-25f, 0, -90f),
                new Vector3(60f, 0, -35f),
                new Vector3(-40f, 0, 15f),
                new Vector3(25f, 0, 40f)
            };

            int count = 0;
            for (int i = 0; i < deerSpawns.Length; i++)
            {
                Vector3 pt = deerSpawns[i];
                float y = terrain.SampleHeight(pt);
                Vector3 spawnPos = new Vector3(pt.x, y + 0.15f, pt.z);

                GameObject deer = (GameObject)PrefabUtility.InstantiatePrefab(deerPrefab, faunaRoot.transform);
                deer.transform.position = spawnPos;
                deer.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0);
                deer.name = $"Realistic_Deer_{i + 1}";

                NavMeshAgent agent = deer.GetComponent<NavMeshAgent>();
                if (agent != null && NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 15f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }

                Undo.RegisterCreatedObjectUndo(deer, "Spawn " + deer.name);
                count++;
            }

            Debug.Log($"<color=#4CAF50><b>[Cerf Réaliste]</b> {count} Cerfs réalistes placés avec succès sur l'île !</color>");
        }
        #endregion
    }
}
#endif
