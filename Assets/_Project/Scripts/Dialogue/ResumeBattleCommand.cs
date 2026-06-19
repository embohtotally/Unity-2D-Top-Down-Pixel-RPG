using UnityEngine;
using Fungus;
using PixelMindscape.Battle;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("PixelMindscape", 
                 "Resume Battle", 
                 "Unpauses the Turn-Based Battle Queue so combat can proceed.")]
    [AddComponentMenu("")]
    public class ResumeBattleCommand : Command
    {
        public override void OnEnter()
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.ResumeQueue();
            }
            else
            {
                Debug.LogWarning("ResumeBattleCommand: No active BattleManager found!");
            }

            Continue();
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }
    }
}
