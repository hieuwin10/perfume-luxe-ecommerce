# Execution Plan: PERFUME LUXE Commercial & API System

## Objectives
Implement R1-R5 and all Acceptance Criteria from `ORIGINAL_REQUEST.md`:
- R1: Flash Sale & Real-time Countdown Timer Engine (Admin + Storefront + Product Details)
- R2: Loyalty Tier Engine (Auto-rank calculation, digital VIP card, checkout discount)
- R3: Custom Discovery Gift Box Builder (3-step custom box builder, cart & order integration)
- R4: Full RESTful API Suite (`/api/v1/...`) & OpenAPI Swagger UI Live Test (`/swagger`)
- R5: Additive Schema Upgrader, xUnit test suite (TDD), Kestrel live test runner

## Phases

### Phase 0: Survey & Scope Mapping
- Dispatch 3 parallel Explorers:
  - Explorer 1 (Core Architecture & DB Schema): Examine ASP.NET Core solution structure, DbContext, Models, Migrations, SeedData.
  - Explorer 2 (Storefront, Controllers & Views): Examine Razor Views, ViewModels, Cart, Checkout, Order flow, Customer Profile.
  - Explorer 3 (API, Swagger & Testing): Examine existing API endpoints, Swagger setup (if any), Program.cs pipeline, `tests/Perfume.Tests`.
- Synthesize findings into `PROJECT.md` Feature Inventory and `TEST_INFRA.md`.

### Phase 1: Architecture & Milestone Decomposition
- Define Milestones M1 through M5 in `PROJECT.md`.
- Formulate interface contracts, data models, and API schemas.
- Set up E2E testing strategy.

### Phase 2: Milestone Execution (Explorer -> Worker -> Reviewer -> Challenger -> Auditor Gate)
- **M1**: Flash Sale Engine & Real-time Countdown Timer (Admin, Homepage Banner, Badges, Expiry logic).
- **M2**: Loyalty Tier Engine (Spending calculation, Bronze/Silver/Gold/Diamond tiers, Profile card, Checkout discount).
- **M3**: Custom Discovery Gift Box Builder (3 box types, 3-5 vials, custom greeting message, Cart/Checkout/Admin integration).
- **M4**: RESTful API Suite & Swagger UI Integration (`/api/v1/...`, Swashbuckle OpenAPI, Live Test enabled).
- **M5**: Additive Schema Upgrader & Automated xUnit Suite (`tests/Perfume.Tests`), Kestrel live verification.

### Phase 3: Final Verification & Audit
- Full regression test run (`dotnet build` with 0 warnings/errors, `dotnet test` 100% pass).
- Live Kestrel HTTP verification for all API endpoints and Swagger UI.
- Forensic Integrity Audit (`teamwork_preview_auditor`).
- Send completion report to Sentinel (`f0eaaf0d-db42-4fe8-aa20-d6f70f031365`).
