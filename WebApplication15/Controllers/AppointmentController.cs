using Microsoft.AspNetCore.Mvc;
using WebApplication15.Models;
using WebApplication15.Services;

namespace WebApplication15.Controllers
{
    // Controllers/AppointmentController.cs
    public class AppointmentController : Controller
    {
        private readonly AppointmentService _service;

        // Dependency Injection
        public AppointmentController(AppointmentService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var appointments = _service.GetAllAppointments();
            return View(appointments);
        }

        [HttpGet]
        public IActionResult Create() => View(); // Add Appointment window

        [HttpPost]
        public IActionResult Create(Appointment model)
        {
            // Khối alt [invalid information]
            if (string.IsNullOrEmpty(model.Name) || (model.EndTime <= model.StartTime))
            {
                ModelState.AddModelError("", "Thông tin không hợp lệ (Tên trống hoặc Thời gian kết thúc phải lớn hơn bắt đầu)");
                return View(model);
            }

            // Khối [check appointment()] - Kiểm tra họp nhóm (Group Meeting) trước
            // Sơ đồ chuỗi: Tìm cuộc họp nhóm trùng tên và thời lượng
            var groupMeeting = _service.FindMatchingGroupMeeting(model.Name, model.StartTime, model.EndTime);
            if (groupMeeting != null)
            {
                // Truyền thông tin Group Meeting sang view để hiển thị Prompt
                ViewBag.ProposedAppointment = model; 
                return View("ConfirmGroupJoin", groupMeeting); 
            }

            // Logic [Check time()] - Xung đột cá nhân
            var conflict = _service.CheckConflict(model.StartTime, model.EndTime);
            if (conflict != null)
            {
                ViewBag.NewAppointment = model;
                return View("ConflictResolve", conflict); // Chuyển sang View xử lý xung đột
            }

            // Khối [No conflict] -> Add calendar()
            _service.AddAppointment(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Replace(int oldAppId, Appointment newApp)
        {
            // Xử lý Replace() từ sơ đồ: Thay thế appointment cũ bằng cái mới
            _service.DeleteOldAppointment(oldAppId);
            
            // Validate lại nếu cần (bỏ qua để code gọn)
            _service.AddAppointment(newApp);
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult JoinGroup(int meetingId)
        {
            // Logic AddParticipant()
            // Giả lập lấy current user
            var currentUser = "user123@example.com"; 
            _service.AddParticipantToGroup(meetingId, currentUser);
            
            return RedirectToAction(nameof(Index));
        }
    }
}
