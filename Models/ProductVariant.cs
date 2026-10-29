using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBanDienThoai.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        [StringLength(100)]
        public string ColorName { get; set; } = string.Empty;

        [StringLength(20)]
        public string ColorHex { get; set; } = "#000000";

        [StringLength(50)]
        public string Storage { get; set; } = string.Empty; // e.g. "128GB", "256GB", "512GB", "1TB"

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int StockQuantity { get; set; } = 50;

        [StringLength(50)]
        public string? Sku { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }
    }
}
