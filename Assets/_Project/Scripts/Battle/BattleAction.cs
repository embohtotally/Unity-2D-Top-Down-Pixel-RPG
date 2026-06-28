using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelMindscape.Data;
using PixelMindscape.Core;
using DG.Tweening;

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
            Debug.Log($"[BattleAction] AttackAction.Execute started by {(Source != null ? Source.gameObject.name : "Unknown")}. Targets count: {Targets.Count}");
            WasWeaknessHit = false;
            if (Source != null)
            {
                Source.PlayAttackAnimation();
                
                // Perform a dynamic DOTween forward lunge animation (scaling for UI Canvas vs World Space)
                bool isUI = Source.GetComponent<RectTransform>() != null;
                float lungeDistance = isUI ? 50f : 1.5f;
                Vector3 punchDir = Source.IsPlayerSide ? new Vector3(lungeDistance, 0, 0) : new Vector3(-lungeDistance, 0, 0);
                Source.transform.DOPunchPosition(punchDir, 0.5f, 0, 1f);
            }

            // Wait for attack animation impact point
            yield return new WaitForSeconds(0.3f);

            bool anyWeak = false;
            foreach (var target in Targets)
            {
                if (target == null || target.IsDefeated)
                {
                    Debug.LogWarning($"[BattleAction] AttackAction target is null or already defeated.");
                    continue;
                }

                var result = DamageCalculator.CalculateDamage(Source, target, null); 
                Debug.Log($"[BattleAction] AttackAction dealing {result.finalDamage} damage to {target.gameObject.name}.");
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
            Debug.Log($"[BattleAction] AttackAction.Execute completed.");
        }
    }

    public class SkillAction : BattleAction
    {
        public SkillData skill;

        public override IEnumerator Execute(BattleManager battle)
        {
            Debug.Log($"[BattleAction] SkillAction.Execute started by {(Source != null ? Source.gameObject.name : "Unknown")} using skill '{(skill != null ? skill.displayName : "Null")}'. Targets count: {Targets.Count}");
            WasWeaknessHit = false;
            if (skill == null || Source.CurrentSP < skill.spCost)
            {
                Debug.LogWarning($"[BattleAction] SkillAction failed: skill is null or not enough SP (CurrentSP={Source.CurrentSP}, Cost={(skill != null ? skill.spCost : 0)})");
                yield break; 
            }
            Source.SpendSP(skill.spCost);

            if (Source != null)
            {
                Source.PlayCastAnimation();
                bool isUI = Source.GetComponent<RectTransform>() != null;
                float hopDistance = isUI ? 30f : 1.0f;
                Source.transform.DOPunchPosition(new Vector3(0, hopDistance, 0), 0.5f, 0, 1f);
            }

            // Wait for spell casting animation
            yield return new WaitForSeconds(0.4f);

            bool anyWeak = false;
            foreach (var target in Targets)
            {
                if (target == null || target.IsDefeated)
                {
                    Debug.LogWarning($"[BattleAction] SkillAction target is null or already defeated.");
                    continue;
                }

                if (skill.vfxPrefab != null)
                {
                    target.PlaySkillVFX(skill.vfxPrefab);
                }

                var result = DamageCalculator.CalculateDamage(Source, target, skill);
                
                if (result.affinity == Affinity.Repel)
                {
                    Debug.Log($"[BattleAction] SkillAction reflected by {target.gameObject.name} back to {Source.gameObject.name}!");
                    if (Source != null && !Source.IsDefeated)
                    {
                        Source.TakeDamage(result.finalDamage, skill.element);
                    }
                }
                else
                {
                    Debug.Log($"[BattleAction] SkillAction dealing {result.finalDamage} damage to {target.gameObject.name}.");
                    target.TakeDamage(result.finalDamage, skill.element);
                    if (result.affinity == Affinity.Weak) 
                    { 
                        target.SetDown(true); 
                        anyWeak = true; 
                    }

                    if (skill.inflictsStatus && !target.IsDefeated)
                    {
                        if (Random.value <= skill.statusChance)
                        {
                            Debug.Log($"[BattleAction] SkillAction successfully inflicted {skill.statusEffect} on {target.gameObject.name}!");
                        }
                    }
                }
            }
            WasWeaknessHit = anyWeak;

            // Wait for spell dissipation / recovery
            yield return new WaitForSeconds(0.4f);
            Debug.Log($"[BattleAction] SkillAction.Execute completed.");
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

            if (Source != null)
            {
                Debug.Log($"{Source.gameObject.name} used {Item.displayName}!");
                Source.PlayCastAnimation();
            }

            yield return new WaitForSeconds(0.3f);

            foreach (var target in Targets)
            {
                if (target == null) continue;

                if (Item.healAmount > 0)
                {
                    Debug.Log($"[BattleAction] ItemAction healing {target.gameObject.name} for {Item.healAmount} HP.");
                    target.TakeDamage(-Item.healAmount, Element.Physical); // TakeDamage treats negative values as healing
                }
                
                if (Item.reviveTarget > 0 && target.IsDefeated)
                {
                    Debug.Log($"[BattleAction] ItemAction reviving {target.gameObject.name}!");
                    target.TakeDamage(-Item.healAmount > 0 ? -Item.healAmount : -50, Element.Physical);
                    target.SetDown(false);
                }
            }

            yield return new WaitForSeconds(0.4f);
        }
    }
}
