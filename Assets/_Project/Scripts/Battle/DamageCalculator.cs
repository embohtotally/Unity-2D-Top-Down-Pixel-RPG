using UnityEngine;
using PixelMindscape.Data;

namespace PixelMindscape.Battle
{
    public static class DamageCalculator
    {
        public struct DamageResult
        {
            public float finalDamage;
            public Affinity affinity;
        }

        public static DamageResult CalculateDamage(Combatant attacker, Combatant defender, SkillData skill)
        {
            Element element = skill != null ? skill.element : Element.Physical;
            int basePower = skill != null ? skill.basePower : attacker.BaseAttackPower;

            Affinity affinity = defender.GetAffinity(element);

            float multiplier = affinity switch
            {
                Affinity.Weak => 1.5f,
                Affinity.Resist => 0.5f,
                Affinity.Null => 0f,
                Affinity.Absorb => -1f,    
                Affinity.Repel => 0f,      
                _ => 1f
            };

            float rawDamage = basePower * attacker.GetAttackStatFor(element) * multiplier;
            float mitigated = rawDamage - defender.GetDefenseStatFor(element);

            return new DamageResult
            {
                finalDamage = Mathf.Max(mitigated, affinity == Affinity.Absorb ? rawDamage : 1f),
                affinity = affinity
            };
        }
    }
}
