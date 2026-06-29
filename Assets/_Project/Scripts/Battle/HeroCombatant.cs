using UnityEngine;
using PixelMindscape.Data;

namespace PixelMindscape.Battle
{
    public class HeroCombatant : Combatant
    {
        [Header("Character Data (Optional)")]
        [SerializeField] private CharacterData characterData;

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

        private void Awake()
        {
            InitializeStats();
        }

        public override void InitializeStats()
        {
            IsPlayerSide = true;
            IsDefeated = false;
            IsDown = false;

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
                SwitchPersona(personaLoadout[0]);
            }
            
            if (MaxHP <= 0)
                Debug.LogError($"[HeroCombatant] {gameObject.name} has MaxHP={MaxHP}! Set it to a value > 0.");
        }

        public void SwitchPersona(PersonaData newPersona)
        {
            if (newPersona == null || ActivePersonaData == newPersona) return;
            ActivePersonaData = newPersona;
            HasSwitchedPersonaThisTurn = true;
            
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
            
            Debug.Log($"[HeroCombatant] SwitchPersona: {gameObject.name} instantly switched active Persona to {newPersona.displayName}! Loaded {availableSkills.Count} skills.");
            
            if (vfxHandler != null) vfxHandler.PlayBatonPassVFX(); // use beautiful vfx flash
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
