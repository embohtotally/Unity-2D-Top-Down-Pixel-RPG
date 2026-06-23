using UnityEngine;
using Fungus;
using PixelMindscape.Core;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("Pixel Mindscape", 
                 "Move To Waypoint", 
                 "Moves a transform to a specific waypoint using DOTween via CutsceneDirector.")]
    public class MoveToWaypointCommand : Command
    {
        [Tooltip("The character or object to move")]
        [SerializeField] private Transform targetTransform;
        
        [Tooltip("The destination waypoint")]
        [SerializeField] private Transform destinationWaypoint;

        [Tooltip("How long the movement should take")]
        [SerializeField] private float duration = 1.0f;

        [Tooltip("Wait until the movement finishes before continuing the flowchart")]
        [SerializeField] private bool waitUntilFinished = true;

        public override void OnEnter()
        {
            if (targetTransform != null && destinationWaypoint != null && CutsceneDirector.Instance != null)
            {
                if (waitUntilFinished)
                {
                    StartCoroutine(MoveAndWaitRoutine());
                }
                else
                {
                    StartCoroutine(CutsceneDirector.Instance.MoveToWaypoint(targetTransform, destinationWaypoint.position, duration));
                    Continue();
                }
            }
            else
            {
                Continue();
            }
        }

        private System.Collections.IEnumerator MoveAndWaitRoutine()
        {
            yield return StartCoroutine(CutsceneDirector.Instance.MoveToWaypoint(targetTransform, destinationWaypoint.position, duration));
            Continue();
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }

        public override string GetSummary()
        {
            if (targetTransform == null || destinationWaypoint == null) return "Error: Missing references";
            return $"{targetTransform.name} to {destinationWaypoint.name} over {duration}s";
        }
    }
}
