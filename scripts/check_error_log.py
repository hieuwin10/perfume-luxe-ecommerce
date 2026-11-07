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

# 2. Trigger error
r_edit = s.get("https://perfumeluxe.somee.com/Admin/AdminDashboard/EditProduct/1")

# 3. Query SystemErrorLogs
r_db = s.get("https://perfumeluxe.somee.com/Admin/DatabaseConsole/Index")
match_db = re.search(r'name=["\']__RequestVerificationToken["\'].*?value=["\']([^"\']+)["\']', r_db.text)
token_db = match_db.group(1) if match_db else token

r_q = s.post("https://perfumeluxe.somee.com/Admin/DatabaseConsole/ExecuteQuery", 
    json={"sql": "SELECT TOP 3 ErrorCode, RequestPath, Message, SUBSTRING(StackTrace, 1, 300) as StackSnippet FROM SystemErrorLogs ORDER BY CreatedAt DESC"},
    headers={
        "RequestVerificationToken": token_db,
        "__RequestVerificationToken": token_db,
        "X-Requested-With": "XMLHttpRequest",
        "Referer": "https://perfumeluxe.somee.com/Admin/DatabaseConsole/Index"
    }
)
print("SystemErrorLogs query result:", r_q.json())

