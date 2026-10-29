using System.ComponentModel.DataAnnotations;

namespace WebsiteBanDienThoai.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(100)]
        public string? IconClass { get; set; }

        [StringLength(255)]
        public string? IconUrl { get; set; }

        public int? ParentId { get; set; }
        public Category? Parent { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
