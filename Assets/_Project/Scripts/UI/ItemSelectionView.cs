using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelMindscape.Data;

namespace PixelMindscape.UI
{
    public class ItemSelectionView : MonoBehaviour
    {
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject itemButtonPrefab; // Must have Button and TMP_Text
        [SerializeField] private Button cancelButton;

        private System.Action<ItemData> onItemSelectedCallback;
        private System.Action onCancelledCallback;

        private List<GameObject> activeButtons = new List<GameObject>();

        private void Start()
        {
            if (cancelButton != null) cancelButton.onClick.AddListener(CancelSelection);
        }

        public void Show(List<ItemData> items, System.Action<ItemData> onSelected, System.Action onCancel = null)
        {
            onItemSelectedCallback = onSelected;
            onCancelledCallback = onCancel;
            if (selectionPanel != null) selectionPanel.SetActive(true);

            ClearButtons();

            if (items == null || items.Count == 0)
            {
                Debug.LogWarning("[ItemSelectionView] Show called with 0 items! Ensure the UICombatPanel has items assigned to 'Available Items' in the Inspector.");
                return;
            }

            Debug.Log($"[ItemSelectionView] Spawning buttons for {items.Count} items...");

            Transform actualContainer = buttonContainer != null ? buttonContainer : (selectionPanel != null ? selectionPanel.transform : transform);

            foreach (var item in items)
            {
                GameObject btnObj = null;
                if (itemButtonPrefab != null)
                {
                    btnObj = Instantiate(itemButtonPrefab, actualContainer);
                }
                else
                {
                    Debug.LogWarning("[ItemSelectionView] ItemButtonPrefab is not assigned in the Inspector! Generating a default UI button dynamically.");
                    btnObj = new GameObject($"Btn_{item.itemId}");
                    btnObj.transform.SetParent(actualContainer, false);
                    var rect = btnObj.AddComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(400, 60);
                    var img = btnObj.AddComponent<Image>();
                    img.color = new Color32(30, 60, 90, 240); // sleek dark blue for items
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

                string desc = item.reviveTarget > 0 ? $"Revives {(item.reviveTarget == 2 ? "all allies" : "an ally")}" : (item.healAmount > 0 ? $"Restores {item.healAmount} HP" : "Consumable Item");
                if (text != null) text.SetText($"{item.displayName} - {desc}");

                var currentItem = item;
                if (button != null) button.onClick.AddListener(() => 
                {
                    var callback = onItemSelectedCallback;
                    Hide();
                    callback?.Invoke(currentItem);
                });
            }
        }

        public void Hide()
        {
            onItemSelectedCallback = null;
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
