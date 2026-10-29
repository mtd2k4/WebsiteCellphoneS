using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;
using WebsiteBanDienThoai.Models.ViewModels;

namespace WebsiteBanDienThoai.Controllers
{
    public class TradeInController : Controller
    {
        private readonly AppDbContext _context;

        public TradeInController(AppDbContext context)
        {
            _context = context;
        }

        private readonly List<string> _branchStores = new()
        {
            "CellphoneS 213 Trần Quang Khải, P. Tân Định, Quận 1, TP.HCM",
            "CellphoneS 55B Trần Quang Khải, P. Tân Định, Quận 1, TP.HCM",
            "CellphoneS 136 Nguyễn Thái Học, P. Phạm Ngũ Lão, Quận 1, TP.HCM",
            "CellphoneS 363 Võ Văn Tần, Phường 5, Quận 3, TP.HCM",
            "CellphoneS 248 Cống Quỳnh, P. Phạm Ngũ Lão, Quận 1, TP.HCM",
            "CellphoneS 1075 Hậu Giang, Phường 11, Quận 6, TP.HCM",
            "CellphoneS 288 Đường 3/2, Phường 12, Quận 10, TP.HCM",
            "CellphoneS 125 Lê Văn Việt, P. Hiệp Phú, TP. Thủ Đức, TP.HCM",
            "CellphoneS 543 Nguyễn Thị Thập, P. Tân Phong, Quận 7, TP.HCM",
            "CellphoneS 19 Thái Hà, P. Trung Liệt, Q. Đống Đa, Hà Nội",
            "CellphoneS 117 Thái Hà, P. Trung Liệt, Q. Đống Đa, Hà Nội",
            "CellphoneS 278 Tây Sơn, P. Trung Liệt, Q. Đống Đa, Hà Nội",
            "CellphoneS 21 Thái Hà, P. Trung Liệt, Q. Đống Đa, Hà Nội",
            "CellphoneS 524 Bạch Mai, P. Trương Định, Q. Hai Bà Trưng, Hà Nội",
            "CellphoneS 133 Thái Hà, P. Trung Liệt, Q. Đống Đa, Hà Nội",
            "CellphoneS 369 Nguyễn Văn Cừ, P. Ngọc Lâm, Q. Long Biên, Hà Nội",
            "CellphoneS 177 Nguyễn Văn Linh, P. Nam Dương, Q. Hải Châu, Đà Nẵng",
            "CellphoneS 272 Đường 30/4, P. Hưng Lợi, Q. Ninh Kiều, Cần Thơ"
        };

        private async Task<AppUser?> GetCurrentUserAsync()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var identifier = User.Identity.Name;
                return await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == identifier || u.Email == identifier);
            }
            return null;
        }

        private static string NormalizeDeviceType(string? deviceType)
        {
            if (string.IsNullOrWhiteSpace(deviceType)) return "Điện thoại";
            deviceType = System.Net.WebUtility.HtmlDecode(deviceType);
            string lower = deviceType.Trim().ToLower();
            if (lower.Contains("thoai") || lower.Contains("phone") || lower.Contains("mobile") || lower.Contains("dien") || lower.Contains("110"))
                return "Điện thoại";
            if (lower.Contains("lap") || lower.Contains("macbook"))
                return "Laptop";
            if (lower.Contains("tab") || lower.Contains("ipad"))
                return "Tablet";
            if (lower.Contains("dong ho") || lower.Contains("watch"))
                return "Đồng hồ";
            if (lower.Contains("tai nghe") || lower.Contains("airpod") || lower.Contains("bud") || lower.Contains("head"))
                return "Tai nghe";
            return deviceType.Trim();
        }

        // GET: /TradeIn
        public async Task<IActionResult> Index(int? targetProductId = null, string? deviceType = "Điện thoại")
        {
            deviceType = NormalizeDeviceType(deviceType);

            var brands = await _context.TradeInDevices
                .Where(d => d.IsActive && (d.DeviceType == deviceType || (deviceType == "Điện thoại" && string.IsNullOrEmpty(d.DeviceType))))
                .Select(d => d.Brand)
                .Distinct()
                .ToListAsync();

            if (!brands.Any())
            {
                brands = await _context.TradeInDevices
                    .Where(d => d.IsActive)
                    .Select(d => d.Brand)
                    .Distinct()
                    .ToListAsync();
            }

            if (!brands.Any())
            {
                brands = new List<string> { "Apple", "Samsung", "Xiaomi", "OPPO" };
            }

            // Lấy toàn bộ danh sách thiết bị cho loại máy đã chọn (không cắt bớt Take(15))
            var featuredOldDevices = await _context.TradeInDevices
                .Where(d => d.IsActive && (d.DeviceType == deviceType || (deviceType == "Điện thoại" && string.IsNullOrEmpty(d.DeviceType))))
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();

            if (!featuredOldDevices.Any())
            {
                featuredOldDevices = await _context.TradeInDevices
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.DisplayOrder)
                    .ToListAsync();
            }

            // Lấy danh sách máy mới muốn lên đời (Điện thoại mới, đặc biệt flagship)
            var targetProducts = await _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Category)
                .Where(p => p.StockQuantity > 0)
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.IsHot)
                .Take(20)
                .ToListAsync();

            Product? selectedTarget = null;
            if (targetProductId.HasValue)
            {
                selectedTarget = await _context.Products
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.Id == targetProductId.Value);
            }
            else
            {
                // Mặc định chọn dòng máy hot nhất: iPhone 16 Pro Max nếu có
                selectedTarget = targetProducts.FirstOrDefault(p => p.Name.Contains("iPhone 16 Pro Max")) 
                                 ?? targetProducts.FirstOrDefault();
            }

            var user = await GetCurrentUserAsync();
            string userSmemberTier = "None";
            if (user != null)
            {
                userSmemberTier = user.SmemberLevel == "S-VIP" ? "S-VIP" : (user.SmemberLevel == "S-Mem" ? "S-Mem" : "None");
                ViewBag.CustomerName = user.FullName;
                ViewBag.CustomerPhone = user.PhoneNumber;
                ViewBag.CustomerEmail = user.Email;
            }

            var vm = new TradeInPageViewModel
            {
                DeviceTypes = new List<string> { "Điện thoại", "Laptop", "Tablet", "Đồng hồ", "Tai nghe" },
                SelectedDeviceType = deviceType,
                Brands = brands,
                FeaturedOldDevices = featuredOldDevices,
                TargetProducts = targetProducts,
                SelectedTargetProduct = selectedTarget,
                BranchStores = _branchStores,
                CurrentUserSmemberTier = userSmemberTier
            };

            return View(vm);
        }

        // AJAX: Lấy danh sách Hãng theo loại thiết bị
        [HttpGet]
        public async Task<IActionResult> GetBrands(string? deviceType)
        {
            var query = _context.TradeInDevices.Where(d => d.IsActive).AsQueryable();
            if (!string.IsNullOrWhiteSpace(deviceType))
            {
                string norm = NormalizeDeviceType(deviceType);
                query = query.Where(d => d.DeviceType == norm || (norm == "Điện thoại" && string.IsNullOrEmpty(d.DeviceType)));
            }

            var brands = await query.Select(d => d.Brand).Distinct().ToListAsync();
            return Json(brands);
        }

        // AJAX: Lấy danh sách dòng máy cũ theo Hãng, Loại máy hoặc Từ khóa tìm kiếm
        [HttpGet]
        public async Task<IActionResult> GetModels(string? brand, string? deviceType, string? keyword)
        {
            var query = _context.TradeInDevices.Where(d => d.IsActive).AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(d => d.ModelName.ToLower().Contains(kw) || d.Brand.ToLower().Contains(kw));
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(deviceType))
                {
                    string norm = NormalizeDeviceType(deviceType);
                    query = query.Where(d => d.DeviceType == norm || (norm == "Điện thoại" && string.IsNullOrEmpty(d.DeviceType)));
                }

                if (!string.IsNullOrWhiteSpace(brand))
                {
                    string b = System.Net.WebUtility.HtmlDecode(brand).Trim().ToLower();
                    query = query.Where(d => d.Brand.ToLower() == b);
                }
            }

            var devices = await query
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new
                {
                    d.Id,
                    d.Brand,
                    d.DeviceType,
                    d.ModelName,
                    d.Storage,
                    d.BasePrice,
                    DisplayName = $"{d.ModelName} {d.Storage}"
                })
                .ToListAsync();

            return Json(devices);
        }

        // AJAX: Tính toán định giá, thưởng Smember và số tiền bù chuẩn CellphoneS
        [HttpPost]
        public async Task<IActionResult> Calculate([FromBody] TradeInCalculateRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
            }

            var oldDevice = await _context.TradeInDevices.FindAsync(request.OldDeviceId);
            if (oldDevice == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy thiết bị cũ trong danh mục định giá." });
            }

            // 5 Bậc phân loại tình trạng máy chuẩn CellphoneS:
            // Loại 1: Máy đẹp như mới, màn đẹp không trầy, thân không trầy, hoạt động hoàn hảo (100% giá thu)
            // Loại 2: Máy trầy xước nhẹ vỏ ngoài, màn hình đẹp, hoạt động hoàn hảo (~88% giá thu)
            // Loại 3: Màn xước nhẹ, thân cấn nhẹ, pin chai nhẹ (~72% giá thu)
            // Loại 4: Màn ám ố nhẹ, thân nứt lưng/trầy sâu (~55% giá thu)
            // Loại 5: Máy lỗi chức năng (Face ID/Vân tay, cam, mic...) hoặc màn nứt nhẹ (~40% giá thu)
            decimal coefficient = request.ConditionGrade switch
            {
                1 => 1.00m,
                2 => 0.88m,
                3 => 0.72m,
                4 => 0.55m,
                5 => 0.40m,
                _ => 1.00m
            };

            decimal baseEstimate = Math.Round((oldDevice.BasePrice * coefficient) / 10000) * 10000;

            // Đặc quyền Smember: Tặng thêm 5% giá trị thu máy cũ (Tối đa 300k cho S-Mem, 500k cho S-VIP)
            decimal smemberBonus = 0;
            if (string.Equals(request.SmemberTier, "S-VIP", StringComparison.OrdinalIgnoreCase))
            {
                smemberBonus = Math.Min(Math.Round((baseEstimate * 0.05m) / 10000) * 10000, 500000m);
            }
            else if (string.Equals(request.SmemberTier, "S-Mem", StringComparison.OrdinalIgnoreCase))
            {
                smemberBonus = Math.Min(Math.Round((baseEstimate * 0.05m) / 10000) * 10000, 300000m);
            }

            bool isCashOut = string.Equals(request.ActionType, "CashOut", StringComparison.OrdinalIgnoreCase);

            decimal subsidy = 0;
            decimal targetPrice = 0;
            string targetName = isCashOut ? "Không đổi máy (Bán lại nhận tiền)" : "Sản phẩm mới tại CellphoneS";

            if (!isCashOut && request.TargetProductId.HasValue)
            {
                var targetProduct = await _context.Products
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.Id == request.TargetProductId.Value);

                if (targetProduct != null)
                {
                    targetName = targetProduct.Name;
                    targetPrice = targetProduct.SalePrice;

                    // Mức trợ giá theo giá trị máy mới:
                    // Flagship > 25tr: Trợ giá 2.500.000₫
                    // Máy > 15tr: Trợ giá 1.500.000₫
                    // Máy > 7tr: Trợ giá 1.000.000₫
                    // Máy dưới 7tr: Trợ giá 500.000₫
                    if (targetPrice >= 25000000)
                    {
                        subsidy = 2500000m;
                    }
                    else if (targetPrice >= 15000000)
                    {
                        subsidy = 1500000m;
                    }
                    else if (targetPrice >= 7000000)
                    {
                        subsidy = 1000000m;
                    }
                    else
                    {
                        subsidy = 500000m;
                    }
                }
            }

            decimal totalBenefit = baseEstimate + smemberBonus + subsidy;
            decimal payDifference = (!isCashOut && targetPrice > 0) ? Math.Max(0, targetPrice - totalBenefit) : 0;

            string conditionName = request.ConditionGrade switch
            {
                1 => "Loại 1 (Máy đẹp 99%)",
                2 => "Loại 2 (Trầy xước nhẹ 95%)",
                3 => "Loại 3 (Cấn nhẹ / Màn xước 90%)",
                4 => "Loại 4 (Ám ố / Vỡ lưng nhẹ)",
                5 => "Loại 5 (Lỗi chức năng / hỏng nhẹ)",
                _ => "Loại 1 (Máy đẹp 99%)"
            };

            return Json(new TradeInCalculateResult
            {
                Success = true,
                ActionType = isCashOut ? "CashOut" : "Upgrade",
                OldDeviceName = oldDevice.ModelName,
                OldDeviceStorage = oldDevice.Storage,
                ConditionGrade = request.ConditionGrade,
                ConditionName = conditionName,
                BaseEstimatePrice = baseEstimate,
                SmemberTier = request.SmemberTier ?? "None",
                SmemberBonus = smemberBonus,
                SubsidyAmount = subsidy,
                TotalBenefit = totalBenefit,
                TargetProductId = isCashOut ? null : request.TargetProductId,
                TargetProductName = targetName,
                TargetProductPrice = targetPrice,
                PayDifference = payDifference
            });
        }

        // AJAX: Đặt lịch hẹn thẩm định & giữ giá
        [HttpPost]
        public async Task<IActionResult> Book([FromBody] TradeInBookingRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ họ tên và số điện thoại liên hệ." });
            }

            var user = await GetCurrentUserAsync();

            // Sinh mã phiếu thu cũ: CPS-TC-YYMM-XXXX
            string datePrefix = DateTime.UtcNow.ToString("yyMM");
            int todayCount = await _context.TradeInRequests
                .CountAsync(r => r.CreatedAt.Year == DateTime.UtcNow.Year && r.CreatedAt.Month == DateTime.UtcNow.Month);
            string requestCode = $"CPS-TC-{datePrefix}-{(todayCount + 1).ToString("D4")}";

            DateTime appointmentTime = request.AppointmentDate > DateTime.UtcNow 
                ? request.AppointmentDate 
                : DateTime.UtcNow.AddDays(1);

            bool isCashOut = string.Equals(request.ActionType, "CashOut", StringComparison.OrdinalIgnoreCase);

            var tradeInRequest = new TradeInRequest
            {
                RequestCode = requestCode,
                UserId = user?.Id,
                CustomerName = request.CustomerName.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                Email = !string.IsNullOrWhiteSpace(request.Email) ? request.Email.Trim() : user?.Email,
                OldDeviceName = request.OldDeviceName,
                OldDeviceStorage = request.OldDeviceStorage ?? string.Empty,
                ConditionGrade = request.ConditionGrade,
                ActionType = isCashOut ? "CashOut" : "Upgrade",
                SmemberTier = request.SmemberTier ?? "None",
                SmemberBonus = request.SmemberBonus,
                BaseEstimatePrice = request.BaseEstimatePrice,
                SubsidyAmount = isCashOut ? 0 : request.SubsidyAmount,
                TotalBenefit = request.TotalBenefit,
                TargetProductId = isCashOut ? null : request.TargetProductId,
                TargetProductName = isCashOut ? "Không đổi máy (Bán lại nhận tiền)" : request.TargetProductName,
                TargetProductPrice = isCashOut ? 0 : request.TargetProductPrice,
                PayDifference = isCashOut ? 0 : request.PayDifference,
                BranchStore = !string.IsNullOrWhiteSpace(request.BranchStore) ? request.BranchStore : _branchStores[0],
                AppointmentDate = appointmentTime,
                CustomerNote = request.CustomerNote,
                Status = 1, // 1: Đã xác nhận lịch hẹn
                CreatedAt = DateTime.UtcNow
            };

            _context.TradeInRequests.Add(tradeInRequest);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                requestCode = tradeInRequest.RequestCode,
                actionType = tradeInRequest.ActionType,
                customerName = tradeInRequest.CustomerName,
                phoneNumber = tradeInRequest.PhoneNumber,
                branchStore = tradeInRequest.BranchStore,
                appointmentDate = tradeInRequest.AppointmentDate.ToString("dd/MM/yyyy HH:mm"),
                smemberBonus = tradeInRequest.SmemberBonus.ToString("N0") + "₫",
                totalBenefit = tradeInRequest.TotalBenefit.ToString("N0") + "₫",
                payDifference = tradeInRequest.PayDifference.ToString("N0") + "₫",
                message = isCashOut ? "Đặt lịch bán lại thiết bị nhận tiền thành công!" : "Đặt lịch hẹn thu cũ đổi mới thành công!"
            });
        }

        // GET: /TradeIn/Tracking?code=CPS-TC-XXXX
        public async Task<IActionResult> Tracking(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return View(null);
            }

            var request = await _context.TradeInRequests
                .Include(r => r.TargetProduct)
                .FirstOrDefaultAsync(r => r.RequestCode == code.Trim() || r.PhoneNumber == code.Trim());

            return View(request);
        }
    }
}
