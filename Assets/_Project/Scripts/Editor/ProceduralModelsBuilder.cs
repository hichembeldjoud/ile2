#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SurvivalIsland.Editor
{
    /// <summary>
    /// Constructeur de modèles et matériaux PBR pour l'île tropicale.
    /// Fournit des modèles 3D réalistes (avec chargement prioritaire des assets OBJ haute fidélité)
    /// et matériaux compatibles Universal Render Pipeline (URP).
    /// </summary>
    public static class ProceduralModelsBuilder
    {
        private const string ModelsDir = "Assets/_Project/Art/Models";

        public static Shader GetURPLitShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (s == null) s = Shader.Find("Standard");
            return s;
        }

        public static Material CreateMaterial(string matName, Color color, float smoothness = 0.2f, float metallic = 0.0f)
        {
            Material mat = new Material(GetURPLitShader())
            {
                name = matName,
                color = color
            };

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            return mat;
        }

        public static GameObject CreatePalmTree(Material trunkMat, Material frondMat, Material coconutMat)
        {
            GameObject palmAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/Environment/PalmTree_Realistic.obj");
            if (palmAsset != null)
            {
                GameObject instance = Object.Instantiate(palmAsset);
                instance.name = "PalmTree_Realistic";
                Renderer r = instance.GetComponentInChildren<Renderer>();
                if (r != null) r.sharedMaterial = trunkMat;
                return instance;
            }

            GameObject palm = new GameObject("PalmTree_Procedural");
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(palm.transform);
            trunk.transform.localPosition = new Vector3(0, 4.5f, 0);
            trunk.transform.localScale = new Vector3(0.5f, 4.5f, 0.5f);
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
            return palm;
        }

        public static GameObject CreateCoastalRock(Material rockMat)
        {
            GameObject rockAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/Environment/CoastalRock_Realistic.obj");
            if (rockAsset != null)
            {
                GameObject instance = Object.Instantiate(rockAsset);
                instance.name = "CoastalRock_Realistic";
                Renderer r = instance.GetComponentInChildren<Renderer>();
                if (r != null) r.sharedMaterial = rockMat;
                return instance;
            }

            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "CoastalRock_Procedural";
            rock.transform.localScale = new Vector3(3f, 2f, 2.5f);
            rock.GetComponent<Renderer>().sharedMaterial = rockMat;
            return rock;
        }

        public static GameObject CreateRealisticBoarModel(Material furMat, Material tuskMat, Material snoutMat)
        {
            GameObject boarAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/Animals/WildBoar_Realistic.obj");
            if (boarAsset != null)
            {
                GameObject instance = Object.Instantiate(boarAsset);
                instance.name = "WildBoar_Realistic";
                Renderer r = instance.GetComponentInChildren<Renderer>();
                if (r != null) r.sharedMaterial = furMat;
                return instance;
            }

            GameObject root = new GameObject("WildBoar_Procedural");
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, 0.7f, 0);
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            body.GetComponent<Renderer>().sharedMaterial = furMat;
            return root;
        }

        public static GameObject CreateRealisticPlayerModel(Material skinMat, Material shirtMat, Material pantsMat, Material gearMat)
        {
            GameObject invectorBot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Invector-3rdPersonController_LITE/3D Models/Characters/Invector@V-Bot 2.0/Prefab/VBOT2.0_Custom.prefab");
            if (invectorBot != null)
            {
                GameObject instance = Object.Instantiate(invectorBot);
                instance.name = "Survivor_Invector";
                foreach (var col in instance.GetComponentsInChildren<Collider>())
                {
                    Object.DestroyImmediate(col);
                }
                return instance;
            }

            GameObject survivorAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/Characters/Survivor_Realistic.obj");
            if (survivorAsset != null)
            {
                GameObject instance = Object.Instantiate(survivorAsset);
                instance.name = "Survivor_Realistic";
                Renderer r = instance.GetComponentInChildren<Renderer>();
                if (r != null) r.sharedMaterial = shirtMat;
                return instance;
            }

            GameObject root = new GameObject("Survivor_Procedural");
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0, 1.0f, 0);
            body.GetComponent<Renderer>().sharedMaterial = shirtMat;
            return root;
        }
    }
}
#endif
