using System;
using System.Collections.Generic;
using UnityEngine;
using SurvivalIsland.AI;
using SurvivalIsland.Survival;
using SurvivalIsland.Audio;

namespace SurvivalIsland.Quests
{
    /// <summary>
    /// Définition d'un objectif ou d'une quête de survie.
    /// </summary>
    [System.Serializable]
    public class Quest
    {
        public string id;
        public string title;
        [TextArea(2, 4)]
        public string description;
        public int currentAmount;
        public int targetAmount;
        public bool isCompleted;
        public bool isActive;

        public Quest(string id, string title, string description, int targetAmount)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.targetAmount = targetAmount;
            this.currentAmount = 0;
            this.isCompleted = false;
            this.isActive = false;
        }

        public string GetProgressString()
        {
            return $"{currentAmount} / {targetAmount}";
        }
    }

    /// <summary>
    /// Gestionnaire centralisé des quêtes et objectifs du joueur.
    /// Donne des objectifs de survie (ex: "Trouver de la nourriture", "Chasser un animal"),
    /// écoute les événements de gameplay et notifie l'interface UI.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Survival Island/Quests/Quest Manager")]
    public class QuestManager : MonoBehaviour
    {
        #region Singleton
        public static QuestManager Instance { get; private set; }
        #endregion

        #region Quêtes Configurées
        [Header("--- Liste des Quêtes ---")]
        [Tooltip("Liste de toutes les quêtes du jeu.")]
        [SerializeField]
        private List<Quest> quests = new List<Quest>();

        [Tooltip("Index de la quête active courante.")]
        [SerializeField]
        private int activeQuestIndex = 0;
        #endregion

        #region Audio de Quête
        [Header("--- Audio Quêtes ---")]
        [Tooltip("Son joué lors de la complétion d'un objectif de quête.")]
        [SerializeField]
        private AudioClip questCompleteAudio;
        #endregion

        #region Événements C#
        public event Action<Quest> OnQuestStarted;
        public event Action<Quest> OnQuestUpdated;
        public event Action<Quest> OnQuestCompleted;
        #endregion

        #region Cycle de Vie Unity
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitializeDefaultQuestsIfEmpty();
        }

        private void Start()
        {
            // Lance la première quête active
            if (quests.Count > 0 && activeQuestIndex < quests.Count)
            {
                SetActiveQuest(activeQuestIndex);
            }
        }
        #endregion

        #region Gestion des Quêtes
        private void InitializeDefaultQuestsIfEmpty()
        {
            if (quests == null || quests.Count == 0)
            {
                quests = new List<Quest>
                {
                    new Quest("find_food", "Trouver de la nourriture", "Mangez de la viande de gibier pour apaiser votre faim.", 1),
                    new Quest("hunt_animal", "Chasser un animal", "Traquez et éliminez un sanglier sauvage à l'arc ou au fusil.", 1),
                    new Quest("explore_island", "Explorer l'île", "Découvrez les différents biotopes de l'île (Plage, Jungle, Montagne).", 3),
                    new Quest("survive_night", "Survivre à la nuit", "Tenez bon jusqu'au lever du soleil.", 1)
                };
            }
        }

        /// <summary>
        /// Renvoie la quête active affichée à l'écran.
        /// </summary>
        public Quest GetActiveQuest()
        {
            if (quests != null && activeQuestIndex >= 0 && activeQuestIndex < quests.Count)
            {
                return quests[activeQuestIndex];
            }
            return null;
        }

        /// <summary>
        /// Active une quête par son index.
        /// </summary>
        public void SetActiveQuest(int index)
        {
            if (index < 0 || index >= quests.Count) return;

            activeQuestIndex = index;
            for (int i = 0; i < quests.Count; i++)
            {
                quests[i].isActive = (i == activeQuestIndex);
            }

            OnQuestStarted?.Invoke(quests[activeQuestIndex]);
        }

        /// <summary>
        /// Fait progresser un objectif de quête.
        /// </summary>
        /// <param name="questId">Identifiant unique de la quête</param>
        /// <param name="amount">Progression ajoutée</param>
        public void ProgressQuest(string questId, int amount = 1)
        {
            Quest quest = quests.Find(q => q.id == questId && !q.isCompleted);
            if (quest == null) return;

            quest.currentAmount += amount;
            OnQuestUpdated?.Invoke(quest);

            Debug.Log($"<color=#FF9800><b>[Quête]</b> Progression : '{quest.title}' ({quest.GetProgressString()})</color>");

            if (quest.currentAmount >= quest.targetAmount)
            {
                CompleteQuest(quest);
            }
        }

        private void CompleteQuest(Quest quest)
        {
            quest.currentAmount = quest.targetAmount;
            quest.isCompleted = true;

            Debug.Log($"<color=#4CAF50><b>[Quête Terminée]</b> Félicitations ! '{quest.title}' accomplie !</color>");

            if (questCompleteAudio != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(questCompleteAudio, 0.95f);
            }

            OnQuestCompleted?.Invoke(quest);

            // Passe automatiquement à la quête suivante si disponible
            if (activeQuestIndex + 1 < quests.Count)
            {
                SetActiveQuest(activeQuestIndex + 1);
            }
        }

        /// <summary>
        /// Notifie la chasse d'un animal (appelé lors de la mort d'un AnimalAI).
        /// </summary>
        public void NotifyAnimalHunted()
        {
            ProgressQuest("hunt_animal", 1);
        }

        /// <summary>
        /// Notifie la consommation de nourriture (appelé par MeatItem ou SurvivalSystem).
        /// </summary>
        public void NotifyFoodEaten()
        {
            ProgressQuest("find_food", 1);
        }

        /// <summary>
        /// Notifie la découverte d'une nouvelle zone.
        /// </summary>
        public void NotifyZoneExplored()
        {
            ProgressQuest("explore_island", 1);
        }
        #endregion
    }
}
