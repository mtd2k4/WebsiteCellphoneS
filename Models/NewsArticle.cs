using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBanDienThoai.Models
{
    public class NewsArticle
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Slug { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tóm tắt không được để trống")]
        [StringLength(1000)]
        public string Summary { get; set; } = string.Empty;

        public string? Content { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập hoặc chọn ảnh thumbnail")]
        [StringLength(500)]
        public string ThumbnailUrl { get; set; } = string.Empty;

        [StringLength(500)]
        public string? SourceUrl { get; set; }

        [StringLength(100)]
        public string Author { get; set; } = "Sforum Editor";

        [StringLength(100)]
        public string Category { get; set; } = "Tin công nghệ";

        public int ViewCount { get; set; } = 1200;

        public bool IsFeatured { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public string TimeAgo
        {
            get
            {
                var span = DateTime.UtcNow - PublishedAt;
                if (span.TotalMinutes < 1) return "Vừa xong";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
                if (span.TotalDays < 7) return $"{(int)span.TotalDays} ngày trước";
                return PublishedAt.ToString("dd/MM/yyyy");
            }
        }
    }
}
