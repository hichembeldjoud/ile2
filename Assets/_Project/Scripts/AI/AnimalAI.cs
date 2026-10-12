using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SurvivalIsland.Player;
using SurvivalIsland.Items;
using SurvivalIsland.Audio;

namespace SurvivalIsland.AI
{
    /// <summary>
    /// États comportementaux de l'animal.
    /// </summary>
    public enum AnimalState
    {
        Idle,       // Au repos, broute ou observe
        Wander,     // Erre calmement sur le NavMesh
        Flee,       // Fuit le joueur à toute allure (herbivore)
        Chase,      // Poursuit le joueur à toute allure (prédateur loup)
        Attack,     // Attaque le joueur au corps à corps
        Dead        // Mort, instancie le loot et disparaît
    }

    /// <summary>
    /// Intelligence Artificielle animale réaliste (ex: Sanglier, Gibier).
    /// Utilise le NavMesh pour errer aléatoirement, détecte le joueur par l'ouïe
    /// (sensible à la marche, course ou sneak), s'enfuit en panique et donne de la viande à sa mort.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival Island/AI/Animal AI")]
    public class AnimalAI : MonoBehaviour
    {
        #region Paramètres de Santé & Identité
        [Header("--- Caractéristiques de l'Animal ---")]
        [Tooltip("Nom de l'espèce (ex: Sanglier Tropical).")]
        [SerializeField]
        private string animalName = "Sanglier Tropical";

        [Tooltip("Points de vie de l'animal.")]
        [SerializeField]
        private float maxHealth = 60f;

        [Tooltip("Santé actuelle.")]
        [SerializeField]
        private float currentHealth = 60f;

        [Header("--- Rôle Prédateur (Loup) ---")]
        [Tooltip("Si vrai, l'animal est un prédateur agressif (ex: Loup) qui poursuit et attaque le joueur.")]
        [SerializeField]
        private bool isPredator = false;

        [Tooltip("Dégâts infligés au joueur par morsure/coup de griffe.")]
        [SerializeField]
        private float attackDamage = 18f;

        [Tooltip("Portée d'attaque au corps à corps.")]
        [SerializeField]
        private float attackRange = 2.2f;

        [Tooltip("Intervalle minimal entre deux attaques.")]
        [SerializeField]
        private float attackCooldown = 1.6f;

        private float lastAttackTime = -10f;
        #endregion

        #region Vitesses & Déplacement NavMesh
        [Header("--- Déplacement (NavMesh) ---")]
        [Tooltip("Vitesse de marche lors de l'errance (m/s).")]
        [SerializeField]
        private float wanderSpeed = 2.2f;

        [Tooltip("Vitesse de sprint lors de la fuite (m/s).")]
        [SerializeField]
        private float fleeSpeed = 6.8f;

        [Tooltip("Rayon maximal d'errance autour de la position d'origine.")]
        [SerializeField]
        private float wanderRadius = 30f;

        [Tooltip("Temps d'attente minimal en pause (Idle) entre deux déplacements.")]
        [SerializeField]
        private float minIdleTime = 3f;

        [Tooltip("Temps d'attente maximal en pause (Idle).")]
        [SerializeField]
        private float maxIdleTime = 7f;
        #endregion

        #region Détection Sensorielle (Ouïe & Vue)
        [Header("--- Détection Sensorielle ---")]
        [Tooltip("Portée d'audition quand le joueur court (Sprint).")]
        [SerializeField]
        private float runHearingRange = 20f;

        [Tooltip("Portée d'audition quand le joueur marche normalement.")]
        [SerializeField]
        private float walkHearingRange = 9f;

        [Tooltip("Portée d'audition quand le joueur est accroupi (Mode Sneak).")]
        [SerializeField]
        private float sneakHearingRange = 3.2f;

        [Tooltip("Angle de vision frontale en degrés.")]
        [SerializeField, Range(30f, 180f)]
        private float fieldOfView = 110f;

        [Tooltip("Distance maximale de vision.")]
        [SerializeField]
        private float sightRange = 15f;

        [Tooltip("Calque des obstacles masquant la vue (Rochers, Arbres).")]
        [SerializeField]
        private LayerMask sightObstaclesLayer = ~0;
        #endregion

        #region Comportement de Fuite
        [Header("--- Comportement de Fuite ---")]
        [Tooltip("Distance ciblée pour s'éloigner du joueur lors d'une fuite.")]
        [SerializeField]
        private float fleeDistance = 35f;

        [Tooltip("Durée de la panique de fuite avant de tenter de se calmer.")]
        [SerializeField]
        private float fleeDuration = 6.5f;

        [Tooltip("Distance de sécurité pour se calmer et reprendre l'errance.")]
        [SerializeField]
        private float safeDistance = 28f;
        #endregion

        #region Loot & Mort
        [Header("--- Chasse & Loot ---")]
        [Tooltip("Préfabriqué de l'objet 'Viande' instancié à la mort.")]
        [SerializeField]
        private GameObject meatPrefab;

        [Tooltip("Nombre de morceaux de viande déposés.")]
        [SerializeField, Range(1, 4)]
        private int meatDropCount = 2;

        [Tooltip("Délai en secondes avant que le corps ne disparaisse.")]
        [SerializeField]
        private float corpseDisappearDelay = 2.5f;

        [Tooltip("Clip audio de cri de douleur/panique.")]
        [SerializeField]
        private AudioClip squealAudioClip;

        [Tooltip("Clip audio de mort.")]
        [SerializeField]
        private AudioClip deathAudioClip;
        #endregion

        #region Animation Mixamo
        [Header("--- Animation Mixamo ---")]
        [Tooltip("Animator du modèle animal.")]
        [SerializeField]
        private Animator animator;

        private static readonly int AnimSpeedHash = Animator.StringToHash("Speed");
        private static readonly int AnimIsFleeingHash = Animator.StringToHash("IsFleeing");
        private static readonly int AnimDieTriggerHash = Animator.StringToHash("Die");
        private static readonly int AnimIsDeadHash = Animator.StringToHash("IsDead");
        #endregion

        #region Événements C#
        public event Action<AnimalAI> OnAnimalDied;
        public event Action<AnimalAI> OnAnimalFled;
        #endregion

        #region Variables Internes
        private NavMeshAgent agent;
        private Transform playerTransform;
        private PlayerController playerController;
        private AnimalState currentState = AnimalState.Idle;

        private Vector3 spawnOrigin;
        private float stateTimer = 0f;
        private float fleeTimer = 0f;
        private bool isDead = false;
        private string currentAnimState = string.Empty;

        // Locomotion procédurale (résout la glisse des animaux sans animation squelettique)
        private Transform visualTransform;
        private Vector3 initialVisualLocalPos;
        private Quaternion initialVisualLocalRot;
        private float strideCycle = 0f;
        private float idleCycle = 0f;
        #endregion

        #region Propriétés Publiques
        public AnimalState CurrentState => currentState;
        public bool IsDead => isDead;
        public float CurrentHealth => currentHealth;
        public string AnimalName => animalName;
        #endregion

        #region Animation Helpers
        /// <summary>
        /// Joue une animation de manière fluide sans réinitialiser la transition à chaque frame.
        /// Résout définitivement le problème des animaux qui glissent sans animation.
        /// </summary>
        public void PlayAnimation(string stateName, float transitionDuration = 0.2f)
        {
            if (animator == null) return;
            if (currentAnimState == stateName) return;

            currentAnimState = stateName;
            animator.CrossFadeInFixedTime(stateName, transitionDuration);
        }
        #endregion

        #region Cycle de Vie Unity
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;
            }

            // Référence visuelle pour la locomotion procédurale (marche, course, roulis)
            visualTransform = transform.Find("VisualModel");
            if (visualTransform == null && transform.childCount > 0)
            {
                visualTransform = transform.GetChild(0);
            }
            if (visualTransform != null)
            {
                EnsureVisualGrounded();
                EnsureMaterialsURPLit();
                initialVisualLocalPos = visualTransform.localPosition;
                initialVisualLocalRot = visualTransform.localRotation;
            }

            currentHealth = maxHealth;
            spawnOrigin = transform.position;
        }

        private void Start()
        {
            FindPlayerReference();
            SetState(AnimalState.Idle);
            stateTimer = UnityEngine.Random.Range(minIdleTime, maxIdleTime);
        }

        private void Update()
        {
            // Locomotion procédurale active même pendant la transition de mort
            UpdateProceduralLocomotion();

            if (isDead) return;

            // Maintient la référence du joueur
            if (playerTransform == null)
            {
                FindPlayerReference();
            }

            // Détection sensorielle permanente si pas déjà en action d'attaque/fuite/poursuite
            if (currentState != AnimalState.Flee && currentState != AnimalState.Chase && currentState != AnimalState.Attack)
            {
                CheckForPlayerSensors();
            }

            // Machine à états
            switch (currentState)
            {
                case AnimalState.Idle:
                    UpdateIdleState();
                    break;

                case AnimalState.Wander:
                    UpdateWanderState();
                    break;

                case AnimalState.Flee:
                    UpdateFleeState();
                    break;

                case AnimalState.Chase:
                    UpdateChaseState();
                    break;

                case AnimalState.Attack:
                    UpdateAttackState();
                    break;
            }

            UpdateAnimator();
        }
        #endregion

        #region Détection Sensorielle (Ouïe Dynamique & Vue)
        private void FindPlayerReference()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerController = playerObj.GetComponent<PlayerController>();
            }
        }

        private void CheckForPlayerSensors()
        {
            if (playerTransform == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // 1. Détection par l'Ouïe (selon l'état du joueur)
            float hearingThreshold = GetCurrentPlayerHearingRange();
            if (distanceToPlayer <= hearingThreshold)
            {
                // L'animal a détecté le joueur !
                if (isPredator) StartChasing();
                else StartFleeing();
                return;
            }

            // 2. Détection par la Vue (Cône visuel)
            if (distanceToPlayer <= sightRange)
            {
                Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToPlayer);

                if (angle <= fieldOfView * 0.5f)
                {
                    // Vérification de la ligne de vue (Line of Sight)
                    if (!Physics.Raycast(transform.position + Vector3.up * 0.8f, dirToPlayer, distanceToPlayer, sightObstaclesLayer, QueryTriggerInteraction.Ignore))
                    {
                        if (isPredator) StartChasing();
                        else StartFleeing();
                    }
                }
            }
        }

        private float GetCurrentPlayerHearingRange()
        {
            if (playerController == null) return walkHearingRange;

            if (!playerController.IsMoving)
            {
                return 0f; // Immobile = Silencieux
            }

            if (playerController.IsSneaking)
            {
                return sneakHearingRange; // Mode discret
            }

            if (playerController.IsRunning)
            {
                return runHearingRange; // Course bruyante
            }

            return walkHearingRange; // Marche standard
        }
        #endregion

        private bool IsAgentReady()
        {
            if (agent == null || !agent.isActiveAndEnabled) return false;
            if (!agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 6f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    return agent.isOnNavMesh;
                }
                return false;
            }
            return true;
        }

        #region États & Comportements
        private void UpdateIdleState()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                // Choisit une nouvelle destination d'errance
                Vector3 newWanderPoint = GetRandomNavMeshPoint(spawnOrigin, wanderRadius);
                if (newWanderPoint != Vector3.zero && IsAgentReady())
                {
                    agent.isStopped = false;
                    agent.speed = wanderSpeed;
                    agent.SetDestination(newWanderPoint);
                    SetState(AnimalState.Wander);
                }
                else
                {
                    stateTimer = UnityEngine.Random.Range(minIdleTime, maxIdleTime);
                }
            }
        }

        private void UpdateWanderState()
        {
            if (IsAgentReady())
            {
                // Vérifie si l'animal est arrivé à destination
                if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + 0.3f))
                {
                    stateTimer = UnityEngine.Random.Range(minIdleTime, maxIdleTime);
                    SetState(AnimalState.Idle);
                }
            }
            else
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    stateTimer = UnityEngine.Random.Range(minIdleTime, maxIdleTime);
                    SetState(AnimalState.Idle);
                }
            }
        }

        private void UpdateFleeState()
        {
            fleeTimer += Time.deltaTime;

            if (playerTransform != null)
            {
                bool needNewPath = false;
                if (IsAgentReady())
                {
                    needNewPath = (!agent.hasPath || agent.remainingDistance <= 2f || fleeTimer > 1.5f);
                }
                else
                {
                    needNewPath = (fleeTimer > 1.5f);
                }

                if (needNewPath)
                {
                    Vector3 fleeTarget = GetFleeDestination(playerTransform.position);
                    if (fleeTarget != Vector3.zero)
                    {
                        if (IsAgentReady())
                        {
                            agent.isStopped = false;
                            agent.speed = fleeSpeed;
                            agent.SetDestination(fleeTarget);
                        }
                        else
                        {
                            transform.position = Vector3.MoveTowards(transform.position, fleeTarget, fleeSpeed * Time.deltaTime);
                            Vector3 look = new Vector3(fleeTarget.x, transform.position.y, fleeTarget.z) - transform.position;
                            if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(look);
                        }
                    }
                }

                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (fleeTimer >= fleeDuration && dist >= safeDistance)
                {
                    // L'animal est en sécurité
                    SetState(AnimalState.Idle);
                    stateTimer = UnityEngine.Random.Range(minIdleTime, maxIdleTime);
                }
            }
        }

        private void StartChasing()
        {
            if (currentState == AnimalState.Chase || currentState == AnimalState.Attack || isDead) return;

            SetState(AnimalState.Chase);
            if (IsAgentReady())
            {
                agent.isStopped = false;
                agent.speed = fleeSpeed;
            }

            if (squealAudioClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(squealAudioClip, transform.position, 0.9f);
            }
        }

        private void UpdateChaseState()
        {
            if (playerTransform == null)
            {
                SetState(AnimalState.Idle);
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);

            if (dist <= attackRange)
            {
                SetState(AnimalState.Attack);
                if (IsAgentReady())
                {
                    agent.isStopped = true;
                }
                return;
            }

            // Si le joueur est trop loin, le prédateur abandonne
            if (dist > safeDistance * 1.5f)
            {
                SetState(AnimalState.Idle);
                stateTimer = UnityEngine.Random.Range(minIdleTime, maxIdleTime);
                if (IsAgentReady())
                {
                    agent.isStopped = false;
                    agent.speed = wanderSpeed;
                }
                return;
            }

            if (IsAgentReady())
            {
                agent.isStopped = false;
                agent.speed = fleeSpeed;
                agent.SetDestination(playerTransform.position);
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, fleeSpeed * Time.deltaTime);
                Vector3 look = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z) - transform.position;
                if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(look);
            }
        }

        private void UpdateAttackState()
        {
            if (playerTransform == null)
            {
                SetState(AnimalState.Idle);
                return;
            }

            // Oriente le prédateur vers le joueur
            Vector3 lookDir = (playerTransform.position - transform.position);
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 8f);
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist > attackRange * 1.4f)
            {
                SetState(AnimalState.Chase);
                return;
            }

            // Attaque avec temps de recharge
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;

                if (animator != null)
                {
                    if (animator.HasState(0, Animator.StringToHash("AttackL")))
                    {
                        animator.CrossFadeInFixedTime("AttackL", 0.1f);
                    }
                    else if (animator.HasState(0, Animator.StringToHash("AttackR")))
                    {
                        animator.CrossFadeInFixedTime("AttackR", 0.1f);
                    }
                }

                var survival = SurvivalIsland.Survival.SurvivalSystem.Instance;
                if (survival != null)
                {
                    survival.TakeDamage(attackDamage, animalName);
                }

                if (squealAudioClip != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFXAtPosition(squealAudioClip, transform.position, 1.0f);
                }
            }
        }

        private void StartFleeing()
        {
            if (currentState == AnimalState.Flee) return;

            SetState(AnimalState.Flee);
            fleeTimer = 0f;

            if (IsAgentReady())
            {
                agent.isStopped = false;
                agent.speed = fleeSpeed;

                if (playerTransform != null)
                {
                    Vector3 fleeTarget = GetFleeDestination(playerTransform.position);
                    if (fleeTarget != Vector3.zero)
                    {
                        agent.SetDestination(fleeTarget);
                    }
                }
            }

            // Joue un son d'alerte / cri animal
            if (squealAudioClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(squealAudioClip, transform.position, 0.85f);
            }

            OnAnimalFled?.Invoke(this);
        }

        private Vector3 GetFleeDestination(Vector3 threatPosition)
        {
            Vector3 fleeDir = (transform.position - threatPosition).normalized;
            // Ajoute une légère variation aléatoire pour une fuite naturelle
            fleeDir += new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 0f, UnityEngine.Random.Range(-0.3f, 0.3f));
            fleeDir.Normalize();

            Vector3 targetPoint = transform.position + fleeDir * fleeDistance;

            if (NavMesh.SamplePosition(targetPoint, out NavMeshHit hit, 15f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return Vector3.zero;
        }

        private Vector3 GetRandomNavMeshPoint(Vector3 center, float radius)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * radius;
            Vector3 randomPos = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return Vector3.zero;
        }

        private void SetState(AnimalState newState)
        {
            currentState = newState;
        }
        #endregion

        #region Dégâts & Mort
        /// <summary>
        /// Inflige des dégâts à l'animal (ex: flèche, balle de fusil).
        /// </summary>
        public void TakeDamage(float damage, Vector3 attackerPosition)
        {
            if (isDead) return;

            currentHealth -= damage;
            Debug.Log($"<color=#E91E63><b>[{animalName}]</b> Dégâts reçus: {damage:F0} | Santé restante: {Mathf.Max(0, currentHealth):F0}/{maxHealth:F0}</color>");

            // Réaction d'impact / Get Hit
            if (animator != null && animator.HasState(0, Animator.StringToHash("Get_Hit")))
            {
                animator.SetTrigger("GetHit");
                currentAnimState = "Get_Hit";
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
            else
            {
                // Si touché, le prédateur contre-attaque, la proie fuit
                if (isPredator) StartChasing();
                else StartFleeing();
            }
        }

        private void Die()
        {
            if (isDead) return;

            isDead = true;
            currentHealth = 0f;
            SetState(AnimalState.Dead);

            // Arrête le déplacement
            if (agent != null)
            {
                if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
                agent.enabled = false;
            }

            // Désactive TOUS les colliders de l'animal pour ne pas bloquer le joueur
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                if (col != null) col.enabled = false;
            }

            // Audio de mort
            if (deathAudioClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(deathAudioClip, transform.position, 1.0f);
            }

            // Instanciation de la Viande
            SpawnMeatLoot();

            OnAnimalDied?.Invoke(this);

            // Notification du système de quêtes
            if (SurvivalIsland.Quests.QuestManager.Instance != null)
            {
                SurvivalIsland.Quests.QuestManager.Instance.NotifyAnimalHunted();
            }

            // Animation de mort adaptée selon le contrôleur
            if (animator != null)
            {
                if (animator.HasState(0, Animator.StringToHash("Dying")))
                {
                    PlayAnimation("Dying", 0.1f);
                }
                else if (animator.HasState(0, Animator.StringToHash("dead")))
                {
                    PlayAnimation("dead", 0.1f);
                }
                else
                {
                    animator.SetTrigger(AnimDieTriggerHash);
                    animator.SetBool(AnimIsDeadHash, true);
                }
            }

            // Disparition progressive du cadavre
            StartCoroutine(CorpseDisappearanceRoutine());
        }

        private void SpawnMeatLoot()
        {
            if (meatPrefab == null)
            {
                Debug.LogWarning($"[AnimalAI] Aucun préfabriqué de viande assigné sur '{animalName}'. Création d'un loot par défaut.");
                CreateFallbackMeatObject();
                return;
            }

            for (int i = 0; i < meatDropCount; i++)
            {
                Vector3 offset = new Vector3(UnityEngine.Random.Range(-0.6f, 0.6f), 0.3f, UnityEngine.Random.Range(-0.6f, 0.6f));
                Instantiate(meatPrefab, transform.position + offset, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0));
            }
        }

        private void CreateFallbackMeatObject()
        {
            GameObject resMeat = Resources.Load<GameObject>("Meat_Loot") ?? Resources.Load<GameObject>("Items/Meat_Loot");
            for (int i = 0; i < meatDropCount; i++)
            {
                Vector3 offset = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0.3f, UnityEngine.Random.Range(-0.5f, 0.5f));
                if (resMeat != null)
                {
                    Instantiate(resMeat, transform.position + offset, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0));
                }
                else
                {
                    GameObject meatObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    meatObj.name = "Meat_Loot";
                    meatObj.transform.localScale = new Vector3(0.4f, 0.15f, 0.4f);
                    meatObj.transform.position = transform.position + offset;
                    meatObj.AddComponent<MeatItem>();
                }
            }
        }

        private IEnumerator CorpseDisappearanceRoutine()
        {
            yield return new WaitForSeconds(corpseDisappearDelay);

            // Fondu d'échelle ou disparition
            float elapsed = 0f;
            Vector3 originalScale = transform.localScale;
            while (elapsed < 0.6f)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsed / 0.6f);
                yield return null;
            }

            Destroy(gameObject);
        }
        #endregion

        #region Animation
        private void UpdateAnimator()
        {
            if (animator == null) return;

            // 1. Support WolfAnimations.controller (Loup Sauvage URP)
            if (animator.HasState(0, Animator.StringToHash("Idle_New")))
            {
                if (isDead)
                {
                    PlayAnimation("dead", 0.15f);
                    return;
                }

                if (currentState == AnimalState.Attack)
                {
                    return; // L'animation d'attaque est déclenchée lors de la morsure
                }

                bool isMoving = (currentState == AnimalState.Chase || currentState == AnimalState.Flee || currentState == AnimalState.Wander);
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    isMoving = agent.velocity.sqrMagnitude > 0.05f || (agent.hasPath && agent.remainingDistance > 0.3f);
                }

                if (isMoving)
                {
                    if (currentState == AnimalState.Chase || currentState == AnimalState.Flee)
                    {
                        PlayAnimation("Running_New", 0.15f);
                    }
                    else
                    {
                        PlayAnimation("Walking_New", 0.2f);
                    }
                }
                else
                {
                    PlayAnimation("Idle_New", 0.25f);
                }
                return;
            }

            // 2. Support Wild_Boar.controller (Dossier Wild Boar)
            if (animator.HasState(0, Animator.StringToHash("Idle")) && animator.HasState(0, Animator.StringToHash("Walking")))
            {
                if (isDead)
                {
                    PlayAnimation("Dying", 0.15f);
                    return;
                }

                if (currentState == AnimalState.Attack)
                {
                    PlayAnimation("Attack_Charge_Gore", 0.15f);
                    return;
                }

                bool isMoving = (currentState == AnimalState.Chase || currentState == AnimalState.Flee || currentState == AnimalState.Wander);
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    isMoving = agent.velocity.sqrMagnitude > 0.05f || (agent.hasPath && agent.remainingDistance > 0.3f);
                }

                if (isMoving)
                {
                    if (currentState == AnimalState.Chase || currentState == AnimalState.Flee)
                    {
                        PlayAnimation("Run", 0.15f);
                    }
                    else
                    {
                        PlayAnimation("Walking", 0.2f);
                    }
                }
                else
                {
                    PlayAnimation("Idle", 0.25f);
                }
                return;
            }

            // 3. Support Mecanim standard (Animaux génériques)
            float normalizedSpeed = 0f;
            if (currentState == AnimalState.Flee || currentState == AnimalState.Chase)
            {
                normalizedSpeed = 1.0f;
            }
            else if (currentState == AnimalState.Wander)
            {
                normalizedSpeed = 0.5f;
            }
            else if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && agent.velocity.sqrMagnitude > 0.05f)
            {
                normalizedSpeed = 0.5f;
            }

            animator.SetFloat(AnimSpeedHash, normalizedSpeed);
            animator.SetBool(AnimIsFleeingHash, currentState == AnimalState.Flee || currentState == AnimalState.Chase);
        }

        /// <summary>
        /// Locomotion procédurale réaliste : génère des foulées de marche et de course,
        /// des oscillations de poids et du roulis naturel pour tout animal sans animation squelettique.
        /// Résout le problème des animaux qui glissent sur le terrain.
        /// </summary>
        private void UpdateProceduralLocomotion()
        {
            if (visualTransform == null) return;

            if (isDead)
            {
                // Bascule naturelle sur le flanc lors de la mort
                Quaternion deathRot = initialVisualLocalRot * Quaternion.Euler(0, 0, 80f);
                visualTransform.localRotation = Quaternion.Slerp(visualTransform.localRotation, deathRot, Time.deltaTime * 6f);
                return;
            }

            // Si un animateur avec des états réels de marche/course est actif (Loup ou Sanglier), on n'interfère pas
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                if (animator.HasState(0, Animator.StringToHash("Idle_New")) ||
                    (animator.HasState(0, Animator.StringToHash("Idle")) && animator.HasState(0, Animator.StringToHash("Walking"))))
                {
                    return;
                }
            }

            float speed = (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) ? agent.velocity.magnitude : 0f;

            if (speed > 0.15f)
            {
                bool isRunning = (currentState == AnimalState.Flee || currentState == AnimalState.Chase || speed > wanderSpeed * 1.25f);
                float freq = isRunning ? 13f : 7.5f;
                strideCycle += Time.deltaTime * freq;

                // 1. Foulée verticale (montée et descente des pattes / galop)
                float bobAmplitude = isRunning ? 0.08f : 0.035f;
                float verticalBob = Mathf.Abs(Mathf.Sin(strideCycle)) * bobAmplitude;

                // 2. Bascule avant-arrière (tangage selon l'allure)
                float pitchAngle = isRunning ? Mathf.Sin(strideCycle) * 8.5f : Mathf.Sin(strideCycle * 0.5f) * 2.5f;

                // 3. Roulis latéral (transfert de masse entre les pattes gauches et droites)
                float rollAngle = Mathf.Sin(strideCycle * 0.5f) * (isRunning ? 4.5f : 3.0f);

                // 4. Lacet de tête
                float yawAngle = Mathf.Sin(strideCycle * 0.5f) * 2.0f;

                Vector3 targetPos = initialVisualLocalPos + new Vector3(0, verticalBob, 0);
                Quaternion targetRot = initialVisualLocalRot * Quaternion.Euler(pitchAngle, yawAngle, rollAngle);

                visualTransform.localPosition = Vector3.Lerp(visualTransform.localPosition, targetPos, Time.deltaTime * 18f);
                visualTransform.localRotation = Quaternion.Slerp(visualTransform.localRotation, targetRot, Time.deltaTime * 18f);
            }
            else
            {
                // Respiration et balancement doux au repos (Idle)
                idleCycle += Time.deltaTime * 2.2f;
                float breathBob = Mathf.Sin(idleCycle) * 0.015f;
                float breathPitch = Mathf.Sin(idleCycle) * 1.5f;

                Vector3 targetPos = initialVisualLocalPos + new Vector3(0, breathBob, 0);
                Quaternion targetRot = initialVisualLocalRot * Quaternion.Euler(breathPitch, 0, 0);

                visualTransform.localPosition = Vector3.Lerp(visualTransform.localPosition, targetPos, Time.deltaTime * 5f);
                visualTransform.localRotation = Quaternion.Slerp(visualTransform.localRotation, targetRot, Time.deltaTime * 5f);
            }
        }
        #endregion

        #region Méthodes Publiques de Configuration
        /// <summary>
        /// Configuration générique pour n'importe quelle espèce animale.
        /// </summary>
        public void ConfigureSpecies(string name, bool predator, float hp, float wanderSpd, float fleeSpd, float atkDmg = 0f, float atkRng = 2f, int drops = 2)
        {
            animalName = name;
            isPredator = predator;
            maxHealth = hp;
            currentHealth = hp;
            wanderSpeed = wanderSpd;
            fleeSpeed = fleeSpd;
            attackDamage = atkDmg;
            attackRange = atkRng;
            meatDropCount = drops;
        }

        /// <summary>
        /// Configure l'IA pour incarner un Loup Sauvage agressif et prédateur.
        /// </summary>
        public void ConfigureAsWolf()
        {
            ConfigureSpecies("Loup Sauvage", true, 80f, 2.4f, 6.4f, 18f, 2.2f, 2);
            attackCooldown = 1.5f;
        }

        /// <summary>
        /// Configure l'IA pour incarner un Sanglier tropical fuyard (gibier).
        /// </summary>
        public void ConfigureAsBoar()
        {
            ConfigureSpecies("Sanglier Tropical", false, 60f, 2.0f, 6.0f, 0f, 2f, 2);
        }

        /// <summary>
        /// Configure l'IA pour incarner un Ours Sauvage (grand prédateur puissant).
        /// </summary>
        public void ConfigureAsBear()
        {
            ConfigureSpecies("Ours Sauvage", true, 160f, 2.0f, 5.8f, 32f, 2.6f, 4);
            attackCooldown = 1.8f;
        }

        /// <summary>
        /// Configure l'IA pour incarner un Cerf / Biche agile (gibier craintif).
        /// </summary>
        public void ConfigureAsDeer()
        {
            ConfigureSpecies("Cerf Majestueux", false, 50f, 2.4f, 7.2f, 0f, 2f, 3);
        }

        /// <summary>
        /// Configure l'IA pour incarner un Renard vif.
        /// </summary>
        public void ConfigureAsFox()
        {
            ConfigureSpecies("Renard Sauvage", false, 35f, 2.4f, 6.8f, 0f, 1.8f, 1);
        }

        /// <summary>
        /// Configure l'IA pour incarner un Lapin rapide des plaines et plages.
        /// </summary>
        public void ConfigureAsRabbit()
        {
            ConfigureSpecies("Lapin Agile", false, 20f, 2.2f, 7.0f, 0f, 1.5f, 1);
        }

        /// <summary>
        /// Configure l'IA pour incarner une Lionne redoutable (grand prédateur).
        /// </summary>
        public void ConfigureAsLioness()
        {
            ConfigureSpecies("Lionne des Steppes", true, 130f, 2.6f, 7.0f, 25f, 2.4f, 3);
            attackCooldown = 1.4f;
        }

        /// <summary>
        /// Configure l'IA pour incarner une Hyène agressive.
        /// </summary>
        public void ConfigureAsHyena()
        {
            ConfigureSpecies("Hyène Tachetée", true, 75f, 2.5f, 6.6f, 16f, 2.0f, 2);
            attackCooldown = 1.5f;
        }

        /// <summary>
        /// Configure l'IA pour incarner un Alligator des côtes et lagons.
        /// </summary>
        public void ConfigureAsAlligator()
        {
            ConfigureSpecies("Alligator du Lagon", true, 140f, 1.8f, 5.2f, 28f, 2.2f, 3);
            attackCooldown = 2.0f;
        }

        /// <summary>
        /// Configure l'IA pour incarner un Élan imposant (grand herbivore).
        /// </summary>
        public void ConfigureAsMoose()
        {
            ConfigureSpecies("Grand Élan", false, 140f, 2.2f, 6.2f, 0f, 2.2f, 4);
        }

        /// <summary>
        /// Configure l'IA pour incarner un Buffle massif.
        /// </summary>
        public void ConfigureAsBuffalo()
        {
            ConfigureSpecies("Buffle Sauvage", false, 150f, 2.0f, 5.6f, 0f, 2.2f, 4);
        }

        /// <summary>
        /// Configure l'IA pour incarner une Chèvre des montagnes.
        /// </summary>
        public void ConfigureAsMountainGoat()
        {
            ConfigureSpecies("Chèvre des Falaises", false, 50f, 2.2f, 6.4f, 0f, 1.8f, 2);
        }

        /// <summary>
        /// Configure l'IA pour incarner un Mouflon Canadien.
        /// </summary>
        public void ConfigureAsBighornSheep()
        {
            ConfigureSpecies("Mouflon des Monts", false, 65f, 2.2f, 6.2f, 0f, 2.0f, 2);
        }

        /// <summary>
        /// Configure l'IA pour incarner un Gnou d'Afrique.
        /// </summary>
        public void ConfigureAsWildebeest()
        {
            ConfigureSpecies("Gnou Sauvage", false, 85f, 2.4f, 6.6f, 0f, 2.0f, 3);
        }

        /// <summary>
        /// Configure l'IA pour incarner un Blaireau à miel agressif.
        /// </summary>
        public void ConfigureAsHoneyBadger()
        {
            ConfigureSpecies("Blaireau à Miel", true, 60f, 2.2f, 5.8f, 14f, 1.8f, 2);
            attackCooldown = 1.3f;
        }

        /// <summary>
        /// Configure l'IA pour incarner un Phacochère.
        /// </summary>
        public void ConfigureAsWarthog()
        {
            ConfigureSpecies("Phacochère d'Afrique", false, 65f, 2.1f, 6.0f, 0f, 2.0f, 2);
        }

        /// <summary>
        /// Configure l'IA pour incarner un Cheval sauvage.
        /// </summary>
        public void ConfigureAsHorse()
        {
            ConfigureSpecies("Cheval Sauvage", false, 90f, 2.6f, 7.2f, 0f, 2.2f, 3);
        }

        /// <summary>
        /// Configure l'IA pour incarner un oiseau / gibier ailé (Oie, Pintade, Faisan).
        /// </summary>
        public void ConfigureAsBird(string birdName = "Gibier Ailé")
        {
            ConfigureSpecies(birdName, false, 18f, 1.8f, 5.5f, 0f, 1.2f, 1);
        }

        /// <summary>
        /// S'assure que le modèle visuel 3D de l'animal est posé au niveau du sol (Y >= 0).
        /// Calcule la boîte englobante locale réelle de tous les maillages enfants et
        /// élève visualTransform si le corps est sous le sol (résout définitivement
        /// le bug des prédateurs invisibles comme l'Ours ou l'Alligator sous terre).
        /// </summary>
        public void EnsureVisualGrounded()
        {
            if (visualTransform == null)
            {
                visualTransform = transform.Find("VisualModel");
                if (visualTransform == null && transform.childCount > 0)
                {
                    visualTransform = transform.GetChild(0);
                }
            }

            if (visualTransform == null) return;

            MeshFilter[] filters = visualTransform.GetComponentsInChildren<MeshFilter>(true);
            float lowestRootLocalY = float.MaxValue;
            bool foundGeometry = false;

            foreach (var mf in filters)
            {
                if (mf == null || mf.sharedMesh == null) continue;

                Bounds b = mf.sharedMesh.bounds;
                Vector3 min = b.min;
                Vector3 max = b.max;

                Vector3[] corners = new Vector3[8]
                {
                    new Vector3(min.x, min.y, min.z),
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(max.x, max.y, max.z)
                };

                foreach (var c in corners)
                {
                    Vector3 worldCorner = mf.transform.TransformPoint(c);
                    Vector3 rootLocalCorner = transform.InverseTransformPoint(worldCorner);
                    if (rootLocalCorner.y < lowestRootLocalY)
                    {
                        lowestRootLocalY = rootLocalCorner.y;
                        foundGeometry = true;
                    }
                }
            }

            // Si le point le plus bas du maillage est sous la racine (plus bas que -0.05m)
            if (foundGeometry && lowestRootLocalY < -0.05f)
            {
                float liftAmount = -lowestRootLocalY;
                Vector3 pos = visualTransform.localPosition;
                pos.y += liftAmount;
                visualTransform.localPosition = pos;
                initialVisualLocalPos = visualTransform.localPosition;
                initialVisualLocalRot = visualTransform.localRotation;
                Debug.Log($"<color=#4CAF50><b>[AnimalAI]</b> {animalName} : Modèle 3D élevé de {liftAmount:F2}m (était sous terre à Y = {lowestRootLocalY:F2}m) ! Désormais parfaitement visible.</color>");
            }
        }

        /// <summary>
        /// S'assure que les matériaux des maillages de l'animal utilisent le shader Universal Render Pipeline/Lit.
        /// </summary>
        public void EnsureMaterialsURPLit()
        {
            if (visualTransform == null) return;

            Renderer[] rends = visualTransform.GetComponentsInChildren<Renderer>(true);
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            if (litShader == null) return;

            foreach (var r in rends)
            {
                if (r == null) continue;
                Material[] mats = r.materials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && mats[i].shader != litShader)
                    {
                        if (mats[i].shader.name.StartsWith("Standard") || mats[i].shader.name.Contains("Error") || mats[i].shader.name.Contains("InternalError") || mats[i].shader.name == "Hidden/InternalErrorShader")
                        {
                            Texture mainTex = mats[i].mainTexture;
                            mats[i].shader = litShader;
                            if (mainTex != null)
                            {
                                mats[i].SetTexture("_BaseMap", mainTex);
                            }
                            changed = true;
                        }
                    }
                }
                if (changed)
                {
                    r.materials = mats;
                }
            }
        }
        #endregion

        #region Gizmos de Débogage
        private void OnDrawGizmosSelected()
        {
            // Rayon d'errance
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(spawnOrigin != Vector3.zero ? spawnOrigin : transform.position, wanderRadius);

            // Cône de vision
            Gizmos.color = new Color(0.9f, 0.9f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, sightRange);

            // Portée d'audition (course)
            Gizmos.color = new Color(0.9f, 0.2f, 0.2f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, runHearingRange);
        }
        #endregion
    }
}
