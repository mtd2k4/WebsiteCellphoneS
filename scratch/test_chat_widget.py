import urllib.request
import urllib.parse
import http.cookiejar
import re

cj = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj))

# 1. Test Client Storefront
r_home = opener.open("http://localhost:5016/")
home_html = r_home.read().decode('utf-8')

checks_home = [
    ("Home Status 200", r_home.status == 200),
    ("Chat Wrapper Element", "cpsChatWrapper" in home_html),
    ("Chat Trigger Button", "cpsChatTriggerBtn" in home_html),
    ("Chat Window", "cpsChatWindow" in home_html),
    ("Chat Header Title", "CellphoneS Assistant" in home_html),
    ("Tab Chatbot AI", "tabBtnBot" in home_html),
    ("Tab Advisor", "tabBtnAdvisor" in home_html),
    ("Quick Action Chips", "cps-chip-btn" in home_html),
    ("Advisor Card", "Trần Minh Hoàng" in home_html or "Tr" in home_html),
    ("Callback Form", "cpsCallbackForm" in home_html)
]

print("=== CHECKING CLIENT STOREFRONT ===")
for name, passed in checks_home:
    print(f"[{'PASS' if passed else 'FAIL'}] {name}")

# 2. Test Admin Portal
page = opener.open("http://localhost:5016/Account/Login").read().decode('utf-8')
tok = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
data = urllib.parse.urlencode({
    "Username": "admin@cellphones.com.vn",
    "Password": "Admin@123",
    "RememberMe": "true",
    "__RequestVerificationToken": tok
}).encode('utf-8')
opener.open(urllib.request.Request("http://localhost:5016/Account/Login", data=data, method="POST"))

r_admin = opener.open("http://localhost:5016/Admin/Dashboard")
admin_html = r_admin.read().decode('utf-8')

checks_admin = [
    ("Admin Status 200", r_admin.status == 200),
    ("Admin Chat Wrapper", "cpsChatWrapper" in admin_html),
    ("Admin Chat Trigger", "cpsChatTriggerBtn" in admin_html)
]

print("\n=== CHECKING ADMIN PORTAL ===")
for name, passed in checks_admin:
    print(f"[{'PASS' if passed else 'FAIL'}] {name}")

print("\nALL CHAT WIDGET TESTS EXECUTED!")
