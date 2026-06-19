using System.Collections.Generic;
using UnityEngine;

namespace PixelMindscape.Data
{
    [System.Serializable]
    public class SkillUnlockEntry
    {
        public int level;
        public SkillData skill;
    }

    [CreateAssetMenu(fileName = "NewPersona", menuName = "PixelMindscape/Persona Data")]
    public class PersonaData : ScriptableObject
    {
        [Header("Identity")]
        public string personaId;
        public string displayName;
        public string arcana;                 // e.g. "Magician", "Empress" — drives fusion compatibility lookups
        public Sprite[] summonFlashFrames;    // 6-10 frame burst animation

        [Header("Base Stats (at base level)")]
        public int baseLevel;
        public int baseHP;
        public int baseSP;
        public int baseStrength;
        public int baseMagic;
        public int baseEndurance;
        public int baseAgility;
        public int baseLuck;

        [Header("Skill Progression")]
        public List<SkillUnlockEntry> innateSkillsByLevel; // skills this Persona learns naturally as it levels

        [Header("Elemental Affinity Table")]
        public List<ElementalAffinityEntry> affinityTable; // full weak/resist/null/absorb/repel per element

        [Header("Fusion")]
        public bool isFusionOnly; // true if this Persona cannot be recruited via negotiation, only produced by fusion
    }
}
