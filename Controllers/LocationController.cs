using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WebsiteBanDienThoai.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private static JsonDocument? _cachedData;
        private static readonly object _lock = new();
        private readonly IWebHostEnvironment _env;

        public LocationController(IWebHostEnvironment env)
        {
            _env = env;
        }

        private JsonElement GetLocationData()
        {
            if (_cachedData == null)
            {
                lock (_lock)
                {
                    if (_cachedData == null)
                    {
                        var filePath = Path.Combine(_env.WebRootPath, "data", "vietnam-addresses.json");
                        if (System.IO.File.Exists(filePath))
                        {
                            var json = System.IO.File.ReadAllText(filePath);
                            _cachedData = JsonDocument.Parse(json);
                        }
                    }
                }
            }
            return _cachedData != null ? _cachedData.RootElement : default;
        }

        [HttpGet("provinces")]
        public IActionResult GetProvinces()
        {
            var root = GetLocationData();
            if (root.ValueKind != JsonValueKind.Array) return NotFound(new { message = "Dữ liệu địa giới hành chính chưa sẵn sàng." });

            var list = root.EnumerateArray().Select(p => new
            {
                id = p.GetProperty("Id").GetString(),
                name = p.GetProperty("Name").GetString()
            }).ToList();

            return Ok(list);
        }

        [HttpGet("districts/{provinceId}")]
        public IActionResult GetDistricts(string provinceId)
        {
            var root = GetLocationData();
            if (root.ValueKind != JsonValueKind.Array) return NotFound();

            foreach (var prov in root.EnumerateArray())
            {
                var id = prov.GetProperty("Id").GetString();
                var name = prov.GetProperty("Name").GetString();
                if (id == provinceId || string.Equals(name, provinceId, StringComparison.OrdinalIgnoreCase))
                {
                    var districts = prov.GetProperty("Districts").EnumerateArray().Select(d => new
                    {
                        id = d.GetProperty("Id").GetString(),
                        name = d.GetProperty("Name").GetString()
                    }).ToList();
                    return Ok(districts);
                }
            }

            return NotFound(new { message = "Không tìm thấy quận/huyện thuộc tỉnh thành này." });
        }

        [HttpGet("wards/{districtId}")]
        public IActionResult GetWards(string districtId)
        {
            var root = GetLocationData();
            if (root.ValueKind != JsonValueKind.Array) return NotFound();

            foreach (var prov in root.EnumerateArray())
            {
                foreach (var dist in prov.GetProperty("Districts").EnumerateArray())
                {
                    var id = dist.GetProperty("Id").GetString();
                    var name = dist.GetProperty("Name").GetString();
                    if (id == districtId || string.Equals(name, districtId, StringComparison.OrdinalIgnoreCase))
                    {
                        var wards = dist.GetProperty("Wards").EnumerateArray().Select(w => new
                        {
                            id = w.GetProperty("Id").GetString(),
                            name = w.GetProperty("Name").GetString()
                        }).ToList();
                        return Ok(wards);
                    }
                }
            }

            return NotFound(new { message = "Không tìm thấy phường/xã thuộc quận huyện này." });
        }
    }
}
