using System.ComponentModel.DataAnnotations;

namespace WebsiteBanDienThoai.Models
{
    public class FavoriteProduct
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public AppUser? User { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
