using System;
using System.Collections.Generic;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Areas.Admin.Models
{
    public class RevenueReportViewModel
    {
        // Bộ lọc thời gian & điều kiện
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Preset { get; set; } = "30days"; // today, 7days, 30days, thismonth, lastmonth, thisyear, custom
        public string PaymentMethod { get; set; } = "all"; // all, COD, BankTransfer, VNPay
        public string StatusFilter { get; set; } = "valid"; // valid (completed+shipping), completed, all

        // Tổng hợp chỉ số tài chính (KPIs)
        public decimal TotalRevenue { get; set; }
        public decimal TotalSubtotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalShippingFee { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int CompletedOrders { get; set; }
        public int ShippingOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal EstimatedProfit { get; set; } // Lợi nhuận gộp ước tính (~20%)

        // Dữ liệu biểu đồ theo thời gian
        public List<string> TimelineLabels { get; set; } = new();
        public List<decimal> TimelineRevenue { get; set; } = new();
        public List<int> TimelineOrders { get; set; } = new();

        // Cơ cấu doanh thu theo Danh mục
        public List<CategoryRevenueItem> CategoryBreakdown { get; set; } = new();

        // Cơ cấu doanh thu theo Phương thức thanh toán
        public List<PaymentMethodRevenueItem> PaymentBreakdown { get; set; } = new();

        // Top sản phẩm doanh thu cao nhất
        public List<TopSellingProductItem> TopProducts { get; set; } = new();

        // Danh sách đơn hàng chi tiết trong kỳ
        public List<Order> Orders { get; set; } = new();
    }

    public class CategoryRevenueItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal Revenue { get; set; }
        public double Percentage { get; set; }
    }

    public class PaymentMethodRevenueItem
    {
        public string MethodName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
        public double Percentage { get; set; }
    }

    public class TopSellingProductItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
