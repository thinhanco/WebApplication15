using Microsoft.AspNetCore.Mvc;
using WebApplication15.Models;
using WebApplication15.Services;

namespace WebApplication15.Controllers
{
    // Controllers/AppointmentController.cs
    public class AppointmentController : Controller
    {
        private readonly AppointmentService _service = new AppointmentService();

        [HttpGet]
        public IActionResult Create() => View(); // Add Appointment window

        [HttpPost]
        public IActionResult Create(Appointment model)
        {
            // Khối alt [invalid information]
            if (string.IsNullOrEmpty(model.Name) || (model.EndTime <= model.StartTime))
            {
                ModelState.AddModelError("", "Invalid information (Tên trống hoặc thời gian âm)");
                return View(model);
            }

            // Logic [Check time()]
            var conflict = _service.CheckConflict(model.StartTime, model.EndTime);
            if (conflict != null)
            {
                TempData["ConflictId"] = conflict.Id;
                return View("ConflictResolve", model); // Chuyển sang View xử lý xung đột
            }

            // Logic [check appointment()]
            var groupMeeting = _service.FindMatchingGroupMeeting(model.Name, model.StartTime, model.EndTime);
            if (groupMeeting != null)
            {
                return View("ConfirmGroupJoin", groupMeeting); // Hỏi Join group meeting?
            }

            // Khối [No conflict] -> Add calendar()
            _service.AddAppointment(model);
            return Content("Successful");
        }

        [HttpPost]
        public IActionResult Replace(Appointment newApp)
        {
            // Xử lý Replace() từ sơ đồ
            // (Logic tìm app cũ và thay thế bằng app mới)
            return Content("Successful (Replaced)");
        }
    }
}
