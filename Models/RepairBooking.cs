using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebsiteBanDienThoai.Models
{
    public enum RepairStatus
    {
        [Display(Name = "Chờ tiếp nhận")]
        Pending = 0,

        [Display(Name = "Đã tiếp nhận máy")]
        Received = 1,

        [Display(Name = "Đang kiểm tra & sửa chữa")]
        Repairing = 2,

        [Display(Name = "Đã sửa xong (Chờ khách lấy)")]
        ReadyForPickup = 3,

        [Display(Name = "Đã hoàn thành bàn giao")]
        Completed = 4,

        [Display(Name = "Đã hủy lịch")]
        Cancelled = 5
    }

    public class RepairBooking
    {
        [Key]
        public int Id { get; set; }

        [ValidateNever]
        [MaxLength(50)]
        public string BookingCode { get; set; } = string.Empty; // e.g. DTV-20261028-1029

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        [ValidateNever]
        public virtual AppUser? User { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại thiết bị")]
        [MaxLength(50)]
        public string DeviceType { get; set; } = "Điện thoại"; // Điện thoại, Laptop, Máy tính bảng, Tai nghe, Đồng hồ thông minh...

        [Required(ErrorMessage = "Vui lòng nhập tên thiết bị / model máy")]
        [MaxLength(150)]
        public string DeviceName { get; set; } = string.Empty; // e.g. iPhone 15 Pro Max, Samsung Galaxy S24 Ultra

        [MaxLength(50)]
        public string? SerialNumber { get; set; } // IMEI hoặc Serial

        [Required(ErrorMessage = "Vui lòng chọn dịch vụ cần sửa chữa")]
        [MaxLength(100)]
        public string ServiceType { get; set; } = string.Empty; // Thay Pin, Thay Màn hình, Ép kính, Lỗi nguồn, Loa / Mic, Vệ sinh máy...

        [MaxLength(1000)]
        public string? IssueDescription { get; set; } // Mô tả chi tiết triệu chứng lỗi

        public bool HasWarranty { get; set; } = false; // Máy mua tại CellphoneS còn hạn bảo hành

        [MaxLength(50)]
        public string? OrderCode { get; set; } // Mã đơn mua hàng

        [Required(ErrorMessage = "Vui lòng chọn Tỉnh / Thành phố")]
        [MaxLength(50)]
        public string City { get; set; } = "Hồ Chí Minh";

        [Required(ErrorMessage = "Vui lòng chọn chi nhánh tiếp nhận")]
        [MaxLength(255)]
        public string StoreBranch { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày hẹn")]
        public DateTime BookingDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khung giờ hẹn")]
        [MaxLength(50)]
        public string TimeSlot { get; set; } = "09:00 - 10:30";

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedCost { get; set; } = 0; // Chi phí dự kiến

        public RepairStatus Status { get; set; } = RepairStatus.Pending;

        [MaxLength(500)]
        public string? TechnicianNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
