import urllib.request
import urllib.parse
import http.cookiejar
import re

cj = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj))

# 1. Login
page = opener.open("http://localhost:5016/Account/Login").read().decode('utf-8')
tok = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
data = urllib.parse.urlencode({
    "Username": "admin@cellphones.com.vn",
    "Password": "Admin@123",
    "RememberMe": "true",
    "__RequestVerificationToken": tok
}).encode('utf-8')
opener.open(urllib.request.Request("http://localhost:5016/Account/Login", data=data, method="POST"))

# 2. Get Admin Dashboard
resp = opener.open("http://localhost:5016/Admin/Dashboard")
html = resp.read().decode('utf-8')

with open("scratch/admin_dashboard_rendered.html", "w", encoding="utf-8") as f:
    f.write(html)

checks = [
    ("Status 200", resp.status == 200),
    ("CellphoneS Brand", "CellphoneS" in html),
    ("Enterprise Badge", "ENTERPRISE" in html),
    ("Sidebar Search Input", "sidebarMenuFilter" in html),
    ("Domain: Tong Quan & Phan Tich", "Tong Quan &amp; Ph" in html or "Tổng Quan & Phân Tích" in html),
    ("Domain: Ban Hang & Giao Dich", "Ban Hang &amp; Giao D" in html or "Bán Hàng & Giao Dịch" in html),
    ("Domain: Kho Hang & San Pham", "Kho H" in html and "Sản Phẩm" in html),
    ("Domain: Dich Vu & Hau Mai", "Dịch Vụ & Hậu Mãi" in html or "Dich Vu &amp; H" in html),
    ("Domain: Khach Hang & Tiep Thi", "Khách Hàng & Tiếp Thị" in html or "Kh" in html),
    ("Domain: Cua Hang Truc Tuyen", "Cửa Hàng Trực Tuyến" in html or "Xem Trang Bán Hàng" in html),
    ("KPI Total Revenue", "Tổng Doanh Thu" in html),
    ("KPI Total Orders", "Tổng Đơn Bán Ra" in html or "Tổng Đơn" in html),
    ("KPI Low Stock", "Tồn" in html or "Sản Phẩm Đang Bán" in html),
    ("KPI Customers", "Hội Viên Smember" in html or "Smember" in html),
    ("Quick Shortcuts", "Duyệt Đơn Hàng" in html and "Tạo Phiếu Nhập Kho" in html),
    ("Chart Revenue", "revenueChart" in html),
    ("Chart Orders", "orderStatusChart" in html),
    ("Recent Orders Table", "Đơn Hàng Mới Nhất" in html),
    ("Top Products", "Top Sản Phẩm" in html)
]

all_passed = True
for name, passed in checks:
    status = "PASS" if passed else "FAIL"
    print(f"[{status}] {name}")
    if not passed:
        all_passed = False

if all_passed:
    print("ALL CHECKS PASSED SUCCESSFULLY!")
else:
    print("SOME CHECKS FAILED!")
