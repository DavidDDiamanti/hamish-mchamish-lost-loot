# Hamish McHamish and the Lost Loot

A top-down rogue-lite educational game where players explore procedurally generated levels as a cat, dig up chests guarded by insect enemies, and answer multiple choice computer science questions to open them. Correct answers feed a streak multiplier and two currencies that buy permanent upgrades and cosmetics. I built it solo in Unity 2021.3.45f2 with C# as my BSc Computer Science dissertation at the University of St Andrews, adapting engagement systems from commercial games (progression loops, tiered upgrades, loot boxes) to established learning frameworks: Self-Determination Theory, Cognitive Load Theory and Flow. The character is modelled on Hamish McHamish, a real cat who was a local celebrity in St Andrews.

[screenshot placeholder: replace with a gameplay capture]

## How to run it

1. Install Unity Hub, then Unity Editor 2021.3.45f2 from the Installs tab.
2. In Unity Hub choose Open and select this repository folder. The first import rebuilds the `Library/` cache and takes a few minutes.
3. Open `Assets/Scenes/LobbyScene.unity` and press Play.

Move with WASD or the arrow keys, dig and attack with left click, and pause with Escape, which is also where quest rewards are collected. The mouse aims a custom cursor whose particle trail runs hot and cold as it nears buried chests, so finding loot needs no instructions.

## How it works

Each level covers one week of lecture material through ten questions, a deliberate cognitive load decision: content arrives in small units on a schedule that mirrors the course. `Assets/Scripts/WorldGenerator.cs` fills the tilemap under the constraint that no tile and rotation pair repeats within any 3x3 neighbourhood, and `EntitySpawner.cs` places chests by rejection sampling with a minimum spacing, so retrying a level produces a fresh layout. When a chest opens, `QuizUI.cs` shuffles the answer positions so players cannot memorise a pattern, and the game freezes completely during a question to remove time pressure from the learning task.

The economy is built for rogue-lite stakes. Currency earned mid run accumulates in a delta that `GoldManager.cs` and `GemsManager.cs` bank only on level completion; dying or quitting rolls it back. Gold buys tiered permanent upgrades, including two transformative unlocks (an autoclicker and a cursor that points toward nearby chests), while gems buy cosmetics. Quests are tracked through a static event bus in `GameEvents.cs`: gameplay raises events such as chest opened or enemy defeated, and `QuestManager.cs` counts them without any direct coupling to the systems that fire them.

Enemies split movement from combat: `AntPathing.cs` wanders between waypoints and chases with hysteresis between detect and lose radii, while `FlyPathing.cs` adds Perlin noise jitter so each fly weaves differently. Ants appear from level one, beetles from two, flies from three, so new behaviour arrives alongside new material.

### Custom question import

Any lecturer can run the game on their own course. Three CSVs, one per level with columns `question, correctAnswer, wrong1, wrong2, wrong3`, are parsed by `Assets/Scripts/CustomCsvRunManager.cs` with an RFC 4180 splitter and stored as JSON. Because every save key is namespaced by profile in `GameProfileContext.cs`, a custom curriculum gets its own independent progression, and the chest and reward pipeline runs identically on either question source.

## Evaluation

I ran a user study with 15 computer science students at St Andrews, with ethics approval from the School; 9 completed the full gameplay survey. All 9 rated the gameplay enjoyable, averaging 4.67 out of 5, and perceived reinforcement of course material averaged 3.33 out of 5. Rewards for correct answers, answer streaks and unlocks rated as the most motivating features; combat rated lowest. The study measured perceived reinforcement rather than learning outcomes, and the sample is too small for statistical claims.

## Credits

- A friend collaborated with me on the design of the main character and the game logo. The remaining art is my own, drawn in Procreate on iPad and Aseprite.
- Showpop font by [Khurasan Studio](https://khurasanstudio.com), free for personal and commercial use per the readme bundled with the download.
- TextMesh Pro by Unity Technologies under the Unity Companion License, including Liberation Sans (SIL OFL 1.1) and the EmojiOne sample sprites (see `Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt`).
- The University of St Andrews crest and the Java logo appear on two cosmetic shirts; both are trademarks of their owners, used decoratively in a student project.

## Known issues and what I would do differently

Combat is the weakest system, limited to clicking enemies in range, and it rated lowest for motivation in the user study; distinct attack patterns per enemy would be the fix. Dig power upgrades have little noticeable effect on play, so that upgrade line feels flat. Two small bugs remain: the lootbox notification stays hidden when more than one lootbox arrives at once, and the ultimate pickup shows its message without applying the stat boosts. Beyond fixes, I would replace the CSV import with a web portal where staff build and manage question sets directly.

## Licence

MIT. The Showpop font and the other credited third party content keep their own licences.
