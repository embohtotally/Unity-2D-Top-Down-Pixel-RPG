using UnityEngine;
using UnityEngine.UI;
using PixelMindscape.Battle;
using DG.Tweening;

namespace PixelMindscape.UI
{
    public class TurnSlot : MonoBehaviour
    {
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image backgroundRing;
        [SerializeField] private Color allyColor = new Color(0.1f, 0.4f, 1f, 1f); // Blue
        [SerializeField] private Color enemyColor = new Color(1f, 0.2f, 0.2f, 1f); // Red
        [SerializeField] private GameObject highlightGlow;

        public Combatant Combatant { get; private set; }

        public void Bind(Combatant combatant, bool isCurrent, bool isOneMore)
        {
            Combatant = combatant;

            if (portraitImage != null)
            {
                if (combatant is HeroCombatant hero && hero.CharacterData != null && hero.CharacterData.overworldPortrait != null)
                {
                    portraitImage.sprite = hero.CharacterData.overworldPortrait;
                }
                else if (combatant is EnemyCombatant enemy && enemy.EnemyData != null && enemy.EnemyData.summonFlashFrames != null && enemy.EnemyData.summonFlashFrames.Length > 0)
                {
                    portraitImage.sprite = enemy.EnemyData.summonFlashFrames[0];
                }
                else if (combatant.TurnPortrait != null)
                {
                    portraitImage.sprite = combatant.TurnPortrait;
                }
            }

            if (backgroundRing != null)
            {
                backgroundRing.color = combatant.IsPlayerSide ? allyColor : enemyColor;
            }

            if (highlightGlow != null)
            {
                highlightGlow.SetActive(isCurrent);
            }

            // DOTween Animation
            if (isCurrent)
            {
                transform.localScale = Vector3.one * 1.25f;
                if (isOneMore)
                {
                    // Pulse animation for '1 More'
                    transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.5f, 5, 0.5f);
                }
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }
    }
}
