using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Controllers
{
    public class WarrantyController : Controller
    {
        private readonly AppDbContext _context;

        public WarrantyController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Warranty or /Warranty/Lookup
        public async Task<IActionResult> Index(string? keyword)
        {
            return await Lookup(keyword);
        }

        // GET: /Warranty/Lookup?keyword=...
        public async Task<IActionResult> Lookup(string? keyword)
        {
            ViewBag.Keyword = keyword;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                // Nếu người dùng đã đăng nhập, tự động lấy các phiếu bảo hành của họ
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    var identifier = User.Identity.Name;
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == identifier || u.Email == identifier);
                    if (user != null)
                    {
                        var userWarranties = await _context.WarrantyCards
                            .Include(w => w.Product)
                            .Where(w => w.UserId == user.Id || w.PhoneNumber == user.PhoneNumber || (user.Email != null && w.Email == user.Email))
                            .OrderByDescending(w => w.PurchaseDate)
                            .ToListAsync();

                        return View("Lookup", userWarranties);
                    }
                }
                return View("Lookup", new System.Collections.Generic.List<WarrantyCard>());
            }

            var k = keyword.Trim().ToLower();
            var results = await _context.WarrantyCards
                .Include(w => w.Product)
                .Where(w => w.SerialNumber.ToLower().Contains(k) ||
                            w.WarrantyCode.ToLower().Contains(k) ||
                            w.PhoneNumber.Contains(k) ||
                            (w.OrderCode != null && w.OrderCode.ToLower().Contains(k)) ||
                            w.ProductName.ToLower().Contains(k))
                .OrderByDescending(w => w.PurchaseDate)
                .ToListAsync();

            return View("Lookup", results);
        }

        // GET: /Warranty/Certificate?code=BH-20241015-0012
        public async Task<IActionResult> Certificate(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return RedirectToAction(nameof(Lookup));
            }

            var card = await _context.WarrantyCards
                .Include(w => w.Product)
                .Include(w => w.Order)
                .Include(w => w.User)
                .FirstOrDefaultAsync(w => w.WarrantyCode == code.Trim() || w.SerialNumber == code.Trim());

            if (card == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy Phiếu bảo hành điện tử với mã hoặc số IMEI đã cung cấp.";
                return RedirectToAction(nameof(Lookup), new { keyword = code });
            }

            // Lấy lịch sử sửa chữa liên quan (nếu có)
            var repairHistory = await _context.RepairBookings
                .Where(r => (r.SerialNumber != null && r.SerialNumber == card.SerialNumber) || 
                            (card.OrderCode != null && r.OrderCode == card.OrderCode))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.RepairHistory = repairHistory;
            return View(card);
        }
    }
}
