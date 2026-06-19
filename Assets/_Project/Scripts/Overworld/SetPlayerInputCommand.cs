using UnityEngine;
using Fungus;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("PixelMindscape", 
                 "Set Player Input", 
                 "Forces the PlayerMovement script into cutscene mode and overrides their movement direction to trigger walking animations.")]
    [AddComponentMenu("")]
    public class SetPlayerInputCommand : Command
    {
        [Tooltip("If true, the player will ignore keyboard input and use the Override Direction below. If false, control is returned to the player.")]
        public bool cutsceneMode = true;

        [Tooltip("The direction the player should walk (X and Y between -1 and 1).")]
        public Vector2 overrideDirection;

        public override void OnEnter()
        {
            PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
            
            if (player != null)
            {
                player.CutsceneMode = cutsceneMode;
                if (cutsceneMode)
                {
                    player.SetOverrideMovement(overrideDirection);
                }
                else
                {
                    player.SetOverrideMovement(Vector2.zero);
                }
            }
            else
            {
                Debug.LogWarning("SetPlayerInputCommand: No PlayerMovement script found in scene!");
            }

            Continue();
        }

        public override string GetSummary()
        {
            if (cutsceneMode)
                return $"Override Input: {overrideDirection}";
            return "Return Control to Player";
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }
    }
}
