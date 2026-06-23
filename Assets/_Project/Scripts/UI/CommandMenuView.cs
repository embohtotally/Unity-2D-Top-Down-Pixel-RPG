using UnityEngine;
using UnityEngine.UI;

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

        public event System.Action OnAttackSelected;
        public event System.Action OnSkillSelected;
        public event System.Action OnGuardSelected;
        public event System.Action OnItemSelected;
        public event System.Action OnBatonPassSelected;
        public event System.Action OnSwitchPersonaSelected;

        private void Start()
        {
            if (attackButton != null) attackButton.onClick.AddListener(() => OnAttackSelected?.Invoke());
            if (skillButton != null) skillButton.onClick.AddListener(() => OnSkillSelected?.Invoke());
            if (guardButton != null) guardButton.onClick.AddListener(() => OnGuardSelected?.Invoke());
            if (itemButton != null) itemButton.onClick.AddListener(() => OnItemSelected?.Invoke());
            if (batonPassButton != null) batonPassButton.onClick.AddListener(() => OnBatonPassSelected?.Invoke());
            if (switchPersonaButton != null) switchPersonaButton.onClick.AddListener(() => OnSwitchPersonaSelected?.Invoke());
        }

        public void Show() 
        {
            if (menuPanel != null) menuPanel.SetActive(true);
        }
        
        public void Hide() 
        {
            if (menuPanel != null) menuPanel.SetActive(false);
        }
    }
}
