using UnityEngine;

namespace PixelMindscape.Data
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "PixelMindscape/Skill Data")]
    public class SkillData : ScriptableObject
    {
        public string skillId;
        public string displayName;
        public SkillCategory category;
        public Element element;
        public int basePower;
        public int spCost;
        public TargetScope targetScope;

        [Header("Status Effect (optional)")]
        public bool inflictsStatus;
        public StatusEffectType statusEffect;   
        public float statusChance;              // 0-1

        [Header("Presentation")]
        public AnimationClip useAnimation;
        public GameObject vfxPrefab;
    }
}
