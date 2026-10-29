using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBanDienThoai.Models
{
    public class ProductReview
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int? UserId { get; set; }
        public AppUser? User { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên của bạn")]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        [StringLength(100)]
        public string? VariantInfo { get; set; }

        public bool IsVerifiedBuyer { get; set; } = true;

        public int LikesCount { get; set; } = 0;

        [StringLength(1000)]
        public string? AdminReply { get; set; }

        public DateTime? AdminReplyAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
