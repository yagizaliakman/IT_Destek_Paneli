using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using IT_Destek_Panel.Models;
using System.Security.Claims;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.AspNetCore.SignalR; // SignalR için gerekli
using IT_Destek_Panel.Hubs; // Hub dosyanın yolu

namespace IT_Destek_Panel.Controllers
{
    public class TicketController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<TicketHub> _hubContext; // Telsiz merkezi

        // Constructor'a hem veritabanını hem de SignalR telsizini bağladık
        public TicketController(AppDbContext context, IHubContext<TicketHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // 1. Kullanıcının Kendi Biletlerini Listelediği Ana Sayfa
        public IActionResult Index()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            var tickets = _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Where(t => t.UserId == userId && t.IsDeleted == false)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            return View(tickets);
        }

        // 2. Yeni Bilet Ekranı (GET)
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.StatusList = new SelectList(_context.TicketStatuses.ToList(), "Id", "Name");
            ViewBag.PriorityList = new SelectList(_context.TicketPriorities.ToList(), "Id", "Name");
            return View();
        }

        // 3. Yeni Bilet Kaydetme (POST) - ImageSharp Presli
        [HttpPost]
        public async Task<IActionResult> Create(string title, string description, int priorityId, IFormFile? attachment)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
            {
                ViewBag.Error = "Başlık ve mesaj alanları boş bırakılamaz!";
                ViewBag.StatusList = new SelectList(_context.TicketStatuses.ToList(), "Id", "Name");
                ViewBag.PriorityList = new SelectList(_context.TicketPriorities.ToList(), "Id", "Name");
                return View();
            }

            var userId = int.Parse(User.FindFirstValue("UserId"));
            string? filePath = null;
            DateTime? attachmentDate = null;

            if (attachment != null && attachment.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                var extension = Path.GetExtension(attachment.FileName).ToLower();

                if (allowedExtensions.Contains(extension))
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(attachment.FileName);
                    var exactPath = Path.Combine(uploadsFolder, uniqueFileName);

                    if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
                    {
                        using (var image = await Image.LoadAsync(attachment.OpenReadStream()))
                        {
                            image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(1280, 720) }));
                            var encoder = new JpegEncoder { Quality = 75 };
                            await image.SaveAsync(exactPath, encoder);
                        }
                    }
                    else if (extension == ".pdf")
                    {
                        using (var stream = new FileStream(exactPath, FileMode.Create))
                        {
                            await attachment.CopyToAsync(stream);
                        }
                    }
                    filePath = "/uploads/" + uniqueFileName;
                    attachmentDate = DateTime.Now;
                }
            }

            var ticket = new Ticket
            {
                Title = title,
                Description = description,
                PriorityId = priorityId,
                StatusId = 1,
                UserId = userId,
                CreatedAt = DateTime.Now,
                AttachmentPath = filePath,
                AttachmentDate = attachmentDate
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = IT_Destek_Panel.Constants.SystemMessages.TicketCreated;
            return RedirectToAction("Index");
        }

        // 4. Bilet Detayı (Mavi Tik/Okundu Bilgisi Dahil)
        public IActionResult Details(int id)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var role = User.FindFirstValue(ClaimTypes.Role);

            var ticket = _context.Tickets
                .Include(t => t.User)
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .FirstOrDefault(t => t.Id == id && t.IsDeleted == false);

            if (ticket == null) return NotFound("Talep bulunamadı.");

            if (role != "Admin" && role != "2" && ticket.UserId != userId)
                return Unauthorized("Bu işleme yetkiniz yok.");

            var messages = _context.TicketMessages
                .Include(m => m.User)
                .Where(m => m.TicketId == id && m.IsDeleted == false)
                .OrderBy(m => m.CreatedAt)
                .ToList();

            var viewModel = new TicketDetailsViewModel { Ticket = ticket, Messages = messages };

            bool isUpdated = false;
            foreach (var msg in messages)
            {
                if (msg.UserId != userId && !msg.IsRead)
                {
                    msg.IsRead = true;
                    isUpdated = true;
                }
            }

            if (isUpdated) _context.SaveChanges();

            return View(viewModel);
        }

        // 5. Yeni Mesaj Gönderme (ANLIK SİNYAL DESTEKLİ)
        [HttpPost]
        public async Task<IActionResult> AddMessage(int ticketId, string newMessage, int? newStatusId)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var role = User.FindFirstValue(ClaimTypes.Role);
            var userName = User.Identity.Name;

            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(newMessage))
            {
                var message = new TicketMessage
                {
                    TicketId = ticketId,
                    UserId = userId,
                    MessageBody = newMessage,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false,
                    IsRead = false
                };
                _context.TicketMessages.Add(message);
            }

            if (role == "Admin" || role == "2")
            {
                if (newStatusId.HasValue) ticket.StatusId = newStatusId.Value;
                else if (ticket.StatusId == 1 && !string.IsNullOrWhiteSpace(newMessage)) ticket.StatusId = 2;
            }

            await _context.SaveChangesAsync();

            // SİNYALİ ÇAKIYORUZ: Sayfa yenilenmeden karşıya gitsin
            if (!string.IsNullOrWhiteSpace(newMessage))
            {
                await _hubContext.Clients.Group(ticketId.ToString()).SendAsync("ReceiveMessage",
                    userName,
                    newMessage,
                    DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                );
            }

            TempData["SuccessMessage"] = "İşleminiz başarıyla tamamlandı.";
            return RedirectToAction("Details", new { id = ticketId });
        }

        // 6. Bilet Kapatma
        public IActionResult CloseTicket(int id)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == id && t.UserId == userId);

            if (ticket != null)
            {
                ticket.StatusId = 3;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Bilet başarıyla kapatıldı.";
            }
            return RedirectToAction("Index");
        }
    }
}