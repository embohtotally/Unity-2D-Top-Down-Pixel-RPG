using UnityEngine;
using Fungus;
using PixelMindscape.Battle;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("Battle", 
                 "Pause Battle Queue", 
                 "Pauses the turn-based combat queue so you can play a cutscene or dialogue.")]
    public class PauseBattleCommand : Command
    {
        public override void OnEnter()
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.PauseQueue();
            }
            Continue();
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }

        public override string GetSummary()
        {
            return "Pauses turn-based combat";
        }
    }
}
