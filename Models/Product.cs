using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBanDienThoai.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(220)]
        public string Slug { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int BrandId { get; set; }
        public Brand? Brand { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OriginalPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; }

        [NotMapped]
        public int DiscountPercent
        {
            get
            {
                if (OriginalPrice > 0 && SalePrice < OriginalPrice)
                {
                    return (int)Math.Round((OriginalPrice - SalePrice) / OriginalPrice * 100);
                }
                return 0;
            }
        }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        // JSON or key-value list of specifications (RAM, ROM, Chip, Pin, Screen...)
        public string? TechnicalSpecs { get; set; }

        public bool IsHot { get; set; } = false;
        public bool IsFeatured { get; set; } = true;
        public bool IsFlashSale { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FlashSalePrice { get; set; }
        public int FlashSaleQuantity { get; set; } = 0;
        public int FlashSaleSold { get; set; } = 0;

        public double Rating { get; set; } = 5.0;
        public int ReviewCount { get; set; } = 0;
        public int StockQuantity { get; set; } = 100;

        [StringLength(100)]
        public string Warranty { get; set; } = "12 tháng chính hãng";

        [Column(TypeName = "decimal(18,2)")]
        public decimal SmemberDiscount { get; set; } = 0;

        [StringLength(255)]
        public string? PromotionInfo { get; set; }

        public bool HasInstallment0Percent { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    }
}
