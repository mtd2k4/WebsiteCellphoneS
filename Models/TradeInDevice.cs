using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBanDienThoai.Models
{
    public class TradeInDevice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Brand { get; set; } = string.Empty; // Apple, Samsung, Xiaomi, OPPO

        [MaxLength(50)]
        public string DeviceType { get; set; } = "Điện thoại"; // Điện thoại, Laptop, Tablet, Đồng hồ, Tai nghe

        [Required]
        [MaxLength(100)]
        public string ModelName { get; set; } = string.Empty; // iPhone 14 Pro Max, Galaxy S23 Ultra

        [Required]
        [MaxLength(50)]
        public string Storage { get; set; } = string.Empty; // 128GB, 256GB, 512GB, 1TB

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; } // Giá thu tối đa cho Loại 1 (Máy đẹp 99%)

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;
    }
}
