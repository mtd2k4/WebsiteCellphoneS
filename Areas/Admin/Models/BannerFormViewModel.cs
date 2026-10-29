using Microsoft.AspNetCore.Http;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Areas.Admin.Models
{
    public class BannerFormViewModel
    {
        public Banner Banner { get; set; } = new Banner();
        public IFormFile? ImageFile { get; set; }
    }
}
