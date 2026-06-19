using UnityEngine;

namespace PixelMindscape.Data
{
    public enum Element { Physical, Gun, Fire, Ice, Electric, Wind, Psychic, Nuclear, Bless, Curse, Almighty }

    public enum Affinity { Normal, Weak, Resist, Null, Absorb, Repel }

    public enum SkillCategory { Physical, Magic, Support, Ailment, Healing }

    public enum TargetScope { SingleEnemy, AllEnemies, SingleAlly, AllAllies, Self, RandomEnemy }

    public enum ShadowPersonality { Upbeat, Timid, Irritable, Gloomy }

    public enum SocialStatType { Knowledge, Guts, Charm, Proficiency, Kindness }

    public enum TimeSlot { Morning, AfterSchool, Evening }

    // Placeholder enum for status effects
    public enum StatusEffectType { None, Burn, Freeze, Shock, Dizzy, Confuse, Fear, Brainwash, Sleep, Despair, Rage, Hunger }
}
