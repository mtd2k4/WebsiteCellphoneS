using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using WebsiteBanDienThoai.Areas.Admin.Models;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Product
        public async Task<IActionResult> Index(string? search, int? categoryId, int? brandId, string? stockStatus, int page = 1)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(p => p.Name.ToLower().Contains(s) || p.Slug.ToLower().Contains(s));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue && brandId.Value > 0)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            if (!string.IsNullOrWhiteSpace(stockStatus))
            {
                if (stockStatus == "in-stock")
                    query = query.Where(p => p.StockQuantity > 20);
                else if (stockStatus == "low-stock")
                    query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity <= 20);
                else if (stockStatus == "out-of-stock")
                    query = query.Where(p => p.StockQuantity == 0);
            }

            var totalItems = await query.CountAsync();
            int pageSize = 12;
            var products = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", categoryId);
            ViewBag.Brands = new SelectList(await _context.Brands.Where(b => b.IsActive).ToListAsync(), "Id", "Name", brandId);
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentBrandId = brandId;
            ViewBag.CurrentStockStatus = stockStatus;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.TotalItems = totalItems;

            return View(products);
        }

        // GET: /Admin/Product/Create
        public async Task<IActionResult> Create()
        {
            var vm = new ProductFormViewModel
            {
                Product = new Product
                {
                    StockQuantity = 50,
                    Warranty = "12 tháng chính hãng",
                    HasInstallment0Percent = true,
                    Rating = 5.0
                },
                CategoryList = await GetCategorySelectList(),
                BrandList = await GetBrandSelectList()
            };
            return View(vm);
        }

        // POST: /Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Product.Slug))
            {
                model.Product.Slug = GenerateSlug(model.Product.Name);
            }

            // Ensure unique slug
            if (await _context.Products.AnyAsync(p => p.Slug == model.Product.Slug))
            {
                model.Product.Slug += "-" + new Random().Next(100, 999);
            }

            if (ModelState.IsValid)
            {
                model.Product.CreatedAt = DateTime.UtcNow;
                _context.Products.Add(model.Product);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Thêm mới sản phẩm '{model.Product.Name}' thành công!";
                return RedirectToAction(nameof(Index));
            }

            model.CategoryList = await GetCategorySelectList();
            model.BrandList = await GetBrandSelectList();
            return View(model);
        }

        // GET: /Admin/Product/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            var vm = new ProductFormViewModel
            {
                Product = product,
                CategoryList = await GetCategorySelectList(product.CategoryId),
                BrandList = await GetBrandSelectList(product.BrandId)
            };
            return View(vm);
        }

        // POST: /Admin/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
        {
            if (id != model.Product.Id)
            {
                return BadRequest();
            }

            if (string.IsNullOrWhiteSpace(model.Product.Slug))
            {
                model.Product.Slug = GenerateSlug(model.Product.Name);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Giữ nguyên số lượng tồn kho thực tế từ cơ sở dữ liệu (quản lý qua kho hàng)
                    var currentProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == model.Product.Id);
                    if (currentProduct != null)
                    {
                        model.Product.StockQuantity = currentProduct.StockQuantity;
                    }

                    _context.Update(model.Product);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Cập nhật sản phẩm '{model.Product.Name}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Products.AnyAsync(p => p.Id == model.Product.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            model.CategoryList = await GetCategorySelectList(model.Product.CategoryId);
            model.BrandList = await GetBrandSelectList(model.Product.BrandId);
            return View(model);
        }

        // POST: /Admin/Product/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
            }

            // Gỡ bỏ liên kết trong FavoriteProducts và TradeInRequests nếu có
            var favs = await _context.FavoriteProducts.Where(f => f.ProductId == id).ToListAsync();
            _context.FavoriteProducts.RemoveRange(favs);

            var tradeIns = await _context.TradeInRequests.Where(t => t.TargetProductId == id).ToListAsync();
            foreach (var t in tradeIns) t.TargetProductId = null;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xoá sản phẩm thành công!" });
        }

        // ==========================================
        // QUẢN LÝ BIẾN THỂ SẢN PHẨM (VARIANTS)
        // ==========================================

        // GET: /Admin/Product/Variants/5
        public async Task<IActionResult> Variants(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Sắp xếp biến thể theo dung lượng, sau đó theo giá
            product.Variants = product.Variants
                .OrderBy(v => v.Storage)
                .ThenBy(v => v.Price)
                .ToList();

            return View(product);
        }

        // GET: /Admin/Product/GetVariant/5
        [HttpGet]
        public async Task<IActionResult> GetVariant(int id)
        {
            var variant = await _context.ProductVariants.FindAsync(id);
            if (variant == null)
            {
                return Json(new { success = false, message = "Không tìm thấy biến thể." });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    id = variant.Id,
                    productId = variant.ProductId,
                    colorName = variant.ColorName,
                    colorHex = variant.ColorHex,
                    storage = variant.Storage,
                    price = variant.Price,
                    stockQuantity = variant.StockQuantity,
                    sku = variant.Sku,
                    imageUrl = variant.ImageUrl
                }
            });
        }

        public class SaveVariantDto
        {
            public int Id { get; set; }
            public int ProductId { get; set; }
            public string ColorName { get; set; } = string.Empty;
            public string? ColorHex { get; set; }
            public string? Storage { get; set; }
            public decimal Price { get; set; }
            public int StockQuantity { get; set; } = 50;
            public string? Sku { get; set; }
            public string? ImageUrl { get; set; }
        }

        // POST: /Admin/Product/SaveVariant
        [HttpPost]
        public async Task<IActionResult> SaveVariant([FromBody] SaveVariantDto? model)
        {
            if (model == null || model.ProductId <= 0)
            {
                return Json(new { success = false, message = "Dữ liệu hoặc sản phẩm không hợp lệ." });
            }

            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
            }

            if (string.IsNullOrWhiteSpace(model.ColorName))
            {
                return Json(new { success = false, message = "Vui lòng nhập tên màu sắc." });
            }

            var colorName = model.ColorName.Trim();
            var storage = (model.Storage ?? string.Empty).Trim();
            var colorHex = string.IsNullOrWhiteSpace(model.ColorHex) ? "#000000" : model.ColorHex.Trim();
            var imageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? product.ImageUrl : model.ImageUrl.Trim();
            var sku = string.IsNullOrWhiteSpace(model.Sku) ? null : model.Sku.Trim();
            var price = model.Price > 0 ? model.Price : product.SalePrice;
            var stock = model.StockQuantity >= 0 ? model.StockQuantity : 0;

            if (model.Id <= 0)
            {
                // Thêm mới biến thể (Số lượng khởi tạo 0, tăng khi tạo phiếu nhập kho)
                var variant = new ProductVariant
                {
                    ProductId = model.ProductId,
                    ColorName = colorName,
                    ColorHex = colorHex,
                    Storage = storage,
                    Price = price,
                    StockQuantity = 0,
                    Sku = sku,
                    ImageUrl = imageUrl
                };
                _context.ProductVariants.Add(variant);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Đã thêm biến thể '{colorName} - {storage}' thành công! Vui lòng nhập hàng tại mục Kho Hàng để tăng số lượng tồn." });
            }
            else
            {
                // Cập nhật biến thể
                var existing = await _context.ProductVariants.FindAsync(model.Id);
                if (existing == null || existing.ProductId != model.ProductId)
                {
                    return Json(new { success = false, message = "Không tìm thấy biến thể cần sửa." });
                }

                existing.ColorName = colorName;
                existing.ColorHex = colorHex;
                existing.Storage = storage;
                existing.Price = price;
                // Giữ nguyên existing.StockQuantity được quản lý qua phiếu nhập kho và bán hàng
                existing.Sku = sku;
                existing.ImageUrl = imageUrl;

                _context.ProductVariants.Update(existing);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Cập nhật biến thể '{existing.ColorName} - {existing.Storage}' thành công!" });
            }
        }

        // POST: /Admin/Product/DeleteVariant/5
        [HttpPost]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            var variant = await _context.ProductVariants.FindAsync(id);
            if (variant == null)
            {
                return Json(new { success = false, message = "Không tìm thấy biến thể cần xoá." });
            }

            // Gỡ bỏ liên kết trong OrderDetails nếu có (đặt foreign key về null để không xoá mất lịch sử đơn hàng)
            var orderDetails = await _context.OrderDetails.Where(od => od.ProductVariantId == id).ToListAsync();
            foreach (var od in orderDetails)
            {
                od.ProductVariantId = null;
            }

            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã xoá biến thể '{variant.ColorName} - {variant.Storage}' thành công!" });
        }

        public class BatchVariantRequest
        {
            public int ProductId { get; set; }
            public List<string> Storages { get; set; } = new();
            public List<string> Colors { get; set; } = new();
            public decimal Price { get; set; }
            public int Stock { get; set; } = 50;
        }

        // POST: /Admin/Product/BatchCreateVariants
        [HttpPost]
        public async Task<IActionResult> BatchCreateVariants([FromBody] BatchVariantRequest req)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == req.ProductId);

            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
            }

            if (!req.Storages.Any() || !req.Colors.Any())
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một dung lượng và một màu sắc." });
            }

            int createdCount = 0;
            foreach (var st in req.Storages)
            {
                foreach (var c in req.Colors)
                {
                    var parts = c.Split('|');
                    var colorName = parts[0].Trim();
                    var colorHex = parts.Length > 1 ? parts[1].Trim() : "#000000";

                    // Tránh tạo trùng lặp
                    bool exists = product.Variants.Any(v => 
                        v.Storage.Equals(st, StringComparison.OrdinalIgnoreCase) && 
                        v.ColorName.Equals(colorName, StringComparison.OrdinalIgnoreCase));

                    if (!exists)
                    {
                        var newVariant = new ProductVariant
                        {
                            ProductId = product.Id,
                            Storage = st.Trim(),
                            ColorName = colorName,
                            ColorHex = colorHex,
                            Price = req.Price > 0 ? req.Price : product.SalePrice,
                            StockQuantity = 0,
                            ImageUrl = product.ImageUrl
                        };
                        _context.ProductVariants.Add(newVariant);
                        createdCount++;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Đã tạo nhanh {createdCount} biến thể mới thành công!" });
        }

        private async Task<IEnumerable<SelectListItem>> GetCategorySelectList(int? selectedId = null)
        {
            var list = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
            return list.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = selectedId.HasValue && c.Id == selectedId.Value
            });
        }

        private async Task<IEnumerable<SelectListItem>> GetBrandSelectList(int? selectedId = null)
        {
            var list = await _context.Brands.Where(b => b.IsActive).OrderBy(b => b.DisplayOrder).ToListAsync();
            return list.Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = b.Name,
                Selected = selectedId.HasValue && b.Id == selectedId.Value
            });
        }

        private string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return "san-pham-" + DateTime.Now.Ticks;
            string str = title.ToLower().Trim();
            string[] vietnamese = new string[] {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ",
                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ",
                "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ",
                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ",
                "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ",
                "ÍÌỊỈĨ",
                "đ",
                "Đ",
                "ýỳỵỷỹ",
                "ÝỲỴỶỸ"
            };
            for (int i = 1; i < vietnamese.Length; i++)
            {
                for (int j = 0; j < vietnamese[i].Length; j++)
                    str = str.Replace(vietnamese[i][j], vietnamese[0][i - 1]);
            }
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            str = Regex.Replace(str, @"\s+", " ").Trim();
            str = Regex.Replace(str, @"\s", "-");
            return str;
        }
    }
}
