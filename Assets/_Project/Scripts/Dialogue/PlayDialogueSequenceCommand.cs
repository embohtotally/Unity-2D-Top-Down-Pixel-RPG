using System.Collections;
using UnityEngine;
using Fungus;
using PixelMindscape.Data;

namespace PixelMindscape.Dialogue
{
    [CommandInfo("PixelMindscape", "Play Dialogue Sequence", "Plays a DialogueSequence asset using Fungus's Say system with portrait + typewriter support.")]
    public class PlayDialogueSequenceCommand : Command
    {
        public DialogueSequence sequence;

        public override void OnEnter()
        {
            if (sequence != null && sequence.lines != null && sequence.lines.Count > 0)
            {
                StartCoroutine(PlaySequence());
            }
            else
            {
                Continue();
            }
        }

        private IEnumerator PlaySequence()
        {
            foreach (var line in sequence.lines)
            {
                // Placeholder for actual lookup
                // var character = CharacterLookup.GetFungusCharacter(line.speakerId); 
                // var portrait = character.GetPortrait(line.expressionId);

                var sayDialog = SayDialog.GetSayDialog();
                // sayDialog.SetCharacter(character);
                // sayDialog.SetCharacterImage(portrait);

                yield return StartCoroutine(sayDialog.DoSay(
                    line.text,
                    true, // clearPrevious
                    true, // waitForInput
                    true, // fadeWhenDone
                    true, // stopVoiceover
                    false, // waitForVO
                    null, // voiceOverClip
                    null  // onComplete
                ));
            }

            Continue(); // resumes the calling Flowchart's next command
        }
        
        public override string GetSummary()
        {
            if (sequence == null)
            {
                return "Error: No sequence selected";
            }
            return sequence.name;
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }
    }
}
