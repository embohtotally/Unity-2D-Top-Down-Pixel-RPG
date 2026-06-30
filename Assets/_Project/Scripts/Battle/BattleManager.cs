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

            bool hasGameManager = GameManager.Instance != null;
            Debug.Log($"[BattleManager] Start: autoStartBattle={autoStartBattle}, hasGameManager={hasGameManager}, heroesContainer assigned={(heroesContainer != null)}, enemiesContainer assigned={(enemiesContainer != null)}");

            // Auto-start if autoStartBattle is true OR if GameManager is active (transitioned from Overworld)
            if (autoStartBattle || hasGameManager)
            {
                StartCoroutine(DelayedBattleSetup());
            }
        }

        private IEnumerator DelayedBattleSetup()
        {
            // Wait 1 frame so ALL Awake() and Start() methods across every scene
            // (including DontDestroyOnLoad objects) have fully completed!
            yield return null;

            if (heroesContainer == null)
            {
                var found = GameObject.Find("Heroes Container") ?? GameObject.Find("HeroesContainer") ?? GameObject.Find("Heroes");
                if (found != null) heroesContainer = found.transform;
                else heroesContainer = new GameObject("Heroes Container").transform;
                Debug.Log($"[BattleManager] heroesContainer was not assigned in Inspector. Auto-detected/created: {heroesContainer.name}");
            }

            if (enemiesContainer == null)
            {
                var found = GameObject.Find("Enemies Container") ?? GameObject.Find("EnemiesContainer") ?? GameObject.Find("Enemies");
                if (found != null) enemiesContainer = found.transform;
                else enemiesContainer = new GameObject("Enemies Container").transform;
                Debug.Log($"[BattleManager] enemiesContainer was not assigned in Inspector. Auto-detected/created: {enemiesContainer.name}");
            }

            // ═══════════════════════════════════════════════════════
            // STEP 1: Find ALL HeroCombatants using every method
            // ═══════════════════════════════════════════════════════
            var persistedHeroes = new List<HeroCombatant>();

            // Safety net: Force-scan for heroes one more time in the battle scene
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EnsureHeroesRegistered();
            }

            // Method A: Check GameManager.ActivePartyRoster
            if (GameManager.Instance != null && GameManager.Instance.ActivePartyRoster != null)
            {
                // Clean out destroyed references
                GameManager.Instance.ActivePartyRoster.RemoveAll(h => h == null);
                
                foreach (var obj in GameManager.Instance.ActivePartyRoster)
                {
                    var hero = obj as HeroCombatant;
                    if (hero != null && !persistedHeroes.Contains(hero))
                        persistedHeroes.Add(hero);
                }
                Debug.Log($"[BattleManager] ActivePartyRoster check: found {persistedHeroes.Count} heroes.");
            }

            // Method B: FindObjectsOfType (catches DontDestroyOnLoad heroes)
            if (persistedHeroes.Count == 0)
            {
                var allHeroes = FindObjectsOfType<HeroCombatant>();
                foreach (var h in allHeroes)
                {
                    // DontDestroyOnLoad heroes are root objects (parent == null)
                    // Also accept any hero NOT inside heroesContainer (those are placeholders)
                    bool isPlaceholder = h.transform.IsChildOf(heroesContainer);
                    if (!isPlaceholder && !persistedHeroes.Contains(h))
                        persistedHeroes.Add(h);
                }
                Debug.Log($"[BattleManager] FindObjectsOfType fallback: found {persistedHeroes.Count} heroes.");
            }

            // ═══════════════════════════════════════════════════════
            // STEP 2: Build party list
            // ═══════════════════════════════════════════════════════
            var party = new List<Combatant>();

            if (persistedHeroes.Count > 0)
            {
                Debug.Log($"[BattleManager] Binding {persistedHeroes.Count} HeroCombatants to battle! Snapping to Hero_Slot_X positions.");

                // Turn off pre-placed placeholder heroes in heroesContainer
                foreach (Transform child in heroesContainer)
                {
                    var placeholder = child.GetComponent<Combatant>();
                    if (placeholder != null) placeholder.gameObject.SetActive(false);
                }

                // Ensure Protagonist is always first so they get Hero_Slot_1!
                persistedHeroes.Sort((a, b) => b.IsProtagonist.CompareTo(a.IsProtagonist));

                int heroSlotIndex = 1;
                foreach (var hero in persistedHeroes)
                {
                    hero.gameObject.SetActive(true);
                    party.Add(hero);
                    Transform slotTarget = heroesContainer.Find($"Hero_Slot_{heroSlotIndex}");
                    if (slotTarget != null)
                    {
                        hero.transform.position = slotTarget.position;
                        Debug.Log($"[BattleManager] Placed {hero.gameObject.name} at {slotTarget.name} ({slotTarget.position})");
                    }
                    else
                    {
                        hero.transform.position = heroesContainer.position;
                        Debug.LogWarning($"[BattleManager] Could not find Hero_Slot_{heroSlotIndex}! Defaulting to container position.");
                    }
                    heroSlotIndex++;
                }

                // Late-register heroes with GameManager if they weren't registered yet
                if (GameManager.Instance != null)
                {
                    foreach (var hero in persistedHeroes)
                        GameManager.Instance.RegisterHero(hero);
                }
            }
            else
            {
                party = new List<Combatant>(heroesContainer.GetComponentsInChildren<Combatant>(true));
                Debug.Log($"[BattleManager] DontDestroyOnLoad heroes not detected. Using {party.Count} pre-placed heroes from heroesContainer. Restoring PlayerPrefs stats!");

                int savedCount = PlayerPrefs.GetInt("Hero_Count", 0);
                for (int i = 0; i < party.Count; i++)
                {
                    if (i < savedCount)
                    {
                        int curHP = PlayerPrefs.GetInt($"Hero_{i}_CurrentHP", party[i].CurrentHP);
                        int maxHP = PlayerPrefs.GetInt($"Hero_{i}_MaxHP", party[i].MaxHP);
                        int curSP = PlayerPrefs.GetInt($"Hero_{i}_CurrentSP", party[i].CurrentSP);
                        int maxSP = PlayerPrefs.GetInt($"Hero_{i}_MaxSP", party[i].MaxSP);
                        party[i].OverrideStatsFromSave(curHP, maxHP, curSP, maxSP);
                    }
                }
            }

            // ═══════════════════════════════════════════════════════
            // STEP 3: Handle pending enemy prefabs from Fungus
            // ═══════════════════════════════════════════════════════
            if (GameManager.Instance != null && GameManager.Instance.PendingEnemyPrefabs != null && GameManager.Instance.PendingEnemyPrefabs.Count > 0)
            {
                Debug.Log($"[BattleManager] Spawning {GameManager.Instance.PendingEnemyPrefabs.Count} pending enemy prefabs from Fungus!");
                foreach (Transform child in enemiesContainer)
                {
                    child.gameObject.SetActive(false);
                }

                int enemyCount = GameManager.Instance.PendingEnemyPrefabs.Count;
                float spacing = 2.0f; // Horizontal distance between enemies
                float startX = -(enemyCount - 1) * spacing / 2f; // Center them around 0 offset

                for (int i = 0; i < enemyCount; i++)
                {
                    GameObject prefab = GameManager.Instance.PendingEnemyPrefabs[i];
                    if (prefab != null)
                    {
                        GameObject spawnedEnemy = Instantiate(prefab, enemiesContainer);
                        
                        // Give them unique names if they are identical (e.g. "Slime A", "Slime B")
                        char suffix = (char)('A' + i);
                        spawnedEnemy.name = enemyCount > 1 ? $"{prefab.name} {suffix}" : prefab.name;

                        // Space them out horizontally so they don't overlap
                        spawnedEnemy.transform.localPosition = new Vector3(startX + (i * spacing), 0, 0);
                    }
                }
                GameManager.Instance.PendingEnemyPrefabs.Clear();
            }

            var enemies = new List<Combatant>(enemiesContainer.GetComponentsInChildren<Combatant>(true));

            // Snap enemies to Enemy_Slot_X positions
            int enemySlotIndex = 1;
            foreach (var enemy in enemies)
            {
                if (!enemy.gameObject.activeInHierarchy && enemies.Count > 1)
                    continue;

                Transform enemySlotTarget = enemiesContainer.Find($"Enemy_Slot_{enemySlotIndex}");
                if (enemySlotTarget == null) enemySlotTarget = heroesContainer.Find($"Enemy_Slot_{enemySlotIndex}");

                if (enemySlotTarget != null)
                {
                    enemy.transform.position = enemySlotTarget.position;
                    Debug.Log($"[BattleManager] Placed enemy {enemy.gameObject.name} at {enemySlotTarget.name} ({enemySlotTarget.position})");
                }
                else
                {
                    Debug.Log($"[BattleManager] Enemy_Slot_{enemySlotIndex} not found; leaving {enemy.gameObject.name} at its initial position.");
                }
                enemySlotIndex++;
            }

            // Filter out inactive placeholders
            enemies.RemoveAll(e => !e.gameObject.activeInHierarchy);

            Debug.Log($"[BattleManager] === BATTLE SETUP COMPLETE === Party: {party.Count} heroes | Enemies: {enemies.Count}");

            if (party.Count > 0 && enemies.Count > 0)
            {
                StartBattle(party, enemies);
            }
            else
            {
                Debug.LogError($"[BattleManager] Cannot start battle! Party={party.Count}, Enemies={enemies.Count}. Check that your overworld HeroCombatants have 'Persist Across Scenes' enabled and that your enemy prefab is assigned.");
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
                if (CurrentState == BattleState.Victory || CurrentState == BattleState.Defeat)
                {
                    if (CurrentState == BattleState.Victory && GameManager.Instance != null)
                    {
                        GameManager.Instance.LastBattleWon = true;
                        yield return new WaitForSeconds(2.0f); // Allow 2 seconds for victory celebration/fist pump!
                        GameManager.Instance.ReturnToPreviousScene();
                    }
                    yield break;
                }

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
