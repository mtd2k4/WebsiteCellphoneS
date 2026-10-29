using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Controllers
{
    public class WishlistController : Controller
    {
        private readonly AppDbContext _context;
        private const string WishlistSessionKey = "Wishlist";

        public WishlistController(AppDbContext context)
        {
            _context = context;
        }

        private List<int> GetSessionWishlist()
        {
            var json = HttpContext.Session.GetString(WishlistSessionKey);
            if (string.IsNullOrEmpty(json))
            {
                return new List<int>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        private void SaveSessionWishlist(List<int> list)
        {
            var json = JsonSerializer.Serialize(list.Distinct().ToList());
            HttpContext.Session.SetString(WishlistSessionKey, json);
        }

        private async Task<int?> GetCurrentUserIdAsync()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userPhone = User.Identity.Name;
                var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == userPhone || u.Email == userPhone);
                return dbUser?.Id;
            }
            return null;
        }

        // GET: /Wishlist
        public async Task<IActionResult> Index()
        {
            var sessionIds = GetSessionWishlist();
            var userId = await GetCurrentUserIdAsync();

            List<int> allFavProductIds = new List<int>(sessionIds);

            if (userId.HasValue)
            {
                var dbFavProductIds = await _context.FavoriteProducts
                    .Where(f => f.UserId == userId.Value)
                    .Select(f => f.ProductId)
                    .ToListAsync();

                // Đồng bộ session vào DB nếu có sản phẩm mới chọn khi chưa đăng nhập
                foreach (var sId in sessionIds)
                {
                    if (!dbFavProductIds.Contains(sId))
                    {
                        _context.FavoriteProducts.Add(new FavoriteProduct
                        {
                            UserId = userId.Value,
                            ProductId = sId,
                            CreatedAt = DateTime.UtcNow
                        });
                        dbFavProductIds.Add(sId);
                    }
                }
                await _context.SaveChangesAsync();

                allFavProductIds = dbFavProductIds;
                SaveSessionWishlist(allFavProductIds);
            }

            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Where(p => allFavProductIds.Contains(p.Id))
                .ToListAsync();

            return View(products);
        }

        // POST: /Wishlist/Toggle
        [HttpPost]
        public async Task<IActionResult> Toggle([FromBody] int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            var sessionList = GetSessionWishlist();
            var userId = await GetCurrentUserIdAsync();
            bool isFavorited;

            if (userId.HasValue)
            {
                var existing = await _context.FavoriteProducts
                    .FirstOrDefaultAsync(f => f.UserId == userId.Value && f.ProductId == productId);

                if (existing != null)
                {
                    _context.FavoriteProducts.Remove(existing);
                    sessionList.Remove(productId);
                    isFavorited = false;
                }
                else
                {
                    _context.FavoriteProducts.Add(new FavoriteProduct
                    {
                        UserId = userId.Value,
                        ProductId = productId,
                        CreatedAt = DateTime.UtcNow
                    });
                    if (!sessionList.Contains(productId)) sessionList.Add(productId);
                    isFavorited = true;
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                if (sessionList.Contains(productId))
                {
                    sessionList.Remove(productId);
                    isFavorited = false;
                }
                else
                {
                    sessionList.Add(productId);
                    isFavorited = true;
                }
            }

            SaveSessionWishlist(sessionList);
            int totalCount = sessionList.Count;

            return Json(new
            {
                success = true,
                isFavorited = isFavorited,
                count = totalCount,
                message = isFavorited ? $"Đã thêm \"{product.Name}\" vào danh sách yêu thích!" : $"Đã bỏ \"{product.Name}\" khỏi danh sách yêu thích!"
            });
        }

        // POST: /Wishlist/Remove
        [HttpPost]
        public async Task<IActionResult> Remove([FromBody] int productId)
        {
            var sessionList = GetSessionWishlist();
            sessionList.Remove(productId);
            SaveSessionWishlist(sessionList);

            var userId = await GetCurrentUserIdAsync();
            if (userId.HasValue)
            {
                var existing = await _context.FavoriteProducts
                    .FirstOrDefaultAsync(f => f.UserId == userId.Value && f.ProductId == productId);
                if (existing != null)
                {
                    _context.FavoriteProducts.Remove(existing);
                    await _context.SaveChangesAsync();
                }
            }

            return Json(new { success = true, count = sessionList.Count, message = "Đã xóa khỏi danh sách yêu thích" });
        }

        // POST: /Wishlist/Clear
        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            SaveSessionWishlist(new List<int>());

            var userId = await GetCurrentUserIdAsync();
            if (userId.HasValue)
            {
                var items = await _context.FavoriteProducts
                    .Where(f => f.UserId == userId.Value)
                    .ToListAsync();
                _context.FavoriteProducts.RemoveRange(items);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Đã dọn sạch danh sách yêu thích" });
        }

        // GET: /Wishlist/CheckStatus?productId=5
        [HttpGet]
        public async Task<IActionResult> CheckStatus(int productId)
        {
            var sessionList = GetSessionWishlist();
            bool isFav = sessionList.Contains(productId);

            var userId = await GetCurrentUserIdAsync();
            if (userId.HasValue && !isFav)
            {
                isFav = await _context.FavoriteProducts
                    .AnyAsync(f => f.UserId == userId.Value && f.ProductId == productId);
            }

            return Json(new { isFavorited = isFav });
        }

        // GET: /Wishlist/GetCount
        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            var sessionList = GetSessionWishlist();
            var userId = await GetCurrentUserIdAsync();

            int count = sessionList.Count;
            if (userId.HasValue)
            {
                count = await _context.FavoriteProducts.CountAsync(f => f.UserId == userId.Value);
            }

            return Json(new { count = count });
        }

        // GET: /Wishlist/GetFavoriteIds
        [HttpGet]
        public async Task<IActionResult> GetFavoriteIds()
        {
            var sessionList = GetSessionWishlist();
            var userId = await GetCurrentUserIdAsync();
            if (userId.HasValue)
            {
                var dbFavs = await _context.FavoriteProducts
                    .Where(f => f.UserId == userId.Value)
                    .Select(f => f.ProductId)
                    .ToListAsync();
                foreach (var id in dbFavs)
                {
                    if (!sessionList.Contains(id)) sessionList.Add(id);
                }
            }

            return Json(sessionList);
        }
    }
}
