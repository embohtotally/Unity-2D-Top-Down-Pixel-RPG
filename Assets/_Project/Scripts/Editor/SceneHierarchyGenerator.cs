using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
namespace PixelMindscape.EditorTools
{
    public class SceneHierarchyGenerator : EditorWindow
    {
        [MenuItem("Tools/PixelMindscape/Generate Scene Hierarchies/Main Menu")]
        public static void GenerateMainMenu()
        {
            CreateSeparator("--- SYSTEMS ---");
            CreateCamera();
            CreateEventSystem();

            CreateSeparator("--- UI ---");
            GameObject canvas = CreateCanvas("Canvas_MainMenu");
            
            GameObject bg = new GameObject("Panel_Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(canvas.transform, false);

            GameObject title = new GameObject("Text_Title", typeof(RectTransform), typeof(Text));
            title.transform.SetParent(canvas.transform, false);

            string[] buttons = { "Button_Start", "Button_Load", "Button_Settings", "Button_Quit" };
            foreach (var btn in buttons)
            {
                GameObject buttonObj = new GameObject(btn, typeof(RectTransform), typeof(Button), typeof(Image));
                buttonObj.transform.SetParent(canvas.transform, false);
            }

            Debug.Log("Main Menu hierarchy generated successfully!");
        }

        [MenuItem("Tools/PixelMindscape/Generate Scene Hierarchies/Overworld")]
        public static void GenerateOverworld()
        {
            CreateSeparator("--- SYSTEMS ---");
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<PixelMindscape.Core.GameManager>();

            GameObject cam = CreateCamera();
            cam.AddComponent<CameraManager>();

            CreateEventSystem();

            CreateSeparator("--- ENVIRONMENT ---");
            GameObject grid = new GameObject("Grid", typeof(Grid));
            string[] tilemaps = { "Tilemap_Ground", "Tilemap_Collisions", "Tilemap_Decorations" };
            foreach (var tm in tilemaps)
            {
                GameObject tObj = new GameObject(tm, typeof(UnityEngine.Tilemaps.Tilemap), typeof(UnityEngine.Tilemaps.TilemapRenderer));
                tObj.transform.SetParent(grid.transform, false);
            }

            CreateSeparator("--- CHARACTERS ---");
            GameObject player = new GameObject("Player", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D));
            player.AddComponent<PlayerMovement>();
            var rb = player.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;

            new GameObject("NPCs_Container");

            CreateSeparator("--- TRIGGERS ---");
            new GameObject("SceneTransitions_Container");

            Debug.Log("Overworld hierarchy generated successfully!");
        }

        [MenuItem("Tools/PixelMindscape/Generate Scene Hierarchies/Battle Scene")]
        public static void GenerateBattle()
        {
            CreateSeparator("--- SYSTEMS ---");
            GameObject bm = new GameObject("BattleManager");
            bm.AddComponent<PixelMindscape.Battle.BattleManager>();

            CreateCamera();
            CreateEventSystem();

            CreateSeparator("--- COMBATANTS ---");
            GameObject heroes = new GameObject("Heroes_Container");
            for(int i=1; i<=4; i++)
            {
                GameObject h = new GameObject($"Hero_Slot_{i}", typeof(SpriteRenderer));
                // Add your concrete HeroCombatant script here later
                h.transform.SetParent(heroes.transform, false);
            }

            GameObject enemies = new GameObject("Enemies_Container");
            for(int i=1; i<=4; i++)
            {
                GameObject e = new GameObject($"Enemy_Slot_{i}", typeof(SpriteRenderer));
                // Add your concrete EnemyCombatant script here later
                e.transform.SetParent(enemies.transform, false);
            }

            CreateSeparator("--- UI ---");
            GameObject canvas = CreateCanvas("Canvas_BattleUI");
            
            GameObject actionMenu = new GameObject("ActionMenu", typeof(RectTransform));
            actionMenu.transform.SetParent(canvas.transform, false);

            GameObject statusBars = new GameObject("StatusBars_Container", typeof(RectTransform));
            statusBars.transform.SetParent(canvas.transform, false);

            Debug.Log("Battle Scene hierarchy generated successfully!");
        }

        // --- Utility Methods ---

        private static void CreateSeparator(string name)
        {
            GameObject separator = new GameObject(name);
            separator.tag = "EditorOnly";
            separator.transform.SetAsLastSibling();
        }

        private static GameObject CreateCamera()
        {
            if (Camera.main != null) return Camera.main.gameObject;
            GameObject cam = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
            cam.tag = "MainCamera";
            return cam;
        }

        private static GameObject CreateEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return null;
            return new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static GameObject CreateCanvas(string name)
        {
            GameObject canvas = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            return canvas;
        }
    }
}
#endif
