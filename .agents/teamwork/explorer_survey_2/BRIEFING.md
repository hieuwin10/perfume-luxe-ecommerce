# BRIEFING — 2026-10-12T08:06:50Z

## Mission
Explore web presentation layer, controllers, Razor views, view models, and client-side assets (CSS/JS) for R1 (Flash Sale), R2 (VIP Loyalty Tier), R3 (Custom Discovery Gift Box Builder).

## 🔒 My Identity
- Archetype: explorer
- Roles: read-only investigation, UI and presentation layer survey
- Working directory: C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\explorer_survey_2
- Original parent: b6612c94-7adb-45ee-ad42-f5cfd31ddb1f
- Milestone: exploration_and_survey

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Deliver survey_report.md and handoff.md in working directory
- Keep progress.md updated with heartbeats

## Current Parent
- Conversation ID: b6612c94-7adb-45ee-ad42-f5cfd31ddb1f
- Updated: 2026-10-12T08:06:50Z

## Investigation State
- **Explored paths**:
  - `src/Perfume.Web/Views/Shared/_Layout.cshtml`, `_AdminLayout.cshtml`
  - `src/Perfume.Web/Views/Home/Index.cshtml`, `src/Perfume.Web/Controllers/HomeController.cs`
  - `src/Perfume.Web/Views/Catalog/Index.cshtml`, `Detail.cshtml`, `src/Perfume.Web/Controllers/CatalogController.cs`
  - `src/Perfume.Web/Views/Cart/Index.cshtml`, `src/Perfume.Web/Controllers/CartController.cs`
  - `src/Perfume.Web/Views/Checkout/Index.cshtml`, `src/Perfume.Web/Controllers/CheckoutController.cs`
  - `src/Perfume.Web/Views/Account/Profile.cshtml`, `MyOrders.cshtml`, `OrderDetails.cshtml`, `src/Perfume.Web/Controllers/AccountController.cs`
  - `src/Perfume.Web/Areas/Admin/Views/AdminDashboard/Index.cshtml`, `Orders.cshtml`, `Products.cshtml`, `src/Perfume.Web/Areas/Admin/Controllers/AdminDashboardController.cs`, `StoreManagerController.cs`
  - `src/Perfume.Web/Controllers/CustomerApiController.cs`
  - `src/Perfume.Web/wwwroot/css/luxury-perfume.css`, `wwwroot/js/customer-api.js`
- **Key findings**:
  - R1: Admin needs `/Admin/AdminDashboard/FlashSales` with campaign management view & nav link in `_AdminLayout.cshtml`. Homepage needs Flash Sale banner with second-accurate countdown timer. Product cards and detail views need badge and automatic price reversion on expiry/out of stock.
  - R2: UserTier already in Domain Enums (Bronze 0đ, Silver 5M 3%, Gold 15M 7%, Diamond 30M 12%). `/Account/Profile` currently has basic text card; needs Digital VIP Card with metallic gradient and spending progress bar. `/Checkout` needs automatic VIP discount calculation alongside voucher discount.
  - R3: 3-step builder (box selection, 3-5 vials with 2ml-5ml, greeting message & handwriting font) integrates smoothly into Cart, Checkout, Account OrderDetails, and Admin OrderDetails.
- **Unexplored areas**: None for presentation layer.

## Key Decisions Made
- Generated exhaustive survey report `survey_report.md` with complete Touchpoint Matrix.
- Generated standard 5-component `handoff.md`.

## Artifact Index
- survey_report.md — Detailed presentation layer survey findings
- handoff.md — Standard handoff report
- progress.md — Liveness heartbeat
- DISPATCH.md — Incoming dispatch record
