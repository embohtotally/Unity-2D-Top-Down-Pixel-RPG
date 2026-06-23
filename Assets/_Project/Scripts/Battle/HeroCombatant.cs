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
        [SerializeField] private int agility = 10;
        
        [Header("Affinities")]
        [SerializeField] private Element strongAgainst;
        [SerializeField] private Element weakAgainst;

        private void Awake()
        {
            IsPlayerSide = true;
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
            // You can expand this later to use Persona stats
            return BaseAttackPower; 
        }

        public override int GetDefenseStatFor(Element element)
        {
            return 10; // Placeholder defense
        }
    }
}
