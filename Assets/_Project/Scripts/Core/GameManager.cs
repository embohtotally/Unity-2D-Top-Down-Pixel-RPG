using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using PixelMindscape.Data;

namespace PixelMindscape.Core
{
    // Interfaces to resolve circular dependencies between assemblies
    public interface IBattleManager { void GrantPassiveAbility(string abilityId); }
    public interface IDialogueManager { }
    public interface IInventoryManager { }
    public interface ICalendarManager { }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public SaveData CurrentSave { get; private set; }

        // Using interfaces instead of direct types to avoid circular references with Core
        public IBattleManager Battle { get; set; }
        public IDialogueManager Dialogue { get; set; }
        public IInventoryManager Inventory { get; set; }
        public ICalendarManager Calendar { get; set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            CurrentSave = new SaveData(); // Initialize with empty save data to prevent nullrefs before loading
        }

        public void NewGame(SaveData seedData) => CurrentSave = seedData;

        public void SaveGame(int slot)
        {
            string json = JsonUtility.ToJson(CurrentSave);
            File.WriteAllText(GetSlotPath(slot), json);
        }

        public void LoadGame(int slot)
        {
            string path = GetSlotPath(slot);
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                CurrentSave = JsonUtility.FromJson<SaveData>(json);
            }
        }

        [Header("Scene Transition State")]
        public Vector3 LastOverworldPosition { get; set; }
        public bool HasSavedOverworldPosition { get; set; }

        [field: SerializeField] public string PreviousSceneName { get; private set; }
        [field: SerializeField] public bool LastBattleWon { get; set; }
        [field: SerializeField] public System.Collections.Generic.List<GameObject> PendingEnemyPrefabs { get; set; } = new System.Collections.Generic.List<GameObject>();

        [Header("Party Management")]
        [Tooltip("Add prefabs for allies who don't exist in the overworld. They will be dynamically spawned into battle!")]
        [SerializeField] private List<GameObject> unlockedPartyPrefabs = new List<GameObject>();
        public List<GameObject> UnlockedPartyPrefabs => unlockedPartyPrefabs;

        public List<MonoBehaviour> ActivePartyRoster { get; private set; } = new List<MonoBehaviour>();

        public void RegisterHero(MonoBehaviour hero)
        {
            if (hero != null && !ActivePartyRoster.Contains(hero))
            {
                ActivePartyRoster.Add(hero);
                Debug.Log($"[GameManager] Registered hero '{hero.gameObject.name}' to ActivePartyRoster. Total party size: {ActivePartyRoster.Count}");
            }
        }

        /// <summary>
        /// Scans the entire scene for all HeroCombatant MonoBehaviours and ensures
        /// they are registered in ActivePartyRoster. Called right before scene transitions.
        /// Uses type name string comparison to avoid Assembly Definition cross-reference issues.
        /// </summary>
        public void EnsureHeroesRegistered()
        {
            // Clean out any destroyed references first
            ActivePartyRoster.RemoveAll(h => h == null);

            var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
            foreach (var mb in allMonoBehaviours)
            {
                if (mb.GetType().Name == "HeroCombatant")
                {
                    if (mb.transform.parent == null) DontDestroyOnLoad(mb.gameObject);
                    RegisterHero(mb);
                }
            }
            Debug.Log($"[GameManager] EnsureHeroesRegistered complete. ActivePartyRoster size: {ActivePartyRoster.Count}");
        }

        public void CleanUpInvisibleAllies()
        {
            foreach (var hero in ActivePartyRoster.ToArray())
            {
                if (hero != null && hero.gameObject.name.Contains("(InvisibleAlly)"))
                {
                    ActivePartyRoster.Remove(hero);
                    Destroy(hero.gameObject);
                }
            }
            Debug.Log("[GameManager] Cleaned up invisible allies upon returning to overworld.");
        }

        public void SavePartyToPlayerPrefs()
        {
            PlayerPrefs.SetInt("Hero_Count", ActivePartyRoster.Count);
            for (int i = 0; i < ActivePartyRoster.Count; i++)
            {
                var hero = ActivePartyRoster[i];
                if (hero == null) continue;

                var type = hero.GetType();
                int curHP = (int)(type.GetProperty("CurrentHP")?.GetValue(hero) ?? 100);
                int maxHP = (int)(type.GetProperty("MaxHP")?.GetValue(hero) ?? 100);
                int curSP = (int)(type.GetProperty("CurrentSP")?.GetValue(hero) ?? 50);
                int maxSP = (int)(type.GetProperty("MaxSP")?.GetValue(hero) ?? 50);

                PlayerPrefs.SetInt($"Hero_{i}_CurrentHP", curHP);
                PlayerPrefs.SetInt($"Hero_{i}_MaxHP", maxHP);
                PlayerPrefs.SetInt($"Hero_{i}_CurrentSP", curSP);
                PlayerPrefs.SetInt($"Hero_{i}_MaxSP", maxSP);
                
                // Remove the "(InvisibleAlly)" tag when saving the name
                string rawName = hero.gameObject.name.Replace("(InvisibleAlly)", "");
                PlayerPrefs.SetString($"Hero_{i}_Name", rawName);
            }
            PlayerPrefs.Save();
            Debug.Log($"[GameManager] SavePartyToPlayerPrefs: Persistently saved {ActivePartyRoster.Count} heroes to PlayerPrefs.");
        }

        public void LoadScene(string sceneName, System.Action onLoaded = null)
        {
            PreviousSceneName = SceneManager.GetActiveScene().name;

            // CRITICAL: Capture all heroes BEFORE the scene unloads!
            EnsureHeroesRegistered();

            // Spawn invisible allies ONLY when going TO battle!
            if (sceneName.Contains("Battle"))
            {
                foreach (var prefab in unlockedPartyPrefabs)
                {
                    if (prefab == null) continue;

                    // SAFETY: Check if this character already exists in ActivePartyRoster
                    // (e.g. the protagonist is already registered from the overworld).
                    // HeroCombatant.Awake() has a static duplicate guard that will Destroy()
                    // any second instance with the same characterId, which would silently
                    // lose the registration. So we skip spawning entirely.
                    bool alreadyInParty = false;
                    // Get the characterId from the prefab's HeroCombatant
                    string prefabCharId = null;
                    var prefabMbs = prefab.GetComponents<MonoBehaviour>();
                    foreach (var pmb in prefabMbs)
                    {
                        if (pmb != null && pmb.GetType().Name == "HeroCombatant")
                        {
                            var charDataField = pmb.GetType().GetField("characterData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (charDataField != null)
                            {
                                var charData = charDataField.GetValue(pmb);
                                if (charData != null)
                                {
                                    var idProp = charData.GetType().GetField("characterId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                    if (idProp != null) prefabCharId = idProp.GetValue(charData) as string;
                                }
                            }
                            // Fallback: use prefab name
                            if (string.IsNullOrEmpty(prefabCharId)) prefabCharId = prefab.name;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(prefabCharId))
                    {
                        foreach (var existing in ActivePartyRoster)
                        {
                            if (existing == null) continue;
                            string existingId = null;
                            var eType = existing.GetType();
                            if (eType.Name == "HeroCombatant")
                            {
                                var eCharDataField = eType.GetField("characterData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (eCharDataField != null)
                                {
                                    var eCharData = eCharDataField.GetValue(existing);
                                    if (eCharData != null)
                                    {
                                        var eIdProp = eCharData.GetType().GetField("characterId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                        if (eIdProp != null) existingId = eIdProp.GetValue(eCharData) as string;
                                    }
                                }
                                if (string.IsNullOrEmpty(existingId)) existingId = existing.gameObject.name;
                            }
                            if (prefabCharId == existingId)
                            {
                                alreadyInParty = true;
                                Debug.Log($"[GameManager] Skipping prefab '{prefab.name}' — character '{prefabCharId}' is already in ActivePartyRoster.");
                                break;
                            }
                        }
                    }

                    if (alreadyInParty) continue;

                    var instance = Instantiate(prefab);
                    instance.name = prefab.name + "(InvisibleAlly)";
                    DontDestroyOnLoad(instance);
                    
                    // We must load their stats immediately so they don't overwrite PlayerPrefs with defaults!
                    MonoBehaviour heroCombatant = null;
                    var allMbs = instance.GetComponents<MonoBehaviour>();
                    foreach (var mb in allMbs)
                    {
                        if (mb != null && mb.GetType().Name == "HeroCombatant")
                        {
                            heroCombatant = mb;
                            break;
                        }
                    }

                    if (heroCombatant != null)
                    {
                        var type = heroCombatant.GetType();
                        int i = ActivePartyRoster.Count;
                        
                        int curHP = PlayerPrefs.GetInt($"Hero_{i}_CurrentHP", -1);
                        if (curHP != -1)
                        {
                            int maxHP = PlayerPrefs.GetInt($"Hero_{i}_MaxHP", 100);
                            int curSP = PlayerPrefs.GetInt($"Hero_{i}_CurrentSP", 50);
                            int maxSP = PlayerPrefs.GetInt($"Hero_{i}_MaxSP", 50);
                            
                            var method = type.GetMethod("OverrideStatsFromSave");
                            if (method != null) method.Invoke(heroCombatant, new object[] { curHP, maxHP, curSP, maxSP });
                        }
                        RegisterHero(heroCombatant);
                    }
                    else
                    {
                        Debug.LogWarning($"[GameManager] Prefab {prefab.name} in UnlockedPartyPrefabs does not have a HeroCombatant script!");
                    }
                }
            }

            SavePartyToPlayerPrefs();

            Debug.Log($"[GameManager] LoadScene: Transitioning from '{PreviousSceneName}' to '{sceneName}'. Carrying {ActivePartyRoster.Count} heroes.");
            StartCoroutine(LoadSceneRoutine(sceneName, onLoaded));
        }

        public void ReturnToPreviousScene()
        {
            if (!string.IsNullOrEmpty(PreviousSceneName))
            {
                // CRITICAL: Capture battle stats and save them before destroying the allies!
                EnsureHeroesRegistered();
                SavePartyToPlayerPrefs();
                CleanUpInvisibleAllies();

                StartCoroutine(LoadSceneRoutine(PreviousSceneName, null));
            }
            else
            {
                Debug.LogWarning("GameManager: No previous scene to return to!");
            }
        }

        private IEnumerator LoadSceneRoutine(string sceneName, System.Action onLoaded)
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            yield return op;
            onLoaded?.Invoke();
        }

        private string GetSlotPath(int slot) => Path.Combine(Application.persistentDataPath, $"save_{slot}.json");

        // --- Template -> Runtime copy helpers ---

        public PartyMemberRuntimeState CreateRuntimeStateFromTemplate(CharacterData template)
        {
            return new PartyMemberRuntimeState
            {
                characterId = template.characterId,
                level = 1,
                currentHP = template.baseHP,
                currentSP = template.baseSP,
                currentExp = 0
            };
        }

        public PersonaRuntimeState CreateRuntimeStateFromTemplate(PersonaData template)
        {
            var learned = new List<string>();
            foreach (var entry in template.innateSkillsByLevel)
                if (entry.level <= template.baseLevel) learned.Add(entry.skill.skillId);

            return new PersonaRuntimeState
            {
                personaId = template.personaId,
                level = template.baseLevel,
                currentExp = 0,
                learnedSkillIds = learned
            };
        }
    }
}
