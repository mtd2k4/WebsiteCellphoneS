using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBanDienThoai.Models
{
    public class TradeInRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RequestCode { get; set; } = string.Empty; // e.g. CPS-TC-2410-0012

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual AppUser? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Email { get; set; }

        // Thông tin máy cũ muốn bán lại
        [Required]
        [MaxLength(100)]
        public string OldDeviceName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string OldDeviceStorage { get; set; } = string.Empty;

        public int ConditionGrade { get; set; } = 1; // 1: Loại 1 (99%), 2: Loại 2 (95%), 3: Loại 3 (90%), 4: Loại 4 (cấn/lỗi)

        [MaxLength(20)]
        public string ActionType { get; set; } = "Upgrade"; // "Upgrade": Lên đời máy mới, "CashOut": Bán lấy tiền mặt

        [MaxLength(20)]
        public string SmemberTier { get; set; } = "None"; // "None", "S-Mem", "S-VIP"

        [Column(TypeName = "decimal(18,2)")]
        public decimal SmemberBonus { get; set; } = 0; // Trợ giá thêm 5% cho hội viên Smember (tối đa 300k - 500k)

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseEstimatePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubsidyAmount { get; set; } // Trợ giá lên đời

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalBenefit { get; set; } // BaseEstimatePrice + SmemberBonus + SubsidyAmount

        // Thông tin máy mới muốn lên đời
        public int? TargetProductId { get; set; }
        [ForeignKey("TargetProductId")]
        public virtual Product? TargetProduct { get; set; }

        [MaxLength(150)]
        public string? TargetProductName { get; set; }

        [MaxLength(50)]
        public string? TargetProductStorage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TargetProductPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PayDifference { get; set; } // Số tiền bù chênh lệch = TargetProductPrice - TotalBenefit

        // Thông tin lịch hẹn
        [Required]
        [MaxLength(255)]
        public string BranchStore { get; set; } = "CellphoneS 213 Trần Quang Khải, Q.1, TP.HCM";

        public DateTime AppointmentDate { get; set; }

        [MaxLength(500)]
        public string? CustomerNote { get; set; }

        // Trạng thái: 0: Chờ tiếp nhận, 1: Đã xác nhận lịch hẹn, 2: Đang thẩm định tại shop, 3: Đã hoàn tất lên đời, 4: Đã hủy
        public int Status { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public string StatusBadgeClass => Status switch
        {
            0 => "bg-secondary",
            1 => "bg-primary",
            2 => "bg-warning text-dark",
            3 => "bg-success",
            4 => "bg-danger",
            _ => "bg-secondary"
        };

        [NotMapped]
        public string StatusText => Status switch
        {
            0 => "Chờ tiếp nhận",
            1 => "Đã xác nhận lịch hẹn",
            2 => "Đang thẩm định tại shop",
            3 => "Đã hoàn tất",
            4 => "Đã hủy",
            _ => "Không xác định"
        };

        [NotMapped]
        public string ConditionName => ConditionGrade switch
        {
            1 => "Loại 1 (Máy đẹp 99%)",
            2 => "Loại 2 (Máy đẹp 95%)",
            3 => "Loại 3 (Trầy xước nhiều 90%)",
            4 => "Loại 4 (Ám ố / Nứt kính nhẹ)",
            5 => "Loại 5 (Lỗi chức năng / hỏng nhẹ)",
            _ => "Loại 1"
        };
    }
}
