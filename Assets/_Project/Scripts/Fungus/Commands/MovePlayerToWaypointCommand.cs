using UnityEngine;
using Fungus;
using PixelMindscape.Core;
using System.Collections;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("Cinematics", 
                 "Move Player To Waypoint", 
                 "Moves the player autonomously to a waypoint while updating walk animations.")]
    public class MovePlayerToWaypointCommand : Command
    {
        [Tooltip("The player transform to move")]
        [SerializeField] private Transform targetTransform;

        [Tooltip("The destination waypoint")]
        [SerializeField] private Transform destination;

        [Tooltip("How long the movement should take in seconds")]
        [SerializeField] private float duration = 2.5f;

        [Tooltip("Wait until the movement has finished before continuing")]
        [SerializeField] private bool waitUntilFinished = true;

        public override void OnEnter()
        {
            if (targetTransform != null && destination != null && CutsceneDirector.Instance != null)
            {
                if (waitUntilFinished)
                {
                    StartCoroutine(MoveAndWaitRoutine());
                }
                else
                {
                    StartCoroutine(CutsceneDirector.Instance.MoveToWaypoint(targetTransform, destination.position, duration));
                    Continue();
                }
            }
            else
            {
                Continue();
            }
        }

        private IEnumerator MoveAndWaitRoutine()
        {
            yield return StartCoroutine(CutsceneDirector.Instance.MoveToWaypoint(targetTransform, destination.position, duration));
            Continue();
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }

        public override string GetSummary()
        {
            if (targetTransform == null || destination == null) return "Error: Missing target or destination";
            return $"{targetTransform.name} moves to {destination.name} ({duration}s)";
        }
    }
}
