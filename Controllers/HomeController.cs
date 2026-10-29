using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;
using WebsiteBanDienThoai.Models.ViewModels;

namespace WebsiteBanDienThoai.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(AppDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel
            {
                Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync(),

                MainBanners = await _context.Banners
                    .Where(b => b.IsActive && b.Position == "MainSlider")
                    .OrderBy(b => b.DisplayOrder)
                    .ToListAsync(),

                RightBanners = await _context.Banners
                    .Where(b => b.IsActive && b.Position == "RightWidget")
                    .OrderBy(b => b.DisplayOrder)
                    .ToListAsync(),

                BigBanners = await _context.Banners
                    .Where(b => b.IsActive && (b.Position == "BigBanner" || b.Position == "MiddleBanner" || b.Position == "SpecialBanner"))
                    .OrderBy(b => b.DisplayOrder)
                    .ToListAsync(),

                PopupBanner = await _context.Banners
                    .Where(b => b.IsActive && (b.Position == "Popup" || b.Position == "PopupBanner"))
                    .OrderBy(b => b.DisplayOrder)
                    .ThenByDescending(b => b.Id)
                    .FirstOrDefaultAsync(),

                FlashSaleProducts = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Where(p => p.IsFlashSale)
                    .OrderByDescending(p => p.FlashSaleSold)
                    .ToListAsync(),

                FeaturedPhones = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Where(p => p.Category != null && p.Category.Slug == "dien-thoai")
                    .OrderByDescending(p => p.IsHot)
                    .ThenByDescending(p => p.ReviewCount)
                    .ToListAsync(),

                FeaturedLaptops = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Where(p => p.Category != null && (p.Category.Slug == "laptop" || p.Category.Slug == "tablet"))
                    .OrderByDescending(p => p.IsHot)
                    .ToListAsync(),

                FeaturedAudio = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Where(p => p.Category != null && p.Category.Slug == "am-thanh")
                    .OrderByDescending(p => p.IsHot)
                    .ToListAsync(),

                FeaturedWatches = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Where(p => p.Category != null && p.Category.Slug == "dong-ho")
                    .OrderByDescending(p => p.IsHot)
                    .ToListAsync(),

                FeaturedAccessories = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Where(p => p.Category != null && p.Category.Slug == "phu-kien")
                    .OrderByDescending(p => p.IsHot)
                    .ToListAsync(),

                FeaturedSmartHome = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Where(p => p.Category != null && p.Category.Slug == "gia-dung")
                    .OrderByDescending(p => p.IsHot)
                    .ToListAsync(),

                Brands = await _context.Brands
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.DisplayOrder)
                    .ToListAsync(),

                LatestNews = await _context.NewsArticles
                    .Where(n => n.IsActive)
                    .OrderByDescending(n => n.IsFeatured)
                    .ThenByDescending(n => n.PublishedAt)
                    .ThenByDescending(n => n.Id)
                    .Take(8)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // Live Search AJAX API
        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new List<object>());
            }

            var cleanQuery = q.Trim().ToLower();

            var results = await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.Name.ToLower().Contains(cleanQuery) ||
                            (p.Brand != null && p.Brand.Name.ToLower().Contains(cleanQuery)))
                .Take(6)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Slug,
                    p.SalePrice,
                    p.ImageUrl,
                    BrandName = p.Brand != null ? p.Brand.Name : ""
                })
                .ToListAsync();

            return Json(results);
        }

        public IActionResult Privacy()
        {
            return RedirectToAction("Privacy", "Policy");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
