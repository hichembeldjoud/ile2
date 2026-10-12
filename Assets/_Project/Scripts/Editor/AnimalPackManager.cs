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
    /// Gestionnaire technique pour extraire, configurer et enregistrer tous les animaux
    /// du pack dans Assets/_Project/Prefabs/Animals sous forme de Prefabs prêts à l'emploi.
    /// </summary>
    public static class AnimalPackManager
    {
        private const string PrefabsRoot = "Assets/_Project/Prefabs/Animals";
        private const string PredatorsDir = "Assets/_Project/Prefabs/Animals/Predateurs";
        private const string HerbivoresDir = "Assets/_Project/Prefabs/Animals/Herbivores";
        private const string BirdsDir = "Assets/_Project/Prefabs/Animals/Oiseaux";
        private const string MaterialsDir = "Assets/_Project/Art/Materials/Animals";

        public struct AnimalDef
        {
            public string id;
            public string displayName;
            public string[] meshKeywords;
            public string category; // Predateurs, Herbivores, Oiseaux
            public bool isPredator;
            public float health;
            public float wanderSpeed;
            public float fleeSpeed;
            public float attackDamage;
            public float attackRange;
            public int meatDropCount;
            public float colliderRadius;
            public float colliderHeight;
            public string mainTexture;
            public string secondaryTexture;
        }

        public static readonly List<AnimalDef> AllSpecies = new List<AnimalDef>()
        {
            // === PRÉDATEURS ===
            new AnimalDef {
                id = "Bear",
                displayName = "Ours Brun Sauvage",
                meshKeywords = new [] { "Bear" },
                category = "Predateurs",
                isPredator = true,
                health = 160f,
                wanderSpeed = 2.0f,
                fleeSpeed = 5.8f,
                attackDamage = 32f,
                attackRange = 2.6f,
                meatDropCount = 4,
                colliderRadius = 0.85f,
                colliderHeight = 1.8f,
                mainTexture = "Image_3.png",
                secondaryTexture = null
            },
            new AnimalDef {
                id = "Lioness",
                displayName = "Lionne Royale",
                meshKeywords = new [] { "Lioness" },
                category = "Predateurs",
                isPredator = true,
                health = 130f,
                wanderSpeed = 2.5f,
                fleeSpeed = 7.0f,
                attackDamage = 25f,
                attackRange = 2.4f,
                meatDropCount = 3,
                colliderRadius = 0.6f,
                colliderHeight = 1.3f,
                mainTexture = "Image_23.png",
                secondaryTexture = null
            },
            new AnimalDef {
                id = "Hyena",
                displayName = "Hyène Tachetée",
                meshKeywords = new [] { "Hyena" },
                category = "Predateurs",
                isPredator = true,
                health = 75f,
                wanderSpeed = 2.4f,
                fleeSpeed = 6.6f,
                attackDamage = 16f,
                attackRange = 2.0f,
                meatDropCount = 2,
                colliderRadius = 0.5f,
                colliderHeight = 1.1f,
                mainTexture = "Image_22.png",
                secondaryTexture = null
            },
            new AnimalDef {
                id = "Alligator",
                displayName = "Alligator du Lagon",
                meshKeywords = new [] { "Alligator" },
                category = "Predateurs",
                isPredator = true,
                health = 140f,
                wanderSpeed = 1.8f,
                fleeSpeed = 5.2f,
                attackDamage = 28f,
                attackRange = 2.2f,
                meatDropCount = 3,
                colliderRadius = 0.65f,
                colliderHeight = 0.8f,
                mainTexture = "Image_0.png",
                secondaryTexture = null
            },
            new AnimalDef {
                id = "HoneyBadger",
                displayName = "Blaireau à Miel",
                meshKeywords = new [] { "Honey_Badger" },
                category = "Predateurs",
                isPredator = true,
                health = 60f,
                wanderSpeed = 2.2f,
                fleeSpeed = 5.8f,
                attackDamage = 14f,
                attackRange = 1.8f,
                meatDropCount = 2,
                colliderRadius = 0.35f,
                colliderHeight = 0.6f,
                mainTexture = "Image_2.png",
                secondaryTexture = null
            },
            new AnimalDef {
                id = "Wolf",
                displayName = "Loup Sauvage",
                meshKeywords = new [] { "Wolf" },
                category = "Predateurs",
                isPredator = true,
                health = 80f,
                wanderSpeed = 2.4f,
                fleeSpeed = 6.4f,
                attackDamage = 18f,
                attackRange = 2.2f,
                meatDropCount = 2,
                colliderRadius = 0.5f,
                colliderHeight = 1.2f,
                mainTexture = "Image_12.png",
                secondaryTexture = null
            },

            // === HERBIVORES & GIBIER ===
            new AnimalDef {
                id = "Boar",
                displayName = "Sanglier Sauvage",
                meshKeywords = new [] { "Boar" },
                category = "Herbivores",
                isPredator = false,
                health = 60f,
                wanderSpeed = 2.0f,
                fleeSpeed = 6.2f,
                attackDamage = 0f,
                attackRange = 2.0f,
                meatDropCount = 2,
                colliderRadius = 0.5f,
                colliderHeight = 1.1f,
                mainTexture = "Image_30.png",
                secondaryTexture = "Image_6.png"
            },
            new AnimalDef {
                id = "Deer",
                displayName = "Cerf Majestueux",
                meshKeywords = new [] { "Deer", "DeerAntler" },
                category = "Herbivores",
                isPredator = false,
                health = 50f,
                wanderSpeed = 2.4f,
                fleeSpeed = 7.4f,
                attackDamage = 0f,
                attackRange = 2.0f,
                meatDropCount = 3,
                colliderRadius = 0.55f,
                colliderHeight = 1.6f,
                mainTexture = "Image_24.png",
                secondaryTexture = "Image_25.png"
            },
            new AnimalDef {
                id = "Moose",
                displayName = "Grand Élan",
                meshKeywords = new [] { "Moose" },
                category = "Herbivores",
                isPredator = false,
                health = 140f,
                wanderSpeed = 2.2f,
                fleeSpeed = 6.2f,
                attackDamage = 0f,
                attackRange = 2.2f,
                meatDropCount = 4,
                colliderRadius = 0.8f,
                colliderHeight = 2.0f,
                mainTexture = "Image_24.png",
                secondaryTexture = "Image_25.png"
            },
            new AnimalDef {
                id = "Buffalo",
                displayName = "Buffle Sauvage",
                meshKeywords = new [] { "Buffalo" },
                category = "Herbivores",
                isPredator = false,
                health = 150f,
                wanderSpeed = 2.0f,
                fleeSpeed = 5.6f,
                attackDamage = 0f,
                attackRange = 2.2f,
                meatDropCount = 4,
                colliderRadius = 0.85f,
                colliderHeight = 1.8f,
                mainTexture = "Image_9.png",
                secondaryTexture = "Image_8.png"
            },
            new AnimalDef {
                id = "Fox",
                displayName = "Renard Sauvage",
                meshKeywords = new [] { "Fox" },
                category = "Herbivores",
                isPredator = false,
                health = 35f,
                wanderSpeed = 2.4f,
                fleeSpeed = 6.8f,
                attackDamage = 0f,
                attackRange = 1.8f,
                meatDropCount = 1,
                colliderRadius = 0.35f,
                colliderHeight = 0.7f,
                mainTexture = "Image_12.png",
                secondaryTexture = null
            },
            new AnimalDef {
                id = "Rabbit",
                displayName = "Lapin Agile",
                meshKeywords = new [] { "Rabbit" },
                category = "Herbivores",
                isPredator = false,
                health = 20f,
                wanderSpeed = 2.2f,
                fleeSpeed = 7.2f,
                attackDamage = 0f,
                attackRange = 1.5f,
                meatDropCount = 1,
                colliderRadius = 0.25f,
                colliderHeight = 0.5f,
                mainTexture = "Image_29.png",
                secondaryTexture = null
            },
            new AnimalDef {
                id = "Wildebeest",
                displayName = "Gnou des Savanes",
                meshKeywords = new [] { "Wildebeest" },
                category = "Herbivores",
                isPredator = false,
                health = 85f,
                wanderSpeed = 2.3f,
                fleeSpeed = 6.6f,
                attackDamage = 0f,
                attackRange = 2.0f,
                meatDropCount = 3,
                colliderRadius = 0.65f,
                colliderHeight = 1.5f,
                mainTexture = "Image_14.png",
                secondaryTexture = null
            },
            new AnimalDef {
                id = "MountainGoat",
                displayName = "Chèvre des Falaises",
                meshKeywords = new [] { "MountainGoat" },
                category = "Herbivores",
                isPredator = false,
                health = 50f,
                wanderSpeed = 2.2f,
                fleeSpeed = 6.4f,
                attackDamage = 0f,
                attackRange = 1.8f,
                meatDropCount = 2,
                colliderRadius = 0.45f,
                colliderHeight = 1.1f,
                mainTexture = "Image_27.png",
                secondaryTexture = "Image_26.png"
            },
            new AnimalDef {
                id = "BighornSheep",
                displayName = "Mouflon Canadien",
                meshKeywords = new [] { "BighornSheep", "Bighorn" },
                category = "Herbivores",
                isPredator = false,
                health = 65f,
                wanderSpeed = 2.2f,
                fleeSpeed = 6.2f,
                attackDamage = 0f,
                attackRange = 2.0f,
                meatDropCount = 2,
                colliderRadius = 0.5f,
                colliderHeight = 1.2f,
                mainTexture = "Image_5.png",
                secondaryTexture = "Image_4.png"
            },
            new AnimalDef {
                id = "Warthog",
                displayName = "Phacochère d'Afrique",
                meshKeywords = new [] { "Warthog" },
                category = "Herbivores",
                isPredator = false,
                health = 65f,
                wanderSpeed = 2.1f,
                fleeSpeed = 6.0f,
                attackDamage = 0f,
                attackRange = 2.0f,
                meatDropCount = 2,
                colliderRadius = 0.5f,
                colliderHeight = 1.0f,
                mainTexture = "Image_30.png",
                secondaryTexture = "Image_6.png"
            },
            new AnimalDef {
                id = "Horse",
                displayName = "Cheval Sauvage",
                meshKeywords = new [] { "Body", "Horse" },
                category = "Herbivores",
                isPredator = false,
                health = 90f,
                wanderSpeed = 2.6f,
                fleeSpeed = 7.2f,
                attackDamage = 0f,
                attackRange = 2.2f,
                meatDropCount = 3,
                colliderRadius = 0.7f,
                colliderHeight = 1.8f,
                mainTexture = "Image_21.png",
                secondaryTexture = null
            },

            // === OISEAUX / PETIT GIBIER ===
            new AnimalDef {
                id = "Goose",
                displayName = "Oie Sauvage",
                meshKeywords = new [] { "Goose" },
                category = "Oiseaux",
                isPredator = false,
                health = 18f,
                wanderSpeed = 1.8f,
                fleeSpeed = 5.5f,
                attackDamage = 0f,
                attackRange = 1.2f,
                meatDropCount = 1,
                colliderRadius = 0.3f,
                colliderHeight = 0.6f,
                mainTexture = "Image_18.png",
                secondaryTexture = null
            },
            new AnimalDef {
                id = "Guineafowl",
                displayName = "Pintade de Jungle",
                meshKeywords = new [] { "Guineafowl" },
                category = "Oiseaux",
                isPredator = false,
                health = 15f,
                wanderSpeed = 1.8f,
                fleeSpeed = 5.6f,
                attackDamage = 0f,
                attackRange = 1.2f,
                meatDropCount = 1,
                colliderRadius = 0.25f,
                colliderHeight = 0.5f,
                mainTexture = "Image_19.png",
                secondaryTexture = null
            },
            new AnimalDef {
                id = "Pheasant",
                displayName = "Faisan Sauvage",
                meshKeywords = new [] { "Pheasant" },
                category = "Oiseaux",
                isPredator = false,
                health = 15f,
                wanderSpeed = 1.8f,
                fleeSpeed = 5.5f,
                attackDamage = 0f,
                attackRange = 1.2f,
                meatDropCount = 1,
                colliderRadius = 0.25f,
                colliderHeight = 0.5f,
                mainTexture = "Image_28.png",
                secondaryTexture = null
            }
        };

        [MenuItem("Survival Island/🦁 1. Extraire & Créer TOUS les Animaux dans Assets", false, 10)]
        public static void SetupAllAnimals()
        {
            Undo.SetCurrentGroupName("Setup All Animals Prefabs");
            int group = Undo.GetCurrentGroup();

            Debug.Log("<color=#4CAF50><b>[Animal Pack Manager]</b> Début de l'extraction et de la création de tous les animaux dans Assets...</color>");

            // 1. Assurer les dossiers de destination
            EnsureDirectories();

            // 2. Récupérer ou créer le Prefab de Viande (Loot)
            GameObject meatPrefab = CreateOrGetMeatPrefab();

            // 3. Charger le contrôleur d'animation des animaux
            RuntimeAnimatorController animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/_Project/Art/Animations/Animals/AnimalAnimatorController.controller");
            if (animCtrl == null)
            {
                animCtrl = AnimalAnimatorControllerBuilder.GenerateAnimalAnimatorController();
            }

            // 4. Localiser le modèle FBX du pack d'animaux
            string fbxPath = FindAnimalPackFBX();
            GameObject fbxAsset = null;
            if (!string.IsNullOrEmpty(fbxPath))
            {
                fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            }

            // Si le FBX n'a pas encore été importé par Unity, forcer le refresh
            if (fbxAsset == null && !string.IsNullOrEmpty(fbxPath))
            {
                AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
                fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            }

            // Mettre à jour les matériaux FBX pour URP Lit
            FixMaterialsToURPLit(fbxPath);

            int createdCount = 0;

            // 5. Générer un Prefab pour chaque espèce
            if (fbxAsset != null)
            {
                GameObject rootInstance = UnityEngine.Object.Instantiate(fbxAsset);
                rootInstance.name = "TEMP_FBX_EXTRACTOR";
                rootInstance.hideFlags = HideFlags.HideAndDontSave;

                try
                {
                    foreach (var def in AllSpecies)
                    {
                        try
                        {
                            bool success = CreateAnimalPrefabFromFBX(rootInstance, def, animCtrl, meatPrefab);
                            if (success) createdCount++;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[Animal Pack Manager] Erreur lors de l'extraction de {def.displayName}: {ex.Message}");
                        }
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(rootInstance);
                }
            }
            else
            {
                Debug.LogWarning("[Animal Pack Manager] FBX non disponible immédiatement. Utilisation des modèles existants (Loup URP & Sanglier).");
            }

            // 6. Intégrer également le loup URP dédié s'il existe
            SetupDedicatedWolfPrefab(animCtrl, meatPrefab);

            // 7. Intégrer les sangliers dédiés de haute qualité du dossier 'Wild Boar'
            SetupDedicatedWildBoarPrefab(meatPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Undo.CollapseUndoOperations(group);
            Debug.Log($"<color=#4CAF50><b>[Animal Pack Manager]</b> SUCCÈS ! {createdCount} espèces d'animaux créées dans <b>Assets/_Project/Prefabs/Animals</b> !</color>");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = new string[]
            {
                "Assets/_Project",
                "Assets/_Project/Prefabs",
                "Assets/_Project/Prefabs/Animals",
                "Assets/_Project/Prefabs/Items",
                PredatorsDir,
                HerbivoresDir,
                BirdsDir,
                "Assets/_Project/Art",
                "Assets/_Project/Art/Materials",
                MaterialsDir,
                "Assets/Animals",
                "Assets/Resources",
                "Assets/Resources/Animals",
                "Assets/Resources/Items"
            };

            foreach (var d in dirs)
            {
                if (!AssetDatabase.IsValidFolder(d))
                {
                    string parent = Path.GetDirectoryName(d).Replace("\\", "/");
                    string folderName = Path.GetFileName(d);
                    if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
                    {
                        if (!AssetDatabase.IsValidFolder(parent))
                        {
                            string grandParent = Path.GetDirectoryName(parent).Replace("\\", "/");
                            string parentFolder = Path.GetFileName(parent);
                            if (!string.IsNullOrEmpty(grandParent) && !AssetDatabase.IsValidFolder(parent))
                            {
                                AssetDatabase.CreateFolder(grandParent, parentFolder);
                            }
                        }
                        AssetDatabase.CreateFolder(parent, folderName);
                    }
                }
            }
        }

        private static string FindAnimalPackFBX()
        {
            // Chemins potentiels
            string directPath = "Assets/ultimate-animal-pack-100-animals-50-off/source/Realistic Animated Pack/Realistic Animated Pack/Realistic Animated Pack.fbx";
            if (File.Exists(directPath)) return directPath;

            // Recherche automatique
            string[] guids = AssetDatabase.FindAssets("Realistic Animated Pack t:Model");
            if (guids != null && guids.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(guids[0]);
            }

            guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/ultimate-animal-pack-100-animals-50-off" });
            if (guids != null && guids.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(guids[0]);
            }

            return null;
        }

        private static Material GetOrCreateAnimalURPMaterial(string matName, string textureFilename)
        {
            if (string.IsNullOrEmpty(textureFilename)) return null;

            EnsureDirectories();
            string matPath = $"{MaterialsDir}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Universal Render Pipeline/Simple Lit");

            if (mat == null)
            {
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else if (mat.shader != urpLit)
            {
                mat.shader = urpLit;
            }

            string texPath = $"Assets/ultimate-animal-pack-100-animals-50-off/textures/{textureFilename}";
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.mainTexture = tex;
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.15f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void FixMaterialsToURPLit(string fbxPath)
        {
            if (string.IsNullOrEmpty(fbxPath)) return;

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpShader == null) return;

            UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in subAssets)
            {
                if (asset is Material mat)
                {
                    if (mat.shader != urpShader)
                    {
                        Texture mainTex = mat.mainTexture;
                        Color col = mat.color;
                        mat.shader = urpShader;
                        if (mainTex != null)
                        {
                            mat.SetTexture("_BaseMap", mainTex);
                            mat.mainTexture = mainTex;
                        }
                        mat.SetColor("_BaseColor", col);
                        mat.color = col;
                        EditorUtility.SetDirty(mat);
                    }
                }
            }
        }

        private static bool CreateAnimalPrefabFromFBX(GameObject rootInstance, AnimalDef def, RuntimeAnimatorController animCtrl, GameObject meatPrefab)
        {
            // Trouver tous les sous-renderers correspondant aux mots-clés de l'animal
            List<Transform> matchingParts = new List<Transform>();
            Renderer[] allRenderers = rootInstance.GetComponentsInChildren<Renderer>(true);

            foreach (var rend in allRenderers)
            {
                string rName = rend.gameObject.name;
                foreach (var kw in def.meshKeywords)
                {
                    if (rName.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matchingParts.Add(rend.transform);
                        break;
                    }
                }
            }

            if (matchingParts.Count == 0)
            {
                return false;
            }

            // Création du GameObject racine de l'animal
            GameObject animalRoot = new GameObject(def.displayName);
            animalRoot.tag = "Untagged";

            // Création du conteneur visuel
            GameObject visualContainer = new GameObject("VisualModel");
            visualContainer.transform.SetParent(animalRoot.transform);
            visualContainer.transform.localPosition = Vector3.zero;
            visualContainer.transform.localRotation = Quaternion.identity;

            // Instancier et copier chaque partie du mesh
            List<Renderer> newRenderers = new List<Renderer>();
            foreach (var part in matchingParts)
            {
                GameObject partCopy = UnityEngine.Object.Instantiate(part.gameObject, visualContainer.transform);
                partCopy.name = part.name;
                partCopy.transform.localPosition = part.localPosition;
                partCopy.transform.localRotation = part.localRotation;
                partCopy.transform.localScale = part.localScale;

                Renderer r = partCopy.GetComponent<Renderer>();
                if (r != null) newRenderers.Add(r);
            }

            // Calcul précis du centrage horizontal et élévation verticale pour poser les pattes exactement au sol (Y = 0)
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            bool hasValidBounds = false;

            foreach (var part in matchingParts)
            {
                MeshFilter mf = part.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Bounds meshBounds = mf.sharedMesh.bounds;
                    Vector3 bMin = meshBounds.min;
                    Vector3 bMax = meshBounds.max;

                    Vector3[] localCorners = new Vector3[8]
                    {
                        new Vector3(bMin.x, bMin.y, bMin.z),
                        new Vector3(bMin.x, bMin.y, bMax.z),
                        new Vector3(bMin.x, bMax.y, bMin.z),
                        new Vector3(bMin.x, bMax.y, bMax.z),
                        new Vector3(bMax.x, bMin.y, bMin.z),
                        new Vector3(bMax.x, bMin.y, bMax.z),
                        new Vector3(bMax.x, bMax.y, bMin.z),
                        new Vector3(bMax.x, bMax.y, bMax.z)
                    };

                    Matrix4x4 localMtx = Matrix4x4.TRS(part.localPosition, part.localRotation, part.localScale);

                    foreach (var c in localCorners)
                    {
                        Vector3 pt = localMtx.MultiplyPoint3x4(c);
                        if (pt.x < minX) minX = pt.x;
                        if (pt.x > maxX) maxX = pt.x;
                        if (pt.y < minY) minY = pt.y;
                        if (pt.y > maxY) maxY = pt.y;
                        if (pt.z < minZ) minZ = pt.z;
                        if (pt.z > maxZ) maxZ = pt.z;
                        hasValidBounds = true;
                    }
                }
            }

            if (hasValidBounds)
            {
                float centerX = (minX + maxX) * 0.5f;
                float centerZ = (minZ + maxZ) * 0.5f;
                float liftY = -minY;
                visualContainer.transform.localPosition = new Vector3(-centerX, liftY, -centerZ);
                Debug.Log($"<color=#4CAF50><b>[Animal Pack Manager]</b> {def.displayName} calibré : Décalage visuel ({-centerX:F2}, {liftY:F2}, {-centerZ:F2}) (minY={minY:F2})</color>");
            }

            // Assignation des matériaux URP Lit texturés
            Material mainMat = GetOrCreateAnimalURPMaterial(def.id + "_Mat", def.mainTexture);
            Material secondaryMat = !string.IsNullOrEmpty(def.secondaryTexture) ?
                GetOrCreateAnimalURPMaterial(def.id + "_SecondaryMat", def.secondaryTexture) : mainMat;

            foreach (var r in newRenderers)
            {
                if (r == null) continue;
                string partName = r.gameObject.name;
                bool isSecondary = partName.IndexOf("Antler", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  partName.IndexOf("Horn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  partName.IndexOf("Tusk", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isSecondary && secondaryMat != null)
                {
                    r.sharedMaterial = secondaryMat;
                }
                else if (mainMat != null)
                {
                    r.sharedMaterial = mainMat;
                }
            }

            // Supprimer d'éventuels colliders résiduels sur les enfants visuels
            foreach (var col in visualContainer.GetComponentsInChildren<Collider>())
            {
                UnityEngine.Object.DestroyImmediate(col);
            }

            // 1. CapsuleCollider sur la racine
            CapsuleCollider capsule = animalRoot.AddComponent<CapsuleCollider>();
            capsule.direction = 2; // Axe Z (horizontal)
            capsule.radius = def.colliderRadius;
            capsule.height = def.colliderHeight;
            capsule.center = new Vector3(0, def.colliderHeight * 0.45f, 0);

            // 2. NavMeshAgent
            NavMeshAgent agent = animalRoot.AddComponent<NavMeshAgent>();
            agent.speed = def.wanderSpeed;
            agent.angularSpeed = 240f;
            agent.acceleration = 12f;
            agent.stoppingDistance = def.isPredator ? def.attackRange * 0.85f : 0.5f;
            agent.radius = def.colliderRadius;
            agent.height = def.colliderHeight;
            agent.baseOffset = 0f;

            // 3. Animator
            Animator anim = animalRoot.AddComponent<Animator>();
            if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;

            // 4. AnimalAI
            AnimalAI ai = animalRoot.AddComponent<AnimalAI>();
            ai.ConfigureSpecies(
                def.displayName,
                def.isPredator,
                def.health,
                def.wanderSpeed,
                def.fleeSpeed,
                def.attackDamage,
                def.attackRange,
                def.meatDropCount
            );

            // Assignation des références internes par réflexion
            var meatField = typeof(AnimalAI).GetField("meatPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            meatField?.SetValue(ai, meatPrefab);

            var animField = typeof(AnimalAI).GetField("animator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            animField?.SetValue(ai, anim);

            // 5. Sauvegarde en Prefab
            string categoryDir = def.category == "Predateurs" ? PredatorsDir :
                                (def.category == "Oiseaux" ? BirdsDir : HerbivoresDir);

            string prefabPath = $"{categoryDir}/{def.id}.prefab";
            string generalPath = $"{PrefabsRoot}/{def.id}.prefab";
            string shortcutPath = $"Assets/Animals/{def.id}.prefab";
            string resourcesPath = $"Assets/Resources/Animals/{def.id}.prefab";

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(animalRoot, prefabPath);
            if (savedPrefab != null)
            {
                // Sauvegarde également dans les dossiers généraux et Resources pour accessibilité immédiate
                PrefabUtility.SaveAsPrefabAsset(animalRoot, generalPath);
                PrefabUtility.SaveAsPrefabAsset(animalRoot, shortcutPath);
                PrefabUtility.SaveAsPrefabAsset(animalRoot, resourcesPath);
            }

            UnityEngine.Object.DestroyImmediate(animalRoot);
            return true;
        }

        public static void RemoveLeakedTemplateInstances()
        {
            foreach (GameObject go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                if ((go.name == "Loup Sauvage" || go.name == "Sanglier Sauvage") && go.GetComponent<AnimalAI>() == null)
                {
                    Undo.DestroyObjectImmediate(go);
                }
            }
        }

        private static void SetupDedicatedWolfPrefab(RuntimeAnimatorController animCtrl, GameObject meatPrefab)
        {
            string wolfSrc = "Assets/Wolf/URP/Wolf/Prefab/Wolf_URP.prefab";
            GameObject wolfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(wolfSrc);
            if (wolfPrefab == null) return;

            GameObject wolfInst = UnityEngine.Object.Instantiate(wolfPrefab);
            wolfInst.name = "Loup Sauvage";
            try
            {
                CapsuleCollider col = wolfInst.GetComponent<CapsuleCollider>();
                if (col == null)
                {
                    col = wolfInst.AddComponent<CapsuleCollider>();
                }
                col.center = new Vector3(0, 0.6f, 0);
                col.radius = 0.5f;
                col.height = 1.2f;
                col.direction = 2;

                NavMeshAgent agent = wolfInst.GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    agent = wolfInst.AddComponent<NavMeshAgent>();
                }
                agent.speed = 2.4f;
                agent.angularSpeed = 240f;
                agent.acceleration = 14f;
                agent.stoppingDistance = 1.8f;
                agent.radius = 0.5f;
                agent.height = 1.2f;

                AnimalAI ai = wolfInst.GetComponent<AnimalAI>();
                if (ai == null)
                {
                    ai = wolfInst.AddComponent<AnimalAI>();
                }
                ai.ConfigureAsWolf();

                var meatField = typeof(AnimalAI).GetField("meatPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                meatField?.SetValue(ai, meatPrefab);

                var animField = typeof(AnimalAI).GetField("animator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Animator anim = wolfInst.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    anim.applyRootMotion = false;
                }
                animField?.SetValue(ai, anim);

                string dstPath = $"{PredatorsDir}/Wolf_Dedicated.prefab";
                PrefabUtility.SaveAsPrefabAsset(wolfInst, dstPath);
                PrefabUtility.SaveAsPrefabAsset(wolfInst, $"{PrefabsRoot}/Wolf_Dedicated.prefab");
                PrefabUtility.SaveAsPrefabAsset(wolfInst, $"Assets/Animals/Wolf_Dedicated.prefab");
                PrefabUtility.SaveAsPrefabAsset(wolfInst, $"Assets/Resources/Animals/Wolf_Dedicated.prefab");
                PrefabUtility.SaveAsPrefabAsset(wolfInst, $"Assets/Resources/Animals/Wolf.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wolfInst);
            }
        }

        public static GameObject SetupDedicatedWildBoarPrefab(GameObject meatPrefab)
        {
            string boarSrc = "Assets/Wild Boar/Prefabs/URP/HD/Wild_Boar.prefab";
            GameObject boarSourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(boarSrc);
            if (boarSourcePrefab == null)
            {
                boarSrc = "Assets/Wild Boar/Prefabs/URP/Mobile/Wild_Boar.prefab";
                boarSourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(boarSrc);
            }

            if (boarSourcePrefab == null)
            {
                Debug.LogWarning("[Animal Pack Manager] Aucun modèle de sanglier trouvé dans Assets/Wild Boar/Prefabs/URP");
                return null;
            }

            GameObject boarInst = UnityEngine.Object.Instantiate(boarSourcePrefab);
            boarInst.name = "Sanglier Sauvage";
            GameObject savedPrefab = null;
            try
            {
                // 1. Collider
                CapsuleCollider col = boarInst.GetComponent<CapsuleCollider>();
                if (col == null)
                {
                    col = boarInst.AddComponent<CapsuleCollider>();
                }
                col.center = new Vector3(0, 0.65f, 0);
                col.radius = 0.55f;
                col.height = 1.6f;
                col.direction = 2; // Axe Z

                // 2. NavMeshAgent
                NavMeshAgent agent = boarInst.GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    agent = boarInst.AddComponent<NavMeshAgent>();
                }
                agent.speed = 2.2f;
                agent.angularSpeed = 240f;
                agent.acceleration = 12f;
                agent.stoppingDistance = 0.8f;
                agent.radius = 0.55f;
                agent.height = 1.3f;

                // 3. Animator
                Animator anim = boarInst.GetComponent<Animator>();
                if (anim == null)
                {
                    anim = boarInst.AddComponent<Animator>();
                }
                RuntimeAnimatorController boarCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/Wild Boar/Animals/Wild_Boar/Animations/Wild_Boar.controller");
                if (boarCtrl != null)
                {
                    anim.runtimeAnimatorController = boarCtrl;
                }
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                anim.applyRootMotion = false;

                // 4. AnimalAI
                AnimalAI ai = boarInst.GetComponent<AnimalAI>();
                if (ai == null)
                {
                    ai = boarInst.AddComponent<AnimalAI>();
                }
                ai.ConfigureAsBoar();

                var meatField = typeof(AnimalAI).GetField("meatPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                meatField?.SetValue(ai, meatPrefab);

                var animField = typeof(AnimalAI).GetField("animator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                animField?.SetValue(ai, anim);

                // 5. Sauvegarde en Prefabs
                string dstPath = $"{HerbivoresDir}/Wild_Boar.prefab";
                savedPrefab = PrefabUtility.SaveAsPrefabAsset(boarInst, dstPath);
                PrefabUtility.SaveAsPrefabAsset(boarInst, $"{PrefabsRoot}/Wild_Boar.prefab");
                PrefabUtility.SaveAsPrefabAsset(boarInst, $"Assets/Animals/Wild_Boar.prefab");
                PrefabUtility.SaveAsPrefabAsset(boarInst, $"Assets/Resources/Animals/Wild_Boar.prefab");

                // Remplace également le Boar générique par le modèle dédié Wild Boar
                PrefabUtility.SaveAsPrefabAsset(boarInst, $"{HerbivoresDir}/Boar.prefab");
                PrefabUtility.SaveAsPrefabAsset(boarInst, $"{PrefabsRoot}/Boar.prefab");
                PrefabUtility.SaveAsPrefabAsset(boarInst, $"Assets/Animals/Boar.prefab");
                PrefabUtility.SaveAsPrefabAsset(boarInst, $"Assets/Resources/Animals/Boar.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(boarInst);
            }
            Debug.Log("<color=#4CAF50><b>[Wild Boar]</b> Le modèle du dossier 'Wild Boar' a été configuré avec succès !</color>");
            return savedPrefab;
        }

        [MenuItem("Survival Island/🐗 Ajouter les Sangliers (Wild Boar) au Jeu", false, 3)]
        public static void AddWildBoarsToGameMenuItem()
        {
            Undo.SetCurrentGroupName("Add Wild Boar To Game");
            int group = Undo.GetCurrentGroup();

            EnsureDirectories();
            GameObject meatPrefab = CreateOrGetMeatPrefab();
            GameObject boarPrefab = SetupDedicatedWildBoarPrefab(meatPrefab);

            if (boarPrefab == null)
            {
                Debug.LogError("[Wild Boar] Impossible de créer le prefab du Wild Boar.");
                return;
            }

            Terrain terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
            if (terrain == null)
            {
                Debug.LogWarning("[Wild Boar] Aucun terrain trouvé pour positionner les sangliers.");
                return;
            }

            GameObject faunaRoot = GameObject.Find("--- WILDLIFE & ANIMALS ---");
            if (faunaRoot == null)
            {
                faunaRoot = new GameObject("--- WILDLIFE & ANIMALS ---");
                Undo.RegisterCreatedObjectUndo(faunaRoot, "Create Fauna Root");
            }

            Vector3[] boarSpawns = new Vector3[]
            {
                new Vector3(15f, 0, -145f),
                new Vector3(-45f, 0, -100f),
                new Vector3(35f, 0, -70f),
                new Vector3(20f, 0, -135f),
                new Vector3(-30f, 0, -115f),
                new Vector3(45f, 0, -90f)
            };

            int count = 0;
            for (int i = 0; i < boarSpawns.Length; i++)
            {
                Vector3 pos = boarSpawns[i];
                float y = terrain.SampleHeight(pos);
                Vector3 spawnPos = new Vector3(pos.x, y + 0.15f, pos.z);

                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(boarPrefab, faunaRoot.transform);
                inst.transform.position = spawnPos;
                inst.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                inst.name = $"Wild_Boar_{i + 1}";

                NavMeshAgent agent = inst.GetComponent<NavMeshAgent>();
                if (agent != null && NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 15f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }

                Undo.RegisterCreatedObjectUndo(inst, "Spawn " + inst.name);
                count++;
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log($"<color=#4CAF50><b>[Wild Boar]</b> {count} Sangliers du dossier 'Wild Boar' ajoutés avec succès au jeu et enregistrés !</color>");
        }

        public static GameObject CreateOrGetMeatPrefab()
        {
            return AddRequestedAssetsManager.SetupRealisticMeatPrefab();
        }

        [MenuItem("Survival Island/🐾 2. Peupler l'Île avec Tous les Animaux", false, 11)]
        public static void PopulateIslandWithAnimals()
        {
            Undo.SetCurrentGroupName("Populate Island Wildlife");
            int group = Undo.GetCurrentGroup();

            // S'assurer que les prefabs sont créés
            SetupAllAnimals();

            Terrain terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
            if (terrain == null)
            {
                Debug.LogWarning("[Animal Pack Manager] Aucun Terrain trouvé pour positionner les animaux.");
                return;
            }

            GameObject faunaRoot = GameObject.Find("--- WILDLIFE & ANIMALS ---");
            if (faunaRoot == null)
            {
                faunaRoot = new GameObject("--- WILDLIFE & ANIMALS ---");
                Undo.RegisterCreatedObjectUndo(faunaRoot, "Create Fauna Root");
            }

            RemoveLeakedTemplateInstances();

            // Nettoyer les animaux existants pour un repeuplement complet propre
            for (int i = faunaRoot.transform.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(faunaRoot.transform.GetChild(i).gameObject);
            }

            // Répartition des animaux sur l'île (Zones variées)
            var spawnPlans = new List<(string prefabName, Vector3[] positions)>()
            {
                // Plages et plaines côtières (Proies faciles pour débuter)
                ("Boar", new [] {
                    new Vector3(15f, 0, -145f),
                    new Vector3(-45f, 0, -100f),
                    new Vector3(35f, 0, -70f)
                }),
                ("Rabbit", new [] {
                    new Vector3(25f, 0, -150f),
                    new Vector3(-15f, 0, -130f),
                    new Vector3(50f, 0, -110f),
                    new Vector3(-25f, 0, -85f)
                }),
                // Forêts et clairières centrales
                ("Deer", new [] {
                    new Vector3(60f, 0, -40f),
                    new Vector3(-60f, 0, 10f),
                    new Vector3(20f, 0, 45f)
                }),
                ("Fox", new [] {
                    new Vector3(80f, 0, -20f),
                    new Vector3(-70f, 0, 30f)
                }),
                ("Moose", new [] {
                    new Vector3(45f, 0, -15f),
                    new Vector3(-35f, 0, 15f)
                }),
                ("Buffalo", new [] {
                    new Vector3(65f, 0, 20f),
                    new Vector3(-55f, 0, -5f)
                }),
                ("Horse", new [] {
                    new Vector3(30f, 0, -35f),
                    new Vector3(-10f, 0, -55f)
                }),
                ("Warthog", new [] {
                    new Vector3(10f, 0, -120f),
                    new Vector3(-40f, 0, -65f)
                }),
                // Collines et contreforts : Prédateurs
                ("Wolf", new [] {
                    new Vector3(-55f, 0f, -30f),
                    new Vector3(45f, 0f, 40f),
                    new Vector3(-85f, 0f, -55f)
                }),
                ("Bear", new [] {
                    new Vector3(10f, 0f, 75f),
                    new Vector3(-75f, 0f, 65f)
                }),
                ("Hyena", new [] {
                    new Vector3(-65f, 0f, -20f),
                    new Vector3(55f, 0f, -10f)
                }),
                ("Lioness", new [] {
                    new Vector3(25f, 0f, 60f)
                }),
                // Hauteurs rocheuses : Animaux des falaises
                ("MountainGoat", new [] {
                    new Vector3(0f, 0f, 105f),
                    new Vector3(-35f, 0f, 95f)
                }),
                ("BighornSheep", new [] {
                    new Vector3(30f, 0f, 100f),
                    new Vector3(-20f, 0f, 110f)
                }),
                // Bord de l'eau et lagons : Alligators & Blaireaux
                ("Alligator", new [] {
                    new Vector3(-95f, 0f, -110f),
                    new Vector3(90f, 0f, -80f)
                }),
                ("HoneyBadger", new [] {
                    new Vector3(15f, 0f, -95f),
                    new Vector3(-35f, 0f, -115f)
                })
            };

            int spawnedCount = 0;
            foreach (var plan in spawnPlans)
            {
                // Recherche exhaustive du préfabriqué
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsRoot}/{plan.prefabName}.prefab");
                if (p == null) p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Animals/{plan.prefabName}.prefab");
                if (p == null) p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Resources/Animals/{plan.prefabName}.prefab");
                if (p == null) p = AssetDatabase.LoadAssetAtPath<GameObject>($"{PredatorsDir}/{plan.prefabName}.prefab");
                if (p == null) p = AssetDatabase.LoadAssetAtPath<GameObject>($"{HerbivoresDir}/{plan.prefabName}.prefab");
                if (p == null) p = AssetDatabase.LoadAssetAtPath<GameObject>($"{BirdsDir}/{plan.prefabName}.prefab");

                if (p == null && plan.prefabName == "Wolf")
                {
                    p = AssetDatabase.LoadAssetAtPath<GameObject>($"{PredatorsDir}/Wolf_Dedicated.prefab");
                    if (p == null) p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Animals/Wolf_Dedicated.prefab");
                }

                if (p == null)
                {
                    string[] guids = AssetDatabase.FindAssets($"{plan.prefabName} t:Prefab");
                    if (guids != null && guids.Length > 0)
                    {
                        p = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    }
                }

                if (p == null) continue;

                for (int i = 0; i < plan.positions.Length; i++)
                {
                    Vector3 pos = plan.positions[i];
                    float y = terrain.SampleHeight(pos);
                    Vector3 spawnPos = new Vector3(pos.x, y + 0.15f, pos.z);

                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(p, faunaRoot.transform);
                    inst.transform.position = spawnPos;
                    inst.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                    inst.name = $"{plan.prefabName}_{i + 1}";

                    // Ajuste le composant NavMeshAgent s'il est présent
                    NavMeshAgent agent = inst.GetComponent<NavMeshAgent>();
                    if (agent != null && NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 15f, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                    }

                    Undo.RegisterCreatedObjectUndo(inst, "Spawn " + inst.name);
                    spawnedCount++;
                }
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log($"<color=#4CAF50><b>[Animal Pack Manager]</b> {spawnedCount} animaux de multiples espèces peuplent désormais l'île et sont sauvegardés dans la scène !</color>");
        }
    }
}
#endif
