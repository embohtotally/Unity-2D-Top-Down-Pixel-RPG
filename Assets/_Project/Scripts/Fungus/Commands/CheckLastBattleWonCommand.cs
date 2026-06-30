using UnityEngine;
using Fungus;
using PixelMindscape.Core;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("Battle", 
                 "Check Last Battle Won", 
                 "Checks if the last battle was won and branches accordingly.")]
    public class CheckLastBattleWonCommand : Command
    {
        [Tooltip("Name of the block to execute if the battle was won (e.g., '3_PostBattle_Escape')")]
        [SerializeField] private string onVictoryBlockName = "3_PostBattle_Escape";

        [Tooltip("Name of the block to execute if the battle was not won (e.g., '1_Awakening_Cinematic')")]
        [SerializeField] private string onNormalBlockName = "1_Awakening_Cinematic";

        [Tooltip("Optional: A PlayerPrefs key to ensure these blocks only play ONCE per save file (e.g., 'Completed_Intro_Sequence'). If left empty, it plays every time.")]
        [SerializeField] private string playOnceEventKey = "Completed_Intro_Sequence";

        public override void OnEnter()
        {
            var flowchart = GetFlowchart();
            if (flowchart == null)
            {
                Continue();
                return;
            }

            // Check if this cinematic has already been completed in this save file
            if (!string.IsNullOrEmpty(playOnceEventKey) && PlayerPrefs.GetInt(playOnceEventKey, 0) == 1)
            {
                Debug.Log($"[CheckLastBattleWon] Event '{playOnceEventKey}' has already played. Skipping cinematic.");
                Continue();
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.LastBattleWon)
            {
                GameManager.Instance.LastBattleWon = false; // Reset flag so it doesn't trigger again
                
                if (!string.IsNullOrEmpty(onVictoryBlockName))
                {
                    // Mark as played so it never happens again!
                    if (!string.IsNullOrEmpty(playOnceEventKey))
                    {
                        PlayerPrefs.SetInt(playOnceEventKey, 1);
                        PlayerPrefs.Save();
                    }
                    
                    flowchart.ExecuteBlock(onVictoryBlockName);
                    return;
                }
            }
            
            if (!string.IsNullOrEmpty(onNormalBlockName))
            {
                flowchart.ExecuteBlock(onNormalBlockName);
            }
            else
            {
                Continue();
            }
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }

        public override string GetSummary()
        {
            return $"If Won -> {onVictoryBlockName} | Else -> {onNormalBlockName}";
        }
    }
}
