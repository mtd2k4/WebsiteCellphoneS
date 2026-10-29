using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBanDienThoai.Models
{
    public enum ReceiptStatus
    {
        Draft = 0,      // Bản nháp (chưa cộng tồn kho)
        Completed = 1,  // Đã nhập kho (đã cộng tồn kho)
        Cancelled = 2   // Đã huỷ
    }

    public class WarehouseReceipt
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ReceiptCode { get; set; } = string.Empty; // e.g. "PNK20261026-0001"

        [Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp")]
        [StringLength(150)]
        public string SupplierName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? SupplierPhone { get; set; }

        [StringLength(255)]
        public string? SupplierAddress { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; } // Admin name/email

        public DateTime ImportDate { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Note { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public ReceiptStatus Status { get; set; } = ReceiptStatus.Completed;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<WarehouseReceiptDetail> Details { get; set; } = new List<WarehouseReceiptDetail>();
    }
}
