using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class NewsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public NewsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Admin/News
        public async Task<IActionResult> Index(string? search, string? category)
        {
            var query = _context.NewsArticles.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(n => n.Title.Contains(search) || n.Summary.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "Tất cả")
            {
                query = query.Where(n => n.Category == category);
            }

            var articles = await query
                .OrderByDescending(n => n.PublishedAt)
                .ThenByDescending(n => n.Id)
                .ToListAsync();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategory = category;
            ViewBag.Categories = await _context.NewsArticles
                .Select(n => n.Category)
                .Distinct()
                .ToListAsync();

            return View(articles);
        }

        // GET: /Admin/News/Create
        public IActionResult Create()
        {
            var article = new NewsArticle
            {
                PublishedAt = DateTime.UtcNow,
                IsActive = true,
                Category = "Tin công nghệ",
                Author = "CellphoneS Editor"
            };
            return View(article);
        }

        // POST: /Admin/News/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewsArticle article, IFormFile? imageFile)
        {
            if (string.IsNullOrWhiteSpace(article.Slug))
            {
                article.Slug = GenerateSlug(article.Title);
            }

            // Xử lý upload ảnh nếu có
            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "news");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString("N").Substring(0, 8) + "_" + Path.GetFileName(imageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                article.ThumbnailUrl = "/images/news/" + uniqueFileName;
            }

            if (string.IsNullOrWhiteSpace(article.ThumbnailUrl))
            {
                article.ThumbnailUrl = "/images/banners/banner-iphone16.jpg";
            }

            article.CreatedAt = DateTime.UtcNow;

            _context.NewsArticles.Add(article);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm bài viết mới thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/News/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var article = await _context.NewsArticles.FindAsync(id);
            if (article == null)
            {
                TempData["Error"] = "Không tìm thấy bài viết!";
                return RedirectToAction(nameof(Index));
            }
            return View(article);
        }

        // POST: /Admin/News/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NewsArticle model, IFormFile? imageFile)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var article = await _context.NewsArticles.FindAsync(id);
            if (article == null)
            {
                TempData["Error"] = "Không tìm thấy bài viết!";
                return RedirectToAction(nameof(Index));
            }

            // Xử lý upload ảnh mới nếu có
            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "news");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString("N").Substring(0, 8) + "_" + Path.GetFileName(imageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                article.ThumbnailUrl = "/images/news/" + uniqueFileName;
            }
            else if (!string.IsNullOrWhiteSpace(model.ThumbnailUrl))
            {
                article.ThumbnailUrl = model.ThumbnailUrl;
            }

            article.Title = model.Title;
            article.Slug = string.IsNullOrWhiteSpace(model.Slug) ? GenerateSlug(model.Title) : model.Slug;
            article.Summary = model.Summary;
            article.Content = model.Content;
            article.Category = model.Category;
            article.Author = model.Author;
            article.SourceUrl = model.SourceUrl;
            article.IsFeatured = model.IsFeatured;
            article.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật bài viết thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/News/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var article = await _context.NewsArticles.FindAsync(id);
            if (article != null)
            {
                _context.NewsArticles.Remove(article);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xoá bài viết thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy bài viết!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/News/ToggleFeatured/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFeatured(int id)
        {
            var article = await _context.NewsArticles.FindAsync(id);
            if (article != null)
            {
                article.IsFeatured = !article.IsFeatured;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật trạng thái Tiêu điểm cho bài viết!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/News/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var article = await _context.NewsArticles.FindAsync(id);
            if (article != null)
            {
                article.IsActive = !article.IsActive;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật hiển thị bài viết!";
            }
            return RedirectToAction(nameof(Index));
        }

        private static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString("N").Substring(0, 8);

            text = text.ToLowerInvariant().Trim();
            // Bỏ dấu tiếng Việt
            string[] vietnameseSigns = new string[]
            {
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

            for (int i = 1; i < vietnameseSigns.Length; i++)
            {
                for (int j = 0; j < vietnameseSigns[i].Length; j++)
                    text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
            }

            text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
            text = Regex.Replace(text, @"\s+", "-").Trim('-');
            return text;
        }
    }
}
