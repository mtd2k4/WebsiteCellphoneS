using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;

namespace WebsiteBanDienThoai.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Notification/GetCustomerNotifications
        [HttpGet]
        public async Task<IActionResult> GetCustomerNotifications()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int uid))
            {
                return Json(new { success = false, unreadCount = 0, notifications = Array.Empty<object>() });
            }

            var query = _context.Notifications
                .Where(n => n.UserId == uid || (n.RecipientRole == "Customer" && n.UserId == null));

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
                    n.IsRead,
                    CreatedAt = n.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeAgo = GetTimeAgo(n.CreatedAt)
                })
                .ToListAsync();

            return Json(new { success = true, unreadCount, notifications = items });
        }

        // POST: /Notification/MarkRead
        [HttpPost]
        public async Task<IActionResult> MarkRead(int? id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int uid))
            {
                return Json(new { success = false });
            }

            if (id.HasValue && id.Value > 0)
            {
                var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id.Value && (n.UserId == uid || n.RecipientRole == "Customer"));
                if (notif != null)
                {
                    notif.IsRead = true;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var unreadList = await _context.Notifications
                    .Where(n => !n.IsRead && (n.UserId == uid || (n.RecipientRole == "Customer" && n.UserId == null)))
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
