using System;

namespace WebApplication15.Models
{
    // Class representing a reminder for an appointment, as defined in the class diagram
    public class Reminder
    {
        // The specific time when the reminder should be triggered
        public DateTime ReminderTime { get; set; }

        // The message to display for the reminder
        public string Message { get; set; }
    }
}
