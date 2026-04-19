using System;

namespace IT_Destek_Panel.Models
{
    // ENUM TANIMLAMALARI
    public enum TicketPriority { Düşük = 1, Normal = 2, Yüksek = 3, Kritik = 4 }
    public enum TicketStatus { Açık = 1, İşlemde = 2, Kapalı = 3 }

    public class Ticket
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        // Artık string değil, Enum kullanıyoruz
        public TicketStatus Status { get; set; } = TicketStatus.Açık;
        public TicketPriority Priority { get; set; } = TicketPriority.Normal;

        public string? AttachmentPath { get; set; }

        //  RESİM YÜKLENME TARİHİ
        public DateTime? AttachmentDate { get; set; }

        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; } = false;
        public User User { get; set; }
    }
}