using System.Collections.Generic;
using UnityEngine;

namespace PixelMindscape.Data
{
    [CreateAssetMenu(fileName = "NewDialogueLine", menuName = "PixelMindscape/Dialogue/Line")]
    public class DialogueLine : ScriptableObject
    {
        public string speakerId;          // matches CharacterData.characterId, or "narrator"
        [TextArea(2, 5)]
        public string text;               // localized at runtime by swapping the active locale's text asset
        public string expressionId;       // maps to a portrait frame in the speaker's portrait sheet
    }

}
