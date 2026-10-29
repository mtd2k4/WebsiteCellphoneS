using Microsoft.AspNetCore.Mvc.Rendering;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Areas.Admin.Models
{
    public class ProductFormViewModel
    {
        public Product Product { get; set; } = new Product();
        public IEnumerable<SelectListItem> CategoryList { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> BrandList { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
