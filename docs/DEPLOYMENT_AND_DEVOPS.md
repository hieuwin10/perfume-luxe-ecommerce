# 🚀 HƯỚNG DẪN TRIỂN KHAI & DEVOPS (DEPLOYMENT & OPERATIONS GUIDE)

> **Môi Trường**: Hỗ trợ 2 mô hình chạy song song: **Localhost Hybrid** (Dịch vụ nền trong Docker, Code chạy qua dotnet CLI) và **Full Containerized** (Toàn bộ trong Docker).

---

## 1. Cấu Hình Biến Môi Trường (`.env`)

Tạo file `.env` tại thư mục gốc từ file mẫu [.env.example](file:///c:/Users/Sieu%20Chu%20nhiem/Desktop/dotnet/.env.example):

```ini
# --- Microsoft SQL Server 2022 ---
MSSQL_SA_PASSWORD=YourStrong@Password123
MSSQL_PID=Developer
MSSQL_TCP_PORT=1433

# --- Chuỗi Kết Nối Ứng Dụng ---
ConnectionStrings__DefaultConnection=Server=localhost,1433;Database=PerfumeDb;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True;MultipleActiveResultSets=True;

# --- Redis 7 Distributed Cache ---
Redis__Configuration=localhost:6379,abortConnect=false

# --- Tài Khoản Admin Khởi Tạo Ban Đầu ---
InitialAdmin__Email=admin@perfumeluxe.com
InitialAdmin__Password=Admin@Perfume2026!
InitialAdmin__FullName=Ban Quản Trị Perfume Luxe

# --- Mailpit SMTP Test Server ---
Smtp__Host=localhost
Smtp__Port=1025
Smtp__Enabled=true

# --- JWT Bearer Authentication ---
Jwt__SecretKey=SuperSecretLuxuryPerfumeKey2026With256BitsLengthMinimum!
Jwt__Issuer=PerfumeLuxeAuth
Jwt__Audience=PerfumeLuxeClient
Jwt__ExpirationMinutes=60
```

---

## 2. Các Mô Hình Triển Khai (Deployment Modes)

### Mô hình 1: Localhost Hybrid (Phát triển & Debug nhanh nhất)
Trong mô hình này, SQL Server 2022, Redis và Mailpit chạy trong Docker; ứng dụng .NET chạy trực tiếp trên máy host qua `dotnet watch`:

```powershell
# 1. Khởi động các dịch vụ nền
docker compose -f docker-compose.infra.yml up -d

# 2. Kiểm tra container đang chạy
docker ps

# 3. Chạy ứng dụng .NET kèm tính năng Hot Reload
dotnet watch --project src/Perfume.Web run
```

### Mô hình 2: Full Docker Containerization (Đóng gói Production)
Toàn bộ hệ thống từ Web App đến Database được đóng gói khép kín trong cùng một mạng Docker Network ảo:

```powershell
# Đóng gói và chạy toàn bộ cụm dịch vụ
docker compose up --build -d

# Xem log thời gian thực của container web
docker compose logs -f web
```

---

## 3. Sao Lưu & Phục Hồi CSDL (Backup & Disaster Recovery)

### 3.1. Sao Lưu Database (Full Backup)
Thực thi lệnh T-SQL tạo file backup `.bak`:
```powershell
docker exec -it perfume_sqlserver_dev /opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P "YourStrong@Password123" -C `
  -Q "BACKUP DATABASE [PerfumeDb] TO DISK = N'/var/opt/mssql/backup/PerfumeDb_Backup.bak' WITH NOFORMAT, NOINIT, SKIP, NOREWIND, NOUNLOAD, STATS = 10"
```

### 3.2. Phục Hồi Database (Restore)
```powershell
docker exec -it perfume_sqlserver_dev /opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P "YourStrong@Password123" -C `
  -Q "ALTER DATABASE [PerfumeDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [PerfumeDb] FROM DISK = N'/var/opt/mssql/backup/PerfumeDb_Backup.bak' WITH REPLACE; ALTER DATABASE [PerfumeDb] SET MULTI_USER;"
```

---

## 4. Kiểm Tra Sức Khỏe Hệ Thống (Health Checks & Monitoring)

- **SQL Server**: `SELECT @@VERSION` (Cổng 1433)
- **Adminer DB UI**: `http://localhost:8088` (Hệ quản trị CSDL nền web nhẹ nhàng)
- **Mailpit Web UI**: `http://localhost:8025` (Hộp thư kiểm thử kích hoạt tài khoản & email hóa đơn)
- **Storefront**: `http://localhost:5000`
- **Admin Dashboard**: `http://localhost:5000/admin/admindashboard`
- **Store Manager Analytics**: `http://localhost:5000/admin/storemanager`
