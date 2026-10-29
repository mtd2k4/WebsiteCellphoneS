using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;
using WebsiteBanDienThoai.Models.ViewModels;

namespace WebsiteBanDienThoai.Controllers
{
    public class VoucherController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VoucherController> _logger;
        private const string ClaimedVouchersSessionKey = "ClaimedVouchers";
        private const string ClaimedVouchersCookieKey = "ClaimedVouchers_CPS";
        private const string DailyClaimDateCookieKey = "DailyClaim_Date";
        private const string DailyClaimStreakCookieKey = "DailyClaim_Streak";
        private const string DailyClaimCodeCookieKey = "DailyClaim_Code";

        public VoucherController(AppDbContext context, ILogger<VoucherController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Voucher
        public async Task<IActionResult> Index(string? category, string? search)
        {
            await EnsureDailyCouponsSeededAsync();

            var now = DateTime.UtcNow;
            var activeCoupons = await _context.Coupons
                .Where(c => c.IsActive && c.ExpiryDate >= now)
                .OrderBy(c => c.Type)
                .ThenByDescending(c => c.DiscountValue)
                .ToListAsync();

            // Daily Check-in Streak Tracking (chỉ tính khi khách hàng đã đăng nhập)
            var todayStr = DateTime.Now.ToString("yyyy-MM-dd");
            var yesterdayStr = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            int? currentUserId = GetCurrentUserId();
            int streak = 1;
            bool hasClaimedToday = false;
            Coupon? todayGift = null;

            if (currentUserId.HasValue)
            {
                string userDateKey = $"{DailyClaimDateCookieKey}_{currentUserId.Value}";
                string userStreakKey = $"{DailyClaimStreakCookieKey}_{currentUserId.Value}";
                string userCodeKey = $"{DailyClaimCodeCookieKey}_{currentUserId.Value}";

                var lastClaimDate = Request.Cookies[userDateKey] ?? Request.Cookies[DailyClaimDateCookieKey];
                var streakVal = Request.Cookies[userStreakKey] ?? Request.Cookies[DailyClaimStreakCookieKey];
                if (int.TryParse(streakVal, out int parsedStreak) && parsedStreak >= 1 && parsedStreak <= 7)
                {
                    streak = parsedStreak;
                }

                hasClaimedToday = string.Equals(lastClaimDate, todayStr, StringComparison.OrdinalIgnoreCase);

                // If user did not claim today and missed yesterday, streak resets to 1
                if (!hasClaimedToday && !string.IsNullOrEmpty(lastClaimDate) && lastClaimDate != yesterdayStr)
                {
                    streak = 1;
                }

                var todayClaimedCode = Request.Cookies[userCodeKey] ?? Request.Cookies[DailyClaimCodeCookieKey];
                if (hasClaimedToday && !string.IsNullOrEmpty(todayClaimedCode))
                {
                    todayGift = activeCoupons.FirstOrDefault(c => string.Equals(c.Code, todayClaimedCode, StringComparison.OrdinalIgnoreCase));
                }
            }

            // Saved Vouchers
            var savedCodes = GetClaimedVoucherCodes();

            // Flash hour calculation (00:00, 09:00, 12:00, 18:00, 21:00)
            var currentHour = DateTime.Now.Hour;
            string currentSlot = "12:00";
            string nextSlot = "18:00";
            DateTime nextSlotTime;

            if (currentHour < 9)
            {
                currentSlot = "00:00";
                nextSlot = "09:00";
                nextSlotTime = DateTime.Today.AddHours(9);
            }
            else if (currentHour < 12)
            {
                currentSlot = "09:00";
                nextSlot = "12:00";
                nextSlotTime = DateTime.Today.AddHours(12);
            }
            else if (currentHour < 18)
            {
                currentSlot = "12:00";
                nextSlot = "18:00";
                nextSlotTime = DateTime.Today.AddHours(18);
            }
            else if (currentHour < 21)
            {
                currentSlot = "18:00";
                nextSlot = "21:00";
                nextSlotTime = DateTime.Today.AddHours(21);
            }
            else
            {
                currentSlot = "21:00";
                nextSlot = "00:00 (Sáng mai)";
                nextSlotTime = DateTime.Today.AddDays(1);
            }

            var timeUntilNext = nextSlotTime > DateTime.Now ? nextSlotTime - DateTime.Now : TimeSpan.FromHours(1);

            // Grouping
            var orderCoupons = activeCoupons.Where(c => c.Type == CouponType.OrderDiscount).ToList();
            var shippingCoupons = activeCoupons.Where(c => c.Type == CouponType.ShippingDiscount).ToList();
            var flashCoupons = activeCoupons.Where(c => c.Code.Contains("FLASH") || c.Code.Contains("LUCKY") || c.DiscountValue >= 100000).ToList();
            var smemberCoupons = activeCoupons.Where(c => c.Code.Contains("SMEMBER") || c.Code.Contains("VIP") || c.Title.Contains("Smember", StringComparison.OrdinalIgnoreCase)).ToList();

            var viewModel = new VoucherViewModel
            {
                AllCoupons = activeCoupons,
                OrderCoupons = orderCoupons,
                ShippingCoupons = shippingCoupons,
                FlashCoupons = flashCoupons,
                SmemberCoupons = smemberCoupons,
                DailyStreak = streak,
                HasClaimedToday = hasClaimedToday,
                TodayGift = todayGift,
                SavedCouponCodes = savedCodes,
                CurrentFlashSlot = currentSlot,
                NextFlashSlot = nextSlot,
                TimeUntilNextFlash = timeUntilNext,
                TotalClaimedCountToday = 15280 + (DateTime.Now.Hour * 140) + DateTime.Now.Minute
            };

            ViewBag.ActiveCategory = category ?? "all";
            ViewBag.SearchTerm = search;

            return View(viewModel);
        }

        // POST: /Voucher/ClaimDaily
        [HttpPost]
        public async Task<IActionResult> ClaimDaily()
        {
            // Bắt buộc đăng nhập mới cho phép điểm danh nhận voucher
            int? currentUserId = GetCurrentUserId();
            if (User.Identity == null || !User.Identity.IsAuthenticated || !currentUserId.HasValue)
            {
                return Json(new
                {
                    success = false,
                    requireLogin = true,
                    loginUrl = "/Account/Login?returnUrl=/Voucher",
                    message = "Quý khách vui lòng đăng nhập tài khoản Smember để điểm danh nhận voucher miễn phí mỗi ngày!"
                });
            }

            string userDateKey = $"{DailyClaimDateCookieKey}_{currentUserId.Value}";
            string userStreakKey = $"{DailyClaimStreakCookieKey}_{currentUserId.Value}";
            string userCodeKey = $"{DailyClaimCodeCookieKey}_{currentUserId.Value}";

            var todayStr = DateTime.Now.ToString("yyyy-MM-dd");
            var yesterdayStr = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var lastClaimDate = Request.Cookies[userDateKey];

            if (string.Equals(lastClaimDate, todayStr, StringComparison.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    success = false,
                    message = "Hôm nay bạn đã mở hộp quà điểm danh rồi! Hãy quay lại vào ngày mai để tiếp tục duy trì chuỗi nhé.",
                    alreadyClaimed = true
                });
            }

            // Calculate new streak
            int streak = 1;
            if (string.Equals(lastClaimDate, yesterdayStr, StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(Request.Cookies[userStreakKey], out int prevStreak))
                {
                    streak = (prevStreak % 7) + 1;
                }
            }

            // Streak reward mapping
            string targetCode = streak switch
            {
                1 => "FREESHIP30K",
                2 => "DAILYGIFT20K",
                3 => "LUCKYBOX50K",
                4 => "FREESHIP50K",
                5 => "GIAM10PT",
                6 => "GIAM100K",
                _ => "CPSVIP500K"
            };

            await EnsureDailyCouponsSeededAsync();

            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == targetCode && c.IsActive);
            if (coupon == null)
            {
                // Fallback to any active coupon
                coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.IsActive) ?? new Coupon
                {
                    Code = targetCode,
                    Title = "Quà tặng Điểm danh CellphoneS",
                    DiscountValue = 30000,
                    MinOrderAmount = 100000,
                    ExpiryDate = DateTime.UtcNow.AddDays(30)
                };
            }

            // Increment usage & tặng thêm 10 điểm thưởng Smember khi điểm danh
            coupon.UsedCount += 1;

            var user = await _context.Users.FindAsync(currentUserId.Value);
            if (user != null)
            {
                user.SmemberPoints += 10;
            }

            _context.Notifications.Add(new AppNotification
            {
                Type = NotificationType.SystemPromotion,
                Title = $"Điểm danh Ngày {streak} thành công!",
                Message = $"Bạn đã nhận được mã {coupon.Code} ({coupon.Title}) và +10 điểm Smember khi điểm danh hôm nay.",
                TargetUrl = "/Voucher",
                UserId = currentUserId.Value,
                RecipientRole = "Customer",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Save voucher to user's saved list
            SaveClaimedVoucherCode(coupon.Code);

            // Save cookie tracking per user
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(30),
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                IsEssential = true
            };

            Response.Cookies.Append(userDateKey, todayStr, cookieOptions);
            Response.Cookies.Append(userStreakKey, streak.ToString(), cookieOptions);
            Response.Cookies.Append(userCodeKey, coupon.Code, cookieOptions);

            return Json(new
            {
                success = true,
                message = $"Chúc mừng! Bạn đã điểm danh Ngày {streak} thành công và nhận được {coupon.Title}!",
                streak = streak,
                voucher = new
                {
                    code = coupon.Code,
                    title = coupon.Title,
                    description = coupon.Description,
                    discountValue = coupon.DiscountValue,
                    isPercentage = coupon.IsPercentage,
                    minOrderAmount = coupon.MinOrderAmount,
                    type = (int)coupon.Type,
                    typeText = coupon.Type == CouponType.OrderDiscount ? "Mã giảm giá đơn hàng" : "Mã miễn phí vận chuyển",
                    expiryDate = coupon.ExpiryDate.ToString("dd/MM/yyyy")
                }
            });
        }

        // POST: /Voucher/SaveVoucher
        [HttpPost]
        public async Task<IActionResult> SaveVoucher(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { success = false, message = "Mã voucher không hợp lệ" });
            }

            var cleanCode = code.Trim().ToUpper();
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == cleanCode && c.IsActive && c.ExpiryDate >= DateTime.UtcNow);
            if (coupon == null)
            {
                return Json(new { success = false, message = "Voucher không tồn tại hoặc đã hết hạn" });
            }

            SaveClaimedVoucherCode(cleanCode);

            return Json(new
            {
                success = true,
                message = $"Đã lưu mã {coupon.Code} ({coupon.Title}) vào ví voucher thành công!",
                code = coupon.Code
            });
        }

        // GET: /Voucher/GetMyVouchers
        [HttpGet]
        public async Task<IActionResult> GetMyVouchers()
        {
            var savedCodes = GetClaimedVoucherCodes();
            var now = DateTime.UtcNow;

            var coupons = await _context.Coupons
                .Where(c => savedCodes.Contains(c.Code) && c.IsActive && c.ExpiryDate >= now)
                .OrderBy(c => c.Type)
                .ThenByDescending(c => c.DiscountValue)
                .Select(c => new
                {
                    id = c.Id,
                    code = c.Code,
                    title = c.Title,
                    description = c.Description,
                    type = (int)c.Type,
                    typeText = c.Type == CouponType.OrderDiscount ? "Mã giảm giá đơn hàng" : "Mã miễn phí vận chuyển",
                    discountValue = c.DiscountValue,
                    isPercentage = c.IsPercentage,
                    minOrderAmount = c.MinOrderAmount,
                    maxDiscountAmount = c.MaxDiscountAmount,
                    expiryDate = c.ExpiryDate.ToString("dd/MM/yyyy")
                })
                .ToListAsync();

            return Json(new { success = true, count = coupons.Count, coupons });
        }

        #region Private Helpers
        private int? GetCurrentUserId()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return null;
            }
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int uid))
            {
                return uid;
            }
            return null;
        }

        private HashSet<string> GetClaimedVoucherCodes()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. From Session
            var sessionJson = HttpContext.Session.GetString(ClaimedVouchersSessionKey);
            if (!string.IsNullOrEmpty(sessionJson))
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<string>>(sessionJson);
                    if (list != null)
                    {
                        foreach (var code in list) result.Add(code);
                    }
                }
                catch { }
            }

            // 2. From Cookie
            var cookieJson = Request.Cookies[ClaimedVouchersCookieKey];
            if (!string.IsNullOrEmpty(cookieJson))
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<string>>(cookieJson);
                    if (list != null)
                    {
                        foreach (var code in list) result.Add(code);
                    }
                }
                catch { }
            }

            return result;
        }

        private void SaveClaimedVoucherCode(string code)
        {
            var saved = GetClaimedVoucherCodes();
            if (saved.Add(code.Trim().ToUpper()))
            {
                var list = saved.ToList();
                var json = JsonSerializer.Serialize(list);

                HttpContext.Session.SetString(ClaimedVouchersSessionKey, json);

                Response.Cookies.Append(ClaimedVouchersCookieKey, json, new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(30),
                    HttpOnly = false,
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true
                });
            }
        }

        private async Task EnsureDailyCouponsSeededAsync()
        {
            try
            {
                var defaultCoupons = new List<Coupon>
                {
                    new Coupon
                    {
                        Code = "DAILYGIFT20K",
                        Title = "Quà Tặng Điểm Danh Hàng Ngày",
                        Description = "Giảm trực tiếp 20.000₫ cho mọi đơn hàng phụ kiện, điện thoại từ 100.000₫",
                        Type = CouponType.OrderDiscount,
                        DiscountValue = 20000,
                        IsPercentage = false,
                        MinOrderAmount = 100000,
                        UsageLimit = 50000,
                        ExpiryDate = DateTime.UtcNow.AddYears(1),
                        IsActive = true
                    },
                    new Coupon
                    {
                        Code = "LUCKYBOX50K",
                        Title = "Hộp Quà May Mắn Mỗi Ngày",
                        Description = "Giảm 50.000₫ cho đơn hàng từ 300.000₫ áp dụng toàn hệ thống CellphoneS",
                        Type = CouponType.OrderDiscount,
                        DiscountValue = 50000,
                        IsPercentage = false,
                        MinOrderAmount = 300000,
                        UsageLimit = 30000,
                        ExpiryDate = DateTime.UtcNow.AddYears(1),
                        IsActive = true
                    },
                    new Coupon
                    {
                        Code = "DAILYFREESHIP",
                        Title = "Freeship Điểm Danh Mỗi Ngày",
                        Description = "Miễn phí 100% phí giao hàng tiêu chuẩn (tối đa 30.000₫) cho đơn từ 0₫",
                        Type = CouponType.ShippingDiscount,
                        DiscountValue = 30000,
                        IsPercentage = false,
                        MinOrderAmount = 0,
                        UsageLimit = 100000,
                        ExpiryDate = DateTime.UtcNow.AddYears(1),
                        IsActive = true
                    },
                    new Coupon
                    {
                        Code = "SMEMBERDAILY",
                        Title = "Đặc Quyền Smember Mỗi Ngày",
                        Description = "Giảm 150.000₫ cho hóa đơn mua sắm thiết bị từ 2.000.000₫",
                        Type = CouponType.OrderDiscount,
                        DiscountValue = 150000,
                        IsPercentage = false,
                        MinOrderAmount = 2000000,
                        UsageLimit = 20000,
                        ExpiryDate = DateTime.UtcNow.AddYears(1),
                        IsActive = true
                    },
                    new Coupon
                    {
                        Code = "FLASHNIGHT",
                        Title = "Voucher Khung Giờ Vàng Đêm Muộn",
                        Description = "Giảm 80.000₫ cho đơn hàng công nghệ từ 800.000₫ khi chốt đơn từ 21h - 24h",
                        Type = CouponType.OrderDiscount,
                        DiscountValue = 80000,
                        IsPercentage = false,
                        MinOrderAmount = 800000,
                        UsageLimit = 15000,
                        ExpiryDate = DateTime.UtcNow.AddYears(1),
                        IsActive = true
                    }
                };

                bool hasNew = false;
                foreach (var c in defaultCoupons)
                {
                    if (!await _context.Coupons.AnyAsync(x => x.Code == c.Code))
                    {
                        _context.Coupons.Add(c);
                        hasNew = true;
                    }
                }

                if (hasNew)
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not seed default daily coupons");
            }
        }
        #endregion
    }
}
