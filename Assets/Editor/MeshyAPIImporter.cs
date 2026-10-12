#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Text;

public class MeshyAPIImporter : EditorWindow
{
    private string apiKey = ""; // Collez votre clé API Meshy ici
    private string prompt = "A wooden pirate treasure chest, low poly game art";
    private string statusMessage = "Prêt.";
    private bool isGenerating = false;

    [MenuItem("Outils IA/Générateur Meshy AI")]
    public static void ShowWindow()
    {
        GetWindow<MeshyAPIImporter>("Générateur Meshy");
    }

    private void OnGUI()
    {
        GUILayout.Label("Paramètres de l'API Meshy (Text-to-3D)", EditorStyles.boldLabel);
        apiKey = EditorGUILayout.TextField("Clé API", apiKey);

        EditorGUILayout.Space();
        GUILayout.Label("Description du modèle 3D (Prompt)", EditorStyles.boldLabel);
        prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(60));

        EditorGUILayout.Space();

        GUI.enabled = !isGenerating;
        if (GUILayout.Button("Générer et Télécharger", GUILayout.Height(40)))
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                statusMessage = "Erreur : Clé API manquante.";
                return;
            }
            GenerateAndDownloadModel();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();
        GUILayout.Label($"Statut : {statusMessage}", EditorStyles.wordWrappedLabel);
    }

    private async void GenerateAndDownloadModel()
    {
        isGenerating = true;
        statusMessage = "1/4 Démarrage de la tâche (Envoi du prompt)...";
        Repaint();

        try
        {
            // Étape 1 : Demander la génération et obtenir l'ID de la tâche
            string taskId = await StartGenerationTask();
            
            if (string.IsNullOrEmpty(taskId)) 
                throw new Exception("Aucun ID de tâche retourné.");

            // Étape 2 : Vérifier le statut toutes les 5 secondes jusqu'à ce qu'il soit fini
            statusMessage = $"2/4 Génération en cours sur les serveurs (Task: {taskId})...";
            Repaint();

            string modelUrl = await PollTaskStatus(taskId);

            // Étape 3 : Télécharger le modèle 3D
            statusMessage = "3/4 Génération réussie ! Téléchargement du fichier .glb...";
            Repaint();
            
            await DownloadModel(modelUrl);

            statusMessage = "4/4 Terminé ! Le modèle a été importé dans le dossier Assets/MeshyModels.";
        }
        catch (Exception e)
        {
            statusMessage = $"Erreur : {e.Message}";
            Debug.LogError("[Meshy Importer] " + e.Message);
        }
        finally
        {
            isGenerating = false;
            Repaint();
        }
    }

    // --- REQUÊTE POST : DÉMARRER LA TÂCHE ---
    private async Task<string> StartGenerationTask()
    {
        MeshyPostRequest requestData = new MeshyPostRequest 
        { 
            mode = "preview", 
            prompt = prompt, 
            art_style = "realistic",
            should_remesh = true
        };
        
        string json = JsonUtility.ToJson(requestData);

        using (UnityWebRequest req = new UnityWebRequest("https://api.meshy.ai/v2/text-to-3d", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            var operation = req.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"Erreur de requête API : {req.error}\nDétails : {req.downloadHandler.text}");
            }

            MeshyPostResponse response = JsonUtility.FromJson<MeshyPostResponse>(req.downloadHandler.text);
            return response.result;
        }
    }

    // --- REQUÊTE GET : BOUCLE DE VÉRIFICATION DU STATUT ---
    private async Task<string> PollTaskStatus(string taskId)
    {
        while (true)
        {
            await Task.Delay(5000); // Pause de 5 secondes pour ne pas spammer l'API

            using (UnityWebRequest req = UnityWebRequest.Get($"https://api.meshy.ai/v2/text-to-3d/{taskId}"))
            {
                req.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                
                var operation = req.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (req.result != UnityWebRequest.Result.Success)
                    throw new Exception($"Erreur de vérification du statut : {req.error}");

                MeshyTaskResponse response = JsonUtility.FromJson<MeshyTaskResponse>(req.downloadHandler.text);

                if (response.status == "SUCCEEDED")
                {
                    if (response.model_urls != null && !string.IsNullOrEmpty(response.model_urls.glb))
                        return response.model_urls.glb;
                    else
                        throw new Exception("Statut SUCCEEDED, mais aucune URL .glb n'a été trouvée dans la réponse.");
                }
                else if (response.status == "FAILED" || response.status == "EXPIRED")
                {
                    throw new Exception($"La génération a échoué du côté de l'IA (Statut: {response.status}).");
                }
                else
                {
                    // Met à jour la barre de progression (IN_PROGRESS)
                    statusMessage = $"2/4 Génération en cours... ({response.progress}%)";
                    Repaint();
                }
            }
        }
    }

    // --- TÉLÉCHARGEMENT DU FICHIER ET SAUVEGARDE SUR LE DISQUE ---
    private async Task DownloadModel(string url)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            var operation = req.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"Erreur lors du téléchargement du fichier 3D : {req.error}");

            string saveDirectory = Path.Combine(Application.dataPath, "MeshyModels");
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            string fileName = $"Model_IA_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.glb";
            string savePath = Path.Combine(saveDirectory, fileName);

            File.WriteAllBytes(savePath, req.downloadHandler.data);
            
            // Rafraîchir l'explorateur de projet Unity pour faire apparaître le fichier
            AssetDatabase.Refresh();
        }
    }
}

// --- CLASSES DE DONNÉES POUR LE PARSING JSON NATIF D'UNITY ---
[Serializable]
public class MeshyPostRequest 
{
    public string mode;
    public string prompt;
    public string art_style;
    public bool should_remesh;
}

[Serializable]
public class MeshyPostResponse 
{
    public string result; // Contient le Task ID
}

[Serializable]
public class MeshyTaskResponse 
{
    public string id;
    public string status; // PENDING, IN_PROGRESS, SUCCEEDED, FAILED
    public int progress;
    public MeshyModelUrls model_urls;
}

[Serializable]
public class MeshyModelUrls 
{
    public string glb; // URL de téléchargement du modèle
}
#endif