# Handoff Report: API Architecture, Swagger Configuration, Pipeline & Automated Test Setup

**Agent**: explorer_survey_3  
**Date**: 2026-10-12  
**Type**: Hard Handoff (Investigation Complete)  
**Target Solution**: `PerfumeEcommerce.sln` (.NET 8.0)

---

## 1. Observation

1. **`Program.cs` Pipeline Configuration**:
   - `src/Perfume.Web/Program.cs` lines 10-58 register services: `AddInfrastructureServices`, `ConfigureApplicationCookie`, `AddSingleton<JwtTokenService>`, custom Bearer authentication scheme, `CustomerApi` authorization policy, `AddControllersWithViews()`, memory cache, and session.
   - Lines 93-111 configure middleware: `UseExceptionHandler`, `UseHsts`, `UseHttpsRedirection`, `UseStaticFiles`, `UseRouting`, `UseAuthentication`, `UseAuthorization`, `UseSession`, and 7 conventional routes via `app.MapControllerRoute(...)`.
   - `app.MapControllers()` is **not** called.
   - Swashbuckle and OpenAPI services (`AddEndpointsApiExplorer`, `AddSwaggerGen`, `UseSwagger`, `UseSwaggerUI`) are completely **absent**.
   - `Program.cs` does not expose `public partial class Program { }` needed for `WebApplicationFactory<Program>`.

2. **Package References**:
   - `src/Perfume.Web/Perfume.Web.csproj` lines 9-15:
     ```xml
     <ItemGroup>
       <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.11" />
       <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="6.35.0" />
     </ItemGroup>
     ```
     `Swashbuckle.AspNetCore` is not present.
   - Searching nuget.org confirmed `Swashbuckle.AspNetCore` (version 6.6.2 and newer) is available.
   - `<GenerateDocumentationFile>` is not enabled in any project file.

3. **Existing API Controller**:
   - `src/Perfume.Web/Controllers/CustomerApiController.cs` (lines 23-25):
     ```csharp
     [ApiController]
     [Route("api/v1")]
     public sealed class CustomerApiController : ControllerBase
     ```
   - Implements catalog, authentication, token refresh, customer profile, shopping cart, voucher check, order submission, guest order tracking, and product reviews.
   - Uses dual auth: Identity application cookies with Antiforgery validation (`ValidateCookieWriteAsync()`) or `Bearer` JWT tokens.

4. **Target Endpoints Status (R4)**:
   - `GET /api/v1/flashsales/active`: Not implemented. No FlashSale entity in Domain or DbContext.
   - `GET /api/v1/loyalty/status`: Not implemented. `UserTier` enum exists in `Perfume.Domain/Enums/PerfumeEnums.cs` (Bronze = 1, Silver = 2, Gold = 3, Diamond = 4). `UserLoyalty` entity exists in `Perfume.Domain/Entities/EcommerceEntities.cs` and `PerfumeDbContext`.
   - `POST /api/v1/discovery-box/validate-and-add`: Not implemented. No discovery box DTOs or cart synchronization logic.
   - `GET /api/v1/orders/track`: Not implemented as a standalone tracking journey API (only `GET /api/v1/orders/guest` with encrypted token exists).
   - `POST /api/v1/experience/tasting-booking`: Not implemented in API. Table `PrivateTastingBookings` exists in SQL Server `PerfumeDb`, but entity is not mapped in `PerfumeDbContext`.

5. **Database & Upgrader**:
   - LocalDB instance `(localdb)\MSSQLLocalDB` is running and contains database `PerfumeDb` with 32 tables.
   - Table `__PerfumeSchemaVersions` contains version `20261012_02_tastings_and_alerts`.
   - `src/Perfume.Infrastructure/Data/DatabaseSchemaUpgrader.cs` requires `configuration["Database:BackupPath"]` when pending schema updates execute.

6. **Test Project Structure**:
   - No `tests/` directory or `.csproj` exists in the repository. `PerfumeEcommerce.sln` has only 4 projects under `src/`.
   - Command `dotnet build` executes cleanly with `0 Warning(s)`, `0 Error(s)`.

---

## 2. Logic Chain

1. **Pipeline & Swagger Setup**:
   - From Observation 1 & 2: Swashbuckle packages and middleware are absent.
   - Therefore, `Swashbuckle.AspNetCore` (6.6.2) must be referenced in `Perfume.Web.csproj`.
   - Enabling `<GenerateDocumentationFile>true</GenerateDocumentationFile>` with `<NoWarn>$(NoWarn);1591</NoWarn>` will produce the XML documentation file without failing compiler warnings.
   - Adding `builder.Services.AddEndpointsApiExplorer();`, `builder.Services.AddSwaggerGen(...)`, `app.UseSwagger()`, `app.UseSwaggerUI(...)`, and `app.MapControllers()` will expose Swagger UI at `/swagger` with interactive Try-It-Out and JWT Bearer authorization.
   - Adding `public partial class Program { }` enables `WebApplicationFactory<Program>` testing.

2. **R4 Endpoints Implementation**:
   - From Observation 3 & 4: Rather than bloating `CustomerApiController.cs` (already 554 lines), the 5 required endpoints should be placed in dedicated API controllers (`Controllers/Api/`):
     - `FlashSaleApiController`: handles `GET /api/v1/flashsales/active`.
     - `LoyaltyApiController`: handles `GET /api/v1/loyalty/status`.
     - `DiscoveryBoxApiController`: handles `POST /api/v1/discovery-box/validate-and-add`.
     - `OrderTrackingApiController`: handles `GET /api/v1/orders/track`.
     - `ExperienceApiController`: handles `POST /api/v1/experience/tasting-booking`.
   - Each action should be decorated with `[ProducesResponseType]` for status codes 200, 201, 400, 404, and XML doc comments.

3. **Automated Test Project Setup**:
   - From Observation 6: A new project `tests/Perfume.Tests/Perfume.Tests.csproj` must be created.
   - Packages required: `xunit` (2.9.2), `xunit.runner.visualstudio` (2.8.2), `Microsoft.NET.Test.Sdk` (17.11.1), `Moq` (4.20.72), `Microsoft.AspNetCore.Mvc.Testing` (8.0.11), and `Microsoft.EntityFrameworkCore.InMemory` (8.0.11).
   - This test project directly supports R5 requirements: testing Flash Sale discounts/expiration, Loyalty Tier auto-upgrades/progress calculation, Custom Discovery Box packaging/pricing, unit tests for all 5 new endpoints, and integration tests for Swagger UI.

4. **Live Kestrel Execution Testing**:
   - From Observation 5 & 6: Live Kestrel testing can be performed two ways:
     - Automated in-process Kestrel via `WebApplicationFactory<Program>` in `dotnet test`.
     - Live socket execution via `dotnet run` on a local port (e.g. `http://127.0.0.1:5123`), making HTTP calls with `HttpClient` and logging results to file.

---

## 3. Caveats

1. **SQL Server vs In-Memory in Tests**:
   - For integration tests, `WebApplicationFactory` should substitute SQL Server with `UseInMemoryDatabase` or SQLite in-memory to ensure tests run deterministically in CI without requiring an active external SQL Server.
2. **`DatabaseSchemaUpgrader` Backup Path**:
   - When running against real SQL Server LocalDB, if new schema versions are added to `DatabaseSchemaUpgrader`, `Database:BackupPath` must be set in configuration (e.g. `%TEMP%`), or the backup step must only run if a valid path is configured.
3. **Cart Integration for Discovery Box**:
   - Discovery Box items need to be represented as cart items. The current cart schema stores `ProductId` and optional `VariantId`. Storing a custom discovery box can either be done via a dedicated "Custom Discovery Box" product template or a custom cart line extension.

---

## 4. Conclusion

The platform architecture is clean and ready for R4 and R5 implementation.
- `Program.cs` needs Swashbuckle configuration, XML docs, `MapControllers()`, and `public partial class Program { }`.
- The 5 required API endpoints should be implemented in dedicated controllers with strict DTO validation, comprehensive HTTP response status codes (200, 201, 400, 404), and OpenAPI metadata.
- `tests/Perfume.Tests` should be created with xUnit, Moq, and `WebApplicationFactory`.
- Live testing on Kestrel is fully feasible and should be executed and logged as part of the quality gate.

---

## 5. Verification Method

1. **Inspect Survey Report**:
   ```powershell
   Get-Content ".agents/teamwork/explorer_survey_3/survey_report.md"
   ```

2. **Verify Project Compilation**:
   ```powershell
   dotnet build
   ```
   (Must output `Build succeeded. 0 Warning(s) 0 Error(s)`).

3. **Verify NuGet Availability for Required Packages**:
   ```powershell
   dotnet package search Swashbuckle.AspNetCore --exact-match
   dotnet package search Microsoft.AspNetCore.Mvc.Testing --exact-match
   ```

4. **Verify LocalDB Status**:
   ```powershell
   sqllocaldb info MSSQLLocalDB
   ```
