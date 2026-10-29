using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBanDienThoai.Models
{
    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Shipping,
        Completed,
        Cancelled
    }

    public class Order
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
        [StringLength(255)]
        public string Address { get; set; } = string.Empty;

        [StringLength(100)]
        public string Province { get; set; } = "Hồ Chí Minh";

        [StringLength(500)]
        public string? Notes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubtotalAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 30000;

        [StringLength(50)]
        public string? ShippingDiscountCode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingDiscountAmount { get; set; } = 0;

        [StringLength(50)]
        public string? OrderDiscountCode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OrderDiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "COD"; // COD, BankTransfer, VNPay

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
