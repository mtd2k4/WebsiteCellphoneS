using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Areas.Admin.Models;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel();

            // Order stats
            var allOrders = await _context.Orders
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            vm.TotalOrders = allOrders.Count;
            vm.TotalRevenue = allOrders
                .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Shipping)
                .Sum(o => o.TotalAmount);

            vm.PendingOrders = allOrders.Count(o => o.Status == OrderStatus.Pending);
            vm.ShippingOrders = allOrders.Count(o => o.Status == OrderStatus.Shipping);
            vm.CompletedOrders = allOrders.Count(o => o.Status == OrderStatus.Completed);
            vm.CancelledOrders = allOrders.Count(o => o.Status == OrderStatus.Cancelled);

            // Products stats
            var allProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .ToListAsync();

            vm.TotalProducts = allProducts.Count;
            vm.LowStockList = allProducts.Where(p => p.StockQuantity <= 20).Take(5).ToList();
            vm.LowStockProducts = vm.LowStockList.Count;

            // Top Products
            vm.TopProducts = allProducts
                .OrderByDescending(p => p.FlashSaleSold + p.ReviewCount)
                .Take(5)
                .ToList();

            // Business Metrics: Customers, Repairs, Warranties, Coupons
            vm.TotalCustomers = await _context.Users.CountAsync(u => u.Role != "Admin");
            vm.PendingRepairs = await _context.RepairBookings.CountAsync(r => r.Status == RepairStatus.Pending);
            vm.TotalWarranties = await _context.WarrantyCards.CountAsync();
            vm.TotalCoupons = await _context.Coupons.CountAsync(c => c.IsActive);

            // Recent Orders
            vm.RecentOrders = allOrders.Take(6).ToList();

            // Revenue last 7 days chart data
            var now = DateTime.UtcNow;
            for (int i = 6; i >= 0; i--)
            {
                var targetDate = now.Date.AddDays(-i);
                var label = targetDate.ToString("dd/MM");
                var dailySum = allOrders
                    .Where(o => o.CreatedAt.Date == targetDate && o.Status != OrderStatus.Cancelled)
                    .Sum(o => o.TotalAmount);

                vm.RevenueLabels.Add(label);
                vm.RevenueValues.Add(dailySum);
            }

            return View(vm);
        }
    }
}
