using System.Collections.Generic;
using UnityEngine;
using PixelMindscape.Battle;
using DG.Tweening;

namespace PixelMindscape.UI
{
    public class TurnTimelineBar : MonoBehaviour
    {
        [SerializeField] private Transform container;
        [SerializeField] private TurnSlot turnSlotPrefab;
        [SerializeField] private float spacing = 70f;
        [SerializeField] private float animationDuration = 0.3f;

        private List<TurnSlot> activeSlots = new List<TurnSlot>();
        private Combatant lastCurrentCombatant = null;

        public void Refresh(List<Combatant> turnQueue)
        {
            if (container == null || turnSlotPrefab == null || turnQueue == null || turnQueue.Count == 0) return;

            bool isOneMore = lastCurrentCombatant == turnQueue[0] && turnQueue[0].HasOneMore;
            lastCurrentCombatant = turnQueue[0];

            // Build display queue predicting up to 5 upcoming turns
            List<Combatant> displayQueue = new List<Combatant>(turnQueue);

            if (displayQueue.Count < 5 && BattleManager.Instance != null)
            {
                List<Combatant> allLiving = new List<Combatant>();
                if (BattleManager.Instance.GetActiveParty() != null)
                {
                    foreach (var p in BattleManager.Instance.GetActiveParty())
                    {
                        if (p != null && !p.IsDefeated) allLiving.Add(p);
                    }
                }
                if (BattleManager.Instance.GetActiveEnemies() != null)
                {
                    foreach (var e in BattleManager.Instance.GetActiveEnemies())
                    {
                        if (e != null && !e.IsDefeated) allLiving.Add(e);
                    }
                }

                // Sort by EffectiveAgility descending to predict next round order
                allLiving.Sort((a, b) => b.EffectiveAgility.CompareTo(a.EffectiveAgility));

                int loopGuard = 0;
                while (displayQueue.Count < 5 && allLiving.Count > 0 && loopGuard < 20)
                {
                    foreach (var c in allLiving)
                    {
                        if (displayQueue.Count >= 5) break;
                        displayQueue.Add(c);
                    }
                    loopGuard++;
                }
            }

            if (displayQueue.Count > 5)
            {
                displayQueue = displayQueue.GetRange(0, 5);
            }

            // Rebuild slots
            foreach (var slot in activeSlots)
            {
                if (slot != null && slot.gameObject != null)
                    Destroy(slot.gameObject);
            }
            activeSlots.Clear();

            for (int i = 0; i < displayQueue.Count; i++)
            {
                var combatant = displayQueue[i];
                var slot = Instantiate(turnSlotPrefab, container);
                activeSlots.Add(slot);

                bool isCurrent = (i == 0);
                slot.Bind(combatant, isCurrent, isOneMore);

                // Smooth DOTween slide-in/arrange
                var rect = slot.GetComponent<RectTransform>();
                if (rect != null)
                {
                    float targetX = i * spacing;
                    rect.anchoredPosition = new Vector2(targetX + 50f, rect.anchoredPosition.y);
                    rect.DOAnchorPosX(targetX, animationDuration).SetEase(Ease.OutBack);
                }
            }
        }
    }
}
