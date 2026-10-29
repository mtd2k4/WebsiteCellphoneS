using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models.ViewModels;

namespace WebsiteBanDienThoai.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            var relatedProducts = await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
                .Take(5)
                .ToListAsync();

            var reviews = await _context.ProductReviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = relatedProducts,
                Reviews = reviews
            };

            return View(viewModel);
        }

        // POST: /Product/AddReview
        [HttpPost]
        public async Task<IActionResult> AddReview([FromBody] AddReviewRequest request)
        {
            if (request == null || request.ProductId <= 0)
            {
                return Json(new { success = false, message = "Dữ liệu đánh giá không hợp lệ." });
            }

            if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Trim().Length < 5)
            {
                return Json(new { success = false, message = "Nội dung đánh giá cần tối thiểu 5 ký tự." });
            }

            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            // Tự động nhận diện tài khoản nếu đã đăng nhập
            int? userId = null;
            string customerName = request.CustomerName?.Trim() ?? string.Empty;
            string? phone = request.PhoneNumber?.Trim();

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userPhone = User.Identity.Name;
                var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == userPhone || u.Email == userPhone);
                if (dbUser != null)
                {
                    userId = dbUser.Id;
                    if (string.IsNullOrEmpty(customerName)) customerName = dbUser.FullName;
                    if (string.IsNullOrEmpty(phone)) phone = dbUser.PhoneNumber;
                }
            }

            if (string.IsNullOrEmpty(customerName))
            {
                customerName = "Khách hàng CellphoneS";
            }

            var review = new Models.ProductReview
            {
                ProductId = request.ProductId,
                UserId = userId,
                CustomerName = customerName,
                PhoneNumber = phone,
                Rating = Math.Clamp(request.Rating, 1, 5),
                Content = request.Content.Trim(),
                VariantInfo = request.VariantInfo,
                IsVerifiedBuyer = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            // Cập nhật lại số lượng và điểm rating trung bình của sản phẩm
            var allRatings = await _context.ProductReviews
                .Where(r => r.ProductId == request.ProductId)
                .Select(r => r.Rating)
                .ToListAsync();

            if (allRatings.Any())
            {
                product.ReviewCount = allRatings.Count;
                product.Rating = Math.Round(allRatings.Average(), 1);
                await _context.SaveChangesAsync();
            }

            return Json(new
            {
                success = true,
                message = "Cảm ơn bạn đã gửi đánh giá! Nhận xét của bạn đã được xuất bản.",
                review = new
                {
                    id = review.Id,
                    customerName = review.CustomerName,
                    rating = review.Rating,
                    content = review.Content,
                    variantInfo = review.VariantInfo,
                    isVerified = review.IsVerifiedBuyer,
                    createdAt = review.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    likes = review.LikesCount
                }
            });
        }

        // POST: /Product/LikeReview
        [HttpPost]
        public async Task<IActionResult> LikeReview([FromBody] int reviewId)
        {
            var review = await _context.ProductReviews.FindAsync(reviewId);
            if (review == null)
            {
                return Json(new { success = false });
            }

            review.LikesCount++;
            await _context.SaveChangesAsync();

            return Json(new { success = true, likes = review.LikesCount });
        }

        // GET: /Product/Category?slug=dien-thoai&brand=apple&minPrice=...&maxPrice=...&sort=...
        public async Task<IActionResult> Category(string slug, string? brand, decimal? minPrice, decimal? maxPrice, string? sort)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
            if (category == null)
            {
                // Fallback nếu không truyền slug hoặc slug là all
                category = await _context.Categories.FirstOrDefaultAsync() ?? new Models.Category { Name = "Sản phẩm", Slug = "all" };
            }

            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .AsQueryable();

            if (category.Slug != "all")
            {
                query = query.Where(p => p.CategoryId == category.Id);
            }

            if (!string.IsNullOrEmpty(brand) && brand != "all")
            {
                query = query.Where(p => p.Brand != null && p.Brand.Slug == brand);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.SalePrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.SalePrice <= maxPrice.Value);
            }

            // Sắp xếp
            switch (sort)
            {
                case "price-asc":
                    query = query.OrderBy(p => p.SalePrice);
                    break;
                case "price-desc":
                    query = query.OrderByDescending(p => p.SalePrice);
                    break;
                case "discount":
                    query = query.OrderByDescending(p => p.OriginalPrice - p.SalePrice);
                    break;
                case "hot":
                default:
                    query = query.OrderByDescending(p => p.IsHot).ThenByDescending(p => p.ReviewCount);
                    break;
            }

            var products = await query.ToListAsync();
            var brands = await _context.Brands.Where(b => b.IsActive).OrderBy(b => b.DisplayOrder).ToListAsync();

            var viewModel = new CategoryViewModel
            {
                Category = category,
                Products = products,
                Brands = brands,
                SelectedBrand = brand,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortOrder = sort,
                TotalCount = products.Count
            };

            return View(viewModel);
        }
    }
}
