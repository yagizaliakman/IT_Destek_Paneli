namespace IT_Destek_Panel.Constants
{
    // static yapıyoruz ki her yerden direkt SystemMessages.XXX diye ulaşabilelim
    public static class SystemMessages
    {
        // BAŞARI MESAJLARI
        public const string TicketCreated = "Destek talebiniz başarıyla oluşturuldu.";
        public const string UserDeleted = "Kullanıcı sistemden başarıyla silindi (Soft Delete).";
        public const string TicketClosed = "Destek talebi başarıyla kapatıldı.";

        // HATA VE UYARI MESAJLARI
        public const string TicketNotFound = "Aradığınız bilet bulunamadı veya sistemden silinmiş.";
        public const string UnauthorizedAccess = "Güvenlik İhlali: Bu bileti görüntüleme veya işlem yapma yetkiniz yok!";
        public const string InvalidFile = "Lütfen geçerli formatta bir dosya yükleyin.";
        public const string SystemError = "Beklenmeyen bir sistem hatası oluştu, lütfen IT ile iletişime geçin.";
    }
}