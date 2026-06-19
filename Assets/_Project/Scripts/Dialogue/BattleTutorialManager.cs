using UnityEngine;
using Fungus;

namespace PixelMindscape.Battle
{
    public class BattleTutorialManager : MonoBehaviour
    {
        [Tooltip("The Flowchart containing the tutorial logic.")]
        public Flowchart tutorialFlowchart;

        private void OnEnable()
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.OnTurnStarted += HandleTurnStarted;
            }
        }

        private void OnDisable()
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.OnTurnStarted -= HandleTurnStarted;
            }
        }

        private void HandleTurnStarted(Combatant combatant)
        {
            if (tutorialFlowchart == null) return;

            // Pause the battle instantly so the UI doesn't activate and enemies don't attack
            BattleManager.Instance.PauseQueue();

            if (combatant.IsPlayerSide)
            {
                // Send a message to Fungus to trigger the block listening for "PlayerTurnStarted"
                Fungus.Flowchart.BroadcastFungusMessage("PlayerTurnStarted");
            }
            else
            {
                // Send a message to Fungus to trigger the block listening for "EnemyTurnStarted"
                Fungus.Flowchart.BroadcastFungusMessage("EnemyTurnStarted");
            }
            
            // Note: Make sure the Fungus Flowchart has a "ResumeBattle" command at the end of its blocks,
            // otherwise the battle will remain paused forever!
        }
    }
}
