using UnityEngine;
using Fungus;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("Variable", 
                 "Save Bool To PlayerPrefs", 
                 "Saves a Fungus Boolean Variable's value directly to PlayerPrefs on the hard drive.")]
    public class SaveBoolToPlayerPrefsCommand : Command
    {
        [Tooltip("The PlayerPrefs key (e.g. 'Tutorial_Battle_Played')")]
        public string playerPrefsKey = "";

        [Tooltip("The Fungus Boolean Variable to save.")]
        [VariableProperty(typeof(BooleanVariable))]
        public BooleanVariable fungusVariable;

        public override void OnEnter()
        {
            if (fungusVariable != null && !string.IsNullOrEmpty(playerPrefsKey))
            {
                // Save true as 1, false as 0
                int value = fungusVariable.Value ? 1 : 0;
                PlayerPrefs.SetInt(playerPrefsKey, value);
                PlayerPrefs.Save();
                Debug.Log($"[SaveBoolToPlayerPrefs] SUCCESS! Saved variable {fungusVariable.Key} ({fungusVariable.Value}) to PlayerPrefs key '{playerPrefsKey}'.");
            }
            else
            {
                Debug.LogError("[SaveBoolToPlayerPrefs] FAILED! Either the Variable was not assigned in the dropdown, or the PlayerPrefs Key was empty.");
            }
            Continue();
        }

        public override Color GetButtonColor()
        {
            return new Color32(253, 253, 150, 255);
        }

        public override string GetSummary()
        {
            if (fungusVariable == null) return "Error: No variable selected";
            return $"Save {fungusVariable.Key} to '{playerPrefsKey}'";
        }
    }
}
