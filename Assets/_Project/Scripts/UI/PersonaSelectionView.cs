using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelMindscape.Data;

namespace PixelMindscape.UI
{
    public class PersonaSelectionView : MonoBehaviour
    {
        [SerializeField] private GameObject selectionPanel; // Assign the UI Panel here
        [SerializeField] private Transform buttonContainer; // Assign a container with VerticalLayoutGroup
        [SerializeField] private GameObject personaButtonPrefab; // Optional prefab with Button and TMP_Text
        [SerializeField] private Button cancelButton;

        private System.Action<PersonaData> onPersonaSelectedCallback;
        private System.Action onCancelledCallback;
        private List<GameObject> activeButtons = new List<GameObject>();

        private void Start()
        {
            if (cancelButton != null) cancelButton.onClick.AddListener(CancelSelection);
        }

        public void Show(List<PersonaData> personas, System.Action<PersonaData> onSelected, System.Action onCancel = null)
        {
            onPersonaSelectedCallback = onSelected;
            onCancelledCallback = onCancel;
            if (selectionPanel != null) selectionPanel.SetActive(true);

            ClearButtons();

            if (personas == null || personas.Count == 0)
            {
                Debug.LogWarning("[PersonaSelectionView] Show called with 0 personas! Ensure the Protagonist has PersonaData assigned to 'Persona Loadout' in the Inspector.");
                return;
            }

            Debug.Log($"[PersonaSelectionView] Spawning buttons for {personas.Count} personas...");

            Transform actualContainer = buttonContainer != null ? buttonContainer : (selectionPanel != null ? selectionPanel.transform : transform);

            foreach (var persona in personas)
            {
                GameObject btnObj = null;
                if (personaButtonPrefab != null)
                {
                    btnObj = Instantiate(personaButtonPrefab, actualContainer);
                }
                else
                {
                    Debug.LogWarning("[PersonaSelectionView] PersonaButtonPrefab is not assigned in the Inspector! Generating a default UI button dynamically.");
                    btnObj = new GameObject($"Btn_{persona.personaId}");
                    btnObj.transform.SetParent(actualContainer, false);
                    var rect = btnObj.AddComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(400, 60);
                    var img = btnObj.AddComponent<Image>();
                    img.color = new Color32(40, 20, 60, 240); // sleek deep purple for personas
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

                if (text != null) text.SetText($"{persona.displayName} ({persona.arcana}) - Lv {persona.baseLevel}");

                var currentPersona = persona;
                if (button != null) button.onClick.AddListener(() => 
                {
                    var callback = onPersonaSelectedCallback;
                    Hide();
                    callback?.Invoke(currentPersona);
                });
            }
        }

        public void Hide()
        {
            onPersonaSelectedCallback = null;
            onCancelledCallback = null;
            if (selectionPanel != null) selectionPanel.SetActive(false);
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

        private void CancelSelection()
        {
            var callback = onCancelledCallback;
            Hide();
            callback?.Invoke();
        }
    }
}
