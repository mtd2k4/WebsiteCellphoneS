using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Models.ViewModels
{
    public class VoucherViewModel
    {
        public List<Coupon> AllCoupons { get; set; } = new();
        public List<Coupon> OrderCoupons { get; set; } = new();
        public List<Coupon> ShippingCoupons { get; set; } = new();
        public List<Coupon> FlashCoupons { get; set; } = new();
        public List<Coupon> SmemberCoupons { get; set; } = new();

        public int DailyStreak { get; set; } = 1;
        public bool HasClaimedToday { get; set; } = false;
        public Coupon? TodayGift { get; set; }
        public HashSet<string> SavedCouponCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        // Flash Sale hours
        public string CurrentFlashSlot { get; set; } = "12:00";
        public string NextFlashSlot { get; set; } = "18:00";
        public TimeSpan TimeUntilNextFlash { get; set; } = TimeSpan.FromHours(2);
        public int TotalClaimedCountToday { get; set; } = 15280;
    }

    public class DailyStreakDayInfo
    {
        public int DayNumber { get; set; }
        public string RewardTitle { get; set; } = string.Empty;
        public string RewardBadge { get; set; } = string.Empty;
        public string CouponCode { get; set; } = string.Empty;
        public bool IsClaimed { get; set; }
        public bool IsCurrentDay { get; set; }
        public bool IsLocked { get; set; }
    }
}
