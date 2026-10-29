using System.ComponentModel.DataAnnotations;

namespace WebsiteBanDienThoai.Models
{
    public enum NotificationType
    {
        NewOrder,           // Đơn hàng mới
        OrderConfirmed,     // Xác nhận đơn hàng
        OrderShipping,      // Đang vận chuyển
        OrderCompleted,     // Đã hoàn thành
        OrderCancelled,     // Đơn hàng bị huỷ
        SystemPromotion     // Khuyến mãi hệ thống
    }

    public class AppNotification
    {
        public int Id { get; set; }

        public NotificationType Type { get; set; } = NotificationType.NewOrder;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(255)]
        public string? TargetUrl { get; set; }

        public int? OrderId { get; set; }

        [StringLength(50)]
        public string? OrderCode { get; set; }

        public int? UserId { get; set; } // Null for broadcast or admin

        [StringLength(50)]
        public string? RecipientRole { get; set; } // "Admin", "Customer", or null

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
