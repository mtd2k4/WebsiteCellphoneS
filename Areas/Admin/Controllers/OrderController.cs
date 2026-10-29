using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;
using WebsiteBanDienThoai.Services;

namespace WebsiteBanDienThoai.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public OrderController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: /Admin/Order
        public async Task<IActionResult> Index(string? search, OrderStatus? status, int page = 1)
        {
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(o => o.OrderCode.ToLower().Contains(s) ||
                                         o.CustomerName.ToLower().Contains(s) ||
                                         o.PhoneNumber.Contains(s));
            }

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            var totalItems = await query.CountAsync();
            int pageSize = 15;
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.TotalItems = totalItems;

            return View(orders);
        }

        // GET: /Admin/Order/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Tự động đánh dấu thông báo của đơn hàng này là đã đọc khi Admin xem chi tiết
            var unreadOrderNotifs = await _context.Notifications
                .Where(n => n.RecipientRole == "Admin" && n.OrderId == id && !n.IsRead)
                .ToListAsync();
            if (unreadOrderNotifs.Any())
            {
                foreach (var n in unreadOrderNotifs)
                {
                    n.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            return View(order);
        }

        // POST: /Admin/Order/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var oldStatus = order.Status;
            order.Status = newStatus;

            // Xử lý kho hàng khi huỷ đơn hoặc khôi phục đơn
            if (oldStatus != OrderStatus.Cancelled && newStatus == OrderStatus.Cancelled)
            {
                // Hoàn lại tồn kho cho sản phẩm & biến thể
                var details = await _context.OrderDetails.Where(od => od.OrderId == id).ToListAsync();
                foreach (var od in details)
                {
                    var product = await _context.Products.FindAsync(od.ProductId);
                    if (product != null)
                    {
                        int beforeStock = product.StockQuantity;
                        product.StockQuantity += od.Quantity;
                        int afterStock = product.StockQuantity;

                        if (od.ProductVariantId.HasValue)
                        {
                            var variant = await _context.ProductVariants.FindAsync(od.ProductVariantId.Value);
                            if (variant != null)
                            {
                                variant.StockQuantity += od.Quantity;
                            }
                        }

                        _context.InventoryLogs.Add(new InventoryLog
                        {
                            ProductId = product.Id,
                            ProductVariantId = od.ProductVariantId,
                            ChangeType = "Return",
                            QuantityChange = od.Quantity,
                            StockBefore = beforeStock,
                            StockAfter = afterStock,
                            ReferenceCode = order.OrderCode,
                            Note = $"Hoàn lại tồn kho do huỷ đơn hàng #{order.OrderCode}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            else if (oldStatus == OrderStatus.Cancelled && newStatus != OrderStatus.Cancelled)
            {
                // Khôi phục lại đơn đã huỷ -> trừ lại tồn kho
                var details = await _context.OrderDetails.Where(od => od.OrderId == id).ToListAsync();
                foreach (var od in details)
                {
                    var product = await _context.Products.FindAsync(od.ProductId);
                    if (product != null)
                    {
                        int beforeStock = product.StockQuantity;
                        product.StockQuantity = Math.Max(0, product.StockQuantity - od.Quantity);
                        int afterStock = product.StockQuantity;

                        if (od.ProductVariantId.HasValue)
                        {
                            var variant = await _context.ProductVariants.FindAsync(od.ProductVariantId.Value);
                            if (variant != null)
                            {
                                variant.StockQuantity = Math.Max(0, variant.StockQuantity - od.Quantity);
                            }
                        }

                        _context.InventoryLogs.Add(new InventoryLog
                        {
                            ProductId = product.Id,
                            ProductVariantId = od.ProductVariantId,
                            ChangeType = "Sale",
                            QuantityChange = -od.Quantity,
                            StockBefore = beforeStock,
                            StockAfter = afterStock,
                            ReferenceCode = order.OrderCode,
                            Note = $"Trừ lại tồn kho do phục hồi đơn hàng #{order.OrderCode}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            // Tự động cấp Phiếu bảo hành điện tử khi đơn hàng chuyển sang Hoàn thành (Completed)
            if (newStatus == OrderStatus.Completed)
            {
                var existingWarranties = await _context.WarrantyCards.Where(w => w.OrderId == id).ToListAsync();
                if (!existingWarranties.Any())
                {
                    var orderDetails = await _context.OrderDetails.Include(od => od.Product).Where(od => od.OrderId == id).ToListAsync();
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == order.PhoneNumber || (order.Email != null && u.Email == order.Email));

                    foreach (var od in orderDetails)
                    {
                        var pName = od.Product?.Name ?? "Thiết bị chính hãng CellphoneS";
                        var wCard = new WarrantyCard
                        {
                            WarrantyCode = $"BH-{DateTime.UtcNow:yyyyMMdd}-{order.Id:D3}{od.ProductId:D3}",
                            OrderId = order.Id,
                            OrderCode = order.OrderCode,
                            OrderDetailId = od.Id,
                            ProductId = od.ProductId,
                            ProductVariantId = od.ProductVariantId,
                            UserId = user?.Id,
                            CustomerName = order.CustomerName,
                            PhoneNumber = order.PhoneNumber,
                            Email = order.Email,
                            Address = order.Address,
                            ProductName = pName,
                            VariantInfo = od.VariantInfo,
                            ProductImageUrl = od.Product?.ImageUrl,
                            SerialNumber = $"IMEI-35{order.Id:D4}{od.ProductId:D3}892",
                            WarrantyPackage = od.Product?.Warranty ?? "12 tháng chính hãng CellphoneS",
                            WarrantyPeriodMonths = 12,
                            PurchaseDate = order.CreatedAt,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(12),
                            ServiceCenter = "Điện Thoại Vui AASP - 136 Nguyễn Thái Học, Quận 1, TP.HCM (Hotline: 1800.2064)",
                            Status = WarrantyCardStatus.Active,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.WarrantyCards.Add(wCard);
                    }
                }
            }

            // Gửi thông báo cập nhật tiến độ đơn hàng cho khách hàng
            var customerUser = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == order.PhoneNumber || (order.Email != null && u.Email == order.Email));
            var notifType = newStatus switch
            {
                OrderStatus.Confirmed => NotificationType.OrderConfirmed,
                OrderStatus.Shipping => NotificationType.OrderShipping,
                OrderStatus.Completed => NotificationType.OrderCompleted,
                OrderStatus.Cancelled => NotificationType.OrderCancelled,
                _ => NotificationType.NewOrder
            };

            var statusMsg = newStatus switch
            {
                OrderStatus.Confirmed => $"Đơn hàng #{order.OrderCode} đã được xác nhận và đang được đóng gói chuẩn bị giao.",
                OrderStatus.Shipping => $"Đơn hàng #{order.OrderCode} đang trên đường giao đến bạn. Vui lòng chú ý điện thoại!",
                OrderStatus.Completed => $"Đơn hàng #{order.OrderCode} đã được giao thành công! Phiếu bảo hành điện tử chính hãng đã được kích hoạt trên hệ thống.",
                OrderStatus.Cancelled => $"Đơn hàng #{order.OrderCode} đã được hủy theo yêu cầu.",
                _ => $"Đơn hàng #{order.OrderCode} đã được cập nhật trạng thái '{GetStatusText(newStatus)}'."
            };

            _context.Notifications.Add(new AppNotification
            {
                Type = notifType,
                Title = $"Đơn hàng #{order.OrderCode}: {GetStatusText(newStatus)}",
                Message = statusMsg,
                TargetUrl = $"/Cart/Tracking?orderCode={order.OrderCode}&phone={order.PhoneNumber}",
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                UserId = customerUser?.Id,
                RecipientRole = "Customer",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Gửi email SMTP thông báo cập nhật trạng thái cho khách hàng
            await _emailService.SendOrderStatusUpdateEmailAsync(order, GetStatusText(newStatus), statusMsg);

            TempData["Success"] = $"Đã cập nhật trạng thái đơn #{order.OrderCode} sang '{GetStatusText(newStatus)}' và gửi thông báo/email cho khách hàng!";
            return RedirectToAction(nameof(Details), new { id });
        }

        private string GetStatusText(OrderStatus status) => status switch
        {
            OrderStatus.Pending => "Chờ xử lý",
            OrderStatus.Confirmed => "Đã xác nhận",
            OrderStatus.Shipping => "Đang giao hàng",
            OrderStatus.Completed => "Hoàn thành",
            OrderStatus.Cancelled => "Đã huỷ",
            _ => status.ToString()
        };
    }
}
