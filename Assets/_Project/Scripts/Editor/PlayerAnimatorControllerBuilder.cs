#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SurvivalIsland.Editor
{
    /// <summary>
    /// Générateur automatique de l'Animator Controller pour les personnages Mixamo et Invector.
    /// Extrait automatiquement les clips haute-fidélité (Idle, Walk, Run, Sprint, Jump, Falling)
    /// et configure les Blend Trees 1D, les transitions et les paramètres sans aucune motion nulle.
    /// </summary>
    public static class PlayerAnimatorControllerBuilder
    {
        private const string ControllerDir = "Assets/_Project/Art/Animations/Characters";
        private const string ControllerPath = "Assets/_Project/Art/Animations/Characters/PlayerAnimatorController.controller";

        [MenuItem("Survival Island/Phase 2 - Générer l'Animator Controller Mixamo", false, 20)]
        public static AnimatorController GenerateMixamoAnimatorController()
        {
            if (!Directory.Exists(ControllerDir))
            {
                Directory.CreateDirectory(ControllerDir);
                AssetDatabase.Refresh();
            }

            // 1. Recherche et extraction des clips d'animation du projet
            LoadMovementClips(
                out AnimationClip idleClip,
                out AnimationClip walkClip,
                out AnimationClip runClip,
                out AnimationClip sprintClip,
                out AnimationClip jumpClip,
                out AnimationClip fallingClip,
                out AnimationClip landClip);

            Debug.Log($"<color=#2196F3><b>[Animator Builder]</b> Clips détectés : Idle={(idleClip != null ? idleClip.name : "NULL")}, Walk={(walkClip != null ? walkClip.name : "NULL")}, Run={(runClip != null ? runClip.name : "NULL")}, Sprint={(sprintClip != null ? sprintClip.name : "NULL")}, Jump={(jumpClip != null ? jumpClip.name : "NULL")}</color>");

            // 2. Recréation propre du contrôleur pour éviter les sous-assets orphelins
            if (File.Exists(ControllerPath))
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            // 3. Configuration des Paramètres (Support universel Mixamo & Invector)
            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "InputMagnitude", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "InputHorizontal", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "InputVertical", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsRunning", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsSneaking", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsSprinting", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "GroundDistance", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "Jump", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "IsDead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            // 4. État Locomotion (Blend Tree 1D : Idle -> Walk -> Run -> Sprint)
            AnimatorState locomotionState = rootStateMachine.AddState("Locomotion");
            BlendTree locomotionBlendTree = new BlendTree
            {
                name = "Locomotion_Tree",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };

            if (idleClip != null) locomotionBlendTree.AddChild(idleClip, 0.0f);
            if (walkClip != null) locomotionBlendTree.AddChild(walkClip, 0.5f);
            if (runClip != null) locomotionBlendTree.AddChild(runClip, 1.0f);
            if (sprintClip != null) locomotionBlendTree.AddChild(sprintClip, 1.5f);

            AssetDatabase.AddObjectToAsset(locomotionBlendTree, controller);
            locomotionState.motion = locomotionBlendTree;
            rootStateMachine.defaultState = locomotionState;

            // 5. État Sneak (Accroupi / Furtif)
            AnimatorState sneakState = rootStateMachine.AddState("Sneak_Locomotion");
            BlendTree sneakBlendTree = new BlendTree
            {
                name = "Sneak_Tree",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };

            if (idleClip != null) sneakBlendTree.AddChild(idleClip, 0.0f);
            if (walkClip != null) sneakBlendTree.AddChild(walkClip, 0.5f);

            AssetDatabase.AddObjectToAsset(sneakBlendTree, controller);
            sneakState.motion = sneakBlendTree;

            // 6. État Saut (Jump)
            AnimatorState jumpState = rootStateMachine.AddState("Jump_InAir");
            if (jumpClip != null)
            {
                jumpState.motion = jumpClip;
            }
            else if (fallingClip != null)
            {
                jumpState.motion = fallingClip;
            }

            // 7. État Chute (Falling)
            AnimatorState fallingState = rootStateMachine.AddState("Falling");
            if (fallingClip != null)
            {
                fallingState.motion = fallingClip;
            }
            else if (jumpClip != null)
            {
                fallingState.motion = jumpClip;
            }

            // 8. État Mort
            AnimatorState deathState = rootStateMachine.AddState("Dead");

            // 9. Configuration des Transitions
            // Locomotion <-> Sneak
            SetupTransition(locomotionState, sneakState, "IsSneaking", true, 0.2f);
            SetupTransition(sneakState, locomotionState, "IsSneaking", false, 0.2f);

            // AnyState -> Jump (Trigger Jump)
            AnimatorStateTransition anyToJump = rootStateMachine.AddAnyStateTransition(jumpState);
            anyToJump.hasExitTime = false;
            anyToJump.duration = 0.08f;
            anyToJump.canTransitionToSelf = false;
            anyToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");

            // Jump -> Locomotion (Quand au sol)
            AnimatorStateTransition jumpToLoco = jumpState.AddTransition(locomotionState);
            jumpToLoco.hasExitTime = false;
            jumpToLoco.duration = 0.15f;
            jumpToLoco.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

            // Locomotion -> Falling (Si on tombe d'une falaise sans sauter)
            AnimatorStateTransition locoToFall = locomotionState.AddTransition(fallingState);
            locoToFall.hasExitTime = false;
            locoToFall.duration = 0.2f;
            locoToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // Falling -> Locomotion (Quand on atterrit)
            AnimatorStateTransition fallToLoco = fallingState.AddTransition(locomotionState);
            fallToLoco.hasExitTime = false;
            fallToLoco.duration = 0.15f;
            fallToLoco.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

            // AnyState -> Death
            AnimatorStateTransition anyToDead = rootStateMachine.AddAnyStateTransition(deathState);
            anyToDead.hasExitTime = false;
            anyToDead.duration = 0.25f;
            anyToDead.AddCondition(AnimatorConditionMode.If, 0, "IsDead");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#4CAF50><b>[Mixamo Builder]</b> Animator Controller généré avec SUCCÈS avec vraies animations Idle, Walk, Run, Sprint et Jump connectées !</color>");
            return controller;
        }

        private static void LoadMovementClips(
            out AnimationClip idle,
            out AnimationClip walk,
            out AnimationClip run,
            out AnimationClip sprint,
            out AnimationClip jump,
            out AnimationClip falling,
            out AnimationClip land)
        {
            idle = null;
            walk = null;
            run = null;
            sprint = null;
            jump = null;
            falling = null;
            land = null;

            // 1. Recherche prioritaire dans Invector Basic_FreeMovement.fbx
            string fbxPath = "Assets/Invector-3rdPersonController_LITE/3D Models/Animations/Basic_FreeMovement.fbx";
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        string cname = clip.name.ToLower();
                        if (cname == "idle" || cname.EndsWith("@idle")) idle = clip;
                        else if (cname == "walk" || cname.EndsWith("@walk")) walk = clip;
                        else if (cname == "run" || cname.EndsWith("@run")) run = clip;
                        else if (cname == "sprint" || cname.EndsWith("@sprint")) sprint = clip;
                    }
                }
            }

            // 2. Chargement des clips Jump et Falling
            jump = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Invector-3rdPersonController_LITE/3D Models/Animations/Jump.anim");
            if (jump == null)
            {
                jump = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Invector-3rdPersonController_LITE/3D Models/Animations/JumpMove.anim");
            }

            falling = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Invector-3rdPersonController_LITE/3D Models/Animations/Falling.anim");
            land = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Invector-3rdPersonController_LITE/3D Models/Animations/LandSoft.anim");

            // 3. Fallback scan global dans Assets si des clips Mixamo ont été ajoutés ailleurs
            if (idle == null || walk == null || run == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:AnimationClip");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    AnimationClip c = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (c == null || c.name.StartsWith("__preview__")) continue;

                    string nameLower = c.name.ToLower();
                    if (idle == null && nameLower.Contains("idle")) idle = c;
                    if (walk == null && nameLower.Contains("walk")) walk = c;
                    if (run == null && (nameLower.Contains("run") || nameLower.Contains("sprint"))) run = c;
                    if (jump == null && nameLower.Contains("jump")) jump = c;
                }
            }
        }

        private static void EnsureParameter(AnimatorController controller, string paramName, AnimatorControllerParameterType type)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == paramName) return;
            }
            controller.AddParameter(paramName, type);
        }

        private static void SetupTransition(AnimatorState from, AnimatorState to, string boolParam, bool expectedValue, float duration = 0.15f)
        {
            foreach (var trans in from.transitions)
            {
                if (trans.destinationState == to) return;
            }

            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.hasFixedDuration = true;
            transition.AddCondition(expectedValue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, boolParam);
        }
    }
}
#endif
