using UnityEngine;
using UnityEngine.UI;
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
        [field: SerializeField] public int EffectiveAgility { get; protected set; }
        [field: SerializeField] public bool IsDown { get; protected set; }
        [field: SerializeField] public bool IsDefeated { get; protected set; }
        
        public ShadowPersonality Personality;
        public float HpPercent => (float)CurrentHP / MaxHP;

        [field: SerializeField] public int CurrentHP { get; protected set; }
        [field: SerializeField] public int MaxHP { get; protected set; }
        [field: SerializeField] public int CurrentSP { get; protected set; }
        [field: SerializeField] public int MaxSP { get; protected set; }
        [field: SerializeField] public int BaseAttackPower { get; protected set; }
        [field: SerializeField] public bool IsGuarding { get; protected set; }
        [field: SerializeField] public float BatonPassMultiplier { get; protected set; } = 1f;

        public virtual void OnTurnStartCleanUp()
        {
            IsGuarding = false;
        }

        public virtual void OnTurnEndCleanUp()
        {
            BatonPassMultiplier = 1f;
        }

        protected Animator animator;
        protected CombatantVFXHandler vfxHandler;
        protected Slider hpSlider;

        public virtual void InitializeStats() { }

        protected virtual void Start()
        {
            animator = GetComponentInChildren<Animator>();
            vfxHandler = GetComponentInChildren<CombatantVFXHandler>();

            // Dynamically find the scene sliders for now (including inactive ones)
            string sliderName = IsPlayerSide ? "PlayerSlider" : "EnemySlider";
            Slider[] allSliders = FindObjectsOfType<Slider>(true);
            foreach (var slider in allSliders)
            {
                if (slider.gameObject.name == sliderName)
                {
                    hpSlider = slider;
                    break;
                }
            }

            if (hpSlider != null)
            {
                hpSlider.maxValue = MaxHP;
                hpSlider.value = CurrentHP;
            }
            else
            {
                Debug.LogWarning($"Could not find slider named {sliderName} in the scene.");
            }
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
            Debug.Log($"[Combatant] TakeDamage called on {gameObject.name}: amount={amount}, element={element}. CurrentHP before={CurrentHP}");
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
                // DOTween integration for hit animation (scaling for UI Canvas vs World Space)
                bool isUI = GetComponent<RectTransform>() != null;
                float shakeStrength = isUI ? 20f : 0.5f;
                transform.DOShakePosition(0.5f, shakeStrength, 10, 90, false, true);
            }

            if (hpSlider == null)
            {
                string sliderName = IsPlayerSide ? "PlayerSlider" : "EnemySlider";
                Slider[] allSliders = FindObjectsOfType<Slider>(true);
                foreach (var slider in allSliders)
                {
                    if (slider.gameObject.name == sliderName)
                    {
                        hpSlider = slider;
                        if (hpSlider != null) hpSlider.maxValue = MaxHP;
                        break;
                    }
                }
            }

            if (hpSlider != null)
            {
                hpSlider.value = CurrentHP;
            }
            else
            {
                Debug.LogWarning($"[Combatant] {gameObject.name} took damage but hpSlider is NULL! The UI health bar will not update. Please verify a Slider named '{(IsPlayerSide ? "PlayerSlider" : "EnemySlider")}' exists in the scene.");
            }

            Debug.Log($"[Combatant] {gameObject.name} took {amount} damage! CurrentHP is now {CurrentHP}/{MaxHP}. (hpSlider updated: {hpSlider != null})");

            // Spawn DOTween Damage Popup
            if (BattleManager.Instance != null && BattleManager.Instance.DamagePopupPrefab != null)
            {
                GameObject canvasObj = GameObject.Find("Canvas_BattleUI");
                Transform parentTransform = canvasObj != null ? canvasObj.transform : null;

                var popup = Instantiate(BattleManager.Instance.DamagePopupPrefab, parentTransform);
                
                // Position the popup correctly based on Canvas render mode
                if (canvasObj != null)
                {
                    Canvas canvas = canvasObj.GetComponent<Canvas>();
                    if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        // If the combatant is a world space object (not on a canvas), convert world to screen point
                        if (GetComponentInParent<Canvas>() == null && Camera.main != null)
                        {
                            popup.transform.position = Camera.main.WorldToScreenPoint(transform.position);
                        }
                        else
                        {
                            popup.transform.position = transform.position;
                        }
                    }
                    else
                    {
                        popup.transform.position = transform.position;
                    }
                }
                else
                {
                    popup.transform.position = transform.position;
                }

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
        public virtual void ApplyBatonPassBuff(int stack) 
        { 
            BatonPassMultiplier = 1f + (stack * 0.25f);
            Debug.Log($"[Combatant] {gameObject.name} received Baton Pass! Multiplier is now {BatonPassMultiplier}x (Stacks: {stack})");
        }
        public virtual void ApplyGuardStance() 
        { 
            IsGuarding = true; 
            Debug.Log($"[Combatant] {gameObject.name} entered Guard Stance!");
        }
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
