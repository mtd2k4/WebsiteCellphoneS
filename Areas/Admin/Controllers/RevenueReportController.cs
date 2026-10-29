using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class RevenueReportController : Controller
    {
        private readonly AppDbContext _context;

        public RevenueReportController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/RevenueReport
        public async Task<IActionResult> Index(
            string? preset = "30days",
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? paymentMethod = "all",
            string? statusFilter = "all")
        {
            var vm = await BuildReportViewModel(preset, startDate, endDate, paymentMethod, statusFilter);
            return View(vm);
        }

        // GET: /Admin/RevenueReport/ExportPdf
        public async Task<IActionResult> ExportPdf(
            string? preset = "30days",
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? paymentMethod = "all",
            string? statusFilter = "all")
        {
            var vm = await BuildReportViewModel(preset, startDate, endDate, paymentMethod, statusFilter);
            return View(vm);
        }

        private async Task<RevenueReportViewModel> BuildReportViewModel(
            string? preset,
            DateTime? startDate,
            DateTime? endDate,
            string? paymentMethod,
            string? statusFilter)
        {
            preset = string.IsNullOrWhiteSpace(preset) ? "30days" : preset.ToLower();
            paymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "all" : paymentMethod;
            statusFilter = string.IsNullOrWhiteSpace(statusFilter) ? "all" : statusFilter;

            var now = DateTime.UtcNow;
            DateTime start;
            DateTime end = now.Date.AddDays(1).AddTicks(-1);

            switch (preset)
            {
                case "today":
                    start = now.Date;
                    break;
                case "7days":
                    start = now.Date.AddDays(-6);
                    break;
                case "thismonth":
                    start = new DateTime(now.Year, now.Month, 1);
                    break;
                case "lastmonth":
                    var lastMonth = now.AddMonths(-1);
                    start = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                    end = new DateTime(now.Year, now.Month, 1).AddTicks(-1);
                    break;
                case "thisyear":
                    start = new DateTime(now.Year, 1, 1);
                    break;
                case "custom":
                    start = startDate.HasValue ? startDate.Value.Date : now.Date.AddDays(-30);
                    if (endDate.HasValue)
                    {
                        end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    }
                    break;
                case "30days":
                default:
                    preset = "30days";
                    start = now.Date.AddDays(-29);
                    break;
            }

            // Truy vấn cơ sở dữ liệu
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p!.Category)
                .Where(o => o.CreatedAt >= start && o.CreatedAt <= end)
                .AsQueryable();

            // Lọc phương thức thanh toán
            if (paymentMethod != "all")
            {
                query = query.Where(o => o.PaymentMethod.ToLower() == paymentMethod.ToLower());
            }

            // Lọc trạng thái đơn hàng (Mặc định: Hiển thị tất cả trạng thái đơn)
            if (statusFilter == "completed")
            {
                query = query.Where(o => o.Status == OrderStatus.Completed);
            }
            else if (statusFilter == "shipping")
            {
                query = query.Where(o => o.Status == OrderStatus.Shipping);
            }
            else if (statusFilter == "pending")
            {
                query = query.Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed);
            }
            else if (statusFilter == "cancelled")
            {
                query = query.Where(o => o.Status == OrderStatus.Cancelled);
            }
            else if (statusFilter == "valid")
            {
                query = query.Where(o => o.Status != OrderStatus.Cancelled);
            }
            // Nếu là "all": Giữ nguyên toàn bộ trạng thái đơn hàng

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            // Tính toán doanh thu (doanh thu tính trên các đơn không bị hủy nếu lọc "all" hoặc "valid")
            decimal revenue = (statusFilter == "cancelled")
                ? orders.Sum(o => o.TotalAmount)
                : orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);

            decimal subtotal = (statusFilter == "cancelled")
                ? orders.Sum(o => o.SubtotalAmount)
                : orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.SubtotalAmount);

            decimal discount = (statusFilter == "cancelled")
                ? orders.Sum(o => o.OrderDiscountAmount + o.ShippingDiscountAmount)
                : orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.OrderDiscountAmount + o.ShippingDiscountAmount);

            decimal shippingFee = (statusFilter == "cancelled")
                ? orders.Sum(o => o.ShippingFee)
                : orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.ShippingFee);

            var vm = new RevenueReportViewModel
            {
                StartDate = start,
                EndDate = end,
                Preset = preset,
                PaymentMethod = paymentMethod,
                StatusFilter = statusFilter,
                Orders = orders,
                TotalOrders = orders.Count,
                TotalRevenue = revenue,
                TotalSubtotal = subtotal,
                TotalDiscount = discount,
                TotalShippingFee = shippingFee,
                CompletedOrders = orders.Count(o => o.Status == OrderStatus.Completed),
                ShippingOrders = orders.Count(o => o.Status == OrderStatus.Shipping),
                PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed),
                CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled)
            };

            vm.AverageOrderValue = vm.TotalOrders > 0 ? Math.Round(vm.TotalRevenue / vm.TotalOrders, 0) : 0;
            vm.EstimatedProfit = Math.Round(vm.TotalRevenue * 0.22m, 0); // Ước tính biên lợi nhuận gộp 22%

            // 1. TẠO DỮ LIỆU BIỂU ĐỒ DOANH THU THEO THỜI GIAN
            if (preset == "today")
            {
                // Chia thành các khung giờ trong ngày (mỗi 2 giờ: 00:00, 02:00, ..., 22:00)
                for (int h = 0; h < 24; h += 2)
                {
                    var label = $"{h:D2}:00";
                    var hourStart = start.Date.AddHours(h);
                    var hourEnd = hourStart.AddHours(2);
                    var hourOrders = orders.Where(o => o.CreatedAt >= hourStart && o.CreatedAt < hourEnd).ToList();
                    var hourRev = hourOrders.Where(o => statusFilter == "cancelled" || o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);

                    vm.TimelineLabels.Add(label);
                    vm.TimelineRevenue.Add(hourRev);
                    vm.TimelineOrders.Add(hourOrders.Count);
                }
            }
            else
            {
                var totalDays = (end.Date - start.Date).Days + 1;
                if (totalDays <= 31)
                {
                    for (int i = 0; i < totalDays; i++)
                    {
                        var curDate = start.Date.AddDays(i);
                        var label = curDate.ToString("dd/MM");
                        var dayOrders = orders.Where(o => o.CreatedAt.Date == curDate).ToList();
                        var dayRev = dayOrders.Where(o => statusFilter == "cancelled" || o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);

                        vm.TimelineLabels.Add(label);
                        vm.TimelineRevenue.Add(dayRev);
                        vm.TimelineOrders.Add(dayOrders.Count);
                    }
                }
                else
                {
                    // Nhóm theo tuần hoặc tháng nếu khoảng thời gian dài
                    var grouped = orders
                        .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                        .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month);

                    foreach (var g in grouped)
                    {
                        var groupRev = g.Where(o => statusFilter == "cancelled" || o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);
                        vm.TimelineLabels.Add($"T{g.Key.Month}/{g.Key.Year}");
                        vm.TimelineRevenue.Add(groupRev);
                        vm.TimelineOrders.Add(g.Count());
                    }
                }
            }

            // 2. CƠ CẤU DOANH THU THEO DANH MỤC
            var allDetails = orders.SelectMany(o => o.OrderDetails).ToList();
            var categoryGroup = allDetails
                .GroupBy(d => d.Product?.Category?.Name ?? "Khác")
                .Select(g => new CategoryRevenueItem
                {
                    CategoryName = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            var totalCatRevenue = categoryGroup.Sum(x => x.Revenue);
            foreach (var cat in categoryGroup)
            {
                cat.Percentage = totalCatRevenue > 0 ? Math.Round((double)cat.Revenue / (double)totalCatRevenue * 100, 1) : 0;
            }
            vm.CategoryBreakdown = categoryGroup;

            // 3. CƠ CẤU DOANH THU THEO PHƯƠNG THỨC THANH TOÁN
            var paymentGroup = orders
                .GroupBy(o => o.PaymentMethod)
                .Select(g => new PaymentMethodRevenueItem
                {
                    MethodName = GetPaymentMethodDisplay(g.Key),
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            var totalPayRevenue = paymentGroup.Sum(p => p.Revenue);
            foreach (var pay in paymentGroup)
            {
                pay.Percentage = totalPayRevenue > 0 ? Math.Round((double)pay.Revenue / (double)totalPayRevenue * 100, 1) : 0;
            }
            vm.PaymentBreakdown = paymentGroup;

            // 4. TOP SẢN PHẨM BÁN CHẠY THEO DOANH THU
            var topGroup = allDetails
                .Where(d => d.Product != null)
                .GroupBy(d => d.ProductId)
                .Select(g =>
                {
                    var first = g.First();
                    return new TopSellingProductItem
                    {
                        ProductId = g.Key,
                        ProductName = first.Product?.Name ?? "Sản phẩm",
                        CategoryName = first.Product?.Category?.Name ?? "Điện thoại",
                        ImageUrl = first.Product?.ImageUrl,
                        QuantitySold = g.Sum(x => x.Quantity),
                        TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
                    };
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(8)
                .ToList();

            vm.TopProducts = topGroup;

            return vm;
        }

        private static string GetPaymentMethodDisplay(string method)
        {
            return method.ToLower() switch
            {
                "cod" => "Thanh toán khi nhận hàng (COD)",
                "vnpay" => "Cổng VNPAY-QR",
                "banktransfer" => "Chuyển khoản ngân hàng",
                _ => method
            };
        }
    }
}
