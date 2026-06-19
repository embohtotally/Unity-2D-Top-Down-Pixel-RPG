using System.Collections.Generic;
using UnityEngine;

namespace PixelMindscape.Data
{
    [System.Serializable]
    public class ElementalAffinityEntry
    {
        public Element element;
        public Affinity affinity;
    }

    [CreateAssetMenu(fileName = "NewCharacter", menuName = "PixelMindscape/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Identity")]
        public string characterId;          // unique key used by SaveData lookups
        public string displayName;
        public Sprite overworldPortrait;
        // public Sprite[] battleSpriteSheet;  // idle/attack/hit/down frames, indexed by BattleSpriteState (omitting for now, will implement animation with DOTween later)

        [Header("Base Stats")]
        public int baseHP;
        public int baseSP;
        public int baseStrength;
        public int baseMagic;
        public int baseEndurance;
        public int baseAgility;
        public int baseLuck;

        [Header("Growth")]
        public AnimationCurve hpGrowthCurve;   // evaluated per level to derive HP at that level
        public AnimationCurve spGrowthCurve;
        public AnimationCurve statGrowthCurve; // applied uniformly to STR/MAG/END/AGI/LUK unless overridden

        [Header("Affinity Overrides")]
        public List<ElementalAffinityEntry> affinityOverrides; // character-specific resist/weak, rare — most affinity comes from equipped Persona

        [Header("Confidant Linkage")]
        public ConfidantData linkedConfidant; // null if this character has no Confidant arc (e.g., a minor NPC)
    }
}
