using System.Collections;
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

        public bool WasWeaknessHit { get; protected set; }

        /// Executes the action as a coroutine to allow animation choreography
        public abstract IEnumerator Execute(BattleManager battle);
    }

    public class AttackAction : BattleAction
    {
        public override IEnumerator Execute(BattleManager battle)
        {
            WasWeaknessHit = false;
            if (Source != null)
            {
                Source.PlayAttackAnimation();
                // Optional: DOTween forward lunge could be added here
            }

            // Wait for attack animation impact point
            yield return new WaitForSeconds(0.3f);

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
            WasWeaknessHit = anyWeak;

            // Wait for recovery / step back
            yield return new WaitForSeconds(0.4f);
        }
    }

    public class SkillAction : BattleAction
    {
        public SkillData skill;

        public override IEnumerator Execute(BattleManager battle)
        {
            WasWeaknessHit = false;
            if (skill == null || Source.CurrentSP < skill.spCost) yield break; 
            Source.SpendSP(skill.spCost);

            if (Source != null)
            {
                Source.PlayCastAnimation();
            }

            // Wait for spell casting animation
            yield return new WaitForSeconds(0.4f);

            bool anyWeak = false;
            foreach (var target in Targets)
            {
                if (target == null || target.IsDefeated) continue;

                if (skill.vfxPrefab != null)
                {
                    target.PlaySkillVFX(skill.vfxPrefab);
                }

                var result = DamageCalculator.CalculateDamage(Source, target, skill);
                target.TakeDamage(result.finalDamage, skill.element);
                if (result.affinity == Affinity.Weak) 
                { 
                    target.SetDown(true); 
                    anyWeak = true; 
                }
            }
            WasWeaknessHit = anyWeak;

            // Wait for spell dissipation / recovery
            yield return new WaitForSeconds(0.4f);
        }
    }

    public class GuardAction : BattleAction
    {
        public override IEnumerator Execute(BattleManager battle)
        {
            WasWeaknessHit = false;
            if (Source != null)
            {
                Source.ApplyGuardStance();
                Source.PlayGuardAnimation();
            }
            yield return new WaitForSeconds(0.3f);
        }
    }

    public class BatonPassAction : BattleAction
    {
        public Combatant PassTo;

        public override IEnumerator Execute(BattleManager battle)
        {
            WasWeaknessHit = false;
            if (PassTo != null && !PassTo.IsDefeated)
            {
                if (Source != null) Source.PlayBatonPassVFX();
                PassTo.PlayBatonPassVFX();
                battle.PerformBatonPass(Source, PassTo);
            }
            yield return new WaitForSeconds(0.3f);
        }
    }

    public class SwitchPersonaAction : BattleAction
    {
        public PersonaRuntimeState NewPersona;

        public override IEnumerator Execute(BattleManager battle)
        {
            WasWeaknessHit = false;
            if (Source != null)
            {
                Source.EquipActivePersona(NewPersona); 

                if (BattleCinematicManager.Instance != null)
                {
                    BattleCinematicManager.Instance.PlayPersonaSummonFlash(Source);
                }
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    public class ItemAction : BattleAction
    {
        public ItemData Item;

        public override IEnumerator Execute(BattleManager battle)
        {
            WasWeaknessHit = false;
            if (Item == null) yield break;

            // Placeholder: Implement item effects based on ItemData (healing, reviving, etc.)
            // e.g. target.TakeDamage(-Item.healAmount, Element.None);
            if (Source != null)
            {
                Debug.Log($"{Source.gameObject.name} used {Item.displayName}!");
            }
            yield return new WaitForSeconds(0.4f);
        }
    }
}
