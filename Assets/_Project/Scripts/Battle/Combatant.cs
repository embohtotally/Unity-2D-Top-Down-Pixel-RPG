using UnityEngine;
using PixelMindscape.Data;
using PixelMindscape.Core;
using DG.Tweening; // Integrating DOTween as requested

namespace PixelMindscape.Battle
{
    public abstract class Combatant : MonoBehaviour
    {
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

        public virtual void GrantOneMore() { }
        public virtual void ApplyBatonPassBuff(int stack) { }
        public virtual void ApplyGuardStance() { }
        public virtual void EquipActivePersona(PersonaRuntimeState persona) { }

        public abstract Affinity GetAffinity(Element element);
        public abstract int GetAttackStatFor(Element element);
        public abstract int GetDefenseStatFor(Element element);
    }
}
