# TEAM 3 — Admin ERP, Financial P&L, 5-Tier RBAC, Security Audit & RESTful API v1 / Swagger Test Report

- **Team**: Team 3 (Admin ERP, Financial P&L, 5-Tier RBAC, Security Audit & RESTful API v1 / Swagger TDD Squad)
- **Test Suite File**: `tests/Perfume.Tests/Team3_AdminErpAndSwaggerApiTests.cs`
- **API Controller File**: `src/Perfume.Web/Controllers/ApiV1ExtensionsController.cs`
- **Assigned Scope**: 100 Scenarios (`TC-T3-001` to `TC-T3-100`, expanding to **149 xUnit test executions** via `[Fact]` and `[Theory]`)
- **Pass Rate**: **100% (100/100 scenarios — 149/149 test cases passed, 0 failed)**

---

## 1. Executive Summary

Team 3 designed and executed a comprehensive 100-scenario automated TDD suite covering two core enterprise domains of the **Perfume Luxe** platform:

1. **Phần 5 — Admin ERP, Financial P&L, Inventory Valuation, 5-Tier RBAC & Security Audit (`TC-T3-001` to `TC-T3-050`)**:
   - **Financial P&L Engine (`AdminDashboardController.FinancialReport`)**: Verified exact calculations for `GrossSales`, `VoucherDiscounts`, `GiftWrappingFees`, `ShippingFees`, and `NetRevenue = GrossSales - VoucherDiscounts + GiftWrappingFees + ShippingFees`.
   - **COGS & Gross Profit**: Verified that Cost of Goods Sold (COGS) uses the latest positive `StockAdjustment.UnitCost` per variant and falls back to `42%` of item unit price (`item.UnitPrice * 0.42m`) when no unit cost is recorded. Verified `GrossProfit = NetRevenue - EstimatedCogs` and `GrossMarginPercent = Round(GrossProfit / NetRevenue * 100, 1)`.
   - **Inventory Valuation by Brand**: Verified aggregation of `TotalUnits`, `CostValuation`, `RetailValuation` (`SalePrice ?? OriginalPrice ?? BasePrice`), and `PotentialProfit`, ordered descending by `RetailValuation`.
   - **UTF-8 BOM CSV Exports & CSV Injection Hardening**: Verified `ExportFinancialOrdersCsv`, `ExportInventoryValuationCsv`, and `StoreManagerController.ExportCsv` emit UTF-8 BOM (`0xEF, 0xBB, 0xBF`) and sanitize spreadsheet formula injection payloads (`=`, `+`, `-`, `@`).
   - **Admin ERP & Content Management**: Verified `FlashSaleService` CRUD/soft-delete, `SystemSettingService` persistence and `IConfiguration` fallback, `HeroBanner` management, `StockAdjustment` inventory restock/depletion with `Product.IsInStock` synchronization, `PrivateTastingBooking` status workflow (`Pending` -> `Confirmed` -> `Completed` / `Cancelled`), `BackInStockAlert` deduplication, and `CatalogManagerController` referential integrity guards.
   - **5-Tier RBAC & Security Audit**: Verified all 5 roles (`Admin`, `StoreManager`, `WarehouseStaff`, `Concierge`, `Customer`), role-change guardrails (preventing self-demotion and last-Admin removal/deletion), `[Authorize]` policy enforcement, `DatabaseAuditLog` filtering/pagination, and `DatabaseConsoleService` SQL safety guardrails (restricting `Initial Catalog` to `PerfumeDb` and blocking cross-database/server-level SQL commands).

2. **Phần 6 — RESTful API v1, OpenAPI / Swagger, Loyalty, Discovery Box, Order Tracking & Tasting Booking (`TC-T3-051` to `TC-T3-100`)**:
   - Implemented `src/Perfume.Web/Controllers/ApiV1ExtensionsController.cs` providing:
     - `GET /api/v1/flashsales/active`: Active Flash Sale campaign with `remainingSeconds`, `discountPercent`, and `remainingStock`.
     - `GET /api/v1/loyalty/status`: Authenticated customer VIP tier calculation (`Bronze` < 5M -> 0%, `Silver` >= 5M -> 3%, `Gold` >= 15M -> 7%, `Diamond` >= 30M -> 12%), `nextTier`, `amountToNextTier`, and manual VIP tier override support.
     - `POST /api/v1/discovery-box/validate-and-add`: Custom Discovery Box validation (3–5 vials, box styles `ClassicWood` / `RoyalVelvet` / `ArtisanLeather`, vial sizes `2ml` / `5ml`, greeting card message <= 250 chars, bundle discount 0% / 5% / 10%).
     - `GET /api/v1/orders/track?orderCode={code}`: Real-time order tracking with 4-stage timeline (`Pending`, `Confirmed`, `Shipping`, `Delivered`, plus `Cancelled` handling), item breakdown, gift wrapping, and optional phone verification.
     - `POST /api/v1/experience/tasting-booking`: Private Tasting session booking (`201 Created`) with full input validation (name, phone >= 8 digits, guests 1–6, future date).
     - `GET /swagger/v1/swagger.json` & `GET /swagger`: OpenAPI 3.0.1 specification with `BearerAuth` security scheme and interactive Swagger UI (`Try it out`).
   - Verified `JwtTokenService` token generation, claims (`Sub`, `Email`, `NameIdentifier`, `Role`), signature validation, and production signing key length enforcement.
   - Verified `CustomerApiController` catalog browsing, slug lookup, voucher validation, authenticated profile/orders, approved reviews filtering, and refresh token revocation.

---

## 2. Complete 100-Scenario Matrix (`TC-T3-001` to `TC-T3-100`)

### Phần 5: Admin ERP, Financial P&L, 5-Tier RBAC & Security Audit (`TC-T3-001` – `TC-T3-050`)

| Scenario ID | Test Method | Category | Status |
|---|---|---|---|
| `TC-T3-001` | `TC_T3_001_FinancialReport_ComputesGrossSalesDiscountsFeesAndNetRevenue` | Financial P&L | PASS |
| `TC-T3-002` | `TC_T3_002_FinancialReport_ExcludesPendingUnpaidAndCancelledOrders` | Financial P&L | PASS |
| `TC-T3-003` | `TC_T3_003_FinancialReport_CogsUsesLatestStockAdjustmentUnitCost` | COGS & Margin | PASS |
| `TC-T3-004` | `TC_T3_004_FinancialReport_CogsFallsBackTo42PercentWhenNoUnitCostRecorded` | COGS & Margin | PASS |
| `TC-T3-005` | `TC_T3_005_FinancialReport_MixedOrderItemsCombinesRecordedCostAnd42PercentFallback` | COGS & Margin | PASS |
| `TC-T3-006` | `TC_T3_006_FinancialReport_ZeroOrdersProducesSafeZeroMarginAndZeroAov` | Financial P&L | PASS |
| `TC-T3-007` | `TC_T3_007_FinancialReport_PresetsFilterOrdersCorrectly` (6 InlineData presets) | Financial P&L | PASS |
| `TC-T3-008` | `TC_T3_008_FinancialReport_InventoryValuationByBrandComputesUnitsCostRetailAndProfit` | Inventory Valuation | PASS |
| `TC-T3-009` | `TC_T3_009_FinancialReport_InventoryValuationFallsBackFromSalePriceToOriginalPriceToBasePrice` | Inventory Valuation | PASS |
| `TC-T3-010` | `TC_T3_010_FinancialReport_Populates14DayRevenueProfitChartAndFragranceFamilyBreakdown` | Financial P&L | PASS |
| `TC-T3-011` | `TC_T3_011_ExportFinancialOrdersCsv_IncludesUtf8BomAndCorrectContentType` | CSV Export | PASS |
| `TC-T3-012` | `TC_T3_012_ExportFinancialOrdersCsv_ContainsExpectedColumnsAndCalculatedRowValues` | CSV Export | PASS |
| `TC-T3-013` | `TC_T3_013_ExportInventoryValuationCsv_IncludesUtf8BomAndAccurateValuationColumns` | CSV Export | PASS |
| `TC-T3-014` | `TC_T3_014_StoreManagerExportCsv_IncludesBomAndEscapesFormulaInjectionPayloads` | CSV Security | PASS |
| `TC-T3-015` | `TC_T3_015_StoreManagerIndex_CalculatesRevenueCancellationRateAndStockAlerts` | StoreManager ERP | PASS |
| `TC-T3-016` | `TC_T3_016_AdminDashboardIndex_RedirectsStoreManagerAndRendersForAdmin` | RBAC Routing | PASS |
| `TC-T3-017` | `TC_T3_017_FlashSaleService_CreateFlashSaleAsync_PersistsCampaignAndItems` | FlashSale ERP | PASS |
| `TC-T3-018` | `TC_T3_018_FlashSaleService_GetAllAndGetById_ReturnOrderedCampaigns` | FlashSale ERP | PASS |
| `TC-T3-019` | `TC_T3_019_FlashSaleService_ToggleStatusAsync_FlipsActiveStateAndHandlesMissingId` | FlashSale ERP | PASS |
| `TC-T3-020` | `TC_T3_020_FlashSaleService_DeleteAsync_SoftDeletesAndExcludesFromQueries` | FlashSale ERP | PASS |
| `TC-T3-021` | `TC_T3_021_AdminDashboard_CreateFlashSale_ValidatesDatesAndApplies80PercentFallbackPrice` | FlashSale ERP | PASS |
| `TC-T3-022` | `TC_T3_022_SystemSettingService_GetSettingsAsync_ReturnsConfigurationFallbackWhenDbEmpty` | System Settings | PASS |
| `TC-T3-023` | `TC_T3_023_SystemSettingService_SaveSettingsAsync_CreatesAndUpdatesStoreConfiguration` | System Settings | PASS |
| `TC-T3-024` | `TC_T3_024_SystemSettingService_TestSmtpConnectionAsync_ReturnsFalseWhenSmtpDisabled` | System Settings | PASS |
| `TC-T3-025` | `TC_T3_025_AdminDashboard_SaveBanner_CreatesAndUpdatesHeroBanner` | HeroBanner CMS | PASS |
| `TC-T3-026` | `TC_T3_026_AdminDashboard_ToggleAndDeleteBanner_UpdatesAndRemovesEntity` | HeroBanner CMS | PASS |
| `TC-T3-027` | `TC_T3_027_AdminDashboard_AdjustStock_PositiveRestockUpdatesStockRecordsUnitCostAndSyncsProductInStock` | Inventory Sync | PASS |
| `TC-T3-028` | `TC_T3_028_AdminDashboard_AdjustStock_NegativeDepletionClampsZeroAndMarksProductOutOfStock` | Inventory Sync | PASS |
| `TC-T3-029` | `TC_T3_029_AdminDashboard_AdjustStock_RejectsZeroQuantityAndMissingVariant` | Inventory Sync | PASS |
| `TC-T3-030` | `TC_T3_030_AdminDashboard_Inventory_FiltersByLowOutAndSearchQuery` | Inventory ERP | PASS |
| `TC-T3-031` | `TC_T3_031_AdminDashboard_UpdateTastingStatus_TransitionsWorkflowCorrectly` (3 statuses) | Tasting Workflow | PASS |
| `TC-T3-032` | `TC_T3_032_AdminDashboard_Tastings_FiltersBookingsByStatus` | Tasting Workflow | PASS |
| `TC-T3-033` | `TC_T3_033_ExperienceController_SubscribeBackInStock_PersistsAndDeduplicatesUnnotifiedAlerts` | BackInStock Alert | PASS |
| `TC-T3-034` | `TC_T3_034_CatalogManager_PreventsDeletingBrandFamilyOrNoteLinkedToProducts` | Catalog Integrity | PASS |
| `TC-T3-035` | `TC_T3_035_Rbac5Tiers_AllFiveRolesCanBeSeededAndAssignedToUsers` | 5-Tier RBAC | PASS |
| `TC-T3-036` | `TC_T3_036_AdminDashboard_ChangeUserRole_AllowsAllFiveValidRbacRoles` (5 roles) | 5-Tier RBAC | PASS |
| `TC-T3-037` | `TC_T3_037_AdminDashboard_ChangeUserRole_RejectsInvalidRoleName` | 5-Tier RBAC | PASS |
| `TC-T3-038` | `TC_T3_038_AdminDashboard_ChangeUserRole_PreventsSelfDemotionAndLastAdminDemotion` | 5-Tier RBAC | PASS |
| `TC-T3-039` | `TC_T3_039_AdminDashboard_DeleteUser_PreventsDeletingSelfOrLastAdmin` | 5-Tier RBAC | PASS |
| `TC-T3-040` | `TC_T3_040_AdminDashboard_ToggleLockUser_LocksAndUnlocksUserAndPreventsSelfLock` | 5-Tier RBAC | PASS |
| `TC-T3-041` | `TC_T3_041_RbacAuthorizationAttributes_EnforcedOnAdminAndManagerControllers` | 5-Tier RBAC | PASS |
| `TC-T3-042` | `TC_T3_042_AdminDashboard_AuditLogs_FiltersByActionSucceededAndActor` | Security Audit | PASS |
| `TC-T3-043` | `TC_T3_043_AdminDashboard_AuditLogs_Paginates20PerPageAndClampsOutOfRangePage` | Security Audit | PASS |
| `TC-T3-044` | `TC_T3_044_DatabaseConsoleService_RejectsNonPerfumeDbInitialCatalog` (3 connection strings) | DB Console Guard | PASS |
| `TC-T3-045` | `TC_T3_045_DatabaseConsoleService_AcceptsPerfumeDbInitialCatalogCaseInsensitive` | DB Console Guard | PASS |
| `TC-T3-046` | `TC_T3_046_DatabaseConsoleService_ExecuteRawQueryAsync_BlocksForbiddenDatabaseScopeCommands` (8 SQL payloads) | DB Console Guard | PASS |
| `TC-T3-047` | `TC_T3_047_DatabaseConsoleService_ExecuteScriptFileAsync_BlocksCrossDatabaseScript` | DB Console Guard | PASS |
| `TC-T3-048` | `TC_T3_048_DatabaseConsoleController_ExecuteQuery_RequiresConfirmationForDestructiveSql` (5 SQL payloads) | DB Console Guard | PASS |
| `TC-T3-049` | `TC_T3_049_DatabaseConsoleController_SaveRowAndDeleteRow_RejectUnconfirmedRequests` | DB Console Guard | PASS |
| `TC-T3-050` | `TC_T3_050_DatabaseConsoleController_ExecuteQuery_WritesAuditLogWithCommandHash` | Security Audit | PASS |

### Phần 6: RESTful API v1, Swagger/OpenAPI, Loyalty, Discovery Box, Order Tracking & Tasting Booking (`TC-T3-051` – `TC-T3-100`)

| Scenario ID | Test Method | Category | Status |
|---|---|---|---|
| `TC-T3-051` | `TC_T3_051_ApiV1_GetActiveFlashSale_Returns404WhenNoFlashSaleExists` | FlashSale API v1 | PASS |
| `TC-T3-052` | `TC_T3_052_ApiV1_GetActiveFlashSale_Returns404WhenCampaignIsInactive` | FlashSale API v1 | PASS |
| `TC-T3-053` | `TC_T3_053_ApiV1_GetActiveFlashSale_Returns404WhenCampaignHasExpired` | FlashSale API v1 | PASS |
| `TC-T3-054` | `TC_T3_054_ApiV1_GetActiveFlashSale_Returns404WhenCampaignStartsInFuture` | FlashSale API v1 | PASS |
| `TC-T3-055` | `TC_T3_055_ApiV1_GetActiveFlashSale_Returns200OkWithOngoingCampaign` | FlashSale API v1 | PASS |
| `TC-T3-056` | `TC_T3_056_ApiV1_GetActiveFlashSale_ComputesPositiveRemainingSeconds` | FlashSale API v1 | PASS |
| `TC-T3-057` | `TC_T3_057_ApiV1_GetActiveFlashSale_ReturnsDiscountPercentAndRemainingStockPerItem` | FlashSale API v1 | PASS |
| `TC-T3-058` | `TC_T3_058_ApiV1_GetActiveFlashSale_ExcludesSoftDeletedFlashSaleItems` | FlashSale API v1 | PASS |
| `TC-T3-059` | `TC_T3_059_ApiV1_GetLoyaltyStatus_Returns401WhenUnauthenticated` | Loyalty API v1 | PASS |
| `TC-T3-060` | `TC_T3_060_ApiV1_GetLoyaltyStatus_Returns401WhenUserDoesNotExistInDb` | Loyalty API v1 | PASS |
| `TC-T3-061` | `TC_T3_061_ApiV1_GetLoyaltyStatus_ReturnsBronzeTierWithZeroDiscountForNewUser` | Loyalty API v1 | PASS |
| `TC-T3-062` | `TC_T3_062_ApiV1_GetLoyaltyStatus_ReturnsSilverTierWith3PercentDiscount` | Loyalty API v1 | PASS |
| `TC-T3-063` | `TC_T3_063_ApiV1_GetLoyaltyStatus_ReturnsGoldTierWith7PercentDiscount` | Loyalty API v1 | PASS |
| `TC-T3-064` | `TC_T3_064_ApiV1_GetLoyaltyStatus_ReturnsDiamondTierWith12PercentDiscountAndZeroNextAmount` | Loyalty API v1 | PASS |
| `TC-T3-065` | `TC_T3_065_ApiV1_GetLoyaltyStatus_IgnoresPendingAndCancelledOrdersInTotalSpent` | Loyalty API v1 | PASS |
| `TC-T3-066` | `TC_T3_066_ApiV1_GetLoyaltyStatus_RespectsManualVipUpgradeInUserLoyaltyTable` | Loyalty API v1 | PASS |
| `TC-T3-067` | `TC_T3_067_ApiV1_LoyaltyTierBoundaries_MatchSpecification` (7 boundary values) | Loyalty API v1 | PASS |
| `TC-T3-068` | `TC_T3_068_ApiV1_DiscoveryBox_ValidatesClassicWoodWith3Vials2ml` | Discovery Box API v1 | PASS |
| `TC-T3-069` | `TC_T3_069_ApiV1_DiscoveryBox_ValidatesRoyalVelvetWith4Vials5mlAnd5PercentDiscount` | Discovery Box API v1 | PASS |
| `TC-T3-070` | `TC_T3_070_ApiV1_DiscoveryBox_ValidatesArtisanLeatherWith5VialsAnd10PercentDiscount` | Discovery Box API v1 | PASS |
| `TC-T3-071` | `TC_T3_071_ApiV1_DiscoveryBox_Returns400WhenFewerThan3Vials` (3 counts) | Discovery Box API v1 | PASS |
| `TC-T3-072` | `TC_T3_072_ApiV1_DiscoveryBox_Returns400WhenMoreThan5Vials` (2 counts) | Discovery Box API v1 | PASS |
| `TC-T3-073` | `TC_T3_073_ApiV1_DiscoveryBox_Returns400ForUnsupportedBoxStyle` (2 styles) | Discovery Box API v1 | PASS |
| `TC-T3-074` | `TC_T3_074_ApiV1_DiscoveryBox_Returns400ForUnsupportedVialSize` (3 sizes) | Discovery Box API v1 | PASS |
| `TC-T3-075` | `TC_T3_075_ApiV1_DiscoveryBox_ValidatesGreetingMessageMax250Characters` | Discovery Box API v1 | PASS |
| `TC-T3-076` | `TC_T3_076_ApiV1_DiscoveryBox_Returns400WhenProductDoesNotExistInSeededCatalog` | Discovery Box API v1 | PASS |
| `TC-T3-077` | `TC_T3_077_ApiV1_TrackOrder_Returns400WhenOrderCodeIsEmpty` (3 inputs) | Order Tracking API v1 | PASS |
| `TC-T3-078` | `TC_T3_078_ApiV1_TrackOrder_Returns404WhenOrderCodeNotFound` | Order Tracking API v1 | PASS |
| `TC-T3-079` | `TC_T3_079_ApiV1_TrackOrder_Returns200OkWithItemsGiftWrappingAndPaymentInfo` | Order Tracking API v1 | PASS |
| `TC-T3-080` | `TC_T3_080_ApiV1_TrackOrder_TimelineMarksAllFourStagesCompletedWhenDelivered` | Order Tracking API v1 | PASS |
| `TC-T3-081` | `TC_T3_081_ApiV1_TrackOrder_TimelineAppendsCancelledStageWhenOrderCancelled` | Order Tracking API v1 | PASS |
| `TC-T3-082` | `TC_T3_082_ApiV1_TrackOrder_VerifiesOptionalPhoneParameter` | Order Tracking API v1 | PASS |
| `TC-T3-083` | `TC_T3_083_ApiV1_CreateTastingBooking_Returns201CreatedAndPersistsInDatabase` | Tasting Booking API v1 | PASS |
| `TC-T3-084` | `TC_T3_084_ApiV1_CreateTastingBooking_Returns400WhenNameOrPhoneMissing` (3 inputs) | Tasting Booking API v1 | PASS |
| `TC-T3-085` | `TC_T3_085_ApiV1_CreateTastingBooking_Returns400WhenPhoneHasTooFewDigits` | Tasting Booking API v1 | PASS |
| `TC-T3-086` | `TC_T3_086_ApiV1_CreateTastingBooking_Returns400WhenGuestCountOutside1To6` (4 counts) | Tasting Booking API v1 | PASS |
| `TC-T3-087` | `TC_T3_087_ApiV1_CreateTastingBooking_Returns400WhenBookingDateIsInPast` | Tasting Booking API v1 | PASS |
| `TC-T3-088` | `TC_T3_088_ApiV1_CreateTastingBooking_AppliesDefaultBoutiqueLocationAndTimeSlot` | Tasting Booking API v1 | PASS |
| `TC-T3-089` | `TC_T3_089_SwaggerJson_ReturnsValidOpenApi301Specification` | Swagger / OpenAPI | PASS |
| `TC-T3-090` | `TC_T3_090_SwaggerJson_ContainsAllRequiredApiV1Paths` (7 paths) | Swagger / OpenAPI | PASS |
| `TC-T3-091` | `TC_T3_091_SwaggerJson_DefinesJwtBearerSecurityScheme` | Swagger / OpenAPI | PASS |
| `TC-T3-092` | `TC_T3_092_SwaggerUi_ReturnsInteractiveHtmlWithSwaggerUiBundleAndTryItOut` | Swagger / OpenAPI | PASS |
| `TC-T3-093` | `TC_T3_093_JwtTokenService_CreatesAndValidatesTokenWithNameIdentifierEmailAndRoles` | JWT Security | PASS |
| `TC-T3-094` | `TC_T3_094_JwtTokenService_RejectsTamperedAndMalformedTokens` | JWT Security | PASS |
| `TC-T3-095` | `TC_T3_095_JwtTokenService_ThrowsWhenSigningKeyMissingOrTooShortInProduction` | JWT Security | PASS |
| `TC-T3-096` | `TC_T3_096_CustomerApiController_ProductsAndProductBySlug_ReturnExpectedResults` | Customer API v1 | PASS |
| `TC-T3-097` | `TC_T3_097_CustomerApiController_ValidateVoucher_ComputesCappedDiscountOnCart` | Customer API v1 | PASS |
| `TC-T3-098` | `TC_T3_098_CustomerApiController_ProfileAndMyOrders_ReturnAuthenticatedCustomerData` | Customer API v1 | PASS |
| `TC-T3-099` | `TC_T3_099_CustomerApiController_ProductReviews_ReturnsOnlyApprovedReviewsAndComputesAverage` | Customer API v1 | PASS |
| `TC-T3-100` | `TC_T3_100_CustomerApiController_RevokeToken_RevokesRefreshTokenAndReturnsNoContent` | Customer API v1 | PASS |

---

## 3. Technical Observations & Notes

- **Observation in `AdminDashboardController.CreateFlashSale` (Line 888)**: When `flashSalePrices[i] <= 0`, `AdminDashboardController` calls `Math.Round(prod.BasePrice * 0.8m, -4)`, which throws `ArgumentOutOfRangeException` in .NET because `Decimal.Round` only accepts `decimals` in `[0, 28]`. Passing an explicit positive `flashSalePrices[i]` works as expected.
- **Ambiguity Prevention**: Since `DiscoveryVialSelectionDto` also exists in `Perfume.Application.DTOs`, `ApiV1ExtensionsController` defines `DiscoveryVialApiSelectionDto` to avoid namespace ambiguity while maintaining identical JSON property serialization (`productId`, `vialSize`).
