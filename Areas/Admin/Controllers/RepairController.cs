using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RepairController : Controller
    {
        private readonly AppDbContext _context;

        public RepairController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Repair
        public async Task<IActionResult> Index(string? search, RepairStatus? status, int page = 1)
        {
            var query = _context.RepairBookings
                .Include(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(r => r.BookingCode.ToLower().Contains(s) ||
                                         r.CustomerName.ToLower().Contains(s) ||
                                         r.PhoneNumber.Contains(s) ||
                                         r.DeviceName.ToLower().Contains(s) ||
                                         (r.SerialNumber != null && r.SerialNumber.ToLower().Contains(s)));
            }

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            var totalItems = await query.CountAsync();
            var pageSize = 15;
            var bookings = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.TotalItems = totalItems;

            // Thống kê nhanh
            ViewBag.TotalBookings = await _context.RepairBookings.CountAsync();
            ViewBag.PendingCount = await _context.RepairBookings.CountAsync(r => r.Status == RepairStatus.Pending);
            ViewBag.RepairingCount = await _context.RepairBookings.CountAsync(r => r.Status == RepairStatus.Repairing || r.Status == RepairStatus.Received);
            ViewBag.ReadyCount = await _context.RepairBookings.CountAsync(r => r.Status == RepairStatus.ReadyForPickup);
            ViewBag.CompletedCount = await _context.RepairBookings.CountAsync(r => r.Status == RepairStatus.Completed);

            return View(bookings);
        }

        // GET: /Admin/Repair/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.RepairBookings
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: /Admin/Repair/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, RepairStatus status, string? technicianNote, decimal? finalCost)
        {
            var booking = await _context.RepairBookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = status;
            if (!string.IsNullOrWhiteSpace(technicianNote))
            {
                booking.TechnicianNote = technicianNote.Trim();
            }
            if (finalCost.HasValue && finalCost.Value >= 0)
            {
                booking.EstimatedCost = finalCost.Value;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái phiếu {booking.BookingCode} thành công!";

            return RedirectToAction(nameof(Details), new { id = booking.Id });
        }
    }
}
