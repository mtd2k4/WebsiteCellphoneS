using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebsiteBanDienThoai.Models
{
    public enum WarrantyCardStatus
    {
        [Display(Name = "Còn hiệu lực")]
        Active = 0,

        [Display(Name = "Sắp hết hạn")]
        ExpiringSoon = 1,

        [Display(Name = "Đã hết hạn")]
        Expired = 2,

        [Display(Name = "Đã hủy / Bị từ chối")]
        Void = 3
    }

    public class WarrantyCard
    {
        [Key]
        public int Id { get; set; }

        [ValidateNever]
        [MaxLength(50)]
        public string WarrantyCode { get; set; } = string.Empty; // e.g. BH-20261028-0042

        public int? OrderId { get; set; }
        [ForeignKey("OrderId")]
        [ValidateNever]
        public virtual Order? Order { get; set; }

        [MaxLength(50)]
        public string? OrderCode { get; set; }

        public int? OrderDetailId { get; set; }

        public int? ProductId { get; set; }
        [ForeignKey("ProductId")]
        [ValidateNever]
        public virtual Product? Product { get; set; }

        public int? ProductVariantId { get; set; }
        [ForeignKey("ProductVariantId")]
        [ValidateNever]
        public virtual ProductVariant? ProductVariant { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        [ValidateNever]
        public virtual AppUser? User { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khách hàng")]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên thiết bị")]
        [MaxLength(255)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? VariantInfo { get; set; } // e.g. Titan Tự Nhiên / 256GB

        [MaxLength(500)]
        public string? ProductImageUrl { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số IMEI hoặc Số Serial")]
        [MaxLength(100)]
        public string SerialNumber { get; set; } = string.Empty; // IMEI / Serial

        [Required(ErrorMessage = "Vui lòng chọn hoặc nhập gói bảo hành")]
        [MaxLength(100)]
        public string WarrantyPackage { get; set; } = "12 tháng chính hãng CellphoneS";

        public int WarrantyPeriodMonths { get; set; } = 12;

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(12);

        [MaxLength(255)]
        public string ServiceCenter { get; set; } = "Điện Thoại Vui AASP - 136 Nguyễn Thái Học, Quận 1, TP.HCM (Hotline: 1800.2064)";

        [MaxLength(2000)]
        public string? WarrantyTerms { get; set; } = "Bảo hành 1 đổi 1 trong 30 ngày đầu nếu lỗi phần cứng do nhà sản xuất. Sửa chữa thay thế linh kiện chính hãng miễn phí tại hệ thống Điện Thoại Vui. Không bảo hành các trường hợp rơi vỡ nứt bể, cấn móp, ngấm chất lỏng hoặc tự ý can thiệp phần cứng.";

        public WarrantyCardStatus Status { get; set; } = WarrantyCardStatus.Active;

        [MaxLength(1000)]
        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Computed status based on dates unless manually voided
        [NotMapped]
        public WarrantyCardStatus EffectiveStatus
        {
            get
            {
                if (Status == WarrantyCardStatus.Void) return WarrantyCardStatus.Void;
                if (DateTime.UtcNow > EndDate) return WarrantyCardStatus.Expired;
                if ((EndDate - DateTime.UtcNow).TotalDays <= 30) return WarrantyCardStatus.ExpiringSoon;
                return WarrantyCardStatus.Active;
            }
        }

        [NotMapped]
        public bool IsActiveNow => EffectiveStatus == WarrantyCardStatus.Active || EffectiveStatus == WarrantyCardStatus.ExpiringSoon;

        [NotMapped]
        public int RemainingDays => (EndDate - DateTime.UtcNow).Days > 0 ? (EndDate - DateTime.UtcNow).Days : 0;

        [NotMapped]
        public int TotalDays => Math.Max(1, (EndDate - StartDate).Days);

        [NotMapped]
        public int UsedPercent
        {
            get
            {
                var passed = (DateTime.UtcNow - StartDate).Days;
                if (passed <= 0) return 0;
                var pct = (int)((passed / (double)TotalDays) * 100);
                return Math.Clamp(pct, 0, 100);
            }
        }
    }
}
