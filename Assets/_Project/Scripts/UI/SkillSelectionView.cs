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

            if (skills == null || skills.Count == 0)
            {
                Debug.LogWarning("[SkillSelectionView] Show called with 0 skills! Ensure the active combatant has skills assigned in the Inspector.");
                return;
            }

            Debug.Log($"[SkillSelectionView] Spawning buttons for {skills.Count} skills...");

            Transform actualContainer = buttonContainer != null ? buttonContainer : (selectionPanel != null ? selectionPanel.transform : transform);

            foreach (var skill in skills)
            {
                GameObject btnObj = null;
                if (skillButtonPrefab != null)
                {
                    btnObj = Instantiate(skillButtonPrefab, actualContainer);
                }
                else
                {
                    Debug.LogWarning("[SkillSelectionView] SkillButtonPrefab is not assigned in the Inspector! Generating a default UI button dynamically.");
                    btnObj = new GameObject($"Btn_{skill.skillId}");
                    btnObj.transform.SetParent(actualContainer, false);
                    var rect = btnObj.AddComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(400, 60);
                    var img = btnObj.AddComponent<Image>();
                    img.color = new Color32(40, 40, 40, 240); // sleek dark grey
                    btnObj.AddComponent<Button>();

                    var textObj = new GameObject("Text");
                    textObj.transform.SetParent(btnObj.transform, false);
                    var textRect = textObj.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                    var tmp = textObj.AddComponent<TextMeshProUGUI>();
                    tmp.fontSize = 24;
                    tmp.alignment = TextAlignmentOptions.Center;
                }

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
