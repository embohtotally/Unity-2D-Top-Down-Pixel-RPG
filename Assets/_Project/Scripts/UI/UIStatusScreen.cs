using UnityEngine;
using PixelMindscape.Data;
using PixelMindscape.Core;
using DG.Tweening;

namespace PixelMindscape.UI
{
    public class UIStatusScreen : MonoBehaviour
    {
        [SerializeField] private GameObject statRowPrefab;
        [SerializeField] private Transform statRowContainer;
        [SerializeField] private CanvasGroup canvasGroup;

        public void Show()
        {
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.3f);
            }
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, 0.2f).OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void Refresh(PartyMemberRuntimeState state, CharacterData template)
        {
            // instantiates/binds statRowPrefab entries for HP, SP, STR, MAG, END, AGI, LUK
            // Using DOTween to stagger animate rows coming in could be added here
        }
    }
}
