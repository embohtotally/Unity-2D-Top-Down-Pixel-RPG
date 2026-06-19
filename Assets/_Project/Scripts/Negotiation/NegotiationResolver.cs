using UnityEngine;
using Fungus;
using PixelMindscape.Data;
using PixelMindscape.Core;
using PixelMindscape.Battle;

namespace PixelMindscape.Negotiation
{
    public enum NegotiationOutcome { Recruit, Money, Item }

    public class NegotiationResolver : MonoBehaviour
    {
        [SerializeField] private Flowchart negotiationFlowchart;

        public void BeginNegotiation(Combatant shadow)
        {
            if (negotiationFlowchart != null)
            {
                negotiationFlowchart.SetStringVariable("shadowPersonality", shadow.Personality.ToString());
                negotiationFlowchart.SetFloatVariable("shadowHpPercent", shadow.HpPercent);

                negotiationFlowchart.SendFungusMessage("BeginNegotiation");
            }
        }

        public void HandleNegotiationResult(string chosenTone, string personalityStr, float hpPercent)
        {
            // Parse personality
            if (!System.Enum.TryParse(personalityStr, out ShadowPersonality personality))
            {
                personality = ShadowPersonality.Upbeat;
            }

            float successChance = CalculateSuccessChance(chosenTone, personality, hpPercent);
            bool success = Random.value <= successChance;

            if (success)
            {
                NegotiationOutcome outcome = RollOutcomeType(); 
                ApplyOutcome(outcome);
            }
            else
            {
                // Failed negotiation
            }

            if (GameManager.Instance != null && GameManager.Instance.Battle != null)
            {
                var bm = GameManager.Instance.Battle as BattleManager;
                if (bm != null) bm.EndNegotiationPhase(success);
            }
        }

        private float CalculateSuccessChance(string tone, ShadowPersonality personality, float hpPercent)
        {
            float matchBonus = 0.2f; // ToneMatchTable.GetMatchBonus(tone, personality);
            float hpFactor = 1f - hpPercent; // closer to 0 HP -> closer to 1.0 factor
            return Mathf.Clamp01(0.3f + matchBonus + (hpFactor * 0.3f));
        }

        private NegotiationOutcome RollOutcomeType() { return NegotiationOutcome.Recruit; }

        private void ApplyOutcome(NegotiationOutcome outcome) { }
    }
}
