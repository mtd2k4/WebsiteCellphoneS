using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Services
{
    public class SmtpSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string SenderName { get; set; } = "CellphoneS - Hệ thống bán lẻ di động";
        public string SenderEmail { get; set; } = string.Empty;
        public string AppPassword { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
    }

    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? fallbackFileName = null);
        Task SendOrderEmailsAsync(Order order, List<CartItem> cartItems, int earnedPoints);
        Task SendOrderStatusUpdateEmailAsync(Order order, string statusTitle, string statusMessage);
    }

    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtp;
        private readonly ILogger<EmailService> _logger;
        private readonly IWebHostEnvironment _env;

        public EmailService(
            IOptions<SmtpSettings> smtpOptions,
            ILogger<EmailService> logger,
            IWebHostEnvironment env)
        {
            _smtp = smtpOptions.Value;
            _logger = logger;
            _env = env;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? fallbackFileName = null)
        {
            // Luôn lưu một bản lưu trữ HTML tại wwwroot/emails để dễ dàng đối chiếu / kiểm thử
            try
            {
                var emailDir = Path.Combine(_env.WebRootPath, "emails");
                if (!Directory.Exists(emailDir))
                {
                    Directory.CreateDirectory(emailDir);
                }

                var safeFileName = string.IsNullOrWhiteSpace(fallbackFileName)
                    ? $"email_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}.html"
                    : fallbackFileName;

                var filePath = Path.Combine(emailDir, safeFileName);
                await File.WriteAllTextAsync(filePath, htmlBody, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể lưu bản sao email HTML tại wwwroot/emails.");
            }

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("Bỏ qua gửi SMTP vì địa chỉ email người nhận trống.");
                return false;
            }

            // Kiểm tra cấu hình SMTP đã được điền thông tin thực tế hay chưa
            bool isPlaceholder = string.IsNullOrWhiteSpace(_smtp.SenderEmail) ||
                                 string.IsNullOrWhiteSpace(_smtp.AppPassword) ||
                                 _smtp.SenderEmail.Contains("your-email", StringComparison.OrdinalIgnoreCase) ||
                                 _smtp.AppPassword.Contains("your-", StringComparison.OrdinalIgnoreCase);

            if (isPlaceholder)
            {
                _logger.LogInformation(
                    "[SMTP SIMULATION] Chưa cấu hình Gmail AppPassword thật trong appsettings.json. " +
                    "Email tới '{ToEmail}' với tiêu đề '{Subject}' đã được xuất ra thư mục wwwroot/emails.",
                    toEmail, subject);
                return false;
            }

            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(_smtp.SenderEmail, _smtp.SenderName, Encoding.UTF8);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.SubjectEncoding = Encoding.UTF8;
                message.Body = htmlBody;
                message.BodyEncoding = Encoding.UTF8;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(_smtp.Host, _smtp.Port)
                {
                    EnableSsl = _smtp.EnableSsl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_smtp.SenderEmail, _smtp.AppPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 15000
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("[SMTP SUCCESS] Đã gửi email thành công tới '{ToEmail}' - Tiêu đề: '{Subject}'", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SMTP ERROR] Lỗi khi gửi email qua SMTP tới '{ToEmail}' (Host: {Host}:{Port}).", toEmail, _smtp.Host, _smtp.Port);
                return false;
            }
        }

        public async Task SendOrderEmailsAsync(Order order, List<CartItem> cartItems, int earnedPoints)
        {
            // 1. Gửi email xác nhận đặt hàng thành công cho Khách hàng (nếu khách có nhập Email)
            if (!string.IsNullOrWhiteSpace(order.Email))
            {
                var customerSubject = $"[CellphoneS] Xác nhận đặt hàng thành công #{order.OrderCode}";
                var customerHtml = BuildCustomerOrderEmailHtml(order, cartItems, earnedPoints);
                await SendEmailAsync(order.Email, customerSubject, customerHtml, $"Order_{order.OrderCode}_Customer.html");
            }

            // 2. Gửi email thông báo có đơn hàng mới về cho Admin
            var adminTargetEmail = !string.IsNullOrWhiteSpace(_smtp.AdminEmail)
                ? _smtp.AdminEmail
                : _smtp.SenderEmail;

            var adminSubject = $"[CellphoneS Admin] 🔔 Đơn hàng mới #{order.OrderCode} - {order.TotalAmount:N0}₫ ({order.CustomerName})";
            var adminHtml = BuildAdminNewOrderEmailHtml(order, cartItems);
            await SendEmailAsync(adminTargetEmail, adminSubject, adminHtml, $"Order_{order.OrderCode}_Admin.html");
        }

        public async Task SendOrderStatusUpdateEmailAsync(Order order, string statusTitle, string statusMessage)
        {
            if (string.IsNullOrWhiteSpace(order.Email))
                return;

            var subject = $"[CellphoneS] Cập nhật đơn hàng #{order.OrderCode}: {statusTitle}";
            var html = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""utf-8"">
</head>
<body style=""margin:0;padding:0;background-color:#f4f6f8;font-family:'Segoe UI',Arial,sans-serif;color:#1e293b;"">
    <div style=""max-width:640px;margin:24px auto;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e2e8f0;box-shadow:0 4px 12px rgba(0,0,0,0.05);"">
        <div style=""background:linear-gradient(135deg,#d70018 0%,#b30014 100%);padding:24px;text-align:center;color:#ffffff;"">
            <h2 style=""margin:0;font-size:22px;font-weight:800;letter-spacing:0.5px;"">CELLPHONES</h2>
            <p style=""margin:6px 0 0;font-size:13px;opacity:0.9;"">Thông báo cập nhật trạng thái đơn hàng</p>
        </div>
        <div style=""padding:28px 24px;"">
            <p style=""margin:0 0 12px;font-size:15px;"">Xin chào <strong>{WebUtility.HtmlEncode(order.CustomerName)}</strong>,</p>
            <p style=""margin:0 0 16px;font-size:14px;line-height:1.6;color:#334155;"">{WebUtility.HtmlEncode(statusMessage)}</p>
            <div style=""background:#f8fafc;border-left:4px solid #d70018;padding:16px;border-radius:6px;margin-bottom:20px;"">
                <div style=""font-size:13px;color:#64748b;"">Mã đơn hàng:</div>
                <div style=""font-size:18px;font-weight:800;color:#d70018;margin:4px 0;"">#{WebUtility.HtmlEncode(order.OrderCode)}</div>
                <div style=""font-size:13px;color:#334155;"">Trạng thái mới: <strong>{WebUtility.HtmlEncode(statusTitle)}</strong></div>
                <div style=""font-size:13px;color:#334155;margin-top:4px;"">Tổng thanh toán: <strong>{order.TotalAmount:N0}₫</strong></div>
            </div>
            <p style=""font-size:13px;color:#64748b;line-height:1.5;margin:0;"">
                Mọi thắc mắc vui lòng liên hệ tổng đài miễn phí <strong>1800.2097</strong> (8h00 - 21h30).
            </p>
        </div>
        <div style=""background:#f1f5f9;padding:14px 24px;text-align:center;font-size:12px;color:#64748b;"">
            © {DateTime.Now.Year} CellphoneS - Hệ thống bán lẻ điện thoại, máy tính, phụ kiện chính hãng.
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(order.Email, subject, html, $"Order_{order.OrderCode}_StatusUpdate.html");
        }

        private static string BuildCustomerOrderEmailHtml(Order order, List<CartItem> cartItems, int earnedPoints)
        {
            var rowsSb = new StringBuilder();
            foreach (var item in cartItems)
            {
                var variantText = !string.IsNullOrWhiteSpace(item.VariantInfo)
                    ? $"<div style='font-size:12px;color:#64748b;'>Phân loại: {WebUtility.HtmlEncode(item.VariantInfo)}</div>"
                    : "";

                rowsSb.Append($@"
                <tr>
                    <td style=""padding:12px 10px;border-bottom:1px solid #e2e8f0;font-size:13.5px;"">
                        <strong>{WebUtility.HtmlEncode(item.ProductName)}</strong>
                        {variantText}
                    </td>
                    <td style=""padding:12px 10px;border-bottom:1px solid #e2e8f0;text-align:center;font-size:13.5px;"">x{item.Quantity}</td>
                    <td style=""padding:12px 10px;border-bottom:1px solid #e2e8f0;text-align:right;font-size:13.5px;font-weight:700;color:#d70018;"">{item.TotalPrice:N0}₫</td>
                </tr>");
            }

            var paymentMethodName = order.PaymentMethod == "COD"
                ? "Thanh toán khi nhận hàng (COD)"
                : "Chuyển khoản ngân hàng (VietQR)";

            var discountRow = (!string.IsNullOrWhiteSpace(order.OrderDiscountCode) && order.OrderDiscountAmount > 0)
                ? $@"<tr>
                        <td colspan=""2"" style=""padding:6px 10px;text-align:right;font-size:13px;color:#d70018;"">Mã giảm giá ({WebUtility.HtmlEncode(order.OrderDiscountCode)}):</td>
                        <td style=""padding:6px 10px;text-align:right;font-size:13px;font-weight:700;color:#d70018;"">-{order.OrderDiscountAmount:N0}₫</td>
                     </tr>"
                : "";

            var shipText = (order.ShippingFee - order.ShippingDiscountAmount) <= 0
                ? "<span style='color:#16a34a;font-weight:700;'>MIỄN PHÍ</span>"
                : $"{(order.ShippingFee - order.ShippingDiscountAmount):N0}₫";

            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""utf-8"">
    <title>Xác nhận đơn hàng #{WebUtility.HtmlEncode(order.OrderCode)}</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f6f8;font-family:'Segoe UI',Arial,sans-serif;color:#1e293b;"">
    <div style=""max-width:660px;margin:24px auto;background:#ffffff;border-radius:14px;overflow:hidden;border:1px solid #e2e8f0;box-shadow:0 6px 20px rgba(0,0,0,0.06);"">
        <!-- Header -->
        <div style=""background:linear-gradient(135deg,#d70018 0%,#b30014 100%);padding:28px 24px;text-align:center;color:#ffffff;"">
            <div style=""font-size:26px;font-weight:900;letter-spacing:0.5px;"">cellphone<span style=""background:#ffffff;color:#d70018;padding:0 6px;border-radius:4px;margin-left:2px;"">S</span></div>
            <h2 style=""margin:12px 0 4px;font-size:20px;font-weight:700;"">ĐẶT HÀNG THÀNH CÔNG!</h2>
            <p style=""margin:0;font-size:13px;opacity:0.9;"">Cảm ơn quý khách đã tin tưởng mua sắm tại hệ thống CellphoneS</p>
        </div>

        <!-- Body -->
        <div style=""padding:28px 24px;"">
            <p style=""margin:0 0 12px;font-size:15px;"">Xin chào <strong>{WebUtility.HtmlEncode(order.CustomerName)}</strong>,</p>
            <p style=""margin:0 0 20px;font-size:14px;line-height:1.6;color:#475569;"">
                Hệ thống CellphoneS đã tiếp nhận đơn hàng của quý khách và đang tiến hành kiểm tra, đóng gói sản phẩm. Nhân viên tư vấn sẽ sớm liên hệ qua số điện thoại <strong>{WebUtility.HtmlEncode(order.PhoneNumber)}</strong> để xác nhận giao hàng.
            </p>

            <!-- Order Summary Badge -->
            <div style=""background:#fff5f5;border:1px dashed #d70018;border-radius:10px;padding:16px;text-align:center;margin-bottom:22px;"">
                <div style=""font-size:12px;color:#64748b;text-transform:uppercase;letter-spacing:0.5px;"">Mã số đơn hàng của bạn</div>
                <div style=""font-size:24px;font-weight:800;color:#d70018;margin:4px 0;"">#{WebUtility.HtmlEncode(order.OrderCode)}</div>
                <div style=""font-size:12.5px;color:#16a34a;font-weight:600;"">★ Tích lũy thành công +{earnedPoints:N0} điểm thưởng Smember</div>
            </div>

            <!-- Customer & Shipping Details -->
            <h3 style=""font-size:15px;font-weight:700;border-bottom:2px solid #f1f5f9;padding-bottom:8px;margin:0 0 12px;color:#0f172a;"">
                1. Thông tin nhận hàng
            </h3>
            <table style=""width:100%;font-size:13.5px;margin-bottom:22px;border-collapse:collapse;"">
                <tr>
                    <td style=""padding:5px 0;color:#64748b;width:150px;"">Người nhận:</td>
                    <td style=""padding:5px 0;font-weight:600;"">{WebUtility.HtmlEncode(order.CustomerName)}</td>
                </tr>
                <tr>
                    <td style=""padding:5px 0;color:#64748b;"">Số điện thoại:</td>
                    <td style=""padding:5px 0;font-weight:600;"">{WebUtility.HtmlEncode(order.PhoneNumber)}</td>
                </tr>
                <tr>
                    <td style=""padding:5px 0;color:#64748b;"">Địa chỉ giao hàng:</td>
                    <td style=""padding:5px 0;font-weight:600;"">{WebUtility.HtmlEncode(order.Address)}, {WebUtility.HtmlEncode(order.Province)}</td>
                </tr>
                <tr>
                    <td style=""padding:5px 0;color:#64748b;"">Phương thức TT:</td>
                    <td style=""padding:5px 0;font-weight:600;"">{paymentMethodName}</td>
                </tr>
                <tr>
                    <td style=""padding:5px 0;color:#64748b;"">Thời gian đặt:</td>
                    <td style=""padding:5px 0;"">{order.CreatedAt.ToLocalTime():HH:mm - dd/MM/yyyy}</td>
                </tr>
            </table>

            <!-- Order Items Table -->
            <h3 style=""font-size:15px;font-weight:700;border-bottom:2px solid #f1f5f9;padding-bottom:8px;margin:0 0 12px;color:#0f172a;"">
                2. Chi tiết sản phẩm đặt mua
            </h3>
            <table style=""width:100%;border-collapse:collapse;margin-bottom:20px;"">
                <thead>
                    <tr style=""background:#f8fafc;text-align:left;font-size:12.5px;color:#64748b;"">
                        <th style=""padding:10px;border-bottom:2px solid #e2e8f0;"">Sản phẩm</th>
                        <th style=""padding:10px;border-bottom:2px solid #e2e8f0;text-align:center;width:65px;"">SL</th>
                        <th style=""padding:10px;border-bottom:2px solid #e2e8f0;text-align:right;width:130px;"">Thành tiền</th>
                    </tr>
                </thead>
                <tbody>
                    {rowsSb}
                </tbody>
                <tfoot>
                    <tr>
                        <td colspan=""2"" style=""padding:10px 10px 4px;text-align:right;font-size:13px;color:#64748b;"">Tạm tính:</td>
                        <td style=""padding:10px 10px 4px;text-align:right;font-size:13px;font-weight:600;"">{order.SubtotalAmount:N0}₫</td>
                    </tr>
                    {discountRow}
                    <tr>
                        <td colspan=""2"" style=""padding:4px 10px;text-align:right;font-size:13px;color:#64748b;"">Phí giao hàng:</td>
                        <td style=""padding:4px 10px;text-align:right;font-size:13px;"">{shipText}</td>
                    </tr>
                    <tr>
                        <td colspan=""2"" style=""padding:12px 10px;text-align:right;font-size:15px;font-weight:700;border-top:2px solid #e2e8f0;"">Tổng thanh toán:</td>
                        <td style=""padding:12px 10px;text-align:right;font-size:18px;font-weight:800;color:#d70018;border-top:2px solid #e2e8f0;"">{order.TotalAmount:N0}₫</td>
                    </tr>
                </tfoot>
            </table>

            <div style=""background:#f8fafc;border-radius:8px;padding:14px;font-size:13px;color:#475569;line-height:1.5;"">
                <strong>Hỗ trợ khách hàng CellphoneS:</strong><br>
                • Tổng đài tư vấn miễn phí: <strong>1800.2097</strong> (8h00 - 21h30)<br>
                • Bảo hành chính hãng toàn quốc tại hơn 120 cửa hàng CellphoneS & Điện Thoại Vui.
            </div>
        </div>

        <!-- Footer -->
        <div style=""background:#0f172a;color:#94a3b8;padding:18px 24px;text-align:center;font-size:12px;"">
            © {DateTime.Now.Year} CellphoneS. Email này được gửi tự động từ hệ thống thông báo đơn hàng.
        </div>
    </div>
</body>
</html>";
        }

        private static string BuildAdminNewOrderEmailHtml(Order order, List<CartItem> cartItems)
        {
            var rowsSb = new StringBuilder();
            foreach (var item in cartItems)
            {
                var variantText = !string.IsNullOrWhiteSpace(item.VariantInfo)
                    ? $" ({WebUtility.HtmlEncode(item.VariantInfo)})"
                    : "";

                rowsSb.Append($@"
                <tr>
                    <td style=""padding:10px;border-bottom:1px solid #e2e8f0;font-size:13px;"">
                        <strong>{WebUtility.HtmlEncode(item.ProductName)}</strong>{variantText}
                    </td>
                    <td style=""padding:10px;border-bottom:1px solid #e2e8f0;text-align:center;font-size:13px;"">x{item.Quantity}</td>
                    <td style=""padding:10px;border-bottom:1px solid #e2e8f0;text-align:right;font-size:13px;font-weight:700;color:#d70018;"">{item.TotalPrice:N0}₫</td>
                </tr>");
            }

            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""utf-8"">
    <title>[Admin] Đơn hàng mới #{WebUtility.HtmlEncode(order.OrderCode)}</title>
</head>
<body style=""margin:0;padding:0;background-color:#f1f5f9;font-family:'Segoe UI',Arial,sans-serif;color:#1e293b;"">
    <div style=""max-width:660px;margin:24px auto;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #cbd5e1;box-shadow:0 4px 16px rgba(15,23,42,0.08);"">
        <div style=""background:#0f172a;padding:22px 24px;color:#ffffff;border-bottom:4px solid #d70018;"">
            <div style=""font-size:12px;color:#f87171;font-weight:700;text-transform:uppercase;letter-spacing:0.8px;"">🔔 HỆ THỐNG QUẢN TRỊ CELLPHONES</div>
            <h2 style=""margin:6px 0 0;font-size:20px;font-weight:800;"">PHÁT SINH ĐƠN HÀNG MỚI #{WebUtility.HtmlEncode(order.OrderCode)}</h2>
        </div>

        <div style=""padding:24px;"">
            <div style=""background:#fef2f2;border-left:4px solid #d70018;padding:14px 16px;border-radius:6px;margin-bottom:20px;font-size:13.5px;"">
                Khách hàng <strong>{WebUtility.HtmlEncode(order.CustomerName)}</strong> ({WebUtility.HtmlEncode(order.PhoneNumber)}) vừa hoàn tất đặt hàng mới với tổng giá trị <strong style=""color:#d70018;font-size:15px;"">{order.TotalAmount:N0}₫</strong>. Vui lòng kiểm tra và xác nhận đơn hàng!
            </div>

            <h3 style=""font-size:14.5px;font-weight:700;margin:0 0 10px;color:#0f172a;"">Thông tin khách hàng & giao nhận</h3>
            <table style=""width:100%;font-size:13px;margin-bottom:20px;border-collapse:collapse;background:#f8fafc;border-radius:8px;"">
                <tr>
                    <td style=""padding:8px 12px;color:#64748b;width:150px;"">Mã đơn hàng:</td>
                    <td style=""padding:8px 12px;font-weight:700;color:#d70018;"">#{WebUtility.HtmlEncode(order.OrderCode)} (ID: {order.Id})</td>
                </tr>
                <tr>
                    <td style=""padding:8px 12px;color:#64748b;"">Họ tên khách:</td>
                    <td style=""padding:8px 12px;font-weight:600;"">{WebUtility.HtmlEncode(order.CustomerName)}</td>
                </tr>
                <tr>
                    <td style=""padding:8px 12px;color:#64748b;"">Số điện thoại:</td>
                    <td style=""padding:8px 12px;font-weight:600;"">{WebUtility.HtmlEncode(order.PhoneNumber)}</td>
                </tr>
                <tr>
                    <td style=""padding:8px 12px;color:#64748b;"">Email khách:</td>
                    <td style=""padding:8px 12px;"">{WebUtility.HtmlEncode(order.Email ?? "Không cung cấp")}</td>
                </tr>
                <tr>
                    <td style=""padding:8px 12px;color:#64748b;"">Địa chỉ giao:</td>
                    <td style=""padding:8px 12px;font-weight:600;"">{WebUtility.HtmlEncode(order.Address)}, {WebUtility.HtmlEncode(order.Province)}</td>
                </tr>
                <tr>
                    <td style=""padding:8px 12px;color:#64748b;"">Thanh toán:</td>
                    <td style=""padding:8px 12px;font-weight:600;"">{WebUtility.HtmlEncode(order.PaymentMethod)}</td>
                </tr>
                <tr>
                    <td style=""padding:8px 12px;color:#64748b;"">Ghi chú đơn:</td>
                    <td style=""padding:8px 12px;"">{WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(order.Notes) ? "Không có ghi chú" : order.Notes)}</td>
                </tr>
            </table>

            <h3 style=""font-size:14.5px;font-weight:700;margin:0 0 10px;color:#0f172a;"">Danh sách sản phẩm cần xuất kho</h3>
            <table style=""width:100%;border-collapse:collapse;margin-bottom:20px;"">
                <thead>
                    <tr style=""background:#e2e8f0;text-align:left;font-size:12px;color:#334155;"">
                        <th style=""padding:8px 10px;"">Sản phẩm</th>
                        <th style=""padding:8px 10px;text-align:center;width:60px;"">SL</th>
                        <th style=""padding:8px 10px;text-align:right;width:130px;"">Thành tiền</th>
                    </tr>
                </thead>
                <tbody>
                    {rowsSb}
                </tbody>
                <tfoot>
                    <tr>
                        <td colspan=""2"" style=""padding:10px;text-align:right;font-weight:700;font-size:14px;"">Tổng giá trị đơn hàng:</td>
                        <td style=""padding:10px;text-align:right;font-weight:800;font-size:16px;color:#d70018;"">{order.TotalAmount:N0}₫</td>
                    </tr>
                </tfoot>
            </table>
        </div>

        <div style=""background:#f8fafc;padding:14px 24px;text-align:center;font-size:12px;color:#64748b;border-top:1px solid #e2e8f0;"">
            Hệ thống thông báo đơn hàng tự động CellphoneS Admin Enterprise Portal
        </div>
    </div>
</body>
</html>";
        }
    }
}

