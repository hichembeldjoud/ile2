using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using SurvivalIsland.Survival;
using SurvivalIsland.Audio;

namespace SurvivalIsland.Player
{
    /// <summary>
    /// Contrôleur de personnage à la 3ème personne complet et optimisé pour les animations Mixamo.
    /// Gère les états Idle, Walk, Run (Sprint), Sneak (Accroupi), le Saut,
    /// la gravité, la rotation relative à la caméra, la stamina et les bruits de pas.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival Island/Player/Player Controller")]
    public class PlayerController : MonoBehaviour
    {
        #region Paramètres de Mouvement
        [Header("--- Vitesses de Déplacement ---")]
        [Tooltip("Vitesse de marche standard en m/s.")]
        [SerializeField]
        private float walkSpeed = 3.0f;

        [Tooltip("Vitesse de course / sprint en m/s.")]
        [SerializeField]
        private float runSpeed = 6.5f;

        [Tooltip("Vitesse en mode furtif (accroupi) en m/s.")]
        [SerializeField]
        private float sneakSpeed = 1.6f;

        [Tooltip("Vitesse de rotation fluide du joueur vers la direction visée.")]
        [SerializeField]
        private float rotationSmoothTime = 0.12f;
        #endregion

        #region Saut & Gravité
        [Header("--- Saut & Gravité ---")]
        [Tooltip("Hauteur du saut en mètres.")]
        [SerializeField]
        private float jumpHeight = 1.35f;

        [Tooltip("Force de gravité appliquée.")]
        [SerializeField]
        private float gravity = -19.62f;

        [Tooltip("Rayon du test au sol supplémentaire.")]
        [SerializeField]
        private float groundCheckRadius = 0.28f;

        [Tooltip("Calque de collision du sol.")]
        [SerializeField]
        private LayerMask groundLayer = ~0;
        #endregion

        #region Mode Furtif (Sneak)
        [Header("--- Mode Sneak / Accroupi ---")]
        [Tooltip("Hauteur du CharacterController debout.")]
        [SerializeField]
        private float standingHeight = 1.8f;

        [Tooltip("Hauteur du CharacterController accroupi.")]
        [SerializeField]
        private float crouchingHeight = 1.15f;

        [Tooltip("Centre du CharacterController debout.")]
        [SerializeField]
        private Vector3 standingCenter = new Vector3(0, 0.9f, 0);

        [Tooltip("Centre du CharacterController accroupi.")]
        [SerializeField]
        private Vector3 crouchingCenter = new Vector3(0, 0.58f, 0);
        #endregion

        #region Animation Mixamo
        [Header("--- Animation Mixamo ---")]
        [Tooltip("Animator du personnage (avatar Humanoid Mixamo).")]
        [SerializeField]
        private Animator animator;

        [Tooltip("Vitesse d'amortissement de la transition des paramètres de l'Animator.")]
        [SerializeField]
        private float animatorDampTime = 0.15f;

        // Hashs des paramètres de l'Animator pour des performances optimales (Mecanim standard & Mixamo)
        private static readonly int AnimSpeedHash = Animator.StringToHash("Speed");
        private static readonly int AnimIsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int AnimIsRunningHash = Animator.StringToHash("IsRunning");
        private static readonly int AnimIsSneakingHash = Animator.StringToHash("IsSneaking");
        private static readonly int AnimIsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int AnimJumpTriggerHash = Animator.StringToHash("Jump");
        private static readonly int AnimIsDeadHash = Animator.StringToHash("IsDead");

        // Paramètres Invector Locomotion & BlendTrees avancés
        private static readonly int AnimInputMagnitudeHash = Animator.StringToHash("InputMagnitude");
        private static readonly int AnimInputVerticalHash = Animator.StringToHash("InputVertical");
        private static readonly int AnimInputHorizontalHash = Animator.StringToHash("InputHorizontal");
        private static readonly int AnimIsSprintingHash = Animator.StringToHash("IsSprinting");
        private static readonly int AnimGroundDistanceHash = Animator.StringToHash("GroundDistance");
        private static readonly int AnimVerticalVelocityHash = Animator.StringToHash("VerticalVelocity");

        private readonly HashSet<int> validAnimParams = new HashSet<int>();
        #endregion

        #region Audio & Bruits de Pas (Footsteps)
        [Header("--- Bruits de Pas (Footsteps) ---")]
        [Tooltip("Clips audio de bruits de pas.")]
        [SerializeField]
        private AudioClip[] footstepClips;

        [Tooltip("Intervalle de temps entre les pas en marchant (secondes).")]
        [SerializeField]
        private float walkStepInterval = 0.52f;

        [Tooltip("Intervalle de temps entre les pas en courant (secondes).")]
        [SerializeField]
        private float runStepInterval = 0.32f;

        [Tooltip("Intervalle de temps entre les pas en mode sneak (secondes).")]
        [SerializeField]
        private float sneakStepInterval = 0.72f;
        #endregion

        #region Références & Variables Internes
        private CharacterController characterController;
        private Transform cameraTransform;
        private SurvivalSystem survivalSystem;

        private Vector3 verticalVelocity;
        private float rotationVelocity;
        private float footstepTimer = 0f;

        private bool isGrounded = true;
        private bool isRunning = false;
        private bool isSneaking = false;
        private bool isMoving = false;
        private float currentTargetSpeed = 0f;
        private float currentAnimSpeedValue = 0f;
        #endregion

        #region Propriétés Publiques
        public bool IsRunning => isRunning;
        public bool IsSneaking => isSneaking;
        public bool IsGrounded => isGrounded;
        public bool IsMoving => isMoving;
        public float CurrentSpeed => currentTargetSpeed;
        #endregion

        #region Cycle de Vie Unity
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                animator.applyRootMotion = false;
                CacheAnimatorParameters();
            }

            // S'assure que le système de combat et chasse est présent
            if (GetComponent<SurvivalIsland.Combat.WeaponSystem>() == null)
            {
                gameObject.AddComponent<SurvivalIsland.Combat.WeaponSystem>();
            }

            // Réglages par défaut optimaux du CharacterController
            characterController.height = standingHeight;
            characterController.center = standingCenter;
            characterController.stepOffset = 0.35f;
            characterController.slopeLimit = 45f;
        }

        private void Start()
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (animator != null)
            {
                animator.applyRootMotion = false;
                CacheAnimatorParameters();
            }

            survivalSystem = GetComponent<SurvivalSystem>();
            if (survivalSystem == null)
            {
                survivalSystem = SurvivalSystem.Instance;
            }

            if (survivalSystem != null)
            {
                survivalSystem.OnPlayerDeath += HandlePlayerDeath;
                survivalSystem.OnPlayerRespawn += HandlePlayerRespawn;
            }
        }

        public void SetAnimator(Animator newAnim)
        {
            animator = newAnim;
            if (animator != null)
            {
                animator.applyRootMotion = false;
                CacheAnimatorParameters();
            }
        }

        private void CacheAnimatorParameters()
        {
            validAnimParams.Clear();
            if (animator == null || animator.runtimeAnimatorController == null) return;

            foreach (var p in animator.parameters)
            {
                validAnimParams.Add(p.nameHash);
            }
        }

        private void SafeSetFloat(int hash, float val)
        {
            if (animator != null && validAnimParams.Contains(hash))
            {
                animator.SetFloat(hash, val);
            }
        }

        private void SafeSetBool(int hash, bool val)
        {
            if (animator != null && validAnimParams.Contains(hash))
            {
                animator.SetBool(hash, val);
            }
        }

        private void SafeSetTrigger(int hash)
        {
            if (animator != null && validAnimParams.Contains(hash))
            {
                animator.SetTrigger(hash);
            }
        }

        private void OnDestroy()
        {
            if (survivalSystem != null)
            {
                survivalSystem.OnPlayerDeath -= HandlePlayerDeath;
                survivalSystem.OnPlayerRespawn -= HandlePlayerRespawn;
            }
        }

        private void Update()
        {
            // Vérifie si le joueur est mort
            if (survivalSystem != null && survivalSystem.IsDead)
            {
                ApplyGravityOnly();
                return;
            }

            CheckGroundStatus();
            ProcessInputs();
            HandleMovement();
            HandleFootsteps();
            UpdateAnimator();
        }
        #endregion

        #region Entrées Joueur (New Input System)
        private Vector2 inputVector = Vector2.zero;
        private bool jumpInput = false;
        private bool sprintInput = false;

        private void ProcessInputs()
        {
            inputVector = Vector2.zero;

            // 1. Clavier
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                // Support ZQSD (AZERTY) et WASD (QWERTY)
                if (kb.wKey.isPressed || kb.zKey.isPressed) inputVector.y += 1f;
                if (kb.sKey.isPressed) inputVector.y -= 1f;
                if (kb.aKey.isPressed || kb.qKey.isPressed) inputVector.x -= 1f;
                if (kb.dKey.isPressed) inputVector.x += 1f;

                // Sprint (Shift)
                sprintInput = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

                // Saut (Espace)
                jumpInput = kb.spaceKey.wasPressedThisFrame;

                // Sneak / Accroupi (C ou Left Control)
                if (kb.cKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame)
                {
                    isSneaking = !isSneaking;
                    ApplySneakCollider(isSneaking);
                }
            }

            // 2. Manette (Gamepad)
            Gamepad gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 stick = gp.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.05f)
                {
                    inputVector = stick;
                }

                if (gp.leftStickButton.isPressed) sprintInput = true;
                if (gp.buttonSouth.wasPressedThisFrame) jumpInput = true; // Bouton A / Croix
                if (gp.buttonEast.wasPressedThisFrame) // Bouton B / Cercle
                {
                    isSneaking = !isSneaking;
                    ApplySneakCollider(isSneaking);
                }
            }

            // Normalisation des entrées pour éviter de courir plus vite en diagonale
            if (inputVector.sqrMagnitude > 1f)
            {
                inputVector.Normalize();
            }

            isMoving = inputVector.sqrMagnitude > 0.01f;
        }
        #endregion

        #region Mouvement & Physique 3ème Personne
        private void HandleMovement()
        {
            // Détermine la vitesse cible selon l'état
            if (!isMoving)
            {
                currentTargetSpeed = 0f;
                isRunning = false;
            }
            else if (isSneaking)
            {
                currentTargetSpeed = sneakSpeed;
                isRunning = false;
            }
            else if (sprintInput && (survivalSystem == null || survivalSystem.HasStamina))
            {
                currentTargetSpeed = runSpeed;
                isRunning = true;
            }
            else
            {
                currentTargetSpeed = walkSpeed;
                isRunning = false;
            }

            // Notifie le système de survie pour la gestion de l'endurance
            if (survivalSystem != null)
            {
                survivalSystem.SetRunningState(isRunning && isMoving);
            }

            // Calcul de la direction relative à la caméra
            Vector3 moveDirection = Vector3.zero;

            if (isMoving)
            {
                Vector3 camForward = Vector3.forward;
                Vector3 camRight = Vector3.right;

                if (cameraTransform != null)
                {
                    camForward = cameraTransform.forward;
                    camRight = cameraTransform.right;
                    camForward.y = 0f;
                    camRight.y = 0f;
                    camForward.Normalize();
                    camRight.Normalize();
                }

                moveDirection = (camForward * inputVector.y + camRight * inputVector.x).normalized;

                // Rotation douce du personnage vers la direction de déplacement
                float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            // Gestion du saut et de la gravité
            if (isGrounded && verticalVelocity.y <= 0f)
            {
                verticalVelocity.y = -2f; // Force d'adhérence au sol

                if (jumpInput && !isSneaking)
                {
                    bool canJump = true;
                    if (survivalSystem != null)
                    {
                        canJump = survivalSystem.TryConsumeJumpStamina();
                    }

                    if (canJump)
                    {
                        // Impulsion vers le haut
                        verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                        isGrounded = false;
                        if (animator != null)
                        {
                            SafeSetTrigger(AnimJumpTriggerHash);

                            // CrossFade immédiat pour compatibilité universelle (Invector ou BlendTrees)
                            if (animator.HasState(0, Animator.StringToHash("Jump")))
                            {
                                animator.CrossFadeInFixedTime("Jump", 0.08f);
                            }
                            else if (animator.HasState(0, Animator.StringToHash("Jump_InAir")))
                            {
                                animator.CrossFadeInFixedTime("Jump_InAir", 0.08f);
                            }
                            else if (animator.HasState(0, Animator.StringToHash("JumpMove")))
                            {
                                animator.CrossFadeInFixedTime("JumpMove", 0.08f);
                            }
                        }
                    }
                }
            }
            else
            {
                // En l'air ou en phase ascendante du saut : appliquer la gravité
                verticalVelocity.y += gravity * Time.deltaTime;
                verticalVelocity.y = Mathf.Clamp(verticalVelocity.y, -30f, 15f);
            }

            // Application du déplacement au CharacterController
            Vector3 finalMovement = (moveDirection * currentTargetSpeed) + verticalVelocity;
            characterController.Move(finalMovement * Time.deltaTime);
        }

        private void ApplyGravityOnly()
        {
            if (isGrounded && verticalVelocity.y <= 0f)
            {
                verticalVelocity.y = -2f;
            }
            else
            {
                verticalVelocity.y += gravity * Time.deltaTime;
                verticalVelocity.y = Mathf.Clamp(verticalVelocity.y, -30f, 15f);
            }
            characterController.Move(verticalVelocity * Time.deltaTime);
        }

        private void CheckGroundStatus()
        {
            // Pendant la montée d'un saut, le joueur n'est pas au sol
            if (verticalVelocity.y > 0.05f)
            {
                isGrounded = false;
                return;
            }

            isGrounded = characterController.isGrounded;

            // Détection au sol supplémentaire vers le bas (ignore le joueur)
            if (!isGrounded)
            {
                int playerMask = 1 << gameObject.layer;
                int effectiveMask = groundLayer & ~playerMask;
                Vector3 rayOrigin = transform.position + Vector3.up * 0.15f;
                isGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckRadius, effectiveMask, QueryTriggerInteraction.Ignore);
            }
        }

        private void ApplySneakCollider(bool sneak)
        {
            if (sneak)
            {
                characterController.height = crouchingHeight;
                characterController.center = crouchingCenter;
            }
            else
            {
                characterController.height = standingHeight;
                characterController.center = standingCenter;
            }
        }
        #endregion

        #region Animation Mixamo & Invector
        private void UpdateAnimator()
        {
            if (animator == null) return;

            if (validAnimParams.Count == 0 && animator.runtimeAnimatorController != null)
            {
                CacheAnimatorParameters();
            }

            // Calcul de la valeur Speed pour le Blend Tree ou transition
            // 0.0 = Idle, 0.35 = Sneak, 0.5 = Walk, 1.0 = Run, 1.5 = Sprint
            float targetAnimSpeed = 0f;
            if (isMoving)
            {
                if (isSneaking)
                {
                    targetAnimSpeed = 0.35f;
                }
                else if (isRunning)
                {
                    targetAnimSpeed = 1.0f;
                }
                else
                {
                    targetAnimSpeed = 0.5f;
                }
            }

            currentAnimSpeedValue = Mathf.MoveTowards(currentAnimSpeedValue, targetAnimSpeed, Time.deltaTime / animatorDampTime);

            // 1. Paramètres Mecanim standard (Mixamo PlayerAnimatorController)
            SafeSetFloat(AnimSpeedHash, currentAnimSpeedValue);
            SafeSetBool(AnimIsMovingHash, isMoving);
            SafeSetBool(AnimIsRunningHash, isRunning);
            SafeSetBool(AnimIsSneakingHash, isSneaking);
            SafeSetBool(AnimIsGroundedHash, isGrounded);

            // 2. Paramètres Invector Locomotion (Invector@BasicLocomotion)
            float invectorMagnitude = isMoving ? (isRunning ? 1.5f : (isSneaking ? 0.35f : 0.85f)) : 0f;
            SafeSetFloat(AnimInputMagnitudeHash, invectorMagnitude);
            SafeSetFloat(AnimInputVerticalHash, inputVector.y);
            SafeSetFloat(AnimInputHorizontalHash, inputVector.x);
            SafeSetBool(AnimIsSprintingHash, isRunning && isMoving);
            SafeSetFloat(AnimGroundDistanceHash, isGrounded ? 0f : 1.5f);
            SafeSetFloat(AnimVerticalVelocityHash, verticalVelocity.y);

            // Transition dynamique de chute libre (Falling)
            if (!isGrounded && verticalVelocity.y < -3.5f)
            {
                if (animator.HasState(0, Animator.StringToHash("Falling")))
                {
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (!stateInfo.IsName("Falling"))
                    {
                        animator.CrossFadeInFixedTime("Falling", 0.15f);
                    }
                }
            }
        }
        #endregion

        #region Bruits de Pas (Footsteps)
        private void HandleFootsteps()
        {
            if (!isGrounded || !isMoving)
            {
                footstepTimer = 0f;
                return;
            }

            float currentInterval = isSneaking ? sneakStepInterval : (isRunning ? runStepInterval : walkStepInterval);
            footstepTimer += Time.deltaTime;

            if (footstepTimer >= currentInterval)
            {
                footstepTimer = 0f;
                PlayFootstepAudio();
            }
        }

        private void PlayFootstepAudio()
        {
            if (footstepClips == null || footstepClips.Length == 0) return;

            AudioClip clip = footstepClips[UnityEngine.Random.Range(0, footstepClips.Length)];
            if (clip != null && AudioManager.Instance != null)
            {
                float stepVol = isSneaking ? 0.3f : (isRunning ? 0.85f : 0.6f);
                AudioManager.Instance.PlaySFXAtPosition(clip, transform.position, stepVol);
            }
        }
        #endregion

        #region Mort et Réapparition
        private void HandlePlayerDeath()
        {
            if (animator != null)
            {
                animator.SetBool(AnimIsDeadHash, true);
            }
        }

        private void HandlePlayerRespawn()
        {
            if (animator != null)
            {
                animator.SetBool(AnimIsDeadHash, false);
            }
            verticalVelocity = Vector3.zero;
        }
        #endregion
    }
}
