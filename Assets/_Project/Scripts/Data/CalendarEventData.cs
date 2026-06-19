using System.Collections.Generic;
using UnityEngine;

namespace PixelMindscape.Data
{
    [CreateAssetMenu(fileName = "NewCalendarEvent", menuName = "PixelMindscape/Calendar Event Data")]
    public class CalendarEventData : ScriptableObject
    {
        public string eventId;
        public int month;
        public int day;
        public TimeSlot timeSlot;
        public List<string> prerequisiteFlags; // SaveData.storyFlags must contain all of these
        
        [Tooltip("Drag the GameObject containing the Fungus Flowchart for this scene.")]
        public GameObject sceneFlowchart;        // Replaced Flowchart with GameObject to maintain decoupling from Fungus package
        
        public bool isForcedStoryEvent;          // if true, CalendarManager auto-triggers it, overriding free choice
    }
}
