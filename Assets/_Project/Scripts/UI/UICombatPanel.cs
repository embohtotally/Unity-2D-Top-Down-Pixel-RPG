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
        
        [SerializeField] private CommandMenuView commandMenu;
        [SerializeField] private TargetSelectionView targetSelection;

        private void OnEnable() 
        {
            if (battleManager != null) 
            {
                battleManager.OnTurnOrderChanged += HandleTurnOrderChanged;
                battleManager.OnTurnStarted += HandleTurnStarted;
            }

            if (commandMenu != null)
            {
                commandMenu.OnAttackSelected += HandleAttackSelected;
                // Hook up other actions here in the future
            }

            if (targetSelection != null)
            {
                targetSelection.OnTargetSelected += OnPlayerSelectsAttack;
                targetSelection.OnCancelled += () => 
                {
                    targetSelection.Hide();
                    commandMenu.Show();
                };
            }
            
            // DOTween pop-in animation
            if (panelRect != null)
            {
                panelRect.localScale = Vector3.zero;
                panelRect.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
            }
        }
        
        private void OnDisable() 
        {
            if (battleManager != null) 
            {
                battleManager.OnTurnOrderChanged -= HandleTurnOrderChanged;
                battleManager.OnTurnStarted -= HandleTurnStarted;
            }
        }

        private void HandleTurnOrderChanged()
        {
            // Update timeline UI
        }

        private void HandleTurnStarted(Combatant currentCombatant)
        {
            if (currentCombatant.IsPlayerSide)
            {
                commandMenu?.Show();
                targetSelection?.Hide();
            }
            else
            {
                commandMenu?.Hide();
                targetSelection?.Hide();
            }
        }

        private void HandleAttackSelected()
        {
            commandMenu.Hide();
            targetSelection.Show(battleManager.GetActiveEnemies());
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
