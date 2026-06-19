using System;
using System.Collections.Generic;
using UnityEngine;
using PixelMindscape.Data;
using PixelMindscape.Core;

namespace PixelMindscape.Calendar
{
    public class CalendarManager : MonoBehaviour, ICalendarManager
    {
        [SerializeField] private List<CalendarEventData> allEvents;
        
        // Use reflection or interface for FungusBridge to avoid tight coupling if preferred,
        // but here we just reference it via a component since they're in different assemblies.
        private Component fungusBridge;

        public DateTime CurrentDate { get; private set; }
        public TimeSlot CurrentSlot { get; private set; }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Calendar = this;
                
                // Load saved date
                if (DateTime.TryParse(GameManager.Instance.CurrentSave.currentDate, out DateTime savedDate))
                    CurrentDate = savedDate;
                else
                    CurrentDate = new DateTime(2025, 4, 11); // Default starting date
                    
                CurrentSlot = GameManager.Instance.CurrentSave.currentTimeSlot;
            }
        }

        public void AdvanceTimeSlot()
        {
            var save = GameManager.Instance.CurrentSave;

            if (CurrentSlot == TimeSlot.Evening)
            {
                CurrentDate = CurrentDate.AddDays(1);
                CurrentSlot = TimeSlot.Morning;
            }
            else
            {
                CurrentSlot = CurrentSlot + 1;
            }

            save.currentDate = CurrentDate.ToString("yyyy-MM-dd");
            save.currentTimeSlot = CurrentSlot;

            CheckDungeonDeadlines();
            TryTriggerForcedEvent();
        }

        private void CheckDungeonDeadlines()
        {
            int daysLeft = 14; // Placeholder
            // fungusBridge.PushDungeonDeadline(daysLeft); 
        }

        private void TryTriggerForcedEvent()
        {
            var forced = allEvents.Find(e =>
                e.isForcedStoryEvent &&
                e.month == CurrentDate.Month &&
                e.day == CurrentDate.Day &&
                e.timeSlot == CurrentSlot &&
                PrerequisitesMet(e));

            if (forced != null)
                ExecuteEvent(forced);
        }

        public List<CalendarEventData> GetAvailableEvents()
        {
            return allEvents.FindAll(e =>
                e.month == CurrentDate.Month &&
                e.day == CurrentDate.Day &&
                e.timeSlot == CurrentSlot &&
                !e.isForcedStoryEvent &&
                PrerequisitesMet(e));
        }

        public void ExecuteEvent(CalendarEventData eventData)
        {
            // Execute event
            if (eventData.sceneFlowchart != null)
            {
                eventData.sceneFlowchart.SendMessage("ExecuteBlock", "StartEvent", SendMessageOptions.DontRequireReceiver);
            }
        }

        private bool PrerequisitesMet(CalendarEventData e)
        {
            var flags = GameManager.Instance.CurrentSave.storyFlags;
            return e.prerequisiteFlags.TrueForAll(flags.Contains);
        }
    }
}
