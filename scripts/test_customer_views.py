import requests
import re

s = requests.Session()

# 1. Login as customer
r = s.get("https://perfumeluxe.somee.com/account/login")
match = re.search(r'name=["\']__RequestVerificationToken["\'].*?value=["\']([^"\']+)["\']', r.text)
token = match.group(1) if match else ""

r_post = s.post("https://perfumeluxe.somee.com/account/login", data={
    "email": "testvip2026@perfumeluxe.vn",
    "password": "TestVip2026!@#",
    "__RequestVerificationToken": token,
    "rememberMe": "true"
}, headers={"Referer": "https://perfumeluxe.somee.com/account/login"})
print("1. Customer Login status:", r_post.status_code, "Final URL:", r_post.url)

# 2. Check MyOrders
r_orders = s.get("https://perfumeluxe.somee.com/account/myorders")
print("2. MyOrders status:", r_orders.status_code)
# verify customer-api.js script tag does NOT have defer
if '/js/customer-api.js' in r_orders.text:
    print("   -> customer-api.js script tag found!")
    for line in r_orders.text.splitlines():
        if 'customer-api.js' in line:
            print("   -> Line:", line.strip())

# 3. Check OrderDetails
r_detail = s.get("https://perfumeluxe.somee.com/account/orderdetails/1")
print("3. OrderDetails status:", r_detail.status_code)

# 4. Check Compare page
r_comp = s.get("https://perfumeluxe.somee.com/so-sanh")
print("4. Compare page status:", r_comp.status_code)
if '<main style=' not in r_comp.text and 'compare-page-container' in r_comp.text:
    print("   -> BUG-P3-04 (Nested <main> tag) is RESOLVED! Container is .compare-page-container")

