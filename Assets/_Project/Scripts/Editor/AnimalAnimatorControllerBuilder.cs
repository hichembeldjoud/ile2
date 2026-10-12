#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SurvivalIsland.Editor
{
    /// <summary>
    /// Générateur de l'Animator Controller pour les animaux Mixamo (Sanglier, faune sauvage).
    /// Configure la locomotion Blend Tree (Idle, Walk, Run/Flee) et l'animation de mort.
    /// </summary>
    public static class AnimalAnimatorControllerBuilder
    {
        private const string ControllerPath = "Assets/_Project/Art/Animations/Animals/AnimalAnimatorController.controller";

        [MenuItem("Survival Island/Phase 3 - Générer l'Animator Controller Animaux", false, 30)]
        public static AnimatorController GenerateAnimalAnimatorController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
                Debug.Log($"<color=#4CAF50><b>[Animal Builder]</b> Nouveau Animator Controller Animal créé à : {ControllerPath}</color>");
            }

            // Paramètres
            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsFleeing", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "IsDead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            // 1. État Locomotion Animale
            AnimatorState locomotionState = FindOrCreateState(rootStateMachine, "Locomotion");
            BlendTree tree = new BlendTree
            {
                name = "Animal_Locomotion_Tree",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            tree.AddChild(null, 0.0f); // Idle
            tree.AddChild(null, 0.5f); // Walk
            tree.AddChild(null, 1.0f); // Run (Fuite)
            AssetDatabase.AddObjectToAsset(tree, controller);

            locomotionState.motion = tree;
            rootStateMachine.defaultState = locomotionState;

            // 2. État Mort
            AnimatorState deathState = FindOrCreateState(rootStateMachine, "Death");

            // Transition AnyState -> Death
            AnimatorStateTransition anyToDead = rootStateMachine.AddAnyStateTransition(deathState);
            anyToDead.hasExitTime = false;
            anyToDead.duration = 0.2f;
            anyToDead.AddCondition(AnimatorConditionMode.If, 0, "Die");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#4CAF50><b>[Animal Builder]</b> Animator Controller Animaux configuré avec succès !</color>");
            return controller;
        }

        private static void EnsureParameter(AnimatorController controller, string paramName, AnimatorControllerParameterType type)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == paramName) return;
            }
            controller.AddParameter(paramName, type);
        }

        private static AnimatorState FindOrCreateState(AnimatorStateMachine sm, string stateName)
        {
            foreach (var childState in sm.states)
            {
                if (childState.state.name == stateName)
                {
                    return childState.state;
                }
            }
            return sm.AddState(stateName);
        }
    }
}
#endif
