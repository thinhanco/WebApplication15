using System.Collections.Generic;

namespace WebApplication15.Models
{
    public class GroupMeeting : Appointment
    {
        public List<string> Participants { get; set; } = new List<string>();
    }
}
