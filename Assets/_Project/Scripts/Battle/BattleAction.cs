using System.Collections.Generic;
using UnityEngine;
using PixelMindscape.Data;
using PixelMindscape.Core;

namespace PixelMindscape.Battle
{
    public abstract class BattleAction
    {
        public Combatant Source;
        // Initialize to empty list to prevent null-reference exceptions (e.g., in GuardAction)
        public List<Combatant> Targets = new List<Combatant>(); 

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
                if (target == null || target.IsDefeated) continue;

                var result = DamageCalculator.CalculateDamage(Source, target, null); 
                target.TakeDamage(result.finalDamage, Element.Physical);
                if (result.affinity == Affinity.Weak) 
                { 
                    target.SetDown(true); 
                    anyWeak = true; 
                }
            }
            return anyWeak;
        }
    }

    public class SkillAction : BattleAction
    {
        public SkillData skill;

        public override bool Execute(BattleManager battle)
        {
            if (skill == null || Source.CurrentSP < skill.spCost) return false; 
            Source.SpendSP(skill.spCost);

            bool anyWeak = false;
            foreach (var target in Targets)
            {
                if (target == null || target.IsDefeated) continue;

                var result = DamageCalculator.CalculateDamage(Source, target, skill);
                target.TakeDamage(result.finalDamage, skill.element);
                if (result.affinity == Affinity.Weak) 
                { 
                    target.SetDown(true); 
                    anyWeak = true; 
                }
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
            if (PassTo != null && !PassTo.IsDefeated)
            {
                battle.PerformBatonPass(Source, PassTo);
            }
            return false; // Baton pass itself doesn't hit a weakness
        }
    }

    public class SwitchPersonaAction : BattleAction
    {
        public PersonaRuntimeState NewPersona;

        public override bool Execute(BattleManager battle)
        {
            Source.EquipActivePersona(NewPersona); 
            
            if (BattleCinematicManager.Instance != null)
            {
                BattleCinematicManager.Instance.PlayPersonaSummonFlash(Source);
            }

            return false;
        }
    }

    public class ItemAction : BattleAction
    {
        public ItemData Item;

        public override bool Execute(BattleManager battle)
        {
            if (Item == null) return false;

            // Placeholder: Implement item effects based on ItemData (healing, reviving, etc.)
            // e.g. target.TakeDamage(-Item.healAmount, Element.None);
            Debug.Log($"{Source.gameObject.name} used {Item.displayName}!");
            return false;
        }
    }
}
