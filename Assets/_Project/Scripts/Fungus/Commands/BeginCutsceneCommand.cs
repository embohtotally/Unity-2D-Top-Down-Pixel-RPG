using UnityEngine;
using Fungus;
using PixelMindscape.Core;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("Cinematics", 
                 "Begin Cutscene", 
                 "Locks player input by starting cutscene mode.")]
    public class BeginCutsceneCommand : Command
    {
        public override void OnEnter()
        {
            if (CutsceneDirector.Instance != null)
            {
                CutsceneDirector.Instance.BeginCutscene();
            }
            Continue();
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }

        public override string GetSummary()
        {
            return "Locks player input";
        }
    }
}
