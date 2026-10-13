# TEAM 2 — COMPREHENSIVE TDD EXECUTION REPORT (100 SCENARIOS)

- **Squad**: TEAM 2 (Customer Account, Automatic VIP Loyalty Tier Engine & 3-Step Discovery Gift Box Builder TDD Squad)
- **Methodology**: `/enterprise` · `/spec-driven-development` · `/test-driven-development`
- **Scope**:
  - **Phần 3 (`part-3-account-loyalty`)**: 50 Test Scenarios (`TC-T2-001` to `TC-T2-050`)
  - **Phần 4 (`part-4-discovery-box`)**: 50 Test Scenarios (`TC-T2-051` to `TC-T2-100`)
- **Test Suite File**: `tests/Perfume.Tests/Team2_LoyaltyAndDiscoveryBoxTests.cs`
- **Implementation Files**:
  - `src/Perfume.Application/DTOs/LoyaltyAndDiscoveryDtos.cs`
  - `src/Perfume.Application/Interfaces/ILoyaltyAndDiscoveryServices.cs`
  - `src/Perfume.Infrastructure/Services/LoyaltyAndDiscoveryServices.cs`

---

## 1. Executive Summary Metrics

| Metric | Part 3 (`part-3-account-loyalty`) | Part 4 (`part-4-discovery-box`) | Total (Team 2) |
| :--- | :---: | :---: | :---: |
| **Planned Scenarios** | 50 (`TC-T2-001` .. `TC-T2-050`) | 50 (`TC-T2-051` .. `TC-T2-100`) | **100** |
| **Executed xUnit Tests** | 50 | 50 | **100** |
| **Passed (`PASS`)** | 50 | 50 | **100** |
| **Failed (`FAIL`)** | 0 | 0 | **0** |
| **Pass Rate** | **100.0%** | **100.0%** | **100.0%** |

---

## 2. Phần 3: Customer Account & Automatic VIP Loyalty Tier Engine (`TC-T2-001` to `TC-T2-050`)

| Test ID | Category | Scenario Description | Expected Outcome | Status |
| :--- | :--- | :--- | :--- | :---: |
| `TC-T2-001` | Account & Identity | Register valid customer (`FullName`, `Email`, `Password`, `ConfirmPassword`) | Creates `AppUser`, assigns `Customer` role, initializes `UserLoyalty` (`Bronze`, `0` pts, `0₫`) | **PASS** |
| `TC-T2-002` | Account & Identity | Reject duplicate email registration (case-insensitive check) | Returns validation error `"Email này đã được sử dụng."` | **PASS** |
| `TC-T2-003` | Account & Identity | Reject empty `FullName` (`""`) on registration | Fails validation (`FullName` length 2..100 required) | **PASS** |
| `TC-T2-004` | Account & Identity | Reject whitespace-only `FullName` (`"   "`) on registration | Fails validation after trimming | **PASS** |
| `TC-T2-005` | Account & Identity | Reject 1-character `FullName` (`"A"`, lower boundary - 1) | Fails validation (`FullName` < 2 chars) | **PASS** |
| `TC-T2-006` | Account & Identity | Reject 101-character `FullName` (upper boundary + 1) | Fails validation (`FullName` > 100 chars) | **PASS** |
| `TC-T2-007` | Account & Identity | Reject empty `Email` (`""`) on registration | Fails email format validation | **PASS** |
| `TC-T2-008` | Account & Identity | Reject email missing `@` symbol (`"plainaddress-without-at-symbol"`) | Fails email format validation | **PASS** |
| `TC-T2-009` | Account & Identity | Reject email missing domain part (`"missingdomain@"`) | Fails email format validation | **PASS** |
| `TC-T2-010` | Password Policy | Reject password shorter than 8 characters (`"Short1!"`) | `ValidatePasswordPolicy` returns `false` | **PASS** |
| `TC-T2-011` | Password Policy | Reject password missing uppercase character (`"alllowercase123!"`) | `ValidatePasswordPolicy` returns `false` | **PASS** |
| `TC-T2-012` | Password Policy | Reject password missing lowercase character (`"ALLUPPERCASE123!"`) | `ValidatePasswordPolicy` returns `false` | **PASS** |
| `TC-T2-013` | Password Policy | Reject password missing numeric digit (`"NoDigitsPassword!"`) | `ValidatePasswordPolicy` returns `false` | **PASS** |
| `TC-T2-014` | Password Policy | Reject password missing special character (`"NoSpecialChar2026"`) | `ValidatePasswordPolicy` returns `false` | **PASS** |
| `TC-T2-015` | Account & Identity | Reject registration when `Password != ConfirmPassword` | Returns error `"Mật khẩu xác nhận không khớp."` | **PASS** |
| `TC-T2-016` | Authentication | Login with valid credentials & auto-sync `UserLoyalty` (`16,000,000₫` Delivered order) | Returns profile with `Tier = "Gold"`, `LoyaltyPoints = 1600` | **PASS** |
| `TC-T2-017` | Authentication | Reject login with wrong password or unregistered email | Returns error `"Email hoặc mật khẩu không đúng."` | **PASS** |
| `TC-T2-018` | Profile Management | Update profile (`FullName`, `PhoneNumber`, `Address`) with surrounding whitespace | Trims and persists updated fields accurately | **PASS** |
| `TC-T2-019` | Profile Management | Reject profile update with invalid `PhoneNumber` or `Address > 500` chars | Returns 2 validation errors and preserves existing user state | **PASS** |
| `TC-T2-020` | Tier Boundary | Cumulative spend `0₫` exact boundary | `UserTier.Bronze`, `0%` VIP discount | **PASS** |
| `TC-T2-021` | Tier Boundary | Cumulative spend `4,999,999₫` exact upper boundary of Bronze | `UserTier.Bronze`, `0%` VIP discount | **PASS** |
| `TC-T2-022` | Tier Boundary | Cumulative spend `5,000,000₫` exact lower boundary of Silver | `UserTier.Silver`, `3%` VIP discount | **PASS** |
| `TC-T2-023` | Tier Boundary | Cumulative spend `14,999,999₫` exact upper boundary of Silver | `UserTier.Silver`, `3%` VIP discount | **PASS** |
| `TC-T2-024` | Tier Boundary | Cumulative spend `15,000,000₫` exact lower boundary of Gold | `UserTier.Gold`, `7%` VIP discount | **PASS** |
| `TC-T2-025` | Tier Boundary | Cumulative spend `29,999,999₫` exact upper boundary of Gold | `UserTier.Gold`, `7%` VIP discount | **PASS** |
| `TC-T2-026` | Tier Boundary | Cumulative spend `30,000,000₫` exact lower boundary of Diamond | `UserTier.Diamond`, `12%` VIP discount | **PASS** |
| `TC-T2-027` | Tier Boundary | Cumulative spend `100,000,000₫` high-value Diamond VIP | `UserTier.Diamond`, `12%` VIP discount | **PASS** |
| `TC-T2-028` | Loyalty Points | Negative spend safety (`-50,000₫`) | `0` points | **PASS** |
| `TC-T2-029` | Loyalty Points | Zero spend (`0₫`) | `0` points | **PASS** |
| `TC-T2-030` | Loyalty Points | Sub-unit spend (`9,999₫` < `10,000₫`) | `0` points (floor rounding) | **PASS** |
| `TC-T2-031` | Loyalty Points | Exact 1-point unit (`10,000₫`) | `1` point | **PASS** |
| `TC-T2-032` | Loyalty Points | Upper Bronze boundary (`4,999,999₫`) | `499` points | **PASS** |
| `TC-T2-033` | Loyalty Points | Gold threshold (`15,000,000₫`) | `1,500` points | **PASS** |
| `TC-T2-034` | Loyalty Points | Ultra Diamond spend (`100,000,000₫`) | `10,000` points | **PASS** |
| `TC-T2-035` | Tier Progress | Progress at `0₫` (Bronze start) | `0%` progress, `5,000,000₫` to Silver | **PASS** |
| `TC-T2-036` | Tier Progress | Progress at `2,500,000₫` (Bronze midpoint) | `50%` progress, `2,500,000₫` to Silver | **PASS** |
| `TC-T2-037` | Tier Progress | Progress at `5,000,000₫` (Silver start) | `0%` progress in Silver, `10,000,000₫` to Gold | **PASS** |
| `TC-T2-038` | Tier Progress | Progress at `10,000,000₫` (Silver midpoint) | `50%` progress in Silver, `5,000,000₫` to Gold | **PASS** |
| `TC-T2-039` | Tier Progress | Progress at `15,000,000₫` (Gold start) | `0%` progress in Gold, `15,000,000₫` to Diamond | **PASS** |
| `TC-T2-040` | Tier Progress | Progress at `22,500,000₫` (Gold midpoint) | `50%` progress in Gold, `7,500,000₫` to Diamond | **PASS** |
| `TC-T2-041` | Tier Progress | Progress at `30,000,000₫` (Diamond reached) | `100%` progress, `0₫` `AmountToNextTier` | **PASS** |
| `TC-T2-042` | Qualifying Orders | Filter qualifying orders (`Delivered` or `PaymentStatus.Completed`), excluding `Cancelled`, `Pending`, `Refunded`, and `IsDeleted` | Counts only 2 valid orders (`7,500,000₫`), upgrades to `Silver` (`3%`) | **PASS** |
| `TC-T2-043` | Tier Upgrade | Multi-order cumulative progression `Bronze` $\rightarrow$ `Silver` $\rightarrow$ `Gold` $\rightarrow$ `Diamond` | Automatically transitions tier and sets `NextTier = null` at Diamond | **PASS** |
| `TC-T2-044` | Order VIP Discount | `Bronze` (`0%`) order pricing (`4,000,000₫`, no voucher, no gift wrap) | `LoyaltyDiscountAmount = 0₫`, `TotalAmount = 4,000,000₫` | **PASS** |
| `TC-T2-045` | Order VIP Discount | `Silver` (`3%`) order (`5,000,000₫`) + `200,000₫` voucher + `50,000₫` Royal Gift Wrap | `LoyaltyDiscountAmount = 150,000₫`, `TotalAmount = 4,700,000₫` | **PASS** |
| `TC-T2-046` | Order VIP Discount | `Gold` (`7%`) order (`10,000,000₫`) + `500,000₫` voucher + `50,000₫` Royal Gift Wrap | `LoyaltyDiscountAmount = 700,000₫`, `TotalAmount = 8,850,000₫` | **PASS** |
| `TC-T2-047` | Order VIP Discount | `Diamond` (`12%`) order (`10,000,000₫`) + `1,000,000₫` voucher | `LoyaltyDiscountAmount = 1,200,000₫`, `TotalAmount = 7,800,000₫` | **PASS** |
| `TC-T2-048` | Shipping & Fees | Order with `SubTotal = 1,500,000₫` (< `2,000,000₫` free-shipping threshold) | Adds `35,000₫` shipping fee (`FinalTotalAmount = 1,535,000₫`) | **PASS** |
| `TC-T2-049` | Discount Cap | Combined Voucher (`1,900,000₫`) + Diamond VIP (`12%`) on `2,000,000₫` order | Caps VIP discount at `100,000₫` so total never becomes negative (`0₫`) | **PASS** |
| `TC-T2-050` | Guest Fallback | `GetOrSyncUserLoyaltyAsync` called with empty `userId` | Safely returns `Bronze`, `0%` discount, `5,000,000₫` to next tier | **PASS** |

---

## 3. Phần 4: 3-Step Discovery Gift Box Interactive Builder (`TC-T2-051` to `TC-T2-100`)

| Test ID | Step / Category | Scenario Description | Expected Outcome | Status |
| :--- | :--- | :--- | :--- | :---: |
| `TC-T2-051` | Step 1: Box Style | Retrieve catalog of available luxury box styles | Returns 3 styles: `ClassicWood` (`250k`), `RoyalVelvet` (`200k`), `ArtisanLeather` (`180k`) | **PASS** |
| `TC-T2-052` | Step 1: Box Style | Select `ClassicWood` (Hộp Gỗ Sơn Mài Hoàng Gia) | Applies box price `250,000₫` | **PASS** |
| `TC-T2-053` | Step 1: Box Style | Select `RoyalVelvet` (Hộp Nhung Đen Huyền Bí) | Applies box price `200,000₫` | **PASS** |
| `TC-T2-054` | Step 1: Box Style | Select `ArtisanLeather` (Túi Da Thủ Công Viền Vàng) | Applies box price `180,000₫` | **PASS** |
| `TC-T2-055` | Step 1: Box Style | Reject empty box style code (`""`) | Fails validation with invalid style error | **PASS** |
| `TC-T2-056` | Step 1: Box Style | Reject whitespace-only box style code (`"   "`) | Fails validation with invalid style error | **PASS** |
| `TC-T2-057` | Step 1: Box Style | Reject unknown box style `"PlasticBox"` | Fails validation with invalid style error | **PASS** |
| `TC-T2-058` | Step 1: Box Style | Reject lowercase `"classicwood"` | Enforces exact style code validation | **PASS** |
| `TC-T2-059` | Step 1: Box Style | Reject uppercase `"ROYALVELVET"` | Enforces exact style code validation | **PASS** |
| `TC-T2-060` | Step 1: Box Style | Reject unsupported `"CardboardPaper"` | Fails validation with invalid style error | **PASS** |
| `TC-T2-061` | Step 2: Vial Count | Boundary test: `0` vials selected (< 3 minimum) | Rejected (`IsValid = false`) | **PASS** |
| `TC-T2-062` | Step 2: Vial Count | Boundary test: `1` vial selected (< 3 minimum) | Rejected (`IsValid = false`) | **PASS** |
| `TC-T2-063` | Step 2: Vial Count | Boundary test: `2` vials selected (lower boundary - 1) | Rejected (`IsValid = false`) | **PASS** |
| `TC-T2-064` | Step 2: Vial Count | Boundary test: `3` vials selected (exact minimum boundary) | Accepted (`IsValid = true`) | **PASS** |
| `TC-T2-065` | Step 2: Vial Count | Boundary test: `4` vials selected (interior valid count) | Accepted (`IsValid = true`) | **PASS** |
| `TC-T2-066` | Step 2: Vial Count | Boundary test: `5` vials selected (exact maximum boundary) | Accepted (`IsValid = true`) | **PASS** |
| `TC-T2-067` | Step 2: Vial Count | Boundary test: `6` vials selected (upper boundary + 1) | Rejected (`IsValid = false`) | **PASS** |
| `TC-T2-068` | Step 2: Vial Count | Boundary test: `7` vials selected (> 5 maximum) | Rejected (`IsValid = false`) | **PASS** |
| `TC-T2-069` | Step 2: Vial Count | Boundary test: `10` vials selected (> 5 maximum) | Rejected (`IsValid = false`) | **PASS** |
| `TC-T2-070` | Step 2: Vial Pricing | `2ml` vial of `3,000,000₫` base product (`3%` = `90k` < `120k` floor) | Applies luxury floor price `120,000₫` | **PASS** |
| `TC-T2-071` | Step 2: Vial Pricing | `2ml` vial of `5,000,000₫` base product (`3%` = `150,000₫`) | Calculates `150,000₫` | **PASS** |
| `TC-T2-072` | Step 2: Vial Pricing | `2ml` vial of `6,500,000₫` base product (`3%` = `195,000₫`) | Calculates `195,000₫` | **PASS** |
| `TC-T2-073` | Step 2: Vial Pricing | `5ml` vial of `3,500,000₫` base product (`6%` = `210k` < `250k` floor) | Applies luxury floor price `250,000₫` | **PASS** |
| `TC-T2-074` | Step 2: Vial Pricing | `5ml` vial of `5,000,000₫` base product (`6%` = `300,000₫`) | Calculates `300,000₫` | **PASS** |
| `TC-T2-075` | Step 2: Vial Pricing | `5ml` vial of `10,000,000₫` base product (`6%` = `600,000₫`) | Calculates `600,000₫` | **PASS** |
| `TC-T2-076` | Step 2: Vial Volume | Reject `0ml` vial volume | Rejected (`Chỉ chấp nhận 2ml hoặc 5ml`) | **PASS** |
| `TC-T2-077` | Step 2: Vial Volume | Reject `1ml` vial volume | Rejected (`Chỉ chấp nhận 2ml hoặc 5ml`) | **PASS** |
| `TC-T2-078` | Step 2: Vial Volume | Reject `10ml` vial volume in `2ml`/`5ml` Discovery Builder | Rejected (`Chỉ chấp nhận 2ml hoặc 5ml`) | **PASS** |
| `TC-T2-079` | Step 2: Stock Check | Reject Discovery Box when any selected perfume has `IsInStock = false` | Rejected with out-of-stock message | **PASS** |
| `TC-T2-080` | Step 2: Existence | Reject Discovery Box when any selected perfume is soft-deleted (`IsDeleted = true`) | Rejected with non-existent/deleted product error | **PASS** |
| `TC-T2-081` | Step 3: Typography | Select `"Playfair Display"` font for Gold-Foil Greeting Card | Accepted and stored in configuration | **PASS** |
| `TC-T2-082` | Step 3: Typography | Select `"Dancing Script"` font for Gold-Foil Greeting Card | Accepted and stored in configuration | **PASS** |
| `TC-T2-083` | Step 3: Typography | Select `"Cormorant Garamond"` font for Gold-Foil Greeting Card | Accepted and stored in configuration | **PASS** |
| `TC-T2-084` | Step 3: Typography | Reject unsupported font `"Comic Sans MS"` | Rejected (`Font chữ ... không được hỗ trợ`) | **PASS** |
| `TC-T2-085` | Step 3: Typography | Reject unsupported font `"Arial"` | Rejected (`Font chữ ... không được hỗ trợ`) | **PASS** |
| `TC-T2-086` | Step 3: Typography | Reject unsupported font `"Papyrus"` | Rejected (`Font chữ ... không được hỗ trợ`) | **PASS** |
| `TC-T2-087` | Step 3: Greeting Card | Greeting message with exact `250` characters + surrounding whitespace | Trimmed to `250` chars and accepted | **PASS** |
| `TC-T2-088` | Step 3: Greeting Card | Greeting message with `251` characters (upper boundary + 1) | Rejected (`không được vượt quá 250 ký tự`) | **PASS** |
| `TC-T2-089` | Step 3: Typography | Blank/whitespace `GreetingFont` fallback | Defaults cleanly to `"Playfair Display"` | **PASS** |
| `TC-T2-090` | Step 3: Recipient | `RecipientName` exceeding `100` characters (`101` chars) | Rejected (`Tên người nhận không được vượt quá 100 ký tự`) | **PASS** |
| `TC-T2-091` | Cart Persistence | `AddDiscoveryBoxToCartAsync` for authenticated customer (`ClassicWood` + 4 mixed 2ml/5ml vials, `Qty = 2`) | Persists `ShoppingCartItem` with `IsCustomBox = true`, `CustomUnitPrice = 1,300,000₫`, and full `CustomBoxConfigJson` | **PASS** |
| `TC-T2-092` | Cart Persistence | `AddDiscoveryBoxToCartAsync` for anonymous guest (`guestToken`) | Creates SHA-256 `guest:` cart and persists custom box item | **PASS** |
| `TC-T2-093` | Cart Persistence | Reject `AddDiscoveryBoxToCartAsync` when both `userId` and `guestToken` are blank | Returns error `"Không xác định được phiên giỏ hàng"` | **PASS** |
| `TC-T2-094` | Quantity Boundary | Reject Discovery Box with `Quantity = 0` | Rejected (`Số lượng hộp quà phải từ 1 đến 20`) | **PASS** |
| `TC-T2-095` | Quantity Boundary | Reject Discovery Box with `Quantity = 21` | Rejected (`Số lượng hộp quà phải từ 1 đến 20`) | **PASS** |
| `TC-T2-096` | Order Persistence | `PlaceDiscoveryBoxOrderAsync` with `RoyalVelvet` + 5 vials (`5ml`) + Royal Gift Wrapping (`50k`) | Persists `Order` & `OrderItem` (`IsCustomBox = true`, `UnitPrice = 2,220,000₫`, `TotalAmount = 2,270,000₫`) | **PASS** |
| `TC-T2-097` | Order + VIP Loyalty | `Diamond` member (`12%` VIP discount) orders 2 `ClassicWood` Discovery Boxes (`SubTotal = 2,380,000₫`) | Automatically applies `285,600₫` VIP discount (`TotalAmount = 2,094,400₫`) | **PASS** |
| `TC-T2-098` | Order Validation | Reject `PlaceDiscoveryBoxOrderAsync` when shipping recipient fields are missing | Fails validation cleanly without creating orphan order | **PASS** |
| `TC-T2-099` | End-to-End Integration | Delivering a Discovery Box order pushes customer cumulative spend over `5,000,000₫` | Automatically upgrades customer from `Bronze` to `Silver` (`3%`) | **PASS** |
| `TC-T2-100` | Typography Catalog | `GetAllowedFonts` returns exact 3 luxury fonts | Returns `["Playfair Display", "Dancing Script", "Cormorant Garamond"]` | **PASS** |
