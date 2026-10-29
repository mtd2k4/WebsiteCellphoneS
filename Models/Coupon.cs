using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBanDienThoai.Models
{
    public enum CouponType
    {
        OrderDiscount = 0,    // Mã giảm giá cho đơn hàng
        ShippingDiscount = 1  // Mã giảm giá phí vận chuyển (Freeship)
    }

    public class Coupon
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã giảm giá")]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề khuyến mãi")]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public CouponType Type { get; set; } = CouponType.OrderDiscount;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        public bool IsPercentage { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        public int UsageLimit { get; set; } = 1000;

        public int UsedCount { get; set; } = 0;

        public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddDays(90);

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
