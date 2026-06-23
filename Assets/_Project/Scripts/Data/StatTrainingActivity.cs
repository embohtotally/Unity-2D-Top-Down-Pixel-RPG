using UnityEngine;

namespace PixelMindscape.Data
{
    [CreateAssetMenu(fileName = "NewStatTraining", menuName = "PixelMindscape/Stat Training Activity")]
    public class StatTrainingActivity : ScriptableObject
    {
        public string activityName;
        public SocialStatType statType;
        public int pointsGranted;
        public TimeSlot timeCost = TimeSlot.AfterSchool; // Optional check for when this can be done
    }
}
