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
    public class TradeInController : Controller
    {
        private readonly AppDbContext _context;

        public TradeInController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/TradeIn
        public async Task<IActionResult> Index(string? search, int? status, int page = 1)
        {
            var query = _context.TradeInRequests
                .Include(r => r.TargetProduct)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(r => r.RequestCode.ToLower().Contains(s) ||
                                         r.CustomerName.ToLower().Contains(s) ||
                                         r.PhoneNumber.Contains(s) ||
                                         r.OldDeviceName.ToLower().Contains(s));
            }

            if (status.HasValue && status.Value >= 0)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            var totalItems = await query.CountAsync();
            var pageSize = 15;
            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.TotalItems = totalItems;

            return View(requests);
        }

        // GET: /Admin/TradeIn/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.TradeInRequests
                .Include(r => r.TargetProduct)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        // POST: /Admin/TradeIn/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, int status)
        {
            var request = await _context.TradeInRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            request.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái phiếu {request.RequestCode} thành công!";
            return RedirectToAction(nameof(Details), new { id = request.Id });
        }

        // POST: /Admin/TradeIn/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.TradeInRequests.FindAsync(id);
            if (request != null)
            {
                _context.TradeInRequests.Remove(request);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa phiếu thu cũ thành công.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
