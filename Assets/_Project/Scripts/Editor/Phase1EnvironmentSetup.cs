#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SurvivalIsland.Environment;
using SurvivalIsland.Audio;

namespace SurvivalIsland.Editor
{
    /// <summary>
    /// Outil d'automatisation Editor pour configurer l'environnement,
    /// le cycle jour/nuit, le système météo et les zones audio de la Phase 1.
    /// </summary>
    public static class Phase1EnvironmentSetup
    {
        [MenuItem("Survival Island/Phase 1 - Configurer l'Environnement et l'Audio", false, 10)]
        public static void SetupPhase1Scene()
        {
            Undo.SetCurrentGroupName("Setup Phase 1 Survival Island");
            int group = Undo.GetCurrentGroup();

            Debug.Log("<color=#4CAF50><b>[Survival Island]</b> Démarrage de la configuration automatique de la Phase 1...</color>");

            // 1. Racine Environnement
            GameObject envRoot = GetOrCreateRoot("--- ENVIRONMENT & ATMOSPHERE ---");

            // 2. Soleil et Lune
            Light sunLight = SetupDirectionalLight(envRoot.transform, "Sun_DirectionalLight", new Color(1f, 0.95f, 0.88f), 1.3f, 50f);
            Light moonLight = SetupDirectionalLight(envRoot.transform, "Moon_DirectionalLight", new Color(0.6f, 0.75f, 1f), 0.25f, -50f);

            // 3. DayNightCycle
            DayNightCycle dayNight = envRoot.GetComponent<DayNightCycle>();
            if (dayNight == null)
            {
                dayNight = Undo.AddComponent<DayNightCycle>(envRoot);
            }
            dayNight.AutoAssignLights(sunLight, moonLight);

            // 4. Weather System & Particules de Pluie
            GameObject weatherObj = GetOrCreateChild(envRoot.transform, "WeatherSystem");
            WeatherSystem weather = weatherObj.GetComponent<WeatherSystem>();
            if (weather == null)
            {
                weather = Undo.AddComponent<WeatherSystem>(weatherObj);
            }

            // Création du Particle System de pluie s'il n'existe pas
            ParticleSystem rainPS = weatherObj.GetComponentInChildren<ParticleSystem>();
            if (rainPS == null)
            {
                rainPS = CreateTropicalRainParticleSystem(weatherObj.transform);
            }

            // 5. Racine Audio
            GameObject audioRoot = GetOrCreateRoot("--- AUDIO SYSTEM ---");
            AudioManager audioManager = audioRoot.GetComponent<AudioManager>();
            if (audioManager == null)
            {
                audioManager = Undo.AddComponent<AudioManager>(audioRoot);
            }

            // 6. Zones d'Ambiance Triggers
            GameObject zonesRoot = GetOrCreateRoot("--- AMBIENT AUDIO ZONES ---");
            CreateAmbientZone(zonesRoot.transform, "BeachZone_Trigger", AmbientZoneType.Beach, new Vector3(0, 0, -100), new Vector3(300, 40, 100), 0);
            CreateAmbientZone(zonesRoot.transform, "JungleZone_Trigger", AmbientZoneType.Jungle, new Vector3(0, 0, 50), new Vector3(250, 60, 200), 1);
            CreateAmbientZone(zonesRoot.transform, "MountainZone_Trigger", AmbientZoneType.Mountain, new Vector3(0, 50, 150), new Vector3(150, 100, 150), 2);

            Undo.CollapseUndoOperations(group);
            Debug.Log("<color=#4CAF50><b>[Survival Island]</b> Configuration Phase 1 terminée avec succès ! Tous les GameObjects sont en place et interconnectés.</color>");
        }

        private static GameObject GetOrCreateRoot(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            }
            return obj;
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child.gameObject;

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.localPosition = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            return obj;
        }

        private static Light SetupDirectionalLight(Transform parent, string name, Color color, float intensity, float initialAngleX)
        {
            Transform existing = parent.Find(name);
            GameObject lightObj = existing != null ? existing.gameObject : new GameObject(name);

            if (existing == null)
            {
                lightObj.transform.SetParent(parent);
                lightObj.transform.rotation = Quaternion.Euler(initialAngleX, 30f, 0f);
                Undo.RegisterCreatedObjectUndo(lightObj, "Create " + name);
            }

            Light lightComp = lightObj.GetComponent<Light>();
            if (lightComp == null)
            {
                lightComp = Undo.AddComponent<Light>(lightObj);
            }

            lightComp.type = LightType.Directional;
            lightComp.color = color;
            lightComp.intensity = intensity;
            lightComp.shadows = LightShadows.Soft;

            return lightComp;
        }

        private static ParticleSystem CreateTropicalRainParticleSystem(Transform parent)
        {
            GameObject rainObj = new GameObject("TropicalRain_VFX");
            rainObj.transform.SetParent(parent);
            rainObj.transform.localPosition = new Vector3(0, 20, 0);
            rainObj.transform.localRotation = Quaternion.Euler(90, 0, 0); // Dirigé vers le bas
            Undo.RegisterCreatedObjectUndo(rainObj, "Create Rain VFX");

            ParticleSystem ps = rainObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = 1.2f;
            main.startSpeed = 35f; // Pluie tropicale rapide
            main.startSize = 0.08f;
            main.startColor = new Color(0.85f, 0.92f, 1f, 0.55f);
            main.maxParticles = 5000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f; // Contrôlé par WeatherSystem

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(45f, 45f, 1f);

            var renderer = rainObj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.04f;
            renderer.lengthScale = 2.5f;

            return ps;
        }

        private static void CreateAmbientZone(Transform parent, string name, AmbientZoneType type, Vector3 center, Vector3 size, int priority)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return;

            GameObject zoneObj = new GameObject(name);
            zoneObj.transform.SetParent(parent);
            zoneObj.transform.position = center;
            Undo.RegisterCreatedObjectUndo(zoneObj, "Create " + name);

            BoxCollider box = zoneObj.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;

            AmbientAudioZone ambientZone = zoneObj.AddComponent<AmbientAudioZone>();
            ambientZone.ConfigureZone(type, name, priority);
        }
    }
}
#endif
