using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;

namespace WebsiteBanDienThoai.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Notification/GetAdminNotifications
        [HttpGet]
        public async Task<IActionResult> GetAdminNotifications()
        {
            var query = _context.Notifications
                .Where(n => n.RecipientRole == "Admin");

            var unreadCount = await query.CountAsync(n => !n.IsRead);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(15)
                .Select(n => new
                {
                    n.Id,
                    Type = (int)n.Type,
                    n.Title,
                    n.Message,
                    n.TargetUrl,
                    n.OrderCode,
                    n.OrderId,
                    n.IsRead,
                    CreatedAt = n.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeAgo = GetTimeAgo(n.CreatedAt)
                })
                .ToListAsync();

            var latestId = items.FirstOrDefault()?.Id ?? 0;

            return Json(new { success = true, unreadCount, latestId, notifications = items });
        }

        // POST: /Admin/Notification/MarkRead
        [HttpPost]
        public async Task<IActionResult> MarkRead(int? id)
        {
            if (id.HasValue && id.Value > 0)
            {
                var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id.Value && n.RecipientRole == "Admin");
                if (notif != null)
                {
                    notif.IsRead = true;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var unreadList = await _context.Notifications
                    .Where(n => !n.IsRead && n.RecipientRole == "Admin")
                    .ToListAsync();

                foreach (var item in unreadList)
                {
                    item.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        private static string GetTimeAgo(DateTime dt)
        {
            var span = DateTime.UtcNow - dt;
            if (span.TotalMinutes < 1) return "Vừa xong";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} ngày trước";
            return dt.ToString("dd/MM/yyyy HH:mm");
        }
    }
}
