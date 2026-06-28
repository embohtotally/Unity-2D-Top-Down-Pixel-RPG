using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelMindscape.Data;

namespace PixelMindscape.UI
{
    public class SkillSelectionView : MonoBehaviour
    {
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject skillButtonPrefab; // Must have Button and TMP_Text
        [SerializeField] private Button cancelButton;

        private System.Action<SkillData> onSkillSelectedCallback;
        private System.Action onCancelledCallback;

        private List<GameObject> activeButtons = new List<GameObject>();

        private void Start()
        {
            if (cancelButton != null) cancelButton.onClick.AddListener(CancelSelection);
        }

        public void Show(List<SkillData> skills, System.Action<SkillData> onSelected, System.Action onCancel = null)
        {
            onSkillSelectedCallback = onSelected;
            onCancelledCallback = onCancel;
            if (selectionPanel != null) selectionPanel.SetActive(true);

            ClearButtons();

            if (skills == null) return;

            foreach (var skill in skills)
            {
                var btnObj = Instantiate(skillButtonPrefab, buttonContainer);
                activeButtons.Add(btnObj);

                var button = btnObj.GetComponent<Button>();
                var text = btnObj.GetComponentInChildren<TMP_Text>();

                if (text != null) text.SetText($"{skill.displayName} [{skill.element}] ({skill.spCost} SP) - Pwr: {skill.basePower} | Scope: {skill.targetScope}");

                var currentSkill = skill;
                if (button != null) button.onClick.AddListener(() => 
                {
                    var callback = onSkillSelectedCallback;
                    Hide();
                    callback?.Invoke(currentSkill);
                });
            }
        }

        public void Hide()
        {
            onSkillSelectedCallback = null;
            onCancelledCallback = null;
            if (selectionPanel != null) selectionPanel.SetActive(false);
            ClearButtons();
        }

        private void CancelSelection()
        {
            var callback = onCancelledCallback;
            Hide();
            callback?.Invoke();
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
