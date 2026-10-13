# BÁO CÁO KIỂM THỬ TOÀN DIỆN — TEAM 1 (STOREFRONT UI, CATALOG, FLASH SALE, AI SCENT QUIZ, BLOG, CART, CHECKOUT & VIETQR)

- **Đội phụ trách**: **TEAM 1 — Storefront, Catalog, Flash Sale UI & Commerce/Checkout/VietQR TDD Squad**
- **Phương pháp luận**: `/enterprise`, `/spec-driven-development`, `/test-driven-development`
- **File kiểm thử thực thi**: `tests/Perfume.Tests/Team1_StorefrontAndCommerceTests.cs`
- **Cơ sở dữ liệu kiểm thử**: EF Core `InMemoryDatabase` cô lập hoàn toàn theo từng kịch bản (`Team1Db_{Guid.NewGuid():N}`)
- **Tổng số kịch bản kiểm thử**: **100 / 100 (`TC-T1-001` → `TC-T1-100`)**
- **Kết quả tổng thể**: **100 PASSED / 0 FAILED / 0 SKIPPED (Tỷ lệ đạt: 100%)**

---

## 1. Tổng hợp Metrics theo Phân hệ (Executive Summary)

| Phần | Phân hệ kiểm thử (Module Scope) | Mã kịch bản | Số lượng Test | Passed | Failed | Tỷ lệ Pass |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: |
| **Phần 1** | Storefront Home, Catalog, Product Detail, Scent Pyramid, Variants, Gallery | `TC-T1-001` → `TC-T1-012` | 12 | 12 | 0 | **100%** |
| **Phần 1** | Flash Sale Engine (`FlashSaleService` & Catalog Badge Enrichment) | `TC-T1-013` → `TC-T1-023` | 11 | 11 | 0 | **100%** |
| **Phần 1** | Advanced Catalog Filters (`Keyword`, `Brand`, `Category`, `Family`, `Note`, `Gender`, `Price`, `Sort`, `Paging`) | `TC-T1-024` → `TC-T1-035` | 12 | 12 | 0 | **100%** |
| **Phần 1** | AI Scent Quiz (`GetQuizRecommendationsAsync`, 5 Archetypes, Match Score 85%–99%) | `TC-T1-036` → `TC-T1-044` | 9 | 9 | 0 | **100%** |
| **Phần 1** | Editorial Journal (`BlogService`: Published, Draft, Vietnamese Slug, Admin CRUD) | `TC-T1-045` → `TC-T1-050` | 6 | 6 | 0 | **100%** |
| **Phần 2** | Shopping Cart (`CustomerCartService`: Guest SHA-256, User Cart, Stock Clamping, Merge, Clear) | `TC-T1-051` → `TC-T1-064` | 14 | 14 | 0 | **100%** |
| **Phần 2** | Checkout, Shipping Rules, Voucher Engine, Royal Gift Wrapping, VietQR & Stock Deduction (`OrderService`) | `TC-T1-065` → `TC-T1-084` | 20 | 20 | 0 | **100%** |
| **Phần 2** | Storefront, Experience, Order Tracking & SEO Routes (`/`, `/bo-suu-tap`, `/Catalog/Quiz`, `/discovery-box`, `/so-sanh`, `/Blog`, `/Cart`, `/Checkout`, `/tra-cuu-don-hang`, `/sitemap.xml`, `/robots.txt`, ...) | `TC-T1-085` → `TC-T1-100` | 16 | 16 | 0 | **100%** |
| **TỔNG** | **Toàn bộ 2 Phần (Storefront UI + Commerce/Checkout)** | **`TC-T1-001` → `TC-T1-100`** | **100** | **100** | **0** | **100%** |

---

## 2. Các Cải tiến Kỹ thuật & Bug Fix trong Phạm vi Team 1

1. **`PerfumeCatalogService.MapToProductDetailDtoAsync` (`src/Perfume.Infrastructure/Services/PerfumeCatalogService.cs`)**:
   - Đảm bảo `Variants` luôn được sắp xếp tăng dần theo `VolumeMl` (`OrderBy(v => v.VolumeMl)`) và `GalleryImages` luôn được sắp xếp theo `DisplayOrder` (`OrderBy(i => i.DisplayOrder)`) ngay cả khi thực thể `Product` đã được EF Core ChangeTracker theo dõi trước đó.
2. **`OrderService.PlaceOrderAsync` (`src/Perfume.Infrastructure/Services/OrderService.cs`)**:
   - Giữ nguyên giao dịch `IsolationLevel.Serializable` và `ExecuteUpdateAsync` nguyên tử trên SQL Server (`_context.Database.IsRelational() == true`), đồng thời bổ sung nhánh tương thích cho Non-Relational Provider (`InMemoryDatabase`) để kiểm thử tự động toàn diện luồng đặt hàng, trừ tồn kho biến thể và tăng lượt dùng Voucher.

---

## 3. Danh sách Chi tiết 100 Kịch bản Kiểm thử (`TC-T1-001` → `TC-T1-100`)

### Phần 1: `part-1-storefront-ui` (`TC-T1-001` → `TC-T1-050`)

| Mã TC | Tên Method Kiểm thử xUnit | Mô tả Kịch bản & Kỳ vọng (Assertions) | Trạng thái |
| :---: | :--- | :--- | :---: |
| `TC-T1-001` | `TC_T1_001_GetFeaturedProductsAsync_ReturnsOnlyFeaturedCappedAt8` | Chỉ trả về sản phẩm `IsFeatured = true`, giới hạn tối đa 8 sản phẩm trên trang chủ. | **PASS** |
| `TC-T1-002` | `TC_T1_002_GetBestSellersAsync_ReturnsOnlyBestSellersCappedAt8` | Chỉ trả về sản phẩm `IsBestSeller = true`, giới hạn tối đa 8 sản phẩm bán chạy. | **PASS** |
| `TC-T1-003` | `TC_T1_003_GetNewArrivalsAsync_ReturnsOnlyNewArrivalsCappedAt8` | Chỉ trả về sản phẩm `IsNewArrival = true`, giới hạn tối đa 8 sản phẩm mới về. | **PASS** |
| `TC-T1-004` | `TC_T1_004_GetProductBySlugAsync_ReturnsCompleteProductDetailMetadata` | Trả về đầy đủ `ProductDetailDto`: thương hiệu, xuất xứ, danh mục, nhóm hương, màu hex, nồng độ, độ lưu/tỏa hương, năm phát hành, SEO meta. | **PASS** |
| `TC-T1-005` | `TC_T1_005_GetProductBySlugAsync_ReturnsNullForUnknownOrSoftDeletedProduct` | Trả về `null` khi slug không tồn tại hoặc sản phẩm đã bị xóa mềm (`IsDeleted = true`). | **PASS** |
| `TC-T1-006` | `TC_T1_006_GetProductByIdAsync_ReturnsDetailForValidIdAndNullForMissing` | Truy xuất chính xác chi tiết sản phẩm theo `Id` và trả về `null` nếu `Id` không tồn tại. | **PASS** |
| `TC-T1-007` | `TC_T1_007_GetProductBySlugAsync_MapsThreeTierScentPyramidCorrectly` | Phân tách chính xác tháp hương 3 tầng (`TopNotes`, `HeartNotes`, `BaseNotes`) theo `NoteLevel`. | **PASS** |
| `TC-T1-008` | `TC_T1_008_GetProductBySlugAsync_OrdersVariantsByVolumeMlAscending` | Sắp xếp danh sách `Variants` tăng dần theo dung tích `VolumeMl` (10ml → 50ml → 100ml) và bảo toàn cờ `IsDefault`. | **PASS** |
| `TC-T1-009` | `TC_T1_009_GetProductBySlugAsync_OrdersGalleryImagesByDisplayOrder` | Sắp xếp bộ sưu tập ảnh `GalleryImages` tăng dần theo `DisplayOrder` (1 → 2 → 3). | **PASS** |
| `TC-T1-010` | `TC_T1_010_GetBrandsAndCategoriesAsync_ReturnsAlphabeticalAndExcludesDeleted` | `GetBrandsAsync` và `GetCategoriesAsync` sắp xếp theo bảng chữ cái A–Z và loại bỏ bản ghi `IsDeleted = true`. | **PASS** |
| `TC-T1-011` | `TC_T1_011_GetFragranceFamiliesAndNotesAsync_ReturnsAlphabeticalActiveEntities` | `GetFragranceFamiliesAsync` và `GetFragranceNotesAsync` trả về danh sách A–Z kèm `ScentType` và `ColorHex`. | **PASS** |
| `TC-T1-012` | `TC_T1_012_ProductCardDto_MapsTop3TopNotesOnly` | Thẻ sản phẩm (`ProductCardDto`) trích xuất tối đa 3 nốt hương đầu (`NoteLevel.Top`), không lẫn nốt hương giữa/cuối. | **PASS** |
| `TC-T1-013` | `TC_T1_013_FlashSale_GetActiveFlashSaleAsync_ReturnsOngoingCampaignWithCountdown` | Trả về chiến dịch Flash Sale đang diễn ra (`IsOngoing = true`), thời gian đếm ngược $> 0$ và tính đúng `RemainingQuantity`, `DiscountPercent`. | **PASS** |
| `TC-T1-014` | `TC_T1_014_FlashSale_MultipleActiveCampaigns_SelectsEarliestEnding` | Khi có nhiều chiến dịch Flash Sale cùng hoạt động, ưu tiên chọn chiến dịch có `EndTime` kết thúc sớm nhất. | **PASS** |
| `TC-T1-015` | `TC_T1_015_FlashSaleItemDto_DiscountRoundingAndZeroPriceSafety` | Làm tròn phần trăm giảm giá (`Math.Round`) chính xác (23.53% → 24%) và chống lỗi chia cho 0 khi `OriginalPrice <= 0`. | **PASS** |
| `TC-T1-016` | `TC_T1_016_FlashSale_EnrichesProductCardAndProductDetailBadges` | Làm giàu thông tin `IsFlashSale = true`, `FlashSalePrice`, `FlashSaleDiscountPercent`, `FlashSaleEndTime` trên cả Card và trang Chi tiết. | **PASS** |
| `TC-T1-017` | `TC_T1_017_FlashSale_ExpiredCampaign_FallsBackToBasePrice` | Khi chiến dịch hết hạn (`EndTime <= UtcNow`), tự động ẩn banner và khôi phục sản phẩm về giá gốc `BasePrice`. | **PASS** |
| `TC-T1-018` | `TC_T1_018_FlashSale_FutureStartTime_IsNotActiveYet` | Chiến dịch có `StartTime > UtcNow` chưa kích hoạt và không áp giá Flash Sale lên danh mục. | **PASS** |
| `TC-T1-019` | `TC_T1_019_FlashSale_ToggleStatusAsync_DisablesAndReEnablesPromotion` | `ToggleStatusAsync` tắt/bật trạng thái `IsActive` tức thời trên Storefront và trang chi tiết sản phẩm. | **PASS** |
| `TC-T1-020` | `TC_T1_020_FlashSale_DeleteAsync_SoftDeletesAndExcludesCampaign` | `DeleteAsync` đánh dấu `IsDeleted = true`, Global Query Filter tự động loại bỏ khỏi Storefront và danh sách. | **PASS** |
| `TC-T1-021` | `TC_T1_021_FlashSale_StockExhausted_SoldQuantityEqualsStockLimit_RevertsToBasePrice` | Khi `SoldQuantity >= StockLimit` (cháy hàng suất Flash Sale), tự động ngắt giá Flash Sale và quay về `BasePrice`. | **PASS** |
| `TC-T1-022` | `TC_T1_022_FlashSale_CreateAndGetByIdAsync_PersistsCampaignAndItems` | Tạo mới chiến dịch Flash Sale kèm danh sách sản phẩm với `SoldQuantity = 0` và truy xuất qua `GetFlashSaleByIdAsync`. | **PASS** |
| `TC-T1-023` | `TC_T1_023_FlashSale_GetActiveFlashSaleItemForProductAsync_ResolvesVariantOriginalPrice` | Lấy thông tin Flash Sale theo `productId` và `variantId`, ưu tiên `Variant.OriginalPrice` khi tính phần trăm giảm. | **PASS** |
| `TC-T1-024` | `TC_T1_024_CatalogFilter_KeywordSearch_MatchesNameBrandAndShortDescription` | Tìm kiếm từ khóa (`Keyword`) khớp trên cả `Product.Name`, `Brand.Name` và `ShortDescription` (tự động `Trim()`). | **PASS** |
| `TC-T1-025` | `TC_T1_025_CatalogFilter_BrandSlug_FiltersExactBrand` | Lọc chính xác sản phẩm theo `BrandSlug`. | **PASS** |
| `TC-T1-026` | `TC_T1_026_CatalogFilter_CategorySlug_FiltersExactCategory` | Lọc chính xác sản phẩm theo `CategorySlug`. | **PASS** |
| `TC-T1-027` | `TC_T1_027_CatalogFilter_FamilySlug_FiltersExactFragranceFamily` | Lọc chính xác sản phẩm theo nhóm hương `FamilySlug`. | **PASS** |
| `TC-T1-028` | `TC_T1_028_CatalogFilter_NoteId_ReturnsOnlyProductsContainingNote` | Lọc chính xác các sản phẩm có chứa nốt hương `NoteId` trong tháp hương. | **PASS** |
| `TC-T1-029` | `TC_T1_029_CatalogFilter_GenderMen_IncludesMenAndUnisexExcludesWomen` | Lọc `Gender = Men` bao gồm cả sản phẩm `Men` và `Unisex`, loại trừ `Women`. | **PASS** |
| `TC-T1-030` | `TC_T1_030_CatalogFilter_GenderWomen_IncludesWomenAndUnisexExcludesMen` | Lọc `Gender = Women` bao gồm cả sản phẩm `Women` và `Unisex`, loại trừ `Men`. | **PASS** |
| `TC-T1-031` | `TC_T1_031_CatalogFilter_Concentration_FiltersExactConcentration` | Lọc chính xác theo nồng độ tinh dầu (`Concentration.Parfum`, `EDP`, `EDT`...). | **PASS** |
| `TC-T1-032` | `TC_T1_032_CatalogFilter_MinAndMaxPriceBoundaries_InclusiveFiltering` | Lọc theo khoảng giá biên `MinPrice <= BasePrice <= MaxPrice` (bao gồm cả giá trị tại biên). | **PASS** |
| `TC-T1-033` | `TC_T1_033_CatalogFilter_SortByPriceAscAndPriceDesc_OrdersAccurately` | Sắp xếp `price_asc` (giá tăng dần) và `price_desc` (giá giảm dần) chính xác. | **PASS** |
| `TC-T1-034` | `TC_T1_034_CatalogFilter_SortByRatingAndNewestDefault_OrdersAccurately` | Sắp xếp `rating` theo điểm đánh giá giảm dần và mặc định theo `CreatedAt` mới nhất. | **PASS** |
| `TC-T1-035` | `TC_T1_035_CatalogFilter_PaginationAndNegativePageClamping` | Phân trang (`Page`, `PageSize`) trả về đúng `TotalCount` và tự động kẹp `Page <= 0` về trang 1. | **PASS** |
| `TC-T1-036` | `TC_T1_036_ScentQuiz_Archetype_TheSovereignAristocrat` | AI Scent Quiz: `men` + `luxury`/`evening` sinh hồ sơ `"The Sovereign Aristocrat"` (Bậc Đế Vương Quyền Uy). | **PASS** |
| `TC-T1-037` | `TC_T1_037_ScentQuiz_Archetype_TheVelvetEnchantress` | AI Scent Quiz: `women` + `romantic`/`evening` sinh hồ sơ `"The Velvet Enchantress"` (Nàng Thơ Mê Hoặc). | **PASS** |
| `TC-T1-038` | `TC_T1_038_ScentQuiz_Archetype_TheEtherealLuminary` | AI Scent Quiz: `fresh` hoặc `daily` sinh hồ sơ `"The Ethereal Luminary"` (Ánh Sáng Tinh Khôi). | **PASS** |
| `TC-T1-039` | `TC_T1_039_ScentQuiz_Archetype_TheMysticAlchemist` | AI Scent Quiz: `unisex` + `cozy`/`mysterious` sinh hồ sơ `"The Mystic Alchemist"` (Nhà Giả Kim Bí Ẩn). | **PASS** |
| `TC-T1-040` | `TC_T1_040_ScentQuiz_Archetype_TheAvantGardeConnoisseur` | AI Scent Quiz: `unisex` + `luxury` + `all` sinh hồ sơ `"The Avant-Garde Connoisseur"` (Nhà Giám Tuyển Độc Bản). | **PASS** |
| `TC-T1-041` | `TC_T1_041_ScentQuiz_MatchScoreNormalization_Top4Within85To99Percent` | Chuẩn hóa điểm tương thích Top 4 sản phẩm trong dải 85%–99% theo thứ tự giảm dần nghiêm ngặt. | **PASS** |
| `TC-T1-042` | `TC_T1_042_ScentQuiz_MatchReasons_CoverAllVibesAndIncludeTopNotes` | Sinh lời giải thích `MatchReason` cá nhân hóa cho cả 5 sắc thái (`luxury`, `cozy`, `fresh`, `romantic`, `mysterious`) kèm nốt hương đầu. | **PASS** |
| `TC-T1-043` | `TC_T1_043_ScentQuiz_ScoringMatrix_WeightsBudgetIntensityAndOccasion` | Ma trận chấm điểm đa chiều ưu tiên đúng sản phẩm theo hoàn cảnh (`daily`/`evening`), cường độ (`moderate`/`intense`) và ngân sách (`sample`/`high`). | **PASS** |
| `TC-T1-044` | `TC_T1_044_ScentQuiz_EmptyDatabase_ReturnsGracefulDefaultArchetype` | Khi kho sản phẩm trống, trả về hồ sơ mặc định `"The Ethereal Luminary"` an toàn không lỗi NRE. | **PASS** |
| `TC-T1-045` | `TC_T1_045_BlogService_GetRecentPostsAsync_ReturnsOnlyPublishedAndExcludesDrafts` | Chỉ lấy bài viết `IsPublished = true` và chưa xóa, loại bỏ bản nháp (`IsPublished = false`). | **PASS** |
| `TC-T1-046` | `TC_T1_046_BlogService_GetPostsPagedAsync_FiltersByCategorySlugAndPaginates` | Lọc bài viết tạp chí theo `categorySlug` và phân trang chính xác. | **PASS** |
| `TC-T1-047` | `TC_T1_047_BlogService_GetPostBySlugAsync_IncrementsViewCountAndRejectsDraft` | Xem chi tiết bài viết tự động cộng `ViewCount += 1` và từ chối truy cập bài viết nháp. | **PASS** |
| `TC-T1-048` | `TC_T1_048_BlogService_CreatePostAsync_GeneratesVietnameseSlugAutomatically` | Tự động chuyển đổi tiêu đề tiếng Việt có dấu sang URL slug chuẩn SEO không dấu khi tạo bài viết. | **PASS** |
| `TC-T1-049` | `TC_T1_049_BlogService_UpdatePostAsync_UpdatesFieldsAndHandlesMissingPost` | Cập nhật bài viết, tự sinh lại slug nếu để trống và trả về `false` nếu ID không tồn tại. | **PASS** |
| `TC-T1-050` | `TC_T1_050_BlogService_DeletePostAsync_SoftDeletesAndExcludesFromAdminAndCategories` | Xóa mềm bài viết (`IsDeleted = true`), ẩn khỏi danh sách Admin và danh mục Blog. | **PASS** |

---

### Phần 2: `part-2-commerce-checkout` (`TC-T1-051` → `TC-T1-100`)

| Mã TC | Tên Method Kiểm thử xUnit | Mô tả Kịch bản & Kỳ vọng (Assertions) | Trạng thái |
| :---: | :--- | :--- | :---: |
| `TC-T1-051` | `TC_T1_051_CustomerCart_GetAsync_WithoutOwnerKeyReturnsEmptyAndGuestCartHashesToken` | Giỏ hàng khách vãng lai băm `guestToken` bằng SHA-256 (`guest:...`), không lưu token thô trong DB. | **PASS** |
| `TC-T1-052` | `TC_T1_052_CustomerCart_AddAsync_AuthenticatedUser_CreatesUserCartAndCalculatesSubTotal` | Tạo giỏ hàng cho tài khoản đăng nhập (`user:{userId}`) và tính tổng tiền `SubTotal`, `TotalCount`. | **PASS** |
| `TC-T1-053` | `TC_T1_053_CustomerCart_AddAsync_ExplicitVariantId_UsesSalePriceWhenPositive` | Chọn đúng biến thể theo `VariantId`; ưu tiên lấy `SalePrice` khi $> 0$, ngược lại lấy `OriginalPrice`. | **PASS** |
| `TC-T1-054` | `TC_T1_054_CustomerCart_AddAsync_WithoutVariantId_SelectsDefaultVariantAutomatically` | Khi không truyền `VariantId`, tự động chọn biến thể mặc định (`IsDefault = true`). | **PASS** |
| `TC-T1-055` | `TC_T1_055_CustomerCart_AddAsync_ProductWithoutVariants_UsesBasePriceAndFullboxLabel` | Sản phẩm không chia biến thể dùng `BasePrice`, nhãn `"Fullbox"` và trần tồn kho an toàn 50. | **PASS** |
| `TC-T1-056` | `TC_T1_056_CustomerCart_AddAsync_ExistingItem_IncrementsQuantityWithoutDuplicatingLine` | Thêm trùng sản phẩm + biến thể đã có trong giỏ sẽ cộng dồn `Quantity` thay vì tạo dòng mới. | **PASS** |
| `TC-T1-057` | `TC_T1_057_CustomerCart_AddAsync_QuantityOutOfBoundsOrExceedingStock_ThrowsInvalidOperationException` | Chặn số lượng $< 1$, $> 50$ hoặc tổng số lượng vượt quá tồn kho khả dụng (`StockQuantity`). | **PASS** |
| `TC-T1-058` | `TC_T1_058_CustomerCart_AddAsync_OutOfStockProductOrZeroStockVariant_RejectsAddition` | Từ chối thêm sản phẩm ngừng bán (`IsInStock = false`) hoặc biến thể hết hàng (`StockQuantity <= 0`). | **PASS** |
| `TC-T1-059` | `TC_T1_059_CustomerCart_AddAsync_InvalidVariantId_ThrowsKeyNotFoundException` | Ném `KeyNotFoundException` khi truyền `VariantId` không thuộc sản phẩm. | **PASS** |
| `TC-T1-060` | `TC_T1_060_CustomerCart_SetQuantityAsync_UpdatesQuantityAndValidatesStockLimit` | Cập nhật số lượng dòng giỏ hàng, tính lại `SubTotal` và chặn vượt tồn kho biến thể. | **PASS** |
| `TC-T1-061` | `TC_T1_061_CustomerCart_SetQuantityAsync_ZeroQuantity_RemovesLineItem` | Đặt `Quantity = 0` qua `SetQuantityAsync` tự động xóa sản phẩm khỏi giỏ hàng. | **PASS** |
| `TC-T1-062` | `TC_T1_062_CustomerCart_RemoveAsync_RemovesTargetVariantLineOnly` | Xóa đúng biến thể được chỉ định mà không ảnh hưởng đến biến thể dung tích khác của cùng sản phẩm. | **PASS** |
| `TC-T1-063` | `TC_T1_063_CustomerCart_MergeGuestCartAsync_CombinesQuantitiesClampsToStockAndClearsGuest` | Hợp nhất giỏ hàng Guest vào tài khoản khi đăng nhập, cộng dồn và kẹp trần theo `Math.Min(stock, 50)`, xóa giỏ Guest. | **PASS** |
| `TC-T1-064` | `TC_T1_064_CustomerCart_ClearAsync_EmptiesAllItems` | `ClearAsync` làm trống toàn bộ giỏ hàng, đưa `SubTotal = 0` và `TotalCount = 0`. | **PASS** |
| `TC-T1-065` | `TC_T1_065_OrderService_PlaceOrderAsync_CalculatesSubTotalAcrossMultipleVariants` | Tính chính xác `SubTotal` trên nhiều biến thể có cả `SalePrice` và `OriginalPrice`. | **PASS** |
| `TC-T1-066` | `TC_T1_066_OrderService_ShippingFee_Applies35000WhenSubTotalUnderThreshold` | Áp phí vận chuyển tiêu chuẩn `35,000₫` cho đơn hàng dưới ngưỡng miễn phí vận chuyển. | **PASS** |
| `TC-T1-067` | `TC_T1_067_OrderService_ShippingFee_FreeShippingWhenSubTotalMeetsThreshold` | Miễn phí vận chuyển (`ShippingFee = 0₫`) cho đơn hàng đạt ngưỡng cao cấp. | **PASS** |
| `TC-T1-068` | `TC_T1_068_OrderService_PercentageVoucher_CalculatesDiscountAndIncrementsUsedCount` | Áp mã giảm giá theo `%` (`DiscountPercent`), trừ đúng số tiền và tăng `UsedCount` thêm 1. | **PASS** |
| `TC-T1-069` | `TC_T1_069_OrderService_FixedAmountVoucher_AppliesExactFixedDiscount` | Áp mã giảm giá số tiền cố định (`DiscountAmount`) và tính đúng `TotalAmount`. | **PASS** |
| `TC-T1-070` | `TC_T1_070_OrderService_VoucherWithMaxDiscountAmount_CapsDiscountAtMaxLimit` | Giới hạn mức giảm tối đa theo `MaxDiscountAmount` đối với voucher phần trăm. | **PASS** |
| `TC-T1-071` | `TC_T1_071_OrderService_ValidateVoucherAsync_RejectsBelowMinOrderAmount` | Từ chối áp dụng voucher khi giá trị đơn hàng `currentSubTotal < MinOrderAmount`. | **PASS** |
| `TC-T1-072` | `TC_T1_072_OrderService_ValidateVoucherAsync_RejectsExpiredAndFutureVouchers` | Từ chối voucher đã hết hạn (`ValidTo < UtcNow`) hoặc chưa đến ngày hiệu lực (`ValidFrom > UtcNow`). | **PASS** |
| `TC-T1-073` | `TC_T1_073_OrderService_ValidateVoucherAsync_RejectsInactiveAndSoftDeletedVouchers` | Từ chối voucher bị vô hiệu hóa (`IsActive = false`) hoặc đã xóa mềm (`IsDeleted = true`). | **PASS** |
| `TC-T1-074` | `TC_T1_074_OrderService_ValidateVoucherAsync_RejectsWhenUsageLimitExhausted` | Từ chối voucher đã hết lượt sử dụng (`UsedCount >= UsageLimit`). | **PASS** |
| `TC-T1-075` | `TC_T1_075_OrderService_ValidateVoucherAsync_CaseInsensitiveAndTrimsWhitespace` | Kiểm tra mã voucher không phân biệt hoa/thường và tự động cắt khoảng trắng thừa (`"  luxe2026  "`). | **PASS** |
| `TC-T1-076` | `TC_T1_076_OrderService_RoyalGiftWrappingTrue_Adds50000FeeAndPersistsGiftMessage` | Gói quà Hoàng Gia (`IsGiftWrapping = true`) cộng `+50,000₫` vào tổng tiền và lưu thông điệp thiệp chúc. | **PASS** |
| `TC-T1-077` | `TC_T1_077_OrderService_RoyalGiftWrappingFalse_AddsZeroFee` | Khi `IsGiftWrapping = false`, `GiftWrappingFee = 0₫`. | **PASS** |
| `TC-T1-078` | `TC_T1_078_OrderService_PaymentMethods_CODAndBankTransfer_GenerateUniqueOrderCodes` | Hỗ trợ `COD` và `BankTransfer` (VietQR), sinh mã đơn hàng duy nhất `LUXE-yyyyMMdd-...`. | **PASS** |
| `TC-T1-079` | `TC_T1_079_OrderService_UnsupportedPaymentMethods_VNPAYAndMoMo_ReturnControlledError` | Trả về thông báo lỗi kiểm soát khi gọi trực tiếp cổng thanh toán chưa bật (`VNPAY`, `MoMo`). | **PASS** |
| `TC-T1-080` | `TC_T1_080_OrderService_StockDeduction_DecrementsVariantStockAndRejectsInsufficientStock` | Khấu trừ chính xác tồn kho `StockQuantity` của biến thể khi đặt hàng và chặn đặt vượt tồn kho còn lại. | **PASS** |
| `TC-T1-081` | `TC_T1_081_OrderService_Validation_RejectsMissingCustomerInfoEmptyCartAndOver50Quantity` | Chặn đặt hàng khi thiếu thông tin người nhận, giỏ hàng rỗng hoặc tổng số lượng 1 biến thể $> 50$. | **PASS** |
| `TC-T1-082` | `TC_T1_082_OrderService_ConfirmPaymentAsync_UpdatesPaymentStatusToCompleted` | `ConfirmPaymentAsync` cập nhật `PaymentStatus = Completed` khi xác nhận chuyển khoản VietQR. | **PASS** |
| `TC-T1-083` | `TC_T1_083_OrderService_GetOrderByCodeAndStatusLifecycle_TransitionsAndRestoresStockOnCancel` | Truy vấn đơn hàng kèm `Items`, chuyển đổi trạng thái đơn hàng và hoàn trả tồn kho khi hủy đơn (`Cancelled`). | **PASS** |
| `TC-T1-084` | `TC_T1_084_CheckoutController_VietQRAndConfirmBankTransfer_GeneratesQrUrlAndConfirmsGuestOrder` | Trang VietQR Napas247 mã hóa/giải mã token khách vãng lai, tạo URL QR `img.vietqr.io` đúng số tiền & nội dung, và xác nhận thanh toán. | **PASS** |
| `TC-T1-085` | `TC_T1_085_StorefrontRoute_HomeIndex_LoadsFlashSaleHeroBannersAndCollections` | Route `/` (`HomeController.Index`) nạp đầy đủ Flash Sale, Hero Banners, Featured, BestSellers, NewArrivals, Brands, Families, Blogs. | **PASS** |
| `TC-T1-086` | `TC_T1_086_StorefrontRoute_CatalogIndex_BoSuuTap_RendersFilteredCatalogAndFacetLists` | Route `/bo-suu-tap` (`CatalogController.Index`) trả về danh sách sản phẩm và bộ lọc Facet. | **PASS** |
| `TC-T1-087` | `TC_T1_087_StorefrontRoute_CatalogQuizAndQuizSubmit_ReturnsViewAndJsonRecommendations` | Route `/Catalog/Quiz` và `POST /Catalog/QuizSubmit` hiển thị giao diện trắc nghiệm và trả về JSON gợi ý mùi hương. | **PASS** |
| `TC-T1-088` | `TC_T1_088_StorefrontRoute_DiscoveryBox_ReturnsInStockPerfumesOrderedByFeatured` | Route `/discovery-box` (`/hop-kham-pha`) hiển thị danh sách nước hoa còn hàng để chọn hộp khám phá 5 ống. | **PASS** |
| `TC-T1-089` | `TC_T1_089_StorefrontRoute_Compare_SoSanh_ComparesSelectedProductsOrDefaultsToFirstTwo` | Route `/so-sanh` (`/catalog/compare`) đặt bàn cân so sánh trực diện 2–3 tuyệt tác nước hoa. | **PASS** |
| `TC-T1-090` | `TC_T1_090_StorefrontRoute_BlogIndexAndDetail_RendersHeroPostAndRelatedPosts` | Route `/Blog` và `/blog/{slug}` hiển thị bài viết tiêu điểm (`HeroPost`), chi tiết bài viết và bài liên quan. | **PASS** |
| `TC-T1-091` | `TC_T1_091_StorefrontRoute_CartController_IndexAddUpdateAndRemoveEndpoints` | Route `/Cart` và các API `AddToCart`, `UpdateQuantity`, `RemoveItem` hoạt động đồng bộ, trả về `200 OK`. | **PASS** |
| `TC-T1-092` | `TC_T1_092_StorefrontRoute_CheckoutController_RedirectsWhenCartEmptyAndValidatesVoucherJson` | Route `/Checkout` chuyển hướng về `/Cart` khi giỏ trống và kiểm tra mã giảm giá qua `CheckVoucher` JSON. | **PASS** |
| `TC-T1-093` | `TC_T1_093_StorefrontRoute_OrderTrackingController_MatchesLast6DigitsOfPhoneAndHandlesMismatch` | Route `/tra-cuu-don-hang` (`/order-tracking`) đối chiếu mã đơn hàng + 6 số cuối SĐT nhận hàng và báo lỗi khi sai SĐT. | **PASS** |
| `TC-T1-094` | `TC_T1_094_StorefrontRoute_VeChungToi_RendersAboutView` | Route `/ve-chung-toi` (`HomeController.About`) trả về `ViewResult` trang Câu chuyện thương hiệu. | **PASS** |
| `TC-T1-095` | `TC_T1_095_StorefrontRoute_Boutique_RendersBoutiqueView` | Route `/boutique` (`HomeController.Boutique`) trả về `ViewResult` hệ thống Flagship Boutique. | **PASS** |
| `TC-T1-096` | `TC_T1_096_StorefrontRoute_ChinhSachDoiTra_RendersReturnPolicyView` | Route `/chinh-sach-doi-tra` (`HomeController.ReturnPolicy`) trả về `ViewResult` chính sách đổi trả. | **PASS** |
| `TC-T1-097` | `TC_T1_097_StorefrontRoute_HuongDanMuaHang_RendersBuyingGuideView` | Route `/huong-dan-mua-hang` (`HomeController.BuyingGuide`) trả về `ViewResult` hướng dẫn mua hàng. | **PASS** |
| `TC-T1-098` | `TC_T1_098_StorefrontRoute_PrivacyAndNewsletterSubscribe_IssuesLuxe2026Voucher` | Route `/privacy` và `/api/newsletter/subscribe` hiển thị chính sách bảo mật và cấp mã ưu đãi `LUXE2026`. | **PASS** |
| `TC-T1-099` | `TC_T1_099_StorefrontRoute_SitemapXml_GeneratesValidXmlWithProductAndBlogUrls` | Route `/sitemap.xml` (`SeoController.Sitemap`) xuất XML chuẩn chứa URL tĩnh, sản phẩm và bài viết Blog. | **PASS** |
| `TC-T1-100` | `TC_T1_100_StorefrontRoute_RobotsTxtAndExperienceBooking_VerifiesAeoBotsAndVipTastingBooking` | Route `/robots.txt` cấu hình AEO/GEO (`GPTBot`, `PerplexityBot`, `ClaudeBot`) và `/dat-lich-trai-nghiem` cấp mã đặt lịch `VIP-xxxxx`. | **PASS** |
