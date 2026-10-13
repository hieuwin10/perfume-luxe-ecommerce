# Project: PERFUME LUXE Commercial & API System

## Architecture
Clean Architecture with .NET 8.0:
- **Perfume.Domain**: Pure domain entities, value objects, enums (`UserTier`, `OrderStatus`, `PaymentStatus`, `FlashSale`, `FlashSaleItem`, `BaseEntity`).
- **Perfume.Application**: Application DTOs, service interfaces (`IFlashSaleService`, `ILoyaltyTierService`, `IDiscoveryBoxService`, `ICustomerCartService`, `IOrderService`).
- **Perfume.Infrastructure**: EF Core `PerfumeDbContext`, Entity Configurations, Identity, Service implementations, and `DatabaseSchemaUpgrader` for zero-data-loss additive migrations.
- **Perfume.Web**: ASP.NET Core MVC & RESTful API:
  - Controllers for Storefront and Admin areas (`/Admin/AdminDashboard/FlashSales`, etc.).
  - Modular API controllers under `Controllers/` and `Controllers/Api/` (`api/v1/...`).
  - Swashbuckle OpenAPI & Swagger UI at `/swagger` with "Try it out" live testing.
  - Razor Views, Luxury Design Tokens in `luxury-perfume.css`, client script helpers in `customer-api.js`.
- **Perfume.Tests**: xUnit test project in `tests/Perfume.Tests` verifying business logic, API contracts, and integration via WebApplicationFactory / live Kestrel.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Flash Sale Domain & Additive Schema | Entity model (`FlashSale`, `FlashSaleItem`), DbContext mapping, and Upgrader Version 2 DDL | M1 | Survey 1, R1, R5 |
| 2 | Flash Sale Admin Management | Admin UI at `/Admin/AdminDashboard/FlashSales` to create/toggle/manage campaigns with stock limits and discount prices | M1 | Survey 2, R1 |
| 3 | Flash Sale Homepage Banner & Real-time Countdown | Homepage banner with second-accurate countdown timer, stock progress bar, and automated price reversion | M1 | Survey 2, R1 |
| 4 | Flash Sale Product Badges & Detail Page Sync | Flash sale badge on product cards and product details, real-time discount pricing and expiry reversion | M1 | Survey 2, R1 |
| 5 | Loyalty Tier Engine & Auto Promotion | Calculate total completed spend (`Completed` / `Delivered`), auto-promote Bronze (0đ), Silver (5M, 3%), Gold (15M, 7%), Diamond (30M, 12%) | M2 | Survey 1, R2 |
| 6 | Digital VIP Membership Card & Progress Bar | Luxury metallic digital membership card on `/Account/Profile` with spending progress bar and VIP benefits matrix | M2 | Survey 2, R2 |
| 7 | VIP Tier Discount in Checkout & Orders | Automatic tier identification, discount deduction combined with Vouchers at `/Checkout` and persisted in `Order` | M2 | Survey 1, Survey 2, R2 |
| 8 | Discovery Box 3-Step Interactive Builder | 3 box styles (Lacquer Wood, Black Velvet, Leather Pouch), 3-5 vial samples (2ml-5ml), gold-foil card with handwriting fonts | M3 | Survey 2, R3 |
| 9 | Discovery Box Cart & Order Persistence | Additive columns `IsCustomBox`, `CustomBoxConfigJson`, `CustomUnitPrice` in `ShoppingCartItem` and `OrderItem` + System Anchor Product | M3 | Survey 1, R3 |
| 10 | Discovery Box Presentation in Cart & Admin | Itemized vial breakdown and greeting message preview in Cart Drawer, `/Cart`, `/Checkout`, and Admin OrderDetails | M3 | Survey 2, R3 |
| 11 | Swashbuckle OpenAPI & Swagger UI Portal | Configure `Swashbuckle.AspNetCore`, Swagger UI at `/swagger` with Try-It-Out, Bearer token auth, and XML docs | M4 | Survey 3, R4 |
| 12 | Active Flash Sale API Endpoint | `GET /api/v1/flashsales/active` returning campaign, remaining seconds, items with status codes (200, 404) | M4 | Survey 3, R4 |
| 13 | Loyalty Status API Endpoint | `GET /api/v1/loyalty/status` returning tier, total spent, discount percent, points, and next tier gap (200, 401, 404) | M4 | Survey 3, R4 |
| 14 | Discovery Box Validation & Add API | `POST /api/v1/discovery-box/validate-and-add` validating 3-5 vials, box type, calculating price, adding to cart (200, 201, 400, 404) | M4 | Survey 3, R4 |
| 15 | Order Tracking API Endpoint | `GET /api/v1/orders/track` looking up order status, tracking number, carrier, and milestone timeline (200, 400, 404) | M4 | Survey 3, R4 |
| 16 | VIP Tasting Booking API Endpoint | `POST /api/v1/experience/tasting-booking` booking boutique tasting slot with validation into database (201, 400) | M4 | Survey 3, R4 |
| 17 | Additive Schema Upgrader Protection | Idempotent DDL upgrade with version tracking in `__PerfumeSchemaVersions`, safe rollback, and non-blocking backup fallback | M1, M5 | Survey 1, R5 |
| 18 | Automated xUnit Test Suite | xUnit test project `tests/Perfume.Tests` verifying Flash Sale logic, Loyalty calculation, Discovery Box pricing, and API endpoints | M5 | Survey 3, R5 |
| 19 | Live Kestrel Execution & Swagger Test | Live test execution against running server validating `/swagger` UI and all API HTTP response status codes | M5 | Survey 3, R5 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Flash Sale Engine & Additive Schema | Feature 1, 2, 3, 4, 17: Entities, Upgrader v2, Admin UI, Storefront countdown banner & badges | none | IN_PROGRESS (ce82102d) |
| M2 | Loyalty Tier Engine & VIP Privileges | Feature 5, 6, 7: Spending accumulation, auto tier promotion, digital card & progress bar, checkout VIP discount | M1 | PLANNED |
| M3 | Custom Discovery Gift Box Builder | Feature 8, 9, 10: 3-step builder, Cart/Order persistence, Cart & Admin display | M1 | PLANNED |
| M4 | RESTful API Suite & Swagger UI Portal | Feature 11, 12, 13, 14, 15, 16: OpenAPI configuration, Swagger UI at `/swagger`, 5 endpoints under `api/v1/...` | M1, M2, M3 | PLANNED |
| M5 | Automated Testing & Live Verification | Feature 18, 19: xUnit suite (`tests/Perfume.Tests`), Live Kestrel test runner, build 0 warnings/errors, 100% test pass | M4 | PLANNED |

## Interface Contracts

### M1: Flash Sale Engine
- Entity `FlashSale`: `Id`, `Name`, `Description`, `StartTime`, `EndTime`, `IsActive`, `BannerUrl`, `Items`.
- Entity `FlashSaleItem`: `Id`, `FlashSaleId`, `ProductId`, `VariantId`, `FlashSalePrice`, `StockLimit`, `SoldQuantity`.
- Admin routes: `GET /Admin/AdminDashboard/FlashSales`, `POST /Admin/AdminDashboard/CreateFlashSale`, `POST /Admin/AdminDashboard/ToggleFlashSaleStatus`.

### M2: Loyalty Tier Engine
- User tier thresholds: Bronze (0đ, 0%), Silver (5.000.000đ, 3%), Gold (15.000.000đ, 7%), Diamond (30.000.000đ, 12%).
- Method `ILoyaltyTierService.RecalculateUserTierAsync(string userId)`: Sums completed orders (`PaymentStatus.Completed` or `OrderStatus.Delivered`), updates `AppUser.Tier` and `UserLoyalty`.
- Checkout discount calculation: Applies tier discount rate to order subtotal concurrently with vouchers; records `LoyaltyDiscountAmount` and `AppliedLoyaltyTier` in `Order`.

### M3: Discovery Box Builder
- DTO `CustomBoxConfigDto`:
  - `BoxStyle`: `ClassicWood` (250.000đ), `RoyalVelvet` (200.000đ), `ArtisanLeather` (180.000đ).
  - `Vials`: List of 3 to 5 items (`ProductId`, `ProductName`, `VolumeMl`, `Price`).
  - `GreetingMessage`: string (up to 250 chars).
  - `GreetingFont`: `Playfair Display`, `Dancing Script`, `Cormorant Garamond`.
  - `TotalPrice`: `BoxPrice + Sum(Vials.Price)`.
- Persistence: `ShoppingCartItem` and `OrderItem` carry `IsCustomBox`, `CustomBoxConfigJson`, `CustomUnitPrice`.

### M4: RESTful API Endpoints (`api/v1/...`)
- `GET /api/v1/flashsales/active` -> `200 OK` (ActiveFlashSaleResponse).
- `GET /api/v1/loyalty/status` -> `200 OK` (LoyaltyStatusDto), `401 Unauthorized`.
- `POST /api/v1/discovery-box/validate-and-add` -> `200 OK` / `201 Created`, `400 Bad Request`.
- `GET /api/v1/orders/track?orderCode={code}` -> `200 OK` (OrderTrackingResponse), `404 Not Found`.
- `POST /api/v1/experience/tasting-booking` -> `201 Created` (TastingBookingResponse), `400 Bad Request`.
- Swagger UI route: `/swagger`.

## Code Layout
- `src/Perfume.Domain/Entities/EcommerceEntities.cs`: Domain models & enums.
- `src/Perfume.Application/`: Service contracts and DTOs.
- `src/Perfume.Infrastructure/Data/PerfumeDbContext.cs`: EF Core DbContext & fluent configurations.
- `src/Perfume.Infrastructure/Data/DatabaseSchemaUpgrader.cs`: Additive schema version upgrades.
- `src/Perfume.Infrastructure/Services/`: Implementations of business services.
- `src/Perfume.Web/Controllers/`: MVC Controllers (`HomeController`, `CatalogController`, `CartController`, `CheckoutController`, `AccountController`).
- `src/Perfume.Web/Areas/Admin/Controllers/AdminDashboardController.cs`: Admin controller.
- `src/Perfume.Web/Controllers/Api/`: Modular API Controllers.
- `src/Perfume.Web/Views/`: Razor views.
- `src/Perfume.Web/Areas/Admin/Views/`: Admin Razor views.
- `src/Perfume.Web/wwwroot/`: Static CSS, JS, images.
- `tests/Perfume.Tests/`: xUnit test suite project.
