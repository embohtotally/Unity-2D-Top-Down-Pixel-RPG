using System.Collections.Generic;
using UnityEngine;
using PixelMindscape.Data;
using PixelMindscape.Core;
using PixelMindscape.Battle;
using DG.Tweening;

namespace PixelMindscape.UI
{
    public enum CombatUIState { Idle, CommandSelect, TargetSelect, SkillSelect, ItemSelect }

    public class UICombatPanel : MonoBehaviour
    {
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private RectTransform panelRect;
        
        [SerializeField] private CommandMenuView commandMenu;
        [SerializeField] private TargetSelectionView targetSelection;
        [SerializeField] private SkillSelectionView skillSelection;
        [SerializeField] private ItemSelectionView itemSelection;

        [Header("Turn Order UI")]
        [SerializeField] private Transform turnOrderContainer;
        [SerializeField] private GameObject turnPortraitPrefab; // Should have an Image component
        private List<GameObject> activeTurnPortraits = new List<GameObject>();

        [Header("Feedback UI")]
        [SerializeField] private GameObject oneMoreTextPrefab; // Prefab with TMP_Text saying "1 MORE!"

        public CombatUIState CurrentState { get; private set; } = CombatUIState.Idle;
        private bool isInputLocked = false;

        private void OnEnable() 
        {
            if (battleManager != null) 
            {
                battleManager.OnTurnOrderChanged += HandleTurnOrderChanged;
                battleManager.OnTurnStarted += HandleTurnStarted;
                battleManager.OnOneMoreTriggered += HandleOneMoreTriggered;
            }

            if (commandMenu != null)
            {
                commandMenu.OnAttackSelected += HandleAttackSelected;
                commandMenu.OnGuardSelected += HandleGuardSelected;
                commandMenu.OnSkillSelected += HandleSkillSelected;
                commandMenu.OnItemSelected += HandleItemSelected;
                commandMenu.OnBatonPassSelected += HandleBatonPassSelected;
                commandMenu.OnSwitchPersonaSelected += HandleSwitchPersonaSelected;
            }
            
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
                battleManager.OnOneMoreTriggered -= HandleOneMoreTriggered;
            }

            if (commandMenu != null)
            {
                commandMenu.OnAttackSelected -= HandleAttackSelected;
                commandMenu.OnGuardSelected -= HandleGuardSelected;
                commandMenu.OnSkillSelected -= HandleSkillSelected;
                commandMenu.OnItemSelected -= HandleItemSelected;
                commandMenu.OnBatonPassSelected -= HandleBatonPassSelected;
                commandMenu.OnSwitchPersonaSelected -= HandleSwitchPersonaSelected;
            }
        }

        private void HandleOneMoreTriggered(Combatant combatant)
        {
            if (oneMoreTextPrefab != null)
            {
                // Spawn it slightly above the combatant
                var popup = Instantiate(oneMoreTextPrefab, combatant.transform.position + Vector3.up * 1.5f, Quaternion.identity);
                
                // Animate it
                popup.transform.DOMoveY(popup.transform.position.y + 1f, 1f).SetEase(Ease.OutCubic);
                var text = popup.GetComponentInChildren<TMPro.TMP_Text>();
                if (text != null)
                {
                    text.DOFade(0, 1f).SetEase(Ease.InExpo);
                }
                Destroy(popup, 1.5f);
            }
            else
            {
                Debug.Log($"1 MORE for {combatant.name}!");
            }
        }

        public void SwitchState(CombatUIState newState)
        {
            CurrentState = newState;
            
            // Hide everything first
            commandMenu?.Hide();
            targetSelection?.Hide();
            skillSelection?.Hide();
            itemSelection?.Hide();

            if (isInputLocked) return;

            // Only show views that don't need parameters.
            if (CurrentState == CombatUIState.CommandSelect)
            {
                commandMenu?.Show();
            }
        }

        private void HandleTurnStarted(Combatant currentCombatant)
        {
            isInputLocked = false; // Turn started, unlock input

            if (currentCombatant.IsPlayerSide)
            {
                SwitchState(CombatUIState.CommandSelect);
            }
            else
            {
                SwitchState(CombatUIState.Idle);
            }
        }

        private void HandleAttackSelected()
        {
            if (isInputLocked) return;
            SwitchState(CombatUIState.TargetSelect);
            
            targetSelection.Show(battleManager.GetActiveEnemies(), OnPlayerSelectsAttack, () => 
            {
                SwitchState(CombatUIState.CommandSelect); // Cancelled
            });
        }

        private void HandleGuardSelected()
        {
            if (isInputLocked || battleManager.ActiveCombatant == null) return;
            
            isInputLocked = true;
            SwitchState(CombatUIState.Idle);
            battleManager.SubmitAction(new GuardAction { Source = battleManager.ActiveCombatant });
        }

        private void HandleSkillSelected()
        {
            if (isInputLocked) return;
            
            if (skillSelection != null)
            {
                SwitchState(CombatUIState.SkillSelect);
                Combatant activeHero = battleManager.ActiveCombatant;
                if (activeHero == null && battleManager.GetActiveParty().Count > 0)
                {
                    activeHero = battleManager.GetActiveParty()[0];
                }
                if (activeHero == null || activeHero.GetAvailableSkills().Count == 0)
                {
                    // Look through all heroes in the entire scene to find the one where the user assigned skills!
                    var allHeroes = FindObjectsOfType<HeroCombatant>(true);
                    foreach (var hero in allHeroes)
                    {
                        if (hero.GetAvailableSkills().Count > 0)
                        {
                            activeHero = hero;
                            break;
                        }
                    }
                    if (activeHero == null && allHeroes.Length > 0)
                    {
                        activeHero = allHeroes[0];
                    }
                }

                var skills = activeHero != null ? activeHero.GetAvailableSkills() : new List<SkillData>();
                if (activeHero != null) Debug.Log($"[UICombatPanel] Found active hero '{activeHero.gameObject.name}' with {skills.Count} skills.");
                else Debug.LogWarning("[UICombatPanel] No active hero could be detected in the scene!");

                skillSelection.Show(skills, OnPlayerSelectsSkill, () => 
                {
                    SwitchState(CombatUIState.CommandSelect);
                });
            }
            else
            {
                Debug.LogWarning("SkillSelectionView is not assigned!");
            }
        }

        [Header("Inventory Data")]
        [SerializeField] private List<ItemData> availableItems = new List<ItemData>();

        private void HandleItemSelected()
        {
            if (isInputLocked) return;
            
            if (itemSelection != null)
            {
                SwitchState(CombatUIState.ItemSelect);
                var items = availableItems != null ? availableItems : new List<ItemData>();
                Debug.Log($"[UICombatPanel] Opening Item menu with {items.Count} items.");
                itemSelection.Show(items, OnPlayerSelectsItem, () => 
                {
                    SwitchState(CombatUIState.CommandSelect);
                });
            }
            else
            {
                Debug.LogWarning("ItemSelectionView is not assigned!");
            }
        }

        private void HandleBatonPassSelected()
        {
            if (isInputLocked || battleManager.ActiveCombatant == null) return;
            
            SwitchState(CombatUIState.TargetSelect);
            var validTargets = battleManager.GetActiveParty();
            
            targetSelection.Show(validTargets, (target) => 
            {
                if (target == battleManager.ActiveCombatant)
                {
                    Debug.LogWarning("Cannot Baton Pass to yourself!");
                    SwitchState(CombatUIState.CommandSelect);
                    return;
                }
                isInputLocked = true;
                SwitchState(CombatUIState.Idle);
                battleManager.SubmitAction(new BatonPassAction 
                { 
                    Source = battleManager.ActiveCombatant, 
                    PassTo = target 
                });
            }, 
            () => 
            {
                SwitchState(CombatUIState.CommandSelect);
            });
        }

        private void HandleSwitchPersonaSelected()
        {
            if (isInputLocked) return;
            Debug.LogWarning("Switch Persona UI is not fully implemented.");
        }

        private void OnPlayerSelectsAttack(Combatant target)
        {
            if (isInputLocked || battleManager.ActiveCombatant == null) return;

            isInputLocked = true;
            SwitchState(CombatUIState.Idle);
            battleManager.SubmitAction(new AttackAction 
            { 
                Source = battleManager.ActiveCombatant, 
                Targets = new List<Combatant> { target } 
            });
        }

        private void OnPlayerSelectsSkill(SkillData skill)
        {
            if (isInputLocked || battleManager.ActiveCombatant == null) return;
            
            SwitchState(CombatUIState.TargetSelect);
            
            bool targetsAllies = skill.targetScope == TargetScope.SingleAlly || skill.targetScope == TargetScope.AllAllies || skill.targetScope == TargetScope.Self;
            var validTargets = targetsAllies ? battleManager.GetActiveParty() : battleManager.GetActiveEnemies();

            targetSelection.Show(validTargets, (target) => 
            {
                isInputLocked = true;
                SwitchState(CombatUIState.Idle);
                
                bool isAllTarget = skill.targetScope == TargetScope.AllEnemies || skill.targetScope == TargetScope.AllAllies;
                var selectedTargets = isAllTarget ? validTargets : new List<Combatant> { target };

                battleManager.SubmitAction(new SkillAction 
                { 
                    Source = battleManager.ActiveCombatant, 
                    Targets = selectedTargets,
                    skill = skill
                });
            }, 
            () => 
            {
                SwitchState(CombatUIState.CommandSelect);
            });
        }

        private void OnPlayerSelectsItem(ItemData item)
        {
            if (isInputLocked || battleManager.ActiveCombatant == null) return;
            
            SwitchState(CombatUIState.TargetSelect);
            var validTargets = battleManager.GetActiveParty();

            targetSelection.Show(validTargets, (target) => 
            {
                isInputLocked = true;
                SwitchState(CombatUIState.Idle);
                
                var selectedTargets = item.reviveTarget == 2 ? validTargets : new List<Combatant> { target };

                battleManager.SubmitAction(new ItemAction 
                { 
                    Source = battleManager.ActiveCombatant, 
                    Targets = selectedTargets,
                    Item = item
                });
            }, 
            () => 
            {
                SwitchState(CombatUIState.CommandSelect);
            });
        }

        private void HandleTurnOrderChanged()
        {
            if (turnOrderContainer == null || turnPortraitPrefab == null) return;

            // Clear old portraits
            foreach (var portrait in activeTurnPortraits)
                Destroy(portrait);
            activeTurnPortraits.Clear();

            // Build new portraits from queue
            var queue = battleManager.GetTurnQueueSnapshot();
            for (int i = 0; i < queue.Count; i++)
            {
                var portraitObj = Instantiate(turnPortraitPrefab, turnOrderContainer);
                activeTurnPortraits.Add(portraitObj);

                var img = portraitObj.GetComponent<UnityEngine.UI.Image>();
                if (img != null && queue[i].TurnPortrait != null)
                {
                    img.sprite = queue[i].TurnPortrait;
                }

                // Highlight the active combatant
                if (i == 0)
                {
                    portraitObj.transform.localScale = Vector3.one * 1.2f;
                    if (img != null) img.color = Color.white;
                }
                else
                {
                    portraitObj.transform.localScale = Vector3.one;
                    if (img != null) img.color = new Color(0.7f, 0.7f, 0.7f, 1f); // slightly dimmed
                }
            }
        }
    }
}
