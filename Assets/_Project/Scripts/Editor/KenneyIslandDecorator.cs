#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurvivalIsland.Editor
{
    public static class KenneyIslandDecorator
    {
        private const string RootName = "--- KENNEY TROPICAL DECOR ---";
        private const string KenneyDir = "Assets/_Project/Art/ThirdParty/Kenney_NatureKit/Models";
        private const string PolyHavenDir = "Assets/_Project/Art/ThirdParty/PolyHaven/Textures";
        private const string LayersDir = "Assets/_Project/Art/ThirdParty/PolyHaven/TerrainLayers";
        private const string KenneyMaterialsDir = "Assets/_Project/Art/ThirdParty/Kenney_NatureKit/Materials";
        private const float WaterLevel = TropicalIslandGenerator.WaterLevel;

        // Kenney's default palette is pastel/teal; these tones blend with the realistic Fantasy Forest trees.
        private static readonly Dictionary<string, Color> NaturalPalette = new Dictionary<string, Color>
        {
            { "leafsGreen", new Color(0.24f, 0.52f, 0.16f) },
            { "leafsDark", new Color(0.15f, 0.38f, 0.12f) },
            { "grass", new Color(0.30f, 0.56f, 0.18f) },
            { "woodBark", new Color(0.45f, 0.33f, 0.22f) },
            { "woodBarkDark", new Color(0.36f, 0.26f, 0.18f) },
            { "wood", new Color(0.58f, 0.42f, 0.27f) },
            { "woodDark", new Color(0.38f, 0.26f, 0.17f) },
            { "woodInner", new Color(0.78f, 0.66f, 0.48f) },
            { "dirt", new Color(0.48f, 0.36f, 0.24f) },
            { "dirtDark", new Color(0.36f, 0.27f, 0.18f) },
            { "stone", new Color(0.56f, 0.55f, 0.52f) },
            { "stoneDark", new Color(0.42f, 0.41f, 0.39f) },
            { "colorRed", new Color(0.62f, 0.22f, 0.18f) },
            { "colorRedDark", new Color(0.48f, 0.16f, 0.14f) },
            { "colorTan", new Color(0.78f, 0.64f, 0.44f) },
        };

        private static readonly string[] Palms = { "tree_palmTall", "tree_palmDetailedTall", "tree_palmBend", "tree_palm", "tree_palmShort", "tree_palmDetailedShort" };
        private static readonly string[] Bushes = { "plant_bush", "plant_bushDetailed", "plant_bushLarge", "plant_flatTall", "plant_flatShort", "grass_large" };
        private static readonly string[] Rocks = { "rock_largeA", "rock_largeB", "rock_largeC", "rock_largeD", "rock_tallA", "rock_tallB", "rock_tallC" };

        [MenuItem("Survival Island/🌴 Décorer l'île (Palmiers Kenney, Sable & Roche Poly Haven)", false, 5)]
        public static void DecorateIsland()
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                Debug.LogWarning("[Kenney Decor] Aucun Terrain actif dans la scène.");
                return;
            }

            ApplyNaturalKenneyMaterials();
            ApplyPolyHavenTerrainLayers(terrain);
            ScatterDecor(terrain);

            EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
            Debug.Log("<color=#4CAF50><b>[Kenney Decor]</b> Île décorée (textures Poly Haven + palmiers, buissons, rochers et campement Kenney).</color>");
        }

        private static void ApplyNaturalKenneyMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) return;
            Directory.CreateDirectory(KenneyMaterialsDir);

            var materials = new Dictionary<string, Material>();
            foreach (var entry in NaturalPalette)
            {
                string path = $"{KenneyMaterialsDir}/{entry.Key}.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(lit);
                    AssetDatabase.CreateAsset(mat, path);
                }
                mat.SetColor("_BaseColor", entry.Value);
                mat.SetFloat("_Smoothness", 0.12f);
                EditorUtility.SetDirty(mat);
                materials[entry.Key] = mat;
            }
            AssetDatabase.SaveAssets();

            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { KenneyDir }))
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(modelPath) is ModelImporter importer)) continue;

                var existing = importer.GetExternalObjectMap();
                bool changed = false;
                foreach (Object sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(modelPath))
                {
                    if (!(sub is Material) || !materials.TryGetValue(sub.name, out Material target)) continue;
                    var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), sub.name);
                    if (existing.TryGetValue(id, out Object current) && current == target) continue;
                    importer.AddRemap(id, target);
                    changed = true;
                }
                if (changed) importer.SaveAndReimport();
            }
        }

        private static void ApplyPolyHavenTerrainLayers(Terrain terrain)
        {
            TerrainLayer[] current = terrain.terrainData.terrainLayers;
            if (current.Length < 3) return;

            Directory.CreateDirectory(LayersDir);
            TerrainLayer sand = CreateLayer("Layer_PH_CoastSand", "coast_sand_01", new Vector2(14, 14), current[0]);
            TerrainLayer rock = CreateLayer("Layer_PH_RockFace", "rock_face", new Vector2(16, 16), current[2]);

            TerrainLayer[] layers = (TerrainLayer[])current.Clone();
            layers[0] = sand ?? current[0];
            layers[2] = rock ?? current[2];
            Undo.RecordObject(terrain.terrainData, "Apply Poly Haven Terrain Layers");
            terrain.terrainData.terrainLayers = layers;
        }

        private static TerrainLayer CreateLayer(string layerName, string texName, Vector2 tile, TerrainLayer fallback)
        {
            string diffusePath = $"{PolyHavenDir}/{texName}/{texName}_diff_2k.jpg";
            string normalPath = $"{PolyHavenDir}/{texName}/{texName}_nor_gl_2k.jpg";
            Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
            if (diffuse == null)
            {
                Debug.LogWarning($"[Kenney Decor] Texture introuvable : {diffusePath}");
                return null;
            }

            if (AssetImporter.GetAtPath(normalPath) is TextureImporter normalImporter && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            string path = $"{LayersDir}/{layerName}.terrainlayer";
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }
            layer.diffuseTexture = diffuse;
            layer.normalMapTexture = normal;
            layer.normalScale = 1f;
            layer.tileSize = tile;
            layer.smoothness = fallback != null ? fallback.smoothness : 0.1f;
            EditorUtility.SetDirty(layer);
            AssetDatabase.SaveAssets();
            return layer;
        }

        private static void ScatterDecor(Terrain terrain)
        {
            GameObject existing = GameObject.Find(RootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Kenney Tropical Decor");

            Random.State previous = Random.state;
            Random.InitState(20261011);

            GameObject player = GameObject.Find("Player");
            Vector3 spawn = player != null ? player.transform.position : Vector3.zero;

            GameObject palmsRoot = Child(root, "Palmiers");
            GameObject bushesRoot = Child(root, "Buissons");
            GameObject rocksRoot = Child(root, "Rochers");

            Scatter(terrain, palmsRoot, Palms, 140, WaterLevel + 0.6f, WaterLevel + 6f, 22f, 6.5f, 9.5f, spawn, ColliderKind.Capsule);
            Scatter(terrain, bushesRoot, Bushes, 220, WaterLevel + 4f, 40f, 30f, 4f, 7f, spawn, ColliderKind.None);
            Scatter(terrain, rocksRoot, Rocks, 70, WaterLevel + 0.3f, 40f, 45f, 3f, 8f, spawn, ColliderKind.Mesh);

            BuildCampsite(terrain, Child(root, "Campement"), spawn);

            Random.state = previous;
        }

        private enum ColliderKind { None, Capsule, Mesh }

        private static GameObject Child(GameObject parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static void Scatter(Terrain terrain, GameObject parent, string[] models, int count, float minHeight, float maxHeight,
            float maxSlope, float minScale, float maxScale, Vector3 spawn, ColliderKind collider)
        {
            List<GameObject> prefabs = LoadModels(models);
            if (prefabs.Count == 0) return;

            TerrainData data = terrain.terrainData;
            Vector3 origin = terrain.transform.position;
            int placed = 0;
            for (int attempt = 0; attempt < count * 30 && placed < count; attempt++)
            {
                float nx = Random.value, nz = Random.value;
                float height = data.GetInterpolatedHeight(nx, nz) + origin.y;
                if (height < minHeight || height > maxHeight) continue;
                if (data.GetSteepness(nx, nz) > maxSlope) continue;

                Vector3 pos = new Vector3(origin.x + nx * data.size.x, height, origin.z + nz * data.size.z);
                if (Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(spawn.x, 0, spawn.z)) < 6f) continue;

                GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
                Place(prefab, parent, pos, Random.Range(minScale, maxScale), collider);
                placed++;
            }
        }

        private static GameObject Place(GameObject prefab, GameObject parent, Vector3 pos, float scale, ColliderKind collider)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            go.transform.position = pos - Vector3.up * 0.05f * scale;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            go.transform.localScale = Vector3.one * scale;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);

            if (collider == ColliderKind.Capsule)
            {
                CapsuleCollider col = go.AddComponent<CapsuleCollider>();
                col.radius = 0.06f;
                col.height = 1.2f;
                col.center = new Vector3(0f, 0.6f, 0f);
            }
            else if (collider == ColliderKind.Mesh)
            {
                foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
                {
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                }
            }
            return go;
        }

        private static void BuildCampsite(Terrain terrain, GameObject parent, Vector3 spawn)
        {
            Vector3 center = spawn + new Vector3(4f, 0f, 6f);
            PlaceOnTerrain(terrain, parent, "campfire_stones", center, 3f, 0f, ColliderKind.None);
            PlaceOnTerrain(terrain, parent, "campfire_logs", center, 3f, 0f, ColliderKind.None);
            PlaceOnTerrain(terrain, parent, "tent_detailedOpen", center + new Vector3(-6f, 0f, 3f), 4f, 160f, ColliderKind.Mesh);
            PlaceOnTerrain(terrain, parent, "log", center + new Vector3(2.5f, 0f, -1.5f), 3f, 30f, ColliderKind.Mesh);
            PlaceOnTerrain(terrain, parent, "log_large", center + new Vector3(-2.5f, 0f, -2f), 3f, -40f, ColliderKind.Mesh);

            Vector3 shore = FindShore(terrain, spawn, Vector3.back);
            PlaceOnTerrain(terrain, parent, "canoe", shore, 4f, 75f, ColliderKind.Mesh);
            PlaceOnTerrain(terrain, parent, "canoe_paddle", shore + new Vector3(1.5f, 0f, 1f), 4f, 20f, ColliderKind.None);
        }

        private static void PlaceOnTerrain(Terrain terrain, GameObject parent, string model, Vector3 pos, float scale, float yaw, ColliderKind collider)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{KenneyDir}/{model}.fbx");
            if (prefab == null) return;
            pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
            GameObject go = Place(prefab, parent, pos, scale, collider);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private static Vector3 FindShore(Terrain terrain, Vector3 from, Vector3 dir)
        {
            Vector3 p = from;
            for (int i = 0; i < 300; i++)
            {
                p += dir;
                if (terrain.SampleHeight(p) + terrain.transform.position.y < WaterLevel + 0.4f) return p - dir * 2f;
            }
            return from + dir * 10f;
        }

        private static List<GameObject> LoadModels(string[] names)
        {
            var list = new List<GameObject>();
            foreach (string n in names)
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>($"{KenneyDir}/{n}.fbx");
                if (go != null) list.Add(go);
            }
            return list;
        }
    }
}
#endif
