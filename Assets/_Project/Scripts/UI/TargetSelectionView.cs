using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelMindscape.Battle;

namespace PixelMindscape.UI
{
    public class TargetSelectionView : MonoBehaviour
    {
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject targetButtonPrefab; // Must have Button and TMP_Text
        [SerializeField] private Button cancelButton;

        public event System.Action<Combatant> OnTargetSelected;
        public event System.Action OnCancelled;

        private List<GameObject> activeButtons = new List<GameObject>();

        private void Start()
        {
            if (cancelButton != null) cancelButton.onClick.AddListener(() => OnCancelled?.Invoke());
        }

        public void Show(List<Combatant> targets)
        {
            gameObject.SetActive(true);
            ClearButtons();

            foreach (var target in targets)
            {
                if (target.IsDefeated) continue; // Skip dead targets

                var btnObj = Instantiate(targetButtonPrefab, buttonContainer);
                activeButtons.Add(btnObj);

                var button = btnObj.GetComponent<Button>();
                var text = btnObj.GetComponentInChildren<TMP_Text>();

                if (text != null) text.SetText(target.gameObject.name);

                var currentTarget = target; // capture for closure
                if (button != null) button.onClick.AddListener(() => 
                {
                    OnTargetSelected?.Invoke(currentTarget);
                    Hide();
                });
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            ClearButtons();
        }

        private void ClearButtons()
        {
            foreach (var btn in activeButtons)
            {
                Destroy(btn);
            }
            activeButtons.Clear();
        }
    }
}
