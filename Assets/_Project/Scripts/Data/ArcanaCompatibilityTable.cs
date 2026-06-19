using System.Collections.Generic;
using UnityEngine;

namespace PixelMindscape.Data
{
    [System.Serializable]
    public class ArcanaFusionRule
    {
        public string arcanaA;
        public string arcanaB;
        public string resultArcana;  // resolved arcana of the fusion product
    }

    [CreateAssetMenu(fileName = "ArcanaCompatibilityTable", menuName = "PixelMindscape/Fusion/Arcana Compatibility Table")]
    public class ArcanaCompatibilityTable : ScriptableObject
    {
        public List<ArcanaFusionRule> rules;

        public ArcanaFusionRule FindRule(string arcanaA, string arcanaB)
        {
            // order-independent lookup
            return rules.Find(r =>
                (r.arcanaA == arcanaA && r.arcanaB == arcanaB) ||
                (r.arcanaA == arcanaB && r.arcanaB == arcanaA));
        }
    }
}
