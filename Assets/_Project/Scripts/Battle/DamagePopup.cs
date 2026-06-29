using UnityEngine;
using TMPro;
using DG.Tweening;

namespace PixelMindscape.Battle
{
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text textMesh;
        [SerializeField] private float floatDistance = 1.5f;
        [SerializeField] private float duration = 1.0f;
        [SerializeField] private Color damageColor = Color.white;
        [SerializeField] private Color healColor = Color.green;

        private void Awake()
        {
            if (textMesh == null)
            {
                textMesh = GetComponent<TMP_Text>();
            }
        }

        public void Setup(int amount, bool isHeal = false)
        {
            textMesh.SetText(Mathf.Abs(amount).ToString());
            textMesh.color = isHeal ? healColor : damageColor;

            // Check if this is a UI Canvas object or a World Space object
            bool isUI = GetComponent<RectTransform>() != null && GetComponentInParent<Canvas>()?.renderMode == RenderMode.ScreenSpaceOverlay;
            float actualDistance = isUI ? floatDistance * 40f : floatDistance;

            // Animate floating up
            transform.DOMoveY(transform.position.y + actualDistance, duration).SetEase(Ease.OutCirc);

            // Animate fade out
            textMesh.DOFade(0, duration).SetEase(Ease.InExpo).OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }
    }
}
