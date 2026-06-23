using UnityEngine;
using Fungus;
using PixelMindscape.Core;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("Pixel Mindscape", 
                 "Play Animation", 
                 "Sets an Animator trigger on the target.")]
    public class PlayAnimationCommand : Command
    {
        [Tooltip("The character to animate")]
        [SerializeField] private Transform targetTransform;

        [Tooltip("The trigger name in the Animator")]
        [SerializeField] private string triggerName;

        public override void OnEnter()
        {
            if (targetTransform != null && CutsceneDirector.Instance != null && !string.IsNullOrEmpty(triggerName))
            {
                CutsceneDirector.Instance.PlayAnimation(targetTransform, triggerName);
            }
            Continue();
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }

        public override string GetSummary()
        {
            if (targetTransform == null) return "Error: Missing target";
            return $"{targetTransform.name} plays '{triggerName}'";
        }
    }
}
