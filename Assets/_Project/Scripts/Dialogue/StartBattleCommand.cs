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

        [Tooltip("The enemy prefab to spawn for this battle (e.g. Slime).")]
        public GameObject enemyPrefab;

        public override void OnEnter()
        {
            if (GameManager.Instance != null)
            {
                if (enemyPrefab != null)
                {
                    GameManager.Instance.PendingEnemyPrefab = enemyPrefab;
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
