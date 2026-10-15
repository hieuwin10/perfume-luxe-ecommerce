# E2E Test Infra: PERFUME LUXE

## Test Philosophy
- Opaque-box, requirement-driven testing derived from `ORIGINAL_REQUEST.md`.
- Methodology: Category-Partition + Boundary Value Analysis (BVA) + Pairwise Combinatorial + Real-World Workload Testing.
- Multi-tier validation: Unit testing (xUnit), in-process API testing (`WebApplicationFactory`), and live external Kestrel socket verification with HTTP client scripts.

## Feature Inventory & Test Coverage Mapping
| # | Feature | Requirement | Tier 1 (Feature) | Tier 2 (Boundary) | Tier 3 (Cross-Feature) | Tier 4 (Real-World) |
|---|---------|-------------|:----------------:|:-----------------:|:----------------------:|:-------------------:|
| 1 | Flash Sale Engine | R1 | >= 5 tests | >= 5 tests | Pairwise | Real-world scenario 1 |
| 2 | Loyalty Tier Engine | R2 | >= 5 tests | >= 5 tests | Pairwise | Real-world scenario 2 |
| 3 | Custom Discovery Box | R3 | >= 5 tests | >= 5 tests | Pairwise | Real-world scenario 3 |
| 4 | RESTful API Suite | R4 | >= 5 tests | >= 5 tests | Pairwise | Real-world scenario 4 |
| 5 | OpenAPI & Swagger UI | R4 | >= 5 tests | >= 5 tests | Pairwise | Real-world scenario 5 |

## Test Architecture
- **Test Project**: `tests/Perfume.Tests/Perfume.Tests.csproj` in `PerfumeEcommerce.sln`.
- **Frameworks**: xUnit, Moq, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory`.
- **Test Runner Commands**:
  - `dotnet test tests/Perfume.Tests/Perfume.Tests.csproj --verbosity normal`
- **Live Kestrel Verification**:
  - Automated script executing HTTP requests against `http://localhost:5000` (or assigned port) verifying `/swagger`, `/swagger/v1/swagger.json`, `/api/v1/flashsales/active`, `/api/v1/loyalty/status`, `/api/v1/discovery-box/validate-and-add`, `/api/v1/orders/track`, `/api/v1/experience/tasting-booking`.

## Real-World Application Scenarios (Tier 4)
| # | Scenario | Features Exercised | Expected Outcome |
|---|----------|--------------------|------------------|
| 1 | Flash Sale Purchase Lifecycle | Flash Sale, Cart, Order, Countdown Expiry | Customer purchases flash sale item at discounted price within window; stock decrements; price reverts upon expiration. |
| 2 | Cumulative Spending VIP Tier Upgrades | Order Completion, Loyalty Tier, Checkout | Customer places orders totaling 15M; automatically promoted from Bronze to Gold; subsequent checkout immediately applies 7% VIP discount. |
| 3 | Custom Discovery Box End-to-End Flow | Discovery Box Builder, Pricing, Cart, Checkout, Admin Order Details | Customer selects Velvet Box, 4 vials (2ml/5ml), custom greeting; package price calculated; order placed; Admin view shows full box spec. |
| 4 | Combined Voucher & VIP Loyalty Discount | Loyalty Discount, Voucher Validation, Order Subtotal | Customer applies 7% Gold tier discount and a valid 100K voucher simultaneously; total reflects both deductions without conflict. |
| 5 | External Client API & Swagger Live Exploration | Swagger UI, JSON schema, All 5 REST Endpoints | External client queries active flash sale, tests discovery box validation, books boutique tasting, and inspects Swagger UI live. |

## Coverage Thresholds
- Tier 1: >= 25 test cases across features
- Tier 2: >= 25 test cases for edge/boundary conditions
- Tier 3: >= 10 cross-feature combinatorial test cases
- Tier 4: >= 5 end-to-end real-world workflow scenarios
- Total Target: >= 65 comprehensive test cases
