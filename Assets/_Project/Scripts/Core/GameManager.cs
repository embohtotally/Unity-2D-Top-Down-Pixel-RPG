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

        public string PreviousSceneName { get; private set; }
        public bool LastBattleWon { get; set; }
        public GameObject PendingEnemyPrefab { get; set; }

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
                PlayerPrefs.SetString($"Hero_{i}_Name", hero.gameObject.name);
            }
            PlayerPrefs.Save();
            Debug.Log($"[GameManager] SavePartyToPlayerPrefs: Persistently saved {ActivePartyRoster.Count} heroes to PlayerPrefs.");
        }

        public void LoadScene(string sceneName, System.Action onLoaded = null)
        {
            PreviousSceneName = SceneManager.GetActiveScene().name;

            // CRITICAL: Capture all heroes BEFORE the scene unloads!
            EnsureHeroesRegistered();
            SavePartyToPlayerPrefs();

            Debug.Log($"[GameManager] LoadScene: Transitioning from '{PreviousSceneName}' to '{sceneName}'. Carrying {ActivePartyRoster.Count} heroes.");
            StartCoroutine(LoadSceneRoutine(sceneName, onLoaded));
        }

        public void ReturnToPreviousScene()
        {
            if (!string.IsNullOrEmpty(PreviousSceneName))
            {
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
