using UnityEngine;
using UnityEngine.UI;

namespace PixelMindscape.UI
{
    public class CommandMenuView : MonoBehaviour
    {
        [SerializeField] private Button attackButton;
        [SerializeField] private Button skillButton;
        [SerializeField] private Button guardButton;
        [SerializeField] private Button itemButton;

        public event System.Action OnAttackSelected;
        public event System.Action OnSkillSelected;
        public event System.Action OnGuardSelected;
        public event System.Action OnItemSelected;

        private void Start()
        {
            if (attackButton != null) attackButton.onClick.AddListener(() => OnAttackSelected?.Invoke());
            if (skillButton != null) skillButton.onClick.AddListener(() => OnSkillSelected?.Invoke());
            if (guardButton != null) guardButton.onClick.AddListener(() => OnGuardSelected?.Invoke());
            if (itemButton != null) itemButton.onClick.AddListener(() => OnItemSelected?.Invoke());
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
