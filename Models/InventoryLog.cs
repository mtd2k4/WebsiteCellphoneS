using System.ComponentModel.DataAnnotations;

namespace WebsiteBanDienThoai.Models
{
    public class InventoryLog
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int? ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }

        [Required]
        [StringLength(50)]
        public string ChangeType { get; set; } = "Import"; // "Import" (Nhập kho), "Sale" (Bán hàng), "Return" (Huỷ đơn hoàn kho), "Adjustment" (Kiểm kê)

        public int QuantityChange { get; set; } // +10 or -2

        public int StockBefore { get; set; }

        public int StockAfter { get; set; }

        [StringLength(50)]
        public string? ReferenceCode { get; set; } // PNK-... or CPS-...

        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
