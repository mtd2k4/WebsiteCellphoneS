using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Models.ViewModels
{
    public class ProfileViewModel
    {
        public AppUser User { get; set; } = new();
        public string ActiveTab { get; set; } = "overview";

        public List<Order> Orders { get; set; } = new();
        public decimal TotalSpent { get; set; }
        public int CompletedOrdersCount { get; set; }

        public List<WarrantyItemViewModel> WarrantyList { get; set; } = new();
        public List<WarrantyCard> WarrantyCards { get; set; } = new();
        public List<TradeInItemViewModel> TradeInList { get; set; } = new();
        public List<SmemberVoucherViewModel> Vouchers { get; set; } = new();
        public List<RepairBooking> RepairBookings { get; set; } = new();

        // Tier Progress
        public string CurrentTierName { get; set; } = "S-New";
        public string NextTierName { get; set; } = "S-Mem";
        public decimal NextTierTarget { get; set; } = 3000000;
        public int TierProgressPercent { get; set; }
        public decimal AmountNeededForNextTier { get; set; }
    }

    public class WarrantyItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSlug { get; set; }
        public string? ImageUrl { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string OrderCode { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public string WarrantyPeriod { get; set; } = "12 tháng chính hãng";
        public DateTime ExpiryDate { get; set; }
        public bool IsActive => DateTime.UtcNow <= ExpiryDate;
        public string ServiceCenter { get; set; } = "Điện Thoại Vui ASP - 136 Nguyễn Thái Học, Quận 1, TP.HCM";
    }

    public class TradeInItemViewModel
    {
        public string RequestCode { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Condition { get; set; } = "Loại 1 (Máy đẹp 99%)";
        public string ActionType { get; set; } = "Upgrade";
        public decimal EstimatedValue { get; set; }
        public decimal SmemberBonus { get; set; } = 0;
        public decimal SubsidyAmount { get; set; }
        public decimal TotalBenefit => EstimatedValue + SmemberBonus + SubsidyAmount;
        public string Status { get; set; } = "Đã xác nhận lịch hẹn";
        public DateTime CreatedDate { get; set; }
        public string Branch { get; set; } = "CellphoneS 213 Trần Quang Khải, Q.1, TP.HCM";
    }

    public class SmemberVoucherViewModel
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string DiscountText { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public string Scope { get; set; } = "Toàn bộ sản phẩm";
    }
}
