namespace WebApplication15.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        // Danh sách các lời nhắc (Reminder) cho cuộc hẹn này (Dựa trên class diagram có quan hệ 1..0..*)
        public List<Reminder> Reminders { get; set; } = new List<Reminder>();

        // Phương thức thêm lời nhắc (như trong class diagram: +addReminder(reminder: Reminder) : void)
        public void AddReminder(Reminder reminder)
        {
            if (reminder != null)
            {
                Reminders.Add(reminder);
            }
        }
    }

}