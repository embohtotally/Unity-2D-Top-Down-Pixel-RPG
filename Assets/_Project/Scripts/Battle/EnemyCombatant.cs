using UnityEngine;
using PixelMindscape.Data;

namespace PixelMindscape.Battle
{
    public class EnemyCombatant : Combatant
    {
        [Header("Enemy Initial Stats")]
        [SerializeField] private int startingHP = 50;
        [SerializeField] private int startingSP = 20;
        [SerializeField] private int baseAttack = 10;
        [SerializeField] private int agility = 8;

        [Header("Affinities")]
        [SerializeField] private Element strongAgainst;
        [SerializeField] private Element weakAgainst;

        private void Awake()
        {
            IsPlayerSide = false;
            MaxHP = startingHP;
            CurrentHP = startingHP;
            MaxSP = startingSP;
            CurrentSP = startingSP;
            BaseAttackPower = baseAttack;
            EffectiveAgility = agility;
        }

        public override Affinity GetAffinity(Element element)
        {
            if (element == weakAgainst) return Affinity.Weak;
            if (element == strongAgainst) return Affinity.Resist;
            return Affinity.Normal;
        }

        public override int GetAttackStatFor(Element element)
        {
            return BaseAttackPower; 
        }

        public override int GetDefenseStatFor(Element element)
        {
            return 5; // Placeholder defense
        }
    }
}
