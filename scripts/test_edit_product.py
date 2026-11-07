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

# 2. Test EditProduct
for pid in [1, 2, 12]:
    r_edit = s.get(f"https://perfumeluxe.somee.com/Admin/AdminDashboard/EditProduct/{pid}")
    print(f"2. EditProduct/{pid}: status={r_edit.status_code}")
    if r_edit.status_code == 200:
        # check title
        m_title = re.search(r'<title>(.*?)</title>', r_edit.text)
        title = m_title.group(1) if m_title else "No title"
        print(f"   -> Page Title: {title}")

