using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;
using WebsiteBanDienThoai.Services;

namespace WebsiteBanDienThoai.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private const string CartSessionKey = "Cart";

        public CartController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }

        private void SaveCart(List<CartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString(CartSessionKey, cartJson);
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, int? variantId = null)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
            }

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(i => i.ProductId == productId && i.VariantId == variantId);

            decimal price = product.FlashSalePrice ?? product.SalePrice;
            string? variantInfo = null;

            if (variantId.HasValue && product.Variants.Any())
            {
                var variant = product.Variants.FirstOrDefault(v => v.Id == variantId.Value);
                if (variant != null)
                {
                    price = variant.Price;
                    variantInfo = $"{variant.ColorName} - {variant.Storage}";
                }
            }

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductSlug = product.Slug,
                    ImageUrl = product.ImageUrl,
                    VariantId = variantId,
                    VariantInfo = variantInfo,
                    Price = price,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
            var totalCount = cart.Sum(i => i.Quantity);

            return Json(new { success = true, totalCount });
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
                SaveCart(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET/POST: /Cart/Remove
        public IActionResult Remove(int productId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Cart/Checkout
        [Authorize]
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Cart = cart;
            ViewBag.TotalAmount = cart.Sum(i => i.TotalPrice);

            var order = new Order();
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int uid))
                {
                    var user = _context.Users.Find(uid);
                    if (user != null)
                    {
                        order.CustomerName = user.FullName;
                        order.PhoneNumber = user.PhoneNumber;
                        order.Email = user.Email;
                        order.Province = user.Province ?? "Thành phố Hồ Chí Minh";
                        order.Address = user.Address ?? string.Empty;
                    }
                }
            }

            return View(order);
        }

        // POST: /Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove("OrderCode");

            if (!ModelState.IsValid)
            {
                ViewBag.Cart = cart;
                ViewBag.TotalAmount = cart.Sum(i => i.TotalPrice);
                return View(order);
            }

            // Kiểm tra số lượng tồn kho trước khi đặt hàng
            foreach (var item in cart)
            {
                var prod = await _context.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (prod == null)
                {
                    ModelState.AddModelError(string.Empty, $"Sản phẩm '{item.ProductName}' không còn tồn tại.");
                    ViewBag.Cart = cart;
                    ViewBag.TotalAmount = cart.Sum(i => i.TotalPrice);
                    return View(order);
                }

                if (item.VariantId.HasValue)
                {
                    var v = prod.Variants.FirstOrDefault(x => x.Id == item.VariantId.Value);
                    if (v != null && v.StockQuantity < item.Quantity)
                    {
                        ModelState.AddModelError(string.Empty, $"Phiên bản '{item.ProductName} ({v.ColorName} - {v.Storage})' chỉ còn {v.StockQuantity} sản phẩm trong kho, không đủ số lượng bạn đặt ({item.Quantity}).");
                        ViewBag.Cart = cart;
                        ViewBag.TotalAmount = cart.Sum(i => i.TotalPrice);
                        return View(order);
                    }
                }
                else if (prod.StockQuantity < item.Quantity)
                {
                    ModelState.AddModelError(string.Empty, $"Sản phẩm '{prod.Name}' chỉ còn {prod.StockQuantity} sản phẩm trong kho, không đủ số lượng bạn đặt ({item.Quantity}).");
                    ViewBag.Cart = cart;
                    ViewBag.TotalAmount = cart.Sum(i => i.TotalPrice);
                    return View(order);
                }
            }

            // Tính toán tổng tiền, phí vận chuyển và áp dụng mã giảm giá (nếu có)
            decimal subtotal = cart.Sum(i => i.TotalPrice);
            // Chính sách CellphoneS: Đơn hàng từ 300.000₫ trở lên mặc định Miễn phí giao hàng tiêu chuẩn (0₫)
            decimal shippingFee = subtotal >= 300000 ? 0 : 30000;
            decimal orderDiscount = 0;
            decimal shippingDiscount = 0;

            // 1. Áp dụng mã giảm giá đơn hàng
            if (!string.IsNullOrWhiteSpace(order.OrderDiscountCode))
            {
                var cleanOrderCode = order.OrderDiscountCode.Trim().ToUpper();
                var cOrder = await _context.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == cleanOrderCode && c.Type == CouponType.OrderDiscount && c.IsActive);
                if (cOrder != null && cOrder.ExpiryDate >= DateTime.UtcNow && subtotal >= cOrder.MinOrderAmount && cOrder.UsedCount < cOrder.UsageLimit)
                {
                    if (cOrder.IsPercentage)
                    {
                        orderDiscount = subtotal * (cOrder.DiscountValue / 100m);
                        if (cOrder.MaxDiscountAmount.HasValue && orderDiscount > cOrder.MaxDiscountAmount.Value)
                        {
                            orderDiscount = cOrder.MaxDiscountAmount.Value;
                        }
                    }
                    else
                    {
                        orderDiscount = cOrder.DiscountValue;
                    }

                    if (orderDiscount > subtotal) orderDiscount = subtotal;

                    order.OrderDiscountCode = cOrder.Code;
                    order.OrderDiscountAmount = orderDiscount;
                    cOrder.UsedCount++;
                }
                else
                {
                    order.OrderDiscountCode = null;
                    order.OrderDiscountAmount = 0;
                }
            }
            else
            {
                order.OrderDiscountCode = null;
                order.OrderDiscountAmount = 0;
            }

            // 2. Áp dụng mã giảm giá phí ship
            if (!string.IsNullOrWhiteSpace(order.ShippingDiscountCode))
            {
                var cleanShipCode = order.ShippingDiscountCode.Trim().ToUpper();
                var cShip = await _context.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == cleanShipCode && c.Type == CouponType.ShippingDiscount && c.IsActive);
                if (cShip != null && cShip.ExpiryDate >= DateTime.UtcNow && subtotal >= cShip.MinOrderAmount && cShip.UsedCount < cShip.UsageLimit)
                {
                    if (cShip.IsPercentage)
                    {
                        shippingDiscount = shippingFee * (cShip.DiscountValue / 100m);
                    }
                    else
                    {
                        shippingDiscount = cShip.DiscountValue;
                    }

                    if (shippingDiscount > shippingFee) shippingDiscount = shippingFee;

                    order.ShippingDiscountCode = cShip.Code;
                    order.ShippingDiscountAmount = shippingDiscount;
                    cShip.UsedCount++;
                }
                else
                {
                    order.ShippingDiscountCode = null;
                    order.ShippingDiscountAmount = 0;
                }
            }
            else
            {
                order.ShippingDiscountCode = null;
                order.ShippingDiscountAmount = 0;
            }

            order.SubtotalAmount = subtotal;
            order.ShippingFee = shippingFee;
            order.TotalAmount = Math.Max(0, subtotal + (shippingFee - shippingDiscount) - orderDiscount);

            // Tạo mã đơn hàng ngẫu nhiên: CPS-YYYYMMDD-XXXX
            order.OrderCode = $"CPS{DateTime.Now:yyyyMMdd}{new Random().Next(1000, 9999)}";
            order.Status = OrderStatus.Pending;
            order.CreatedAt = DateTime.UtcNow;

            foreach (var item in cart)
            {
                var product = await _context.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (product != null)
                {
                    int beforeStock = product.StockQuantity;
                    product.StockQuantity = Math.Max(0, product.StockQuantity - item.Quantity);
                    int afterStock = product.StockQuantity;

                    ProductVariant? variant = null;
                    if (item.VariantId.HasValue)
                    {
                        variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId.Value);
                        if (variant != null)
                        {
                            variant.StockQuantity = Math.Max(0, variant.StockQuantity - item.Quantity);
                        }
                    }

                    _context.InventoryLogs.Add(new InventoryLog
                    {
                        ProductId = product.Id,
                        ProductVariantId = variant?.Id,
                        ChangeType = "Sale",
                        QuantityChange = -item.Quantity,
                        StockBefore = beforeStock,
                        StockAfter = afterStock,
                        ReferenceCode = order.OrderCode,
                        Note = $"Khách hàng {order.CustomerName} mua hàng thành công qua Website",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = item.ProductId,
                    ProductVariantId = item.VariantId,
                    VariantInfo = item.VariantInfo,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                });
            }

            _context.Orders.Add(order);

            int? currentUserId = null;
            // Tích luỹ điểm Smember cho tài khoản khách hàng đã đăng nhập
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int uid))
                {
                    currentUserId = uid;
                    var user = await _context.Users.FindAsync(uid);
                    if (user != null)
                    {
                        // 1 điểm cho mỗi 100.000₫ thanh toán
                        int earnedPoints = (int)(order.TotalAmount / 100000m);
                        if (earnedPoints > 0)
                        {
                            user.SmemberPoints += earnedPoints;
                        }

                        if (string.IsNullOrWhiteSpace(user.Address) && !string.IsNullOrWhiteSpace(order.Address))
                        {
                            user.Address = order.Address;
                        }
                        if (string.IsNullOrWhiteSpace(user.Province) && !string.IsNullOrWhiteSpace(order.Province))
                        {
                            user.Province = order.Province;
                        }
                    }
                }
            }

            // Lưu đơn hàng trước để lấy Order.Id chính thức từ Database
            await _context.SaveChangesAsync();

            // 1. Tạo thông báo gửi cho Quản Trị Viên (Admin)
            var adminNotification = new AppNotification
            {
                Type = NotificationType.NewOrder,
                Title = $"Đơn hàng mới: #{order.OrderCode}",
                Message = $"Khách hàng {order.CustomerName} ({order.PhoneNumber}) vừa đặt đơn hàng trị giá {order.TotalAmount:N0}₫ ({order.PaymentMethod}).",
                TargetUrl = $"/Admin/Order/Details/{order.Id}",
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                RecipientRole = "Admin",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(adminNotification);

            // 2. Tạo thông báo đặt hàng thành công gửi cho Khách Hàng (Customer)
            var customerNotification = new AppNotification
            {
                Type = NotificationType.NewOrder,
                Title = $"Đặt hàng thành công #{order.OrderCode}",
                Message = $"Đơn hàng #{order.OrderCode} trị giá {order.TotalAmount:N0}₫ đã được tiếp nhận thành công. CellphoneS sẽ sớm liên hệ giao hàng cho bạn!",
                TargetUrl = $"/Cart/Tracking?orderCode={order.OrderCode}&phone={order.PhoneNumber}",
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                UserId = currentUserId,
                RecipientRole = "Customer",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(customerNotification);

            await _context.SaveChangesAsync();

            // 3. Gửi Email qua SMTP cho cả Khách hàng và Admin
            await _emailService.SendOrderEmailsAsync(order, cart, (int)(order.TotalAmount / 100000m));

            TempData["Success"] = $"Đặt hàng thành công! Đã gửi thông báo & email xác nhận đơn #{order.OrderCode}.";

            // Xóa giỏ hàng
            HttpContext.Session.Remove(CartSessionKey);

            return RedirectToAction(nameof(OrderConfirmation), new { id = order.Id });
        }

        // GET: /Cart/OrderConfirmation/5
        public async Task<IActionResult> OrderConfirmation(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: /Cart/Tracking?orderCode=...&phone=...
        public async Task<IActionResult> Tracking(string? orderCode, string? phone)
        {
            List<Order> orders = new();

            if (!string.IsNullOrWhiteSpace(orderCode))
            {
                orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .Where(o => o.OrderCode.ToLower() == orderCode.Trim().ToLower())
                    .ToListAsync();
            }
            else if (!string.IsNullOrWhiteSpace(phone))
            {
                orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .Where(o => o.PhoneNumber.Contains(phone.Trim()))
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }

            ViewBag.SearchCode = orderCode;
            ViewBag.SearchPhone = phone;

            return View(orders);
        }

        // GET: /Cart/GetAvailableCoupons
        [HttpGet]
        public async Task<IActionResult> GetAvailableCoupons()
        {
            var cart = GetCart();
            var subtotal = cart.Sum(i => i.TotalPrice);

            var now = DateTime.UtcNow;
            var coupons = await _context.Coupons
                .Where(c => c.IsActive && c.ExpiryDate >= now)
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
                    expiryDate = c.ExpiryDate.ToString("dd/MM/yyyy"),
                    isEligible = subtotal >= c.MinOrderAmount
                })
                .ToListAsync();

            return Json(new { success = true, subtotal, coupons });
        }

        // POST: /Cart/ValidateCoupons
        [HttpPost]
        public async Task<IActionResult> ValidateCoupons(string? orderCode, string? shippingCode)
        {
            var cart = GetCart();
            var subtotal = cart.Sum(i => i.TotalPrice);
            // Chính sách CellphoneS: Đơn hàng từ 300.000₫ trở lên mặc định Miễn phí giao hàng tiêu chuẩn (0₫)
            decimal shippingFee = subtotal >= 300000 ? 0 : 30000;

            decimal orderDiscountAmount = 0;
            object? orderCouponResult = null;

            decimal shippingDiscountAmount = 0;
            object? shippingCouponResult = null;

            var now = DateTime.UtcNow;

            // 1. Kiểm tra mã đơn hàng
            if (!string.IsNullOrWhiteSpace(orderCode))
            {
                var code = orderCode.Trim().ToUpper();
                var c = await _context.Coupons.FirstOrDefaultAsync(x => x.Code.ToUpper() == code && x.Type == CouponType.OrderDiscount);
                if (c == null)
                {
                    orderCouponResult = new { valid = false, message = $"Mã đơn hàng '{code}' không tồn tại." };
                }
                else if (!c.IsActive || c.ExpiryDate < now)
                {
                    orderCouponResult = new { valid = false, message = $"Mã '{code}' đã hết hạn hoặc tạm ngưng." };
                }
                else if (subtotal < c.MinOrderAmount)
                {
                    orderCouponResult = new { valid = false, message = $"Đơn hàng chưa đạt mức tối thiểu {c.MinOrderAmount:N0}₫ (Hiện tại: {subtotal:N0}₫)." };
                }
                else
                {
                    if (c.IsPercentage)
                    {
                        orderDiscountAmount = subtotal * (c.DiscountValue / 100m);
                        if (c.MaxDiscountAmount.HasValue && orderDiscountAmount > c.MaxDiscountAmount.Value)
                        {
                            orderDiscountAmount = c.MaxDiscountAmount.Value;
                        }
                    }
                    else
                    {
                        orderDiscountAmount = c.DiscountValue;
                    }
                    if (orderDiscountAmount > subtotal) orderDiscountAmount = subtotal;

                    orderCouponResult = new
                    {
                        valid = true,
                        code = c.Code,
                        title = c.Title,
                        discountAmount = orderDiscountAmount,
                        message = $"Áp dụng thành công: -{orderDiscountAmount:N0}₫"
                    };
                }
            }

            // 2. Kiểm tra mã vận chuyển
            if (!string.IsNullOrWhiteSpace(shippingCode))
            {
                var code = shippingCode.Trim().ToUpper();
                var c = await _context.Coupons.FirstOrDefaultAsync(x => x.Code.ToUpper() == code && x.Type == CouponType.ShippingDiscount);
                if (c == null)
                {
                    shippingCouponResult = new { valid = false, message = $"Mã freeship '{code}' không tồn tại." };
                }
                else if (!c.IsActive || c.ExpiryDate < now)
                {
                    shippingCouponResult = new { valid = false, message = $"Mã '{code}' đã hết hạn hoặc tạm ngưng." };
                }
                else if (subtotal < c.MinOrderAmount)
                {
                    shippingCouponResult = new { valid = false, message = $"Đơn hàng chưa đạt mức tối thiểu {c.MinOrderAmount:N0}₫ để nhận Freeship." };
                }
                else
                {
                    if (shippingFee == 0)
                    {
                        shippingDiscountAmount = 0;
                        shippingCouponResult = new
                        {
                            valid = true,
                            code = c.Code,
                            title = c.Title,
                            discountAmount = 0m,
                            message = "Đơn hàng đã được Miễn phí giao hàng (0₫)"
                        };
                    }
                    else
                    {
                        if (c.IsPercentage)
                        {
                            shippingDiscountAmount = shippingFee * (c.DiscountValue / 100m);
                        }
                        else
                        {
                            shippingDiscountAmount = c.DiscountValue;
                        }
                        if (shippingDiscountAmount > shippingFee) shippingDiscountAmount = shippingFee;

                        shippingCouponResult = new
                        {
                            valid = true,
                            code = c.Code,
                            title = c.Title,
                            discountAmount = shippingDiscountAmount,
                            message = $"Áp dụng thành công: -{shippingDiscountAmount:N0}₫ phí ship"
                        };
                    }
                }
            }

            decimal finalTotal = Math.Max(0, subtotal + (shippingFee - shippingDiscountAmount) - orderDiscountAmount);

            return Json(new
            {
                success = true,
                subtotal,
                shippingFee,
                orderCoupon = orderCouponResult,
                shippingCoupon = shippingCouponResult,
                orderDiscountAmount,
                shippingDiscountAmount,
                finalTotal
            });
        }
    }
}
