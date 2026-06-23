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

            if (items == null) return;

            foreach (var item in items)
            {
                var btnObj = Instantiate(itemButtonPrefab, buttonContainer);
                activeButtons.Add(btnObj);

                var button = btnObj.GetComponent<Button>();
                var text = btnObj.GetComponentInChildren<TMP_Text>();

                if (text != null) text.SetText(item.displayName);

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
