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

        public void LoadScene(string sceneName, System.Action onLoaded = null)
        {
            StartCoroutine(LoadSceneRoutine(sceneName, onLoaded));
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
