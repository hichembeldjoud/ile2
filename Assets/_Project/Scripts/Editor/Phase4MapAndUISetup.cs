#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SurvivalIsland.UI;
using SurvivalIsland.Quests;

namespace SurvivalIsland.Editor
{
    /// <summary>
    /// Outil d'automatisation Editor pour configurer la Minimap orthographique,
    /// le système de quêtes et le HUD complet de la Phase 4.
    /// </summary>
    public static class Phase4MapAndUISetup
    {
        [MenuItem("Survival Island/Phase 4 - Configurer la Minimap, Quêtes et HUD Complet", false, 40)]
        public static void SetupPhase4Scene()
        {
            Undo.SetCurrentGroupName("Setup Phase 4 Survival Island");
            int group = Undo.GetCurrentGroup();

            Debug.Log("<color=#4CAF50><b>[Survival Island]</b> Démarrage de la configuration automatique de la Phase 4...</color>");

            // 1. Système de Quêtes
            GameObject questRoot = GameObject.Find("--- QUEST SYSTEM ---");
            if (questRoot == null)
            {
                questRoot = new GameObject("--- QUEST SYSTEM ---");
                Undo.RegisterCreatedObjectUndo(questRoot, "Create Quest Root");
            }
            QuestManager questManager = questRoot.GetComponent<QuestManager>();
            if (questManager == null)
            {
                questManager = Undo.AddComponent<QuestManager>(questRoot);
            }

            // 2. Contrôleur et Caméra de la Minimap
            GameObject minimapObj = GameObject.Find("--- MINIMAP SYSTEM ---");
            if (minimapObj == null)
            {
                minimapObj = new GameObject("--- MINIMAP SYSTEM ---");
                Undo.RegisterCreatedObjectUndo(minimapObj, "Create Minimap System");
            }
            Minimap minimap = minimapObj.GetComponent<Minimap>();
            if (minimap == null)
            {
                minimap = Undo.AddComponent<Minimap>(minimapObj);
            }

            // 3. Canvas HUD Complet
            GameObject canvasObj = GameObject.Find("SurvivalHUD_Canvas");
            Canvas canvas;
            if (canvasObj == null)
            {
                canvasObj = new GameObject("SurvivalHUD_Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create HUD Canvas");
            }
            else
            {
                canvas = canvasObj.GetComponent<Canvas>();
            }

            // 4. Widget Minimap (En haut à droite)
            RawImage minimapRawImg = SetupMinimapUI(canvasObj.transform);
            minimap.SetRawImage(minimapRawImg);

            // 5. Widget Quêtes (En haut à gauche)
            (TextMeshProUGUI qTitle, TextMeshProUGUI qDesc, TextMeshProUGUI qProg) = SetupQuestUI(canvasObj.transform);

            // 6. Widget Environnement & Horloge (En haut au centre)
            (TextMeshProUGUI clockText, TextMeshProUGUI weatherText) = SetupEnvironmentUI(canvasObj.transform);

            // 7. Panneau de Mort (Game Over)
            GameObject deathPanel = SetupDeathUI(canvasObj.transform);

            // 8. UIManager central
            UIManager uiManager = canvasObj.GetComponent<UIManager>();
            if (uiManager == null)
            {
                uiManager = Undo.AddComponent<UIManager>(canvasObj);
            }

            // Récupère les sliders existants
            Slider healthSlider = FindSlider(canvasObj.transform, "HealthBar");
            Slider hungerSlider = FindSlider(canvasObj.transform, "HungerBar");
            Slider thirstSlider = FindSlider(canvasObj.transform, "ThirstBar");
            Slider staminaSlider = FindSlider(canvasObj.transform, "StaminaBar");

            uiManager.AssignReferences(
                healthSlider, hungerSlider, thirstSlider, staminaSlider,
                minimapRawImg, minimap,
                qTitle, qDesc, qProg,
                clockText, weatherText,
                deathPanel
            );

            Undo.CollapseUndoOperations(group);
            Debug.Log("<color=#4CAF50><b>[Survival Island]</b> Configuration Phase 4 terminée avec succès ! Minimap, Quêtes et HUD complets opérationnels.</color>");
        }

        private static RawImage SetupMinimapUI(Transform parent)
        {
            Transform existing = parent.Find("MinimapWidget");
            if (existing != null) return existing.GetComponentInChildren<RawImage>();

            GameObject widget = new GameObject("MinimapWidget");
            widget.transform.SetParent(parent, false);
            RectTransform rect = widget.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-25, -25);
            rect.sizeDelta = new Vector2(180, 180);

            // Fond / Bordure
            GameObject border = new GameObject("Border");
            border.transform.SetParent(widget.transform, false);
            Image borderImg = border.AddComponent<Image>();
            borderImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            RectTransform borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = Vector2.zero;

            // RawImage de la carte
            GameObject map = new GameObject("MapDisplay");
            map.transform.SetParent(border.transform, false);
            RawImage rawImg = map.AddComponent<RawImage>();
            RectTransform mapRect = map.GetComponent<RectTransform>();
            mapRect.anchorMin = Vector2.zero;
            mapRect.anchorMax = Vector2.one;
            mapRect.offsetMin = new Vector2(4, 4);
            mapRect.offsetMax = new Vector2(-4, -4);

            // Indicateur Joueur (flèche centrale)
            GameObject playerIcon = new GameObject("PlayerIcon");
            playerIcon.transform.SetParent(map.transform, false);
            Image iconImg = playerIcon.AddComponent<Image>();
            iconImg.color = new Color(1f, 0.9f, 0.2f, 1f); // Jaune vif
            RectTransform iconRect = playerIcon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(12, 12);

            return rawImg;
        }

        private static (TextMeshProUGUI, TextMeshProUGUI, TextMeshProUGUI) SetupQuestUI(Transform parent)
        {
            Transform existing = parent.Find("QuestWidget");
            if (existing != null)
            {
                return (
                    existing.Find("QuestTitle")?.GetComponent<TextMeshProUGUI>(),
                    existing.Find("QuestDescription")?.GetComponent<TextMeshProUGUI>(),
                    existing.Find("QuestProgress")?.GetComponent<TextMeshProUGUI>()
                );
            }

            GameObject widget = new GameObject("QuestWidget");
            widget.transform.SetParent(parent, false);
            RectTransform rect = widget.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(25, -25);
            rect.sizeDelta = new Vector2(300, 110);

            // Titre de Quête
            GameObject titleObj = new GameObject("QuestTitle");
            titleObj.transform.SetParent(widget.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(1f, 0.85f, 0.2f);
            titleText.text = "OBJECTIF : Trouver de la nourriture";
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0, 1);
            titleRect.anchoredPosition = new Vector2(0, 0);
            titleRect.sizeDelta = new Vector2(0, 28);

            // Description
            GameObject descObj = new GameObject("QuestDescription");
            descObj.transform.SetParent(widget.transform, false);
            TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.fontSize = 14;
            descText.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            descText.text = "Mangez de la viande de gibier pour restaurer votre faim.";
            RectTransform descRect = descObj.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 1);
            descRect.anchorMax = new Vector2(1, 1);
            descRect.pivot = new Vector2(0, 1);
            descRect.anchoredPosition = new Vector2(0, -32);
            descRect.sizeDelta = new Vector2(0, 40);

            // Progression
            GameObject progObj = new GameObject("QuestProgress");
            progObj.transform.SetParent(widget.transform, false);
            TextMeshProUGUI progText = progObj.AddComponent<TextMeshProUGUI>();
            progText.fontSize = 16;
            progText.fontStyle = FontStyles.Bold;
            progText.color = new Color(0.4f, 0.9f, 0.4f);
            progText.text = "Objectif : 0 / 1";
            RectTransform progRect = progObj.GetComponent<RectTransform>();
            progRect.anchorMin = new Vector2(0, 1);
            progRect.anchorMax = new Vector2(1, 1);
            progRect.pivot = new Vector2(0, 1);
            progRect.anchoredPosition = new Vector2(0, -78);
            progRect.sizeDelta = new Vector2(0, 26);

            return (titleText, descText, progText);
        }

        private static (TextMeshProUGUI, TextMeshProUGUI) SetupEnvironmentUI(Transform parent)
        {
            Transform existing = parent.Find("EnvironmentWidget");
            if (existing != null)
            {
                return (
                    existing.Find("ClockText")?.GetComponent<TextMeshProUGUI>(),
                    existing.Find("WeatherText")?.GetComponent<TextMeshProUGUI>()
                );
            }

            GameObject widget = new GameObject("EnvironmentWidget");
            widget.transform.SetParent(parent, false);
            RectTransform rect = widget.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -15);
            rect.sizeDelta = new Vector2(250, 45);

            // Horloge
            GameObject clockObj = new GameObject("ClockText");
            clockObj.transform.SetParent(widget.transform, false);
            TextMeshProUGUI clockText = clockObj.AddComponent<TextMeshProUGUI>();
            clockText.fontSize = 18;
            clockText.alignment = TextAlignmentOptions.Center;
            clockText.color = Color.white;
            clockText.text = "08:00";
            RectTransform clockRect = clockObj.GetComponent<RectTransform>();
            clockRect.anchorMin = new Vector2(0, 0.5f);
            clockRect.anchorMax = new Vector2(1, 1);
            clockRect.sizeDelta = Vector2.zero;

            // Météo
            GameObject weatherObj = new GameObject("WeatherText");
            weatherObj.transform.SetParent(widget.transform, false);
            TextMeshProUGUI weatherText = weatherObj.AddComponent<TextMeshProUGUI>();
            weatherText.fontSize = 14;
            weatherText.alignment = TextAlignmentOptions.Center;
            weatherText.color = new Color(0.8f, 0.85f, 0.9f);
            weatherText.text = "Ciel Degage";
            RectTransform weatherRect = weatherObj.GetComponent<RectTransform>();
            weatherRect.anchorMin = new Vector2(0, 0);
            weatherRect.anchorMax = new Vector2(1, 0.5f);
            weatherRect.sizeDelta = Vector2.zero;

            return (clockText, weatherText);
        }

        private static GameObject SetupDeathUI(Transform parent)
        {
            Transform existing = parent.Find("DeathPanel");
            if (existing != null) return existing.gameObject;

            GameObject panel = new GameObject("DeathPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image img = panel.AddComponent<Image>();
            img.color = new Color(0.5f, 0.05f, 0.05f, 0.75f); // Voile rouge sanglant

            GameObject textObj = new GameObject("GameOverText");
            textObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "VOUS AVEZ SUCCOMBÉ\n<size=22><color=#DDD>Cliquez pour réapparaître</color></size>";
            tmp.fontSize = 42;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.3f);
            textRect.anchorMax = new Vector2(0.9f, 0.7f);
            textRect.sizeDelta = Vector2.zero;

            Button btn = panel.AddComponent<Button>();

            panel.SetActive(false);
            return panel;
        }

        private static Slider FindSlider(Transform parent, string name)
        {
            Transform t = parent.Find($"VitalsPanel/{name}");
            return t != null ? t.GetComponent<Slider>() : null;
        }
    }
}
#endif
