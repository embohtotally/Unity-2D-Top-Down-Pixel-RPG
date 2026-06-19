using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PixelMindscape.Data;
using PixelMindscape.Core;

namespace PixelMindscape.Persona
{
    public class PersonaFusionManager : MonoBehaviour
    {
        [SerializeField] private ArcanaCompatibilityTable compatibilityTable;
        [SerializeField] private List<PersonaData> allPersonaTemplates;

        public PersonaData GetFusionResult(PersonaData personaA, PersonaData personaB)
        {
            var rule = compatibilityTable.FindRule(personaA.arcana, personaB.arcana);
            if (rule == null) return null; // UI should disallow confirming an invalid pairing before this is called

            int resultLevel = (personaA.baseLevel + personaB.baseLevel) / 2;

            // Among templates matching resultArcana, pick the highest-level one not exceeding resultLevel + tolerance.
            return allPersonaTemplates.FindAll(p => p.arcana == rule.resultArcana)
                .FindAll(p => p.baseLevel <= resultLevel + 5)
                .OrderByDescending(p => p.baseLevel)
                .FirstOrDefault();
        }

        public List<SkillData> GetInheritableSkillPool(PersonaRuntimeState a, PersonaRuntimeState b)
        {
            // Placeholder: we need a SkillLookup mechanism. For now returning empty.
            // var pool = new List<string>();
            // pool.AddRange(a.learnedSkillIds);
            // pool.AddRange(b.learnedSkillIds);
            // return pool.Distinct().Select(SkillLookup.GetById).ToList();
            return new List<SkillData>();
        }

        public PersonaRuntimeState ApplyInheritance(PersonaRuntimeState resultRuntime, List<SkillData> chosenSkills, int maxSkillSlots)
        {
            if (chosenSkills.Count > maxSkillSlots)
                throw new System.InvalidOperationException("Chosen skill count exceeds available slots on the fusion result.");

            foreach (var skill in chosenSkills)
                if (!resultRuntime.learnedSkillIds.Contains(skill.skillId))
                    resultRuntime.learnedSkillIds.Add(skill.skillId);

            return resultRuntime;
        }
    }
}
