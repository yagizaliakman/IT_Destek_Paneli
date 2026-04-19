using System;

namespace IT_Destek_Panel.Models
{
    public class TicketMessage
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string MessageBody { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // OKUNDU BİLGİSİ
        public bool IsRead { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        public Ticket Ticket { get; set; }
        public User User { get; set; }
    }
}