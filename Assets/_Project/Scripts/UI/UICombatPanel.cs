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
        [SerializeField] private GameObject weaknessPopupPrefab; // Big red 'WEAK' text popup
        [SerializeField] private TurnTimelineBar turnTimelineBar;

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
            Debug.Log($"[UICombatPanel] Weakness hit! Triggering 'WEAK' popup and '1 MORE' for {combatant.gameObject.name}");
            
            // 1. Weakness Popup & Fist Pump Animation
            if (weaknessPopupPrefab != null)
            {
                var weakPopup = Instantiate(weaknessPopupPrefab, combatant.transform.position + Vector3.up * 2.5f, Quaternion.identity);
                weakPopup.transform.localScale = Vector3.zero;
                weakPopup.transform.DOScale(Vector3.one * 1.5f, 0.3f).SetEase(Ease.OutBack);
                weakPopup.transform.DOMoveY(weakPopup.transform.position.y + 1f, 1f).SetEase(Ease.OutCubic);
                var text = weakPopup.GetComponentInChildren<TMPro.TMP_Text>();
                if (text != null) text.DOFade(0, 1f).SetDelay(0.5f);
                Destroy(weakPopup, 1.5f);
            }

            // Quick fist pump / victory animation
            var animator = combatant.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Victory"); // or fist pump
            }

            // 2. 1 MORE Popup
            if (oneMoreTextPrefab != null)
            {
                var popup = Instantiate(oneMoreTextPrefab, combatant.transform.position + Vector3.up * 1.5f, Quaternion.identity);
                popup.transform.localScale = Vector3.zero;
                popup.transform.DOScale(Vector3.one * 1.3f, 0.4f).SetDelay(0.3f).SetEase(Ease.OutBack);
                popup.transform.DOMoveY(popup.transform.position.y + 1f, 1f).SetDelay(0.3f).SetEase(Ease.OutCubic);
                var text = popup.GetComponentInChildren<TMPro.TMP_Text>();
                if (text != null) text.DOFade(0, 1f).SetDelay(0.7f);
                Destroy(popup, 1.8f);
            }

            // Re-open command menu after delay
            if (combatant.IsPlayerSide)
            {
                DOVirtual.DelayedCall(1.0f, () => 
                {
                    if (commandMenu != null)
                    {
                        commandMenu.SetBatonPassAvailable(true);
                        SwitchState(CombatUIState.CommandSelect);
                    }
                });
            }
        }

        [SerializeField] private PersonaSelectionView personaSelection;

        public void SwitchState(CombatUIState newState)
        {
            CurrentState = newState;
            
            // Hide everything first
            commandMenu?.Hide();
            targetSelection?.Hide();
            skillSelection?.Hide();
            itemSelection?.Hide();
            personaSelection?.Hide();

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
                Debug.Log($"[UICombatPanel] Turn started for player '{currentCombatant.gameObject.name}'. IsProtagonist={currentCombatant.IsProtagonist}, HasSwitchedPersona={currentCombatant.HasSwitchedPersonaThisTurn}, HasOneMore={currentCombatant.HasOneMore}");
                commandMenu?.SetBatonPassAvailable(currentCombatant.HasOneMore);
                commandMenu?.SetSwitchPersonaAvailable(currentCombatant.IsProtagonist && !currentCombatant.HasSwitchedPersonaThisTurn);
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
            
            // Filter activeParty to exclude self, defeated, and downed allies
            var allParty = battleManager.GetActiveParty();
            var validTargets = new List<Combatant>();
            foreach (var ally in allParty)
            {
                if (ally != battleManager.ActiveCombatant && !ally.IsDefeated && !ally.IsDown)
                {
                    validTargets.Add(ally);
                }
            }
            
            if (validTargets.Count == 0)
            {
                Debug.LogWarning("[UICombatPanel] No valid allies available to receive Baton Pass!");
                SwitchState(CombatUIState.CommandSelect);
                return;
            }
            
            targetSelection.Show(validTargets, (target) => 
            {
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
            if (isInputLocked || battleManager.ActiveCombatant == null) return;
            
            var hero = battleManager.ActiveCombatant as HeroCombatant;
            if (hero == null || !hero.IsProtagonist || hero.HasSwitchedPersonaThisTurn) 
            {
                Debug.LogWarning("Switch Persona is not available for this combatant right now.");
                return;
            }

            if (personaSelection != null)
            {
                SwitchState(CombatUIState.SkillSelect); // use skill select enum state to hide other menus
                personaSelection.Show(hero.GetPersonaLoadout(), (selectedPersona) => 
                {
                    hero.SwitchPersona(selectedPersona);
                    commandMenu?.SetSwitchPersonaAvailable(false); // Disabled for rest of turn
                    SwitchState(CombatUIState.CommandSelect); // Return to command select immediately without advancing turn!
                }, 
                () => 
                {
                    SwitchState(CombatUIState.CommandSelect);
                });
            }
            else
            {
                Debug.LogWarning("PersonaSelectionView is not assigned in the Inspector!");
            }
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
            if (turnTimelineBar != null)
            {
                turnTimelineBar.Refresh(battleManager.GetTurnQueueSnapshot());
                return;
            }

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
