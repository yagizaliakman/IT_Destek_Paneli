using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using IT_Destek_Panel.Models;
using IT_Destek_Panel.Constants; 
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;
using SixLabors.ImageSharp; 
using SixLabors.ImageSharp.Processing;

namespace IT_Destek_Panel.Controllers
{
    [Authorize] // Sadece giriş yapanlar girebilir
    public class TicketController : Controller
    {
        private readonly AppDbContext _context;

        public TicketController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Kullanıcının Kendi Biletlerini Listelediği Ana Sayfa
        public IActionResult Index()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));

            var tickets = _context.Tickets
                .Where(t => t.UserId == userId && t.IsDeleted == false)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            return View(tickets);
        }

        // 2. Yeni Bilet Açma Ekranını Getiren Metod (GET)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // 3. Kullanıcı Formu Doldurup "Gönder"e Bastığında Çalışan Metod (POST)
        [HttpPost]
        public async Task<IActionResult> Create(string title, string description, TicketPriority priority, IFormFile? attachment)
        {
            // --- GÜVENLİK DUVARI: EĞER BAŞLIK VEYA MESAJ BOŞSA ÇÖKME, GERİ YOLLA ---
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
            {
                ViewBag.Error = "Başlık ve mesaj alanları boş bırakılamaz, lütfen formu eksiksiz doldurun!";
                return View(); // Hata mesajıyla birlikte aynı sayfayı tekrar aç
            }
            // ----------------------------------------------------------------------
            var userId = int.Parse(User.FindFirstValue("UserId"));
            string? filePath = null;
            DateTime? attachmentDate = null; // Resim yüklenme tarihi

            // Eğer kullanıcı resim seçtiyse
            if (attachment != null && attachment.Length > 0)
            {
                // --- VİP 1: ANTİ-HACK KALKANI (UZANTI KONTROLÜ) ---
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                var extension = Path.GetExtension(attachment.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ViewBag.Error = "Güvenlik İhlali! Sadece .jpg, .png veya .pdf dosyaları yükleyebilirsiniz.";
                    return View(); 
                }
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(attachment.FileName);
                var exactPath = Path.Combine(uploadsFolder, uniqueFileName);

                // ImageSharp ile Resmi Boyutlandırma (Genişlik max 800px)
                using (var image = await Image.LoadAsync(attachment.OpenReadStream()))
                {
                    int newWidth = 800;
                    int newHeight = (int)((double)image.Height / image.Width * newWidth);

                    image.Mutate(x => x.Resize(newWidth, newHeight));
                    await image.SaveAsync(exactPath);
                }

                filePath = "/uploads/" + uniqueFileName;
                attachmentDate = DateTime.Now; // Tarihi atıyoruz
            }

            var newTicket = new Ticket
            {
                Title = title,
                Description = description,
                Priority = priority, // Artık Enum kullanıyoruz
                AttachmentPath = filePath,
                AttachmentDate = attachmentDate, // Resim tarihi
                UserId = userId,
                Status = TicketStatus.Açık, // Artık Enum kullanıyoruz
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.Tickets.Add(newTicket);
            _context.SaveChanges();

            // İşlem bitti, mesajı çantaya (TempData) koyuyoruz
            TempData["SuccessMessage"] = "Talebiniz başarıyla oluşturuldu. Teknik ekibimiz en kısa sürede inceleyecektir!";

            return RedirectToAction("Index"); // Sonra sayfayı yönlendir
        }

        // 4. Bilet Detayını ve Mesaj Geçmişini Getiren Metod (GET)
        [HttpGet]
        public IActionResult Details(int id)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var role = User.FindFirstValue(ClaimTypes.Role);

            var ticket = _context.Tickets
                .Include(t => t.User)
                .FirstOrDefault(t => t.Id == id && t.IsDeleted == false);

            if (ticket == null) return NotFound(SystemMessages.TicketNotFound);

            if (role != "Admin" && role != "2" && ticket.UserId != userId)
            {
                return Unauthorized(SystemMessages.UnauthorizedAccess);
            }

            var messages = _context.TicketMessages
                .Include(m => m.User)
                .Where(m => m.TicketId == id && m.IsDeleted == false)
                .OrderBy(m => m.CreatedAt)
                .ToList();

            // Karşı tarafın gönderdiği ve henüz okunmamış mesajları bul
            var unreadMessages = messages.Where(m => m.UserId != userId && m.IsRead == false).ToList();

            // Eğer okunmamış mesaj varsa, hepsini "Okundu (true)" yap ve veritabanına kaydet
            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                _context.SaveChanges();
            }

            var viewModel = new TicketDetailsViewModel
            {
                Ticket = ticket,
                Messages = messages
            };

            return View(viewModel);
        }

        // 5. Yeni Mesaj Gönderme ve Durum Güncelleme Metodu (POST)
        [HttpPost]
        public IActionResult AddMessage(int ticketId, string newMessage, TicketStatus? newStatus)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var role = User.FindFirstValue(ClaimTypes.Role);

            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == ticketId);
            if (ticket == null) return NotFound(SystemMessages.TicketNotFound);

            if (!string.IsNullOrWhiteSpace(newMessage))
            {
                var message = new TicketMessage
                {
                    TicketId = ticketId,
                    UserId = userId,
                    MessageBody = newMessage,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false
                };
                _context.TicketMessages.Add(message);
            }

            // Admin Otomasyonu
            if (role == "Admin" || role == "2")
            {
                if (newStatus.HasValue)
                {
                    ticket.Status = newStatus.Value;
                }
                else if (ticket.Status == TicketStatus.Açık && !string.IsNullOrWhiteSpace(newMessage))
                {
                    ticket.Status = TicketStatus.İşlemde;
                }
                // GÜVENLİK/BAŞARI MESAJI (ŞOV ZAMANI)
                TempData["SuccessMessage"] = "Talep detayı ve durumu başarıyla güncellendi!";
            }

            _context.SaveChanges();
            return RedirectToAction("Details", new { id = ticketId });
        }

        // 6. Kullanıcının Kendi Biletini Kapatması (Listeleme ekranındaki Kapat butonu)
        [HttpGet]
        public IActionResult CloseTicket(int id)
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == id && t.UserId == userId);

            if (ticket != null && ticket.Status != TicketStatus.Kapalı)
            {
                ticket.Status = TicketStatus.Kapalı;
                _context.SaveChanges();
                TempData["Success"] = SystemMessages.TicketClosed;
            }

            return RedirectToAction("Index");
        }
    }
}