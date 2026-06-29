using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

            Debug.Log($"[BattleManager] Start: autoStartBattle={autoStartBattle}, heroesContainer assigned={(heroesContainer != null)}, enemiesContainer assigned={(enemiesContainer != null)}");

            // Auto-start for testing purposes
            if (autoStartBattle)
            {
                if (heroesContainer != null && enemiesContainer != null)
                {
                    var party = new List<Combatant>(heroesContainer.GetComponentsInChildren<Combatant>(true));
                    var enemies = new List<Combatant>(enemiesContainer.GetComponentsInChildren<Combatant>(true));
                    
                    Debug.Log($"[BattleManager] Auto-Start: Found {party.Count} heroes in {heroesContainer.name}. Found {enemies.Count} enemies in {enemiesContainer.name}.");
                    
                    foreach (var enemy in enemies)
                    {
                        Debug.Log($"[BattleManager] Enemy detected in container: {enemy.gameObject.name} (Active in hierarchy: {enemy.gameObject.activeInHierarchy})");
                    }

                    StartBattle(party, enemies);
                }
                else
                {
                    Debug.LogWarning("[BattleManager] Auto-Start is enabled, but heroesContainer or enemiesContainer is not assigned in the Inspector!");
                }
            }
        }

        public void StartBattle(List<Combatant> party, List<Combatant> enemies)
        {
            activeParty = party;
            activeEnemies = enemies;

            // Ensure all combatants are active in the hierarchy and explicitly initialize their stats
            foreach (var p in activeParty)
            {
                if (p != null)
                {
                    p.gameObject.SetActive(true);
                    p.InitializeStats();
                }
            }
            foreach (var e in activeEnemies)
            {
                if (e != null)
                {
                    e.gameObject.SetActive(true);
                    e.InitializeStats();
                }
            }

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

                Debug.Log($"[BattleManager] Turn started for: {current.gameObject.name} (IsPlayerSide: {current.IsPlayerSide})");
                current.OnTurnStartCleanUp();
                OnTurnStarted?.Invoke(current);

                if (isQueuePaused)
                {
                    Debug.LogWarning($"[BattleManager] Queue is currently PAUSED (likely by BattleTutorialManager). Waiting for ResumeQueue() before {current.gameObject.name} can act...");
                }
                while (isQueuePaused) yield return null;

                CurrentState = current.IsPlayerSide ? BattleState.PlayerTurn : BattleState.EnemyTurn;

                if (CurrentState == BattleState.EnemyTurn)
                {
                    Debug.Log($"[BattleManager] Executing Enemy AI for {current.gameObject.name}...");
                    ExecuteEnemyAI(current);
                }

                // Wait for an action to be submitted (by UI or AI)
                while (pendingAction == null) yield return null;

                var action = pendingAction;
                pendingAction = null;

                CurrentState = BattleState.Resolving;

                bool isBatonPass = action is BatonPassAction;
                
                // Clear OneMore flag now that the player has chosen an action (Baton Pass or regular action)
                if (current.HasOneMore)
                {
                    current.ClearOneMore();
                }
                
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
                    turnQueue.Remove(current);
                    if (!current.IsDefeated) turnQueue.Add(current); // back of queue

                    // The target was already placed at front by PerformBatonPass
                    // The target has HasOneMore, so they will act next.
                    // Chain count stays incremented; reset it if an enemy acts.
                }
                else if (current.HasOneMore)
                {
                    // Combatant gets to go again immediately! Do NOT clear HasOneMore yet, so UICombatPanel knows Baton Pass is available!
                    // Remains at the front of the queue, do not move to back
                }
                else
                {
                    // Normal turn ends, move to back
                    current.OnTurnEndCleanUp();
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
            var availableSkills = enemy.GetAvailableSkills();
            var profile = enemy.EnemyAIType;

            Debug.Log($"[BattleManager] Executing Enemy AI for '{enemy.gameObject.name}'. Profile: {profile}, HP: {enemy.HpPercent:P0}");

            // --- Pre-Action: Healing & Buffs ---
            var healingSkills = availableSkills.Where(s => s.category == SkillCategory.Healing && enemy.CurrentSP >= s.spCost);
            var supportSkills = availableSkills.Where(s => s.category == SkillCategory.Support && enemy.CurrentSP >= s.spCost);

            // Self heal if critical
            if (enemy.HpPercent < 0.3f && healingSkills.Any())
            {
                Debug.Log($"[BattleManager] Enemy AI ({profile}): {enemy.gameObject.name} is at critical HP (<30%) and chose Self-Heal!");
                SubmitAction(new SkillAction { Source = enemy, Targets = new List<Combatant> { enemy }, skill = healingSkills.First() });
                return;
            }

            // Heal an ally (other enemy combatant) if they are below 50%
            if (activeEnemies.Count > 1)
            {
                var woundedAlly = activeEnemies
                    .Where(e => e != enemy && !e.IsDefeated && e.HpPercent < 0.5f)
                    .OrderBy(e => e.HpPercent)
                    .FirstOrDefault();
                if (woundedAlly != null && healingSkills.Any())
                {
                    Debug.Log($"[BattleManager] Enemy AI ({profile}): {enemy.gameObject.name} chose to Heal wounded ally {woundedAlly.gameObject.name}");
                    SubmitAction(new SkillAction { Source = enemy, Targets = new List<Combatant> { woundedAlly }, skill = healingSkills.First() });
                    return;
                }
            }

            // Use a support buff if nothing urgent and we have one
            if (supportSkills.Any() && (!healingSkills.Any() || enemy.HpPercent > 0.5f))
            {
                // Target self or random ally; for simplicity, buff self
                Debug.Log($"[BattleManager] Enemy AI ({profile}): {enemy.gameObject.name} chose Support Buff!");
                SubmitAction(new SkillAction { Source = enemy, Targets = new List<Combatant> { enemy }, skill = supportSkills.First() });
                return;
            }

            // --- Find lowest HP target for fallback and profile checks ---
            Combatant lowestHpTarget = null;
            foreach (var p in activeParty)
            {
                if (p.IsDefeated) continue;
                if (lowestHpTarget == null || p.HpPercent < lowestHpTarget.HpPercent)
                    lowestHpTarget = p;
            }

            // --- Profile-based modifications ---
            switch (profile)
            {
                case EnemyAIType.Aggressive:
                    // Default behaviour – no change to the current two-step priority.
                    break;
                case EnemyAIType.Tactical:
                    // Prefer medium-cost skills; if HP < 40%, consider guarding
                    if (enemy.HpPercent < 0.4f)
                    {
                        Debug.Log($"[BattleManager] Enemy AI ({profile}): {enemy.gameObject.name} is low on HP (<40%) and chose to Guard!");
                        SubmitAction(new GuardAction { Source = enemy });
                        return;
                    }
                    break;
                case EnemyAIType.Desperate:
                    // If HP < 25%, ignore SP limits and use strongest available move
                    if (enemy.HpPercent < 0.25f && availableSkills.Any() && lowestHpTarget != null)
                    {
                        var bestSkillDesperate = availableSkills.OrderByDescending(s => s.basePower).First();
                        if (enemy.CurrentSP >= bestSkillDesperate.spCost)
                        {
                            Debug.Log($"[BattleManager] Enemy AI ({profile}): {enemy.gameObject.name} is desperate (<25%) and chose strongest move '{bestSkillDesperate.displayName}'!");
                            SubmitAction(new SkillAction { Source = enemy, Targets = new List<Combatant> { lowestHpTarget }, skill = bestSkillDesperate });
                            return;
                        }
                    }
                    break;
            }

            // --- Existing Priority 1: Find a skill that hits a weakness (Skipping Downed Targets) ---
            SkillData bestSkill = null;
            Combatant bestTarget = null;

            foreach (var skill in availableSkills)
            {
                if (enemy.CurrentSP < skill.spCost) continue;

                foreach (var partyMember in activeParty)
                {
                    // Prevent Wasting 'One More' on Already-Downed Targets!
                    if (partyMember.IsDefeated || partyMember.IsDown) continue;

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

            if (bestSkill != null && bestTarget != null)
            {
                Debug.Log($"[BattleManager] Enemy AI ({profile}): {enemy.gameObject.name} chose Skill '{bestSkill.displayName}' against weakness on {bestTarget.gameObject.name}");
                SubmitAction(new SkillAction 
                { 
                    Source = enemy, 
                    Targets = new List<Combatant> { bestTarget },
                    skill = bestSkill
                });
                return;
            }

            // --- Existing Fallback: Attack lowest HP target ---
            if (lowestHpTarget != null)
            {
                Debug.Log($"[BattleManager] Enemy AI ({profile}): {enemy.gameObject.name} chose Attack against {lowestHpTarget.gameObject.name}");
                SubmitAction(new AttackAction 
                { 
                    Source = enemy, 
                    Targets = new List<Combatant> { lowestHpTarget } 
                });
            }
            else
            {
                Debug.LogError($"[BattleManager] Enemy AI: {enemy.gameObject.name} could not find any valid active party targets to attack! (activeParty.Count={activeParty.Count})");
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
        public List<Combatant> GetActiveParty() => activeParty;

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

        public void PauseQueue() { Debug.Log("[BattleManager] PauseQueue() called. Battle queue paused."); isQueuePaused = true; }
        public void ResumeQueue() { Debug.Log("[BattleManager] ResumeQueue() called. Battle queue resumed."); isQueuePaused = false; }

        private float CalculateAllOutDamage() { return 100f; } // Placeholder
        
        public List<Combatant> GetTurnQueueSnapshot() => new List<Combatant>(turnQueue);
    }
}
