# 🌴 Survival Island - Architecture & Guide Technique Complet

Bienvenue dans l'architecture technique du projet **Survival Island** ! Ce document résume l'organisation des dossiers, les fonctionnalités développées à travers les 4 phases, ainsi que le guide pour importer vos modèles Sketchfab et animations Mixamo.

---

## 📁 1. Organisation des Dossiers (Prêt pour Sketchfab & Mixamo)

L'ensemble de vos ressources est organisé sous `Assets/_Project/` :

```
Assets/_Project/
├── Art/
│   ├── Animations/
│   │   ├── Characters/       <- Vos animations Mixamo du joueur (Idle, Walk, Run, Sneak, Jump, Death)
│   │   └── Animals/          <- Vos animations Mixamo des animaux (Idle, Walk, Run/Flee, Death)
│   ├── Models/
│   │   ├── Characters/       <- Modèles 3D FBX de personnages (Mixamo / Sketchfab)
│   │   ├── Animals/          <- Modèles 3D animaux (Sketchfab)
│   │   ├── Environment/      <- Végétation, rochers, palmiers, sable (Sketchfab)
│   │   ├── Weapons/          <- Modèles d'arc, flèches, fusil (Sketchfab)
│   │   └── Items/            <- Viande, noix de coco, outils (Sketchfab)
│   ├── Materials/Skybox/     <- Matériaux de ciel dynamique
│   ├── Textures/             <- Textures PBR (Albedo, Normal, Metallic/Smoothness, AO)
│   └── VFX/                  <- Particules météo, éclaboussures, impacts
├── Audio/
│   ├── Ambience/             <- Pistes d'ambiance en boucle (Plage/Vagues, Jungle/Insectes, Vent d'altitude)
│   ├── SFX/                  <- Bruits de pas, tir, impact, tonnerre, survie, animaux
│   └── Music/                <- Musiques d'ambiance et thèmes
├── Prefabs/                  <- Prefabs configurés par catégorie
└── Scripts/                  <- Architecture C# modulaire par domaine
```

---

## ⚡ 2. Menus d'Automatisation en 1 Clic (Unity Editor)

Tous les systèmes peuvent être créés, interconnectés et placés dans votre scène active via le menu supérieur de Unity :

🌟 **MENU TOUT-EN-UN (RECOMMANDÉ) :**  
👉 **`Survival Island` > `🌟 METTRE EN PLACE L'ÎLE TROPICALE RÉALISTE (Mer, Ciel, Animaux, Palmiers)`**  
*Génère instantanément le terrain 3D de l'île (600x600m), l'océan turquoise des Caraïbes avec vagues animées, 55 palmiers 3D, les récifs côtiers, remplace la capsule par un vrai survivant avec sac à dos, remplace les cubes par des sangliers sculptés avec défenses, et applique le ciel azur et le post-processing cinématographique (ACES/Bloom).*

**Menus modulaires par phase :**

1. 👉 **`Survival Island` > `Phase 1 - Configurer l'Environnement et l'Audio`**
   - Soleil, Lune et Ciel avec `DayNightCycle`.
   - Système météo avec particules de pluie tropicale et orages `WeatherSystem`.
   - `AudioManager` (double canal crossfade) et les 3 zones audio triggers (`Beach`, `Jungle`, `Mountain`).

2. 👉 **`Survival Island` > `Phase 2 - Configurer le Joueur et le HUD de Survie`**
   - Génère l'**Animator Controller Mixamo** (`PlayerAnimatorController.controller`).
   - Crée le **Joueur** avec `CharacterController`, `PlayerController` 3ème personne, `SurvivalSystem`.
   - Configure la **Main Camera** avec `ThirdPersonCamera` (suivi orbital, zoom molette, anti-collision).
   - Génère le Canvas HUD avec les jauges vitales de survie (`SurvivalUI`).

3. 👉 **`Survival Island` > `Phase 3 - Configurer la Chasse, Animaux et Armes`**
   - Génère l'**Animator Controller** des animaux (`AnimalAnimatorController.controller`).
   - Crée le groupe d'animaux (Sangliers) avec `NavMeshAgent` et IA acoustique (`AnimalAI`).
   - Équipe le joueur du système d'armes (`WeaponSystem` pour Arc et Fusil avec visée zoom).
   - Crée le préfabriqué de loot `Meat_Loot.prefab` qui restaure la faim dans le `SurvivalSystem`.

4. 👉 **`Survival Island` > `Phase 4 - Configurer la Minimap, Quêtes et HUD Complet`**
   - Crée la caméra orthographique de la Minimap (`Minimap`) avec texture de rendu dynamique et affichage RawImage.
   - Initialise le gestionnaire de quêtes (`QuestManager`) avec objectifs de survie et de chasse.
   - Configure le gestionnaire d'interface central (`UIManager`) qui synchronise l'ensemble du HUD (jauges, minimap, quêtes, horloge, météo, game over).

---

## 🛠️ 3. Récapitulatif des Phases de Développement

### ☀️ Phase 1 : Environnement, Météo & Audio Spatial
- **`DayNightCycle.cs`** : Cycle jour/nuit réaliste (latitude 15°N), dégradés PBR (aube dorée, zénith blanc éclatant, crépuscule carmin, nuit bleutée), brouillard dynamique et gestion intelligente des ombres GPU.
- **`WeatherSystem.cs`** : Ciel dégagé, nuages, pluie tropicale et orages. Particules suivant la caméra, assombrissement du soleil et synchronisation des éclairs avec le bruit du tonnerre.
- **`AudioManager.cs` & `AmbientAudioZone.cs`** : Crossfader à double source sans coupure. Triggers spatiaux (Plage, Jungle, Montagne) avec Gizmos de couleur dans l'éditeur.

### 🏃 Phase 2 : Personnage Mixamo & Système de Survie
- **`PlayerController.cs`** : Contrôleur 3ème personne complet (Idle, Walk, Run, Sneak, Jump, Dead) avec modification dynamique de la hauteur du collider pour passer sous les obstacles bas et bruits de pas synchronisés.
- **`ThirdPersonCamera.cs`** : Caméra orbitale fluide, zoom molette et détection de collision décor (`SphereCast`).
- **`SurvivalSystem.cs`** : Faim, Soif, Santé, Endurance. Dégâts d'inanition et de déshydratation à zéro, régénération passive si bien nourri et hydraté (>70%).
- **`SurvivalUI.cs`** : Liaison temps réel avec lissage (Lerp) pour les barres de statut.

### 🐗 Phase 3 : Écosystème, Animaux & Chasse
- **`AnimalAI.cs`** : Navigation sur NavMesh, errance aléatoire (`Wander`), détection acoustique réaliste (le joueur en sprint alerte à 20m, en marche à 9m, et en **Sneak à seulement 3.2m** !). Fuite panique (`Flee`), cri d'agonie et loot de viande.
- **`WeaponSystem.cs` & `Projectile.cs`** : Touche [1] pour Arc (silencieux, flèche balistique avec gravité), Touche [2] pour Fusil (rapide, détonation sonore qui alerte la faune à 45m). Mode visée (Clic droit) avec zoom FOV et réticule central.
- **`MeatItem.cs`** : Consommable au sol (touche [E]) restaurant instantanément +35 Faim et +5 Santé avec effet sonore de repas.

### 🗺️ Phase 4 : Carte, Interface & Quêtes
- **`Minimap.cs`** : Caméra orthographique au-dessus du joueur projetant une RenderTexture dynamique sur une RawImage avec boussole/orientation du joueur.
- **`QuestManager.cs`** : Système de quêtes (ex: "Trouver de la nourriture", "Chasser un animal", "Explorer l'île", "Survivre à la nuit") interconnecté avec les événements de gameplay (mort d'animal, repas de viande).
- **`UIManager.cs`** : Chef d'orchestre de l'interface reliant en direct les barres de survie, la minimap, le panneau d'objectifs, l'horloge in-game ("14:20"), l'indicateur météo et l'écran de mort avec réapparition.

---

## 🎮 Commandes du Joueur en Jeu

| Action | Clavier (AZERTY / QWERTY) | Manette (Gamepad) |
|---|---|---|
| **Déplacement** | ZQSD / WASD | Stick Gauche |
| **Caméra & Orientation** | Souris | Stick Droit |
| **Courir (Sprint)** | Maj Gauche (Shift) | Clic Stick Gauche (L3) |
| **Mode Furtif (Sneak)** | Touche [C] ou [Ctrl] | Bouton B / Rond |
| **Sauter** | Barre Espace | Bouton A / Croix |
| **Viser** | Clic Droit (Maintenu) | Gâchette Gauche (LT / L2) |
| **Tirer** | Clic Gauche | Gâchette Droite (RT / R2) |
| **Changer d'arme** | Touches [1] (Arc) et [2] (Fusil) | D-Pad |
| **Interagir / Manger** | Touche [E] | Bouton X / Carré |
| **Déverrouiller le curseur** | Touche Échap | Start |
