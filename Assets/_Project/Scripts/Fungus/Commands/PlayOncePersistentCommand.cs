using UnityEngine;
using Fungus;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("Battle", 
                 "Play Once (Persistent)", 
                 "Checks the player's hard drive save data. If this event has played before, it instantly stops the block. If not, it saves the flag and continues.")]
    public class PlayOncePersistentCommand : Command
    {
        [Tooltip("A unique string key to save to the hard drive (e.g., 'Tutorial_Battle_Played').")]
        public string playerPrefsKey = "Tutorial_Battle_Played";

        public override void OnEnter()
        {
            if (PlayerPrefs.GetInt(playerPrefsKey, 0) == 1)
            {
                // This has already been played on this save file/PC! 
                // Stop the block completely so the dialogue doesn't play.
                StopParentBlock(); 
            }
            else
            {
                // First time! Mark as played to the hard drive and continue.
                PlayerPrefs.SetInt(playerPrefsKey, 1);
                PlayerPrefs.Save();
                Continue();
            }
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }

        public override string GetSummary()
        {
            return $"Key: {playerPrefsKey}";
        }
    }
}
