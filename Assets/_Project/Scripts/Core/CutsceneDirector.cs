using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace PixelMindscape.Core
{
    public class CutsceneDirector : MonoBehaviour
    {
        public static CutsceneDirector Instance { get; private set; }

        public bool IsCutscenePlaying { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void BeginCutscene()
        {
            IsCutscenePlaying = true;
            // PlayerMovement checks this flag to block input
        }

        public void EndCutscene()
        {
            IsCutscenePlaying = false;
        }

        public IEnumerator MoveToWaypoint(Transform targetTransform, Vector3 destination, float duration)
        {
            if (targetTransform != null)
            {
                yield return targetTransform.DOMove(destination, duration).SetEase(Ease.Linear).WaitForCompletion();
            }
        }

        public void FaceDirection(Transform targetTransform, Vector2 direction)
        {
            if (targetTransform != null)
            {
                var animator = targetTransform.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.SetFloat("Horizontal", direction.x);
                    animator.SetFloat("Vertical", direction.y);
                }
            }
        }

        public void PlayAnimation(Transform targetTransform, string animationTrigger)
        {
            if (targetTransform != null)
            {
                var animator = targetTransform.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger(animationTrigger);
                }
            }
        }
    }
}
