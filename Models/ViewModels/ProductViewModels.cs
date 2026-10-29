using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Models.ViewModels
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = null!;
        public List<Product> RelatedProducts { get; set; } = new();
        public List<ProductReview> Reviews { get; set; } = new();

        public double AverageRating => Reviews.Any() ? Math.Round(Reviews.Average(r => r.Rating), 1) : (Product.Rating > 0 ? Product.Rating : 5.0);
        public int TotalReviews => Reviews.Count > 0 ? Reviews.Count : Product.ReviewCount;

        public int Star5Count => Reviews.Count(r => r.Rating == 5);
        public int Star4Count => Reviews.Count(r => r.Rating == 4);
        public int Star3Count => Reviews.Count(r => r.Rating == 3);
        public int Star2Count => Reviews.Count(r => r.Rating == 2);
        public int Star1Count => Reviews.Count(r => r.Rating == 1);

        public int GetStarPercentage(int starCount)
        {
            if (Reviews.Count == 0) return 0;
            return (int)Math.Round((double)starCount / Reviews.Count * 100);
        }
    }

    public class AddReviewRequest
    {
        public int ProductId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int Rating { get; set; } = 5;
        public string Content { get; set; } = string.Empty;
        public string? VariantInfo { get; set; }
    }

    public class CategoryViewModel
    {
        public Category Category { get; set; } = null!;
        public List<Product> Products { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();
        public string? SelectedBrand { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SortOrder { get; set; }
        public int TotalCount { get; set; }
    }
}
