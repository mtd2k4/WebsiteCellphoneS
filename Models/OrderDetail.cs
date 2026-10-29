using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBanDienThoai.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int? ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }

        public string? VariantInfo { get; set; } // e.g. "Titan Tự Nhiên / 256GB"

        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [NotMapped]
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
