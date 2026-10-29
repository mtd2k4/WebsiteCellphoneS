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
    public class WarrantyController : Controller
    {
        private readonly AppDbContext _context;

        public WarrantyController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Warranty
        public async Task<IActionResult> Index(string? search, WarrantyCardStatus? status, int page = 1)
        {
            var query = _context.WarrantyCards
                .Include(w => w.Product)
                .Include(w => w.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(w => w.WarrantyCode.ToLower().Contains(s) ||
                                         w.SerialNumber.ToLower().Contains(s) ||
                                         w.CustomerName.ToLower().Contains(s) ||
                                         w.PhoneNumber.Contains(s) ||
                                         w.ProductName.ToLower().Contains(s) ||
                                         (w.OrderCode != null && w.OrderCode.ToLower().Contains(s)));
            }

            if (status.HasValue)
            {
                if (status.Value == WarrantyCardStatus.Active)
                {
                    query = query.Where(w => w.Status == WarrantyCardStatus.Active && w.EndDate >= DateTime.UtcNow);
                }
                else if (status.Value == WarrantyCardStatus.Expired)
                {
                    query = query.Where(w => w.Status == WarrantyCardStatus.Expired || (w.Status == WarrantyCardStatus.Active && w.EndDate < DateTime.UtcNow));
                }
                else
                {
                    query = query.Where(w => w.Status == status.Value);
                }
            }

            var totalItems = await query.CountAsync();
            var pageSize = 15;
            var items = await query
                .OrderByDescending(w => w.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.TotalItems = totalItems;

            // Thống kê nhanh
            ViewBag.TotalWarranties = await _context.WarrantyCards.CountAsync();
            ViewBag.ActiveCount = await _context.WarrantyCards.CountAsync(w => w.Status == WarrantyCardStatus.Active && w.EndDate >= DateTime.UtcNow);
            ViewBag.ExpiringSoonCount = await _context.WarrantyCards.CountAsync(w => w.Status == WarrantyCardStatus.Active && w.EndDate >= DateTime.UtcNow && w.EndDate <= DateTime.UtcNow.AddDays(30));
            ViewBag.ExpiredCount = await _context.WarrantyCards.CountAsync(w => w.Status == WarrantyCardStatus.Expired || (w.Status == WarrantyCardStatus.Active && w.EndDate < DateTime.UtcNow));
            ViewBag.VoidCount = await _context.WarrantyCards.CountAsync(w => w.Status == WarrantyCardStatus.Void);

            return View(items);
        }

        // GET: /Admin/Warranty/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var card = await _context.WarrantyCards
                .Include(w => w.Product)
                .Include(w => w.Order)
                .Include(w => w.User)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (card == null)
            {
                return NotFound();
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

        // GET: /Admin/Warranty/Create
        public async Task<IActionResult> Create(int? orderId)
        {
            var model = new WarrantyCard
            {
                WarrantyCode = $"BH-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                PurchaseDate = DateTime.UtcNow,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(12),
                WarrantyPeriodMonths = 12,
                WarrantyPackage = "12 tháng chính hãng CellphoneS"
            };

            if (orderId.HasValue)
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId.Value);

                if (order != null)
                {
                    model.OrderId = order.Id;
                    model.OrderCode = order.OrderCode;
                    model.CustomerName = order.CustomerName;
                    model.PhoneNumber = order.PhoneNumber;
                    model.Email = order.Email;
                    model.Address = order.Address;
                    model.PurchaseDate = order.CreatedAt;
                    model.StartDate = order.CreatedAt;
                    model.EndDate = order.CreatedAt.AddMonths(12);

                    var firstItem = order.OrderDetails.FirstOrDefault();
                    if (firstItem != null)
                    {
                        model.ProductId = firstItem.ProductId;
                        model.ProductName = firstItem.Product?.Name ?? string.Empty;
                        model.VariantInfo = firstItem.VariantInfo;
                        model.ProductImageUrl = firstItem.Product?.ImageUrl;
                        model.SerialNumber = $"IMEI-35{order.Id:D4}{firstItem.ProductId:D3}892";
                    }
                }
            }

            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            return View(model);
        }

        // POST: /Admin/Warranty/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WarrantyCard model)
        {
            if (string.IsNullOrWhiteSpace(model.CustomerName))
                ModelState.AddModelError("CustomerName", "Vui lòng nhập tên khách hàng");
            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
                ModelState.AddModelError("PhoneNumber", "Vui lòng nhập số điện thoại");
            if (string.IsNullOrWhiteSpace(model.ProductName))
                ModelState.AddModelError("ProductName", "Vui lòng nhập tên thiết bị");
            if (string.IsNullOrWhiteSpace(model.SerialNumber))
                ModelState.AddModelError("SerialNumber", "Vui lòng nhập số IMEI hoặc Serial");

            ModelState.Remove(nameof(model.WarrantyCode));
            ModelState.Remove(nameof(model.Product));
            ModelState.Remove(nameof(model.Order));
            ModelState.Remove(nameof(model.User));

            if (!ModelState.IsValid)
            {
                ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.WarrantyCode))
            {
                model.WarrantyCode = $"BH-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            }

            // Tự động tìm user tương ứng nếu có
            if (!model.UserId.HasValue)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber || (model.Email != null && u.Email == model.Email));
                if (user != null)
                {
                    model.UserId = user.Id;
                }
            }

            // Nếu người dùng chọn ProductId nhưng chưa có ảnh hoặc tên
            if (model.ProductId.HasValue)
            {
                var p = await _context.Products.FindAsync(model.ProductId.Value);
                if (p != null)
                {
                    if (string.IsNullOrWhiteSpace(model.ProductName)) model.ProductName = p.Name;
                    if (string.IsNullOrWhiteSpace(model.ProductImageUrl)) model.ProductImageUrl = p.ImageUrl;
                }
            }

            if (model.EndDate <= model.StartDate)
            {
                model.EndDate = model.StartDate.AddMonths(model.WarrantyPeriodMonths > 0 ? model.WarrantyPeriodMonths : 12);
            }

            model.CreatedAt = DateTime.UtcNow;
            _context.WarrantyCards.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cấp thành công Phiếu bảo hành điện tử mã: {model.WarrantyCode}!";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        // POST: /Admin/Warranty/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, WarrantyCardStatus status, string? serialNumber, int? extendMonths, string? adminNote)
        {
            var card = await _context.WarrantyCards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            card.Status = status;

            if (!string.IsNullOrWhiteSpace(serialNumber))
            {
                card.SerialNumber = serialNumber.Trim();
            }

            if (extendMonths.HasValue && extendMonths.Value > 0)
            {
                card.EndDate = card.EndDate.AddMonths(extendMonths.Value);
                card.WarrantyPeriodMonths += extendMonths.Value;
                if (card.Status == WarrantyCardStatus.Expired && card.EndDate > DateTime.UtcNow)
                {
                    card.Status = WarrantyCardStatus.Active;
                }
            }

            if (!string.IsNullOrWhiteSpace(adminNote))
            {
                card.AdminNote = adminNote.Trim();
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật Phiếu bảo hành #{card.WarrantyCode} thành công!";
            return RedirectToAction(nameof(Details), new { id = card.Id });
        }

        // POST: /Admin/Warranty/Void
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Void(int id, string reason)
        {
            var card = await _context.WarrantyCards.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            card.Status = WarrantyCardStatus.Void;
            card.AdminNote = $"[TỪ CHỐI BẢO HÀNH {DateTime.UtcNow:dd/MM/yyyy HH:mm}]: {reason}";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã hủy hiệu lực bảo hành phiếu #{card.WarrantyCode}.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
