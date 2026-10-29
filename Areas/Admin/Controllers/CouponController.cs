using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CouponController : Controller
    {
        private readonly AppDbContext _context;

        public CouponController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Coupon
        public async Task<IActionResult> Index(int? type, string? status, string? search)
        {
            var query = _context.Coupons.AsQueryable();

            // Lọc theo loại mã: 0 = Đơn hàng, 1 = Phí vận chuyển
            if (type.HasValue)
            {
                query = query.Where(c => (int)c.Type == type.Value);
            }

            // Lọc theo trạng thái
            var now = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(status))
            {
                switch (status.ToLower())
                {
                    case "active":
                        query = query.Where(c => c.IsActive && c.ExpiryDate >= now);
                        break;
                    case "inactive":
                        query = query.Where(c => !c.IsActive);
                        break;
                    case "expired":
                        query = query.Where(c => c.ExpiryDate < now);
                        break;
                }
            }

            // Tìm kiếm theo mã hoặc tiêu đề
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(c => c.Code.Contains(s) || c.Title.Contains(s) || c.Description.Contains(s));
            }

            var coupons = await query
                .OrderBy(c => c.Type)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();

            // Thống kê KPI
            ViewBag.TotalCount = await _context.Coupons.CountAsync();
            ViewBag.OrderCouponCount = await _context.Coupons.CountAsync(c => c.Type == CouponType.OrderDiscount);
            ViewBag.ShippingCouponCount = await _context.Coupons.CountAsync(c => c.Type == CouponType.ShippingDiscount);
            ViewBag.ActiveCount = await _context.Coupons.CountAsync(c => c.IsActive && c.ExpiryDate >= now);

            ViewBag.CurrentType = type;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;

            return View(coupons);
        }

        // GET: /Admin/Coupon/Create
        public IActionResult Create()
        {
            var coupon = new Coupon
            {
                Type = CouponType.OrderDiscount,
                DiscountValue = 50000,
                MinOrderAmount = 200000,
                UsageLimit = 1000,
                ExpiryDate = DateTime.Now.AddDays(30),
                IsActive = true
            };
            return View(coupon);
        }

        // POST: /Admin/Coupon/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (string.IsNullOrWhiteSpace(coupon.Code))
            {
                ModelState.AddModelError("Code", "Vui lòng nhập mã giảm giá.");
            }
            else
            {
                coupon.Code = coupon.Code.Trim().ToUpper();
                if (await _context.Coupons.AnyAsync(c => c.Code.ToUpper() == coupon.Code))
                {
                    ModelState.AddModelError("Code", $"Mã giảm giá '{coupon.Code}' đã tồn tại trong hệ thống.");
                }
            }

            if (coupon.DiscountValue <= 0)
            {
                ModelState.AddModelError("DiscountValue", "Mức giảm giá phải lớn hơn 0.");
            }

            if (coupon.IsPercentage && (coupon.DiscountValue < 1 || coupon.DiscountValue > 100))
            {
                ModelState.AddModelError("DiscountValue", "Giảm giá theo % phải nằm trong khoảng từ 1% đến 100%.");
            }

            if (!ModelState.IsValid)
            {
                return View(coupon);
            }

            coupon.CreatedAt = DateTime.UtcNow;
            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo mã giảm giá '{coupon.Code}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Coupon/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                TempData["Error"] = "Không tìm thấy mã giảm giá yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            return View(coupon);
        }

        // POST: /Admin/Coupon/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Coupon model)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                TempData["Error"] = "Không tìm thấy mã giảm giá cần cập nhật.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(model.Code))
            {
                ModelState.AddModelError("Code", "Vui lòng nhập mã giảm giá.");
            }
            else
            {
                model.Code = model.Code.Trim().ToUpper();
                if (await _context.Coupons.AnyAsync(c => c.Id != id && c.Code.ToUpper() == model.Code))
                {
                    ModelState.AddModelError("Code", $"Mã giảm giá '{model.Code}' đã tồn tại ở bản ghi khác.");
                }
            }

            if (model.DiscountValue <= 0)
            {
                ModelState.AddModelError("DiscountValue", "Mức giảm giá phải lớn hơn 0.");
            }

            if (model.IsPercentage && (model.DiscountValue < 1 || model.DiscountValue > 100))
            {
                ModelState.AddModelError("DiscountValue", "Giảm giá theo % phải nằm trong khoảng từ 1% đến 100%.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            coupon.Code = model.Code;
            coupon.Title = model.Title.Trim();
            coupon.Description = model.Description?.Trim() ?? string.Empty;
            coupon.Type = model.Type;
            coupon.DiscountValue = model.DiscountValue;
            coupon.IsPercentage = model.IsPercentage;
            coupon.MinOrderAmount = model.MinOrderAmount;
            coupon.MaxDiscountAmount = model.MaxDiscountAmount;
            coupon.UsageLimit = model.UsageLimit;
            coupon.ExpiryDate = model.ExpiryDate;
            coupon.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật mã giảm giá '{coupon.Code}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Coupon/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return Json(new { success = false, message = "Không tìm thấy mã giảm giá." });
            }

            coupon.IsActive = !coupon.IsActive;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isActive = coupon.IsActive,
                message = coupon.IsActive ? $"Đã kích hoạt mã '{coupon.Code}'!" : $"Đã tạm khóa mã '{coupon.Code}'!"
            });
        }

        // POST: /Admin/Coupon/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                TempData["Error"] = "Không tìm thấy mã giảm giá cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            var code = coupon.Code;
            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa mã giảm giá '{code}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
