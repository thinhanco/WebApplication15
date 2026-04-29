namespace WebApplication15.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<string> Reminders { get; set; } = new List<string>();
    }

    // Models/GroupMeeting.cs
    public class GroupMeeting : Appointment
    {
        public List<string> Participants { get; set; } = new List<string>();
    }
}