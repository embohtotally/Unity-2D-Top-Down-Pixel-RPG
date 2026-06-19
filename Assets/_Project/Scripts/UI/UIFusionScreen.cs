using UnityEngine;
using PixelMindscape.Data;
using PixelMindscape.Core;
using PixelMindscape.Persona;
using DG.Tweening;

namespace PixelMindscape.UI
{
    public class UIFusionScreen : MonoBehaviour
    {
        [SerializeField] private PersonaFusionManager fusionManager;
        [SerializeField] private RectTransform fusionWindow;
        
        // [SerializeField] private PersonaSlotView slotA, slotB, resultPreview;

        public void OpenScreen()
        {
            gameObject.SetActive(true);
            if (fusionWindow != null)
            {
                fusionWindow.anchoredPosition = new Vector2(0, -1000);
                fusionWindow.DOAnchorPos(Vector2.zero, 0.5f).SetEase(Ease.OutCubic);
            }
        }

        public void CloseScreen()
        {
            if (fusionWindow != null)
            {
                fusionWindow.DOAnchorPos(new Vector2(0, -1000), 0.4f).SetEase(Ease.InCubic)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void OnSlotsChanged(PersonaData a, PersonaData b)
        {
            if (fusionManager == null) return;
            var result = fusionManager.GetFusionResult(a, b);
            // resultPreview.Bind(result); // shows null state ("Incompatible") if result is null
        }

        public void OnConfirmFusion(PersonaData a, PersonaData b)
        {
            if (fusionManager == null) return;
            var resultTemplate = fusionManager.GetFusionResult(a, b);
            if (resultTemplate == null) return;
            
            if (GameManager.Instance != null)
            {
                var runtime = GameManager.Instance.CreateRuntimeStateFromTemplate(resultTemplate);
                // opens skill-inheritance selection sub-screen before finalizing, calling ApplyInheritance() on confirm
            }
        }
    }
}
