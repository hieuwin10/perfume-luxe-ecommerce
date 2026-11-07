import requests
import re

s = requests.Session()

# 1. Login
r = s.get("https://perfumeluxe.somee.com/account/login")
match = re.search(r'name=["\']__RequestVerificationToken["\'].*?value=["\']([^"\']+)["\']', r.text)
token = match.group(1) if match else ""

r_post = s.post("https://perfumeluxe.somee.com/account/login", data={
    "email": "admin@perfumeluxe.vn",
    "password": "AdminPassword123!",
    "__RequestVerificationToken": token,
    "rememberMe": "true"
}, headers={"Referer": "https://perfumeluxe.somee.com/account/login"})
print("1. Login status:", r_post.status_code)

# 2. Check SystemHealth
r_health = s.get("https://perfumeluxe.somee.com/admin/admindashboard/systemhealth")
html = r_health.text
idx = html.find("Cơ Sở Dữ Liệu SQL")
if idx != -1:
    snippet = html[idx:idx+350]
    print("\n2. SystemHealth SQL Section Snippet:")
    print(snippet.replace('\n', ' '))
    if "10b981" in snippet or "Trực" in snippet:
        print(">>> RESULT: CƠ SỞ DỮ LIỆU SQL ĐANG TRỰC TUYẾN (ONLINE) HOÀN TOÀN XANH! <<<")
    elif "ef4444" in snippet:
        print(">>> RESULT: CƠ SỞ DỮ LIỆU SQL NGOẠI TUYẾN (RED)")
else:
    print("SQL Section not found in SystemHealth")
