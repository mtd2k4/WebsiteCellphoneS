using System.ComponentModel.DataAnnotations;

namespace WebsiteBanDienThoai.Models
{
    public class Banner
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Subtitle { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(500)]
        public string LinkUrl { get; set; } = "#";

        // Position: "MainSlider", "RightWidget", "SpecialBanner", "MiddleBanner"
        [Required]
        [StringLength(50)]
        public string Position { get; set; } = "MainSlider";

        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}
