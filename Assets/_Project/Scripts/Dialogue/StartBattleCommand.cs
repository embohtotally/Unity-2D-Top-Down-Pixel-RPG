using UnityEngine;
using Fungus;
using PixelMindscape.Core;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("PixelMindscape", 
                 "Start Battle", 
                 "Tells the GameManager to load the Battle Scene.")]
    [AddComponentMenu("")]
    public class StartBattleCommand : Command
    {
        [Tooltip("The name of the scene to load (e.g., 'Battle_Standard').")]
        public string battleSceneName = "Battle_Standard";

        [Tooltip("The list of enemy prefabs to spawn for this battle (e.g. Slime, Slime).")]
        public System.Collections.Generic.List<GameObject> enemyPrefabs = new System.Collections.Generic.List<GameObject>();

        public override void OnEnter()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PendingEnemyPrefabs.Clear();
                if (enemyPrefabs != null && enemyPrefabs.Count > 0)
                {
                    GameManager.Instance.PendingEnemyPrefabs.AddRange(enemyPrefabs);
                }
                
                // Save the player's exact overworld position before they get teleported to a Battle Slot
                var playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player") ?? GameObject.Find("Player_Overworld");
                if (playerObj != null)
                {
                    GameManager.Instance.LastOverworldPosition = playerObj.transform.position;
                    GameManager.Instance.HasSavedOverworldPosition = true;
                }

                // Fungus won't continue executing because the scene is unloading
                GameManager.Instance.LoadScene(battleSceneName);
            }
            else
            {
                Debug.LogError("StartBattleCommand: GameManager Instance is null!");
                Continue();
            }
        }

        public override string GetSummary()
        {
            return $"Load Scene: {battleSceneName}";
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }
    }
}
