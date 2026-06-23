using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelMindscape.Battle;

namespace PixelMindscape.UI
{
    public class UICombatantInfo : MonoBehaviour
    {
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private GameObject infoPanel;
        
        [Header("UI Elements")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text spText;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Slider spSlider;

        private void OnEnable()
        {
            if (battleManager != null)
                battleManager.OnTurnStarted += HandleTurnStarted;
        }

        private void OnDisable()
        {
            if (battleManager != null)
                battleManager.OnTurnStarted -= HandleTurnStarted;
        }

        private void HandleTurnStarted(Combatant currentCombatant)
        {
            if (currentCombatant.IsPlayerSide)
            {
                if (infoPanel != null) infoPanel.SetActive(true);

                if (nameText != null) nameText.text = currentCombatant.gameObject.name;
                
                if (hpText != null) hpText.text = $"HP: {currentCombatant.CurrentHP} / {currentCombatant.MaxHP}";
                if (hpSlider != null) 
                {
                    hpSlider.maxValue = currentCombatant.MaxHP;
                    hpSlider.value = currentCombatant.CurrentHP;
                }

                if (spText != null) spText.text = $"SP: {currentCombatant.CurrentSP} / {currentCombatant.MaxSP}";
                if (spSlider != null) 
                {
                    spSlider.maxValue = currentCombatant.MaxSP;
                    spSlider.value = currentCombatant.CurrentSP;
                }
            }
            else
            {
                // Hide or show enemy info depending on your design. Usually hidden for player command phase.
                if (infoPanel != null) infoPanel.SetActive(false);
            }
        }
    }
}
