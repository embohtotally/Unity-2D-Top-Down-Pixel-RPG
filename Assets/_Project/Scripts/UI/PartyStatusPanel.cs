using System.Collections.Generic;
using UnityEngine;
using PixelMindscape.Core;
using PixelMindscape.Data;
using PixelMindscape.Battle;

namespace PixelMindscape.UI
{
    public class PartyStatusPanel : MonoBehaviour
    {
        [SerializeField] private Transform container;
        [SerializeField] private PartySlotUI partySlotPrefab;

        private List<PartySlotUI> activeSlots = new List<PartySlotUI>();

        private void Start()
        {
            BuildPanel();
        }

        private void Update()
        {
            if (activeSlots.Count == 0 && BattleManager.Instance != null && BattleManager.Instance.GetActiveParty().Count > 0)
            {
                BuildPanel();
            }
        }

        public void BuildPanel()
        {
            if (container == null || partySlotPrefab == null) return;

            foreach (var slot in activeSlots)
            {
                if (slot != null && slot.gameObject != null)
                    Destroy(slot.gameObject);
            }
            activeSlots.Clear();

            // Check BattleManager for active party combatants
            if (BattleManager.Instance != null)
            {
                var party = BattleManager.Instance.GetActiveParty();
                foreach (var combatant in party)
                {
                    var slot = Instantiate(partySlotPrefab, container);
                    activeSlots.Add(slot);

                    CharacterData cData = null;
                    if (combatant is HeroCombatant hero) cData = hero.CharacterData;

                    PartyMemberRuntimeState state = null;
                    if (GameManager.Instance != null && GameManager.Instance.CurrentSave != null && cData != null)
                    {
                        state = GameManager.Instance.CurrentSave.partyMembers.Find(p => p.characterId == cData.characterId);
                    }

                    slot.Bind(state, cData, combatant);

                    // Subscribe to OnStatsChanged
                    combatant.OnStatsChanged += (c) => 
                    {
                        if (slot != null)
                        {
                            slot.UpdateBars(c.CurrentHP, c.MaxHP, c.CurrentSP, c.MaxSP);
                        }
                    };
                }
            }
        }
    }
}
