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
                var sayDialog = SayDialog.GetSayDialog();

                string displayName = "";
                Color nameColor = Color.white;

                if (line.speakerId == "char_protagonist")
                {
                    displayName = "Ren Amamiya";
                    nameColor = new Color32(220, 50, 50, 255); // Crimson Red for Joker
                }
                else if (line.speakerId == "char_brawler")
                {
                    displayName = "Takeshi";
                    nameColor = new Color32(240, 180, 40, 255); // Golden Yellow for Best Friend
                }
                else if (line.speakerId == "char_mage")
                {
                    displayName = "Hana";
                    nameColor = new Color32(255, 100, 150, 255); // Elegant Pink
                }
                else if (line.speakerId == "char_rogue")
                {
                    displayName = "Yuki";
                    nameColor = new Color32(80, 220, 100, 255); // Rogue Green
                }
                else if (line.speakerId == "char_tactician")
                {
                    displayName = "Sakura";
                    nameColor = new Color32(140, 100, 255, 255); // Tactician Purple
                }
                else if (line.speakerId == "char_healer")
                {
                    displayName = "Mai";
                    nameColor = new Color32(100, 200, 255, 255); // Gentle Cyan
                }
                else if (line.speakerId == "narrator")
                {
                    displayName = ""; // Leave blank for immersive narration
                    nameColor = Color.white;
                }
                else if (!string.IsNullOrEmpty(line.speakerId))
                {
                    displayName = line.speakerId;
                    nameColor = Color.white;
                }

                sayDialog.SetCharacterName(displayName, nameColor);

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
