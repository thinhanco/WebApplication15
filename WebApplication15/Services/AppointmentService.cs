using WebApplication15.Models;

namespace WebApplication15.Services
{
    // Services/AppointmentService.cs
    public class AppointmentService
    {
        // Giả lập cơ sở dữ liệu
        private static List<Appointment> _appointments = new List<Appointment>();
        private static List<GroupMeeting> _groupMeetings = new List<GroupMeeting>();

        // Check time()
        public Appointment CheckConflict(DateTime start, DateTime end)
        {
            return _appointments.FirstOrDefault(a => start < a.EndTime && end > a.StartTime);
        }

        // check appointment() - Tìm cuộc họp nhóm trùng tên và thời lượng
        public GroupMeeting FindMatchingGroupMeeting(string name, DateTime start, DateTime end)
        {
            var duration = end - start;
            return _groupMeetings.FirstOrDefault(g => g.Name == name && (g.EndTime - g.StartTime) == duration);
        }

        public void AddAppointment(Appointment app) => _appointments.Add(app);

        public void ReplaceAppointment(Appointment oldApp, Appointment newApp)
        {
            _appointments.Remove(oldApp);
            _appointments.Add(newApp);
        }

        public void AddParticipantToGroup(GroupMeeting meeting, string user) => meeting.Participants.Add(user);
    }
}
