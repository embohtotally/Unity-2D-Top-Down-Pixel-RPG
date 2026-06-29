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

        public override void OnEnter()
        {
            var flowchart = GetFlowchart();
            if (flowchart == null)
            {
                Continue();
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.LastBattleWon)
            {
                GameManager.Instance.LastBattleWon = false; // Reset flag so it doesn't trigger again
                if (!string.IsNullOrEmpty(onVictoryBlockName))
                {
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
