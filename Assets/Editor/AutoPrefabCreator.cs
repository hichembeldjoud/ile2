#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class AutoPrefabCreator : EditorWindow
{
    // Ajoute un nouveau menu dans la barre supérieure d'Unity
    [MenuItem("Outils IA/Créer Prefabs depuis Modèles 3D")]
    public static void ShowWindow()
    {
        GetWindow<AutoPrefabCreator>("Auto Prefab");
    }

    private void OnGUI()
    {
        GUILayout.Label("Automatisation de l'importation", EditorStyles.boldLabel);
        GUILayout.Label("1. Sélectionne un dossier dans l'onglet Project contenant tes .fbx ou .obj\n2. Clique sur le bouton ci-dessous.", EditorStyles.wordWrappedLabel);
        
        EditorGUILayout.Space();

        if (GUILayout.Button("Générer les Prefabs avec Colliders", GUILayout.Height(40)))
        {
            ProcessSelectedFolder();
        }
    }

    private void ProcessSelectedFolder()
    {
        string[] guids = Selection.assetGUIDs;
        if (guids.Length == 0)
        {
            Debug.LogWarning("Veuillez d'abord sélectionner un dossier dans la fenêtre Project.");
            return;
        }

        string folderPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning("La sélection n'est pas un dossier valide.");
            return;
        }

        // Récupère tous les fichiers du dossier
        string[] filePaths = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
        int count = 0;

        foreach (string path in filePaths)
        {
            if (path.EndsWith(".fbx") || path.EndsWith(".obj"))
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model != null)
                {
                    // Instancie le modèle en mémoire
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    
                    // Automatisation : Ajout d'un MeshCollider si absent
                    if (instance.GetComponent<Collider>() == null)
                    {
                        MeshCollider mc = instance.AddComponent<MeshCollider>();
                        mc.convex = true; // Utile si l'objet doit avoir un Rigidbody plus tard
                    }
                    
                    // Définition du chemin de sauvegarde pour le nouveau Prefab
                    string prefabPath = path.Replace(".fbx", ".prefab").Replace(".obj", ".prefab");
                    
                    // Sauvegarde et nettoyage
                    PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                    DestroyImmediate(instance);
                    count++;
                }
            }
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"Terminé : {count} Prefabs générés et configurés automatiquement.");
    }
}
#endif