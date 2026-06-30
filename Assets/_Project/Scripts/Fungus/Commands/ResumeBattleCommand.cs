using UnityEngine;
using Fungus;
using PixelMindscape.Battle;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("Battle", 
                 "Resume Battle Queue", 
                 "Resumes the turn-based combat queue after a cutscene or dialogue finishes.")]
    public class ResumeBattleCommand : Command
    {
        public override void OnEnter()
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.ResumeQueue();
            }
            Continue();
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }

        public override string GetSummary()
        {
            return "Resumes turn-based combat";
        }
    }
}
