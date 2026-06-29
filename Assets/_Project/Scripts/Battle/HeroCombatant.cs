using UnityEngine;
using PixelMindscape.Data;

namespace PixelMindscape.Battle
{
    public class HeroCombatant : Combatant
    {
        [Header("Character Data (Optional)")]
        [SerializeField] private CharacterData characterData;
        public CharacterData CharacterData => characterData;

        [Header("Persona Loadout (For Protagonist)")]
        [SerializeField] private System.Collections.Generic.List<PersonaData> personaLoadout = new System.Collections.Generic.List<PersonaData>();

        [Header("Hero Initial Stats (Fallback)")]
        [SerializeField] private int startingHP = 100;
        [SerializeField] private int startingSP = 50;
        [SerializeField] private int baseAttack = 15;
        [SerializeField] private int baseMagic = 15;
        [SerializeField] private int agility = 10;
        
        [Header("Affinities")]
        [SerializeField] private Element strongAgainst;
        [SerializeField] private Element weakAgainst;

        public PersonaData ActivePersonaData { get; private set; }
        public System.Collections.Generic.List<PersonaData> GetPersonaLoadout() => personaLoadout;

        [Header("Persistence")]
        [Tooltip("If true, this HeroCombatant will persist across scene loads (DontDestroyOnLoad).")]
        [SerializeField] private bool persistAcrossScenes = true;
        private static System.Collections.Generic.Dictionary<string, HeroCombatant> persistentHeroes = new System.Collections.Generic.Dictionary<string, HeroCombatant>();
        private bool isInitialized = false;

        private void Awake()
        {
            if (persistAcrossScenes)
            {
                string uniqueKey = characterData != null ? characterData.characterId : gameObject.name;
                if (persistentHeroes.ContainsKey(uniqueKey) && persistentHeroes[uniqueKey] != this)
                {
                    // A persisted instance already exists! Destroy this duplicate overworld reload instance.
                    Destroy(gameObject);
                    return;
                }
                
                transform.SetParent(null); // DontDestroyOnLoad requires root object
                DontDestroyOnLoad(gameObject);
                persistentHeroes[uniqueKey] = this;
            }

            if (PixelMindscape.Core.GameManager.Instance != null)
            {
                PixelMindscape.Core.GameManager.Instance.RegisterHero(this);
            }
            else
            {
                Debug.LogWarning($"[HeroCombatant] Awake: GameManager.Instance is null! '{gameObject.name}' could not register. Will retry in Start().");
            }

            InitializeStats();
        }

        private void Start()
        {
            // RETRY registration! Start() is guaranteed to run AFTER all Awake() calls,
            // so GameManager.Instance is definitely available now.
            if (PixelMindscape.Core.GameManager.Instance != null)
            {
                PixelMindscape.Core.GameManager.Instance.RegisterHero(this);
            }

            base.Start(); // Call Combatant.Start() for animator/vfx handler setup
        }

        public override void InitializeStats()
        {
            if (isInitialized) return; // Preserve persisted HP, SP, and Persona stats across scene loads!
            isInitialized = true;

            IsPlayerSide = true;
            IsDefeated = false;
            IsDown = false;

            if (personaLoadout.Count > 0)
            {
                IsProtagonist = true;
            }

            if (characterData != null)
            {
                MaxHP = characterData.baseHP;
                CurrentHP = characterData.baseHP;
                MaxSP = characterData.baseSP;
                CurrentSP = characterData.baseSP;
                BaseAttackPower = characterData.baseStrength * 2; // derived attack
                EffectiveAgility = characterData.baseAgility;
                Debug.Log($"[HeroCombatant] InitializeStats: '{gameObject.name}' loaded stats from CharacterData '{characterData.displayName}' => HP={CurrentHP}/{MaxHP}, SP={CurrentSP}/{MaxSP}");
            }
            else
            {
                MaxHP = startingHP;
                CurrentHP = startingHP;
                MaxSP = startingSP;
                CurrentSP = startingSP;
                BaseAttackPower = baseAttack;
                EffectiveAgility = agility;
                Debug.Log($"[HeroCombatant] InitializeStats: '{gameObject.name}' has no CharacterData assigned. Using fallback test stats => HP={CurrentHP}/{MaxHP}, SP={CurrentSP}/{MaxSP}");
            }

            if (personaLoadout.Count > 0 && ActivePersonaData == null)
            {
                SwitchPersona(personaLoadout[0], true);
            }
            
            if (MaxHP <= 0)
                Debug.LogError($"[HeroCombatant] {gameObject.name} has MaxHP={MaxHP}! Set it to a value > 0.");
        }

        public void SwitchPersona(PersonaData newPersona, bool isInitialSetup = false)
        {
            if (newPersona == null || ActivePersonaData == newPersona) return;
            ActivePersonaData = newPersona;
            
            if (!isInitialSetup)
            {
                HasSwitchedPersonaThisTurn = true;
                if (BattleCinematicManager.Instance != null)
                {
                    BattleCinematicManager.Instance.PlayPersonaSummonFlash(this, newPersona);
                }
                else if (vfxHandler != null) 
                {
                    vfxHandler.PlayBatonPassVFX(); // use beautiful vfx flash fallback
                }
            }
            
            // Recalculate stats based on new Persona
            BaseAttackPower = newPersona.baseStrength * 2;
            EffectiveAgility = newPersona.baseAgility;

            availableSkills.Clear();
            if (newPersona.innateSkillsByLevel != null)
            {
                foreach (var entry in newPersona.innateSkillsByLevel)
                {
                    if (entry.skill != null) availableSkills.Add(entry.skill);
                }
            }
            
            Debug.Log($"[HeroCombatant] SwitchPersona: {gameObject.name} switched active Persona to {newPersona.displayName} (InitialSetup: {isInitialSetup}). Loaded {availableSkills.Count} skills.");
        }

        public override Affinity GetAffinity(Element element)
        {
            if (element == weakAgainst) return Affinity.Weak;
            if (element == strongAgainst) return Affinity.Resist;
            return Affinity.Normal;
        }

        public override int GetAttackStatFor(Element element)
        {
            if (element == Element.Physical)
            {
                return BaseAttackPower; 
            }
            else
            {
                if (ActivePersonaData != null) return ActivePersonaData.baseMagic * 2;
                return characterData != null ? characterData.baseMagic * 2 : baseMagic;
            }
        }

        public override int GetDefenseStatFor(Element element)
        {
            if (ActivePersonaData != null) return ActivePersonaData.baseEndurance * 2;
            return characterData != null ? characterData.baseEndurance * 2 : 10;
        }

        [Header("Hero Skills")]
        [SerializeField] private System.Collections.Generic.List<SkillData> availableSkills = new System.Collections.Generic.List<SkillData>();

        public override System.Collections.Generic.List<SkillData> GetAvailableSkills()
        {
            return availableSkills;
        }
    }
}
