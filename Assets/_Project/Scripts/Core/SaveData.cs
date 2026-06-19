using System.Collections.Generic;
using PixelMindscape.Data;

namespace PixelMindscape.Core
{
    [System.Serializable]
    public class SaveData
    {
        public string currentDate;             // ISO date string
        public TimeSlot currentTimeSlot;
        public List<string> storyFlags = new List<string>(); // generic flag bag, checked by CalendarEventData prerequisites

        public List<PartyMemberRuntimeState> partyMembers = new List<PartyMemberRuntimeState>();
        public List<PersonaRuntimeState> personaRoster = new List<PersonaRuntimeState>();
        public List<InventoryEntry> inventory = new List<InventoryEntry>();

        // Dictionary is not directly serializable by JsonUtility, but we will use a workaround or keep it simple for now
        // Unity's JsonUtility does not support Dictionaries. In a real project, we'd use a serializable Dictionary class or lists.
        // For the sake of the GDD, we will use a list of key-value pairs or rely on a better serializer like Newtonsoft JSON.
        // Let's implement a simple SerializableKVP for these
        
        public List<SocialStatEntry> socialStats = new List<SocialStatEntry>();
        public List<ConfidantRankState> confidantRanks = new List<ConfidantRankState>(); 

        public string activePersonaId;
        public List<string> equippedPersonaIds = new List<string>(); // loadout slots besides the active one
        
        public int GetSocialStat(SocialStatType type)
        {
            var stat = socialStats.Find(s => s.type == type);
            return stat != null ? stat.value : 0;
        }

        public void SetSocialStat(SocialStatType type, int value)
        {
            var stat = socialStats.Find(s => s.type == type);
            if (stat != null) stat.value = value;
            else socialStats.Add(new SocialStatEntry { type = type, value = value });
        }

        public int GetConfidantRank(string confidantId)
        {
            var rank = confidantRanks.Find(c => c.confidantId == confidantId);
            return rank != null ? rank.rank : 0;
        }

        public void SetConfidantRank(string confidantId, int rank)
        {
            var cr = confidantRanks.Find(c => c.confidantId == confidantId);
            if (cr != null) cr.rank = rank;
            else confidantRanks.Add(new ConfidantRankState { confidantId = confidantId, rank = rank });
        }
    }

    [System.Serializable]
    public class SocialStatEntry
    {
        public SocialStatType type;
        public int value;
    }

    [System.Serializable]
    public class ConfidantRankState
    {
        public string confidantId;
        public int rank;
    }

    [System.Serializable]
    public class PartyMemberRuntimeState
    {
        public string characterId; // matches CharacterData.characterId
        public int level;
        public int currentHP;
        public int currentSP;
        public int currentExp;
        public string equippedWeaponId, equippedArmorId, equippedAccessoryId;
    }

    [System.Serializable]
    public class PersonaRuntimeState
    {
        public string personaId; // matches PersonaData.personaId
        public int level;
        public int currentExp;
        public List<string> learnedSkillIds; // includes innate + inherited
    }

    [System.Serializable]
    public class InventoryEntry
    {
        public string itemId;
        public int quantity;
    }
}
