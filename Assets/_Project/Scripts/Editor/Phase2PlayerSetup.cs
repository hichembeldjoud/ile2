#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SurvivalIsland.Player;
using SurvivalIsland.Survival;
using SurvivalIsland.UI;

namespace SurvivalIsland.Editor
{
    /// <summary>
    /// Outil d'automatisation Editor pour configurer le joueur Mixamo,
    /// la caméra 3ème personne et l'interface de survie (HUD) de la Phase 2.
    /// </summary>
    public static class Phase2PlayerSetup
    {
        [MenuItem("Survival Island/Phase 2 - Configurer le Joueur et le HUD de Survie", false, 21)]
        public static void SetupPhase2Scene()
        {
            Undo.SetCurrentGroupName("Setup Phase 2 Survival Island");
            int group = Undo.GetCurrentGroup();

            Debug.Log("<color=#4CAF50><b>[Survival Island]</b> Démarrage de la configuration automatique de la Phase 2...</color>");

            // 1. Génération de l'Animator Controller Mixamo
            AnimatorController animController = PlayerAnimatorControllerBuilder.GenerateMixamoAnimatorController();

            // 2. Création ou Récupération du Joueur
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                playerObj = GameObject.Find("Player");
            }

            if (playerObj == null)
            {
                playerObj = new GameObject("Player");
                playerObj.transform.position = new Vector3(0f, 1f, 0f);
                Undo.RegisterCreatedObjectUndo(playerObj, "Create Player");
            }

            // Tag Player indispensable pour les triggers de zones audio et météo
            playerObj.tag = "Player";

            // CharacterController
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = Undo.AddComponent<CharacterController>(playerObj);
            }
            cc.height = 1.8f;
            cc.center = new Vector3(0, 0.9f, 0);

            // SurvivalSystem
            SurvivalSystem survival = playerObj.GetComponent<SurvivalSystem>();
            if (survival == null)
            {
                survival = Undo.AddComponent<SurvivalSystem>(playerObj);
            }

            // PlayerController
            PlayerController controller = playerObj.GetComponent<PlayerController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<PlayerController>(playerObj);
            }

            // Modèle visuel / Placeholder Animator
            Transform visualTransform = playerObj.transform.Find("VisualModel");
            GameObject visualObj;
            if (visualTransform == null)
            {
                // Charge en priorité le modèle V-Bot 2.0 d'Invector (ou fallback capsule)
                GameObject invectorBot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Invector-3rdPersonController_LITE/3D Models/Characters/Invector@V-Bot 2.0/Prefab/VBOT2.0_Custom.prefab");
                if (invectorBot != null)
                {
                    visualObj = Object.Instantiate(invectorBot);
                    visualObj.name = "VisualModel";
                    foreach (var col in visualObj.GetComponentsInChildren<Collider>())
                    {
                        Object.DestroyImmediate(col);
                    }
                }
                else
                {
                    visualObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    visualObj.name = "VisualModel";
                    Collider col = visualObj.GetComponent<Collider>();
                    if (col != null) Object.DestroyImmediate(col);
                }

                visualObj.transform.SetParent(playerObj.transform);
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localRotation = Quaternion.identity;
                Undo.RegisterCreatedObjectUndo(visualObj, "Create VisualModel");
            }
            else
            {
                visualObj = visualTransform.gameObject;
            }

            Animator animator = visualObj.GetComponent<Animator>();
            if (animator == null)
            {
                animator = Undo.AddComponent<Animator>(visualObj);
            }
            animator.runtimeAnimatorController = animController;
            animator.applyRootMotion = false;
            controller.SetAnimator(animator);

            // 3. Configuration de la Caméra 3ème Personne
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                ThirdPersonCamera tpCam = mainCam.GetComponent<ThirdPersonCamera>();
                if (tpCam == null)
                {
                    tpCam = Undo.AddComponent<ThirdPersonCamera>(mainCam.gameObject);
                }
                tpCam.SetTarget(playerObj.transform);
            }

            // 4. Configuration du HUD de Survie (UI)
            SetupSurvivalHUD(survival);

            Undo.CollapseUndoOperations(group);
            Debug.Log("<color=#4CAF50><b>[Survival Island]</b> Configuration Phase 2 achevée ! Joueur Mixamo, Caméra 3ème personne et HUD de Survie connectés.</color>");
        }

        private static void SetupSurvivalHUD(SurvivalSystem survival)
        {
            GameObject hudObj = GameObject.Find("SurvivalHUD_Canvas");
            if (hudObj != null) return;

            // Création du Canvas
            hudObj = new GameObject("SurvivalHUD_Canvas");
            Canvas canvas = hudObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudObj.AddComponent<CanvasScaler>();
            hudObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(hudObj, "Create Survival HUD Canvas");

            // Panneau des jauges en bas à gauche
            GameObject panelObj = new GameObject("VitalsPanel");
            panelObj.transform.SetParent(hudObj.transform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 0);
            panelRect.pivot = new Vector2(0, 0);
            panelRect.anchoredPosition = new Vector2(25, 25);
            panelRect.sizeDelta = new Vector2(220, 140);

            // Création des 4 jauges avec libellés et style moderne
            Slider healthSlider = CreateVitalBar(panelObj.transform, "HealthBar", new Vector2(0, 105), new Color(0.88f, 0.18f, 0.22f), "SANTE");
            Slider hungerSlider = CreateVitalBar(panelObj.transform, "HungerBar", new Vector2(0, 70), new Color(0.95f, 0.58f, 0.15f), "FAIM");
            Slider thirstSlider = CreateVitalBar(panelObj.transform, "ThirstBar", new Vector2(0, 35), new Color(0.18f, 0.72f, 0.95f), "SOIF");
            Slider staminaSlider = CreateVitalBar(panelObj.transform, "StaminaBar", new Vector2(0, 0), new Color(0.22f, 0.85f, 0.35f), "ENDURANCE");

            // Composant SurvivalUI
            SurvivalUI uiComponent = hudObj.AddComponent<SurvivalUI>();

            // Assignation par réflexion sécurisée pour initialiser l'inspecteur
            var healthField = typeof(SurvivalUI).GetField("healthSlider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            healthField?.SetValue(uiComponent, healthSlider);

            var hungerField = typeof(SurvivalUI).GetField("hungerSlider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            hungerField?.SetValue(uiComponent, hungerSlider);

            var thirstField = typeof(SurvivalUI).GetField("thirstSlider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            thirstField?.SetValue(uiComponent, thirstSlider);

            var staminaField = typeof(SurvivalUI).GetField("staminaSlider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            staminaField?.SetValue(uiComponent, staminaSlider);
        }

        private static Slider CreateVitalBar(Transform parent, string barName, Vector2 position, Color fillColor, string labelText = "")
        {
            GameObject barObj = new GameObject(barName);
            barObj.transform.SetParent(parent, false);

            RectTransform rect = barObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(210, 24);

            Slider slider = barObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            // Arrière-plan
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(barObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.82f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Remplissage (Fill Area)
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(barObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(2, 2);
            fillAreaRect.offsetMax = new Vector2(-2, -2);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            slider.fillRect = fillRect;
            slider.targetGraphic = fillImg;

            // Libellé textuel
            if (!string.IsNullOrEmpty(labelText))
            {
                GameObject txtObj = new GameObject("Label");
                txtObj.transform.SetParent(barObj.transform, false);
                TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
                tmp.text = labelText;
                tmp.fontSize = 11;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.color = Color.white;
                RectTransform txtRect = txtObj.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.offsetMin = new Vector2(8, 0);
                txtRect.offsetMax = new Vector2(-8, 0);
            }

            return slider;
        }
    }
}
#endif
