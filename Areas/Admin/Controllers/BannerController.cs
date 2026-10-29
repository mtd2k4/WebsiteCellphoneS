using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Areas.Admin.Models;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BannerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BannerController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Admin/Banner
        public async Task<IActionResult> Index(string? position, string? search)
        {
            var query = _context.Banners.AsQueryable();

            if (!string.IsNullOrWhiteSpace(position))
            {
                if (position == "Popup" || position == "PopupBanner")
                {
                    query = query.Where(b => b.Position == "Popup" || b.Position == "PopupBanner");
                }
                else if (position == "BigBanner")
                {
                    query = query.Where(b => b.Position == "BigBanner" || b.Position == "MiddleBanner");
                }
                else
                {
                    query = query.Where(b => b.Position == position);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(b => b.Title.Contains(search) || (b.Subtitle != null && b.Subtitle.Contains(search)));
            }

            var banners = await query
                .OrderBy(b => b.Position)
                .ThenBy(b => b.DisplayOrder)
                .ThenBy(b => b.Id)
                .ToListAsync();

            ViewBag.CurrentPosition = position;
            ViewBag.CurrentSearch = search;
            ViewBag.TotalCount = await _context.Banners.CountAsync();
            ViewBag.PopupCount = await _context.Banners.CountAsync(b => b.Position == "Popup" || b.Position == "PopupBanner");
            ViewBag.MainSliderCount = await _context.Banners.CountAsync(b => b.Position == "MainSlider");
            ViewBag.BigBannerCount = await _context.Banners.CountAsync(b => b.Position == "BigBanner" || b.Position == "MiddleBanner");
            ViewBag.RightWidgetCount = await _context.Banners.CountAsync(b => b.Position == "RightWidget");

            return View(banners);
        }

        // GET: /Admin/Banner/Create
        public async Task<IActionResult> Create(string? position)
        {
            var maxOrder = await _context.Banners.AnyAsync() 
                ? await _context.Banners.MaxAsync(b => b.DisplayOrder) 
                : 0;

            var defaultPos = !string.IsNullOrWhiteSpace(position) ? position : "Popup";

            var vm = new BannerFormViewModel
            {
                Banner = new Banner
                {
                    Position = defaultPos,
                    DisplayOrder = maxOrder + 1,
                    IsActive = true,
                    LinkUrl = "#"
                }
            };
            return View(vm);
        }

        // POST: /Admin/Banner/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BannerFormViewModel model)
        {
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadedPath = await SaveUploadedImageAsync(model.ImageFile);
                if (uploadedPath != null)
                {
                    model.Banner.ImageUrl = uploadedPath;
                }
            }

            if (string.IsNullOrWhiteSpace(model.Banner.ImageUrl))
            {
                ModelState.AddModelError("Banner.ImageUrl", "Vui lòng chọn file tải ảnh lên từ máy hoặc nhập đường dẫn ảnh hợp lệ.");
            }

            if (ModelState.IsValid)
            {
                _context.Banners.Add(model.Banner);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Thêm mới banner '{model.Banner.Title}' thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: /Admin/Banner/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
            {
                return NotFound();
            }

            var vm = new BannerFormViewModel
            {
                Banner = banner
            };
            return View(vm);
        }

        // POST: /Admin/Banner/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BannerFormViewModel model)
        {
            if (id != model.Banner.Id)
            {
                return BadRequest();
            }

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadedPath = await SaveUploadedImageAsync(model.ImageFile);
                if (uploadedPath != null)
                {
                    model.Banner.ImageUrl = uploadedPath;
                }
            }

            if (string.IsNullOrWhiteSpace(model.Banner.ImageUrl))
            {
                ModelState.AddModelError("Banner.ImageUrl", "Vui lòng chọn file tải ảnh lên hoặc nhập đường dẫn ảnh hợp lệ.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(model.Banner);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Cập nhật banner '{model.Banner.Title}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Banners.AnyAsync(b => b.Id == model.Banner.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            return View(model);
        }

        // POST: /Admin/Banner/QuickUpdateImage
        [HttpPost]
        public async Task<IActionResult> QuickUpdateImage(int id, IFormFile? imageFile, string? imageUrl)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
            {
                return Json(new { success = false, message = "Không tìm thấy banner cần cập nhật." });
            }

            string? newUrl = null;

            if (imageFile != null && imageFile.Length > 0)
            {
                newUrl = await SaveUploadedImageAsync(imageFile);
            }
            else if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                newUrl = imageUrl.Trim();
            }

            if (string.IsNullOrWhiteSpace(newUrl))
            {
                return Json(new { success = false, message = "Vui lòng chọn file ảnh hợp lệ hoặc nhập URL ảnh." });
            }

            banner.ImageUrl = newUrl;
            await _context.SaveChangesAsync();

            return Json(new { success = true, newImageUrl = newUrl, message = "Cập nhật ảnh banner thành công!" });
        }

        // POST: /Admin/Banner/ToggleActive/5
        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
            {
                return Json(new { success = false, message = "Không tìm thấy banner" });
            }

            banner.IsActive = !banner.IsActive;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = banner.IsActive });
        }

        // POST: /Admin/Banner/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
            {
                return Json(new { success = false, message = "Không tìm thấy banner" });
            }

            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xoá banner thành công!" });
        }

        private async Task<string?> SaveUploadedImageAsync(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return null;
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "banners");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"banner_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString()[..6]}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/images/banners/{uniqueFileName}";
        }
    }
}
