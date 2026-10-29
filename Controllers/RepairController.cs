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
    public class RepairController : Controller
    {
        private readonly AppDbContext _context;

        public RepairController(AppDbContext context)
        {
            _context = context;
        }

        // Danh sách các chi nhánh Điện Thoại Vui / CellphoneS Care
        public static readonly List<string> Branches = new()
        {
            "Điện Thoại Vui ASP - 136 Nguyễn Thái Học, P. Phạm Ngũ Lão, Quận 1, TP.HCM",
            "Điện Thoại Vui - 300 Cầu Giấy, P. Dịch Vọng, Q. Cầu Giấy, Hà Nội",
            "Điện Thoại Vui - 19 Thái Hà, P. Trung Liệt, Q. Đống Đa, Hà Nội",
            "Điện Thoại Vui - 816 Hậu Giang, Phường 12, Quận 6, TP.HCM",
            "Điện Thoại Vui - 250 Hoàng Văn Thụ, Phường 4, Q. Tân Bình, TP.HCM",
            "Điện Thoại Vui - 363 Võ Văn Tần, Phường 5, Quận 3, TP.HCM",
            "Điện Thoại Vui - 129 Lê Văn Việt, P. Hiệp Phú, TP. Thủ Đức, TP.HCM",
            "Điện Thoại Vui - 365 Nguyễn Văn Cừ, P. Ngọc Lâm, Q. Long Biên, Hà Nội",
            "Điện Thoại Vui - 177 Nguyễn Văn Linh, P. Nam Dương, Q. Hải Châu, Đà Nẵng",
            "Điện Thoại Vui - 272 Đường 30/4, P. Hưng Lợi, Q. Ninh Kiều, Cần Thơ",
            "Điện Thoại Vui - 543 Nguyễn Thị Thập, P. Tân Phong, Quận 7, TP.HCM",
            "Điện Thoại Vui - 51 Quang Trung, Phường 10, Q. Gò Vấp, TP.HCM"
        };

        // Danh sách các dịch vụ sửa chữa phổ biến và chi phí ước tính
        public static readonly Dictionary<string, (decimal Price, string Time, string Icon)> Services = new()
        {
            { "Thay Pin chính hãng dung lượng cao", (450000, "15 - 30 phút", "bi-battery-charging") },
            { "Thay Màn hình OLED / Chính hãng", (950000, "30 - 60 phút", "bi-phone") },
            { "Ép kính / Thay mặt kính cảm ứng", (350000, "1 - 2 giờ", "bi-shield-check") },
            { "Sửa lỗi nguồn / Thay chân sạc", (390000, "30 - 60 phút", "bi-plug-fill") },
            { "Thay / Sửa Camera trước & sau", (490000, "30 - 45 phút", "bi-camera-fill") },
            { "Thay Loa trong / Loa ngoài / Mic", (320000, "30 phút", "bi-volume-up-fill") },
            { "Khắc phục lỗi phần mềm / Treo máy", (150000, "20 - 40 phút", "bi-cpu-fill") },
            { "Vệ sinh máy & Tra keo tản nhiệt (Miễn phí Smember)", (0, "15 phút", "bi-stars") },
            { "Khám bệnh & Thẩm định lỗi tổng quát (Miễn phí)", (0, "15 phút", "bi-clipboard2-pulse-fill") }
        };

        // GET: /Repair hoặc /Repair/Booking
        public async Task<IActionResult> Index(
            string? imei = null,
            string? serialNumber = null,
            string? productName = null,
            string? orderCode = null,
            bool isWarranty = false,
            string? service = null)
        {
            return await Booking(imei, serialNumber, productName, orderCode, isWarranty, service);
        }

        // GET: /Repair/Booking
        public async Task<IActionResult> Booking(
            string? imei = null,
            string? serialNumber = null,
            string? productName = null,
            string? orderCode = null,
            bool isWarranty = false,
            string? service = null)
        {
            var model = new RepairBooking
            {
                SerialNumber = !string.IsNullOrWhiteSpace(imei) ? imei : serialNumber,
                DeviceName = productName ?? string.Empty,
                OrderCode = orderCode,
                HasWarranty = isWarranty,
                ServiceType = service ?? "Thay Pin chính hãng dung lượng cao",
                BookingDate = DateTime.UtcNow.Date.AddDays(1),
                TimeSlot = "09:00 - 10:30",
                City = "Hồ Chí Minh",
                StoreBranch = Branches.First()
            };

            // Nếu người dùng đã đăng nhập, tự động điền thông tin và lấy danh sách máy đã mua
            List<WarrantyItemViewModel> userDevices = new();
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var identifier = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == identifier || u.Email == identifier);
                if (user != null)
                {
                    model.UserId = user.Id;
                    model.CustomerName = user.FullName;
                    model.PhoneNumber = user.PhoneNumber;
                    model.Email = user.Email;

                    // Lấy các thiết bị từ đơn hàng hoàn thành
                    var completedOrders = await _context.Orders
                        .Include(o => o.OrderDetails)
                            .ThenInclude(od => od.Product)
                        .Where(o => (o.CustomerName == user.FullName || o.PhoneNumber == user.PhoneNumber || (user.Email != null && o.Email == user.Email)) && o.Status == OrderStatus.Completed)
                        .OrderByDescending(o => o.CreatedAt)
                        .ToListAsync();

                    foreach (var ord in completedOrders)
                    {
                        foreach (var d in ord.OrderDetails)
                        {
                            userDevices.Add(new WarrantyItemViewModel
                            {
                                ProductName = d.Product?.Name ?? "Thiết bị chính hãng",
                                SerialNumber = $"IMEI-35{ord.Id:D4}{d.ProductId:D3}892",
                                OrderCode = ord.OrderCode,
                                ImageUrl = d.Product?.ImageUrl,
                                PurchaseDate = ord.CreatedAt,
                                ExpiryDate = ord.CreatedAt.AddMonths(12)
                            });
                        }
                    }
                }
            }

            ViewBag.UserDevices = userDevices;
            ViewBag.Branches = Branches;
            ViewBag.Services = Services;

            return View(model);
        }

        // POST: /Repair/Booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Booking(RepairBooking model)
        {
            if (string.IsNullOrWhiteSpace(model.CustomerName))
                ModelState.AddModelError("CustomerName", "Vui lòng nhập họ và tên");
            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
                ModelState.AddModelError("PhoneNumber", "Vui lòng nhập số điện thoại");
            if (string.IsNullOrWhiteSpace(model.DeviceName))
                ModelState.AddModelError("DeviceName", "Vui lòng nhập model hoặc chọn thiết bị");
            if (string.IsNullOrWhiteSpace(model.ServiceType))
                ModelState.AddModelError("ServiceType", "Vui lòng chọn dịch vụ sửa chữa");
            if (string.IsNullOrWhiteSpace(model.StoreBranch))
                ModelState.AddModelError("StoreBranch", "Vui lòng chọn chi nhánh tiếp nhận");

            ModelState.Remove(nameof(model.BookingCode));
            ModelState.Remove(nameof(model.User));

            if (!ModelState.IsValid)
            {
                ViewBag.Branches = Branches;
                ViewBag.Services = Services;
                return View(model);
            }

            // Gán người dùng nếu đang đăng nhập
            if (User.Identity != null && User.Identity.IsAuthenticated && !model.UserId.HasValue)
            {
                var identifier = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == identifier || u.Email == identifier);
                if (user != null)
                {
                    model.UserId = user.Id;
                }
            }

            // Tạo mã lịch hẹn độc nhất: DTV-YYYYMMDD-XXXX
            var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
            var rand = new Random().Next(1000, 9999);
            model.BookingCode = $"DTV-{dateStr}-{rand}";

            // Tính chi phí ước tính
            if (model.HasWarranty)
            {
                // Máy còn bảo hành chính hãng: miễn phí kiểm tra và sửa chữa bảo hành
                model.EstimatedCost = 0;
            }
            else if (Services.TryGetValue(model.ServiceType, out var sInfo))
            {
                model.EstimatedCost = sInfo.Price;
            }
            else
            {
                model.EstimatedCost = 350000;
            }

            model.Status = RepairStatus.Pending;
            model.CreatedAt = DateTime.UtcNow;

            _context.RepairBookings.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đặt lịch sửa chữa thành công! Mã phiếu hẹn của bạn là: {model.BookingCode}";
            return RedirectToAction(nameof(Confirmation), new { code = model.BookingCode });
        }

        // GET: /Repair/Confirmation
        public async Task<IActionResult> Confirmation(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return RedirectToAction(nameof(Booking));
            }

            var booking = await _context.RepairBookings
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.BookingCode == code.Trim());

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin lịch hẹn sửa chữa với mã đã cung cấp.";
                return RedirectToAction(nameof(Booking));
            }

            return View(booking);
        }

        // GET: /Repair/Tracking
        public async Task<IActionResult> Tracking(string? code = null, string? phone = null)
        {
            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(phone))
            {
                // Nếu người dùng đã đăng nhập, hiển thị các lịch hẹn của người đó
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    var identifier = User.Identity.Name;
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == identifier || u.Email == identifier);
                    if (user != null)
                    {
                        var userBookings = await _context.RepairBookings
                            .Where(r => r.UserId == user.Id || r.PhoneNumber == user.PhoneNumber)
                            .OrderByDescending(r => r.CreatedAt)
                            .ToListAsync();
                        ViewBag.UserBookings = userBookings;
                    }
                }
                return View((RepairBooking?)null);
            }

            var query = _context.RepairBookings.AsQueryable();
            if (!string.IsNullOrWhiteSpace(code))
            {
                var c = code.Trim();
                query = query.Where(r => r.BookingCode.ToLower() == c.ToLower());
            }
            if (!string.IsNullOrWhiteSpace(phone))
            {
                var p = phone.Trim();
                query = query.Where(r => r.PhoneNumber.Contains(p));
            }

            var item = await query.OrderByDescending(r => r.CreatedAt).FirstOrDefaultAsync();
            if (item == null)
            {
                ViewBag.NotFound = true;
                ViewBag.SearchCode = code;
                ViewBag.SearchPhone = phone;
            }

            return View(item);
        }

        // POST: /Repair/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _context.RepairBookings.FindAsync(id);
            if (booking != null && (booking.Status == RepairStatus.Pending || booking.Status == RepairStatus.Received))
            {
                booking.Status = RepairStatus.Cancelled;
                booking.TechnicianNote = $"Khách hàng đã hủy lịch hẹn lúc {DateTime.UtcNow:dd/MM/yyyy HH:mm}";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã hủy lịch hẹn {booking.BookingCode} thành công.";
            }

            return RedirectToAction(nameof(Confirmation), new { code = booking?.BookingCode });
        }

        // GET: /Repair/CheckWarrantyByImei?imei=...
        [HttpGet]
        public async Task<IActionResult> CheckWarrantyByImei(string? imei)
        {
            if (string.IsNullOrWhiteSpace(imei))
            {
                return Json(new { success = false, message = "Vui lòng nhập số IMEI hoặc số Serial." });
            }

            var cleanImei = imei.Trim();
            var rawImei = cleanImei.Replace("IMEI-", "", StringComparison.OrdinalIgnoreCase)
                                   .Replace("IMEI", "", StringComparison.OrdinalIgnoreCase)
                                   .Replace("-", "").Trim();

            // 1. Tìm trong bảng WarrantyCards theo SerialNumber hoặc WarrantyCode (hỗ trợ cả có/không tiền tố IMEI- và chuỗi nhập)
            var card = await _context.WarrantyCards
                .Include(w => w.Product)
                .Where(w => w.SerialNumber.ToLower() == cleanImei.ToLower() ||
                            w.SerialNumber.ToLower() == ("imei-" + rawImei.ToLower()) ||
                            w.SerialNumber.Replace("-", "").ToLower() == rawImei.ToLower() ||
                            w.WarrantyCode.ToLower() == cleanImei.ToLower() ||
                            (cleanImei.Length >= 8 && w.SerialNumber.ToLower().Contains(cleanImei.ToLower())) ||
                            (rawImei.Length >= 8 && w.SerialNumber.ToLower().Contains(rawImei.ToLower())) ||
                            (w.OrderCode != null && w.OrderCode.ToLower() == cleanImei.ToLower()))
                .OrderByDescending(w => w.EndDate)
                .FirstOrDefaultAsync();

            if (card != null)
            {
                var isValid = card.IsActiveNow && card.Status != WarrantyCardStatus.Void;
                return Json(new
                {
                    success = true,
                    found = true,
                    hasWarranty = isValid,
                    warrantyCode = card.WarrantyCode,
                    productName = card.ProductName,
                    variantInfo = card.VariantInfo ?? "",
                    package = card.WarrantyPackage,
                    purchaseDate = card.PurchaseDate.ToString("dd/MM/yyyy"),
                    startDate = card.StartDate.ToString("dd/MM/yyyy"),
                    endDate = card.EndDate.ToString("dd/MM/yyyy"),
                    remainingDays = card.RemainingDays,
                    serviceCenter = card.ServiceCenter,
                    status = (int)card.EffectiveStatus,
                    statusText = card.EffectiveStatus switch
                    {
                        WarrantyCardStatus.Active => "Còn hạn bảo hành",
                        WarrantyCardStatus.ExpiringSoon => "Sắp hết hạn",
                        WarrantyCardStatus.Expired => "Đã hết hạn",
                        WarrantyCardStatus.Void => "Đã bị hủy / Từ chối",
                        _ => "Không xác định"
                    },
                    message = isValid
                        ? $"Thiết bị còn bảo hành chính hãng CellphoneS đến ngày {card.EndDate:dd/MM/yyyy} ({card.WarrantyPackage}). Miễn phí công kiểm tra & thay thế linh kiện lỗi do NSX."
                        : (card.Status == WarrantyCardStatus.Void
                            ? $"Phiếu bảo hành {card.WarrantyCode} đã bị hủy hoặc từ chối bảo hành. Thiết bị áp dụng biểu phí sửa chữa dịch vụ tiêu chuẩn."
                            : $"Phiếu bảo hành {card.WarrantyCode} đã HẾT HẠN từ ngày {card.EndDate:dd/MM/yyyy}. Áp dụng bảng giá sửa chữa dịch vụ tiêu chuẩn.")
                });
            }

            // 2. Dự phòng: Kiểm tra theo mã đơn hàng đã hoàn thành trong hệ thống
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                .Where(od => od.Order != null && od.Order.Status == OrderStatus.Completed &&
                             (od.Order.OrderCode.ToLower() == cleanImei.ToLower() ||
                              cleanImei.ToLower().Contains(od.Order.OrderCode.ToLower())))
                .OrderByDescending(od => od.Order!.CreatedAt)
                .FirstOrDefaultAsync();

            if (orderDetail != null)
            {
                var purchaseDate = orderDetail.Order!.CreatedAt;
                var expiryDate = purchaseDate.AddMonths(12);
                var isValid = expiryDate >= DateTime.UtcNow;

                return Json(new
                {
                    success = true,
                    found = true,
                    hasWarranty = isValid,
                    warrantyCode = $"BH-{orderDetail.Order.OrderCode}",
                    productName = orderDetail.Product?.Name ?? "Thiết bị chính hãng CellphoneS",
                    variantInfo = "",
                    package = "12 tháng chính hãng CellphoneS",
                    purchaseDate = purchaseDate.ToString("dd/MM/yyyy"),
                    startDate = purchaseDate.ToString("dd/MM/yyyy"),
                    endDate = expiryDate.ToString("dd/MM/yyyy"),
                    remainingDays = Math.Max(0, (expiryDate - DateTime.UtcNow).Days),
                    serviceCenter = "Hệ thống bảo hành CellphoneS & Điện Thoại Vui toàn quốc",
                    status = isValid ? 0 : 2,
                    statusText = isValid ? "Còn hạn bảo hành" : "Đã hết hạn",
                    message = isValid
                        ? $"Thiết bị thuộc đơn hàng #{orderDetail.Order.OrderCode}, còn hạn bảo hành đến ngày {expiryDate:dd/MM/yyyy}."
                        : $"Thiết bị thuộc đơn hàng #{orderDetail.Order.OrderCode}, nhưng đã hết hạn bảo hành từ ngày {expiryDate:dd/MM/yyyy}."
                });
            }

            // 3. Không tìm thấy phiếu bảo hành
            return Json(new
            {
                success = true,
                found = false,
                hasWarranty = false,
                message = "Không tìm thấy thông tin bảo hành tại CellphoneS cho số IMEI này. Thiết bị sẽ được tiếp nhận theo diện sửa chữa dịch vụ tính phí."
            });
        }
    }
}
