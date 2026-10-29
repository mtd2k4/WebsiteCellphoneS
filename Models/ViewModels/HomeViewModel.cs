namespace WebsiteBanDienThoai.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; } = new();
        public List<Banner> MainBanners { get; set; } = new();
        public List<Banner> RightBanners { get; set; } = new();
        public List<Banner> BigBanners { get; set; } = new();
        public Banner? PopupBanner { get; set; }
        public List<Product> FlashSaleProducts { get; set; } = new();
        public List<Product> FeaturedPhones { get; set; } = new();
        public List<Product> FeaturedLaptops { get; set; } = new();
        public List<Product> FeaturedAudio { get; set; } = new();
        public List<Product> FeaturedWatches { get; set; } = new();
        public List<Product> FeaturedAccessories { get; set; } = new();
        public List<Product> FeaturedSmartHome { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();
        public List<NewsArticle> LatestNews { get; set; } = new();
    }
}
