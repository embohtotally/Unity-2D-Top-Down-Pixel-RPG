using System.Collections.Generic;
using UnityEngine;
using PixelMindscape.Data;
using PixelMindscape.Core;
using PixelMindscape.Battle;
using DG.Tweening;

namespace PixelMindscape.UI
{
    public class UICombatPanel : MonoBehaviour
    {
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private RectTransform panelRect;
        
        // Placeholders for views
        // [SerializeField] private TimelineBarView timelineBar;
        // [SerializeField] private CommandMenuView commandMenu;

        private void OnEnable() 
        {
            if (battleManager != null) battleManager.OnTurnOrderChanged += HandleTurnOrderChanged;
            
            // DOTween pop-in animation
            if (panelRect != null)
            {
                panelRect.localScale = Vector3.zero;
                panelRect.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
            }
        }
        
        private void OnDisable() 
        {
            if (battleManager != null) battleManager.OnTurnOrderChanged -= HandleTurnOrderChanged;
        }

        private void HandleTurnOrderChanged()
        {
            // timelineBar.Refresh(battleManager.GetTurnQueueSnapshot());
        }

        public void OnPlayerSelectsAttack(Combatant target)
        {
            if (battleManager.ActiveCombatant != null)
            {
                battleManager.SubmitAction(new AttackAction 
                { 
                    Source = battleManager.ActiveCombatant, 
                    Targets = new List<Combatant> { target } 
                });
            }
        }
    }
}
