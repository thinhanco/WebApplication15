using Microsoft.AspNetCore.Mvc;
using WebApplication15.Models;
using WebApplication15.Services;

namespace WebApplication15.Controllers
{
    /// <summary>
    /// Handles all HTTP requests related to calendar appointments.
    ///
    /// The POST Create action enforces the exact sequence mandated by the Use Case:
    ///   1. Validate input (empty name / negative duration)
    ///   2. Check for a matching group meeting (same name + duration)
    ///   3. Check for a personal time conflict
    ///   4. Add reminders → Save appointment
    /// </summary>
    public class AppointmentController : Controller
    {
        private readonly AppointmentService _service;

        public AppointmentController(AppointmentService service)
        {
            _service = service;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // INDEX — Show all appointments and group meetings
        // ─────────────────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Index()
            => View(_service.GetAllAppointments());

        // ─────────────────────────────────────────────────────────────────────────
        // CREATE — Show the "Add Appointment" form
        // Use Case: "The UI notices which part of the calendar is active and pops
        //            up an Add Appointment window for that date and time."
        // ─────────────────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Create(DateTime? prefillDate)
        {
            // Pre-fill start/end times from the calendar cell the user clicked (optional).
            var model = new Appointment
            {
                StartTime = prefillDate ?? DateTime.Today.AddHours(9),
                EndTime   = prefillDate?.AddHours(1) ?? DateTime.Today.AddHours(10)
            };
            return View(model);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // CREATE POST — Core use-case logic
        //
        // Sequence:
        //   [alt: invalid information]  → return form with errors
        //   [alt: duplicate info]       → ask user to join group meeting
        //   [alt: time conflict]        → ask user to choose other time or replace
        //   [no conflict]               → addReminder() → recordAppointment() → redirect
        // ─────────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Appointment model, List<int> selectedReminders)
        {
            // ── STEP 1 · Validate ────────────────────────────────────────────────
            // Data Annotations on the model handle [Required], [StringLength].
            // IValidatableObject.Validate() handles EndTime <= StartTime.
            // Use Case: "The UI will prevent the user from entering an appointment
            //            that has invalid information, such as an empty name or
            //            negative duration."
            if (!ModelState.IsValid)
            {
                // [alt: invalid information] — return to form
                return View(model);
            }

            // ── STEP 2 · Check for matching group meeting ────────────────────────
            // Use Case: "If the user enters an appointment with the same name and
            //            duration as an existing group meeting, the calendar asks
            //            the user whether he/she intended to join that group meeting."
            //
            // Checks: Name (case-insensitive) AND Duration are identical.
            var matchingGroup = _service.FindMatchingGroupMeeting(
                model.Name, model.StartTime, model.EndTime);

            if (matchingGroup != null)
            {
                // [alt: duplicate information] — ask whether to join the group meeting
                ViewBag.ProposedAppointment = model;
                return View("ConfirmGroupJoin", matchingGroup);
            }

            // ── STEP 3 · Check for personal time conflict ────────────────────────
            // Use Case: "If the user already has an appointment at that time, the
            //            user is shown a warning message and asked to choose an
            //            available time or replace the previous appointment."
            var conflictingAppointment = _service.CheckTimeConflict(
                model.StartTime, model.EndTime);

            if (conflictingAppointment != null)
            {
                // [alt: time conflict] — show warning; offer two branches:
                //   • "Choose other time"  → link back to Create (GET)
                //   • "Replace"            → POST to Replace action below
                ViewBag.NewAppointment = model;
                return View("ConflictResolve", conflictingAppointment);
            }

            // ── STEP 4 · Attach reminders ────────────────────────────────────────
            // Use Case: "Any reminder selected by the user is added to the list of
            //            reminders."
            // selectedReminders contains the MinutesBefore values chosen in the form.
            if (selectedReminders != null && selectedReminders.Count > 0)
            {
                foreach (var minutes in selectedReminders)
                {
                    model.Reminders.Add(new Reminder { MinutesBefore = minutes });
                }
            }

            // ── STEP 5 · Record the appointment ─────────────────────────────────
            // Use Case: "The calendar records the new appointment in the user's
            //            list of appointments."
            _service.AddAppointment(model);

            TempData["Success"] = $"Appointment \"{model.Name}\" has been successfully added.";
            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // REPLACE — User chose to overwrite the conflicting appointment
        // Use Case: "replace the previous appointment"
        // ─────────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Replace(int oldAppointmentId, Appointment newAppointment,
                                     List<int> selectedReminders)
        {
            // Re-attach reminders chosen before the conflict screen was shown.
            if (selectedReminders != null)
            {
                foreach (var minutes in selectedReminders)
                    newAppointment.Reminders.Add(new Reminder { MinutesBefore = minutes });
            }

            bool replaced = _service.ReplaceAppointment(oldAppointmentId, newAppointment);

            if (!replaced)
            {
                TempData["Error"] = "Could not replace the appointment. It may have already been removed.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = $"Appointment replaced with \"{newAppointment.Name}\" successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // CHOOSE OTHER TIME — User chose to pick a different slot
        // Use Case: "choose an available time" — simply returns to the Create form,
        // pre-filled with the data the user already entered.
        // ─────────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChooseOtherTime(Appointment proposedAppointment)
        {
            // Return to the Create form with the user's data still populated so
            // they only need to change the conflicting time fields.
            ModelState.Clear();  // Clear validation state so no stale errors are shown
            return View("Create", proposedAppointment);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // JOIN GROUP — User confirmed they want to join the matching group meeting
        // Use Case: "the user is added to that group meeting's list of participants."
        // ─────────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult JoinGroup(int meetingId)
        {
            // Identify the current user (use Identity username, fall back to placeholder)
            var currentUser = User.Identity?.Name ?? "guest@example.com";

            bool success = _service.AddParticipantToGroupMeeting(meetingId, currentUser);

            TempData[success ? "Success" : "Error"] = success
                ? "You have successfully joined the group meeting."
                : "Could not join the group meeting. It may no longer exist.";

            return RedirectToAction(nameof(Index));
        }
    }
}
