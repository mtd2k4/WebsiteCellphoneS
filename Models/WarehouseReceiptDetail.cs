using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBanDienThoai.Models
{
    public class WarehouseReceiptDetail
    {
        public int Id { get; set; }

        public int WarehouseReceiptId { get; set; }
        public WarehouseReceipt? WarehouseReceipt { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int? ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }

        [StringLength(150)]
        public string? VariantInfo { get; set; } // Snapshot: e.g. "Titan Sa Mạc - 256GB"

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập phải lớn hơn 0")]
        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá nhập không thể âm")]
        public decimal ImportPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [StringLength(255)]
        public string? Note { get; set; }
    }
}
