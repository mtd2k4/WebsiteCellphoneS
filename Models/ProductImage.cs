using System.ComponentModel.DataAnnotations;

namespace WebsiteBanDienThoai.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsThumbnail { get; set; } = false;

        [StringLength(50)]
        public string? ColorKey { get; set; }
    }
}
