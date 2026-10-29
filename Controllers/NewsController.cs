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
    public class NewsController : Controller
    {
        private readonly AppDbContext _context;

        public NewsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /News or /tin-cong-nghe
        [Route("News")]
        [Route("tin-cong-nghe")]
        [Route("tin-tuc-cong-nghe")]
        public async Task<IActionResult> Index(string? category, string? search, int page = 1)
        {
            const int pageSize = 9;
            page = Math.Max(1, page);

            var query = _context.NewsArticles
                .Where(n => n.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(n => n.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(n => n.Title.Contains(search) || n.Summary.Contains(search) || n.Category.Contains(search));
            }

            var allCategories = await _context.NewsArticles
                .Where(n => n.IsActive)
                .Select(n => n.Category)
                .Distinct()
                .ToListAsync();

            var categories = new List<string> { "Tất cả" };
            categories.AddRange(allCategories);

            int totalArticles = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalArticles / pageSize);
            page = Math.Min(page, Math.Max(1, totalPages));

            // Toàn bộ bài sắp xếp theo tiêu điểm và ngày đăng
            var orderedQuery = query
                .OrderByDescending(n => n.IsFeatured)
                .ThenByDescending(n => n.PublishedAt)
                .ThenByDescending(n => n.Id);

            // Bài viết tiêu điểm lớn nhất (nếu ở trang 1 và không tìm kiếm)
            NewsArticle? featuredArticle = null;
            if (page == 1 && string.IsNullOrWhiteSpace(search) && (string.IsNullOrWhiteSpace(category) || category == "Tất cả"))
            {
                featuredArticle = await orderedQuery.FirstOrDefaultAsync();
            }

            // 4 bài xu hướng nổi bật
            var trendingArticles = await orderedQuery
                .Where(n => featuredArticle == null || n.Id != featuredArticle.Id)
                .Take(4)
                .ToListAsync();

            // Danh sách bài phân trang
            var pagedArticles = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Sản phẩm HOT liên quan tại CellphoneS cho sidebar
            var relatedProducts = await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.StockQuantity > 0)
                .OrderByDescending(p => p.IsHot)
                .ThenByDescending(p => p.FlashSaleSold)
                .Take(4)
                .ToListAsync();

            var viewModel = new TechNewsViewModel
            {
                FeaturedArticle = featuredArticle,
                TrendingArticles = trendingArticles,
                Articles = pagedArticles,
                Categories = categories,
                CurrentCategory = category ?? "Tất cả",
                CurrentSearch = search,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalArticles = totalArticles,
                HotTags = new()
                {
                    "iPhone 16 Pro Max",
                    "Galaxy S24 Ultra",
                    "Thu Cũ Đổi Mới",
                    "Apple Intelligence",
                    "Laptop AI HSSV",
                    "iOS 18 Mẹo Hay",
                    "OPPO Find X10",
                    "Snapdragon 8 Elite"
                },
                RelatedProducts = relatedProducts
            };

            return View(viewModel);
        }

        // GET: /News/Details/slug
        [Route("News/Details/{id:int}")]
        [Route("tin-cong-nghe/{slug}")]
        public async Task<IActionResult> Details(string? slug, int? id)
        {
            NewsArticle? article = null;

            if (id.HasValue && id.Value > 0)
            {
                article = await _context.NewsArticles.FirstOrDefaultAsync(n => n.Id == id.Value && n.IsActive);
            }
            else if (!string.IsNullOrWhiteSpace(slug))
            {
                article = await _context.NewsArticles.FirstOrDefaultAsync(n => n.Slug == slug && n.IsActive);
            }

            if (article == null)
            {
                return NotFound();
            }

            // Tăng lượt xem
            article.ViewCount++;
            await _context.SaveChangesAsync();

            // Các bài viết liên quan cùng chuyên mục
            var relatedNews = await _context.NewsArticles
                .Where(n => n.IsActive && n.Id != article.Id && n.Category == article.Category)
                .OrderByDescending(n => n.PublishedAt)
                .Take(4)
                .ToListAsync();

            if (relatedNews.Count < 4)
            {
                var moreNews = await _context.NewsArticles
                    .Where(n => n.IsActive && n.Id != article.Id && !relatedNews.Select(r => r.Id).Contains(n.Id))
                    .OrderByDescending(n => n.PublishedAt)
                    .Take(4 - relatedNews.Count)
                    .ToListAsync();
                relatedNews.AddRange(moreNews);
            }

            ViewBag.RelatedNews = relatedNews;

            // Sản phẩm liên quan
            ViewBag.RelatedProducts = await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.StockQuantity > 0)
                .OrderByDescending(p => p.IsHot)
                .Take(3)
                .ToListAsync();

            return View(article);
        }
    }
}
