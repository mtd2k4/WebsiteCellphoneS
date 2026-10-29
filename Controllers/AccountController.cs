using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Helpers;
using WebsiteBanDienThoai.Models;
using WebsiteBanDienThoai.Models.ViewModels;

namespace WebsiteBanDienThoai.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
                return LocalRedirect(returnUrl ?? "/");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var identifier = model.Username.Trim().ToLower();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == identifier || u.PhoneNumber == identifier);

            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác.");
                return View(model);
            }

            if (!PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Tạo danh sách Claims xác thực
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("SmemberLevel", user.SmemberLevel),
                new Claim("SmemberPoints", user.SmemberPoints.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            TempData["Success"] = $"Đăng nhập thành công! Chào mừng {user.FullName} ({user.Role}).";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var cleanEmail = model.Email.Trim().ToLower();
            var cleanPhone = model.PhoneNumber.Trim();

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == cleanEmail))
            {
                ModelState.AddModelError("Email", "Địa chỉ email này đã được đăng ký tài khoản khác.");
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.PhoneNumber == cleanPhone))
            {
                ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được đăng ký tài khoản khác.");
                return View(model);
            }

            var newUser = new AppUser
            {
                FullName = model.FullName.Trim(),
                Email = cleanEmail,
                PhoneNumber = cleanPhone,
                PasswordHash = PasswordHelper.HashPassword(model.Password),
                Role = "Customer",
                SmemberLevel = "S-New",
                SmemberPoints = 50, // Tặng 50 điểm khởi đầu
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Tự động đăng nhập sau khi đăng ký
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, newUser.Id.ToString()),
                new Claim(ClaimTypes.Name, newUser.FullName),
                new Claim(ClaimTypes.Email, newUser.Email),
                new Claim(ClaimTypes.MobilePhone, newUser.PhoneNumber),
                new Claim(ClaimTypes.Role, newUser.Role),
                new Claim("SmemberLevel", newUser.SmemberLevel),
                new Claim("SmemberPoints", newUser.SmemberPoints.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });

            TempData["Success"] = "Chào mừng bạn gia nhập hội viên Smember của CellphoneS! Bạn được tặng 50 điểm tích luỹ.";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // POST / GET: /Account/Logout
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Bạn đã đăng xuất tài khoản thành công!";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            return View();
        }

        // GET: /Account/Profile (Trang cá nhân Smember đa tính năng)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile(string tab = "overview")
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(nameof(Login));
            }

            // Lấy danh sách đơn hàng của người dùng kèm thông tin sản phẩm
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.PhoneNumber == user.PhoneNumber || o.Email == user.Email)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var vm = new ProfileViewModel
            {
                User = user,
                ActiveTab = tab.ToLower(),
                Orders = orders
            };

            // 1. Tính toán chi tiêu & hoàn thành
            var completedOrders = orders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Shipping).ToList();
            vm.TotalSpent = completedOrders.Sum(o => o.TotalAmount);
            vm.CompletedOrdersCount = completedOrders.Count;

            // 2. Tính toán Hạng thành viên & Tiến trình
            if (vm.TotalSpent < 3000000)
            {
                vm.CurrentTierName = "S-New";
                vm.NextTierName = "S-Mem";
                vm.NextTierTarget = 3000000;
                vm.AmountNeededForNextTier = 3000000 - vm.TotalSpent;
                vm.TierProgressPercent = (int)Math.Min(100, Math.Round(vm.TotalSpent / 3000000 * 100));
            }
            else if (vm.TotalSpent < 15000000)
            {
                vm.CurrentTierName = "S-Mem";
                vm.NextTierName = "S-VIP";
                vm.NextTierTarget = 15000000;
                vm.AmountNeededForNextTier = 15000000 - vm.TotalSpent;
                vm.TierProgressPercent = (int)Math.Min(100, Math.Round((vm.TotalSpent - 3000000) / 12000000 * 100));
            }
            else
            {
                vm.CurrentTierName = "S-VIP";
                vm.NextTierName = "S-VIP Đẳng Cấp Nhất";
                vm.NextTierTarget = 15000000;
                vm.AmountNeededForNextTier = 0;
                vm.TierProgressPercent = 100;
            }

            // Đồng bộ hạng thẻ của người dùng
            if (user.Role == "Admin")
            {
                vm.CurrentTierName = "S-VIP";
            }
            else if (user.SmemberLevel != vm.CurrentTierName)
            {
                user.SmemberLevel = vm.CurrentTierName;
                await _context.SaveChangesAsync();
            }

            // 3. Xây dựng danh sách Phiếu bảo hành điện tử chính thức từ Database
            var userWarranties = await _context.WarrantyCards
                .Where(w => w.UserId == user.Id || w.PhoneNumber == user.PhoneNumber || (user.Email != null && w.Email == user.Email))
                .OrderByDescending(w => w.PurchaseDate)
                .ToListAsync();

            // Nếu người dùng có đơn hàng hoàn thành nhưng chưa có phiếu bảo hành, tự động đồng bộ
            if (completedOrders.Any())
            {
                bool hasNewCard = false;
                foreach (var ord in completedOrders)
                {
                    foreach (var detail in ord.OrderDetails)
                    {
                        var serial = $"IMEI-35{ord.Id:D4}{detail.ProductId:D3}892";
                        if (!userWarranties.Any(w => w.SerialNumber == serial || (w.OrderId == ord.Id && w.ProductId == detail.ProductId)))
                        {
                            var wCard = new WarrantyCard
                            {
                                WarrantyCode = $"BH-{ord.CreatedAt:yyyyMMdd}-{ord.Id:D3}{detail.ProductId:D3}",
                                OrderId = ord.Id,
                                OrderCode = ord.OrderCode,
                                OrderDetailId = detail.Id,
                                ProductId = detail.ProductId,
                                ProductVariantId = detail.ProductVariantId,
                                UserId = user.Id,
                                CustomerName = user.FullName,
                                PhoneNumber = user.PhoneNumber,
                                Email = user.Email,
                                Address = user.Address,
                                ProductName = detail.Product?.Name ?? "Thiết bị chính hãng CellphoneS",
                                VariantInfo = detail.VariantInfo,
                                ProductImageUrl = detail.Product?.ImageUrl,
                                SerialNumber = serial,
                                WarrantyPackage = detail.Product?.Warranty ?? "12 tháng chính hãng CellphoneS",
                                WarrantyPeriodMonths = 12,
                                PurchaseDate = ord.CreatedAt,
                                StartDate = ord.CreatedAt,
                                EndDate = ord.CreatedAt.AddMonths(12),
                                ServiceCenter = "Điện Thoại Vui AASP - 136 Nguyễn Thái Học, Quận 1, TP.HCM (Hotline: 1800.2064)",
                                Status = DateTime.UtcNow <= ord.CreatedAt.AddMonths(12) ? WarrantyCardStatus.Active : WarrantyCardStatus.Expired,
                                CreatedAt = ord.CreatedAt
                            };
                            _context.WarrantyCards.Add(wCard);
                            userWarranties.Add(wCard);
                            hasNewCard = true;
                        }
                    }
                }
                if (hasNewCard)
                {
                    await _context.SaveChangesAsync();
                }
            }

            vm.WarrantyCards = userWarranties;

            // Đồng bộ sang WarrantyList
            foreach (var card in userWarranties)
            {
                vm.WarrantyList.Add(new WarrantyItemViewModel
                {
                    ProductName = card.ProductName,
                    ImageUrl = card.ProductImageUrl,
                    SerialNumber = card.SerialNumber,
                    OrderCode = card.OrderCode ?? card.WarrantyCode,
                    PurchaseDate = card.PurchaseDate,
                    WarrantyPeriod = card.WarrantyPackage,
                    ExpiryDate = card.EndDate,
                    ServiceCenter = card.ServiceCenter
                });
            }

            // 4. Danh sách thẩm định Thu cũ đổi mới từ Database
            var dbTradeIns = await _context.TradeInRequests
                .Where(t => t.UserId == user.Id || t.PhoneNumber == user.PhoneNumber || (user.Email != null && t.Email == user.Email))
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            if (dbTradeIns.Any())
            {
                foreach (var tc in dbTradeIns)
                {
                    vm.TradeInList.Add(new TradeInItemViewModel
                    {
                        RequestCode = tc.RequestCode,
                        DeviceName = $"{tc.OldDeviceName} {tc.OldDeviceStorage}",
                        Condition = tc.ConditionName,
                        ActionType = tc.ActionType,
                        EstimatedValue = tc.BaseEstimatePrice,
                        SmemberBonus = tc.SmemberBonus,
                        SubsidyAmount = tc.SubsidyAmount,
                        Status = tc.StatusText,
                        CreatedDate = tc.CreatedAt,
                        Branch = tc.BranchStore
                    });
                }
            }
            else
            {
                vm.TradeInList.Add(new TradeInItemViewModel
                {
                    RequestCode = "TC-" + (user.Id * 1000 + 192),
                    DeviceName = "iPhone 14 Pro Max 128GB",
                    Condition = "Loại 1 (Máy đẹp 99%)",
                    EstimatedValue = 17000000,
                    SubsidyAmount = 2000000,
                    Status = "Đã xác nhận lịch hẹn",
                    CreatedDate = DateTime.UtcNow.AddDays(-2),
                    Branch = "CellphoneS 213 Trần Quang Khải, P. Tân Định, Quận 1, TP.HCM"
                });
            }

            // 4.5. Danh sách lịch hẹn sửa chữa Điện Thoại Vui / CellphoneS
            vm.RepairBookings = await _context.RepairBookings
                .Where(r => r.UserId == user.Id || r.PhoneNumber == user.PhoneNumber || (user.Email != null && r.Email == user.Email))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // 5. Kho Voucher ưu đãi Smember
            vm.Vouchers.Add(new SmemberVoucherViewModel
            {
                Code = "CPSVIP500K",
                Title = "Voucher Hội Viên Smember VIP",
                DiscountText = "Giảm 500.000₫",
                Condition = "Đơn hàng điện thoại / tablet từ 10 triệu",
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                Scope = "Điện thoại, Laptop, Tablet"
            });

            vm.Vouchers.Add(new SmemberVoucherViewModel
            {
                Code = "PHUKIEN100K",
                Title = "Ưu đãi Phụ kiện độc quyền",
                DiscountText = "Giảm 100.000₫",
                Condition = "Đơn hàng phụ kiện cáp sạc, loa, tai nghe từ 500k",
                ExpiryDate = DateTime.UtcNow.AddDays(45),
                Scope = "Phụ kiện & Âm thanh"
            });

            vm.Vouchers.Add(new SmemberVoucherViewModel
            {
                Code = "THUCUOI1TR",
                Title = "Trợ giá Lên đời iPhone 16",
                DiscountText = "Trợ giá thêm 1.000.000₫",
                Condition = "Áp dụng đồng thời cùng chương trình Thu cũ đổi mới",
                ExpiryDate = DateTime.UtcNow.AddDays(60),
                Scope = "Chương trình Thu cũ đổi mới"
            });

            vm.Vouchers.Add(new SmemberVoucherViewModel
            {
                Code = "FREESHIP50K",
                Title = "Miễn phí Vận chuyển Toàn quốc",
                DiscountText = "Freeship 50.000₫",
                Condition = "Đơn hàng phụ kiện / gia dụng từ 300.000₫",
                ExpiryDate = DateTime.UtcNow.AddDays(90),
                Scope = "Giao hàng tận nơi"
            });

            return View(vm);
        }

        // POST: /Account/UpdateProfile
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string email, string? address, string? province, string? gender, DateTime? birthday)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                TempData["Error"] = "Họ và tên không được để trống!";
                return RedirectToAction(nameof(Profile), new { tab = "account" });
            }

            // Kiểm tra trùng email nếu thay đổi
            var cleanEmail = email.Trim().ToLower();
            if (cleanEmail != user.Email.ToLower() && await _context.Users.AnyAsync(u => u.Id != user.Id && u.Email.ToLower() == cleanEmail))
            {
                TempData["Error"] = "Địa chỉ email này đã được sử dụng bởi tài khoản khác!";
                return RedirectToAction(nameof(Profile), new { tab = "account" });
            }

            user.FullName = fullName.Trim();
            user.Email = cleanEmail;
            user.Address = address?.Trim();
            user.Province = string.IsNullOrWhiteSpace(province) ? "Hồ Chí Minh" : province.Trim();
            user.Gender = gender ?? "Nam";
            user.Birthday = birthday;

            await _context.SaveChangesAsync();

            // Cập nhật lại Identity Cookie để phản ánh tên mới ngay lập tức
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("SmemberLevel", user.SmemberLevel),
                new Claim("SmemberPoints", user.SmemberPoints.ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });

            TempData["Success"] = "Cập nhật thông tin cá nhân thành công!";
            return RedirectToAction(nameof(Profile), new { tab = "account" });
        }

        // POST: /Account/ChangePassword
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmNewPassword)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (!PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash))
            {
                TempData["Error"] = "Mật khẩu hiện tại không chính xác!";
                return RedirectToAction(nameof(Profile), new { tab = "account" });
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "Mật khẩu mới phải từ 6 ký tự trở lên!";
                return RedirectToAction(nameof(Profile), new { tab = "account" });
            }

            if (newPassword != confirmNewPassword)
            {
                TempData["Error"] = "Mật khẩu mới và mật khẩu xác nhận không trùng khớp!";
                return RedirectToAction(nameof(Profile), new { tab = "account" });
            }

            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công! Hãy bảo mật mật khẩu mới của bạn.";
            return RedirectToAction(nameof(Profile), new { tab = "account" });
        }
    }
}
