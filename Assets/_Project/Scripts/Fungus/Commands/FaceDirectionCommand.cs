using UnityEngine;
using Fungus;
using PixelMindscape.Core;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("Pixel Mindscape", 
                 "Face Direction", 
                 "Makes a character face a specific direction by updating animator floats.")]
    public class FaceDirectionCommand : Command
    {
        [Tooltip("The character to face")]
        [SerializeField] private Transform targetTransform;

        [Tooltip("Direction to face (e.g., 0, -1 for down)")]
        [SerializeField] private Vector2 direction;

        public override void OnEnter()
        {
            if (targetTransform != null && CutsceneDirector.Instance != null)
            {
                CutsceneDirector.Instance.FaceDirection(targetTransform, direction);
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
            return $"{targetTransform.name} faces {direction}";
        }
    }
}
