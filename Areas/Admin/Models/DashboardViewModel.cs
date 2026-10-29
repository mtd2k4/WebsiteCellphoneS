using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ShippingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }

        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }

        public int TotalCustomers { get; set; }
        public int PendingRepairs { get; set; }
        public int TotalWarranties { get; set; }
        public int TotalCoupons { get; set; }

        public List<Order> RecentOrders { get; set; } = new();
        public List<Product> TopProducts { get; set; } = new();
        public List<Product> LowStockList { get; set; } = new();

        // Chart.js Data
        public List<string> RevenueLabels { get; set; } = new();
        public List<decimal> RevenueValues { get; set; } = new();
    }
}
