========================================================================
              🦁 SURVIVAL ISLAND - ÉCOSYSTÈME & PACK D'ANIMAUX 🐾
========================================================================

1. EMPLACEMENT DES FICHIERS SOURCES :
   - Pack 3D complet : Assets/ultimate-animal-pack-100-animals-50-off/
   - Modèle FBX contenant les espèces : Realistic Animated Pack.fbx
   - Textures PBR : textures/ (Image_0.png à Image_60.png)
   - Loup Sauvage Réaliste URP : Assets/Wolf/

2. PREFABS PRÊTS À L'EMPLOI :
   Tous les prefabs générés sont stockés dans :
   - Assets/Animals/ (raccourci direct)
   - Assets/_Project/Prefabs/Animals/ :
     * Predateurs/ : Ours, Loup, Lionne, Hyène, Alligator, Blaireau à miel
     * Herbivores/ : Sanglier, Cerf, Élan, Buffle, Renard, Lapin, Gnou, Chèvre, Mouflon, Phacochère, Cheval
     * Oiseaux/    : Oie sauvage, Pintade, Faisan

3. COMPOSANTS DE CHAQUE ANIMAL :
   Chaque animal possède :
   - Le modèle 3D centré avec pieds au niveau du sol (Y=0)
   - CapsuleCollider adapté à sa taille
   - NavMeshAgent (vitesse de marche et de fuite/poursuite)
   - AnimalAI configuré selon son espèce (Herbivore fuyard ou Prédateur agressif)
   - Système de loot de viande (MeatItem) à la mort
   - Animator connecté à l'AnimalAnimatorController

4. COMMANDES MENU UNITY :
   En haut dans la barre de menus de Unity :
   - "Survival Island" > "🦁 1. Extraire & Créer TOUS les Animaux dans Assets"
   - "Survival Island" > "🐾 2. Peupler l'Île avec Tous les Animaux"
   - "Survival Island" > "🌟 METTRE EN PLACE L'ÎLE TROPICALE RÉALISTE"
========================================================================
