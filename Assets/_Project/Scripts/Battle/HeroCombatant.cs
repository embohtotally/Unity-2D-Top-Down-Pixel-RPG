using UnityEngine;
using PixelMindscape.Data;

namespace PixelMindscape.Battle
{
    public class HeroCombatant : Combatant
    {
        [Header("Hero Initial Stats")]
        [SerializeField] private int startingHP = 100;
        [SerializeField] private int startingSP = 50;
        [SerializeField] private int baseAttack = 15;
        [SerializeField] private int baseMagic = 15;
        [SerializeField] private int agility = 10;
        
        [Header("Affinities")]
        [SerializeField] private Element strongAgainst;
        [SerializeField] private Element weakAgainst;

        private void Awake()
        {
            InitializeStats();
        }

        public override void InitializeStats()
        {
            IsPlayerSide = true;
            MaxHP = startingHP;
            CurrentHP = startingHP;
            MaxSP = startingSP;
            CurrentSP = startingSP;
            BaseAttackPower = baseAttack;
            EffectiveAgility = agility;
            IsDefeated = false;
            IsDown = false;
            
            if (startingHP <= 0)
                Debug.LogError($"[HeroCombatant] {gameObject.name} has startingHP={startingHP} in the Inspector! Set it to a value > 0.");
            
            Debug.Log($"[HeroCombatant] InitializeStats: {gameObject.name} => HP={CurrentHP}/{MaxHP}, IsDefeated={IsDefeated}");
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
                return baseMagic;
            }
        }

        public override int GetDefenseStatFor(Element element)
        {
            return 10; // Placeholder defense
        }

        [Header("Hero Skills")]
        [SerializeField] private System.Collections.Generic.List<SkillData> availableSkills = new System.Collections.Generic.List<SkillData>();

        public override System.Collections.Generic.List<SkillData> GetAvailableSkills()
        {
            return availableSkills;
        }
    }
}
