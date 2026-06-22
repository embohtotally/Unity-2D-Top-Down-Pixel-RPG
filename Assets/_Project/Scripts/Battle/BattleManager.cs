using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelMindscape.Data;
using PixelMindscape.Core;

namespace PixelMindscape.Battle
{
    public enum BattleState { Start, PlayerTurn, EnemyTurn, Resolving, Negotiation, Victory, Defeat }

    public class BattleManager : MonoBehaviour, IBattleManager
    {
        public static BattleManager Instance { get; private set; }

        [Header("UI & Effects")]
        public DamagePopup DamagePopupPrefab;

        public BattleState CurrentState { get; private set; }
        public event System.Action OnTurnOrderChanged;
        public event System.Action<Combatant> OnTurnStarted;

        private bool isQueuePaused = false;

        private List<Combatant> turnQueue = new List<Combatant>();
        private List<Combatant> activeParty = new List<Combatant>();
        private List<Combatant> activeEnemies = new List<Combatant>();

        private int batonPassChainCount = 0;
        private const int MaxBatonPassStacks = 3;

        public Combatant ActiveCombatant => turnQueue.Count > 0 ? turnQueue[0] : null;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // Register self with GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.Battle = this;
        }

        public void StartBattle(List<Combatant> party, List<Combatant> enemies)
        {
            activeParty = party;
            activeEnemies = enemies;
            CurrentState = BattleState.Start;
            CalculateTurnOrder();
            CurrentState = BattleState.PlayerTurn; // Ideally we check whose turn it actually is
            StartCoroutine(AdvanceTurnRoutine());
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
            CurrentState = BattleState.Resolving;

            StartCoroutine(ResolveActionRoutine(action));
        }

        private IEnumerator ResolveActionRoutine(BattleAction action)
        {
            bool wasWeaknessHit = action.Execute(this);
            
            // Wait for DOTween animations or other visual effects here
            yield return new WaitForSeconds(1.0f);

            if (wasWeaknessHit)
            {
                action.Source.GrantOneMore(); 
            }

            if (AllEnemiesDown())
            {
                TriggerAllOutAttackPrompt();
            }
            else
            {
                StartCoroutine(AdvanceTurnRoutine());
            }
        }

        public void PerformBatonPass(Combatant from, Combatant to)
        {
            batonPassChainCount = Mathf.Min(batonPassChainCount + 1, MaxBatonPassStacks);
            to.ApplyBatonPassBuff(batonPassChainCount); 
            to.GrantOneMore();
        }

        public void TriggerAllOutAttackPrompt()
        {
            // UI prompts the player; on confirm, calls ResolveAllOutAttack()
            // For now, auto-resolve
            ResolveAllOutAttack();
        }

        public void ResolveAllOutAttack()
        {
            float allOutDamage = CalculateAllOutDamage();
            foreach (var enemy in activeEnemies)
                enemy.TakeDamage(allOutDamage, Element.Almighty);

            CheckBattleEndConditions();
        }

        public bool AllEnemiesDown() => activeEnemies.TrueForAll(e => e.IsDown);
        
        public List<Combatant> GetActiveEnemies() => activeEnemies;

        private void CheckBattleEndConditions()
        {
            if (activeEnemies.TrueForAll(e => e.IsDefeated)) CurrentState = BattleState.Victory;
            else if (activeParty.TrueForAll(p => p.IsDefeated)) CurrentState = BattleState.Defeat;
        }

        public void EnterNegotiation(Combatant remainingShadow)
        {
            CurrentState = BattleState.Negotiation;
            // Hands off to NegotiationResolver
        }

        public void GrantPassiveAbility(string abilityId)
        {
            // registers the ability with the protagonist's passive ability set
        }

        public void EndNegotiationPhase(bool success)
        {
            if (success) CheckBattleEndConditions();
            else StartCoroutine(AdvanceTurnRoutine());
        }

        public void PauseQueue() { isQueuePaused = true; }
        public void ResumeQueue() { isQueuePaused = false; }

        private IEnumerator AdvanceTurnRoutine() 
        { 
            CheckBattleEndConditions();
            if (CurrentState == BattleState.Victory || CurrentState == BattleState.Defeat) yield break;

            if (turnQueue.Count > 0)
            {
                var current = turnQueue[0];
                turnQueue.RemoveAt(0);
                
                // If the combatant is defeated, skip their turn
                if (current.IsDefeated)
                {
                    StartCoroutine(AdvanceTurnRoutine());
                    yield break;
                }

                turnQueue.Add(current);
                OnTurnOrderChanged?.Invoke();
                OnTurnStarted?.Invoke(current);

                while (isQueuePaused) yield return null;

                CurrentState = current.IsPlayerSide ? BattleState.PlayerTurn : BattleState.EnemyTurn;
                
                if (CurrentState == BattleState.EnemyTurn)
                {
                    // Basic enemy AI here
                    SubmitAction(new AttackAction { Source = current, Targets = new List<Combatant> { activeParty[0] } });
                }
            }
        }

        private float CalculateAllOutDamage() { return 100f; } // Placeholder
        
        public List<Combatant> GetTurnQueueSnapshot() => new List<Combatant>(turnQueue);
    }
}
