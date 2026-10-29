using System.Collections.Generic;

namespace WebsiteBanDienThoai.Models.ViewModels
{
    public class TechNewsViewModel
    {
        public NewsArticle? FeaturedArticle { get; set; }
        public List<NewsArticle> TrendingArticles { get; set; } = new List<NewsArticle>();
        public List<NewsArticle> Articles { get; set; } = new List<NewsArticle>();
        public List<string> Categories { get; set; } = new List<string>();
        public string? CurrentCategory { get; set; }
        public string? CurrentSearch { get; set; }
        public List<string> HotTags { get; set; } = new List<string>();
        public List<Product> RelatedProducts { get; set; } = new List<Product>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalArticles { get; set; } = 0;
    }
}
