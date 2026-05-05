using System.Collections.Concurrent;
using WebApplication15.Models;

namespace WebApplication15.Services
{
    /// <summary>
    /// In-memory service that manages appointments and group meetings.
    ///
    /// Refactored from ConcurrentBag (which has no efficient remove) to
    /// ConcurrentDictionary&lt;int, T&gt; for O(1) lookup, update, and delete —
    /// the standard pattern for thread-safe in-memory CRUD in ASP.NET Core.
    /// </summary>
    public class AppointmentService
    {
        // ── Storage (keyed by Id for O(1) CRUD) ──────────────────────────────────
        private readonly ConcurrentDictionary<int, Appointment> _appointments
            = new();
        private readonly ConcurrentDictionary<int, GroupMeeting> _groupMeetings
            = new();

        // Simple thread-safe ID generator (avoids Random collisions under concurrency)
        private int _nextId = 1;
        private int GetNextId() => Interlocked.Increment(ref _nextId);

        // ── Constructor: seed one group meeting for demo / testing ─────────────
        public AppointmentService()
        {
            var seed = new GroupMeeting
            {
                Id          = GetNextId(),
                Name        = "Alpha Project Meeting",       // English name, maps to Use Case noun
                Location    = "Conference Room A",
                StartTime   = DateTime.Today.AddHours(14),  // 14:00 today
                EndTime     = DateTime.Today.AddHours(16),  // 16:00 today (2-hour duration)
                Participants = new List<string> { "alice@example.com", "bob@example.com" }
            };
            _groupMeetings[seed.Id] = seed;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // READ
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>Returns all personal appointments ordered by start time.</summary>
        public IEnumerable<Appointment> GetAllAppointments()
            => _appointments.Values.OrderBy(a => a.StartTime);

        /// <summary>Returns a single appointment by ID, or null if not found.</summary>
        public Appointment? GetAppointmentById(int id)
            => _appointments.TryGetValue(id, out var appt) ? appt : null;

        /// <summary>Returns all group meetings ordered by start time.</summary>
        public IEnumerable<GroupMeeting> GetAllGroupMeetings()
            => _groupMeetings.Values.OrderBy(g => g.StartTime);

        // ─────────────────────────────────────────────────────────────────────────
        // TIME CONFLICT CHECK
        // Use Case: "If the user already has an appointment at that time,
        //            the user is shown a warning message."
        // Two intervals overlap when: newStart < existing.End AND newEnd > existing.Start
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Checks whether any existing personal appointment overlaps with [start, end).
        /// Returns the conflicting appointment, or null if the time slot is free.
        /// </summary>
        public Appointment? CheckTimeConflict(DateTime start, DateTime end)
            => _appointments.Values
                .FirstOrDefault(a => start < a.EndTime && end > a.StartTime);

        // ─────────────────────────────────────────────────────────────────────────
        // GROUP MEETING MATCH
        // Use Case: "same name AND duration" — EXACT equality on both criteria.
        // Duration = EndTime - StartTime (not start time itself, per the spec).
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Finds a group meeting whose Name equals <paramref name="name"/> (case-insensitive)
        /// AND whose Duration equals the duration implied by [start, end).
        ///
        /// Strictly implements the Use Case: "same name and duration as an existing group meeting".
        /// Does NOT require the start times to match.
        /// </summary>
        public GroupMeeting? FindMatchingGroupMeeting(string name, DateTime start, DateTime end)
        {
            var proposedDuration = end - start;
            return _groupMeetings.Values.FirstOrDefault(g =>
                g.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                (g.EndTime - g.StartTime) == proposedDuration);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // WRITE — Appointments
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Saves a new appointment (including any reminders the user selected).
        /// Use Case: "The calendar records the new appointment in the user's list of appointments.
        ///            Any reminder selected by the user is added to the list of reminders."
        ///
        /// Reminders are already attached to <paramref name="appointment"/> by the controller
        /// before calling this method, so they are persisted as part of the entity.
        /// </summary>
        public Appointment AddAppointment(Appointment appointment)
        {
            appointment.Id = GetNextId();
            _appointments[appointment.Id] = appointment;
            return appointment;
        }

        /// <summary>
        /// Replaces an existing appointment with a new one.
        /// Use Case: "replace the previous appointment"
        /// </summary>
        public bool ReplaceAppointment(int oldAppointmentId, Appointment newAppointment)
        {
            // Remove old entry atomically
            if (!_appointments.TryRemove(oldAppointmentId, out _))
                return false;

            // Save new entry (assign a fresh ID)
            newAppointment.Id = GetNextId();
            _appointments[newAppointment.Id] = newAppointment;
            return true;
        }

        /// <summary>Removes an appointment by its ID. Returns true if found and removed.</summary>
        public bool DeleteAppointment(int id)
            => _appointments.TryRemove(id, out _);

        // ─────────────────────────────────────────────────────────────────────────
        // WRITE — Group Meetings
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds the user to the participants list of a group meeting.
        /// Use Case: "the user is added to that group meeting's list of participants."
        /// Returns true on success, false if the meeting was not found.
        /// </summary>
        public bool AddParticipantToGroupMeeting(int meetingId, string username)
        {
            if (!_groupMeetings.TryGetValue(meetingId, out var meeting))
                return false;

            // Guard against duplicate entries
            if (!meeting.Participants.Contains(username, StringComparer.OrdinalIgnoreCase))
                meeting.Participants.Add(username);

            return true;
        }
    }
}
