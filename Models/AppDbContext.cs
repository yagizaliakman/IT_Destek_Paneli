using Microsoft.EntityFrameworkCore;

namespace IT_Destek_Panel.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // C# modellerimizi SQL tablolarımızla eşleştiriyoruz
        public DbSet<User> Users { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketMessage> TicketMessages { get; set; }
    }
}