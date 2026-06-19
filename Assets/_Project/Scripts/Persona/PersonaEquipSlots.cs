using System.Collections.Generic;
using UnityEngine;
using PixelMindscape.Data;
using PixelMindscape.Core;

namespace PixelMindscape.Persona
{
    public class PersonaEquipSlots : MonoBehaviour
    {
        public PersonaRuntimeState ActivePersona { get; private set; }
        public List<PersonaRuntimeState> Loadout { get; private set; } = new List<PersonaRuntimeState>(); // includes ActivePersona + benched-but-equipped slots

        public void SetActive(PersonaRuntimeState persona)
        {
            if (!Loadout.Contains(persona))
                throw new System.InvalidOperationException("Persona must be in the current loadout to be set active.");
            ActivePersona = persona;
        }

        public void EquipToLoadout(PersonaRuntimeState persona, int maxLoadoutSize)
        {
            if (Loadout.Count >= maxLoadoutSize)
                throw new System.InvalidOperationException("Loadout full — unequip a Persona first.");
            Loadout.Add(persona);
        }
    }
}
