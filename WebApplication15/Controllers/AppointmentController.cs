using Microsoft.AspNetCore.Mvc;
using WebApplication15.Models;
using WebApplication15.Services;

namespace WebApplication15.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly AppointmentService _service;

        public AppointmentController(AppointmentService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult Index() => View(_service.GetAllAppointments());

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(Appointment model)
        {
            // --- 1. Khối [alt: invalid information] ---
            if (string.IsNullOrEmpty(model.Name) || (model.EndTime <= model.StartTime))
            {
                ModelState.AddModelError("", "Thông tin không hợp lệ.");
                return View(model);
            }

            // --- 2. Nhánh [No conflict] -> Check time() TRƯỚC ---
            // Truy cập vào Calendar (Service) để kiểm tra xung đột cá nhân
            var conflict = _service.CheckConflict(model.StartTime, model.EndTime);
            if (conflict != null)
            {
                // Khối [alt: time conflict] 
                // Trả về View để User chọn "Choose other time or replace"
                ViewBag.NewAppointment = model;
                return View("ConflictResolve", conflict);
            }

            // --- 3. Nhánh [No conflict] (Cá nhân rảnh) -> check appointment() ---
            // (Thiết kế mới): Truy cập trực tiếp vào danh sách Group Meeting để kiểm tra trùng
            var groupMeeting = _service.FindMatchingGroupMeeting(model.Name, model.StartTime, model.EndTime);
            if (groupMeeting != null)
            {
                // Khối [alt: duplicate information]
                // Trả về dữ liệu existed appointment (GroupMeeting)
                ViewBag.ProposedAppointment = model;
                return View("ConfirmGroupJoin", groupMeeting);
            }

            // --- 4. Khối [alt: No conflict] cuối cùng ---
            _service.AddAppointment(model);
            TempData["Success"] = "Add calendar successfully";
            return RedirectToAction(nameof(Index));
        }

        // Action xử lý nút "Replace" từ trang ConflictResolve
        [HttpPost]
        public IActionResult Replace(int oldAppId, Appointment newApp)
        {
            // Nhánh [replace]: Xóa cái cũ, thêm cái mới
            _service.DeleteOldAppointment(oldAppId);
            _service.AddAppointment(newApp);

            return RedirectToAction(nameof(Index));
        }

        // Action xử lý nút "Confirm" từ trang ConfirmGroupJoin
       
        [HttpPost]
        public IActionResult JoinGroup(int meetingId)
        {
            // 1. Lấy thông tin user (giả lập hoặc từ Identity)
            var currentUser = User.Identity.Name ?? "user@example.com";

            // 2. Gọi service xử lý logic nghiệp vụ
            bool success = _service.AddParticipantToGroup(meetingId, currentUser);

            if (success)
            {
                TempData["Success"] = "Bạn đã tham gia cuộc họp nhóm thành công!";
            }
            else
            {
                TempData["Error"] = "Không thể tham gia cuộc họp nhóm này.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
