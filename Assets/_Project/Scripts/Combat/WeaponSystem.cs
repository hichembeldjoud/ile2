using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using SurvivalIsland.AI;
using SurvivalIsland.Audio;

namespace SurvivalIsland.Combat
{
    /// <summary>
    /// Type d'arme de chasse équipée.
    /// </summary>
    public enum WeaponType
    {
        Bow,    // Arc de chasse (silencieux, flèche balistique, tension progressive)
        Rifle   // Fusil de chasse (puissant, portée tendue, détonation bruyante)
    }

    /// <summary>
    /// Système complet de gestion des armes, visée et tir de projectiles.
    /// Gère la visée avec zoom de caméra (FOV), la tension de corde d'arc ou la cadence de fusil,
    /// la trajectoire précise vers le réticule et l'impact sonore sur la faune.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival Island/Combat/Weapon System")]
    public class WeaponSystem : MonoBehaviour
    {
        #region Type d'Arme & Équipement
        [Header("--- Configuration de l'Arme ---")]
        [Tooltip("Type d'arme actuellement équipée.")]
        [SerializeField]
        private WeaponType currentWeaponType = WeaponType.Bow;

        [Tooltip("Point de sortie du projectile (bout du canon ou repose-flèche de l'arc).")]
        [SerializeField]
        private Transform firePoint;

        [Tooltip("Préfabriqué du projectile tiré (Flèche pour Arc, Balle pour Fusil).")]
        [SerializeField]
        private GameObject projectilePrefab;
        #endregion

        #region Statistiques de Tir
        [Header("--- Paramètres Balistiques & Cadence ---")]
        [Tooltip("Dégâts de base de l'arme.")]
        [SerializeField]
        private float baseDamage = 45f;

        [Tooltip("Temps nécessaire pour bander l'arc au maximum (secondes).")]
        [SerializeField]
        private float bowMaxDrawTime = 1.1f;

        [Tooltip("Cadence de tir du fusil (délai minimal entre deux coups en secondes).")]
        [SerializeField]
        private float rifleFireRate = 0.85f;

        [Tooltip("Portée maximale de visée assistée vers le centre de l'écran.")]
        [SerializeField]
        private float maxAimDistance = 150f;
        #endregion

        #region Visée & Caméra
        [Header("--- Mode Visée (Aiming) ---")]
        [Tooltip("Champ de vision standard de la caméra.")]
        [SerializeField]
        private float defaultCameraFOV = 60f;

        [Tooltip("Champ de vision en mode visée (zoom).")]
        [SerializeField]
        private float aimCameraFOV = 42f;

        [Tooltip("Vitesse de transition du zoom de caméra.")]
        [SerializeField]
        private float zoomSpeed = 8f;

        [Tooltip("Activer le réticule à l'écran lors de la visée.")]
        [SerializeField]
        private bool showCrosshair = true;
        #endregion

        #region Audio & Effets Visuels (VFX)
        [Header("--- Effets Visuels & Sonores ---")]
        [Tooltip("Clip audio de tension de corde d'arc.")]
        [SerializeField]
        private AudioClip bowDrawAudio;

        [Tooltip("Clip audio de décoche d'arc (tir).")]
        [SerializeField]
        private AudioClip bowReleaseAudio;

        [Tooltip("Clip audio de coup de feu (fusil).")]
        [SerializeField]
        private AudioClip rifleShotAudio;

        [Tooltip("Flash de bouche de canon pour le fusil (Muzzle Flash VFX).")]
        [SerializeField]
        private ParticleSystem muzzleFlashVfx;

        [Tooltip("Rayon d'alerte sonore des animaux lors d'un coup de feu de fusil.")]
        [SerializeField]
        private float gunshotAlertRadius = 45f;
        #endregion

        #region Événements C#
        public event Action<bool> OnAimStateChanged;
        public event Action<WeaponType> OnWeaponFired;
        public event Action<WeaponType> OnWeaponSwitched;
        #endregion

        #region Variables Internes
        private Camera mainCamera;
        private bool isAiming = false;
        private float currentDrawTime = 0f;
        private float nextFireTime = 0f;
        private bool isDrawingBow = false;
        #endregion

        #region Propriétés Publiques
        public WeaponType CurrentWeaponType => currentWeaponType;
        public bool IsAiming => isAiming;
        public float DrawProgress => Mathf.Clamp01(currentDrawTime / bowMaxDrawTime);
        #endregion

        #region Cycle de Vie Unity
        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                defaultCameraFOV = mainCamera.fieldOfView;
            }

            EnsureFirePointExists();
        }

        private void Update()
        {
            ProcessWeaponSwitchInputs();
            ProcessAiming();
            ProcessShooting();
            UpdateCameraZoom();
        }
        #endregion

        #region Changement d'Arme
        private void ProcessWeaponSwitchInputs()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame)
                {
                    SwitchWeapon(WeaponType.Bow);
                }
                else if (kb.digit2Key.wasPressedThisFrame)
                {
                    SwitchWeapon(WeaponType.Rifle);
                }
            }
        }

        /// <summary>
        /// Change l'arme équipée.
        /// </summary>
        public void SwitchWeapon(WeaponType newWeapon)
        {
            if (currentWeaponType == newWeapon) return;

            currentWeaponType = newWeapon;
            isAiming = false;
            isDrawingBow = false;
            currentDrawTime = 0f;

            Debug.Log($"<color=#03A9F4><b>[WeaponSystem]</b> Arme équipée : {currentWeaponType}</color>");
            OnWeaponSwitched?.Invoke(newWeapon);
        }
        #endregion

        #region Visée
        private void ProcessAiming()
        {
            bool aimInput = false;

            // Souris (Clic droit maintenu)
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                aimInput = true;
            }

            // Manette (Gâchette gauche LT/L2)
            Gamepad gp = Gamepad.current;
            if (gp != null && gp.leftTrigger.isPressed)
            {
                aimInput = true;
            }

            #if ENABLE_LEGACY_INPUT_MANAGER
            // Fallback Legacy Input
            try
            {
                if (Input.GetMouseButton(1))
                {
                    aimInput = true;
                }
            }
            catch { }
            #endif

            if (aimInput != isAiming)
            {
                isAiming = aimInput;
                OnAimStateChanged?.Invoke(isAiming);

                if (!isAiming)
                {
                    isDrawingBow = false;
                    currentDrawTime = 0f;
                }
            }
        }

        private void UpdateCameraZoom()
        {
            if (mainCamera == null) return;

            float targetFOV = isAiming ? aimCameraFOV : defaultCameraFOV;
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }
        #endregion

        #region Tir & Chasse
        private void ProcessShooting()
        {
            if (isAiming)
            {
                if (currentWeaponType == WeaponType.Bow)
                {
                    HandleBowMechanics();
                }
                else if (currentWeaponType == WeaponType.Rifle)
                {
                    HandleRifleMechanics();
                }
            }
            else
            {
                // Tir réflexe / Attaque de chasse directe au Clic Gauche
                bool shootPressed = false;
                Mouse mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame) shootPressed = true;
                Gamepad gp = Gamepad.current;
                if (gp != null && gp.rightTrigger.wasPressedThisFrame) shootPressed = true;

                #if ENABLE_LEGACY_INPUT_MANAGER
                try
                {
                    if (Input.GetMouseButtonDown(0)) shootPressed = true;
                }
                catch { }
                #endif

                if (shootPressed && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + (currentWeaponType == WeaponType.Bow ? 0.6f : rifleFireRate);
                    PerformDirectHuntAttack();
                }
            }
        }

        /// <summary>
        /// Attaque directe (au corps à corps ou tir sans visée) pour chasser facilement les animaux à proximité.
        /// </summary>
        private void PerformDirectHuntAttack()
        {
            float huntDamage = Mathf.Max(baseDamage, 55f);

            // 1. Détection ciblée devant la caméra / le joueur (portée 5.5m)
            Ray aimRay = (mainCamera != null) ?
                mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)) :
                new Ray(transform.position + Vector3.up * 1.2f, transform.forward);

            RaycastHit[] hits = Physics.SphereCastAll(aimRay.origin, 0.85f, aimRay.direction, 5.5f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var h in hits)
            {
                if (h.collider.transform.root == transform.root) continue; // Ignorer le joueur lui-même
                AnimalAI animal = h.collider.GetComponentInParent<AnimalAI>();
                if (animal != null && !animal.IsDead)
                {
                    animal.TakeDamage(huntDamage, h.point);
                    PlayHuntAttackEffects(h.point);
                    Debug.Log($"<color=#FF5722><b>[Chasse]</b> Coup ciblé infligé à {animal.AnimalName} ! (Dégâts: {huntDamage})</color>");
                    return;
                }
            }

            // 2. Détection de proximité immédiate à 360° (rayon 3.8m autour du joueur)
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position + Vector3.up * 0.8f, 3.8f, ~0, QueryTriggerInteraction.Ignore);
            AnimalAI closestAnimal = null;
            float closestDist = float.MaxValue;
            Vector3 hitPointFallback = transform.position + transform.forward * 1.5f;

            foreach (var col in nearbyColliders)
            {
                if (col.transform.root == transform.root) continue;
                AnimalAI animal = col.GetComponentInParent<AnimalAI>();
                if (animal != null && !animal.IsDead)
                {
                    float dist = Vector3.Distance(transform.position, animal.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestAnimal = animal;
                        hitPointFallback = col.ClosestPoint(transform.position);
                    }
                }
            }

            if (closestAnimal != null)
            {
                closestAnimal.TakeDamage(huntDamage, hitPointFallback);
                PlayHuntAttackEffects(hitPointFallback);
                Debug.Log($"<color=#FF5722><b>[Chasse]</b> Coup de mêlée porté à {closestAnimal.AnimalName} ! (Dégâts: {huntDamage})</color>");
                return;
            }

            // 3. Si aucun animal n'est à portée de frappe, tire un projectile balistique vers le réticule
            Vector3 shootDirection = (mainCamera != null) ? mainCamera.transform.forward : transform.forward;
            SpawnAndLaunchProjectile(shootDirection, 1.0f, baseDamage);
        }

        private void PlayHuntAttackEffects(Vector3 hitPoint)
        {
            if (currentWeaponType == WeaponType.Rifle)
            {
                if (rifleShotAudio != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(rifleShotAudio, 0.85f);
                }
                if (muzzleFlashVfx != null)
                {
                    muzzleFlashVfx.Play();
                }
            }
            else
            {
                if (bowReleaseAudio != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(bowReleaseAudio, 0.85f);
                }
            }
        }

        private void HandleBowMechanics()
        {
            Mouse mouse = Mouse.current;
            bool shootHold = (mouse != null && mouse.leftButton.isPressed);
            #if ENABLE_LEGACY_INPUT_MANAGER
            try { if (Input.GetMouseButton(0)) shootHold = true; } catch { }
            #endif

            if (shootHold)
            {
                if (!isDrawingBow)
                {
                    isDrawingBow = true;
                    if (bowDrawAudio != null && AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySFX(bowDrawAudio, 0.6f);
                    }
                }

                currentDrawTime += Time.deltaTime;
            }
            else if (isDrawingBow)
            {
                // Relâchement de la corde : Tir de la flèche
                if (currentDrawTime >= 0.2f) // Tension minimale pour tirer
                {
                    float drawPower = Mathf.Clamp01(currentDrawTime / bowMaxDrawTime);
                    FireBow(drawPower);
                }

                isDrawingBow = false;
                currentDrawTime = 0f;
            }
        }

        private void HandleRifleMechanics()
        {
            Mouse mouse = Mouse.current;
            bool shootPressed = (mouse != null && mouse.leftButton.wasPressedThisFrame);
            #if ENABLE_LEGACY_INPUT_MANAGER
            try { if (Input.GetMouseButtonDown(0)) shootPressed = true; } catch { }
            #endif

            if (shootPressed && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + rifleFireRate;
                FireRifle();
            }
        }

        private void FireBow(float drawPower)
        {
            Vector3 aimTarget = GetAimTargetPoint();
            Vector3 shootDirection = (aimTarget - firePoint.position).normalized;

            // Son de décoche
            if (bowReleaseAudio != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(bowReleaseAudio, 0.85f);
            }

            SpawnAndLaunchProjectile(shootDirection, 0.6f + (drawPower * 0.7f), baseDamage * (0.5f + drawPower * 0.8f));
            OnWeaponFired?.Invoke(WeaponType.Bow);
        }

        private void FireRifle()
        {
            Vector3 aimTarget = GetAimTargetPoint();
            Vector3 shootDirection = (aimTarget - firePoint.position).normalized;

            // VFX Flamme de canon
            if (muzzleFlashVfx != null)
            {
                muzzleFlashVfx.Play();
            }

            // Son de coup de feu puissant
            if (rifleShotAudio != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(rifleShotAudio, 1.0f);
            }

            // Alerte de la faune sauvage aux alentours
            AlertNearbyAnimals(transform.position, gunshotAlertRadius);

            SpawnAndLaunchProjectile(shootDirection, 2.2f, baseDamage * 1.5f);
            OnWeaponFired?.Invoke(WeaponType.Rifle);
        }

        private void SpawnAndLaunchProjectile(Vector3 direction, float speedMultiplier, float finalDamage)
        {
            Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position + Vector3.up * 1.4f + transform.forward * 0.8f;
            if (Vector3.Distance(spawnPos, transform.position) < 0.8f)
            {
                spawnPos = transform.position + Vector3.up * 1.4f + transform.forward * 0.9f;
            }

            GameObject projObj;
            if (projectilePrefab != null)
            {
                projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
            }
            else
            {
                // Fallback propre : Crée une flèche cylindrique si aucun préfabriqué n'est encore assigné
                projObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                projObj.name = "Arrow_Projectile";
                projObj.transform.localScale = new Vector3(0.04f, 0.4f, 0.04f);
                projObj.transform.position = spawnPos;
                projObj.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90, 0, 0);

                Collider c = projObj.GetComponent<Collider>();
                if (c != null) c.isTrigger = true;
            }

            Projectile projectile = projObj.GetComponent<Projectile>();
            if (projectile == null)
            {
                projectile = projObj.AddComponent<Projectile>();
            }

            projectile.Launch(direction, speedMultiplier, finalDamage / baseDamage);
        }

        private Vector3 GetAimTargetPoint()
        {
            if (mainCamera == null) return firePoint.position + transform.forward * 50f;

            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            return ray.origin + ray.direction * maxAimDistance;
        }

        private void AlertNearbyAnimals(Vector3 center, float radius)
        {
            Collider[] colliders = Physics.OverlapSphere(center, radius);
            foreach (var col in colliders)
            {
                AnimalAI animal = col.GetComponentInParent<AnimalAI>();
                if (animal != null && !animal.IsDead)
                {
                    // L'animal entend la détonation et fuit immédiatement
                    animal.TakeDamage(0f, center); // 0 dégât mais déclenche la fuite panique
                }
            }
        }
        #endregion

        #region Helpers & Initialisation
        private void EnsureFirePointExists()
        {
            if (firePoint == null)
            {
                Transform existing = transform.Find("FirePoint");
                if (existing != null)
                {
                    firePoint = existing;
                }
                else
                {
                    GameObject fpObj = new GameObject("FirePoint");
                    fpObj.transform.SetParent(transform);
                    fpObj.transform.localPosition = new Vector3(0.35f, 1.4f, 0.6f);
                    firePoint = fpObj.transform;
                }
            }
        }

        private void OnGUI()
        {
            // Dessine un réticule minimaliste et élégant au centre de l'écran lors de la visée
            if (isAiming && showCrosshair)
            {
                float centerX = Screen.width / 2f;
                float centerY = Screen.height / 2f;
                float size = 8f;

                Color crosshairColor = new Color(1f, 1f, 1f, 0.85f);
                Texture2D tex = Texture2D.whiteTexture;
                GUI.color = crosshairColor;

                // Lignes horizontale et verticale fines
                GUI.DrawTexture(new Rect(centerX - size, centerY - 1, size * 2, 2), tex);
                GUI.DrawTexture(new Rect(centerX - 1, centerY - size, 2, size * 2), tex);
            }
        }
        #endregion
    }
}
