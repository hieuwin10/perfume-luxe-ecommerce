# SURVEY REPORT: API Architecture, Swagger Configuration, Pipeline, & Automated Testing

**Investigator**: explorer_survey_3  
**Date**: 2026-10-12  
**Target Solution**: `PerfumeEcommerce.sln`  
**Platform**: .NET 8.0 (SDK 9.0.318, Windows 10/11 x64)

---

## Executive Summary

This survey report provides a thorough, deep-dive technical investigation into the API architecture, routing and middleware pipeline, Swashbuckle OpenAPI configuration, database status, the five required R4 endpoints, automated test project structure, and live Kestrel execution testing feasibility for the **PERFUME LUXE** platform.

Key findings:
1. **API Pipeline & Swagger**: `Program.cs` currently registers MVC controllers via `builder.Services.AddControllersWithViews()`, but lacks `Swashbuckle.AspNetCore`, OpenAPI metadata, XML doc comment generation, `AddEndpointsApiExplorer()`, and `app.MapControllers()`. Swashbuckle 6.6.2 is readily available on nuget.org.
2. **Existing Web APIs**: `CustomerApiController` (`[Route("api/v1")]`) is currently the single API controller, co-located in `src/Perfume.Web/Controllers/` with dual-mode authentication (ASP.NET Identity cookie for web, and custom JWT Bearer tokens for external API clients).
3. **Database & Environment**: LocalDB instance `(localdb)\MSSQLLocalDB` is running and contains `PerfumeDb` with 32 tables. Tables `UserLoyalties` and `PrivateTastingBookings` already exist in SQL Server. `DatabaseSchemaUpgrader` requires `Database:BackupPath` to be configured when non-empty migrations run.
4. **Testing Status**: There is **no existing test project** in the solution (`tests/Perfume.Tests` is missing). A new xUnit test project referencing `xunit`, `Moq`, `Microsoft.AspNetCore.Mvc.Testing`, and `Microsoft.EntityFrameworkCore.InMemory` must be created.
5. **Live Kestrel Execution**: High feasibility. Both in-process execution via `WebApplicationFactory<Program>` (requiring `public partial class Program { }`) and external socket live testing (launching Kestrel on `http://localhost:5000` or a specified port and invoking endpoints via `HttpClient`) are fully supported.

---

## 1. Program.cs & Middleware Pipeline Investigation

### 1.1 Current Architecture in `src/Perfume.Web/Program.cs`
- **Service Registrations**:
  - `builder.Services.AddInfrastructureServices(builder.Configuration)`: Configures SQL Server via `options.UseSqlServer(connectionString)`, Identity (`AppUser`, `AppRole`), and scoped domain services (`IPerfumeCatalogService`, `IOrderService`, `ICustomerCartService`, etc.).
  - `ConfigureApplicationCookie`: Sets up login/logout paths, 30-day sliding cookie expiration, and intercepts `/api` calls in `OnRedirectToLogin` and `OnRedirectToAccessDenied` to emit HTTP 401 / 403 status codes instead of redirecting to HTML login pages.
  - `JwtTokenService`: Singleton JWT service for generating and validating API access tokens.
  - `builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, BearerTokenHandler>("Bearer", _ => { })`: Custom Bearer authentication scheme.
  - `Authorization Policy`: `"CustomerApi"` policy allowing either Identity Application Scheme or Bearer tokens.
  - `AddControllersWithViews()`: Discovers MVC and API controllers.
  - `AddDistributedMemoryCache()` and `AddSession()`: Configures 2-hour sliding session cookies.
- **Startup Lifecycle**:
  - Lines 60-91: Service scope applies `EnsureCreatedAsync()`, runs `DatabaseSchemaUpgrader.ApplyAsync(...)`, seeds default roles (`Admin`, `StoreManager`, `Customer`), and creates the initial admin user if configured.
- **Middleware Order**:
  1. `app.UseExceptionHandler("/Home/Error")` / `app.UseHsts()`
  2. `app.UseHttpsRedirection()`
  3. `app.UseStaticFiles()`
  4. `app.UseRouting()`
  5. `app.UseAuthentication()`
  6. `app.UseAuthorization()`
  7. `app.UseSession()`
  8. `app.MapControllerRoute(...)` for productDetail, catalogFilter, blogDetail, sitemap, robots, areas, and default routes.

### 1.2 Gaps & Deficiencies Identified
1. **Missing Swashbuckle / OpenAPI packages**:
   - `src/Perfume.Web/Perfume.Web.csproj` has only `Microsoft.EntityFrameworkCore.Design` and `System.IdentityModel.Tokens.Jwt`.
   - Need to add package `<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />`.
2. **Missing Swagger DI and Middleware**:
   - Needs `builder.Services.AddEndpointsApiExplorer();`
   - Needs `builder.Services.AddSwaggerGen(options => { ... });`
   - Needs `app.UseSwagger();`
   - Needs `app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "PERFUME LUXE API v1"); c.RoutePrefix = "swagger"; c.EnableTryItOutByDefault(); });`
3. **Missing `app.MapControllers()`**:
   - `Program.cs` only maps conventional routes via `app.MapControllerRoute(...)`. While attribute-routed controllers sometimes get picked up by endpoint routing, ASP.NET Core best practices mandate `app.MapControllers()` to guarantee attribute-routed controllers (`[Route("api/v1/...")]`) are mapped consistently.
4. **Missing XML Documentation**:
   - `<GenerateDocumentationFile>true</GenerateDocumentationFile>` must be added to `Perfume.Web.csproj` and `Perfume.Application.csproj`.
   - To prevent compiler warnings from breaking the 0-warning bar: add `<NoWarn>$(NoWarn);1591</NoWarn>` (CS1591: Missing XML comment for publicly visible type or member).
   - In `AddSwaggerGen`, load XML files from `AppContext.BaseDirectory`.
5. **Program Class Accessibility for WebApplicationFactory**:
   - In .NET 8 top-level statements, `Program` is internal by default.
   - For `WebApplicationFactory<Program>` in `Perfume.Tests`, `Program.cs` needs:
     ```csharp
     public partial class Program { }
     ```
     at the end of `Program.cs`.

---

## 2. Existing API Controller & Integration Pattern

### 2.1 Analysis of `CustomerApiController.cs`
- **Location**: `src/Perfume.Web/Controllers/CustomerApiController.cs`
- **Routing**: `[ApiController]`, `[Route("api/v1")]`
- **Controller Type**: Inherits from `ControllerBase`.
- **Existing Endpoints**:
  - `GET /api/v1/catalog/products`: Paginated catalog with `ProductFilterParams`.
  - `GET /api/v1/catalog/products/{slug}`: Product detail by slug.
  - `POST /api/v1/auth/register`, `login`, `logout`: Customer auth with Identity.
  - `POST /api/v1/auth/token`, `refresh`, `revoke`: JWT token lifecycle.
  - `POST /api/v1/auth/password/forgot`, `reset`: Password recovery.
  - `GET /api/v1/customer/profile`, `PUT /api/v1/customer/profile`: Customer profile (`[Authorize(Policy = "CustomerApi")]`).
  - `GET /api/v1/customer/orders`, `GET /api/v1/customer/orders/{id}`: Order history.
  - `GET /api/v1/cart`, `POST /api/v1/cart/items`, `PUT /api/v1/cart/items`, `DELETE /api/v1/cart/items/{productId}`, `POST /api/v1/cart/merge`: Cart operations.
  - `POST /api/v1/vouchers/validate`: Voucher validation.
  - `POST /api/v1/orders`: Checkout and place order.
  - `GET /api/v1/orders/guest`: Guest order lookup via encrypted token (`IDataProtector`).
  - `GET /api/v1/products/{productId}/reviews`, `POST /api/v1/products/{productId}/reviews`: Product reviews with image uploads.
- **Authentication & Security Pattern**:
  - Cookie validation uses Antiforgery: `ValidateCookieWriteAsync()`. If header starts with `Bearer `, Antiforgery is bypassed; otherwise, it validates the Antiforgery token from cookie/header.
  - Responses return standard JSON objects with HTTP status codes: `Ok(...)`, `NotFound(...)`, `BadRequest(...)`, `Unauthorized(...)`, `Forbid()`, `Conflict(...)`.

### 2.2 Integration Strategy for R4 Endpoints
Two architectural options exist:
1. **Extend `CustomerApiController`**: Add new methods directly into `CustomerApiController`.
   - *Pros*: Centralized, shares existing dependencies (`_db`, `_carts`, `_orders`).
   - *Cons*: `CustomerApiController` is already 554 lines long; mixing FlashSale, Loyalty, DiscoveryBox, OrderTracking, and TastingBooking makes it monolithic.
2. **Dedicated Modular API Controllers (Recommended)**:
   - Create separate controllers under `src/Perfume.Web/Controllers/Api/`:
     - `FlashSaleApiController.cs` (`[Route("api/v1/flashsales")]`)
     - `LoyaltyApiController.cs` (`[Route("api/v1/loyalty")]`)
     - `DiscoveryBoxApiController.cs` (`[Route("api/v1/discovery-box")]`)
     - `OrderTrackingApiController.cs` (`[Route("api/v1/orders")]`)
     - `ExperienceApiController.cs` (`[Route("api/v1/experience")]`)
   - *Pros*: Clean separation of concerns, clean Swagger grouping by controller tag, modular testing, matches clean architecture.

---

## 3. Endpoints Required for R4

### 3.1 `GET /api/v1/flashsales/active`
- **Objective**: Retrieve the currently active Flash Sale campaign, countdown end time, and participating products/variants.
- **Domain Modeling**:
  - Table / Entity: `FlashSaleCampaign` (`Id`, `Name`, `Description`, `BannerImageUrl`, `StartTimeUtc`, `EndTimeUtc`, `IsActive`, `CreatedAt`, `IsDeleted`).
  - Table / Entity: `FlashSaleItem` (`Id`, `CampaignId`, `ProductId`, `VariantId`, `OriginalPrice`, `FlashSalePrice`, `PromoStockLimit`, `SoldQuantity`, `IsDeleted`).
- **Response DTO**:
  ```csharp
  public class ActiveFlashSaleResponse
  {
      public int Id { get; set; }
      public string Name { get; set; } = string.Empty;
      public string? Description { get; set; }
      public string? BannerImageUrl { get; set; }
      public DateTime StartTime { get; set; }
      public DateTime EndTime { get; set; }
      public int RemainingSeconds { get; set; }
      public bool IsActive { get; set; }
      public List<FlashSaleItemDto> Items { get; set; } = new();
  }

  public class FlashSaleItemDto
  {
      public int ProductId { get; set; }
      public int? VariantId { get; set; }
      public string ProductName { get; set; } = string.Empty;
      public string ProductSlug { get; set; } = string.Empty;
      public string? ProductImageUrl { get; set; }
      public string VariantLabel { get; set; } = string.Empty;
      public decimal OriginalPrice { get; set; }
      public decimal FlashSalePrice { get; set; }
      public int DiscountPercent { get; set; }
      public int PromoStockLimit { get; set; }
      public int SoldQuantity { get; set; }
      public int AvailableStock => Math.Max(0, PromoStockLimit - SoldQuantity);
      public bool IsSoldOut => SoldQuantity >= PromoStockLimit;
  }
  ```
- **HTTP Status Codes**:
  - `200 OK`: Returns active campaign DTO. If none active, returns response with `IsActive = false` and empty items list.
  - `400 Bad Request`: If invalid query filter provided.
  - `404 Not Found`: If querying by campaign ID and not found.

### 3.2 `GET /api/v1/loyalty/status`
- **Objective**: Retrieve customer loyalty membership status, total accumulated spend, current tier, discount percentage, reward points, and progress to next tier.
- **Tiers & Thresholds (from R2)**:
  - **Bronze**: 0đ threshold, 0% discount.
  - **Silver**: >= 5.000.000đ threshold, 3% discount.
  - **Gold**: >= 15.000.000đ threshold, 7% discount.
  - **Diamond**: >= 30.000.000đ threshold, 12% discount + VIP exclusive gift.
- **Accumulated Spend Calculation**:
  - Total spending = sum of `TotalAmount` for all orders belonging to user where `(PaymentStatus == PaymentStatus.Completed || OrderStatus == OrderStatus.Delivered)`.
  - Automatic tier promotion updates `AppUser.Tier` and `UserLoyalty.Tier`.
- **Response DTO**:
  ```csharp
  public class LoyaltyStatusDto
  {
      public string UserId { get; set; } = string.Empty;
      public string FullName { get; set; } = string.Empty;
      public string Tier { get; set; } = "Bronze";
      public string TierName { get; set; } = "Thành viên Đồng (Bronze)";
      public int DiscountPercent { get; set; } = 0;
      public decimal TotalSpent { get; set; }
      public int LoyaltyPoints { get; set; }
      public string? NextTier { get; set; }
      public decimal NextTierMinSpent { get; set; }
      public decimal AmountNeededForNextTier { get; set; }
      public double ProgressPercent { get; set; }
      public List<string> Privileges { get; set; } = new();
  }
  ```
- **Authentication**:
  - Authenticated via `User` (`[Authorize(Policy = "CustomerApi")]`), OR allows optional `[FromQuery] string? emailOrPhone` for guest lookup.
- **HTTP Status Codes**:
  - `200 OK`: Returns loyalty status details.
  - `401 Unauthorized`: When user is not authenticated and no identifier provided.
  - `404 Not Found`: When specified user account does not exist.

### 3.3 `POST /api/v1/discovery-box/validate-and-add`
- **Objective**: Validate custom discovery gift box configuration (box type, 3 to 5 vials, gift card message and typography font) and add as a package item into the shopping cart.
- **Configuration Spec**:
  - Box models:
    - `lacquer-wood`: "Hộp Gỗ Sơn Mài Cổ Điển" (250.000đ)
    - `royal-velvet`: "Hộp Nhung Đen Hoàng Gia" (180.000đ)
    - `leather-pouch`: "Bao Da Thủ Công" (200.000đ)
  - Vials: List of 3 to 5 perfume samples (2ml - 5ml) with ProductId, VariantId.
  - Greeting card: Message text (up to 250 characters) + Calligraphy font style (`ParisianScript`, `RoyalCalligraphy`, `ModernElegance`).
- **Validation Rules**:
  - Vial count must be in range `[3, 5]`. If `< 3` or `> 5`, return `400 Bad Request`.
  - Box style must match one of the predefined styles.
  - All selected vials must exist and be in stock.
- **Calculation**:
  - Total package price = `BoxPrice + Sum(VialPrice * Quantity)`.
- **Cart & Order Synchronization**:
  - Inserts into `ShoppingCart` via `ICustomerCartService`, passing custom package metadata.
- **Request & Response DTOs**:
  ```csharp
  public class DiscoveryBoxRequest
  {
      [Required]
      public string BoxStyle { get; set; } = string.Empty;
      [Required, MinLength(3), MaxLength(5)]
      public List<DiscoveryBoxVialItem> Vials { get; set; } = new();
      [StringLength(250)]
      public string? GreetingMessage { get; set; }
      public string? CalligraphyFont { get; set; }
  }

  public class DiscoveryBoxVialItem
  {
      [Required]
      public int ProductId { get; set; }
      public int? VariantId { get; set; }
  }

  public class DiscoveryBoxValidationResponse
  {
      public bool IsValid { get; set; }
      public string BoxStyleName { get; set; } = string.Empty;
      public decimal BoxPrice { get; set; }
      public decimal VialsTotal { get; set; }
      public decimal TotalPackagePrice { get; set; }
      public List<DiscoveryBoxVialSummary> VialDetails { get; set; } = new();
      public CartStateDto Cart { get; set; } = new();
      public string Message { get; set; } = string.Empty;
  }
  ```
- **HTTP Status Codes**:
  - `200 OK` / `201 Created`: Configuration valid, added to cart successfully.
  - `400 Bad Request`: Validation failure (e.g., fewer than 3 or more than 5 vials, invalid box type).
  - `404 Not Found`: One or more selected vial perfume products do not exist.

### 3.4 `GET /api/v1/orders/track`
- **Objective**: Track order journey and live logistics status.
- **Parameters**: `[FromQuery] string? orderCode`, `[FromQuery] string? trackingNumber`, `[FromQuery] string? phone`.
- **Response DTO**:
  ```csharp
  public class OrderTrackingResponse
  {
      public string OrderCode { get; set; } = string.Empty;
      public string TrackingNumber { get; set; } = string.Empty;
      public string CarrierName { get; set; } = "PERFUME LUXE Express";
      public string OrderStatus { get; set; } = string.Empty;
      public string PaymentStatus { get; set; } = string.Empty;
      public DateTime CreatedAt { get; set; }
      public DateTime? EstimatedDelivery { get; set; }
      public string CustomerName { get; set; } = string.Empty;
      public string ShippingAddress { get; set; } = string.Empty;
      public string? City { get; set; }
      public decimal TotalAmount { get; set; }
      public List<OrderTrackingStepDto> Timeline { get; set; } = new();
      public List<CustomerOrderLineDto> Items { get; set; } = new();
  }

  public class OrderTrackingStepDto
  {
      public string Title { get; set; } = string.Empty;
      public string Description { get; set; } = string.Empty;
      public DateTime? Timestamp { get; set; }
      public bool IsCompleted { get; set; }
      public bool IsCurrent { get; set; }
  }
  ```
- **HTTP Status Codes**:
  - `200 OK`: Found and returned tracking details.
  - `400 Bad Request`: If neither `orderCode`, `trackingNumber`, nor `phone` was provided.
  - `404 Not Found`: No order matched the specified lookup criteria.

### 3.5 `POST /api/v1/experience/tasting-booking`
- **Objective**: Book a VIP private perfume tasting experience at the boutique.
- **Persistence**: Table `PrivateTastingBookings` already exists in `PerfumeDb` with columns: `Id`, `CustomerName`, `PhoneNumber`, `Email`, `BoutiqueLocation`, `BookingDate`, `TimeSlot`, `GuestCount`, `PreferredScentFamilies`, `Notes`, `Status`, `CreatedAt`, `IsDeleted`.
- **Request & Response DTOs**:
  ```csharp
  public class TastingBookingRequest
  {
      [Required, StringLength(100, MinimumLength = 2)]
      public string CustomerName { get; set; } = string.Empty;
      [Required, Phone, StringLength(30)]
      public string PhoneNumber { get; set; } = string.Empty;
      [Required, EmailAddress]
      public string Email { get; set; } = string.Empty;
      public string? BoutiqueLocation { get; set; }
      [Required]
      public DateTime BookingDate { get; set; }
      [Required]
      public string TimeSlot { get; set; } = string.Empty;
      [Range(1, 6)]
      public int GuestCount { get; set; } = 1;
      public string? PreferredScentFamilies { get; set; }
      [StringLength(500)]
      public string? Notes { get; set; }
  }

  public class TastingBookingResponse
  {
      public int BookingId { get; set; }
      public string BookingCode { get; set; } = string.Empty;
      public string CustomerName { get; set; } = string.Empty;
      public string BoutiqueLocation { get; set; } = string.Empty;
      public DateTime BookingDate { get; set; }
      public string TimeSlot { get; set; } = string.Empty;
      public int GuestCount { get; set; }
      public string Status { get; set; } = "Confirmed";
      public string Message { get; set; } = string.Empty;
  }
  ```
- **Validation**:
  - `BookingDate` must be in the future (`> DateTime.UtcNow.Date`).
  - `GuestCount` between 1 and 6.
  - Time slots matching boutique sessions (e.g. `10:00 - 11:30`, `14:00 - 15:30`, `16:00 - 17:30`, `19:00 - 20:30`).
- **HTTP Status Codes**:
  - `201 Created`: Booking created, returns `TastingBookingResponse` with reference code.
  - `400 Bad Request`: Validation failure (past date, guest count out of range, missing required fields).

---

## 4. Swagger UI & Swashbuckle OpenAPI Setup

### 4.1 Dependency & Project Configuration
In `src/Perfume.Web/Perfume.Web.csproj`:
```xml
<ItemGroup>
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
</ItemGroup>

<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```
In `src/Perfume.Application/Perfume.Application.csproj`:
```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

### 4.2 Configuration in `Program.cs`
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PERFUME LUXE RESTful API",
        Version = "v1",
        Description = "Nền tảng thương mại điện tử nước hoa cao cấp PERFUME LUXE - Cổng thử nghiệm trực tiếp OpenAPI & Swagger UI",
        Contact = new OpenApiContact
        {
            Name = "PERFUME LUXE Architecture Team",
            Email = "contact@perfumeluxe.local"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập token JWT theo định dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments
    var webXml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(webXml)) c.IncludeXmlComments(webXml);

    var appXml = Path.Combine(AppContext.BaseDirectory, "Perfume.Application.xml");
    if (File.Exists(appXml)) c.IncludeXmlComments(appXml);
});
```

And in the HTTP request pipeline:
```csharp
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PERFUME LUXE API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "PERFUME LUXE - Swagger UI Live Test";
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    c.EnableTryItOutByDefault();
});
app.MapControllers();
```

---

## 5. Automated Test Setup (`tests/Perfume.Tests`)

### 5.1 Project Layout
A new test project needs to be created at `tests/Perfume.Tests/Perfume.Tests.csproj` and added to `PerfumeEcommerce.sln`.

Structure:
```
tests/Perfume.Tests/
├── Perfume.Tests.csproj
├── UnitTests/
│   ├── FlashSaleLogicTests.cs       # Flash sale discount calculation, expiration, stock limit
│   ├── LoyaltyTierLogicTests.cs     # Loyalty spending thresholds, tier auto-promotion, progress
│   ├── DiscoveryBoxLogicTests.cs    # Custom discovery box pricing, 3-5 vial constraints
│   ├── OrderTrackingTests.cs        # Order journey milestones, carrier details
│   └── TastingBookingTests.cs       # VIP tasting booking validations
├── ApiTests/
│   ├── FlashSaleApiTests.cs         # GET /api/v1/flashsales/active
│   ├── LoyaltyApiTests.cs           # GET /api/v1/loyalty/status
│   ├── DiscoveryBoxApiTests.cs      # POST /api/v1/discovery-box/validate-and-add
│   ├── OrderTrackingApiTests.cs     # GET /api/v1/orders/track
│   └── TastingBookingApiTests.cs    # POST /api/v1/experience/tasting-booking
└── IntegrationTests/
    ├── CustomWebApplicationFactory.cs # WebApplicationFactory with InMemory DbContext
    └── SwaggerAndApiLiveTests.cs      # Swagger UI and OpenAPI JSON live verification
```

### 5.2 Package Dependencies in `Perfume.Tests.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.11" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Perfume.Domain\Perfume.Domain.csproj" />
    <ProjectReference Include="..\..\src\Perfume.Application\Perfume.Application.csproj" />
    <ProjectReference Include="..\..\src\Perfume.Infrastructure\Perfume.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Perfume.Web\Perfume.Web.csproj" />
  </ItemGroup>
</Project>
```

---

## 6. Live Kestrel Execution Testing Feasibility

### 6.1 Two-Tier Live Verification Architecture
1. **Tier 1: In-Process Kestrel Test Host (`WebApplicationFactory<Program>`)**:
   - Executes tests through the real ASP.NET Core pipeline without binding external TCP sockets.
   - Replaces SQL Server with an in-memory database to isolate integration tests from external dependencies.
   - Tests:
     - `GET /swagger/v1/swagger.json` -> HTTP 200 OK, valid JSON containing `openapi: 3.0.1` and all endpoints.
     - `GET /swagger/index.html` -> HTTP 200 OK, HTML containing Swagger UI bundle.
     - `GET /api/v1/flashsales/active` -> HTTP 200 OK.
     - `GET /api/v1/loyalty/status` -> HTTP 200 OK or 401 Unauthorized.
     - `POST /api/v1/discovery-box/validate-and-add` -> HTTP 200 OK or 400 Bad Request.
     - `GET /api/v1/orders/track` -> HTTP 200 OK or 404 Not Found.
     - `POST /api/v1/experience/tasting-booking` -> HTTP 201 Created.
2. **Tier 2: Real Local Kestrel Socket Execution (`dotnet run`)**:
   - Launches `src/Perfume.Web` using `dotnet run --urls "http://127.0.0.1:5123"`.
   - LocalDB instance `(localdb)\MSSQLLocalDB` is running and accessible.
   - Invokes endpoints over live TCP HTTP requests via PowerShell script / `HttpClient`.
   - Records live execution response logs into a dedicated test artifact file (e.g., `tests/live-kestrel-results.log`).

### 6.2 Key Operational Safeguard: `DatabaseSchemaUpgrader` & `Database:BackupPath`
- When starting `Perfume.Web` against existing SQL Server databases, `DatabaseSchemaUpgrader` checks:
  ```csharp
  var backupDirectory = configuration["Database:BackupPath"];
  if (string.IsNullOrWhiteSpace(backupDirectory))
      throw new InvalidOperationException("A schema update is pending, but Database:BackupPath is not configured...");
  ```
- **Actionable Safeguard**:
  - In `appsettings.Development.json` or `appsettings.json`, set:
    `"Database:BackupPath": "C:\\Users\\Sieu Chu nhiem\\AppData\\Local\\Temp"`
    or configure fallback in `DatabaseSchemaUpgrader.cs` so local developer runs do not fail if backup path is missing.
  - Furthermore, ensure additive migrations for FlashSale and Tasting check `IF OBJECT_ID(...) IS NULL` so they execute safely.

---

## 7. Synthesis & Architectural Recommendations

| Area | Current State | Required Target State | Key Tasks |
|---|---|---|---|
| **Swashbuckle Package** | Not referenced in any csproj | Referenced in `Perfume.Web.csproj` v6.6.2 | Add PackageReference |
| **XML Comments** | Not generated | Enabled in Web & Application csproj | `<GenerateDocumentationFile>true</GenerateDocumentationFile>` with `<NoWarn>$(NoWarn);1591</NoWarn>` |
| **Swagger Pipeline** | None | `/swagger` UI with Try-It-Out & Bearer Auth | `AddEndpointsApiExplorer`, `AddSwaggerGen`, `UseSwagger`, `UseSwaggerUI` |
| **Controller Routing** | Conventional routes only | Conventional + Attribute (`MapControllers`) | Add `app.MapControllers()` |
| **Program Visibility** | Internal top-level | Public partial class for test factory | Add `public partial class Program { }` |
| **R4 Endpoints** | Not implemented | 5 endpoints under `api/v1/...` | Create modular API controllers with full DTOs & status codes |
| **Test Project** | No test project in repo | `tests/Perfume.Tests` in sln | Create xUnit test project with unit & integration tests |
| **Live Kestrel Testing**| None | Live HTTP testing with logged output | Script/test runner verifying live Kestrel requests |

---
