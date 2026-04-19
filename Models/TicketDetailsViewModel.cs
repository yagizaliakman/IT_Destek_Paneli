using System.Collections.Generic;

namespace IT_Destek_Panel.Models
{
    // Bu sınıf veritabanında tablo oluşturmaz! 
    // Sadece View(Arayüz) sayfasına birden fazla veriyi tek paket halinde taşımak için kurye görevi görür.
    public class TicketDetailsViewModel
    {
        public Ticket Ticket { get; set; }
        public List<TicketMessage> Messages { get; set; }
        public string NewMessage { get; set; }
        public string NewStatus { get; set; } // Admin biletin durumunu (Açık, İşlemde, Kapalı) değiştirebilsin diye.
    }
}