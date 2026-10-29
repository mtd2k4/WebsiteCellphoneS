import urllib.request
import urllib.parse
import http.cookiejar
import re

cj = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj))

# Login
page = opener.open("http://localhost:5016/Account/Login").read().decode('utf-8')
tok = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
data = urllib.parse.urlencode({
    "Username": "admin@cellphones.com.vn",
    "Password": "Admin@123",
    "RememberMe": "true",
    "__RequestVerificationToken": tok
}).encode('utf-8')
opener.open(urllib.request.Request("http://localhost:5016/Account/Login", data=data, method="POST"))

admin_urls = [
    "/Admin/Dashboard",
    "/Admin/RevenueReport",
    "/Admin/Order",
    "/Admin/Coupon",
    "/Admin/Product",
    "/Admin/Warehouse",
    "/Admin/Warranty",
    "/Admin/Repair",
    "/Admin/TradeIn",
    "/Admin/Customer",
    "/Admin/Banner",
    "/Admin/News"
]

all_ok = True
for url in admin_urls:
    try:
        r = opener.open(f"http://localhost:5016{url}")
        if r.status == 200:
            print(f"[OK 200] {url}")
        else:
            print(f"[ERR {r.status}] {url}")
            all_ok = False
    except Exception as e:
        print(f"[FAIL] {url}: {e}")
        all_ok = False

if all_ok:
    print("ALL 12 ADMIN CONTROLLER PAGES RENDERED WITH 200 OK!")
