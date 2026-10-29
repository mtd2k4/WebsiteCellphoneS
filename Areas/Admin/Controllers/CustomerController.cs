using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;

        public CustomerController(AppDbContext context)
        {
            _context = context;
        }

        // ========================================================
        // 1. DANH SÁCH KHÁCH HÀNG ĐÃ ĐĂNG KÝ THAM GIA HỆ THỐNG
        // ========================================================
        public async Task<IActionResult> Index(
            string? search, 
            string? smemberLevel, 
            bool? isActive, 
            string? province, 
            string? sortBy = "newest", 
            int page = 1)
        {
            // Chỉ lấy người dùng có vai trò là Customer (Khách hàng đã đăng ký tài khoản)
            var query = _context.Users.Where(u => u.Role == "Customer").AsQueryable();

            // 1. Thống kê KPI tổng quan khách hàng
            ViewBag.TotalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer");
            
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            ViewBag.NewCustomersThisMonth = await _context.Users.CountAsync(u => u.Role == "Customer" && u.CreatedAt >= startOfMonth);
            ViewBag.VipCustomers = await _context.Users.CountAsync(u => u.Role == "Customer" && u.SmemberLevel == "S-VIP");
            ViewBag.TotalSmemberPoints = await _context.Users.Where(u => u.Role == "Customer").SumAsync(u => (long)u.SmemberPoints);
            ViewBag.ActiveCustomers = await _context.Users.CountAsync(u => u.Role == "Customer" && u.IsActive);
            ViewBag.LockedCustomers = await _context.Users.CountAsync(u => u.Role == "Customer" && !u.IsActive);

            // 2. Tìm kiếm tự do theo Tên, Email, SĐT
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(u => 
                    u.FullName.ToLower().Contains(s) || 
                    u.Email.ToLower().Contains(s) || 
                    u.PhoneNumber.Contains(s));
            }

            // 3. Lọc theo hạng thành viên Smember (S-New, S-Mem, S-VIP)
            if (!string.IsNullOrWhiteSpace(smemberLevel) && smemberLevel != "All")
            {
                query = query.Where(u => u.SmemberLevel == smemberLevel);
            }

            // 4. Lọc theo trạng thái tài khoản (Hoạt động / Đã khóa)
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            // 5. Lọc theo Tỉnh/Thành
            if (!string.IsNullOrWhiteSpace(province) && province != "All")
            {
                query = query.Where(u => u.Province == province);
            }

            // 6. Sắp xếp
            query = sortBy switch
            {
                "oldest" => query.OrderBy(u => u.CreatedAt),
                "points_desc" => query.OrderByDescending(u => u.SmemberPoints),
                "points_asc" => query.OrderBy(u => u.SmemberPoints),
                "name_asc" => query.OrderBy(u => u.FullName),
                "name_desc" => query.OrderByDescending(u => u.FullName),
                _ => query.OrderByDescending(u => u.CreatedAt) // default newest
            };

            // 7. Phân trang
            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var customers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 8. Tải thống kê đơn hàng cho danh sách khách hàng này
            var customerEmails = customers.Select(c => c.Email.ToLower()).ToList();
            var customerPhones = customers.Select(c => c.PhoneNumber).ToList();

            var orderStats = await _context.Orders
                .Where(o => (o.Email != null && customerEmails.Contains(o.Email.ToLower())) || customerPhones.Contains(o.PhoneNumber))
                .GroupBy(o => new { o.Email, o.PhoneNumber })
                .Select(g => new
                {
                    Email = g.Key.Email != null ? g.Key.Email.ToLower() : "",
                    Phone = g.Key.PhoneNumber,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            var statsDict = new Dictionary<int, CustomerStatsDto>();
            foreach (var c in customers)
            {
                var matching = orderStats.Where(s => 
                    (!string.IsNullOrEmpty(s.Email) && s.Email == c.Email.ToLower()) || 
                    s.Phone == c.PhoneNumber).ToList();

                int count = matching.Sum(m => m.OrderCount);
                decimal spent = matching.Sum(m => m.TotalSpent);
                statsDict[c.Id] = new CustomerStatsDto { OrderCount = count, TotalSpent = spent };
            }
            ViewBag.OrderStats = statsDict;

            // View state
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentSmemberLevel = smemberLevel;
            ViewBag.CurrentIsActive = isActive;
            ViewBag.CurrentProvince = province;
            ViewBag.CurrentSortBy = sortBy;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(customers);
        }

        // ========================================================
        // 2. CHI TIẾT KHÁCH HÀNG & LỊCH SỬ TƯƠNG TÁC
        // ========================================================
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == "Customer");
            if (customer == null)
            {
                return NotFound();
            }

            // Tải danh sách đơn hàng đã mua
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => (o.Email != null && o.Email.ToLower() == customer.Email.ToLower()) || o.PhoneNumber == customer.PhoneNumber)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // Tải yêu cầu Thu cũ Đổi mới (nếu có)
            var tradeIns = await _context.TradeInRequests
                .Where(t => t.UserId == customer.Id || t.PhoneNumber == customer.PhoneNumber || (t.Email != null && t.Email.ToLower() == customer.Email.ToLower()))
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Tải đánh giá sản phẩm của khách (nếu có)
            var reviews = await _context.ProductReviews
                .Include(r => r.Product)
                .Where(r => r.UserId == customer.Id || r.PhoneNumber == customer.PhoneNumber)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Orders = orders;
            ViewBag.TradeIns = tradeIns;
            ViewBag.Reviews = reviews;
            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalSpent = orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);

            return View(customer);
        }

        // ========================================================
        // 3. KHÓA / MỞ KHÓA TÀI KHOẢN KHÁCH HÀNG (AJAX)
        // ========================================================
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var customer = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == "Customer");
            if (customer == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khách hàng." });
            }

            customer.IsActive = !customer.IsActive;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isActive = customer.IsActive,
                message = customer.IsActive 
                    ? $"Đã mở khóa tài khoản khách hàng '{customer.FullName}' thành công!" 
                    : $"Đã tạm khóa tài khoản khách hàng '{customer.FullName}'!"
            });
        }

        // ========================================================
        // 4. CẬP NHẬT HẠNG SMEMBER & ĐIỂM TÍCH LŨY (AJAX)
        // ========================================================
        [HttpPost]
        public async Task<IActionResult> UpdateSmember(int id, string smemberLevel, int smemberPoints)
        {
            var customer = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == "Customer");
            if (customer == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khách hàng." });
            }

            customer.SmemberLevel = smemberLevel;
            customer.SmemberPoints = Math.Max(0, smemberPoints);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Đã cập nhật hạng {customer.SmemberLevel} và {customer.SmemberPoints:N0} điểm Smember cho khách hàng '{customer.FullName}'!"
            });
        }
    }

    public class CustomerStatsDto
    {
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
