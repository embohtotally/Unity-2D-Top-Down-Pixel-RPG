using System.Collections.Generic;
using UnityEngine;

namespace PixelMindscape.Data
{
    [CreateAssetMenu(fileName = "NewDialogueSequence", menuName = "PixelMindscape/Dialogue/Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        public List<DialogueLine> lines; // played in order by the custom Fungus command
    }
}
