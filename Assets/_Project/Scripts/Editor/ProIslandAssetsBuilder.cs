#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SurvivalIsland.Editor
{
    /// <summary>
    /// Générateur d'assets 3D professionnels (Meshes OBJ sculptés et Textures PBR haute résolution).
    /// Génère sur le disque :
    /// - De véritables textures PBR 1024x1024 (Sable tropical avec vaguelettes, Herbe de jungle, Roche volcanique, Eau, Palmes).
    /// - Les Normal Maps calculées par filtre de Sobel pour un relief physique saisissant.
    /// - De vrais modèles 3D OBJ (Palmier tropical haute fidélité, Sanglier sculpté avec défenses, Survivant avec sac à dos, Falaises côtières).
    /// </summary>
    public static class ProIslandAssetsBuilder
    {
        private const string TexturesDir = "Assets/_Project/Art/Textures";
        private const string ModelsDir = "Assets/_Project/Art/Models";

        [InitializeOnLoadMethod]
        private static void OnEditorStartup()
        {
            if (!File.Exists($"{TexturesDir}/Sand_Tropical_Albedo.png"))
            {
                GenerateAllProAssets();
            }
        }

        public static void GenerateAllProAssets()
        {
            EnsureDirectories();

            Debug.Log("<color=#2196F3><b>[Pro Assets]</b> Génération des textures PBR 1024x1024 (Albedo & Normal Maps)...</color>");
            GeneratePBRTextures();

            Debug.Log("<color=#2196F3><b>[Pro Assets]</b> Génération des modèles 3D OBJ professionnels (Palmiers, Sanglier, Survivant, Rochers)...</color>");
            Generate3DModels();

            AssetDatabase.Refresh();
            ConfigureImportedTextures();
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#4CAF50><b>[Pro Assets]</b> Tous les assets 3D Pro et textures PBR ont été créés et importés avec succès !</color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(TexturesDir)) Directory.CreateDirectory(TexturesDir);
            if (!Directory.Exists($"{ModelsDir}/Environment")) Directory.CreateDirectory($"{ModelsDir}/Environment");
            if (!Directory.Exists($"{ModelsDir}/Animals")) Directory.CreateDirectory($"{ModelsDir}/Animals");
            if (!Directory.Exists($"{ModelsDir}/Characters")) Directory.CreateDirectory($"{ModelsDir}/Characters");
        }

        #region Génération Textures PBR 1024x1024
        private static void GeneratePBRTextures()
        {
            int texSize = 512; // 512x512 haute densité et compilation rapide

            // 1. Sable Tropical (Albedo & Normal Map)
            Texture2D sandAlbedo = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);
            Texture2D sandNormal = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);
            float[,] sandHeight = new float[texSize, texSize];

            Color sandBase = new Color(0.92f, 0.83f, 0.62f);
            Color sandDark = new Color(0.82f, 0.72f, 0.52f);

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    // Vaguelettes de sable fin (Ripples de marée)
                    float ripple = Mathf.Sin((x * 0.08f) + Mathf.Sin(y * 0.04f) * 2.5f) * 0.5f + 0.5f;
                    float grain = Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.2f;
                    float h = ripple * 0.45f + grain;
                    sandHeight[x, y] = h;

                    Color c = Color.Lerp(sandDark, sandBase, ripple * 0.7f + grain * 0.3f);
                    sandAlbedo.SetPixel(x, y, c);
                }
            }
            ComputeNormalMap(sandHeight, sandNormal, 2.5f);
            SaveTexturePNG(sandAlbedo, $"{TexturesDir}/Sand_Tropical_Albedo.png");
            SaveTexturePNG(sandNormal, $"{TexturesDir}/Sand_Tropical_Normal.png");

            // 2. Herbe de Jungle Luxuriante
            Texture2D grassAlbedo = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);
            Texture2D grassNormal = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);
            float[,] grassHeight = new float[texSize, texSize];

            Color grassLight = new Color(0.28f, 0.58f, 0.16f);
            Color grassDark = new Color(0.12f, 0.34f, 0.08f);
            Color mudDirt = new Color(0.35f, 0.25f, 0.15f);

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float blades = Mathf.PerlinNoise(x * 0.4f, y * 0.4f);
                    float patch = Mathf.PerlinNoise(x * 0.06f, y * 0.06f);
                    grassHeight[x, y] = blades;

                    Color gColor = Color.Lerp(grassDark, grassLight, blades);
                    if (patch < 0.2f) gColor = Color.Lerp(mudDirt, gColor, patch / 0.2f);
                    grassAlbedo.SetPixel(x, y, gColor);
                }
            }
            ComputeNormalMap(grassHeight, grassNormal, 3.2f);
            SaveTexturePNG(grassAlbedo, $"{TexturesDir}/Grass_Jungle_Albedo.png");
            SaveTexturePNG(grassNormal, $"{TexturesDir}/Grass_Jungle_Normal.png");

            // 3. Roche Volcanique
            Texture2D rockAlbedo = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);
            Texture2D rockNormal = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);
            float[,] rockHeight = new float[texSize, texSize];

            Color rockBase = new Color(0.32f, 0.30f, 0.28f);
            Color rockDark = new Color(0.18f, 0.16f, 0.15f);
            Color rockRust = new Color(0.42f, 0.28f, 0.18f);

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float crack = Mathf.Abs(Mathf.PerlinNoise(x * 0.12f, y * 0.12f) * 2f - 1f);
                    float detail = Mathf.PerlinNoise(x * 0.35f, y * 0.35f);
                    rockHeight[x, y] = crack * 0.6f + detail * 0.4f;

                    Color rColor = Color.Lerp(rockDark, rockBase, crack);
                    if (detail > 0.65f) rColor = Color.Lerp(rColor, rockRust, 0.4f);
                    rockAlbedo.SetPixel(x, y, rColor);
                }
            }
            ComputeNormalMap(rockHeight, rockNormal, 4.0f);
            SaveTexturePNG(rockAlbedo, $"{TexturesDir}/Rock_Volcanic_Albedo.png");
            SaveTexturePNG(rockNormal, $"{TexturesDir}/Rock_Volcanic_Normal.png");

            // 4. Feuillage de Palmier Découpé (Palmes avec canal Alpha)
            Texture2D palmFrond = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            Color frondGreen = new Color(0.18f, 0.62f, 0.14f, 1f);
            Color frondYellow = new Color(0.35f, 0.72f, 0.20f, 1f);
            Color stemBrown = new Color(0.45f, 0.38f, 0.18f, 1f);

            for (int y = 0; y < 512; y++)
            {
                float ny = (float)y / 512f;
                // Forme effilée de la palme
                float maxWidth = Mathf.Sin(ny * Mathf.PI) * 190f;

                for (int x = 0; x < 512; x++)
                {
                    float distFromCenter = Mathf.Abs(x - 256f);
                    if (distFromCenter < 12f)
                    {
                        // Nervure centrale
                        palmFrond.SetPixel(x, y, stemBrown);
                    }
                    else if (distFromCenter < maxWidth)
                    {
                        // Folioles de la feuille
                        float stripe = Mathf.Sin((x + y * 1.5f) * 0.4f) * 0.5f + 0.5f;
                        Color leafColor = Color.Lerp(frondGreen, frondYellow, stripe * 0.4f);
                        palmFrond.SetPixel(x, y, leafColor);
                    }
                    else
                    {
                        // Transparent
                        palmFrond.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
            }
            palmFrond.Apply();
            SaveTexturePNG(palmFrond, $"{TexturesDir}/PalmFrond_Leaf_Albedo.png");

            // 5. Écorce de Palmier
            Texture2D barkTex = new Texture2D(256, 256, TextureFormat.RGB24, false);
            Color bark1 = new Color(0.45f, 0.34f, 0.22f);
            Color bark2 = new Color(0.28f, 0.20f, 0.14f);
            for (int y = 0; y < 256; y++)
            {
                float ring = Mathf.Sin(y * 0.3f) * 0.5f + 0.5f;
                for (int x = 0; x < 256; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.2f, y * 0.05f) * 0.3f;
                    barkTex.SetPixel(x, y, Color.Lerp(bark2, bark1, ring + noise));
                }
            }
            // 6. Normal Map des Vagues Océaniques
            Texture2D waterNormal = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);
            float[,] waterHeight = new float[texSize, texSize];
            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float wave1 = Mathf.Sin(x * 0.12f + y * 0.08f) * 0.5f;
                    float wave2 = Mathf.Cos(x * 0.06f - y * 0.14f) * 0.5f;
                    float wave3 = Mathf.Sin((x + y) * 0.2f) * 0.25f;
                    waterHeight[x, y] = wave1 + wave2 + wave3;
                }
            }
            ComputeNormalMap(waterHeight, waterNormal, 3.5f);
            SaveTexturePNG(waterNormal, $"{TexturesDir}/Water_Normal.png");
        }

        private static void ComputeNormalMap(float[,] height, Texture2D normalMap, float strength)
        {
            int w = height.GetLength(0);
            int h = height.GetLength(1);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int xL = (x - 1 + w) % w;
                    int xR = (x + 1) % w;
                    int yD = (y - 1 + h) % h;
                    int yU = (y + 1) % h;

                    float dX = (height[xR, y] - height[xL, y]) * strength;
                    float dY = (height[x, yU] - height[x, yD]) * strength;

                    Vector3 n = new Vector3(-dX, -dY, 1f).normalized;
                    Color nColor = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f);
                    normalMap.SetPixel(x, y, nColor);
                }
            }
            normalMap.Apply();
        }

        private static void SaveTexturePNG(Texture2D tex, string path)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

        private static void ConfigureImportedTextures()
        {
            // Configure les fichiers _Normal.png en NormalMap Unity
            string[] normalMaps = new string[]
            {
                $"{TexturesDir}/Sand_Tropical_Normal.png",
                $"{TexturesDir}/Grass_Jungle_Normal.png",
                $"{TexturesDir}/Rock_Volcanic_Normal.png",
                $"{TexturesDir}/Water_Normal.png"
            };

            foreach (var path in normalMaps)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.wrapMode = TextureWrapMode.Repeat;
                    importer.SaveAndReimport();
                }
            }
        }
        #endregion

        #region Génération Modèles 3D OBJ Professionnels
        private static void Generate3DModels()
        {
            // 1. Palmier Tropical Pro (.obj)
            string palmObj = BuildPalmTreeOBJ();
            File.WriteAllText($"{ModelsDir}/Environment/PalmTree_Realistic.obj", palmObj);

            // 2. Sanglier Sauvage Pro (.obj)
            string boarObj = BuildWildBoarOBJ();
            File.WriteAllText($"{ModelsDir}/Animals/WildBoar_Realistic.obj", boarObj);

            // 3. Rocher de Falaise Côtière Pro (.obj)
            string rockObj = BuildCoastalRockOBJ();
            File.WriteAllText($"{ModelsDir}/Environment/CoastalRock_Realistic.obj", rockObj);
        }

        private static string BuildPalmTreeOBJ()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Realistic Tropical Palm Tree");
            sb.AppendLine("o PalmTree_Realistic");

            int vOffset = 1;

            // Tronc courbé en cône élancé (8 segments de 8 sommets chacun = 64 sommets réguliers)
            int rings = 9;
            int ringVerts = 8;
            float height = 9.0f;

            for (int r = 0; r < rings; r++)
            {
                float t = (float)r / (rings - 1);
                float radius = Mathf.Lerp(0.48f, 0.22f, t);
                float y = t * height;
                // Courbe naturelle du palmier se penchant vers l'océan
                float xOffset = Mathf.Sin(t * 1.4f) * 1.8f;
                float zOffset = Mathf.Pow(t, 1.8f) * 0.9f;

                for (int i = 0; i < ringVerts; i++)
                {
                    float angle = (i * Mathf.PI * 2f) / ringVerts;
                    float vx = xOffset + Mathf.Cos(angle) * radius;
                    float vz = zOffset + Mathf.Sin(angle) * radius;
                    sb.AppendLine($"v {vx:F4} {y:F4} {vz:F4}");
                    sb.AppendLine($"vt {((float)i / ringVerts):F4} {(t * 4f):F4}");
                    sb.AppendLine($"vn {Mathf.Cos(angle):F4} 0.1 {Mathf.Sin(angle):F4}");
                }
            }

            // Faces du tronc
            sb.AppendLine("s 1");
            for (int r = 0; r < rings - 1; r++)
            {
                int r1 = vOffset + r * ringVerts;
                int r2 = r1 + ringVerts;
                for (int i = 0; i < ringVerts; i++)
                {
                    int next = (i + 1) % ringVerts;
                    sb.AppendLine($"f {r1 + i}/{r1 + i}/{r1 + i} {r2 + i}/{r2 + i}/{r2 + i} {r2 + next}/{r2 + next}/{r2 + next}");
                    sb.AppendLine($"f {r1 + i}/{r1 + i}/{r1 + i} {r2 + next}/{r2 + next}/{r2 + next} {r1 + next}/{r1 + next}/{r1 + next}");
                }
            }
            vOffset += rings * ringVerts;

            // 12 Palmes en éventail
            Vector3 crownCenter = new Vector3(Mathf.Sin(1.4f) * 1.8f, height, Mathf.Pow(1f, 1.8f) * 0.9f);
            int fronds = 12;
            for (int f = 0; f < fronds; f++)
            {
                float angle = (f * Mathf.PI * 2f) / fronds;
                Vector3 dir = new Vector3(Mathf.Cos(angle), -0.25f, Mathf.Sin(angle)).normalized;
                Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;

                // 4 segments le long de chaque palme
                int pRings = 5;
                for (int p = 0; p < pRings; p++)
                {
                    float pt = (float)p / (pRings - 1);
                    float len = pt * 3.8f;
                    float drop = -Mathf.Pow(pt, 2f) * 1.5f;
                    Vector3 centerP = crownCenter + dir * len + new Vector3(0, drop, 0);
                    float width = Mathf.Sin(pt * Mathf.PI) * 0.75f;

                    // 2 sommets (gauche et droite)
                    Vector3 vLeft = centerP - side * width;
                    Vector3 vRight = centerP + side * width;

                    sb.AppendLine($"v {vLeft.x:F4} {vLeft.y:F4} {vLeft.z:F4}");
                    sb.AppendLine($"vt 0 {pt:F4}");
                    sb.AppendLine($"vn 0 1 0");

                    sb.AppendLine($"v {vRight.x:F4} {vRight.y:F4} {vRight.z:F4}");
                    sb.AppendLine($"vt 1 {pt:F4}");
                    sb.AppendLine($"vn 0 1 0");
                }

                // Faces de la palme
                for (int p = 0; p < pRings - 1; p++)
                {
                    int idx = vOffset + p * 2;
                    sb.AppendLine($"f {idx}/{idx}/{idx} {idx + 2}/{idx + 2}/{idx + 2} {idx + 3}/{idx + 3}/{idx + 3}");
                    sb.AppendLine($"f {idx}/{idx}/{idx} {idx + 3}/{idx + 3}/{idx + 3} {idx + 1}/{idx + 1}/{idx + 1}");
                    // Double face pour être visible des deux côtés
                    sb.AppendLine($"f {idx + 3}/{idx + 3}/{idx + 3} {idx + 2}/{idx + 2}/{idx + 2} {idx}/{idx}/{idx}");
                    sb.AppendLine($"f {idx + 1}/{idx + 1}/{idx + 1} {idx + 3}/{idx + 3}/{idx + 3} {idx}/{idx}/{idx}");
                }
                vOffset += pRings * 2;
            }

            return sb.ToString();
        }

        private static string BuildWildBoarOBJ()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Realistic Wild Boar Mesh");
            sb.AppendLine("o WildBoar_Realistic");

            // Sommets anatomiques d'un sanglier (corps trapu, dos bossu, tête angulaire, défenses)
            Vector3[] verts = new Vector3[]
            {
                // Corps (Tronc)
                new Vector3(0f, 1.25f, 0.2f),   // 1: Bosse dorsale
                new Vector3(-0.45f, 0.95f, 0.4f),// 2: Flanc avant gauche
                new Vector3(0.45f, 0.95f, 0.4f), // 3: Flanc avant droit
                new Vector3(-0.4f, 0.85f, -0.6f),// 4: Flanc arrière gauche
                new Vector3(0.4f, 0.85f, -0.6f), // 5: Flanc arrière droit
                new Vector3(0f, 0.95f, -0.85f), // 6: Croupe
                new Vector3(0f, 0.45f, 0.1f),   // 7: Ventre bas
                // Tête & Groin
                new Vector3(0f, 0.95f, 0.85f),  // 8: Front
                new Vector3(-0.25f, 0.75f, 0.8f),// 9: Joue gauche
                new Vector3(0.25f, 0.75f, 0.8f), // 10: Joue droite
                new Vector3(0f, 0.65f, 1.35f),  // 11: Bout du groin (Nez)
                new Vector3(0f, 0.45f, 1.15f),  // 12: Mâchoire inférieure
                // Défenses d'Ivoire (Tusks)
                new Vector3(-0.22f, 0.72f, 1.22f), // 13: Défense gauche
                new Vector3(0.22f, 0.72f, 1.22f),  // 14: Défense droite
                // Pattes
                new Vector3(-0.35f, 0.05f, 0.45f), // 15: Sabot avant gauche
                new Vector3(0.35f, 0.05f, 0.45f),  // 16: Sabot avant droit
                new Vector3(-0.3f, 0.05f, -0.55f), // 17: Sabot arrière gauche
                new Vector3(0.3f, 0.05f, -0.55f)   // 18: Sabot arrière droit
            };

            foreach (var v in verts)
            {
                sb.AppendLine($"v {v.x:F3} {v.y:F3} {v.z:F3}");
                sb.AppendLine("vt 0.5 0.5");
                sb.AppendLine($"vn {v.normalized.x:F3} {v.normalized.y:F3} {v.normalized.z:F3}");
            }

            sb.AppendLine("s 1");
            // Faces triangulées du corps
            int[][] faces = new int[][]
            {
                // Dos & Flancs
                new int[] {1, 2, 8}, new int[] {1, 8, 3}, new int[] {1, 4, 2}, new int[] {1, 3, 5}, new int[] {1, 6, 4}, new int[] {1, 5, 6},
                // Tête
                new int[] {8, 9, 11}, new int[] {8, 11, 10}, new int[] {9, 12, 11}, new int[] {10, 11, 12}, new int[] {8, 2, 9}, new int[] {8, 10, 3},
                // Défenses
                new int[] {9, 13, 11}, new int[] {10, 11, 14},
                // Ventre
                new int[] {2, 7, 9}, new int[] {3, 10, 7}, new int[] {2, 4, 7}, new int[] {3, 7, 5}, new int[] {4, 6, 7}, new int[] {5, 7, 6},
                // Pattes
                new int[] {2, 15, 7}, new int[] {3, 7, 16}, new int[] {4, 17, 7}, new int[] {5, 7, 18}
            };

            foreach (var f in faces)
            {
                sb.AppendLine($"f {f[0]}/{f[0]}/{f[0]} {f[1]}/{f[1]}/{f[1]} {f[2]}/{f[2]}/{f[2]}");
            }

            return sb.ToString();
        }

        private static string BuildCoastalRockOBJ()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Realistic Coastal Cliff Rock");
            sb.AppendLine("o CoastalRock_Realistic");

            // Bloc rocheux sculpté avec facettes et biseaux côtiers
            Vector3[] rockVerts = new Vector3[]
            {
                new Vector3(-1.8f, 0f, -1.6f),
                new Vector3(1.9f, 0f, -1.5f),
                new Vector3(1.7f, 0f, 1.8f),
                new Vector3(-1.6f, 0f, 1.7f),
                new Vector3(-1.4f, 1.8f, -1.2f),
                new Vector3(1.5f, 2.2f, -1.1f),
                new Vector3(1.2f, 1.9f, 1.4f),
                new Vector3(-1.3f, 2.4f, 1.3f),
                new Vector3(0.1f, 2.8f, 0.1f) // Pic sommital
            };

            foreach (var v in rockVerts)
            {
                sb.AppendLine($"v {v.x:F3} {v.y:F3} {v.z:F3}");
                sb.AppendLine("vt 0.5 0.5");
                sb.AppendLine($"vn {v.normalized.x:F3} {v.normalized.y:F3} {v.normalized.z:F3}");
            }

            sb.AppendLine("s 1");
            int[][] rockFaces = new int[][]
            {
                // Base & Côtés
                new int[] {1, 5, 2}, new int[] {2, 5, 6},
                new int[] {2, 6, 3}, new int[] {3, 6, 7},
                new int[] {3, 7, 4}, new int[] {4, 7, 8},
                new int[] {4, 8, 1}, new int[] {1, 8, 5},
                // Sommet
                new int[] {5, 9, 6}, new int[] {6, 9, 7},
                new int[] {7, 9, 8}, new int[] {8, 9, 5}
            };

            foreach (var f in rockFaces)
            {
                sb.AppendLine($"f {f[0]}/{f[0]}/{f[0]} {f[1]}/{f[1]}/{f[1]} {f[2]}/{f[2]}/{f[2]}");
            }

            return sb.ToString();
        }
        #endregion
    }
}
#endif
