using System.Collections.Generic;
using UnityEngine;

namespace PixelMindscape.Data
{
    [System.Serializable]
    public class ConfidantRankEntry
    {
        public int rankNumber;
        public SocialStatType requiredStatType; // stat checked before rank-up scene offers success branch
        public int requiredStatValue;
        
        [Tooltip("Drag the GameObject containing the Fungus Flowchart for this rank.")]
        public GameObject rankUpFlowchart;        // Replaced Flowchart with GameObject to maintain decoupling from Fungus package
        
        public string grantedAbilityId;          // looked up by BattleManager / passive ability resolver
    }

    [CreateAssetMenu(fileName = "NewConfidant", menuName = "PixelMindscape/Confidant Data")]
    public class ConfidantData : ScriptableObject
    {
        public string confidantId;
        public string displayName;
        public CharacterData linkedCharacter;

        [Header("Rank Definitions")]
        public List<ConfidantRankEntry> ranks; // ordered rank 1..N
    }
}
