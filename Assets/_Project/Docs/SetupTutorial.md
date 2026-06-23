# Pixel Mindscape: Step-by-Step Setup Tutorial

This document serves as a permanent, step-by-step tutorial for setting up the newly created systems in the Unity Editor.
Because this file is saved in your `Assets/_Project/Docs/` folder, it will be committed to your version control (like Git) and will never be lost!

---

## 1. Setting up the BattleCinematicManager

The `BattleCinematicManager` handles the visual flair for All-Out Attacks and Persona Summons.

**Step 1: Create the Manager Object**
- Open your `BattleScene`.
- Right-click in the Hierarchy and select `Create Empty`.
- Name it `BattleCinematicManager`.
- Drag and drop the `BattleCinematicManager.cs` script onto this new object.

**Step 2: Create the All-Out Attack UI**
- Right-click your main Canvas and select `UI -> Panel`. Name it `AllOutSplashPanel`.
- Change its background color to Black and ensure it stretches across the whole screen.
- Right-click the Panel, select `UI -> Image`. Name it `CharacterArt` (this is the freeze-frame sprite).
- Right-click the Panel, select `UI -> Text - TextMeshPro`. Name it `AllOutText`. Change the text to "ALL-OUT ATTACK!".
- Disable (uncheck) the `AllOutSplashPanel` so it starts hidden.

**Step 3: Link the References**
- Click on your `BattleCinematicManager` object.
- Drag the `AllOutSplashPanel` into the *All Out Splash Panel* slot.
- Drag the `AllOutSplashPanel`'s Image component into the *All Out Background Image* slot.
- Drag the `CharacterArt` into the *All Out Character Art* slot.
- Drag the `AllOutText` into the *All Out Text* slot.

---

## 2. Setting up "1 MORE!" Visual Feedback

The `UICombatPanel` now listens for when a weakness is hit and spawns a floating text prefab.

**Step 1: Create the Prefab**
- Right-click in the Hierarchy and select `UI -> Text - TextMeshPro`.
- Name it `OneMoreTextFeedback`.
- Change the text to "1 MORE!". Make it bold, red, and add an Outline in the material settings.
- Important: Add a `Canvas` component to it, and check `Override Sorting`. Set the Sort Order to something high (like 100) so it appears over characters.
- Drag `OneMoreTextFeedback` from the Hierarchy into your `Assets/_Project/Prefabs/UI/` folder to make it a Prefab.
- Delete the object from the Hierarchy.

**Step 2: Link the Prefab**
- Find your `UICombatPanel` in the scene (the script handling the battle UI).
- Drag the `OneMoreTextFeedback` prefab from your Project window into the `One More Text Prefab` slot in the inspector.

---

## 3. Creating Stat Training Activities

You can now create ScriptableObjects that represent activities to increase Social Stats (like studying, eating big burgers, etc.).

**Step 1: Create the Asset**
- In your Project window, navigate to `Assets/_Project/Data/Activities/` (create the folder if it doesn't exist).
- Right-click in the empty space -> `Create -> PixelMindscape -> Stat Training Activity`.
- Name the file something like `Activity_StudyInLibrary`.

**Step 2: Configure the Asset**
- Click on the new asset.
- In the Inspector:
  - **Activity Name**: "Study at Library"
  - **Stat Type**: Change the dropdown to `Knowledge`.
  - **Points Granted**: 2
  - **Time Cost**: `AfterSchool`

**Step 3: Triggering it via Fungus**
- In your Fungus Flowchart, when the player talks to the Library chair and agrees to study, use a `Call Method` or `Invoke Event` block.
- Point it to the `CalendarManager.PerformActivity` method and pass in the `Activity_StudyInLibrary` asset.
- The manager will automatically advance time and update your Fungus variables!

---

## 4. Using the Custom Fungus Cutscene Commands

We added new commands so you don't need timelines for simple 2D pixel-art cutscenes.

**Step 1: Open your Flowchart**
- Select a Block in your Fungus Flowchart.
- Click the `+` button to add a new Command.
- Look for the **"Pixel Mindscape"** category in the list.

**Step 2: Move To Waypoint**
- Select `Move To Waypoint`.
- **Target Transform**: Drag the GameObject of the character you want to move.
- **Destination Waypoint**: Create an Empty GameObject in the scene where you want them to walk to, and drag it here.
- **Duration**: How many seconds it takes to walk there.
- Keep `Wait Until Finished` checked if you want the dialogue to pause while they walk.

**Step 3: Play Animation**
- Click `+` and select `Pixel Mindscape -> Play Animation`.
- **Target Transform**: Drag the character.
- **Trigger Name**: Type the exact name of the Trigger parameter in your Animator (e.g., `Surprised`, `Laugh`).

*Remember: Cutscenes will automatically flip the `CutsceneDirector.IsCutscenePlaying` flag, ensuring the player can't walk away during the sequence!*
