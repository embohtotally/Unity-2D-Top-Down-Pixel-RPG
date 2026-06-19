using UnityEngine;
using Fungus;
using PixelMindscape.Battle;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("PixelMindscape", 
                 "Pause Battle", 
                 "Pauses the Turn-Based Battle Queue, preventing the next character from taking their turn until Resume Battle is called.")]
    [AddComponentMenu("")]
    public class PauseBattleCommand : Command
    {
        public override void OnEnter()
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.PauseQueue();
            }
            else
            {
                Debug.LogWarning("PauseBattleCommand: No active BattleManager found!");
            }

            Continue();
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }
    }
}
