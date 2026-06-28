using UnityEngine;
using PixelMindscape.Data;

namespace PixelMindscape.Battle
{
    public class EnemyCombatant : Combatant
    {
        [Header("Enemy ScriptableObject Data")]
        [SerializeField] private PersonaData enemyData;

        [Header("Enemy Initial Stats (Fallback if no PersonaData assigned)")]
        [SerializeField] private int startingHP = 50;
        [SerializeField] private int startingSP = 20;
        [SerializeField] private int baseAttack = 10;
        [SerializeField] private int agility = 8;

        [Header("Affinities (Fallback)")]
        [SerializeField] private Element strongAgainst;
        [SerializeField] private Element weakAgainst;

        private void Awake()
        {
            InitializeStats();
        }

        public override void InitializeStats()
        {
            IsPlayerSide = false;
            if (enemyData != null)
            {
                MaxHP = enemyData.baseHP > 0 ? enemyData.baseHP : startingHP;
                CurrentHP = MaxHP;
                MaxSP = enemyData.baseSP > 0 ? enemyData.baseSP : startingSP;
                CurrentSP = MaxSP;
                BaseAttackPower = enemyData.baseStrength > 0 ? enemyData.baseStrength : baseAttack;
                EffectiveAgility = enemyData.baseAgility > 0 ? enemyData.baseAgility : agility;
            }
            else
            {
                MaxHP = startingHP;
                CurrentHP = startingHP;
                MaxSP = startingSP;
                CurrentSP = startingSP;
                BaseAttackPower = baseAttack;
                EffectiveAgility = agility;
            }
            IsDefeated = false;
            IsDown = false;
            Debug.Log($"[EnemyCombatant] InitializeStats: {gameObject.name} (PersonaData: {(enemyData != null ? enemyData.displayName : "None")}) => HP={CurrentHP}/{MaxHP}, IsDefeated={IsDefeated}");
        }

        public override Affinity GetAffinity(Element element)
        {
            if (enemyData != null && enemyData.affinityTable != null)
            {
                foreach (var entry in enemyData.affinityTable)
                {
                    if (entry.element == element) return entry.affinity;
                }
            }
            if (element == weakAgainst) return Affinity.Weak;
            if (element == strongAgainst) return Affinity.Resist;
            return Affinity.Normal;
        }

        public override System.Collections.Generic.List<SkillData> GetAvailableSkills()
        {
            var list = new System.Collections.Generic.List<SkillData>();
            if (enemyData != null && enemyData.innateSkillsByLevel != null)
            {
                foreach (var entry in enemyData.innateSkillsByLevel)
                {
                    if (entry.skill != null) list.Add(entry.skill);
                }
            }
            return list;
        }

        public override int GetAttackStatFor(Element element)
        {
            if (element == Element.Physical)
            {
                return BaseAttackPower; 
            }
            else
            {
                return enemyData != null && enemyData.baseMagic > 0 ? enemyData.baseMagic : 10;
            }
        }

        public override int GetDefenseStatFor(Element element)
        {
            return enemyData != null && enemyData.baseEndurance > 0 ? enemyData.baseEndurance : 5;
        }
    }
}
