using UnityEngine;
using Fungus;
using PixelMindscape.Core;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("Cinematics", 
                 "End Cutscene", 
                 "Unlocks player input by ending cutscene mode.")]
    public class EndCutsceneCommand : Command
    {
        public override void OnEnter()
        {
            if (CutsceneDirector.Instance != null)
            {
                CutsceneDirector.Instance.EndCutscene();
            }
            Continue();
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }

        public override string GetSummary()
        {
            return "Unlocks player input";
        }
    }
}
