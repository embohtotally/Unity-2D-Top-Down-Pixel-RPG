using System.Collections.Generic;
using UnityEngine;

namespace PixelMindscape.Data
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "PixelMindscape/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string itemId;
        public string displayName;
        public Sprite icon;
        public bool isConsumable;
        public int healAmount;     // 0 if not a healing item
        public int reviveTarget;   // 0 = none, 1 = single, 2 = all
    }

    public enum EquipSlotType { Weapon, Armor, Accessory }

    [CreateAssetMenu(fileName = "NewEquipment", menuName = "PixelMindscape/Equipment Data")]
    public class EquipmentData : ScriptableObject
    {
        public string equipmentId;
        public string displayName;
        public Sprite icon;
        public EquipSlotType slotType; // Weapon, Armor, Accessory

        [Header("Stat Modifiers")]
        public int strengthMod;
        public int magicMod;
        public int enduranceMod;
        public int agilityMod;
        public int luckMod;

        [Header("Affinity Modifiers")]
        public List<ElementalAffinityEntry> affinityModifiers; // additive overrides while equipped
    }
}
