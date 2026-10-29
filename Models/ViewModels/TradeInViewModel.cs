using System;
using System.Collections.Generic;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Models.ViewModels
{
    public class TradeInPageViewModel
    {
        public List<string> DeviceTypes { get; set; } = new() { "Điện thoại", "Laptop", "Tablet", "Đồng hồ", "Tai nghe" };
        public string SelectedDeviceType { get; set; } = "Điện thoại";
        public List<string> Brands { get; set; } = new();
        public List<TradeInDevice> FeaturedOldDevices { get; set; } = new();
        public List<Product> TargetProducts { get; set; } = new();
        public Product? SelectedTargetProduct { get; set; }
        public List<string> BranchStores { get; set; } = new();
        public string CurrentUserSmemberTier { get; set; } = "None";
    }

    public class TradeInCalculateRequest
    {
        public int OldDeviceId { get; set; }
        public int ConditionGrade { get; set; } = 1; // 1: Loại 1 (100%), 2: Loại 2 (~88%), 3: Loại 3 (~72%), 4: Loại 4 (~55%), 5: Loại 5 (~40%)
        public int? TargetProductId { get; set; }
        public string ActionType { get; set; } = "Upgrade"; // "Upgrade" (Lên đời máy mới) hoặc "CashOut" (Bán lại nhận tiền)
        public string SmemberTier { get; set; } = "None"; // "None", "S-Mem", "S-VIP"
    }

    public class TradeInCalculateResult
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public string OldDeviceName { get; set; } = string.Empty;
        public string OldDeviceStorage { get; set; } = string.Empty;
        public int ConditionGrade { get; set; }
        public string ConditionName { get; set; } = string.Empty;
        public decimal BaseEstimatePrice { get; set; }
        public decimal SubsidyAmount { get; set; }
        public string SmemberTier { get; set; } = "None";
        public decimal SmemberBonus { get; set; }
        public decimal TotalBenefit { get; set; }
        public string ActionType { get; set; } = "Upgrade";
        public int? TargetProductId { get; set; }
        public string TargetProductName { get; set; } = string.Empty;
        public decimal TargetProductPrice { get; set; }
        public decimal PayDifference { get; set; }
    }

    public class TradeInBookingRequest
    {
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string OldDeviceName { get; set; } = string.Empty;
        public string OldDeviceStorage { get; set; } = string.Empty;
        public int ConditionGrade { get; set; } = 1;
        public string ActionType { get; set; } = "Upgrade";
        public string SmemberTier { get; set; } = "None";
        public decimal SmemberBonus { get; set; } = 0;
        public decimal BaseEstimatePrice { get; set; }
        public decimal SubsidyAmount { get; set; }
        public decimal TotalBenefit { get; set; }
        public int? TargetProductId { get; set; }
        public string? TargetProductName { get; set; }
        public decimal TargetProductPrice { get; set; }
        public decimal PayDifference { get; set; }
        public string BranchStore { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public string? CustomerNote { get; set; }
    }
}
