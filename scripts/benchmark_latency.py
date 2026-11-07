import time
import requests
import json
import urllib3

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

BASE_URL = "https://perfumeluxe.somee.com"

session = requests.Session()
session.headers.update({
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) PerfumeLuxeLatencyTester/1.0"
})

def measure_request(method, endpoint, data=None, json_data=None, headers=None):
    url = f"{BASE_URL}{endpoint}" if endpoint.startswith("/") else endpoint
    start = time.perf_counter()
    try:
        if method.upper() == "GET":
            resp = session.get(url, headers=headers, timeout=15)
        elif method.upper() == "POST":
            resp = session.post(url, data=data, json=json_data, headers=headers, timeout=15)
        elif method.upper() == "PUT":
            resp = session.put(url, data=data, json=json_data, headers=headers, timeout=15)
        elapsed_sec = time.perf_counter() - start
        elapsed_ms = round(elapsed_sec * 1000, 1)
        return {
            "endpoint": endpoint,
            "method": method.upper(),
            "status_code": resp.status_code,
            "latency_ms": elapsed_ms,
            "over_1s": elapsed_sec > 1.0,
            "size_bytes": len(resp.content),
            "error": None
        }
    except Exception as e:
        elapsed_sec = time.perf_counter() - start
        return {
            "endpoint": endpoint,
            "method": method.upper(),
            "status_code": 0,
            "latency_ms": round(elapsed_sec * 1000, 1),
            "over_1s": elapsed_sec > 1.0,
            "size_bytes": 0,
            "error": str(e)
        }

print("=" * 80)
print(f"BẮT ĐẦU KIỂM THỬ HIỆU NĂNG LATENCY API & QUERY SQL TRÊN: {BASE_URL}")
print("=" * 80)

# 1. Đo lường các Public Storefront & REST APIs
public_endpoints = [
    ("GET", "/"),
    ("GET", "/bo-suu-tap"),
    ("GET", "/catalog/quiz"),
    ("GET", "/discovery-box"),
    ("GET", "/so-sanh"),
    ("GET", "/blog"),
    ("GET", "/nuoc-hoa/chanel-coco-mademoiselle-edp"),
    ("GET", "/api/v1/catalog/products"),
    ("GET", "/api/v1/catalog/products?page=1&pageSize=12&sortBy=price-asc"),
    ("GET", "/api/v1/catalog/products?keyword=Chanel&page=1&pageSize=6"),
    ("GET", "/api/v1/catalog/products/chanel-coco-mademoiselle-edp"),
    ("GET", "/api/v1/cart"),
    ("GET", "/api/v1/orders/track?orderCode=LUXE-20261107-TEST"),
    ("GET", "/swagger/v1/swagger.json"),
    ("POST", "/api/v1/vouchers/validate", None, {"code": "LUXE2026"})
]

results = []
print("\n--- 1. Kiểm thử Public Endpoints & Storefront APIs ---")
for item in public_endpoints:
    method = item[0]
    ep = item[1]
    data = item[2] if len(item) > 2 else None
    json_data = item[3] if len(item) > 3 else None
    res = measure_request(method, ep, data=data, json_data=json_data)
    results.append(res)
    flag = " [!] > 1.0s CẦN TỐI ƯU" if res["over_1s"] else " [OK]"
    print(f"[{res['method']}] {res['endpoint']:<50} | HTTP {res['status_code']} | {res['latency_ms']:>6.1f} ms | {res['size_bytes']:>6} bytes{flag}")

# 2. Đăng nhập Admin để kiểm thử các Protected Admin APIs & SQL Queries
print("\n--- 2. Đăng nhập Quản trị viên để kiểm thử Admin APIs & SQL Queries ---")
login_res = measure_request("POST", "/api/v1/auth/login", json_data={
    "email": "admin@perfumeluxe.vn",
    "password": "AdminPassword123!"
})
results.append(login_res)
flag = " [!] > 1.0s CẦN TỐI ƯU" if login_res["over_1s"] else " [OK]"
print(f"[{login_res['method']}] {login_res['endpoint']:<50} | HTTP {login_res['status_code']} | {login_res['latency_ms']:>6.1f} ms{flag}")

admin_endpoints = [
    ("GET", "/admin/admindashboard"),
    ("GET", "/admin/admindashboard/products"),
    ("GET", "/admin/admindashboard/inventory"),
    ("GET", "/admin/admindashboard/financialreport"),
    ("GET", "/admin/admindashboard/financialreport?preset=today"),
    ("GET", "/admin/admindashboard/financialreport?preset=year"),
    ("GET", "/admin/admindashboard/settings"),
    ("GET", "/admin/admindashboard/blogs"),
    ("GET", "/admin/databaseconsole"),
    ("GET", "/admin/admindashboard/systemhealth"),
    ("GET", "/api/v1/store-manager/dashboard"),
    ("GET", "/api/v1/loyalty/status")
]

for method, ep in admin_endpoints:
    res = measure_request(method, ep)
    results.append(res)
    flag = " [!] > 1.0s CẦN TỐI ƯU" if res["over_1s"] else " [OK]"
    print(f"[{res['method']}] {res['endpoint']:<50} | HTTP {res['status_code']} | {res['latency_ms']:>6.1f} ms | {res['size_bytes']:>6} bytes{flag}")

# 3. Đo lường các câu lệnh truy vấn SQL trực tiếp qua API Database Console
print("\n--- 3. Đo lường tốc độ thực thi trực tiếp các câu lệnh SQL (Database Console API) ---")
sql_queries = [
    ("SELECT COUNT(*) FROM Products", "SELECT COUNT(*) AS TotalProducts FROM Products"),
    ("SELECT TOP 10 * FROM Products WITH (NOLOCK)", "SELECT TOP 10 Id, Name, Sku, BasePrice, BrandId FROM Products"),
    ("SELECT TOP 10 * FROM Orders JOIN OrderItems", "SELECT TOP 10 o.Id, o.OrderCode, o.TotalAmount, COUNT(i.Id) AS ItemCount FROM Orders o LEFT JOIN OrderItems i ON o.Id = i.OrderId GROUP BY o.Id, o.OrderCode, o.TotalAmount"),
    ("SELECT Inventory Valuation Aggregation", "SELECT SUM(v.StockQuantity * v.SalePrice) AS TotalValuation, SUM(v.StockQuantity) AS TotalStock FROM ProductVariants v"),
    ("Complex Multi-Join: Product + Brand + Category + Family", "SELECT TOP 10 p.Id, p.Name, b.Name AS Brand, c.Name AS Category, f.Name AS Family FROM Products p JOIN Brands b ON p.BrandId = b.Id JOIN Categories c ON p.CategoryId = c.Id LEFT JOIN FragranceFamilies f ON p.FragranceFamilyId = f.Id")
]

for label, sql in sql_queries:
    res = measure_request("POST", "/admin/databaseconsole/executequery", json_data={"query": sql})
    res["endpoint"] = f"SQL: {label}"
    results.append(res)
    flag = " [!] > 1.0s CẦN TỐI ƯU" if res["over_1s"] else " [OK]"
    print(f"[SQL] {res['endpoint']:<50} | HTTP {res['status_code']} | {res['latency_ms']:>6.1f} ms{flag}")

print("\n" + "=" * 80)
print("DANH SÁCH CÁC MỤC VƯỢT QUÁ 1.0 GIÂY (> 1000ms):")
print("=" * 80)
slow_items = [r for r in results if r["over_1s"]]
if not slow_items:
    print("TUYỆT VỜI! Không có endpoint hoặc query nào vượt quá 1.0s.")
else:
    for idx, item in enumerate(slow_items, 1):
        print(f"{idx}. [{item['method']}] {item['endpoint']} - Thời gian: {item['latency_ms']} ms (HTTP {item['status_code']})")

with open("benchmark_results.json", "w", encoding="utf-8") as f:
    json.dump(results, f, ensure_ascii=False, indent=2)
print("\nĐã lưu kết quả chi tiết vào benchmark_results.json")

