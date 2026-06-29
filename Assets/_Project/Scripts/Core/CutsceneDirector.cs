using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace PixelMindscape.Core
{
    public class CutsceneDirector : MonoBehaviour
    {
        private static CutsceneDirector _instance;
        public static CutsceneDirector Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CutsceneDirector>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("CutsceneDirector");
                        _instance = obj.AddComponent<CutsceneDirector>();
                    }
                }
                return _instance;
            }
        }

        public bool IsCutscenePlaying { get; private set; }

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else if (_instance != this) Destroy(gameObject);
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
                // Use Component and SendMessage to bypass any Assembly Definition (.asmdef) boundaries!
                var playerMovement = targetTransform.GetComponent("PlayerMovement");
                if (playerMovement != null)
                {
                    targetTransform.SendMessage("SetCutsceneMode", true, SendMessageOptions.DontRequireReceiver);
                    Vector3 startPos = targetTransform.position;
                    float elapsed = 0f;

                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        Vector2 direction = (destination - targetTransform.position).normalized;
                        targetTransform.SendMessage("SetOverrideMovement", direction, SendMessageOptions.DontRequireReceiver);
                        
                        targetTransform.position = Vector3.Lerp(startPos, destination, elapsed / duration);
                        yield return null;
                    }

                    targetTransform.position = destination;
                    targetTransform.SendMessage("SetOverrideMovement", Vector2.zero, SendMessageOptions.DontRequireReceiver);
                    if (!IsCutscenePlaying) targetTransform.SendMessage("SetCutsceneMode", false, SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    yield return targetTransform.DOMove(destination, duration).SetEase(Ease.Linear).WaitForCompletion();
                }
            }
        }

        public void SetPlayerCutsceneMode(Component player, bool isCutscene)
        {
            if (player != null) player.SendMessage("SetCutsceneMode", isCutscene, SendMessageOptions.DontRequireReceiver);
            IsCutscenePlaying = isCutscene;
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
