using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace PixelMindscape.UI
{
    public class CommandMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject menuPanel; // Assign the UI Panel here
        [SerializeField] private Button attackButton;
        [SerializeField] private Button skillButton;
        [SerializeField] private Button guardButton;
        [SerializeField] private Button itemButton;
        [SerializeField] private Button batonPassButton;
        [SerializeField] private Button switchPersonaButton;

        [Header("Animation Settings")]
        [SerializeField] private float slideDuration = 0.3f;
        [SerializeField] private float offscreenX = -500f;
        [SerializeField] private float onscreenX = 50f;
        [SerializeField] private Vector3 baseButtonScale = new Vector3(3f, 3f, 3f);
        [SerializeField] private Vector3 selectedButtonScale = new Vector3(3.45f, 3.45f, 3.45f);

        public event System.Action OnAttackSelected;
        public event System.Action OnSkillSelected;
        public event System.Action OnGuardSelected;
        public event System.Action OnItemSelected;
        public event System.Action OnBatonPassSelected;
        public event System.Action OnSwitchPersonaSelected;

        private List<Button> activeButtons = new List<Button>();
        private int selectedIndex = 0;
        private RectTransform panelRect;
        private bool isMenuVisible = false;

        private void Start()
        {
            if (menuPanel != null) panelRect = menuPanel.GetComponent<RectTransform>();

            if (attackButton != null) attackButton.onClick.AddListener(() => OnAttackSelected?.Invoke());
            if (skillButton != null) skillButton.onClick.AddListener(() => OnSkillSelected?.Invoke());
            if (guardButton != null) guardButton.onClick.AddListener(() => OnGuardSelected?.Invoke());
            if (itemButton != null) itemButton.onClick.AddListener(() => OnItemSelected?.Invoke());
            if (batonPassButton != null) batonPassButton.onClick.AddListener(() => OnBatonPassSelected?.Invoke());
            if (switchPersonaButton != null) switchPersonaButton.onClick.AddListener(() => OnSwitchPersonaSelected?.Invoke());
        }

        public void Show() 
        {
            isMenuVisible = true;
            if (menuPanel != null) menuPanel.SetActive(true);

            if (panelRect != null)
            {
                panelRect.anchoredPosition = new Vector2(offscreenX, panelRect.anchoredPosition.y);
                panelRect.DOAnchorPosX(onscreenX, slideDuration).SetEase(Ease.OutBack);
            }

            RefreshActiveButtons();
        }
        
        public void Hide() 
        {
            isMenuVisible = false;
            if (menuPanel != null) menuPanel.SetActive(false);
        }

        public void SetBatonPassAvailable(bool available)
        {
            Debug.Log($"[CommandMenuView] SetBatonPassAvailable({available}). batonPassButton assigned: {batonPassButton != null}");
            if (batonPassButton != null) batonPassButton.gameObject.SetActive(available);
            RefreshActiveButtons();
        }

        public void SetSwitchPersonaAvailable(bool available)
        {
            Debug.Log($"[CommandMenuView] SetSwitchPersonaAvailable({available}). switchPersonaButton assigned: {switchPersonaButton != null}");
            if (switchPersonaButton != null) switchPersonaButton.gameObject.SetActive(available);
            RefreshActiveButtons();
        }

        private void RefreshActiveButtons()
        {
            activeButtons.Clear();
            if (attackButton != null && attackButton.gameObject.activeInHierarchy) activeButtons.Add(attackButton);
            if (skillButton != null && skillButton.gameObject.activeInHierarchy) activeButtons.Add(skillButton);
            if (guardButton != null && guardButton.gameObject.activeInHierarchy) activeButtons.Add(guardButton);
            if (itemButton != null && itemButton.gameObject.activeInHierarchy) activeButtons.Add(itemButton);
            if (batonPassButton != null && batonPassButton.gameObject.activeInHierarchy) activeButtons.Add(batonPassButton);
            if (switchPersonaButton != null && switchPersonaButton.gameObject.activeInHierarchy) activeButtons.Add(switchPersonaButton);

            if (activeButtons.Count > 0)
            {
                selectedIndex = 0;
                SelectButton(selectedIndex);
            }
        }

        private void Update()
        {
            if (!isMenuVisible || activeButtons.Count == 0) return;

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || (Input.GetButtonDown("Vertical") && Input.GetAxis("Vertical") > 0))
            {
                selectedIndex = (selectedIndex - 1 + activeButtons.Count) % activeButtons.Count;
                SelectButton(selectedIndex);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || (Input.GetButtonDown("Vertical") && Input.GetAxis("Vertical") < 0))
            {
                selectedIndex = (selectedIndex + 1) % activeButtons.Count;
                SelectButton(selectedIndex);
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Submit"))
            {
                if (selectedIndex >= 0 && selectedIndex < activeButtons.Count)
                {
                    activeButtons[selectedIndex].onClick.Invoke();
                }
            }
        }

        private void SelectButton(int index)
        {
            for (int i = 0; i < activeButtons.Count; i++)
            {
                var btn = activeButtons[i];
                if (i == index)
                {
                    if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(btn.gameObject);
                    btn.transform.DOKill();
                    btn.transform.DOScale(selectedButtonScale, 0.2f).SetEase(Ease.OutBack);
                    btn.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.2f); // loud slice / pulse animation
                }
                else
                {
                    btn.transform.DOKill();
                    btn.transform.DOScale(baseButtonScale, 0.2f).SetEase(Ease.OutCubic);
                }
            }
        }
    }
}
