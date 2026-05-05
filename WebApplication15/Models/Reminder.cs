using System.ComponentModel.DataAnnotations;

namespace WebApplication15.Models
{
    /// <summary>
    /// Represents a reminder associated with an appointment.
    /// Maps to: "Any reminder selected by the user is added to the list of reminders." (Use Case)
    /// </summary>
    public class Reminder
    {
        public int Id { get; set; }

        /// <summary>
        /// How many minutes before the appointment to trigger the reminder (e.g. 10, 30, 60).
        /// </summary>
        [Required(ErrorMessage = "Reminder offset is required.")]
        [Range(0, 10080, ErrorMessage = "Reminder must be between 0 and 10080 minutes (1 week).")]
        [Display(Name = "Remind me (minutes before)")]
        public int MinutesBefore { get; set; }

        /// <summary>
        /// Human-readable label shown in the UI (e.g. "10 minutes", "1 hour").
        /// </summary>
        [Display(Name = "Reminder Label")]
        public string Label => MinutesBefore switch
        {
            0    => "At time of event",
            5    => "5 minutes before",
            10   => "10 minutes before",
            15   => "15 minutes before",
            30   => "30 minutes before",
            60   => "1 hour before",
            120  => "2 hours before",
            1440 => "1 day before",
            _    => $"{MinutesBefore} minutes before"
        };
    }
}
