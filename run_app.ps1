$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__DefaultConnection = 'Server=localhost,1433;Database=PerfumeDb;User Id=sa;Password=PerfumePass@2026!;TrustServerCertificate=True;'
$env:Database__BackupPath = '/var/opt/mssql/backup'
$env:Storage__RootPath = 'C:\Users\Sieu Chu nhiem\Desktop\dotnet\src\Perfume.Web\App_Data'
$env:InitialAdmin__Email = 'admin@perfumeluxe.com'
$env:InitialAdmin__Password = 'Admin@Perfume2026!'
$env:JWT_SIGNING_KEY = 'SuperSecretPerfumeLuxeKeyWithAtLeast32BytesLength!'
$env:Jwt__SigningKey = 'SuperSecretPerfumeLuxeKeyWithAtLeast32BytesLength!'
$env:JWT_ISSUER = 'PerfumeLuxe'
$env:JWT_AUDIENCE = 'PerfumeLuxe.Clients'

dotnet run --project src/Perfume.Web --no-launch-profile --urls "http://localhost:5000"
