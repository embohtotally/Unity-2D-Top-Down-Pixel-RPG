using UnityEngine;
using PixelMindscape.Data;
using PixelMindscape.Core;
using DG.Tweening; // Integrating DOTween as requested

namespace PixelMindscape.Battle
{
    public abstract class Combatant : MonoBehaviour
    {
        [Header("UI Data")]
        public Sprite TurnPortrait; // Assign an icon for the Turn Order UI

        public bool IsPlayerSide;
        public int EffectiveAgility { get; protected set; }
        public bool IsDown { get; protected set; }
        public bool IsDefeated { get; protected set; }
        
        public ShadowPersonality Personality;
        public float HpPercent => (float)CurrentHP / MaxHP;

        public int CurrentHP { get; protected set; }
        public int MaxHP { get; protected set; }
        public int CurrentSP { get; protected set; }
        public int MaxSP { get; protected set; }
        public int BaseAttackPower { get; protected set; }

        protected Animator animator;
        protected CombatantVFXHandler vfxHandler;

        protected virtual void Start()
        {
            animator = GetComponentInChildren<Animator>();
            vfxHandler = GetComponentInChildren<CombatantVFXHandler>();
        }

        public virtual void PlayAttackAnimation()
        {
            if (animator != null) animator.SetTrigger("Attack");
            if (vfxHandler != null) vfxHandler.PlayAttackVFX();
        }

        public virtual void PlayCastAnimation()
        {
            if (animator != null) animator.SetTrigger("Cast");
        }

        public virtual void PlayGuardAnimation()
        {
            if (animator != null) animator.SetTrigger("Guard");
            if (vfxHandler != null) vfxHandler.PlayGuardVFX();
        }

        public virtual void PlayBatonPassVFX()
        {
            if (vfxHandler != null) vfxHandler.PlayBatonPassVFX();
        }

        public virtual void PlaySkillVFX(GameObject vfxPrefab)
        {
            if (vfxHandler != null && vfxPrefab != null)
            {
                vfxHandler.PlayVFX(vfxPrefab);
            }
        }

        public virtual void TakeDamage(float amount, Element element)
        {
            bool isHeal = amount < 0;
            if (isHeal) 
            {
                CurrentHP = Mathf.Min(CurrentHP - (int)amount, MaxHP);
            }
            else
            {
                CurrentHP = Mathf.Max(CurrentHP - (int)amount, 0);
                if (CurrentHP <= 0) IsDefeated = true;
                
                if (animator != null) animator.SetTrigger("TakeDamage");
                if (vfxHandler != null) vfxHandler.PlayHitVFX();
                // DOTween integration for hit animation
                transform.DOShakePosition(0.5f, 0.5f, 10, 90, false, true);
            }

            // Spawn DOTween Damage Popup
            if (BattleManager.Instance != null && BattleManager.Instance.DamagePopupPrefab != null)
            {
                var popup = Instantiate(BattleManager.Instance.DamagePopupPrefab, transform.position, Quaternion.identity);
                popup.Setup((int)amount, isHeal);
            }
        }

        public virtual void SpendSP(int amount)
        {
            CurrentSP = Mathf.Max(0, CurrentSP - amount);
        }

        public virtual void SetDown(bool state)
        {
            IsDown = state;
            if (animator != null) animator.SetBool("IsDown", state);

            if (state)
            {
                // DOTween integration for knockdown
                transform.DORotate(new Vector3(0, 0, 90), 0.3f);
            }
            else
            {
                transform.DORotate(Vector3.zero, 0.3f);
            }
        }

        public bool HasOneMore { get; protected set; }
        public virtual void GrantOneMore() { HasOneMore = true; }
        public virtual void ClearOneMore() { HasOneMore = false; }
        public virtual void ApplyBatonPassBuff(int stack) { }
        public virtual void ApplyGuardStance() { }
        public virtual void EquipActivePersona(PersonaRuntimeState persona) { }

        public abstract Affinity GetAffinity(Element element);
        public abstract int GetAttackStatFor(Element element);
        public abstract int GetDefenseStatFor(Element element);

        public virtual System.Collections.Generic.List<SkillData> GetAvailableSkills()
        {
            return new System.Collections.Generic.List<SkillData>();
        }
    }
}
