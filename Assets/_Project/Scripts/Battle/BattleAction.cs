using System.Collections.Generic;
using PixelMindscape.Data;
using PixelMindscape.Core;

namespace PixelMindscape.Battle
{
    public abstract class BattleAction
    {
        public Combatant Source;
        public List<Combatant> Targets;

        /// Returns true if the action resulted in a weakness hit
        public abstract bool Execute(BattleManager battle);
    }

    public class AttackAction : BattleAction
    {
        public override bool Execute(BattleManager battle)
        {
            bool anyWeak = false;
            foreach (var target in Targets)
            {
                var result = DamageCalculator.CalculateDamage(Source, target, null); 
                target.TakeDamage(result.finalDamage, Element.Physical);
                if (result.affinity == Affinity.Weak) { target.SetDown(true); anyWeak = true; }
            }
            return anyWeak;
        }
    }

    public class SkillAction : BattleAction
    {
        public SkillData skill;

        public override bool Execute(BattleManager battle)
        {
            if (Source.CurrentSP < skill.spCost) return false; 
            Source.SpendSP(skill.spCost);

            bool anyWeak = false;
            foreach (var target in Targets)
            {
                var result = DamageCalculator.CalculateDamage(Source, target, skill);
                target.TakeDamage(result.finalDamage, skill.element);
                if (result.affinity == Affinity.Weak) { target.SetDown(true); anyWeak = true; }
            }
            return anyWeak;
        }
    }

    public class GuardAction : BattleAction
    {
        public override bool Execute(BattleManager battle)
        {
            Source.ApplyGuardStance();
            return false;
        }
    }

    public class BatonPassAction : BattleAction
    {
        public Combatant PassTo;

        public override bool Execute(BattleManager battle)
        {
            battle.PerformBatonPass(Source, PassTo);
            return false;
        }
    }

    public class SwitchPersonaAction : BattleAction
    {
        public PersonaRuntimeState NewPersona;

        public override bool Execute(BattleManager battle)
        {
            Source.EquipActivePersona(NewPersona); 
            return false;
        }
    }
}
