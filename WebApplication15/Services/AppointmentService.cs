using System.Collections.Concurrent;
using WebApplication15.Models;

namespace WebApplication15.Services
{
    // Services/AppointmentService.cs
    public class AppointmentService
    {
        // Sử dụng ConcurrentBag thay vì List để đảm bảo Thread-safe trong môi trường Web đa luồng
        private readonly ConcurrentBag<Appointment> _appointments = new ConcurrentBag<Appointment>();
        private readonly ConcurrentBag<GroupMeeting> _groupMeetings = new ConcurrentBag<GroupMeeting>();

        public AppointmentService()
        {
            // Seed data cho GroupMeeting để có thể test
            _groupMeetings.Add(new GroupMeeting 
            { 
                Id = 100, 
                Name = "Họp dự án Alpha", 
                StartTime = DateTime.Today.AddHours(14), 
                EndTime = DateTime.Today.AddHours(16) 
            });
        }

        public IEnumerable<Appointment> GetAllAppointments()
        {
            return _appointments.ToList();
        }

        // Check time()
        public Appointment CheckConflict(DateTime start, DateTime end)
        {
            // Trả về cuộc hẹn đầu tiên bị trùng thời gian
            return _appointments.FirstOrDefault(a => start < a.EndTime && end > a.StartTime);
        }

        // check appointment() - Tìm cuộc họp nhóm trùng tên và thời lượng
        // Services/AppointmentService.cs
        public GroupMeeting FindMatchingGroupMeeting(string name, DateTime start, DateTime end)
        {
            var duration = end - start;
            return _groupMeetings.FirstOrDefault(g =>
                g.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                g.StartTime == start && // Khớp chính xác giờ bắt đầu
                (g.EndTime - g.StartTime) == duration); // Khớp độ dài cuộc họp
        }

        public void AddAppointment(Appointment app) 
        {
            if (app.Id == 0) app.Id = new Random().Next(1, 10000);
            _appointments.Add(app);
        }

        public void ReplaceAppointment(Appointment oldApp, Appointment newApp)
        {
            DeleteOldAppointment(oldApp.Id);
            AddAppointment(newApp);
        }

        // Phương thức thêm Reminder vào Appointment, tương ứng với "addReminderToList(reminderInfo)" trong Sequence Diagram
        public bool AddReminderToAppointment(int appointmentId, Reminder reminder)
        {
            var appointment = _appointments.FirstOrDefault(a => a.Id == appointmentId);
            if (appointment != null)
            {
                appointment.AddReminder(reminder);
                return true;
            }
            return false;
        }

        public void DeleteOldAppointment(int oldAppId)
        {
            // Xóa phần tử khỏi ConcurrentBag hơi phức tạp một chút do ConcurrentBag tối ưu cho việc thêm
            // Cách đơn giản để mô phỏng là giữ lại các phần tử không bị xóa
            var itemToRemove = _appointments.FirstOrDefault(x => x.Id == oldAppId);
            if (itemToRemove != null)
            {
                // Đưa item ra khỏi bag (Trong thực tế nên dùng ConcurrentDictionary nếu cần xóa/cập nhật theo ID)
                // Tuy nhiên với Prototype này có thể chấp nhận cách này hoặc cấu trúc lại.
                // Để đơn giản, ta sẽ clear và add lại (không khuyến khích thực tế) 
                // hoặc sử dụng thủ thuật TryTake.
                
                // Thủ thuật cho ConcurrentBag (Lưu ý: chỉ áp dụng cho dữ liệu nhỏ)
                List<Appointment> temp = new List<Appointment>();
                while (_appointments.TryTake(out var item))
                {
                    if (item.Id != oldAppId) temp.Add(item);
                }
                foreach (var item in temp) _appointments.Add(item);
            }
        }
        public IEnumerable<GroupMeeting> GetAllGroupMeetings()
        {
            return _groupMeetings.ToList();
        }
        public bool AddParticipantToGroup(int meetingId, string user) 
        {
            var meeting = _groupMeetings.FirstOrDefault(m => m.Id == meetingId);
            if (meeting != null)
            {
                // Tránh lỗi khi danh sách null
                if (meeting.Participants == null) meeting.Participants = new List<string>();
                if (!meeting.Participants.Contains(user))
                {
                    meeting.Participants.Add(user);
                }
                return true;
            }
            return false;
        }
    }
}
