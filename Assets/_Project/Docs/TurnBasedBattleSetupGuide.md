# Pixel Mindscape: Complete Turn-Based Battle Setup Guide

This guide covers the complete end-to-end workflow for setting up characters, enemies, the battle scene, and overworld triggers for the turn-based combat system in Pixel Mindscape.

---

## Prerequisites: Build Settings & Core Managers

Before creating characters or testing battles, ensure your scene transitions and core managers are properly configured.

### 1. Configure Build Settings
For `GameManager` to transition between the Overworld and Battle scenes, both scenes must be added to your project build.
1. In the Unity top menu, go to `File -> Build Settings...`.
2. Ensure both your Overworld scene (e.g., `Overworld.unity`) and your Battle scene (e.g., `TurnBased.unity`) are listed in the **Scenes In Build** list.
3. If they are missing, open each scene and click **Add Open Scenes**.

### 2. Verify GameManager Presence
Every Overworld scene requires a `GameManager` instance to handle scene loading and persist party data.
1. Open your Overworld scene.
2. Ensure you have a GameObject named `GameManager` with the `GameManager.cs` script attached.
3. Because `GameManager` uses `DontDestroyOnLoad`, it will automatically carry over into the battle scene.

---

## Phase 1: Creating Character & Enemy Data (ScriptableObjects)

Core stats, stat growth, portraits, and identity strings are stored in `CharacterData` ScriptableObjects.

### 1. Create Character Data Assets
1. In the Project window, navigate to `Assets/_Project/ScriptableObjects/Characters/` (create the folder if it doesn't exist).
2. Right-click in empty space -> `Create -> PixelMindscape -> Character Data`.
3. Name the file appropriately (e.g., `CharacterData_Hero`).

### 2. Configure the Data Inspector
Select the newly created asset and configure the following parameters:
- **Identity**: 
  - `Character Id`: Unique internal key (e.g., `protagonist`, `slime_01`).
  - `Display Name`: The name displayed in UI menus and turn queues.
  - `Overworld Portrait`: The sprite used in dialogue and status menus.
- **Base Stats**: Set initial values for `Base HP`, `Base SP`, `Base Strength`, `Base Magic`, `Base Endurance`, `Base Agility`, and `Base Luck`.
- **Growth Curves**: Assign Animation Curves for `HP Growth Curve`, `SP Growth Curve`, and `Stat Growth Curve` to govern leveling progression.
- **Affinity Overrides**: Configure innate elemental resistances or weaknesses if applicable.

---

## Phase 2: Creating Combatant Prefabs

Combatant GameObjects handle visual representation, animations, visual effects (VFX), and active battle state.

### 1. Create the Hero Combatant Prefab
1. In an open scene (or prefab edit mode), right-click the Hierarchy -> `Create Empty`. Name it `Combatant_Hero`.
2. Select `Combatant_Hero` and click `Add Component -> HeroCombatant`.
3. Configure the `HeroCombatant` Inspector:
   - **UI Data**: Assign a `Turn Portrait` sprite (used in the battle turn order timeline UI).
   - **Hero Initial Stats**: Set `Starting HP`, `Starting SP`, `Base Attack`, and `Agility`. *(Note: Higher Agility acts earlier in the turn queue).*
   - **Affinities**: Choose which element the character is `Strong Against` (Resist) and `Weak Against` (Weak).

### 2. Configure Animation & VFX Handlers
1. Right-click `Combatant_Hero` in the Hierarchy -> `Create Empty` to create a child GameObject named `SpriteRoot`.
2. Add a `SpriteRenderer` component to `SpriteRoot` and assign your character's battle sprite.
3. Add an `Animator` component to `SpriteRoot`. Ensure your animation controller includes the required triggers: `Attack`, `Cast`, `Guard`, `TakeDamage`, and the boolean `IsDown`.
4. Add a `CombatantVFXHandler` component to `SpriteRoot`. Assign the corresponding VFX prefabs for `Hit`, `Guard`, `Attack`, and `Baton Pass`.
5. Drag `Combatant_Hero` into `Assets/_Project/Prefabs/Battle/` to save it as a Prefab, then delete it from the scene Hierarchy.

### 3. Create the Enemy Combatant Prefab
Repeat the steps above for enemies, but attach the `EnemyCombatant` script instead of `HeroCombatant`.
- Configure `Enemy Initial Stats` and `Affinities`.
- Save as a prefab (e.g., `Combatant_Enemy_Slime`) in `Assets/_Project/Prefabs/Battle/`.

---

## Phase 3: Setting Up the Battle Scene

The `BattleManager` coordinates turns, UI popups, All-Out attacks, and party staging.

### 1. Organize Scene Hierarchy & Containers
1. Open your battle scene (e.g., `TurnBased.unity`).
2. Ensure you have two empty GameObjects acting as parent folders for positions: `HeroesContainer` and `EnemiesContainer`.
3. Drag your `Combatant_Hero` prefab into the Hierarchy as a **child of `HeroesContainer`**. Position the GameObject at the desired formation coordinate on screen.
4. Drag your `Combatant_Enemy_Slime` prefab into the Hierarchy as a **child of `EnemiesContainer`**. Position it on the opposing side of the battlefield.

### 2. Configure BattleManager Settings
1. Locate or create the `BattleManager` GameObject in the scene (with the `BattleManager.cs` script attached).
2. Configure the Inspector properties:
   - **Damage Popup Prefab**: Assign the `DamagePopup` prefab (from `Assets/_Project/Prefabs/UI/`).
   - **Testing Auto-Start**: Check the **`Auto Start Battle`** checkbox.
   - **Heroes Container**: Drag the `HeroesContainer` GameObject into this field.
   - **Enemies Container**: Drag the `EnemiesContainer` GameObject into this field.
3. Save the `TurnBased.unity` scene.

---

## Phase 4: Setting Up Overworld Triggers via Fungus

To initiate combat organically from exploration, configure an overworld trigger zone using Fungus.

### 1. Create the Collision Trigger
1. Open your Overworld scene.
2. Right-click in the Hierarchy -> `Create Empty`. Name it `EncounterTrigger_Enemy`.
3. Select `EncounterTrigger_Enemy` -> `Add Component -> BoxCollider2D`.
4. Check the **`Is Trigger`** box on the `BoxCollider2D`.
5. Adjust the collider `Size` and move it to cover the encounter zone on your map.

### 2. Attach and Configure the Fungus Flowchart
1. With `EncounterTrigger_Enemy` selected, click `Add Component -> Flowchart`.
2. Open the Flowchart window (`Tools -> Fungus -> Flowchart Window`).
3. Select the default Block and rename it to `StartEncounter` in the Inspector.
4. In the `StartEncounter` Inspector, click the **`Execute On Event`** dropdown and select `Mono -> On Trigger Enter 2D`.
5. In the event settings that appear directly below, enter `Player` into the **`Tag`** field.
   *(Note: Ensure your player GameObject in the overworld has its Tag set to `Player`).*

### 3. Add the Start Battle Command
1. With the `StartEncounter` block selected, click the `+` button at the bottom of the command list in the Inspector.
2. Navigate to `PixelMindscape -> Start Battle`.
3. Select the newly added command. In the `Battle Scene Name` field, type the exact name of your battle scene (e.g., `TurnBased`).
4. Save the Overworld scene.

---

## Phase 5: Architectural Flow Summary

When all four phases are complete, the runtime execution behaves as follows:

1. **Exploration**: The player navigates the overworld and enters the `EncounterTrigger_Enemy` BoxCollider2D zone.
2. **Trigger Detection**: The Fungus `OnTriggerEnter2D` listener detects the `Player` tag and executes the `StartEncounter` block.
3. **Scene Transition**: The `Start Battle` command invokes `GameManager.Instance.LoadScene("TurnBased")`.
4. **Battle Initialization**: Upon loading `TurnBased`, `BattleManager` initializes in `Start()`. Because `Auto Start Battle` is enabled, it scans `HeroesContainer` and `EnemiesContainer` for all `Combatant` scripts.
5. **Turn Calculation**: `BattleManager.CalculateTurnOrder()` sorts all active combatants by their `EffectiveAgility` stat.
6. **Combat Loop**: The battle loop coroutine begins, passing turn execution to the combatant with the highest agility.
