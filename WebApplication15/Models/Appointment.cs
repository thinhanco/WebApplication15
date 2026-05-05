using System.ComponentModel.DataAnnotations;

namespace WebApplication15.Models
{
    /// <summary>
    /// Represents a calendar appointment created by the user.
    /// Maps directly to the nouns in the Use Case:
    /// "The user enters the necessary information about the appointment's name,
    ///  location, start and end times."
    /// </summary>
    public class Appointment : IValidatableObject
    {
        public int Id { get; set; }

        // ── Core fields (Use Case: "appointment's name, location, start and end times") ──

        /// <summary>
        /// The name/title of the appointment.
        /// Use Case constraint: "The UI will prevent the user from entering an appointment
        /// that has invalid information, such as an empty name."
        /// </summary>
        [Required(ErrorMessage = "Appointment name is required.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters.")]
        [Display(Name = "Appointment Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Location where the appointment takes place.</summary>
        [StringLength(300)]
        [Display(Name = "Location")]
        public string? Location { get; set; }

        /// <summary>Date and time the appointment begins.</summary>
        [Required(ErrorMessage = "Start time is required.")]
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }

        /// <summary>Date and time the appointment ends.</summary>
        [Required(ErrorMessage = "End time is required.")]
        [Display(Name = "End Time")]
        public DateTime EndTime { get; set; }

        // ── Reminder collection ──

        /// <summary>
        /// Reminders the user selected for this appointment.
        /// Use Case: "Any reminder selected by the user is added to the list of reminders."
        /// </summary>
        public List<Reminder> Reminders { get; set; } = new();

        // ── Cross-field validation (Data Annotations cannot span two fields) ──

        /// <summary>
        /// Use Case constraint: "negative duration" → EndTime must be after StartTime.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndTime <= StartTime)
            {
                yield return new ValidationResult(
                    "End time must be after start time (duration must be positive).",
                    new[] { nameof(EndTime) });
            }
        }

        /// <summary>Calculated duration of the appointment (derived, not stored).</summary>
        public TimeSpan Duration => EndTime - StartTime;
    }

    /// <summary>
    /// A special type of appointment that has a list of participants.
    /// Use Case: "the user is added to that group meeting's list of participants."
    /// </summary>
    public class GroupMeeting : Appointment
    {
        /// <summary>Usernames / email addresses of all participants.</summary>
        public List<string> Participants { get; set; } = new();
    }
}