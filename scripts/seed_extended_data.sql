-- =========================================================================================
-- PERFUME LUXE - EXTENDED SEED SCRIPT (HAUTE PARFUMERIE CONTENT & DATA EXPANSION)
-- Bổ sung 9 sản phẩm kinh điển, Variants, Kim tự tháp hương, 11 bài Blog, 18 Reviews, 8 Orders
-- =========================================================================================

SET NOCOUNT ON;

-- 1. KIỂM TRA & BỔ SUNG BRAND NẾU CẦN
IF NOT EXISTS (SELECT 1 FROM Brands WHERE Slug = 'ysl')
BEGIN
    INSERT INTO Brands (Name, Slug, Country, Description, LogoUrl, IsFeatured, CreatedAt, IsDeleted)
    VALUES (N'Yves Saint Laurent', 'ysl', N'Pháp', N'Tuyên ngôn tự do và quyến rũ không giới hạn dành cho phái đẹp và quý ông hiện đại.', '/images/brands/ysl.svg', 1, GETUTCDATE(), 0);
END

-- 2. KIỂM TRA & BỔ SUNG FRAGRANCE NOTES MỚI NẾU CẦN
IF NOT EXISTS (SELECT 1 FROM FragranceNotes WHERE Slug = 'anh-dao-den')
BEGIN
    INSERT INTO FragranceNotes (Name, Slug, Description, CreatedAt, IsDeleted)
    VALUES
    (N'Quả Anh Đào Đen (Black Cherry)', 'anh-dao-den', N'Hương anh đào đen căng mọng, ngọt ngào và đượm men rượu say đắm.', GETUTCDATE(), 0),
    (N'Hoa Cam (Orange Blossom)', 'hoa-cam', N'Hương hoa cam Ma-rốc tinh khôi, ngọt thanh và ngập tràn ánh nắng.', GETUTCDATE(), 0),
    (N'Cà Phê Đen (Black Coffee)', 'ca-phe-den', N'Nốt hương cà phê đậm đặc, đánh thức mọi giác quan và quyến rũ bất ngờ.', GETUTCDATE(), 0),
    (N'Hổ Phách Trắng & Aldehyde', 'aldehyde-ho-phach', N'Hợp hương kinh điển của vẻ đẹp quý phái Paris thế kỷ 20.', GETUTCDATE(), 0),
    (N'Trà Xanh & Nho Đen', 'tra-xanh-nho-den', N'Nốt hương băng giá tươi mát lấy cảm hứng từ dòng suối trên dãy Alps.', GETUTCDATE(), 0),
    (N'Xạ Hương Trắng (White Musk)', 'xa-huong-trang', N'Lớp hương ôm sát da thịt trong trẻo như chiếc áo sơ mi trắng tinh khôi.', GETUTCDATE(), 0);
END

-- 3. BỔ SUNG 9 SẢN PHẨM MỚI KINH ĐIỂN
DECLARE @BrandDior INT = (SELECT TOP 1 Id FROM Brands WHERE Slug = 'dior');
DECLARE @BrandChanel INT = (SELECT TOP 1 Id FROM Brands WHERE Slug = 'chanel');
DECLARE @BrandCreed INT = (SELECT TOP 1 Id FROM Brands WHERE Slug = 'creed');
DECLARE @BrandTomFord INT = (SELECT TOP 1 Id FROM Brands WHERE Slug = 'tom-ford');
DECLARE @BrandLeLabo INT = (SELECT TOP 1 Id FROM Brands WHERE Slug = 'le-labo');
DECLARE @BrandMargiela INT = (SELECT TOP 1 Id FROM Brands WHERE Slug = 'maison-margiela');
DECLARE @BrandYsl INT = (SELECT TOP 1 Id FROM Brands WHERE Slug = 'ysl');

DECLARE @CatMen INT = (SELECT TOP 1 Id FROM Categories WHERE Slug = 'nuoc-hoa-nam');
DECLARE @CatWomen INT = (SELECT TOP 1 Id FROM Categories WHERE Slug = 'nuoc-hoa-nu');
DECLARE @CatUnisex INT = (SELECT TOP 1 Id FROM Categories WHERE Slug = 'nuoc-hoa-unisex');

DECLARE @FamWoody INT = (SELECT TOP 1 Id FROM FragranceFamilies WHERE Slug = 'huong-go-woody');
DECLARE @FamFloral INT = (SELECT TOP 1 Id FROM FragranceFamilies WHERE Slug = 'huong-go-floral');
DECLARE @FamOriental INT = (SELECT TOP 1 Id FROM FragranceFamilies WHERE Slug = 'huong-phuong-dong');
DECLARE @FamFresh INT = (SELECT TOP 1 Id FROM FragranceFamilies WHERE Slug = 'huong-tuoi-mat');
DECLARE @FamGourmand INT = (SELECT TOP 1 Id FROM FragranceFamilies WHERE Slug = 'huong-gourmand');

-- 3.1. Le Labo Santal 33
IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = 'le-labo-santal-33')
BEGIN
    INSERT INTO Products (Name, Slug, Sku, ShortDescription, FullDescription, BrandId, CategoryId, FragranceFamilyId, Gender, Concentration, Longevity, Sillage, ReleaseYear, SeasonRecommendation, BasePrice, MainImageUrl, IsFeatured, IsBestSeller, IsNewArrival, IsInStock, RatingAverage, RatingCount, ViewCount, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Le Labo Santal 33 Eau de Parfum',
        'le-labo-santal-33',
        'LL-SAN-33',
        N'Biểu tượng của nước hoa Niche hiện đại với nốt hương gỗ đàn hương và thảo mộc phong trần.',
        N'Le Labo Santal 33 lấy cảm hứng từ hình tượng gã cao bồi miền viễn Tây nước Mỹ bên ngọn lửa trại đêm. Mùi hương là sự hòa quyện ma mị của gỗ đàn hương Úc, tuyết tùng, bạch đậu khấu và hoa diên vĩ. Mùi hương cá tính độc bản khiến bất kỳ ai lướt qua cũng phải ngoái nhìn.',
        @BrandLeLabo, @CatUnisex, @FamWoody, 1, 3, N'Trên 10 giờ', N'Tỏa hương trong bán kính 1 - 2 mét', 2011, N'Bốn mùa, Ngày và Đêm',
        6800000, 'https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?auto=format&fit=crop&w=800&q=80',
        1, 1, 0, 1, 5.0, 48, 3210,
        N'Nước Hoa Le Labo Santal 33 Chính Hãng | Perfume Luxe',
        N'Khám phá mùi hương huyền thoại Le Labo Santal 33 EDP với gỗ đàn hương và da thuộc sang trọng bậc nhất.',
        GETUTCDATE(), 0
    );
END

-- 3.2. YSL Libre EDP
IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = 'ysl-libre-edp')
BEGIN
    INSERT INTO Products (Name, Slug, Sku, ShortDescription, FullDescription, BrandId, CategoryId, FragranceFamilyId, Gender, Concentration, Longevity, Sillage, ReleaseYear, SeasonRecommendation, BasePrice, MainImageUrl, IsFeatured, IsBestSeller, IsNewArrival, IsInStock, RatingAverage, RatingCount, ViewCount, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Yves Saint Laurent Libre Eau de Parfum',
        'ysl-libre-edp',
        'YSL-LIB-001',
        N'Bản tuyên ngôn tự do rực rỡ với sự kết hợp đối lập giữa hoa oải hương Pháp và hoa cam nồng nàn.',
        N'YSL Libre đại diện cho vẻ đẹp kiêu hãnh và gợi cảm của người phụ nữ hiện đại. Cấu trúc hương độc đáo kết hợp hoa oải hương dịu mát vốn quen thuộc trong nước hoa nam với hoa cam Ma-rốc ấm áp và vani Madagascar ngọt ngào.',
        @BrandYsl, @CatWomen, @FamFloral, 3, 3, N'8 - 10 giờ', N'Tỏa hương xa 1 - 1.5 mét', 2019, N'Thu, Đông, Xuân',
        3800000, 'https://images.unsplash.com/photo-1541643600914-78b084683601?auto=format&fit=crop&w=800&q=80',
        1, 1, 1, 1, 4.9, 36, 2850,
        N'Nước Hoa YSL Libre EDP Chính Hãng | Perfume Luxe',
        N'YSL Libre Eau de Parfum quyến rũ, sang trọng cho quý cô kiêu hãnh.',
        GETUTCDATE(), 0
    );
END

-- 3.3. YSL Black Opium EDP
IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = 'ysl-black-opium-edp')
BEGIN
    INSERT INTO Products (Name, Slug, Sku, ShortDescription, FullDescription, BrandId, CategoryId, FragranceFamilyId, Gender, Concentration, Longevity, Sillage, ReleaseYear, SeasonRecommendation, BasePrice, MainImageUrl, IsFeatured, IsBestSeller, IsNewArrival, IsInStock, RatingAverage, RatingCount, ViewCount, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Yves Saint Laurent Black Opium Eau de Parfum',
        'ysl-black-opium-edp',
        'YSL-BO-002',
        N'Mùi hương gây nghiện tột bậc từ cà phê đen đậm đặc, vani ngọt ngào và hoa trắng gợi cảm.',
        N'Black Opium được ví như liều thuốc kích thích khứu giác dành cho những đêm tiệc cuồng nhiệt. Hương thơm bùng nổ năng lượng của cà phê hạt rang xay quyện cùng hoa nhài Sambac thanh khiết và kết thúc bằng nốt vani ấm áp gợi cảm.',
        @BrandYsl, @CatWomen, @FamGourmand, 3, 3, N'7 - 9 giờ', N'Tỏa hương vừa phải 1 mét', 2014, N'Thu, Đông, Đêm tiệc',
        3650000, 'https://images.unsplash.com/photo-1523293182086-7651a899d37f?auto=format&fit=crop&w=800&q=80',
        0, 1, 0, 1, 4.8, 29, 2140,
        N'Nước Hoa YSL Black Opium Chính Hãng | Perfume Luxe',
        N'YSL Black Opium - Hương thơm cà phê và vani gây nghiện cho phái đẹp.',
        GETUTCDATE(), 0
    );
END

-- 3.4. Chanel No.5 EDP
IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = 'chanel-no-5-edp')
BEGIN
    INSERT INTO Products (Name, Slug, Sku, ShortDescription, FullDescription, BrandId, CategoryId, FragranceFamilyId, Gender, Concentration, Longevity, Sillage, ReleaseYear, SeasonRecommendation, BasePrice, MainImageUrl, IsFeatured, IsBestSeller, IsNewArrival, IsInStock, RatingAverage, RatingCount, ViewCount, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Chanel No.5 Eau de Parfum',
        'chanel-no-5-edp',
        'CH-NO5-001',
        N'Huyền thoại thế kỷ của sự thanh lịch vượt thời gian, biểu tượng vĩnh cửu của Haute Parfumerie.',
        N'Được tạo ra năm 1921 bởi Ernest Beaux theo yêu cầu của Gabrielle Chanel, Chanel No.5 là chai nước hoa nổi tiếng nhất lịch sử nhân loại. Lớp hương aldehyde rực rỡ mở lối cho vườn hoa hồng tháng Năm Grasse và hoa nhài thượng hạng.',
        @BrandChanel, @CatWomen, @FamFloral, 3, 3, N'8 - 12 giờ', N'Tỏa hương quý phái 1 mét', 1921, N'Bốn mùa, Sự kiện trang trọng',
        4250000, 'https://images.unsplash.com/photo-1588405748880-12d1d2a59f75?auto=format&fit=crop&w=800&q=80',
        1, 1, 0, 1, 5.0, 52, 4100,
        N'Chanel No.5 EDP Chính Hãng Pháp | Perfume Luxe',
        N'Biểu tượng nước hoa huyền thoại Chanel No.5 Eau de Parfum chính hãng nhập khẩu Pháp.',
        GETUTCDATE(), 0
    );
END

-- 3.5. Chanel Coco Mademoiselle EDP
IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = 'chanel-coco-mademoiselle-edp')
BEGIN
    INSERT INTO Products (Name, Slug, Sku, ShortDescription, FullDescription, BrandId, CategoryId, FragranceFamilyId, Gender, Concentration, Longevity, Sillage, ReleaseYear, SeasonRecommendation, BasePrice, MainImageUrl, IsFeatured, IsBestSeller, IsNewArrival, IsInStock, RatingAverage, RatingCount, ViewCount, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Chanel Coco Mademoiselle Eau de Parfum',
        'chanel-coco-mademoiselle-edp',
        'CH-CM-002',
        N'Hương thơm hổ phách hoa cỏ tươi sáng, thanh lịch và cuốn hút cho quý cô Paris duyên dáng.',
        N'Coco Mademoiselle mang tinh thần tự do và táo bạo của Mademoiselle Chanel thời trẻ. Cam Bergamot tươi mát bung nở, hòa quyện với hoa hồng Grasse kiêu sa và hoắc hương Indonesia tinh tế sang trọng.',
        @BrandChanel, @CatWomen, @FamOriental, 3, 3, N'8 - 10 giờ', N'Tỏa hương xa 1 - 2 mét', 2001, N'Bốn mùa, Đi làm và Hẹn hò',
        4150000, 'https://images.unsplash.com/photo-1594035910387-fea47794261f?auto=format&fit=crop&w=800&q=80',
        1, 1, 0, 1, 4.9, 41, 3600,
        N'Chanel Coco Mademoiselle EDP Chính Hãng | Perfume Luxe',
        N'Chanel Coco Mademoiselle - Mùi hương kiêu kỳ, sang trọng được yêu thích nhất toàn cầu.',
        GETUTCDATE(), 0
    );
END

-- 3.6. Dior J'adore EDP
IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = 'dior-jadore-edp')
BEGIN
    INSERT INTO Products (Name, Slug, Sku, ShortDescription, FullDescription, BrandId, CategoryId, FragranceFamilyId, Gender, Concentration, Longevity, Sillage, ReleaseYear, SeasonRecommendation, BasePrice, MainImageUrl, IsFeatured, IsBestSeller, IsNewArrival, IsInStock, RatingAverage, RatingCount, ViewCount, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Christian Dior J''adore Eau de Parfum',
        'dior-jadore-edp',
        'DIOR-JAD-002',
        N'Bó hoa vàng lộng lẫy và nữ tính tuyệt đối, tôn vinh vẻ đẹp kiêu sa của người phụ nữ.',
        N'Dior J''adore là tuyệt tác tôn vinh người phụ nữ với thiết kế chai lấy cảm hứng từ chiếc bình Amphora Hy Lạp cổ đại. Bó hoa rực rỡ kết hợp hoa ngọc lan tây Comoros, hoa hồng Damask và hoa nhài Grasse tinh khiết.',
        @BrandDior, @CatWomen, @FamFloral, 3, 3, N'7 - 9 giờ', N'Tỏa hương 1 mét', 1999, N'Xuân, Hạ, Thu, Ban ngày',
        3950000, 'https://images.unsplash.com/photo-1523293182086-7651a899d37f?auto=format&fit=crop&w=800&q=80',
        1, 1, 0, 1, 4.9, 33, 2700,
        N'Dior J''adore EDP Chính Hãng | Perfume Luxe',
        N'Bó hoa vàng rực rỡ Dior J''adore Eau de Parfum - Đỉnh cao sự nữ tính và xa xỉ.',
        GETUTCDATE(), 0
    );
END

-- 3.7. Tom Ford Lost Cherry EDP
IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = 'tom-ford-lost-cherry')
BEGIN
    INSERT INTO Products (Name, Slug, Sku, ShortDescription, FullDescription, BrandId, CategoryId, FragranceFamilyId, Gender, Concentration, Longevity, Sillage, ReleaseYear, SeasonRecommendation, BasePrice, MainImageUrl, IsFeatured, IsBestSeller, IsNewArrival, IsInStock, RatingAverage, RatingCount, ViewCount, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Tom Ford Lost Cherry Eau de Parfum',
        'tom-ford-lost-cherry',
        'TF-LC-002',
        N'Hương thơm cám dỗ táo bạo giữa quả anh đào đen ngâm rượu mùi và hạnh nhân đắng.',
        N'Tom Ford Lost Cherry là cuộc hành trình vào những điều cấm kỵ ngọt ngào. Trái anh đào đen mọng nước hòa quyện cùng rượu mùi Griotte, hạnh nhân đắng và hoa hồng Thổ Nhĩ Kỳ. Mùi hương khiêu khích, quyến rũ không thể cưỡng lại.',
        @BrandTomFord, @CatUnisex, @FamOriental, 1, 3, N'8 - 10 giờ', N'Tỏa hương vừa phải 1 mét', 2018, N'Thu, Đông, Đêm tiệc',
        8900000, 'https://images.unsplash.com/photo-1547887537-6158d64c35b3?auto=format&fit=crop&w=800&q=80',
        1, 1, 1, 1, 5.0, 39, 3950,
        N'Tom Ford Lost Cherry EDP Cao Cấp | Perfume Luxe',
        N'Nước hoa Niche đắt giá Tom Ford Lost Cherry - Quyến rũ, ngọt ngào và đầy mê hoặc.',
        GETUTCDATE(), 0
    );
END

-- 3.8. Creed Silver Mountain Water
IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = 'creed-silver-mountain-water')
BEGIN
    INSERT INTO Products (Name, Slug, Sku, ShortDescription, FullDescription, BrandId, CategoryId, FragranceFamilyId, Gender, Concentration, Longevity, Sillage, ReleaseYear, SeasonRecommendation, BasePrice, MainImageUrl, IsFeatured, IsBestSeller, IsNewArrival, IsInStock, RatingAverage, RatingCount, ViewCount, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Creed Silver Mountain Water Eau de Parfum',
        'creed-silver-mountain-water',
        'CR-SMW-002',
        N'Dòng suối tuyết tan trong trẻo trên dãy núi Alps Thụy Sĩ mát lạnh và sảng khoái phi thường.',
        N'Lấy cảm hứng từ niềm đam mê trượt tuyết trên dãy Alps của Olivier Creed, Silver Mountain Water tái hiện khung cảnh dòng suối băng lấp lánh dưới ánh mặt trời. Trà xanh, nho đen, cam bergamot và xạ hương trong trẻo mang lại cảm giác thanh khiết quý tộc.',
        @BrandCreed, @CatUnisex, @FamFresh, 1, 3, N'7 - 9 giờ', N'Tỏa hương thanh thoát 1 mét', 1995, N'Xuân, Hạ, Ngày nắng',
        7500000, 'https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?auto=format&fit=crop&w=800&q=80',
        0, 1, 0, 1, 4.8, 27, 2350,
        N'Creed Silver Mountain Water Chính Hãng | Perfume Luxe',
        N'Creed Silver Mountain Water - Mùi hương suối băng thanh khiết đỉnh cao quý tộc Anh.',
        GETUTCDATE(), 0
    );
END

-- 3.9. Maison Margiela Replica Lazy Sunday Morning
IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = 'replica-lazy-sunday-morning')
BEGIN
    INSERT INTO Products (Name, Slug, Sku, ShortDescription, FullDescription, BrandId, CategoryId, FragranceFamilyId, Gender, Concentration, Longevity, Sillage, ReleaseYear, SeasonRecommendation, BasePrice, MainImageUrl, IsFeatured, IsBestSeller, IsNewArrival, IsInStock, RatingAverage, RatingCount, ViewCount, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Maison Margiela Replica Lazy Sunday Morning',
        'replica-lazy-sunday-morning',
        'MM-LSM-002',
        N'Cảm giác cuộn mình trong tấm ga trải giường bằng vải lanh trắng giặt sạch dưới nắng sớm Florence.',
        N'Lazy Sunday Morning là chai nước hoa bán chạy nhất dòng Replica. Tái hiện lại buổi sáng Chủ nhật lười biếng ở Florence năm 2003, hương thơm của hoa linh lan thung lũng, hoa diên vĩ và xạ hương trắng sạch sẽ đem lại cảm giác bình yên, thư thái tuyệt đối.',
        @BrandMargiela, @CatUnisex, @FamFloral, 1, 2, N'6 - 8 giờ', N'Tỏa hương gần ôm sát cơ thể', 2013, N'Bốn mùa, Ban ngày, Không gian riêng tư',
        3400000, 'https://images.unsplash.com/photo-1541643600914-78b084683601?auto=format&fit=crop&w=800&q=80',
        1, 1, 1, 1, 4.9, 31, 2600,
        N'Replica Lazy Sunday Morning Chính Hãng | Perfume Luxe',
        N'Maison Margiela Lazy Sunday Morning - Mùi hương vải lanh sạch sẽ và nắng sớm thư thái.',
        GETUTCDATE(), 0
    );
END

-- 4. BỔ SUNG PRODUCT VARIANTS CHO CÁC SẢN PHẨM MỚI
DECLARE @P_Santal INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'le-labo-santal-33');
DECLARE @P_Libre INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'ysl-libre-edp');
DECLARE @P_BlackOpium INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'ysl-black-opium-edp');
DECLARE @P_No5 INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'chanel-no-5-edp');
DECLARE @P_CocoMad INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'chanel-coco-mademoiselle-edp');
DECLARE @P_Jadore INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'dior-jadore-edp');
DECLARE @P_LostCherry INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'tom-ford-lost-cherry');
DECLARE @P_SMW INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'creed-silver-mountain-water');
DECLARE @P_LazySunday INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'replica-lazy-sunday-morning');

IF @P_Santal IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductVariants WHERE ProductId = @P_Santal)
BEGIN
    INSERT INTO ProductVariants (ProductId, VolumeMl, VolumeLabel, VariantSku, OriginalPrice, SalePrice, StockQuantity, IsDefault, CreatedAt, IsDeleted)
    VALUES
    (@P_Santal, 10, N'Chiết 10ml', 'LL-SAN33-10ML', 890000, 790000, 45, 0, GETUTCDATE(), 0),
    (@P_Santal, 50, N'Fullbox 50ml', 'LL-SAN33-50ML', 5500000, 4990000, 15, 0, GETUTCDATE(), 0),
    (@P_Santal, 100, N'Fullbox 100ml', 'LL-SAN33-100ML', 7400000, 6800000, 20, 1, GETUTCDATE(), 0);
END

IF @P_Libre IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductVariants WHERE ProductId = @P_Libre)
BEGIN
    INSERT INTO ProductVariants (ProductId, VolumeMl, VolumeLabel, VariantSku, OriginalPrice, SalePrice, StockQuantity, IsDefault, CreatedAt, IsDeleted)
    VALUES
    (@P_Libre, 10, N'Chiết 10ml', 'YSL-LIB-10ML', 550000, 490000, 60, 0, GETUTCDATE(), 0),
    (@P_Libre, 50, N'Fullbox 50ml', 'YSL-LIB-50ML', 3300000, 2990000, 25, 0, GETUTCDATE(), 0),
    (@P_Libre, 90, N'Fullbox 90ml', 'YSL-LIB-90ML', 4200000, 3800000, 30, 1, GETUTCDATE(), 0);
END

IF @P_BlackOpium IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductVariants WHERE ProductId = @P_BlackOpium)
BEGIN
    INSERT INTO ProductVariants (ProductId, VolumeMl, VolumeLabel, VariantSku, OriginalPrice, SalePrice, StockQuantity, IsDefault, CreatedAt, IsDeleted)
    VALUES
    (@P_BlackOpium, 10, N'Chiết 10ml', 'YSL-BO-10ML', 520000, 470000, 50, 0, GETUTCDATE(), 0),
    (@P_BlackOpium, 90, N'Fullbox 90ml', 'YSL-BO-90ML', 4000000, 3650000, 22, 1, GETUTCDATE(), 0);
END

IF @P_No5 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductVariants WHERE ProductId = @P_No5)
BEGIN
    INSERT INTO ProductVariants (ProductId, VolumeMl, VolumeLabel, VariantSku, OriginalPrice, SalePrice, StockQuantity, IsDefault, CreatedAt, IsDeleted)
    VALUES
    (@P_No5, 10, N'Chiết 10ml', 'CH-NO5-10ML', 590000, 530000, 40, 0, GETUTCDATE(), 0),
    (@P_No5, 100, N'Fullbox 100ml', 'CH-NO5-100ML', 4700000, 4250000, 18, 1, GETUTCDATE(), 0);
END

IF @P_CocoMad IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductVariants WHERE ProductId = @P_CocoMad)
BEGIN
    INSERT INTO ProductVariants (ProductId, VolumeMl, VolumeLabel, VariantSku, OriginalPrice, SalePrice, StockQuantity, IsDefault, CreatedAt, IsDeleted)
    VALUES
    (@P_CocoMad, 10, N'Chiết 10ml', 'CH-CM-10ML', 580000, 520000, 55, 0, GETUTCDATE(), 0),
    (@P_CocoMad, 100, N'Fullbox 100ml', 'CH-CM-100ML', 4600000, 4150000, 25, 1, GETUTCDATE(), 0);
END

IF @P_Jadore IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductVariants WHERE ProductId = @P_Jadore)
BEGIN
    INSERT INTO ProductVariants (ProductId, VolumeMl, VolumeLabel, VariantSku, OriginalPrice, SalePrice, StockQuantity, IsDefault, CreatedAt, IsDeleted)
    VALUES
    (@P_Jadore, 10, N'Chiết 10ml', 'DIOR-JAD-10ML', 560000, 490000, 45, 0, GETUTCDATE(), 0),
    (@P_Jadore, 100, N'Fullbox 100ml', 'DIOR-JAD-100ML', 4400000, 3950000, 20, 1, GETUTCDATE(), 0);
END

IF @P_LostCherry IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductVariants WHERE ProductId = @P_LostCherry)
BEGIN
    INSERT INTO ProductVariants (ProductId, VolumeMl, VolumeLabel, VariantSku, OriginalPrice, SalePrice, StockQuantity, IsDefault, CreatedAt, IsDeleted)
    VALUES
    (@P_LostCherry, 10, N'Chiết 10ml', 'TF-LC-10ML', 1100000, 990000, 30, 0, GETUTCDATE(), 0),
    (@P_LostCherry, 50, N'Fullbox 50ml', 'TF-LC-50ML', 9500000, 8900000, 10, 1, GETUTCDATE(), 0);
END

IF @P_SMW IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductVariants WHERE ProductId = @P_SMW)
BEGIN
    INSERT INTO ProductVariants (ProductId, VolumeMl, VolumeLabel, VariantSku, OriginalPrice, SalePrice, StockQuantity, IsDefault, CreatedAt, IsDeleted)
    VALUES
    (@P_SMW, 10, N'Chiết 10ml', 'CR-SMW-10ML', 920000, 850000, 35, 0, GETUTCDATE(), 0),
    (@P_SMW, 100, N'Fullbox 100ml', 'CR-SMW-100ML', 8200000, 7500000, 14, 1, GETUTCDATE(), 0);
END

IF @P_LazySunday IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductVariants WHERE ProductId = @P_LazySunday)
BEGIN
    INSERT INTO ProductVariants (ProductId, VolumeMl, VolumeLabel, VariantSku, OriginalPrice, SalePrice, StockQuantity, IsDefault, CreatedAt, IsDeleted)
    VALUES
    (@P_LazySunday, 10, N'Chiết 10ml', 'MM-LSM-10ML', 480000, 420000, 50, 0, GETUTCDATE(), 0),
    (@P_LazySunday, 100, N'Fullbox 100ml', 'MM-LSM-100ML', 3800000, 3400000, 28, 1, GETUTCDATE(), 0);
END

-- 5. BỔ SUNG KIM TỰ THÁP HƯƠNG (PRODUCT FRAGRANCE NOTES)
DECLARE @N_Bergamot INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'cam-bergamot');
DECLARE @N_Cedar INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'tuyet-tung');
DECLARE @N_Sandal INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'dan-huong');
DECLARE @N_Vani INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'vani-bourbon');
DECLARE @N_Rose INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'hoa-hong-damask');
DECLARE @N_Jasmine INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'hoa-nhai-sambac');
DECLARE @N_Lavender INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'oai-huong');
DECLARE @N_Cherry INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'anh-dao-den');
DECLARE @N_OrangeBlossom INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'hoa-cam');
DECLARE @N_Coffee INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'ca-phe-den');
DECLARE @N_Musk INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'xa-huong-trang');

-- Kim tự tháp cho Santal 33
IF @P_Santal IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductFragranceNotes WHERE ProductId = @P_Santal)
BEGIN
    INSERT INTO ProductFragranceNotes (ProductId, FragranceNoteId, NoteLevel, DisplayOrder)
    VALUES
    (@P_Santal, @N_Bergamot, 1, 1),
    (@P_Santal, @N_Cedar, 2, 1),
    (@P_Santal, @N_Sandal, 3, 1);
END

-- Kim tự tháp cho YSL Libre
IF @P_Libre IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductFragranceNotes WHERE ProductId = @P_Libre)
BEGIN
    INSERT INTO ProductFragranceNotes (ProductId, FragranceNoteId, NoteLevel, DisplayOrder)
    VALUES
    (@P_Libre, @N_Lavender, 1, 1),
    (@P_Libre, @N_OrangeBlossom, 2, 1),
    (@P_Libre, @N_Vani, 3, 1);
END

-- Kim tự tháp cho Lost Cherry
IF @P_LostCherry IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductFragranceNotes WHERE ProductId = @P_LostCherry)
BEGIN
    INSERT INTO ProductFragranceNotes (ProductId, FragranceNoteId, NoteLevel, DisplayOrder)
    VALUES
    (@P_LostCherry, @N_Cherry, 1, 1),
    (@P_LostCherry, @N_Rose, 2, 1),
    (@P_LostCherry, @N_Sandal, 3, 1);
END

-- Kim tự tháp cho Lazy Sunday Morning
IF @P_LazySunday IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductFragranceNotes WHERE ProductId = @P_LazySunday)
BEGIN
    INSERT INTO ProductFragranceNotes (ProductId, FragranceNoteId, NoteLevel, DisplayOrder)
    VALUES
    (@P_LazySunday, @N_Bergamot, 1, 1),
    (@P_LazySunday, @N_Rose, 2, 1),
    (@P_LazySunday, @N_Musk, 3, 1);
END

-- 6. BỔ SUNG BLOG CATEGORIES & 11 BÀI VIẾT BLOG CHUẨN TẠP CHÍ HAUTE PARFUMERIE
IF NOT EXISTS (SELECT 1 FROM BlogCategories WHERE Slug = 'nghe-thuat-dung-huong')
BEGIN
    INSERT INTO BlogCategories (Name, Slug, Description, CreatedAt, IsDeleted)
    VALUES (N'Nghệ Thuật Dùng Hương', 'nghe-thuat-dung-huong', N'Bí quyết sử dụng, lưu hương và định hình phong cách cá nhân qua mùi hương.', GETUTCDATE(), 0);
END

DECLARE @CatKnowledge INT = (SELECT TOP 1 Id FROM BlogCategories WHERE Slug = 'kien-thuc-nuoc-hoa');
DECLARE @CatArt INT = (SELECT TOP 1 Id FROM BlogCategories WHERE Slug = 'nghe-thuat-dung-huong');
DECLARE @CatTrend INT = (SELECT TOP 1 Id FROM BlogCategories WHERE Slug = 'xu-huong-mui-huong');

-- 6.1. Bài 1: Cách xịt nước hoa thơm lâu
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'cach-xit-nuoc-hoa-thom-lau-ca-ngay')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Nghệ Thuật Xịt Nước Hoa Giữ Hương Suốt 12 Giờ Mà Không Gây Nồng Gắt',
        'cach-xit-nuoc-hoa-thom-lau-ca-ngay',
        N'Xịt vào cổ tay rồi chà xát là sai lầm phổ biến nhất làm phá vỡ phân tử mùi hương. Khám phá 5 điểm mạch vàng giúp hương thơm lan tỏa quý phái cả ngày dài.',
        N'<p class="lead">Nhiều người thường tự hỏi tại sao cùng một chai nước hoa đắt tiền, người khác dùng lại thơm thoang thoảng suốt ngày dài trong khi mình xịt chỉ 2 tiếng đã bay mất. Câu trả lời không nằm ở chất lượng nước hoa, mà nằm ở kỹ thuật xịt.</p>
        <h3>1. Sai lầm chí mạng: Chà xát cổ tay sau khi xịt</h3>
        <p>Thói quen xịt lên một bên cổ tay rồi áp hai cổ tay vào nhau chà xát mạnh là sai lầm nghiêm trọng nhất. Ma sát sinh ra nhiệt độ làm bẻ gãy cấu trúc của các nốt hương đầu (Top notes), khiến nốt hương bị biến dạng và bay hơi nhanh gấp 3 lần.</p>
        <h3>2. 5 Điểm mạch vàng lưu hương tối ưu</h3>
        <ul>
            <li><strong>Hai bên hõm cổ & sau gáy:</strong> Vùng da mỏng và mạch máu ấm giúp khuếch tán hương thơm tự nhiên khi bạn di chuyển hoặc quay đầu.</li>
            <li><strong>Phía sau vành tai:</strong> Vùng da ít tiếp xúc với ánh nắng mặt trời và dầu nhờn, giữ trọn nốt hương nguyên bản.</li>
            <li><strong>Mặt trong khuỷu tay:</strong> Nơi lý tưởng để hương thơm lan tỏa khi bạn cử động tay trong giao tiếp.</li>
            <li><strong>Khoảng cách vàng:</strong> Giữ chai cách bề mặt da từ 15 đến 20 cm để làn sương mù mỏng phủ đều thay vì tụ thành giọt nước.</li>
        </ul>
        <h3>3. Bí quyết dưỡng ẩm trước khi dùng</h3>
        <p>Phân tử nước hoa bám dính cực kỳ tốt trên nền da có độ ẩm cao. Thoa một lớp mỏng kem dưỡng thể không mùi hoặc một chút vaseline lên điểm xịt trước khi dùng nước hoa sẽ giúp thời gian lưu hương tăng lên thêm 4 đến 6 tiếng.</p>',
        'https://images.unsplash.com/photo-1594035910387-fea47794261f?auto=format&fit=crop&w=800&q=80',
        N'Master Parfumeur Jean-Paul', 2850, 1, @CatArt,
        N'Bí Quyết Xịt Nước Hoa Thơm Lâu 12 Giờ | Perfume Luxe',
        N'Hướng dẫn chi tiết cách xịt nước hoa chuẩn chuyên gia giữ hương suốt ngày dài mà không làm hỏng nốt hương.',
        GETUTCDATE(), 0
    );
END

-- 6.2. Bài 2: Phân biệt EDT, EDP và Parfum
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'phan-biet-edt-edp-parfum')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Giải Mã Nồng Độ Nước Hoa: Sự Khác Biệt Giữa EDT, EDP, Parfum Và Extrait',
        'phan-biet-edt-edp-parfum',
        N'Đừng để các ký hiệu viết tắt làm bạn bối rối. Hiểu đúng tỉ lệ tinh dầu nguyên chất sẽ giúp bạn chọn được chai nước hoa phù hợp nhất với khí hậu Việt Nam.',
        N'<p class="lead">Khi chọn mua nước hoa, bạn sẽ luôn bắt gặp các từ viết tắt như EDT, EDP hay Parfum. Đây là những thuật ngữ tiêu chuẩn quốc tế biểu thị nồng độ tinh dầu thơm (Pure Perfume Oil) hòa tan trong cồn.</p>
        <h3>1. Eau de Toilette (EDT) - 5% đến 15% tinh dầu</h3>
        <p>EDT là nồng độ phổ biến và dễ dùng nhất, lưu hương từ 4 đến 6 giờ. Nồng độ này làm nổi bật các nốt hương đầu tươi mát, vô cùng lý tưởng cho môi trường văn phòng, ngày hè nóng bức hoặc các buổi tập thể thao.</p>
        <h3>2. Eau de Parfum (EDP) - 15% đến 20% tinh dầu</h3>
        <p>EDP là chuẩn mực vàng của ngành công nghiệp nước hoa. Với tỉ lệ tinh dầu đậm đặc hơn, các nốt hương giữa và hương cuối phát triển dày dặn, có khả năng lưu hương từ 8 đến 10 giờ trên da thịt.</p>
        <h3>3. Parfum & Extrait de Parfum - 20% đến 40% tinh dầu</h3>
        <p>Đỉnh cao của sự xa xỉ với lượng tinh dầu cô đặc tuyệt đối. Nước hoa Parfum tỏa hương trong khoảng cách gần nhưng lưu hương bền bỉ lên tới 16 - 24 giờ, biến cơ thể bạn thành một bức tranh mùi hương tinh tế nhất.</p>',
        'https://images.unsplash.com/photo-1547887537-6158d64c35b3?auto=format&fit=crop&w=800&q=80',
        N'Hélène de Grasse', 3420, 1, @CatKnowledge,
        N'Phân Biệt Nồng Độ Nước Hoa EDT, EDP, Extrait | Perfume Luxe',
        N'Bảng so sánh chi tiết nồng độ tinh dầu nước hoa và độ lưu hương thực tế.',
        GETUTCDATE(), 0
    );
END

-- 6.3. Bài 3: Thế giới nước hoa Niche
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'nuoc-hoa-niche-la-gi')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Nước Hoa Niche Là Gì? Vì Sao Giới Thượng Lưu Lại Săn Lùng Mùi Hương Độc Bản?',
        'nuoc-hoa-niche-la-gi',
        N'Khác với Designer Perfume sản xuất hàng loạt, nước hoa Niche là nghệ thuật điêu khắc khứu giác dành cho những cá tính không chấp nhận sự hòa lẫn.',
        N'<p class="lead">Trong tiếng Pháp, "Niche" có nghĩa là một hốc tường hay một thị trường ngách. Nước hoa Niche ra đời để phục vụ những tâm hồn sành sỏi mong muốn tìm kiếm một dấu ấn mùi hương độc nhất vô nhị.</p>
        <h3>1. Tự do sáng tạo tuyệt đối không chạy theo thị hiếu</h3>
        <p>Nếu các dòng nước hoa thương mại phải nghiên cứu thị hiếu số đông và chi hàng triệu USD cho quảng cáo, thì các Master Perfumer của dòng Niche như Creed, Le Labo, Kilian chỉ tập trung vào một điều duy nhất: chất lượng của mùi hương.</p>
        <h3>2. Nguyên liệu tự nhiên quý hiếm bậc nhất hành tinh</h3>
        <p>Nước hoa Niche sử dụng những nguyên liệu đắt hơn vàng ròng: Trầm hương Oud khai thác từ cây dó bầu hàng chục năm tuổi, hoa hồng xứ Grasse thu hoạch bằng tay vào lúc bình minh hay Long diên hương tự nhiên trôi dạt từ đại dương.</p>',
        'https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?auto=format&fit=crop&w=800&q=80',
        N'Antoine Laurent', 4120, 1, @CatTrend,
        N'Nước Hoa Niche Là Gì? Đẳng Cấp Mùi Hương Độc Bản | Perfume Luxe',
        N'Khám phá thế giới nước hoa Niche cao cấp và lý do vì sao những mùi hương này có giá trị vượt thời gian.',
        GETUTCDATE(), 0
    );
END

-- 6.4. Bài 4: Top 5 nước hoa nam công sở
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = '5-chai-nuoc-hoa-nam-cong-so-lich-lam')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Top 5 Mùi Hương Nam Công Sở Thanh Lịch, Đĩnh Đạc Và Tinh Tế Cho Quý Ông',
        '5-chai-nuoc-hoa-nam-cong-so-lich-lam',
        N'Trong phòng họp kín hay không gian mở, một mùi hương đúng điệu sẽ gia tăng sức hút cá nhân và sự tin cậy từ đối tác mà không gây khó chịu cho người xung quanh.',
        N'<p class="lead">Nước hoa công sở là một bài toán cân bằng tinh vi: đủ ấn tượng để khẳng định vị thế nhưng phải đủ chừng mực để không lấn át không gian chung.</p>
        <h3>1. Bleu de Chanel Parfum - Đĩnh đạc và chuẩn mực</h3>
        <p>Bleu de Chanel phiên bản Parfum mang đến chiều sâu ấm áp của gỗ tuyết tùng và đàn hương New Caledonia, tạo cảm giác một quý ông chỉn chu và đáng tin cậy tuyệt đối.</p>
        <h3>2. Dior Sauvage Elixir - Quyền lực của nhà lãnh đạo</h3>
        <p>Với nốt hương oải hương và gia vị ấm, chỉ cần một nhát xịt nhẹ là bạn đã sở hữu hào quang cuốn hút trong những buổi thương thảo quan trọng.</p>
        <h3>3. Creed Aventus - Khí chất hoàng gia</h3>
        <p>Sự kết hợp giữa dứa nướng khói và rêu sồi mang lại phong thái người chiến thắng, là chai nước hoa khẳng định vị thế dẫn đầu trong mọi tập thể.</p>',
        'https://images.unsplash.com/photo-1523293182086-7651a899d37f?auto=format&fit=crop&w=800&q=80',
        N'Thanh Phong - Scent Critic', 1980, 1, @CatTrend,
        N'Top 5 Nước Hoa Nam Công Sở Lịch Lãm Nhất | Perfume Luxe',
        N'Bảng xếp hạng 5 chai nước hoa nam văn phòng lịch sự, sang trọng và thu hút nhất.',
        GETUTCDATE(), 0
    );
END

-- 6.5. Bài 5: Mùi hương cho đêm hẹn hò
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'nhung-mui-huong-quyen-ru-nhat-cho-dem-hen-ho')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Vũ Khí Vô Hình: Những Nốt Hương Quyến Rũ Khiến Đối Phương Vấn Vương Suốt Đêm',
        'nhung-mui-huong-quyen-ru-nhat-cho-dem-hen-ho',
        N'Khứu giác là giác quan duy nhất liên kết trực tiếp tới vùng não cảm xúc và ký ức. Hãy để hương thơm thay lời muốn nói trong buổi hẹn hò lãng mạn.',
        N'<p class="lead">Ánh nến lung linh, tiếng nhạc du dương và một làn hương thoảng qua khi hai người kề sát - đó chính là khoảnh khắc kỳ diệu nhất của tình yêu.</p>
        <h3>1. Nốt hương Gourmand ngọt ngào mời gọi</h3>
        <p>YSL Black Opium với cà phê đen và vani hay Kilian Angels'' Share với rượu cognac ủ thùng gỗ sồi tạo ra lực hút khó cưỡng, khiến người đối diện luôn muốn tiến lại gần hơn.</p>
        <h3>2. Xạ hương và hoa trắng gợi cảm</h3>
        <p>Hoa nhài Sambac nở về đêm và xạ hương tự nhiên tạo nên cảm giác da thịt mềm mại, kích hoạt những rung cảm nguyên sơ và tinh tế nhất.</p>',
        'https://images.unsplash.com/photo-1541643600914-78b084683601?auto=format&fit=crop&w=800&q=80',
        N'Hélène de Grasse', 2650, 1, @CatArt,
        N'Nước Hoa Hẹn Hò Quyến Rũ Khiến Đối Phương Mê Đắm | Perfume Luxe',
        N'Bí quyết chọn nước hoa hẹn hò giúp bạn để lại dấu ấn khó quên trong tâm trí đối phương.',
        GETUTCDATE(), 0
    );
END

-- 6.6. Bài 6: Lịch sử Chanel No.5
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'lich-su-chanel-no-5-huyen-thoai-the-ky')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Chanel No.5: Huyền Thoại Trăm Năm Và Bí Mật Đằng Sau Con Số 5 Ma Lực',
        'lich-su-chanel-no-5-huyen-thoai-the-ky',
        N'Từ câu nói để đời của Marilyn Monroe đến cuộc cách mạng aldehyde năm 1921, Chanel No.5 đã định nghĩa lại sự thanh lịch của người phụ nữ toàn cầu.',
        N'<p class="lead">"Tôi mặc gì khi đi ngủ? Chỉ vài giọt Chanel No.5." Câu trả lời bất hủ của biểu tượng gợi cảm Marilyn Monroe đã đưa Chanel No.5 trở thành huyền thoại bất tử của nhân loại.</p>
        <h3>1. Con số 5 định mệnh</h3>
        <p>Năm 1921, Gabrielle Coco Chanel yêu cầu nhà chế tác Ernest Beaux tạo ra một mùi hương "có mùi của một người phụ nữ chứ không phải một bông hoa". Beaux đã trình lên các mẫu thử đánh số từ 1 đến 5 và từ 20 đến 24. Coco Chanel đã chọn ngay mẫu thử số 5 - con số may mắn của bà.</p>
        <h3>2. Cuộc cách mạng Aldehyde</h3>
        <p>Chanel No.5 là chai nước hoa tiên phong sử dụng hàm lượng aldehyde tổng hợp cao, tạo ra một luồng sáng lấp lánh như champagne sủi bọt, phá tan khuôn mẫu đơn hương nhàm chán của thế kỷ trước.</p>',
        'https://images.unsplash.com/photo-1588405748880-12d1d2a59f75?auto=format&fit=crop&w=800&q=80',
        N'Chuyên Gia Lịch Sử Hương Thơm', 5200, 1, @CatKnowledge,
        N'Lịch Sử Huyền Thoại Nước Hoa Chanel No.5 | Perfume Luxe',
        N'Khám phá câu chuyện đằng sau sự ra đời của kiệt tác nước hoa vĩ đại nhất lịch sử.',
        GETUTCDATE(), 0
    );
END

-- 6.7. Bài 7: Khám phá nốt hương gỗ Woody
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'huong-go-woody-tuyen-ngon-khi-chat-vinh-cuu')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Nốt Hương Gỗ (Woody): Tuyên Ngôn Của Sự Vững Chãi, Trầm Ấm Và Chiều Sâu Nội Tâm',
        'huong-go-woody-tuyen-ngon-khi-chat-vinh-cuu',
        N'Từ tuyết tùng Atlas khô ráo, đàn hương Mysore béo ngậy cho đến trầm hương Oud huyền bí, nhóm hương gỗ mang đến sức mạnh bình yên trong tâm trí.',
        N'<p class="lead">Nếu hương hoa cỏ tượng trưng cho sự tươi trẻ chóng tàn, thì hương gỗ lại là biểu tượng của thời gian ngưng đọng và chiều sâu nội tâm tĩnh lặng.</p>
        <h3>1. Tuyết tùng (Cedarwood) - Khí chất mạnh mẽ</h3>
        <p>Mùi hương gỗ tuyết tùng khô, sắc sảo gợi nhớ đến những trang sách cũ trong thư viện cổ hay tủ gỗ quý gia truyền, mang lại cảm giác tin cậy tuyệt đối.</p>
        <h3>2. Đàn hương (Sandalwood) - Sự mềm mại thiền định</h3>
        <p>Đàn hương là loại gỗ đặc biệt với đặc tính béo nhẹ, ngọt ngào như kem sữa và phảng phất hương vị thiền tịnh, giúp xoa dịu mọi căng thẳng của nhịp sống hiện đại.</p>',
        'https://images.unsplash.com/photo-1594035910387-fea47794261f?auto=format&fit=crop&w=800&q=80',
        N'Master Parfumeur Jean-Paul', 1890, 1, @CatKnowledge,
        N'Khám Phá Nhóm Hương Gỗ Woody Đẳng Cấp | Perfume Luxe',
        N'Cẩm nang chuyên sâu về các loại gỗ quý được ứng dụng trong nghệ thuật chế tác nước hoa.',
        GETUTCDATE(), 0
    );
END

-- 6.8. Bài 8: Bảo quản nước hoa đúng chuẩn
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'cach-bao-quan-nuoc-hoa-dung-chuan-khong-bien-mui')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Bảo Quản Nước Hoa Đúng Chuẩn: Giữ Mùi Hương Vẹn Nguyên Suốt 10 Năm Không Biến Đổi',
        'cach-bao-quan-nuoc-hoa-dung-chuan-khong-bien-mui',
        N'Nhiệt độ, ánh sáng mặt trời và độ ẩm trong phòng tắm là 3 kẻ thù thầm lặng giết chết linh hồn chai nước hoa yêu quý của bạn.',
        N'<p class="lead">Một chai nước hoa cao cấp được bảo quản tốt có thể sử dụng và lưu giữ hương thơm hoàn hảo từ 5 đến 10 năm mà không hề bị bay mùi hay ôi dầu.</p>
        <h3>1. Tránh xa ánh sáng mặt trời trực tiếp</h3>
        <p>Tia cực tím (UV) có khả năng phân hủy liên kết hóa học của tinh dầu thơm, làm nước hoa bị đổi màu và biến chất mùi hương chỉ sau vài tuần.</p>
        <h3>2. Không để nước hoa trong phòng tắm</h3>
        <p>Độ ẩm cao và sự thay đổi nhiệt độ liên tục khi tắm nước nóng sẽ kích thích quá trình oxy hóa bên trong bình chứa. Nơi lý tưởng nhất là trong tủ gỗ râm mát hoặc hộp đựng chuyên dụng.</p>',
        'https://images.unsplash.com/photo-1547887537-6158d64c35b3?auto=format&fit=crop&w=800&q=80',
        N'Bảo An - Perfume Sommelier', 2210, 1, @CatArt,
        N'Cách Bảo Quản Nước Hoa Chuẩn Chuyên Gia | Perfume Luxe',
        N'Bí kíp giữ nước hoa bền mùi qua năm tháng tránh biến đổi chất lượng.',
        GETUTCDATE(), 0
    );
END

-- 6.9. Bài 9: Nước hoa chiết chính hãng
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'nuoc-hoa-chiet-la-gi-co-nen-dung')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Nước Hoa Chiết Là Gì? Xu Hướng Trải Nghiệm Mùi Hương Đa Dạng Tiết Kiệm Chi Phí',
        'nuoc-hoa-chiet-la-gi-co-nen-dung',
        N'Tại sao phải bỏ 7-8 triệu để mua một chai fullbox khi bạn chưa chắc có hợp mùi hương đó? Nước hoa chiết chính hãng 10ml là giải pháp thông minh của người tiêu dùng hiện đại.',
        N'<p class="lead">Nước hoa chiết là sản phẩm được rút trực tiếp từ chai fullbox chính hãng sang các lọ xịt dung tích nhỏ từ 10ml đến 20ml bằng dụng cụ chuyên dụng vô trùng.</p>
        <h3>1. Tiện lợi bỏ túi mang đi khắp nơi</h3>
        <p>Những chai chiết 10ml với lớp vỏ nhôm bảo vệ gọn nhẹ dễ dàng mang theo khi đi làm, du lịch hay dự tiệc để dặm lại mùi hương bất cứ lúc nào.</p>
        <h3>2. Trải nghiệm nhiều mùi hương đẳng cấp</h3>
        <p>Thay vì đầu tư một số tiền lớn cho một chai nước hoa duy nhất, người yêu hương có thể sở hữu cả một bộ sưu tập 5-6 mùi hương khác nhau phù hợp cho từng hoàn cảnh và thời tiết.</p>',
        'https://images.unsplash.com/photo-1523293182086-7651a899d37f?auto=format&fit=crop&w=800&q=80',
        N'Thanh Phong - Scent Critic', 3100, 1, @CatTrend,
        N'Nước Hoa Chiết Chính Hãng 10ml Uy Tín | Perfume Luxe',
        N'Hiểu đúng về nước hoa chiết và cách phân biệt nước hoa chiết chính hãng với hàng giả.',
        GETUTCDATE(), 0
    );
END

-- 6.10. Bài 10: Le Labo và triết lý tối giản
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'le-labo-va-triet-ly-mui-huong-toi-gian')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Le Labo: Khi Mùi Hương Trở Thành Nghệ Thuật Thủ Công Đậm Chất New York',
        'le-labo-va-triet-ly-mui-huong-toi-gian',
        N'Không bao bì cầu kỳ, nhãn dán in thủ công theo tên khách hàng và con số bí ẩn sau mỗi cái tên - Le Labo đã làm chao đảo giới mộ điệu như thế nào?',
        N'<p class="lead">Ra đời tại phố Elizabeth, Nolita, New York vào năm 2006 bởi Fabrice Penot và Edouard Roschi, Le Labo (nghĩa là Phòng Thí Nghiệm) đã đảo lộn mọi quy ước xa hoa truyền thống của ngành nước hoa Pháp.</p>
        <h3>1. Ý nghĩa con số sau tên chai</h3>
        <p>Mỗi chai nước hoa Le Labo được đặt tên theo thành phần chính và số lượng nguyên liệu cấu thành. Ví dụ, Santal 33 được tạo nên từ 33 thành phần xoay quanh nốt hương chủ đạo là đàn hương Úc.</p>
        <h3>2. Tuyên ngôn Wabi-Sabi tôn vinh sự mộc mạc</h3>
        <p>Le Labo theo đuổi triết lý Wabi-Sabi của Nhật Bản - tìm kiếm vẻ đẹp trong sự bất toàn và mộc mạc. Mùi hương của họ không nịnh mũi đại trà mà mang một phong vị hoang dã, tự nhiên và chân thật đến nao lòng.</p>',
        'https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?auto=format&fit=crop&w=800&q=80',
        N'Antoine Laurent', 2740, 1, @CatTrend,
        N'Le Labo - Triết Lý Mùi Hương Thủ Công Niche | Perfume Luxe',
        N'Khám phá câu chuyện thương hiệu Le Labo và hiện tượng toàn cầu Santal 33.',
        GETUTCDATE(), 0
    );
END

-- 6.11. Bài 11: Nghệ thuật Layering nước hoa
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'nghe-thuat-layering-pha-tron-mui-huong')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Nghệ Thuật Layering: Bí Kíp Tự Pha Trộn Để Tạo Ra Mùi Hương Độc Nhất Vô Nhị',
        'nghe-thuat-layering-pha-tron-mui-huong',
        N'Đừng để mùi hương của bạn bị trùng lặp với bất kỳ ai. Học ngay kỹ thuật chồng lớp mùi hương từ các chuyên gia nước hoa hàng đầu thế giới.',
        N'<p class="lead">Fragrance Layering (xếp tầng mùi hương) là nghệ thuật kết hợp từ hai loại nước hoa trở lên trên cùng một cơ thể để tạo nên một mùi hương hoàn toàn mới mang dấu ấn cá nhân của riêng bạn.</p>
        <h3>1. Quy tắc nặng xịt trước, nhẹ xịt sau</h3>
        <p>Luôn xịt chai có nốt hương đậm, nồng nặc và cấu trúc phân tử nặng (như Trầm hương, Hoắc hương, Hổ phách, Gỗ đàn hương) trước. Sau đó mới phủ một lớp hương nhẹ hơn (Cam chanh, Hoa cỏ, Trà xanh) lên trên để tránh làm lấn át nốt hương thanh mảnh.</p>
        <h3>2. Những cặp đôi hoàn hảo không thể sai</h3>
        <ul>
            <li><strong>Gỗ đàn hương + Hoa cam / Hoa nhài:</strong> Cân bằng giữa sự trầm tĩnh và nét rạng rỡ tươi vui.</li>
            <li><strong>Hương da thuộc + Quả ngọt / Vani:</strong> Tạo nên sự tương phản bùng nổ giữa nét phong trần và sự quyến rũ chết người.</li>
        </ul>',
        'https://images.unsplash.com/photo-1541643600914-78b084683601?auto=format&fit=crop&w=800&q=80',
        N'Master Parfumeur Jean-Paul', 3680, 1, @CatArt,
        N'Nghệ Thuật Layering Chồng Lớp Mùi Hương | Perfume Luxe',
        N'Hướng dẫn cách tự phối mùi hương độc bản tạo phong cách quý phái khác biệt.',
        GETUTCDATE(), 0
    );
END

-- 7. BỔ SUNG 18 ĐÁNH GIÁ 5 SAO MẪU (REVIEWS)
DECLARE @ProdDiorSE INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'dior-sauvage-elixir');
DECLARE @ProdBDC INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'bleu-de-chanel-parfum');
DECLARE @ProdAventus INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'creed-aventus');
DECLARE @ProdTomFordTV INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'tom-ford-tobacco-vanille');
DECLARE @ProdJazzClub INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'replica-jazz-club');
DECLARE @ProdAngels INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'kilian-angels-share');

IF NOT EXISTS (SELECT 1 FROM Reviews WHERE Comment LIKE N'%Đẳng cấp thực sự%')
BEGIN
    INSERT INTO Reviews (ProductId, UserName, Rating, Comment, LongevityRating, IsApproved, CreatedAt, IsDeleted)
    VALUES
    -- Dior Sauvage Elixir
    (@ProdDiorSE, N'Trần Hoàng Nam', 5, N'Đẳng cấp thực sự! Mùi hương đậm đặc, chỉ xịt đúng 2 nhát buổi sáng mà đến tối về phòng vẫn còn thơm nức nở. Đóng gói hộp nhung rất sang trọng.', N'Rất lâu (trên 12h)', 1, DATEADD(DAY, -12, GETUTCDATE()), 0),
    (@ProdDiorSE, N'Lê Minh Tuấn', 5, N'Hàng chuẩn chính hãng tem mác đầy đủ. Mùi hương gỗ và tiêu cay ấm rất quyền lực, đi tiệc ai cũng hỏi dùng chai gì.', N'Rất lâu (trên 12h)', 1, DATEADD(DAY, -9, GETUTCDATE()), 0),
    
    -- Bleu de Chanel
    (@ProdBDC, N'Nguyễn Đức Huy', 5, N'Bản Parfum này mượt mà hơn hẳn bản EDT. Hương gỗ đàn hương béo nhẹ và tuyết tùng cực kỳ sang trọng, đi làm văn phòng rất lịch sự.', N'Tốt (8-10h)', 1, DATEADD(DAY, -15, GETUTCDATE()), 0),
    (@ProdBDC, N'Phạm Quang Minh', 5, N'Chai nước hoa quốc dân nhưng phiên bản này ở một tầm cao khác. Khuyên anh em nên mua chai fullbox dùng lâu dài.', N'Tốt (8-10h)', 1, DATEADD(DAY, -7, GETUTCDATE()), 0),

    -- Creed Aventus
    (@ProdAventus, N'Hoàng Gia Bảo', 5, N'Mùi hương đỉnh của chóp! Mùi dứa nướng quyện khói rêu sồi cực kỳ nam tính và quyền lực. Đáng từng đồng bỏ ra.', N'Rất lâu (trên 10h)', 1, DATEADD(DAY, -18, GETUTCDATE()), 0),
    (@ProdAventus, N'Đặng Thanh Tùng', 5, N'Giao hàng hỏa tốc trong 2 tiếng tại Hà Nội, nhân viên tư vấn rất am hiểu nước hoa Niche. Sẽ ủng hộ shop dài lâu.', N'Tốt (8-10h)', 1, DATEADD(DAY, -5, GETUTCDATE()), 0),

    -- Tom Ford Tobacco Vanille
    (@ProdTomFordTV, N'Vũ Đình Khang', 5, N'Mùa đông mà xịt em này thì ấm áp vô cùng. Nốt lá thuốc lá và vani béo ngọt cực sang chảnh.', N'Rất lâu (trên 12h)', 1, DATEADD(DAY, -14, GETUTCDATE()), 0),

    -- Replica Jazz Club
    (@ProdJazzClub, N'Bùi Quốc Anh', 5, N'Mùi rượu rum và lá thuốc lá nhẹ nhàng, cảm giác như đang ngồi thưởng thức ly cocktail trong quán bar cổ điển ở Brooklyn.', N'Khá (6-8h)', 1, DATEADD(DAY, -11, GETUTCDATE()), 0),

    -- Kilian Angels' Share
    (@ProdAngels, N'Ngọc Mai', 5, N'Chai nước hoa đẹp như một ly pha lê đựng rượu quý. Mùi hương rượu cognac và quế ấm áp ngọt ngào thôi miên.', N'Rất lâu (trên 10h)', 1, DATEADD(DAY, -8, GETUTCDATE()), 0);
END

-- Đánh giá cho các sản phẩm mới
IF @P_Santal IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reviews WHERE ProductId = @P_Santal)
BEGIN
    INSERT INTO Reviews (ProductId, UserName, Rating, Comment, LongevityRating, IsApproved, CreatedAt, IsDeleted)
    VALUES
    (@P_Santal, N'Đỗ Phương Thảo', 5, N'Santal 33 đúng là mùi hương độc bản! Ban đầu hơi ngái mùi thảo mộc nhưng sau 15 phút nốt đàn hương khô tỏa ra cực kỳ nghệ thuật và cuốn hút.', N'Rất lâu (trên 10h)', 1, DATEADD(DAY, -6, GETUTCDATE()), 0),
    (@P_Santal, N'Lâm Khánh Hà', 5, N'Mình dùng chai này ai đi qua cũng hỏi. Chai chiết 10ml cầm rất chắc tay và vòi xịt sương tơi đều.', N'Rất lâu (trên 10h)', 1, DATEADD(DAY, -3, GETUTCDATE()), 0);
END

IF @P_Libre IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reviews WHERE ProductId = @P_Libre)
BEGIN
    INSERT INTO Reviews (ProductId, UserName, Rating, Comment, LongevityRating, IsApproved, CreatedAt, IsDeleted)
    VALUES
    (@P_Libre, N'Nguyễn Thùy Linh', 5, N'YSL Libre kiêu kỳ sang chảnh dã man! Mùi hoa oải hương mát kết hợp hoa cam ngọt ngào, xịt đi làm hay đi tiệc đều tôn thần thái.', N'Tốt (8-10h)', 1, DATEADD(DAY, -8, GETUTCDATE()), 0),
    (@P_Libre, N'Trần Bích Ngọc', 5, N'Mùi hương quyền lực của một nữ giám đốc hiện đại. Nước hoa thơm lâu và tỏa hương xa.', N'Tốt (8-10h)', 1, DATEADD(DAY, -4, GETUTCDATE()), 0);
END

IF @P_No5 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reviews WHERE ProductId = @P_No5)
BEGIN
    INSERT INTO Reviews (ProductId, UserName, Rating, Comment, LongevityRating, IsApproved, CreatedAt, IsDeleted)
    VALUES
    (@P_No5, N'Bà Thanh Hằng', 5, N'Chanel No.5 là tình yêu vượt thời gian của tôi. Mùi hương sang trọng cổ điển của phụ nữ Pháp quý tộc.', N'Rất lâu (trên 10h)', 1, DATEADD(DAY, -16, GETUTCDATE()), 0);
END

IF @P_CocoMad IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reviews WHERE ProductId = @P_CocoMad)
BEGIN
    INSERT INTO Reviews (ProductId, UserName, Rating, Comment, LongevityRating, IsApproved, CreatedAt, IsDeleted)
    VALUES
    (@P_CocoMad, N'Thu Thảo Paris', 5, N'Mùi hương quyến rũ và thanh lịch nhất mà mình từng sở hữu. Rất thích hợp cho các buổi hẹn hò lãng mạn.', N'Tốt (8-10h)', 1, DATEADD(DAY, -10, GETUTCDATE()), 0);
END

IF @P_LostCherry IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reviews WHERE ProductId = @P_LostCherry)
BEGIN
    INSERT INTO Reviews (ProductId, UserName, Rating, Comment, LongevityRating, IsApproved, CreatedAt, IsDeleted)
    VALUES
    (@P_LostCherry, N'Hoàng Yến', 5, N'Mùi anh đào ngâm rượu ngọt lịm và quyến rũ kinh khủng! Đúng là siêu phẩm của Tom Ford, xứng đáng từng đồng.', N'Tốt (8-10h)', 1, DATEADD(DAY, -5, GETUTCDATE()), 0);
END

IF @P_LazySunday IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reviews WHERE ProductId = @P_LazySunday)
BEGIN
    INSERT INTO Reviews (ProductId, UserName, Rating, Comment, LongevityRating, IsApproved, CreatedAt, IsDeleted)
    VALUES
    (@P_LazySunday, N'Minh Thư', 5, N'Mùi hương sạch sẽ tinh tươm như vừa giặt xong ga giường và phơi dưới nắng sớm. Rất thư giãn và dễ chịu.', N'Khá (6-8h)', 1, DATEADD(DAY, -2, GETUTCDATE()), 0);
END

-- 8. BỔ SUNG 8 ĐƠN HÀNG MẪU (ORDERS + ORDERITEMS) ĐỦ CÁC TRẠNG THÁI
IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderCode = 'PF-20261001-8841')
BEGIN
    -- Đơn 1: Đã giao thành công (Delivered)
    INSERT INTO Orders (OrderCode, CustomerName, CustomerPhone, CustomerEmail, ShippingAddress, City, OrderNotes, SubTotal, DiscountAmount, ShippingFee, TotalAmount, PaymentMethod, PaymentStatus, OrderStatus, TrackingNumber, CreatedAt, IsDeleted)
    VALUES ('PF-20261001-8841', N'Trần Văn Long', '0912345678', 'long.tran@gmail.com', N'88 Phố Huế, Hoàn Kiếm', N'Hà Nội', N'Giao giờ hành chính', 3850000, 385000, 0, 3465000, 2, 2, 4, 'VNPOST-8841920', DATEADD(DAY, -10, GETUTCDATE()), 0);
    DECLARE @O1 INT = SCOPE_IDENTITY();
    INSERT INTO OrderItems (OrderId, ProductId, ProductName, VariantLabel, UnitPrice, Quantity, CreatedAt, IsDeleted)
    VALUES (@O1, @ProdDiorSE, N'Dior Sauvage Elixir Parfum', N'Fullbox 60ml', 3850000, 1, DATEADD(DAY, -10, GETUTCDATE()), 0);

    -- Đơn 2: Đã giao thành công (Delivered)
    INSERT INTO Orders (OrderCode, CustomerName, CustomerPhone, CustomerEmail, ShippingAddress, City, OrderNotes, SubTotal, DiscountAmount, ShippingFee, TotalAmount, PaymentMethod, PaymentStatus, OrderStatus, TrackingNumber, CreatedAt, IsDeleted)
    VALUES ('PF-20261003-7729', N'Nguyễn Mai Anh', '0988776655', 'maianh.nguyen@outlook.com', N'124 Nguyễn Huệ, Quận 1', N'TP. Hồ Chí Minh', N'Gói quà sinh nhật giúp mình', 7800000, 0, 0, 7800000, 4, 2, 4, 'GHN-7729112', DATEADD(DAY, -8, GETUTCDATE()), 0);
    DECLARE @O2 INT = SCOPE_IDENTITY();
    INSERT INTO OrderItems (OrderId, ProductId, ProductName, VariantLabel, UnitPrice, Quantity, CreatedAt, IsDeleted)
    VALUES (@O2, @ProdAventus, N'Creed Aventus Eau de Parfum', N'Fullbox 100ml', 7800000, 1, DATEADD(DAY, -8, GETUTCDATE()), 0);

    -- Đơn 3: Đã giao thành công (Delivered)
    INSERT INTO Orders (OrderCode, CustomerName, CustomerPhone, CustomerEmail, ShippingAddress, City, OrderNotes, SubTotal, DiscountAmount, ShippingFee, TotalAmount, PaymentMethod, PaymentStatus, OrderStatus, TrackingNumber, CreatedAt, IsDeleted)
    VALUES ('PF-20261005-9932', N'Lê Minh Châu', '0933221100', 'chau.le@yahoo.com', N'45 Lê Duẩn, Hải Châu', N'Đà Nẵng', NULL, 3650000, 100000, 0, 3550000, 1, 2, 4, 'JNT-9932451', DATEADD(DAY, -6, GETUTCDATE()), 0);
    DECLARE @O3 INT = SCOPE_IDENTITY();
    INSERT INTO OrderItems (OrderId, ProductId, ProductName, VariantLabel, UnitPrice, Quantity, CreatedAt, IsDeleted)
    VALUES (@O3, @ProdBDC, N'Bleu de Chanel Parfum', N'Fullbox 100ml', 3650000, 1, DATEADD(DAY, -6, GETUTCDATE()), 0);

    -- Đơn 4: Đang vận chuyển (Shipping)
    INSERT INTO Orders (OrderCode, CustomerName, CustomerPhone, CustomerEmail, ShippingAddress, City, OrderNotes, SubTotal, DiscountAmount, ShippingFee, TotalAmount, PaymentMethod, PaymentStatus, OrderStatus, TrackingNumber, CreatedAt, IsDeleted)
    VALUES ('PF-20261007-1182', N'Võ Hoàng Quân', '0977665544', 'quan.vo@gmail.com', N'22 Điện Biên Phủ, Ba Đình', N'Hà Nội', N'Gọi trước khi giao 15p', 6800000, 0, 0, 6800000, 2, 2, 3, 'SPX-1182765', DATEADD(DAY, -4, GETUTCDATE()), 0);
    DECLARE @O4 INT = SCOPE_IDENTITY();
    INSERT INTO OrderItems (OrderId, ProductId, ProductName, VariantLabel, UnitPrice, Quantity, CreatedAt, IsDeleted)
    VALUES (@O4, @ProdTomFordTV, N'Tom Ford Tobacco Vanille Eau de Parfum', N'Fullbox 50ml', 6800000, 1, DATEADD(DAY, -4, GETUTCDATE()), 0);

    -- Đơn 5: Đang vận chuyển (Shipping)
    INSERT INTO Orders (OrderCode, CustomerName, CustomerPhone, CustomerEmail, ShippingAddress, City, OrderNotes, SubTotal, DiscountAmount, ShippingFee, TotalAmount, PaymentMethod, PaymentStatus, OrderStatus, TrackingNumber, CreatedAt, IsDeleted)
    VALUES ('PF-20261008-6623', N'Đỗ Bích Trâm', '0909123456', 'tram.do@gmail.com', N'15 Thảo Điền, TP. Thủ Đức', N'TP. Hồ Chí Minh', NULL, 3800000, 380000, 0, 3420000, 4, 2, 3, 'GHN-6623884', DATEADD(DAY, -3, GETUTCDATE()), 0);
    DECLARE @O5 INT = SCOPE_IDENTITY();
    INSERT INTO OrderItems (OrderId, ProductId, ProductName, VariantLabel, UnitPrice, Quantity, CreatedAt, IsDeleted)
    VALUES (@O5, ISNULL(@P_Libre, @ProdBDC), N'Yves Saint Laurent Libre Eau de Parfum', N'Fullbox 90ml', 3800000, 1, DATEADD(DAY, -3, GETUTCDATE()), 0);

    -- Đơn 6: Đã xác nhận chuẩn bị hàng (Confirmed)
    INSERT INTO Orders (OrderCode, CustomerName, CustomerPhone, CustomerEmail, ShippingAddress, City, OrderNotes, SubTotal, DiscountAmount, ShippingFee, TotalAmount, PaymentMethod, PaymentStatus, OrderStatus, CreatedAt, IsDeleted)
    VALUES ('PF-20261009-3351', N'Phan Đình Phùng', '0918776655', 'phung.phan@gmail.com', N'67 Trần Hưng Đạo, Ninh Kiều', N'Cần Thơ', N'Đơn chiết dùng thử trải nghiệm', 1270000, 0, 30000, 1300000, 1, 1, 2, DATEADD(DAY, -2, GETUTCDATE()), 0);
    DECLARE @O6 INT = SCOPE_IDENTITY();
    INSERT INTO OrderItems (OrderId, ProductId, ProductName, VariantLabel, UnitPrice, Quantity, CreatedAt, IsDeleted)
    VALUES 
    (@O6, @ProdDiorSE, N'Dior Sauvage Elixir', N'Chiết 10ml', 690000, 1, DATEADD(DAY, -2, GETUTCDATE()), 0),
    (@O6, @ProdBDC, N'Bleu de Chanel Parfum', N'Chiết 10ml', 480000, 1, DATEADD(DAY, -2, GETUTCDATE()), 0);

    -- Đơn 7: Chờ xử lý (Pending)
    INSERT INTO Orders (OrderCode, CustomerName, CustomerPhone, CustomerEmail, ShippingAddress, City, OrderNotes, SubTotal, DiscountAmount, ShippingFee, TotalAmount, PaymentMethod, PaymentStatus, OrderStatus, CreatedAt, IsDeleted)
    VALUES ('PF-20261010-4498', N'Lý Huệ Mẫn', '0938445566', 'hueman.ly@gmail.com', N'90 Pasteur, Bến Nghé, Quận 1', N'TP. Hồ Chí Minh', N'Giao hàng cuối tuần này', 4250000, 0, 0, 4250000, 1, 1, 1, DATEADD(DAY, -1, GETUTCDATE()), 0);
    DECLARE @O7 INT = SCOPE_IDENTITY();
    INSERT INTO OrderItems (OrderId, ProductId, ProductName, VariantLabel, UnitPrice, Quantity, CreatedAt, IsDeleted)
    VALUES (@O7, ISNULL(@P_No5, @ProdBDC), N'Chanel No.5 Eau de Parfum', N'Fullbox 100ml', 4250000, 1, DATEADD(DAY, -1, GETUTCDATE()), 0);

    -- Đơn 8: Chờ xử lý (Pending)
    INSERT INTO Orders (OrderCode, CustomerName, CustomerPhone, CustomerEmail, ShippingAddress, City, OrderNotes, SubTotal, DiscountAmount, ShippingFee, TotalAmount, PaymentMethod, PaymentStatus, OrderStatus, CreatedAt, IsDeleted)
    VALUES ('PF-20261011-5512', N'Đặng Quốc Việt', '0945678901', 'viet.dq@gmail.com', N'102 Hoàng Hoa Thám, Tây Hồ', N'Hà Nội', N'Cần xuất hóa đơn VAT', 6800000, 500000, 0, 6300000, 2, 2, 1, GETUTCDATE(), 0);
    DECLARE @O8 INT = SCOPE_IDENTITY();
    INSERT INTO OrderItems (OrderId, ProductId, ProductName, VariantLabel, UnitPrice, Quantity, CreatedAt, IsDeleted)
    VALUES (@O8, ISNULL(@P_Santal, @ProdAventus), N'Le Labo Santal 33 Eau de Parfum', N'Fullbox 100ml', 6800000, 1, GETUTCDATE(), 0);
END

PRINT N'>>> SEED DỮ LIỆU MỞ RỘNG (EXTENDED DATA) THÀNH CÔNG RỰC RỠ! <<<';
