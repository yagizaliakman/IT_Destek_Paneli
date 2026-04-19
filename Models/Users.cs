namespace IT_Destek_Panel.Models
{
    // KULLANICI ROLLERİ İÇİN ENUM
    public enum UserRole { User = 1, Admin = 2 }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        // Artık string değil, Enum
        public UserRole Role { get; set; } = UserRole.User;

        public bool IsDeleted { get; set; } = false;
    }
}