using UnityEngine;
using Fungus;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("Variable", 
                 "Load Bool From PlayerPrefs", 
                 "Loads a saved boolean from PlayerPrefs into a Fungus Boolean Variable.")]
    public class LoadBoolFromPlayerPrefsCommand : Command
    {
        [Tooltip("The PlayerPrefs key (e.g. 'Tutorial_Battle_Played')")]
        public string playerPrefsKey = "";

        [Tooltip("The Fungus Boolean Variable to store the loaded value in.")]
        [VariableProperty(typeof(BooleanVariable))]
        public BooleanVariable fungusVariable;

        public override void OnEnter()
        {
            if (fungusVariable != null && !string.IsNullOrEmpty(playerPrefsKey))
            {
                // Load 1 as true, 0 as false
                int value = PlayerPrefs.GetInt(playerPrefsKey, 0);
                fungusVariable.Value = (value == 1);
                Debug.Log($"[LoadBoolFromPlayerPrefs] SUCCESS! Loaded '{playerPrefsKey}' (Value: {value}) into variable {fungusVariable.Key}.");
            }
            else
            {
                Debug.LogError("[LoadBoolFromPlayerPrefs] FAILED! Either the Variable was not assigned in the dropdown, or the PlayerPrefs Key was empty.");
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
            return $"Load '{playerPrefsKey}' into {fungusVariable.Key}";
        }
    }
}
