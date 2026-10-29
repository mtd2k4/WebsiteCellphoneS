using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class WarehouseController : Controller
    {
        private readonly AppDbContext _context;

        public WarehouseController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. TỔNG QUAN KHO & DANH SÁCH PHIẾU NHẬP
        // ==========================================
        public async Task<IActionResult> Index(string? search, DateTime? fromDate, DateTime? toDate, string? tab = "receipts", int page = 1)
        {
            ViewBag.ActiveTab = tab ?? "receipts";
            ViewBag.CurrentSearch = search;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;

            // Thống kê tổng quan kho hàng
            var allProducts = await _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .ToListAsync();

            ViewBag.TotalProducts = allProducts.Count;
            ViewBag.TotalStockCount = allProducts.Sum(p => p.StockQuantity);
            ViewBag.TotalInventoryValue = allProducts.Sum(p => (decimal)p.StockQuantity * p.SalePrice);
            ViewBag.LowStockCount = allProducts.Count(p => p.StockQuantity > 0 && p.StockQuantity <= 10);
            ViewBag.OutOfStockCount = allProducts.Count(p => p.StockQuantity == 0);
            ViewBag.TotalReceiptsCount = await _context.WarehouseReceipts.CountAsync();

            int pageSize = 15;

            if (tab == "inventory")
            {
                // Tab tồn kho hiện tại theo sản phẩm
                var prodQuery = allProducts.AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim().ToLower();
                    prodQuery = prodQuery.Where(p => p.Name.ToLower().Contains(s) || (p.Brand != null && p.Brand.Name.ToLower().Contains(s)));
                }

                var totalProdItems = prodQuery.Count();
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalProdItems / pageSize);
                ViewBag.ProductList = prodQuery
                    .OrderBy(p => p.StockQuantity)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return View();
            }
            else if (tab == "logs")
            {
                // Tab lịch sử biến động kho (Thẻ kho)
                var logQuery = _context.InventoryLogs
                    .Include(l => l.Product)
                    .Include(l => l.ProductVariant)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim().ToLower();
                    logQuery = logQuery.Where(l => (l.Product != null && l.Product.Name.ToLower().Contains(s)) ||
                                                   (l.ReferenceCode != null && l.ReferenceCode.ToLower().Contains(s)) ||
                                                   (l.Note != null && l.Note.ToLower().Contains(s)));
                }

                if (fromDate.HasValue)
                {
                    logQuery = logQuery.Where(l => l.CreatedAt >= fromDate.Value.Date);
                }
                if (toDate.HasValue)
                {
                    logQuery = logQuery.Where(l => l.CreatedAt <= toDate.Value.Date.AddDays(1).AddTicks(-1));
                }

                var totalLogs = await logQuery.CountAsync();
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalLogs / pageSize);
                ViewBag.InventoryLogs = await logQuery
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return View();
            }
            else
            {
                // Tab danh sách phiếu nhập kho (Mặc định)
                var receiptQuery = _context.WarehouseReceipts
                    .Include(r => r.Details)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim().ToLower();
                    receiptQuery = receiptQuery.Where(r => r.ReceiptCode.ToLower().Contains(s) ||
                                                           r.SupplierName.ToLower().Contains(s) ||
                                                           (r.CreatedBy != null && r.CreatedBy.ToLower().Contains(s)));
                }

                if (fromDate.HasValue)
                {
                    receiptQuery = receiptQuery.Where(r => r.ImportDate >= fromDate.Value.Date);
                }
                if (toDate.HasValue)
                {
                    receiptQuery = receiptQuery.Where(r => r.ImportDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
                }

                var totalReceipts = await receiptQuery.CountAsync();
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalReceipts / pageSize);
                var receipts = await receiptQuery
                    .OrderByDescending(r => r.ImportDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return View(receipts);
            }
        }

        // ==========================================
        // 2. TẠO PHIẾU NHẬP HÀNG MỚI
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Create(int? productId = null)
        {
            ViewBag.PreselectedProductId = productId;
            var prods = await _context.Products
                .OrderBy(p => p.Name)
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    stock = p.StockQuantity,
                    price = p.SalePrice,
                    image = p.ImageUrl
                })
                .ToListAsync();

            ViewBag.ProductsJson = System.Text.Json.JsonSerializer.Serialize(prods);

            // Sinh mã phiếu nhập tự động: PNK-YYYYMMDD-XXXX
            ViewBag.GeneratedReceiptCode = $"PNK{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

            return View();
        }

        public class CreateReceiptItemDto
        {
            public int ProductId { get; set; }
            public int? ProductVariantId { get; set; }
            public string? VariantInfo { get; set; }
            public int Quantity { get; set; }
            public decimal ImportPrice { get; set; }
            public string? Note { get; set; }
        }

        public class CreateReceiptRequest
        {
            public string ReceiptCode { get; set; } = string.Empty;
            public string SupplierName { get; set; } = string.Empty;
            public string? SupplierPhone { get; set; }
            public string? SupplierAddress { get; set; }
            public DateTime? ImportDate { get; set; }
            public string? Note { get; set; }
            public bool IsDraft { get; set; } = false;
            public List<CreateReceiptItemDto> Items { get; set; } = new();
        }

        [HttpPost]
        public async Task<IActionResult> CreateReceipt([FromBody] CreateReceiptRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SupplierName))
            {
                return Json(new { success = false, message = "Vui lòng nhập tên nhà cung cấp." });
            }

            if (request.Items == null || !request.Items.Any(i => i.Quantity > 0))
            {
                return Json(new { success = false, message = "Phiếu nhập phải có ít nhất 1 sản phẩm với số lượng lớn hơn 0." });
            }

            // Đảm bảo mã phiếu nhập duy nhất
            var receiptCode = string.IsNullOrWhiteSpace(request.ReceiptCode)
                ? $"PNK{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}"
                : request.ReceiptCode.Trim();

            while (await _context.WarehouseReceipts.AnyAsync(r => r.ReceiptCode == receiptCode))
            {
                receiptCode = $"PNK{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            }

            var userName = User.Identity?.Name ?? "Quản Trị Viên CellphoneS";

            var receipt = new WarehouseReceipt
            {
                ReceiptCode = receiptCode,
                SupplierName = request.SupplierName.Trim(),
                SupplierPhone = request.SupplierPhone?.Trim(),
                SupplierAddress = request.SupplierAddress?.Trim(),
                CreatedBy = userName,
                ImportDate = request.ImportDate ?? DateTime.UtcNow,
                Note = request.Note?.Trim(),
                Status = request.IsDraft ? ReceiptStatus.Draft : ReceiptStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            decimal grandTotal = 0;

            foreach (var item in request.Items.Where(i => i.Quantity > 0))
            {
                var product = await _context.Products
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product == null) continue;

                ProductVariant? variant = null;
                if (item.ProductVariantId.HasValue && item.ProductVariantId.Value > 0)
                {
                    variant = product.Variants.FirstOrDefault(v => v.Id == item.ProductVariantId.Value);
                }

                string? variantInfo = variant != null ? $"{variant.ColorName} - {variant.Storage}" : item.VariantInfo;
                decimal itemTotal = item.Quantity * item.ImportPrice;
                grandTotal += itemTotal;

                receipt.Details.Add(new WarehouseReceiptDetail
                {
                    ProductId = product.Id,
                    ProductVariantId = variant?.Id,
                    VariantInfo = variantInfo,
                    Quantity = item.Quantity,
                    ImportPrice = item.ImportPrice,
                    TotalPrice = itemTotal,
                    Note = item.Note
                });

                // NẾU KHÔNG PHẢI BẢN NHÁP: CẬP NHẬT TỒN KHO NGAY LẬP TỨC
                if (!request.IsDraft)
                {
                    int beforeStock = product.StockQuantity;
                    product.StockQuantity += item.Quantity;
                    int afterStock = product.StockQuantity;

                    if (variant != null)
                    {
                        variant.StockQuantity += item.Quantity;
                    }

                    _context.InventoryLogs.Add(new InventoryLog
                    {
                        ProductId = product.Id,
                        ProductVariantId = variant?.Id,
                        ChangeType = "Import",
                        QuantityChange = item.Quantity,
                        StockBefore = beforeStock,
                        StockAfter = afterStock,
                        ReferenceCode = receiptCode,
                        Note = $"Nhập kho từ {receipt.SupplierName} (Đơn giá: {item.ImportPrice:N0}₫)",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            receipt.TotalAmount = grandTotal;
            _context.WarehouseReceipts.Add(receipt);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = request.IsDraft
                    ? $"Đã lưu bản nháp phiếu nhập '{receipt.ReceiptCode}'!"
                    : $"Đã nhập kho thành công phiếu '{receipt.ReceiptCode}', số lượng tồn kho đã tăng!",
                receiptId = receipt.Id
            });
        }

        // ==========================================
        // 3. XEM CHI TIẾT PHIẾU NHẬP
        // ==========================================
        public async Task<IActionResult> Details(int id)
        {
            var receipt = await _context.WarehouseReceipts
                .Include(r => r.Details)
                    .ThenInclude(d => d.Product)
                .Include(r => r.Details)
                    .ThenInclude(d => d.ProductVariant)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null)
            {
                return NotFound();
            }

            return View(receipt);
        }

        // ==========================================
        // 4. DUYỆT BẢN NHÁP THÀNH ĐÃ NHẬP KHO
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> CompleteDraft(int id)
        {
            var receipt = await _context.WarehouseReceipts
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null || receipt.Status != ReceiptStatus.Draft)
            {
                return Json(new { success = false, message = "Phiếu nhập không hợp lệ hoặc đã hoàn tất." });
            }

            foreach (var detail in receipt.Details)
            {
                var product = await _context.Products.FindAsync(detail.ProductId);
                if (product != null)
                {
                    int beforeStock = product.StockQuantity;
                    product.StockQuantity += detail.Quantity;
                    int afterStock = product.StockQuantity;

                    if (detail.ProductVariantId.HasValue)
                    {
                        var variant = await _context.ProductVariants.FindAsync(detail.ProductVariantId.Value);
                        if (variant != null)
                        {
                            variant.StockQuantity += detail.Quantity;
                        }
                    }

                    _context.InventoryLogs.Add(new InventoryLog
                    {
                        ProductId = product.Id,
                        ProductVariantId = detail.ProductVariantId,
                        ChangeType = "Import",
                        QuantityChange = detail.Quantity,
                        StockBefore = beforeStock,
                        StockAfter = afterStock,
                        ReferenceCode = receipt.ReceiptCode,
                        Note = $"Nhập kho chính thức từ phiếu nháp {receipt.ReceiptCode}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            receipt.Status = ReceiptStatus.Completed;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Phiếu nhập '{receipt.ReceiptCode}' đã hoàn thành! Số lượng tồn kho đã được cộng." });
        }

        // ==========================================
        // 5. HUỶ PHIẾU NHẬP KHO (HOÀN TỒN)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> CancelReceipt(int id)
        {
            var receipt = await _context.WarehouseReceipts
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null || receipt.Status == ReceiptStatus.Cancelled)
            {
                return Json(new { success = false, message = "Phiếu nhập không hợp lệ hoặc đã huỷ trước đó." });
            }

            // Nếu phiếu đã nhập kho, trừ ngược lại số lượng tồn kho
            if (receipt.Status == ReceiptStatus.Completed)
            {
                foreach (var detail in receipt.Details)
                {
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product != null)
                    {
                        int beforeStock = product.StockQuantity;
                        product.StockQuantity = Math.Max(0, product.StockQuantity - detail.Quantity);
                        int afterStock = product.StockQuantity;

                        if (detail.ProductVariantId.HasValue)
                        {
                            var variant = await _context.ProductVariants.FindAsync(detail.ProductVariantId.Value);
                            if (variant != null)
                            {
                                variant.StockQuantity = Math.Max(0, variant.StockQuantity - detail.Quantity);
                            }
                        }

                        _context.InventoryLogs.Add(new InventoryLog
                        {
                            ProductId = product.Id,
                            ProductVariantId = detail.ProductVariantId,
                            ChangeType = "Adjustment",
                            QuantityChange = -detail.Quantity,
                            StockBefore = beforeStock,
                            StockAfter = afterStock,
                            ReferenceCode = receipt.ReceiptCode,
                            Note = $"Giảm tồn kho do huỷ phiếu nhập {receipt.ReceiptCode}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            receipt.Status = ReceiptStatus.Cancelled;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã huỷ phiếu nhập '{receipt.ReceiptCode}' và cập nhật lại tồn kho!" });
        }

        // ==========================================
        // 6. JSON APIS PHỤC VỤ DYNAMIC UI
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetProductVariants(int productId)
        {
            var variants = await _context.ProductVariants
                .Where(v => v.ProductId == productId)
                .Select(v => new
                {
                    v.Id,
                    v.ColorName,
                    v.ColorHex,
                    v.Storage,
                    v.Price,
                    v.StockQuantity,
                    v.Sku,
                    v.ImageUrl
                })
                .ToListAsync();

            return Json(variants);
        }
    }
}
