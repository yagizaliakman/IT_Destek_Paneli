using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using IT_Destek_Panel.Models;
using IT_Destek_Panel.Constants;
using ClosedXML.Excel;

namespace IT_Destek_Panel.Controllers
{
    [Authorize(Roles = "Admin, 2")] // Enum Role 2 Admin demek
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var tickets = _context.Tickets.Include(t => t.User).Where(t => t.IsDeleted == false).ToList();

            var viewModel = new AdminDashboardViewModel
            {
                TotalTickets = tickets.Count,
                // "Açık", "İşlemde", "Kapalı" yerine Enum çağırdık
                OpenTickets = tickets.Count(t => t.Status == TicketStatus.Açık),
                InProgressTickets = tickets.Count(t => t.Status == TicketStatus.İşlemde),
                ClosedTickets = tickets.Count(t => t.Status == TicketStatus.Kapalı),
                Tickets = tickets.OrderByDescending(t => t.CreatedAt).ToList()
            };

            return View(viewModel);
        }

        public IActionResult Users()
        {
            var users = _context.Users.Where(u => u.IsDeleted == false).ToList();
            return View(users);
        }

        public IActionResult DeleteTicket(int id)
        {
            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket != null)
            {
                ticket.IsDeleted = true;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                user.IsDeleted = true;
                _context.SaveChanges();
                TempData["Success"] = SystemMessages.UserDeleted;
            }
            return RedirectToAction("Users");
        }

        public IActionResult ExportToExcel()
        {
            // 1. Veritabanındaki silinmemiş tüm talepleri çekiyoruz
            var tickets = _context.Tickets.Where(t => t.IsDeleted == false).ToList();

            using (var workbook = new XLWorkbook())
            {
                // 2. Excel sayfasını oluşturuyoruz
                var worksheet = workbook.Worksheets.Add("Destek Talepleri");

                // 3. Başlıklar
                worksheet.Cell(1, 1).Value = "Talep ID";
                worksheet.Cell(1, 2).Value = "Başlık";
                worksheet.Cell(1, 3).Value = "Aciliyet";
                worksheet.Cell(1, 4).Value = "Durum";
                worksheet.Cell(1, 5).Value = "Oluşturma Tarihi";

                // Başlıkları kalın
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#2c3e50");
                headerRow.Style.Font.FontColor = XLColor.White;

                // 4. Verileri Satır Satır İşliyoruz
                int row = 2;
                foreach (var item in tickets)
                {
                    worksheet.Cell(row, 1).Value = item.Id;
                    worksheet.Cell(row, 2).Value = item.Title;
                    worksheet.Cell(row, 3).Value = item.Priority.ToString();
                    worksheet.Cell(row, 4).Value = item.Status.ToString();
                    worksheet.Cell(row, 5).Value = item.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                    row++;
                }

                // Sütun genişliklerini içeriğe göre otomatik ayarla
                worksheet.Columns().AdjustToContents();

                // 5. Dosyayı Stream olarak kullanıcıya fırlatıyoruz
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Destek_Talepleri_Raporu_" + DateTime.Now.ToString("ddMMyyyy") + ".xlsx"
                    );
                }
            }
        }
    }
}
           
        
    