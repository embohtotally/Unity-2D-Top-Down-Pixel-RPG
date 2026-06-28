using UnityEngine;
using Fungus;
using PixelMindscape.Data;
using PixelMindscape.Core;

namespace PixelMindscape.Dialogue
{
    public class FungusBridge : MonoBehaviour
    {
        [SerializeField] private Flowchart sharedFlowchart; // the flowchart holding the global shared variables

        public void PushStateToFungus()
        {
            var save = GameManager.Instance.CurrentSave;

            sharedFlowchart.SetIntegerVariable("knowledgeStat", save.GetSocialStat(SocialStatType.Knowledge));
            sharedFlowchart.SetIntegerVariable("gutsStat", save.GetSocialStat(SocialStatType.Guts));
            sharedFlowchart.SetIntegerVariable("charmStat", save.GetSocialStat(SocialStatType.Charm));
            sharedFlowchart.SetIntegerVariable("proficiencyStat", save.GetSocialStat(SocialStatType.Proficiency));
            sharedFlowchart.SetIntegerVariable("kindnessStat", save.GetSocialStat(SocialStatType.Kindness));
        }

        public void PullStateFromFungus()
        {
            var save = GameManager.Instance.CurrentSave;

            save.SetSocialStat(SocialStatType.Knowledge, sharedFlowchart.GetIntegerVariable("knowledgeStat"));
            save.SetSocialStat(SocialStatType.Guts, sharedFlowchart.GetIntegerVariable("gutsStat"));
            save.SetSocialStat(SocialStatType.Charm, sharedFlowchart.GetIntegerVariable("charmStat"));
            save.SetSocialStat(SocialStatType.Proficiency, sharedFlowchart.GetIntegerVariable("proficiencyStat"));
            save.SetSocialStat(SocialStatType.Kindness, sharedFlowchart.GetIntegerVariable("kindnessStat"));
        }

        // Called by a Fungus "Call Method" / Invoke Event block at the end of a successful rank-up branch.
        public void OnConfidantRankUpConfirmed(string confidantId, int newRank)
        {
            var save = GameManager.Instance.CurrentSave;
            save.SetConfidantRank(confidantId, newRank);

            // Fetch confidant and apply grantedAbilityId
            // var confidant = ConfidantLookup.GetById(confidantId);
            // var rankEntry = confidant.ranks.Find(r => r.rankNumber == newRank);
            // if (rankEntry != null && !string.IsNullOrEmpty(rankEntry.grantedAbilityId))
            //    GameManager.Instance.Battle.GrantPassiveAbility(rankEntry.grantedAbilityId);
        }

        public void CheckStoryFlag(string flagName)
        {
            var save = GameManager.Instance.CurrentSave;
            bool hasFlag = save != null && save.storyFlags.Contains(flagName);
            if (sharedFlowchart != null)
            {
                sharedFlowchart.SetBooleanVariable(flagName, hasFlag);
            }
        }

        public void SetStoryFlag(string flagName)
        {
            var save = GameManager.Instance.CurrentSave;
            if (save != null && !save.storyFlags.Contains(flagName))
            {
                save.storyFlags.Add(flagName);
            }
        }
    }
}
