using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Data;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Controllers
{
    public class PolicyController : Controller
    {
        private readonly AppDbContext _context;

        public PolicyController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Mua hàng và thanh toán Online
        [Route("chinh-sach/thanh-toan-online")]
        [Route("Policy/PaymentGuide")]
        public IActionResult PaymentGuide()
        {
            ViewData["ActivePolicy"] = "PaymentGuide";
            return View();
        }

        // 2. Mua hàng trả góp 0%
        [Route("chinh-sach/tra-gop-0-phan-tram")]
        [Route("Policy/Installment")]
        public IActionResult Installment()
        {
            ViewData["ActivePolicy"] = "Installment";
            return View();
        }

        // 3. Chính sách giao hàng
        [Route("chinh-sach/giao-hang")]
        [Route("Policy/Shipping")]
        public IActionResult Shipping()
        {
            ViewData["ActivePolicy"] = "Shipping";
            return View();
        }

        // 4. Tra cứu hóa đơn điện tử
        [HttpGet]
        [Route("tra-cuu-hoa-don")]
        [Route("Policy/InvoiceLookup")]
        [Route("Policy/EInvoice")]
        public async Task<IActionResult> InvoiceLookup(string? orderCode, string? phone)
        {
            ViewData["ActivePolicy"] = "InvoiceLookup";
            ViewBag.OrderCode = orderCode?.Trim() ?? "";
            ViewBag.Phone = phone?.Trim() ?? "";

            Order? foundOrder = null;
            if (!string.IsNullOrWhiteSpace(orderCode) || !string.IsNullOrWhiteSpace(phone))
            {
                var query = _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(orderCode))
                {
                    var cleanCode = orderCode.Trim();
                    query = query.Where(o => o.OrderCode == cleanCode || o.Id.ToString() == cleanCode);
                }

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    var cleanPhone = phone.Trim();
                    query = query.Where(o => o.PhoneNumber.Contains(cleanPhone));
                }

                foundOrder = await query.OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
                ViewBag.Searched = true;
            }
            else
            {
                ViewBag.Searched = false;
            }

            return View(foundOrder);
        }

        // AJAX API tra cứu hóa đơn điện tử
        [HttpPost]
        [Route("Policy/SearchInvoiceAjax")]
        public async Task<IActionResult> SearchInvoiceAjax([FromForm] string orderCode, [FromForm] string phone)
        {
            if (string.IsNullOrWhiteSpace(orderCode) && string.IsNullOrWhiteSpace(phone))
            {
                return Json(new { success = false, message = "Vui lòng nhập Mã đơn hàng hoặc Số điện thoại đặt hàng." });
            }

            var query = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(orderCode))
            {
                var cleanCode = orderCode.Trim();
                query = query.Where(o => o.OrderCode == cleanCode || o.Id.ToString() == cleanCode);
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var cleanPhone = phone.Trim();
                query = query.Where(o => o.PhoneNumber.Contains(cleanPhone));
            }

            var order = await query.OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin hóa đơn cho đơn hàng yêu cầu. Vui lòng kiểm tra lại mã đơn hoặc số điện thoại." });
            }

            // Tính toán VAT 10%
            decimal preTax = Math.Round(order.TotalAmount / 1.1m, 0);
            decimal vatAmount = order.TotalAmount - preTax;

            return Json(new
            {
                success = true,
                order = new
                {
                    order.Id,
                    order.OrderCode,
                    order.CustomerName,
                    order.PhoneNumber,
                    Email = order.Email ?? "Chưa cung cấp",
                    order.Address,
                    order.Province,
                    order.TotalAmount,
                    order.PaymentMethod,
                    order.SubtotalAmount,
                    order.ShippingFee,
                    order.OrderDiscountAmount,
                    order.ShippingDiscountAmount,
                    CreatedAt = order.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    InvoiceNumber = $"HD-{order.CreatedAt:yyyyMMdd}-{order.Id:D5}",
                    TemplateCode = "1C26TND",
                    TaxCode = "0107778899",
                    SellerName = "CÔNG TY CỔ PHẦN BÁN LẺ CELLPHONES",
                    PreTaxAmount = preTax,
                    VatAmount = vatAmount,
                    Items = order.OrderDetails.Select(d => new
                    {
                        ProductName = d.Product != null ? d.Product.Name : "Sản phẩm",
                        d.Quantity,
                        d.UnitPrice,
                        TotalPrice = d.Quantity * d.UnitPrice
                    })
                }
            });
        }

        // 5. Chính sách bảo hành
        [Route("chinh-sach/bao-hanh")]
        [Route("Policy/Warranty")]
        public IActionResult Warranty()
        {
            ViewData["ActivePolicy"] = "Warranty";
            return View();
        }

        // 6. Chính sách bảo mật thông tin
        [Route("chinh-sach/bao-mat-thong-tin")]
        [Route("Policy/Privacy")]
        public IActionResult Privacy()
        {
            ViewData["ActivePolicy"] = "Privacy";
            return View();
        }
    }
}
