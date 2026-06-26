using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelMindscape.Data;
using PixelMindscape.Core;

namespace PixelMindscape.Battle
{
    public enum BattleState { Start, PlayerTurn, EnemyTurn, Resolving, PromptingAllOut, Negotiation, Victory, Defeat }

    public class BattleManager : MonoBehaviour, IBattleManager
    {
        public static BattleManager Instance { get; private set; }

        [Header("UI & Effects")]
        public DamagePopup DamagePopupPrefab;

        [Header("Debug Info")]
        [field: SerializeField] public BattleState CurrentState { get; private set; }
        
        public event System.Action OnTurnOrderChanged;
        public event System.Action<Combatant> OnTurnStarted;
        public event System.Action<Combatant> OnOneMoreTriggered;
        
        // Event for UI to prompt negotiation vs all-out attack
        public event System.Action OnPromptAllOutAttack;

        private bool isQueuePaused = false;

        [SerializeField] private List<Combatant> turnQueue = new List<Combatant>();
        [SerializeField] private List<Combatant> activeParty = new List<Combatant>();
        [SerializeField] private List<Combatant> activeEnemies = new List<Combatant>();

        private int batonPassChainCount = 0;
        private const int MaxBatonPassStacks = 3;

        private BattleAction pendingAction = null;

        public Combatant ActiveCombatant => turnQueue.Count > 0 ? turnQueue[0] : null;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        [Header("Testing Auto-Start")]
        [SerializeField] private bool autoStartBattle = false;
        [SerializeField] private Transform heroesContainer;
        [SerializeField] private Transform enemiesContainer;

        private void Start()
        {
            // Register self with GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.Battle = this;

            // Auto-start for testing purposes
            if (autoStartBattle && heroesContainer != null && enemiesContainer != null)
            {
                var party = new List<Combatant>(heroesContainer.GetComponentsInChildren<Combatant>());
                var enemies = new List<Combatant>(enemiesContainer.GetComponentsInChildren<Combatant>());
                StartBattle(party, enemies);
            }
        }

        public void StartBattle(List<Combatant> party, List<Combatant> enemies)
        {
            activeParty = party;
            activeEnemies = enemies;
            batonPassChainCount = 0;
            CurrentState = BattleState.Start;
            CalculateTurnOrder();
            StartCoroutine(BattleLoopRoutine());
        }

        public void CalculateTurnOrder()
        {
            var all = new List<Combatant>();
            all.AddRange(activeParty);
            all.AddRange(activeEnemies);

            all.Sort((a, b) => b.EffectiveAgility.CompareTo(a.EffectiveAgility));
            turnQueue = all;
            OnTurnOrderChanged?.Invoke();
        }

        public void SubmitAction(BattleAction action)
        {
            if (CurrentState != BattleState.PlayerTurn && CurrentState != BattleState.EnemyTurn) return;
            pendingAction = action;
        }

        private IEnumerator BattleLoopRoutine()
        {
            while (CurrentState != BattleState.Victory && CurrentState != BattleState.Defeat)
            {
                if (turnQueue.Count == 0) yield break;

                var current = turnQueue[0];

                // Remove dead combatants from queue
                if (current.IsDefeated)
                {
                    turnQueue.RemoveAt(0);
                    OnTurnOrderChanged?.Invoke();
                    continue;
                }

                // Reset baton pass chain if it's an enemy's turn
                if (!current.IsPlayerSide)
                {
                    batonPassChainCount = 0;
                }

                OnTurnStarted?.Invoke(current);
                while (isQueuePaused) yield return null;

                CurrentState = current.IsPlayerSide ? BattleState.PlayerTurn : BattleState.EnemyTurn;

                if (CurrentState == BattleState.EnemyTurn)
                {
                    ExecuteEnemyAI(current);
                }

                // Wait for an action to be submitted (by UI or AI)
                while (pendingAction == null) yield return null;

                var action = pendingAction;
                pendingAction = null;

                CurrentState = BattleState.Resolving;

                bool isBatonPass = action is BatonPassAction;
                
                // Wait for choreographed action coroutine to complete
                yield return StartCoroutine(action.Execute(this));
                bool wasWeaknessHit = action.WasWeaknessHit;

                // Check end conditions after action
                CheckBattleEndConditions();
                if (CurrentState == BattleState.Victory || CurrentState == BattleState.Defeat) yield break;

                if (wasWeaknessHit)
                {
                    current.GrantOneMore(); 
                    OnOneMoreTriggered?.Invoke(current);
                }

                // Handle All Enemies Down scenario
                if (AllEnemiesDown() && current.IsPlayerSide && !isBatonPass)
                {
                    CurrentState = BattleState.PromptingAllOut;
                    OnPromptAllOutAttack?.Invoke();
                    
                    // Wait for player to choose All-Out, Negotiation, or Cancel
                    while (CurrentState == BattleState.PromptingAllOut || CurrentState == BattleState.Negotiation)
                    {
                        yield return null;
                    }
                }

                // Turn Queue Management
                if (isBatonPass)
                {
                    // Baton pass removes current combatant's extra turn.
                    // The receiver was already moved to front in PerformBatonPass.
                    current.ClearOneMore();
                    turnQueue.Remove(current);
                    if (!current.IsDefeated) turnQueue.Add(current);
                }
                else if (current.HasOneMore)
                {
                    // Combatant gets to go again immediately
                    current.ClearOneMore();
                    // Remains at the front of the queue, do not move to back
                }
                else
                {
                    // Normal turn ends, move to back
                    turnQueue.Remove(current);
                    if (!current.IsDefeated) turnQueue.Add(current);
                    
                    // Reset baton pass chain since the combo is broken
                    if (current.IsPlayerSide) batonPassChainCount = 0;
                }

                OnTurnOrderChanged?.Invoke();
                CheckBattleEndConditions();
            }
        }

        private void ExecuteEnemyAI(Combatant enemy)
        {
            SkillData bestSkill = null;
            Combatant bestTarget = null;

            // 1. Priority: Find a skill that hits a weakness
            var availableSkills = enemy.GetAvailableSkills();
            foreach (var skill in availableSkills)
            {
                if (enemy.CurrentSP < skill.spCost) continue;

                foreach (var partyMember in activeParty)
                {
                    if (partyMember.IsDefeated) continue;

                    var result = DamageCalculator.CalculateDamage(enemy, partyMember, skill);
                    if (result.affinity == Affinity.Weak)
                    {
                        bestSkill = skill;
                        bestTarget = partyMember;
                        break;
                    }
                }
                if (bestSkill != null) break;
            }

            // 2. Fallback: Attack lowest HP target
            if (bestSkill != null && bestTarget != null)
            {
                SubmitAction(new SkillAction 
                { 
                    Source = enemy, 
                    Targets = new List<Combatant> { bestTarget },
                    skill = bestSkill
                });
            }
            else
            {
                Combatant lowestHpTarget = null;
                foreach (var p in activeParty)
                {
                    if (p.IsDefeated) continue;
                    if (lowestHpTarget == null || p.HpPercent < lowestHpTarget.HpPercent)
                        lowestHpTarget = p;
                }

                if (lowestHpTarget != null)
                {
                    SubmitAction(new AttackAction 
                    { 
                        Source = enemy, 
                        Targets = new List<Combatant> { lowestHpTarget } 
                    });
                }
            }
        }

        public void PerformBatonPass(Combatant from, Combatant to)
        {
            // Increase chain count
            batonPassChainCount = Mathf.Min(batonPassChainCount + 1, MaxBatonPassStacks);
            to.ApplyBatonPassBuff(batonPassChainCount); 
            to.GrantOneMore();

            // Reorder queue: move receiver to the front so they act immediately
            turnQueue.Remove(to);
            turnQueue.Insert(0, to);
            OnTurnOrderChanged?.Invoke();
        }

        public void ResolveAllOutAttack()
        {
            StartCoroutine(ResolveAllOutAttackRoutine());
        }

        private IEnumerator ResolveAllOutAttackRoutine()
        {
            CurrentState = BattleState.Resolving;

            if (BattleCinematicManager.Instance != null)
            {
                yield return StartCoroutine(BattleCinematicManager.Instance.PlayAllOutAttackSplashRoutine());
            }

            float allOutDamage = CalculateAllOutDamage();
            foreach (var enemy in activeEnemies)
            {
                if (!enemy.IsDefeated)
                    enemy.TakeDamage(allOutDamage, Element.Almighty);
            }

            ClearEnemiesDownState();
            // Loop will continue naturally
        }

        public void EnterNegotiation()
        {
            CurrentState = BattleState.Negotiation;
            // E.g., NegotiationUI.Instance.Show(...)
        }

        public void EndNegotiationPhase(bool success)
        {
            if (!success)
            {
                ClearEnemiesDownState(); // Stand back up if negotiation fails
            }
            CurrentState = BattleState.Resolving; // Return to loop
        }

        public void CancelAllOutPrompt()
        {
            CurrentState = BattleState.Resolving; // Return to loop without doing anything
        }

        private void ClearEnemiesDownState()
        {
            foreach (var enemy in activeEnemies)
            {
                enemy.SetDown(false);
            }
        }

        public bool AllEnemiesDown() => activeEnemies.TrueForAll(e => e.IsDown || e.IsDefeated);
        
        public List<Combatant> GetActiveEnemies() => activeEnemies;

        private void CheckBattleEndConditions()
        {
            if (activeEnemies.Count > 0 && activeEnemies.TrueForAll(e => e.IsDefeated)) 
                CurrentState = BattleState.Victory;
            else if (activeParty.Count > 0 && activeParty.TrueForAll(p => p.IsDefeated)) 
                CurrentState = BattleState.Defeat;
        }

        public void GrantPassiveAbility(string abilityId)
        {
            // registers the ability with the protagonist's passive ability set
            // TODO: To be implemented with the Confidant system.
        }

        public void PauseQueue() { isQueuePaused = true; }
        public void ResumeQueue() { isQueuePaused = false; }

        private float CalculateAllOutDamage() { return 100f; } // Placeholder
        
        public List<Combatant> GetTurnQueueSnapshot() => new List<Combatant>(turnQueue);
    }
}
