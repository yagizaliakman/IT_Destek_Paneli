using System.Collections.Generic;

namespace IT_Destek_Panel.Models
{
    public class AdminDashboardViewModel
    {
        public List<Ticket> Tickets { get; set; }
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int ClosedTickets { get; set; }
    }
}