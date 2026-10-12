#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.AI.Navigation;
using UnityEngine.AI;
using SurvivalIsland.Environment;
using SurvivalIsland.Player;
using SurvivalIsland.AI;

namespace SurvivalIsland.Editor
{
    /// <summary>
    /// Outil d'art technique principal de niveau professionnel :
    /// Transforme instantanément la scène en une véritable île tropicale paradisiaque réaliste :
    /// - Génère de vraies textures PBR 512x512 avec Normal Maps (Sable, Herbe de jungle, Roche volcanique).
    /// - Génère et instancie de vrais modèles 3D OBJ (Palmiers détaillés, Sangliers sculptés avec défenses, Récifs rocheux).
    /// - Océan turquoise des Caraïbes avec reflets et vagues animées.
    /// - Positionne le joueur directement sur la plage de sable doré face au lagon turquoise sous les palmiers.
    /// </summary>
    public static class MakeGameRealistic
    {
        private const string TexturesDir = "Assets/_Project/Art/Textures";
        private const string ModelsDir = "Assets/_Project/Art/Models";

        [MenuItem("Survival Island/🌟 METTRE EN PLACE L'ÎLE TROPICALE RÉALISTE (Mer, Ciel, Animaux, Palmiers)", false, 0)]
        public static void MakeSceneRealistic()
        {
            Undo.SetCurrentGroupName("Make Survival Island Realistic Pro");
            int group = Undo.GetCurrentGroup();

            Debug.Log("<color=#4CAF50><b>[Technical Artist Pro]</b> 1/6 : Génération des textures PBR réelles et des modèles 3D OBJ...</color>");
            ProIslandAssetsBuilder.GenerateAllProAssets();

            Debug.Log("<color=#4CAF50><b>[Technical Artist Pro]</b> 2/6 : Construction de l'île 3D avec textures PBR et Normal Maps...</color>");
            var (terrain, ocean) = TropicalIslandGenerator.GenerateFullTropicalIsland();

            Debug.Log("<color=#4CAF50><b>[Technical Artist Pro]</b> 3/6 : Configuration du Ciel, de la Lumière solaire et de l'Atmosphère...</color>");
            SetupTropicalSkyAndAtmosphere();

            Debug.Log("<color=#4CAF50><b>[Technical Artist Pro]</b> 4/6 : Positionnement du Joueur sur la plage de sable face au lagon...</color>");
            SetupRealisticPlayer(terrain);

            Debug.Log("<color=#4CAF50><b>[Technical Artist Pro]</b> 5/6 : Cuisson du NavMesh sur l'île...</color>");
            BakeNavMeshOnIsland(terrain.gameObject);

            Debug.Log("<color=#4CAF50><b>[Technical Artist Pro]</b> 6/6 : Intégration de la Faune Sauvage Réaliste et Post-Processing URP...</color>");
            SetupRealisticWildlife(terrain);
            SetupPostProcessing();

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("<color=#4CAF50><b>[Technical Artist Pro]</b> TERMINE AVEC SUCCES ! L'île tropicale réaliste est prête avec ses textures PBR, mer turquoise, forêt Fantasy Forest, meute de loups, sangliers et ciel panoramique HD !</color>");
        }

        #region Ciel & Éclairage Tropical
        private static void SetupTropicalSkyAndAtmosphere()
        {
            GameObject sunObj = GameObject.Find("Sun_DirectionalLight");
            Light sunLight = null;
            if (sunObj != null)
            {
                sunLight = sunObj.GetComponent<Light>();
            }
            else
            {
                sunLight = RenderSettings.sun;
                if (sunLight == null)
                {
                    GameObject dirObj = GameObject.Find("Directional Light");
                    if (dirObj != null) sunLight = dirObj.GetComponent<Light>();
                }
            }

            if (sunLight != null)
            {
                sunLight.color = new Color(1.0f, 0.95f, 0.86f);
                sunLight.intensity = 1.4f;
                sunLight.shadows = LightShadows.Soft;
                sunLight.shadowStrength = 0.88f;
                sunLight.transform.rotation = Quaternion.Euler(42f, -40f, 0f); // Ombres chaudes d'après-midi
            }

            Material daySky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fantasy Skybox FREE/Panoramics/FS002/FS002_Day.mat");
            Material sunriseSky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fantasy Skybox FREE/Panoramics/FS002/FS002_Sunrise.mat");
            Material sunsetSky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fantasy Skybox FREE/Panoramics/FS002/FS002_Sunset.mat");
            Material nightSky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fantasy Skybox FREE/Panoramics/FS002/FS002_Night.mat");
            Material rainySky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fantasy Skybox FREE/Panoramics/FS002/FS002_Rainy.mat");

            if (daySky != null)
            {
                RenderSettings.skybox = daySky;
            }
            else
            {
                Material fallbackSky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fantasy Skybox FREE/Cubemaps/Classic/FS000_Day_01.mat");
                if (fallbackSky != null) RenderSettings.skybox = fallbackSky;
            }

            DayNightCycle dayNight = Object.FindFirstObjectByType<DayNightCycle>();
            if (dayNight != null)
            {
                dayNight.ConfigureSkyboxes(daySky, sunriseSky, sunsetSky, nightSky);
            }

            WeatherSystem weather = Object.FindFirstObjectByType<WeatherSystem>();
            if (weather != null && rainySky != null)
            {
                weather.ConfigureRainSkybox(rainySky, daySky);
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.85f, 0.98f);
            RenderSettings.ambientEquatorColor = new Color(0.25f, 0.68f, 0.74f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.45f, 0.22f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.65f, 0.85f, 0.95f);
            RenderSettings.fogDensity = 0.002f;
        }
        #endregion

        #region Joueur Naufragé Réaliste
        private static void SetupRealisticPlayer(Terrain terrain)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("Player");

            if (player == null)
            {
                Phase2PlayerSetup.SetupPhase2Scene();
                player = GameObject.FindWithTag("Player");
            }

            if (player == null) return;

            // Emplacement : SUR LA PLAGE DE SABLE DORE face au lagon turquoise (Z = -160)
            Vector3 beachPos = new Vector3(0f, 0f, -160f);
            float terrainY = terrain.SampleHeight(beachPos);
            player.transform.position = new Vector3(beachPos.x, terrainY + 1.2f, beachPos.z);
            player.transform.rotation = Quaternion.Euler(0, 180f, 0); // Regarde l'océan turquoise

            Transform oldVisual = player.transform.Find("VisualModel");
            if (oldVisual != null)
            {
                Object.DestroyImmediate(oldVisual.gameObject);
            }

            // 1. Tente de charger le modèle V-Bot 2.0 d'Invector (Personnage Pro Haute Fidélité)
            GameObject invectorBot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Invector-3rdPersonController_LITE/3D Models/Characters/Invector@V-Bot 2.0/Prefab/VBOT2.0_Custom.prefab");
            GameObject realisticVisual;

            if (invectorBot != null)
            {
                realisticVisual = Object.Instantiate(invectorBot);
                realisticVisual.name = "VisualModel";
                // Supprime d'éventuels colliders enfants sur le modèle visuel pour éviter tout conflit physique
                foreach (var col in realisticVisual.GetComponentsInChildren<Collider>())
                {
                    Object.DestroyImmediate(col);
                }
            }
            else
            {
                // Modèle de survivant avec matériaux PBR procéduraux en fallback
                Material skinMat = ProceduralModelsBuilder.CreateMaterial("Mat_PlayerSkin", new Color(0.85f, 0.70f, 0.56f), 0.25f);
                Material shirtMat = ProceduralModelsBuilder.CreateMaterial("Mat_PlayerShirt", new Color(0.52f, 0.48f, 0.38f), 0.1f);
                Material pantsMat = ProceduralModelsBuilder.CreateMaterial("Mat_PlayerPants", new Color(0.20f, 0.26f, 0.34f), 0.15f);
                Material gearMat = ProceduralModelsBuilder.CreateMaterial("Mat_PlayerGear", new Color(0.36f, 0.24f, 0.14f), 0.1f);
                realisticVisual = ProceduralModelsBuilder.CreateRealisticPlayerModel(skinMat, shirtMat, pantsMat, gearMat);
                realisticVisual.name = "VisualModel";
            }

            realisticVisual.transform.SetParent(player.transform);
            realisticVisual.transform.localPosition = Vector3.zero;
            realisticVisual.transform.localRotation = Quaternion.identity;

            // Liaison et configuration de l'Animator
            Animator anim = realisticVisual.GetComponent<Animator>();
            if (anim == null)
            {
                anim = realisticVisual.AddComponent<Animator>();
            }
            anim.applyRootMotion = false; // Ne pas interférer avec le déplacement CharacterController

            // Génère automatiquement l'Animator Controller avec les animations réelles
            RuntimeAnimatorController animCtrl = PlayerAnimatorControllerBuilder.GenerateMixamoAnimatorController();
            if (animCtrl == null)
            {
                string fallbackCtrlPath = "Assets/Invector-3rdPersonController_LITE/Animator/Invector@BasicLocomotion.controller";
                animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(fallbackCtrlPath);
            }
            if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;

            // Réassigne l'Animator sur le PlayerController
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.SetAnimator(anim);
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                ThirdPersonCamera tpCam = mainCam.GetComponent<ThirdPersonCamera>();
                if (tpCam != null)
                {
                    tpCam.SetTarget(player.transform);
                }
                // Oriente la caméra face à la mer
                mainCam.transform.position = player.transform.position + new Vector3(0, 2.2f, 3.8f);
                mainCam.transform.rotation = Quaternion.Euler(15f, 180f, 0f);
            }
        }
        #endregion

        #region Faune Sauvage Réaliste (Pack d'Animaux & Meute de Loups URP)
        private static void SetupRealisticWildlife(Terrain terrain)
        {
            // 1. Extraction et génération de tous les Prefabs d'animaux dans Assets/_Project/Prefabs/Animals
            AnimalPackManager.SetupAllAnimals();

            // 2. Peuplement de l'île avec les multiples espèces
            AnimalPackManager.PopulateIslandWithAnimals();
        }
        #endregion

        #region Navigation (Baking NavMesh)
        private static void BakeNavMeshOnIsland(GameObject terrainObj)
        {
            if (terrainObj == null) return;

            if (terrainObj.GetComponent<IslandNavMeshRuntime>() == null)
            {
                terrainObj.AddComponent<IslandNavMeshRuntime>();
            }

            NavMeshSurface surface = terrainObj.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = terrainObj.AddComponent<NavMeshSurface>();
            }

            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();
        }
        #endregion

        #region Post-Processing URP
        private static void SetupPostProcessing()
        {
            GameObject volumeObj = GameObject.Find("Global Volume");
            if (volumeObj == null)
            {
                volumeObj = new GameObject("Global Volume");
                volumeObj.AddComponent<Volume>();
            }

            Volume volume = volumeObj.GetComponent<Volume>();
            volume.isGlobal = true;

            VolumeProfile profile = volume.sharedProfile;
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "TropicalIsland_Profile";
                string path = "Assets/Settings/TropicalIsland_Profile.asset";
                AssetDatabase.CreateAsset(profile, path);
                volume.sharedProfile = profile;
            }

            if (!profile.TryGet(out Tonemapping tonemapping))
            {
                tonemapping = profile.Add<Tonemapping>(true);
            }
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.35f;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.95f;

            if (!profile.TryGet(out ColorAdjustments colorAdj))
            {
                colorAdj = profile.Add<ColorAdjustments>(true);
            }
            colorAdj.postExposure.overrideState = true;
            colorAdj.postExposure.value = 0.2f;
            colorAdj.contrast.overrideState = true;
            colorAdj.contrast.value = 16f;
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = 24f;

            if (!profile.TryGet(out WhiteBalance whiteBal))
            {
                whiteBal = profile.Add<WhiteBalance>(true);
            }
            whiteBal.temperature.overrideState = true;
            whiteBal.temperature.value = 7f;
        }
        #endregion
    }

    /// <summary>
    /// Générateur complet d'île tropicale avec textures PBR et modèles OBJ.
    /// </summary>
    public static class TropicalIslandGenerator
    {
        public const int TerrainSize = 600;
        public const int TerrainHeight = 55;
        public const float WaterLevel = 1.8f;
        private const string TexturesDir = "Assets/_Project/Art/Textures";
        private const string ModelsDir = "Assets/_Project/Art/Models";

        public static (Terrain terrain, GameObject ocean) GenerateFullTropicalIsland()
        {
            Material sandMat = ProceduralModelsBuilder.CreateMaterial("Mat_TropicalSand", new Color(0.92f, 0.84f, 0.62f), 0.15f);
            Material grassMat = ProceduralModelsBuilder.CreateMaterial("Mat_JungleGrass", new Color(0.24f, 0.52f, 0.18f), 0.25f);
            Material rockMat = ProceduralModelsBuilder.CreateMaterial("Mat_VolcanicRock", new Color(0.32f, 0.30f, 0.28f), 0.1f);
            Material trunkMat = ProceduralModelsBuilder.CreateMaterial("Mat_PalmTrunk", new Color(0.42f, 0.30f, 0.20f), 0.1f);
            Material frondMat = ProceduralModelsBuilder.CreateMaterial("Mat_PalmFronds", new Color(0.18f, 0.65f, 0.15f), 0.45f);
            Material coconutMat = ProceduralModelsBuilder.CreateMaterial("Mat_Coconut", new Color(0.35f, 0.22f, 0.12f), 0.2f);

            Terrain terrain = CreateIslandTerrain(sandMat, grassMat, rockMat);
            GameObject ocean = CreateTropicalOcean(WaterLevel);
            PopulateIslandVegetation(terrain, trunkMat, frondMat, coconutMat, rockMat);

            return (terrain, ocean);
        }

        private static Terrain CreateIslandTerrain(Material sandMat, Material grassMat, Material rockMat)
        {
            GameObject existingTerrainObj = GameObject.Find("--- TROPICAL ISLAND TERRAIN ---");
            if (existingTerrainObj != null)
            {
                Object.DestroyImmediate(existingTerrainObj);
            }

            TerrainData terrainData = new TerrainData
            {
                heightmapResolution = 257,
                size = new Vector3(TerrainSize, TerrainHeight, TerrainSize)
            };

            int res = terrainData.heightmapResolution;
            float[,] heights = new float[res, res];

            Vector2 center = new Vector2(res * 0.5f, res * 0.5f);
            float maxRadius = res * 0.44f;
            float waterNorm = WaterLevel / TerrainHeight;

            for (int x = 0; x < res; x++)
            {
                for (int y = 0; y < res; y++)
                {
                    float distFromCenter = Vector2.Distance(new Vector2(x, y), center);
                    float normalizedDist = distFromCenter / maxRadius;

                    float nx = (float)x / res;
                    float ny = (float)y / res;

                    // 1. Pic volcanique central escarpé
                    float mountain = Mathf.Pow(Mathf.PerlinNoise(nx * 2.2f + 14.2f, ny * 2.2f + 31.8f), 2.2f) * 0.72f;
                    // 2. Collines de jungle ondulantes
                    float jungleHills = Mathf.PerlinNoise(nx * 4.5f + 5.1f, ny * 4.5f + 8.9f) * 0.22f;
                    // 3. Micro-reliefs et dunes côtières
                    float microRelief = Mathf.PerlinNoise(nx * 10f, ny * 10f) * 0.035f;

                    float rawElevation = mountain + jungleHills + microRelief;
                    float heightVal;

                    if (normalizedDist > 0.96f)
                    {
                        heightVal = 0.005f; // Abysse sous-marin profond
                    }
                    else if (normalizedDist > 0.82f)
                    {
                        // Lagon sous-marin translucide (plateau de sable sous l'eau turquoise)
                        float tLagoon = (normalizedDist - 0.82f) / (0.96f - 0.82f);
                        heightVal = Mathf.Lerp(waterNorm - 0.006f, 0.008f, tLagoon);
                    }
                    else if (normalizedDist > 0.62f)
                    {
                        // Plage de sable doré en pente très douce vers l'océan
                        float tBeach = (normalizedDist - 0.62f) / (0.82f - 0.62f);
                        float beachTop = waterNorm + 0.065f; // ~5.5m au-dessus de l'eau
                        float beachEdge = waterNorm - 0.006f; // se fond sous l'eau
                        heightVal = Mathf.Lerp(beachTop, beachEdge, Mathf.SmoothStep(0f, 1f, tBeach)) + microRelief * 0.2f;
                    }
                    else
                    {
                        // Jungle luxuriante et reliefs intérieurs
                        float islandFalloff = Mathf.Clamp01(1f - Mathf.Pow(normalizedDist / 0.62f, 1.8f));
                        heightVal = Mathf.Max(waterNorm + 0.065f, (waterNorm + 0.065f) + rawElevation * islandFalloff * 0.85f);
                    }

                    heights[x, y] = heightVal;
                }
            }

            terrainData.SetHeights(0, 0, heights);

            TerrainLayer[] layers = CreateTerrainLayers();
            terrainData.terrainLayers = layers;

            ApplySplatmaps(terrainData);

            GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
            terrainObj.name = "--- TROPICAL ISLAND TERRAIN ---";
            terrainObj.transform.position = new Vector3(-TerrainSize * 0.5f, 0f, -TerrainSize * 0.5f);

            Terrain terrainComp = terrainObj.GetComponent<Terrain>();
            terrainComp.materialTemplate = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit") ?? Shader.Find("Nature/Terrain/Standard"));

            return terrainComp;
        }

        private static TerrainLayer[] CreateTerrainLayers()
        {
            // Charge les vraies textures PBR 512x512 générées (avec fallback procédural instantané)
            Texture2D sandAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/Sand_Tropical_Albedo.png") 
                ?? CreateProceduralNoiseTexture(new Color(0.92f, 0.84f, 0.62f), new Color(0.82f, 0.74f, 0.52f));
            Texture2D sandNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/Sand_Tropical_Normal.png");

            Texture2D grassAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/Grass_Jungle_Albedo.png")
                ?? CreateProceduralNoiseTexture(new Color(0.18f, 0.48f, 0.12f), new Color(0.32f, 0.62f, 0.18f));
            Texture2D grassNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/Grass_Jungle_Normal.png");

            Texture2D rockAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/Rock_Volcanic_Albedo.png")
                ?? CreateProceduralNoiseTexture(new Color(0.24f, 0.22f, 0.20f), new Color(0.38f, 0.35f, 0.32f));
            Texture2D rockNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/Rock_Volcanic_Normal.png");

            TerrainLayer layerSand = new TerrainLayer
            {
                name = "Layer_TropicalSand",
                diffuseTexture = sandAlbedo,
                normalMapTexture = sandNormal,
                normalScale = 1.6f,
                tileSize = new Vector2(4, 4),
                smoothness = 0.15f
            };

            TerrainLayer layerGrass = new TerrainLayer
            {
                name = "Layer_JungleGrass",
                diffuseTexture = grassAlbedo,
                normalMapTexture = grassNormal,
                normalScale = 1.4f,
                tileSize = new Vector2(4, 4),
                smoothness = 0.22f
            };

            TerrainLayer layerRock = new TerrainLayer
            {
                name = "Layer_VolcanicRock",
                diffuseTexture = rockAlbedo,
                normalMapTexture = rockNormal,
                normalScale = 2.0f,
                tileSize = new Vector2(6, 6),
                smoothness = 0.1f
            };

            return new TerrainLayer[] { layerSand, layerGrass, layerRock };
        }

        private static Texture2D CreateProceduralNoiseTexture(Color c1, Color c2, int size = 128)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.12f, y * 0.12f);
                    tex.SetPixel(x, y, Color.Lerp(c1, c2, n));
                }
            }
            tex.Apply();
            return tex;
        }

        private static void ApplySplatmaps(TerrainData data)
        {
            int alphaRes = data.alphamapResolution;
            float[,,] splatmapData = new float[alphaRes, alphaRes, 3];

            for (int y = 0; y < alphaRes; y++)
            {
                for (int x = 0; x < alphaRes; x++)
                {
                    float normX = (float)x / alphaRes;
                    float normY = (float)y / alphaRes;

                    float height = data.GetHeight(Mathf.RoundToInt(normX * data.heightmapResolution), Mathf.RoundToInt(normY * data.heightmapResolution));
                    float steepness = data.GetSteepness(normX, normY);

                    float sandWeight = 0f;
                    float grassWeight = 0f;
                    float rockWeight = 0f;

                    // Falaises et roches volcaniques sur fortes pentes
                    if (steepness > 22f)
                    {
                        rockWeight = Mathf.Clamp01((steepness - 22f) / 10f);
                    }

                    // Bande côtière de sable fin doré (toute la plage et hauts-fonds)
                    if (height < 6.0f)
                    {
                        sandWeight = Mathf.Clamp01(1f - (height / 6.0f));
                    }

                    grassWeight = Mathf.Clamp01(1f - (sandWeight + rockWeight));

                    float total = sandWeight + grassWeight + rockWeight;
                    if (total > 0f)
                    {
                        splatmapData[x, y, 0] = sandWeight / total;
                        splatmapData[x, y, 1] = grassWeight / total;
                        splatmapData[x, y, 2] = rockWeight / total;
                    }
                    else
                    {
                        splatmapData[x, y, 0] = 1f; // Défaut sable doré
                    }
                }
            }

            data.SetAlphamaps(0, 0, splatmapData);
        }

        private static GameObject CreateTropicalOcean(float waterY)
        {
            GameObject existingOcean = GameObject.Find("--- TROPICAL OCEAN ---");
            if (existingOcean != null)
            {
                Object.DestroyImmediate(existingOcean);
            }

            GameObject ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ocean.name = "--- TROPICAL OCEAN ---";
            ocean.transform.position = new Vector3(0, waterY, 0);
            ocean.transform.localScale = new Vector3(140f, 1f, 140f);

            Material waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            waterMat.name = "Mat_TropicalOcean";

            // Configuration URP Transparent pour voir le lagon et le sable sous l'eau
            waterMat.SetFloat("_Surface", 1); // Transparent
            waterMat.SetFloat("_Blend", 0);   // Alpha
            waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            waterMat.SetInt("_ZWrite", 0);
            waterMat.DisableKeyword("_ALPHATEST_ON");
            waterMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            waterMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // Couleur turquoise des Caraïbes translucide
            Color lagoonColor = new Color(0.05f, 0.74f, 0.84f, 0.74f);
            waterMat.color = lagoonColor;
            if (waterMat.HasProperty("_BaseColor")) waterMat.SetColor("_BaseColor", lagoonColor);
            if (waterMat.HasProperty("_Smoothness")) waterMat.SetFloat("_Smoothness", 0.97f); // Miroitement spéculaire du soleil
            if (waterMat.HasProperty("_Metallic")) waterMat.SetFloat("_Metallic", 0.05f);

            // Normal Map pour ondulations des vagues
            Texture2D waterNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/Water_Normal.png");
            if (waterNormal != null && waterMat.HasProperty("_BumpMap"))
            {
                waterMat.SetTexture("_BumpMap", waterNormal);
                waterMat.EnableKeyword("_NORMALMAP");
            }

            ocean.GetComponent<Renderer>().sharedMaterial = waterMat;
            ocean.AddComponent<WaterAnimator>();

            return ocean;
        }

        private static void PopulateIslandVegetation(Terrain terrain, Material trunkMat, Material frondMat, Material coconutMat, Material rockMat)
        {
            GameObject vegRoot = GameObject.Find("--- TROPICAL VEGETATION ---");
            if (vegRoot != null) Object.DestroyImmediate(vegRoot);

            vegRoot = new GameObject("--- TROPICAL VEGETATION ---");

            GameObject palmMeshAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/Environment/PalmTree_Realistic.obj");
            GameObject rockMeshAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/Environment/CoastalRock_Realistic.obj");

            // Matériau pour les palmes découpées
            Texture2D frondTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/PalmFrond_Leaf_Albedo.png");
            if (frondTex != null && frondMat != null && frondMat.HasProperty("_BaseMap"))
            {
                frondMat.SetTexture("_BaseMap", frondTex);
            }

            // 1. Placement de 75 Palmiers 3D courbés penchant vers l'océan le long de la plage
            int palmCount = 75;
            for (int i = 0; i < palmCount; i++)
            {
                float angle = (360f / palmCount) * i + Random.Range(-4f, 4f);
                float rad = angle * Mathf.Deg2Rad;
                float dist = Random.Range(130f, 185f); // Plage sablonneuse

                float worldX = Mathf.Cos(rad) * dist;
                float worldZ = Mathf.Sin(rad) * dist;
                float sampleY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ));

                if (sampleY > WaterLevel + 0.15f && sampleY < 7.5f)
                {
                    GameObject palm;
                    if (palmMeshAsset != null)
                    {
                        palm = Object.Instantiate(palmMeshAsset);
                        Renderer r = palm.GetComponentInChildren<Renderer>();
                        if (r != null) r.sharedMaterial = trunkMat;
                    }
                    else
                    {
                        palm = ProceduralModelsBuilder.CreatePalmTree(trunkMat, frondMat, coconutMat);
                    }

                    palm.name = $"PalmTree_{i + 1}";
                    palm.transform.SetParent(vegRoot.transform);
                    palm.transform.position = new Vector3(worldX, sampleY, worldZ);

                    // Oriente la courbe du tronc vers la mer pour un effet carte postale
                    Vector3 seawardDir = new Vector3(worldX, 0, worldZ).normalized;
                    palm.transform.localRotation = Quaternion.LookRotation(seawardDir) * Quaternion.Euler(0, Random.Range(-25f, 25f), 0);
                    palm.transform.localScale = Vector3.one * Random.Range(1.0f, 1.45f);
                }
            }

            // 2. Placement de 35 Rochers côtiers léchés par les vagues
            int rockCount = 35;
            for (int r = 0; r < rockCount; r++)
            {
                float angle = (360f / rockCount) * r + Random.Range(-5f, 5f);
                float rad = angle * Mathf.Deg2Rad;
                float dist = Random.Range(165f, 205f); // Bord de l'eau et hauts-fonds

                float worldX = Mathf.Cos(rad) * dist;
                float worldZ = Mathf.Sin(rad) * dist;
                float sampleY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ));

                GameObject rock;
                if (rockMeshAsset != null)
                {
                    rock = Object.Instantiate(rockMeshAsset);
                    Renderer rend = rock.GetComponentInChildren<Renderer>();
                    if (rend != null) rend.sharedMaterial = rockMat;
                }
                else
                {
                    rock = ProceduralModelsBuilder.CreateCoastalRock(rockMat);
                }

                rock.name = $"CoastalRock_{r + 1}";
                rock.transform.SetParent(vegRoot.transform);
                rock.transform.position = new Vector3(worldX, sampleY - 0.2f, worldZ);
                rock.transform.localRotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0, 360), Random.Range(-10f, 10f));
                rock.transform.localScale = Vector3.one * Random.Range(1.2f, 2.5f);
            }

            // 3. Montagne Centrale 3D Réaliste (Pack Mountain Terrain rocks and tree)
            UpgradeMountainMaterialsToURP();
            GameObject mountainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mountain Terrain rocks and tree/Prefab/mountain_terrain_01.prefab");
            if (mountainPrefab != null)
            {
                GameObject mountainObj = Object.Instantiate(mountainPrefab);
                mountainObj.name = "--- MOUNTAIN_PEAK_CENTER ---";
                mountainObj.transform.SetParent(vegRoot.transform);
                // Positionne le massif montagneux au centre de l'île
                mountainObj.transform.position = new Vector3(0f, 10f, 20f);
                mountainObj.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
                mountainObj.transform.localScale = new Vector3(1.1f, 1.25f, 1.1f);
            }

            // 4. Formations Rocheuses et Falaises 3D Pro (Pack Mountain Terrain rocks and tree)
            List<GameObject> mountainRockPrefabs = new List<GameObject>();
            for (int r = 1; r <= 4; r++)
            {
                GameObject rockAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Mountain Terrain rocks and tree/Prefab/rock_set_0{r}.prefab");
                if (rockAsset != null) mountainRockPrefabs.Add(rockAsset);
            }

            if (mountainRockPrefabs.Count > 0)
            {
                int mRockCount = 28;
                for (int i = 0; i < mRockCount; i++)
                {
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float dist = Random.Range(30f, 155f);
                    float rx = Mathf.Cos(angle) * dist;
                    float rz = Mathf.Sin(angle) * dist;
                    float ry = terrain.SampleHeight(new Vector3(rx, 0, rz));

                    if (ry > 5.0f)
                    {
                        GameObject selectedRock = mountainRockPrefabs[Random.Range(0, mountainRockPrefabs.Count)];
                        GameObject rockInstance = Object.Instantiate(selectedRock);
                        rockInstance.name = $"MountainCliffRock_{i + 1}";
                        rockInstance.transform.SetParent(vegRoot.transform);
                        rockInstance.transform.position = new Vector3(rx, ry - 0.5f, rz);
                        rockInstance.transform.localRotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0, 360), Random.Range(-15f, 15f));
                        rockInstance.transform.localScale = Vector3.one * Random.Range(1.2f, 2.8f);
                    }
                }
            }

            // 5. Forêt tropicale luxuriante (Pack Anime Trees & Mountain Trees)
            List<GameObject> treePrefabs = new List<GameObject>();
            for (int t = 1; t <= 8; t++)
            {
                GameObject treeAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Anime Trees/Prefabs/Tree_0{t}.prefab");
                if (treeAsset != null) treePrefabs.Add(treeAsset);
            }
            GameObject mountainTreeAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mountain Terrain rocks and tree/Prefab/tree_01.prefab");
            if (mountainTreeAsset != null) treePrefabs.Add(mountainTreeAsset);

            List<GameObject> bushPrefabs = new List<GameObject>();
            for (int b = 1; b <= 4; b++)
            {
                GameObject bushAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Anime Trees/Prefabs/Bush_0{b}.prefab");
                if (bushAsset != null) bushPrefabs.Add(bushAsset);
            }

            List<GameObject> grassPrefabs = new List<GameObject>();
            GameObject grassAsset1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Anime Trees/Prefabs/Grass.prefab");
            GameObject grassAsset2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Anime Trees/Prefabs/GrassMesh.prefab");
            if (grassAsset1 != null) grassPrefabs.Add(grassAsset1);
            if (grassAsset2 != null) grassPrefabs.Add(grassAsset2);

            // Arbres de jungle et montagne
            if (treePrefabs.Count > 0)
            {
                int jungleTreeCount = 65;
                for (int j = 0; j < jungleTreeCount; j++)
                {
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float dist = Random.Range(20f, 135f); // Intérieur des terres
                    float wx = Mathf.Cos(angle) * dist;
                    float wz = Mathf.Sin(angle) * dist;
                    float wy = terrain.SampleHeight(new Vector3(wx, 0, wz));

                    if (wy > 5.5f)
                    {
                        GameObject selectedTree = treePrefabs[Random.Range(0, treePrefabs.Count)];
                        GameObject treeInstance = Object.Instantiate(selectedTree);
                        treeInstance.name = $"JungleTree_{j + 1}";
                        treeInstance.transform.SetParent(vegRoot.transform);
                        treeInstance.transform.position = new Vector3(wx, wy, wz);
                        treeInstance.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                        treeInstance.transform.localScale = Vector3.one * Random.Range(0.85f, 1.45f);
                    }
                }
            }

            // Buissons et sous-bois
            if (bushPrefabs.Count > 0)
            {
                int bushCount = 45;
                for (int b = 0; b < bushCount; b++)
                {
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float dist = Random.Range(25f, 150f);
                    float bx = Mathf.Cos(angle) * dist;
                    float bz = Mathf.Sin(angle) * dist;
                    float by = terrain.SampleHeight(new Vector3(bx, 0, bz));

                    if (by > 4.5f)
                    {
                        GameObject selectedBush = bushPrefabs[Random.Range(0, bushPrefabs.Count)];
                        GameObject bushInstance = Object.Instantiate(selectedBush);
                        bushInstance.name = $"TropicalBush_{b + 1}";
                        bushInstance.transform.SetParent(vegRoot.transform);
                        bushInstance.transform.position = new Vector3(bx, by, bz);
                        bushInstance.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                        bushInstance.transform.localScale = Vector3.one * Random.Range(0.9f, 1.6f);
                    }
                }
            }

            // Touffes d'herbe haute 3D
            if (grassPrefabs.Count > 0)
            {
                int grassCount = 65;
                for (int g = 0; g < grassCount; g++)
                {
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float dist = Random.Range(30f, 150f);
                    float gx = Mathf.Cos(angle) * dist;
                    float gz = Mathf.Sin(angle) * dist;
                    float gy = terrain.SampleHeight(new Vector3(gx, 0, gz));

                    if (gy > 4.0f)
                    {
                        GameObject selectedGrass = grassPrefabs[Random.Range(0, grassPrefabs.Count)];
                        GameObject grassInstance = Object.Instantiate(selectedGrass);
                        grassInstance.name = $"JungleGrass_{g + 1}";
                        grassInstance.transform.SetParent(vegRoot.transform);
                        grassInstance.transform.position = new Vector3(gx, gy, gz);
                        grassInstance.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                        grassInstance.transform.localScale = Vector3.one * Random.Range(1.0f, 1.8f);
                    }
                }
            }

            // 6. Grands Chênes & Végétation Détaillée (Pack Fantasy Forest Environment Free Sample)
            UpgradeFantasyForestMaterialsToURP();
            GameObject fantasyTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Fantasy Forest Environment Free Sample/Meshes/Prefabs/tree_1.prefab");
            GameObject fantasyGrassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Fantasy Forest Environment Free Sample/Meshes/Prefabs/grass01.prefab");

            if (fantasyTreePrefab != null)
            {
                int fTreeCount = 45;
                for (int ft = 0; ft < fTreeCount; ft++)
                {
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float dist = Random.Range(35f, 145f);
                    float fx = Mathf.Cos(angle) * dist;
                    float fz = Mathf.Sin(angle) * dist;
                    float fy = terrain.SampleHeight(new Vector3(fx, 0, fz));

                    if (fy > 4.5f && fy < 38f)
                    {
                        GameObject ftObj = Object.Instantiate(fantasyTreePrefab);
                        ftObj.name = $"FantasyForest_Oak_{ft + 1}";
                        ftObj.transform.SetParent(vegRoot.transform);
                        ftObj.transform.position = new Vector3(fx, fy, fz);
                        ftObj.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                        ftObj.transform.localScale = Vector3.one * Random.Range(0.85f, 1.35f);
                    }
                }
            }

            if (fantasyGrassPrefab != null)
            {
                int fGrassCount = 75;
                for (int fg = 0; fg < fGrassCount; fg++)
                {
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float dist = Random.Range(25f, 155f);
                    float gx = Mathf.Cos(angle) * dist;
                    float gz = Mathf.Sin(angle) * dist;
                    float gy = terrain.SampleHeight(new Vector3(gx, 0, gz));

                    if (gy > 3.0f && gy < 28f)
                    {
                        GameObject fgObj = Object.Instantiate(fantasyGrassPrefab);
                        fgObj.name = $"FantasyForest_Grass_{fg + 1}";
                        fgObj.transform.SetParent(vegRoot.transform);
                        fgObj.transform.position = new Vector3(gx, gy, gz);
                        fgObj.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                        fgObj.transform.localScale = Vector3.one * Random.Range(1.0f, 1.8f);
                    }
                }
            }
        }

        private static void UpgradeFantasyForestMaterialsToURP()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            string[] solidMats = new string[]
            {
                "Assets/Fantasy Forest Environment Free Sample/Materials/bark01_bottom.mat",
                "Assets/Fantasy Forest Environment Free Sample/Materials/dirt01.mat"
            };

            foreach (var path in solidMats)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m != null)
                {
                    Texture main = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                    m.shader = urpLit;
                    if (main != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", main);
                    if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
                    EditorUtility.SetDirty(m);
                }
            }

            string[] cutoutMats = new string[]
            {
                "Assets/Fantasy Forest Environment Free Sample/Materials/tree_branches.mat",
                "Assets/Fantasy Forest Environment Free Sample/Materials/grass01.mat",
                "Assets/Fantasy Forest Environment Free Sample/Materials/grassmesh.mat"
            };

            foreach (var path in cutoutMats)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m != null)
                {
                    Texture main = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                    m.shader = urpLit;
                    if (main != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", main);

                    // Alpha Cutout & Rendu Double-Face pour feuillage luxuriant
                    if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.45f);
                    if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
                    if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f); // Double-face (Cull Off)
                    if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 0f); // Opaque
                    m.EnableKeyword("_ALPHATEST_ON");
                    m.renderQueue = (int)RenderQueue.AlphaTest;
                    EditorUtility.SetDirty(m);
                }
            }
        }

        private static void UpgradeMountainMaterialsToURP()
        {
            string[] matPaths = new string[]
            {
                "Assets/Mountain Terrain rocks and tree/Materials/Terrain/mountain_terrain_01/mountain_terrain_01.mat",
                "Assets/Mountain Terrain rocks and tree/Materials/Rock/rock_set_mat.mat",
                "Assets/Mountain Terrain rocks and tree/Materials/Trees/tree_01/bark_01.mat",
                "Assets/Mountain Terrain rocks and tree/Materials/Trees/tree_01/leaves_01.mat"
            };

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            foreach (var path in matPaths)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m != null)
                {
                    Texture main = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                    Texture bump = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
                    m.shader = urpLit;
                    if (main != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", main);
                    if (bump != null && m.HasProperty("_BumpMap")) m.SetTexture("_BumpMap", bump);
                    EditorUtility.SetDirty(m);
                }
            }
        }
    }
}
#endif
