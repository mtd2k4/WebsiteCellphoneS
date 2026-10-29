using System.ComponentModel.DataAnnotations;

namespace WebsiteBanDienThoai.Models
{
    public class AppUser
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(50)]
        public string Role { get; set; } = "Customer"; // "Admin" or "Customer"

        public int SmemberPoints { get; set; } = 0;

        [StringLength(50)]
        public string SmemberLevel { get; set; } = "S-New"; // "S-New", "S-Mem", "S-VIP"

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? Province { get; set; } = "Hồ Chí Minh";

        [StringLength(20)]
        public string? Gender { get; set; } = "Nam";

        public DateTime? Birthday { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
