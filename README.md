# Hamish McHamish and the Lost Loot

An educational 2D game where players roam a procedurally generated field, dig up buried chests guarded by insects, and answer multiple choice computer science questions to open them. I built it solo in Unity 2021.3.45f2 as my BSc Computer Science dissertation at the University of St Andrews. Correct answers pay out gold and gems that buy permanent upgrades and cosmetics, and lecturers can swap in their own question sets from CSV files. All code and pixel art are my own except where credited below.

## How to run it

1. Install Unity Hub, then Unity Editor 2021.3.45f2 from the Installs tab.
2. In Unity Hub choose Open and select this repository folder. The first import rebuilds the `Library/` cache and takes a few minutes.
3. Open `Assets/Scenes/LobbyScene.unity` and press Play.

Move with WASD or the arrow keys. The mouse aims a custom cursor whose particle trail runs hot and cold as it nears buried chests. Left click digs and attacks, and Escape opens the pause menu, which is also where quest rewards are collected.

## How it works

**Quests.** `Assets/Scripts/QuestManager.cs` listens to a static event bus in `GameEvents.cs`: gameplay raises events such as chest opened or enemy defeated, and the manager counts them against up to four `QuestDefinition` ScriptableObjects drawn at random each level. Completed quest ids persist, so a quest never repeats across runs.

**Progression.** Currency earned mid run sits in a delta that `GoldManager.cs` and `GemsManager.cs` bank only on level completion; dying or quitting discards it. Every save key is namespaced by profile in `GameProfileContext.cs`, so an imported curriculum keeps its own gold, upgrades and cosmetics.

**Enemy AI.** Each enemy splits movement (`AntPathing.cs`, `FlyPathing.cs`) from stats and combat (`AntEnemy.cs`, `FlyEnemy.cs`). Ground enemies wander near a home point and chase with hysteresis between detect and lose radii; flies add Perlin noise jitter to their chase target so each one weaves differently.

**Procedural generation.** `WorldGenerator.cs` fills the tilemap with randomly rotated tiles under the constraint that no tile and rotation pair repeats within any 3x3 neighbourhood. `EntitySpawner.cs` places chests by rejection sampling with a minimum spacing, then seeds each with a shuffled question and surrounding enemies.

**UI.** All HUD animation is hand rolled coroutines over TextMeshPro in `PlayerUI.cs` and `QuizUI.cs`. The quiz shuffles answer positions and fades the buttons in before enabling them, on unscaled time, since the game clock is frozen during questions.

### Custom question import

A lecturer supplies three CSVs, one per level, ten rows each, with columns `question, correctAnswer, wrong1, wrong2, wrong3`. `Assets/Scripts/CustomCsvRunManager.cs` parses them with an RFC 4180 splitter, stores the run as JSON in the user data folder, and plays it under a separate save profile. The default questions ship as ScriptableObjects in `Assets/Prefabs/Questions/`.

## Known issues and what I would do differently

`GameManager.cs` grew to 850 lines and handles scenes, pausing, currency and upgrades; I would split it into smaller components. Saves use PlayerPrefs, which worked at this scope, but a versioned JSON save file would be easier to debug. Two small bugs remain: the lootbox notification stays hidden when more than one lootbox arrives at once, and the ultimate pickup shows its message without applying the stat boosts. There are no automated tests, which I felt whenever a change touched the save path.

## Credits

- Showpop font by [Khurasan Studio](https://khurasanstudio.com), free for personal and commercial use per the readme bundled with the download.
- TextMesh Pro by Unity Technologies under the Unity Companion License, including Liberation Sans (SIL OFL 1.1) and the EmojiOne sample sprites (see `Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt`).
- The University of St Andrews crest and the Java logo appear on two cosmetic shirts; both are trademarks of their owners, used decoratively in a student project.

## Licence

MIT. The Showpop font and the other credited third party content keep their own licences.
