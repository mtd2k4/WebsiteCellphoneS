namespace WebsiteBanDienThoai.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSlug { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int? VariantId { get; set; }
        public string? VariantInfo { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;

        public decimal TotalPrice => Price * Quantity;
    }
}
