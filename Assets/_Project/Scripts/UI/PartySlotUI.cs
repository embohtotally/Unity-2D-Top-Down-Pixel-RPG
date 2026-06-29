using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelMindscape.Core;
using PixelMindscape.Data;
using PixelMindscape.Battle;
using DG.Tweening;

namespace PixelMindscape.UI
{
    public class PartySlotUI : MonoBehaviour
    {
        [SerializeField] private Image portraitImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image hpBar;
        [SerializeField] private Image spBar;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI spText;
        [SerializeField] private Color normalSPColor = new Color(0f, 0.8f, 1f, 1f); // Bright Cyan
        [SerializeField] private Color lowSPColor = new Color(1f, 0.2f, 0.2f, 1f); // Red for tension cue

        private string boundMemberId;
        private Combatant boundCombatant;

        public string BoundMemberId => boundMemberId;
        public Combatant BoundCombatant => boundCombatant;

        public void Bind(PartyMemberRuntimeState state, CharacterData data, Combatant combatant = null)
        {
            boundMemberId = state != null ? state.characterId : (data != null ? data.characterId : "");
            boundCombatant = combatant;

            // 1. Resolve Portrait
            if (portraitImage != null)
            {
                if (data != null && data.overworldPortrait != null)
                    portraitImage.sprite = data.overworldPortrait;
                else if (combatant != null && combatant.TurnPortrait != null)
                    portraitImage.sprite = combatant.TurnPortrait;
                else if (combatant != null && combatant.GetComponentInChildren<SpriteRenderer>() != null)
                    portraitImage.sprite = combatant.GetComponentInChildren<SpriteRenderer>().sprite;
            }

            // 2. Resolve Name
            if (nameText != null)
            {
                if (data != null && !string.IsNullOrEmpty(data.displayName))
                    nameText.text = data.displayName;
                else if (combatant != null)
                    nameText.text = combatant.gameObject.name.Replace("Combatant", "").Replace("(Clone)", "").Trim();
            }

            // 3. Resolve Stats
            int currentHP = combatant != null ? combatant.CurrentHP : (state != null ? state.currentHP : (data != null ? data.baseHP : 100));
            int maxHP = combatant != null ? combatant.MaxHP : (data != null ? data.baseHP : (state != null ? state.currentHP : 100));
            int currentSP = combatant != null ? combatant.CurrentSP : (state != null ? state.currentSP : (data != null ? data.baseSP : 50));
            int maxSP = combatant != null ? combatant.MaxSP : (data != null ? data.baseSP : (state != null ? state.currentSP : 50));

            UpdateBars(currentHP, maxHP, currentSP, maxSP);
        }

        public void UpdateBars(int currentHP, int maxHP, int currentSP, int maxSP)
        {
            if (maxHP <= 0) maxHP = 1;
            if (maxSP <= 0) maxSP = 1;

            float targetHpFill = (float)currentHP / maxHP;
            float targetSpFill = (float)currentSP / maxSP;

            if (hpBar != null) hpBar.DOFillAmount(targetHpFill, 0.3f).SetEase(Ease.OutCubic);
            if (spBar != null) spBar.DOFillAmount(targetSpFill, 0.3f).SetEase(Ease.OutCubic);

            if (hpText != null) hpText.text = $"{currentHP} / {maxHP}";
            if (spText != null) spText.text = $"{currentSP} / {maxSP}";

            // Low SP tension cue (< 25%)
            if (spBar != null)
            {
                if (targetSpFill < 0.25f)
                {
                    spBar.color = lowSPColor;
                }
                else
                {
                    spBar.color = normalSPColor;
                }
            }
        }
    }
}
