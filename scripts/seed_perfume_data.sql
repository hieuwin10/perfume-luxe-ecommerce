-- =========================================================================================
-- PERFUME LUXE - INITIAL DATA SEED SCRIPT
-- Thiết kế độc lập với mã nguồn C# - Có thể thực thi trực tiếp qua SSMS hoặc Web Database Console
-- =========================================================================================

SET NOCOUNT ON;

-- 1. SEED BRANDS (Thương hiệu nước hoa hàng đầu thế giới)
IF NOT EXISTS (SELECT 1 FROM Brands WHERE Slug = 'dior')
BEGIN
    INSERT INTO Brands (Name, Slug, Country, Description, LogoUrl, IsFeatured, CreatedAt, IsDeleted)
    VALUES 
    (N'Christian Dior', 'dior', N'Pháp', N'Biểu tượng thời trang và nước hoa xa xỉ phong cách quý tộc Pháp từ năm 1947.', '/images/brands/dior.svg', 1, GETUTCDATE(), 0),
    (N'Chanel', 'chanel', N'Pháp', N'Đỉnh cao sự thanh lịch vượt thời gian, sở hữu những kiệt tác mùi hương huyền thoại thế giới.', '/images/brands/chanel.svg', 1, GETUTCDATE(), 0),
    (N'Creed', 'creed', N'Anh / Pháp', N'Nhà chế tác nước hoa hoàng gia lừng danh từ năm 1760 với những công thức độc quyền.', '/images/brands/creed.svg', 1, GETUTCDATE(), 0),
    (N'Tom Ford', 'tom-ford', N'Mỹ', N'Định nghĩa sự gợi cảm, táo bạo và đẳng cấp tột bậc trong thế giới nước hoa Niche cao cấp.', '/images/brands/tomford.svg', 1, GETUTCDATE(), 0),
    (N'Le Labo', 'le-labo', N'Mỹ', N'Thương hiệu nước hoa Niche thủ công phong cách phòng thí nghiệm New York đầy cá tính.', '/images/brands/lelabo.svg', 1, GETUTCDATE(), 0),
    (N'Maison Margiela', 'maison-margiela', N'Pháp', N'Dòng Replica tái hiện những ký ức và khoảnh khắc thân quen qua từng nốt hương.', '/images/brands/margiela.svg', 1, GETUTCDATE(), 0),
    (N'Kilian', 'kilian', N'Pháp', N'Đỉnh cao nước hoa bóng đêm lấy cảm hứng từ những thùng rượu cognac ủ lâu năm.', '/images/brands/kilian.svg', 1, GETUTCDATE(), 0),
    (N'Yves Saint Laurent', 'ysl', N'Pháp', N'Tuyên ngôn tự do và quyến rũ không giới hạn dành cho người phụ nữ và quý ông hiện đại.', '/images/brands/ysl.svg', 1, GETUTCDATE(), 0);
END
GO

-- 2. SEED CATEGORIES (Danh mục phân loại)
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'nuoc-hoa-nam')
BEGIN
    INSERT INTO Categories (Name, Slug, Description, Gender, IsFeatured, CreatedAt, IsDeleted)
    VALUES
    (N'Nước Hoa Nam', 'nuoc-hoa-nam', N'Bộ sưu tập nước hoa nam tính, lịch lãm, lôi cuốn và mạnh mẽ.', 2, 1, GETUTCDATE(), 0),
    (N'Nước Hoa Nữ', 'nuoc-hoa-nu', N'Những tuyệt tác hương thơm thanh lịch, ngọt ngào và quyến rũ dành cho phái đẹp.', 3, 1, GETUTCDATE(), 0),
    (N'Nước Hoa Unisex', 'nuoc-hoa-unisex', N'Mùi hương phi giới tính độc đáo phá vỡ mọi khuôn mẫu truyền thống.', 1, 1, GETUTCDATE(), 0),
    (N'Nước Hoa Niche', 'nuoc-hoa-niche', N'Nước hoa thủ công cao cấp với nốt hương hiếm và số lượng sản xuất giới hạn.', NULL, 1, GETUTCDATE(), 0),
    (N'Nước Hoa Chiết 10ml', 'nuoc-hoa-chiet', N'Dung tích chiết tiện lợi trải nghiệm nhiều mùi hương với chi phí tiết kiệm.', NULL, 1, GETUTCDATE(), 0);
END
GO

-- 3. SEED FRAGRANCE FAMILIES (Nhóm hương)
IF NOT EXISTS (SELECT 1 FROM FragranceFamilies WHERE Slug = 'huong-go-woody')
BEGIN
    INSERT INTO FragranceFamilies (Name, Slug, Description, ColorHex, ScentType, CreatedAt, IsDeleted)
    VALUES
    (N'Hương Gỗ (Woody)', 'huong-go-woody', N'Trầm ấm, chững chạc, mang chiều sâu của gỗ tuyết tùng, đàn hương và trầm hương.', '#8B5A2B', 1, GETUTCDATE(), 0),
    (N'Hương Hoa Cỏ (Floral)', 'huong-go-floral', N'Ngọt ngào, nhẹ nhàng và lãng mạn từ hoa hồng Damask, nhài trắng, mẫu đơn.', '#E56B6F', 2, GETUTCDATE(), 0),
    (N'Hương Phương Đông (Oriental)', 'huong-phuong-dong', N'Ấm áp, bí ẩn, nồng nàn với hổ phách, vani bourbon và quế cay nồng.', '#B8860B', 3, GETUTCDATE(), 0),
    (N'Hương Tươi Mát & Cam Chanh (Fresh & Citrus)', 'huong-tuoi-mat', N'Sảng khoái, tràn đầy năng lượng với bergamot, bưởi hồng và gió biển.', '#2A9D8F', 4, GETUTCDATE(), 0),
    (N'Hương Ngọt Ẩm Thực (Gourmand)', 'huong-gourmand', N'Quyến rũ khó cưỡng từ socola đen, cà phê hạt, kẹo caramel và mật ong.', '#6F4E37', 6, GETUTCDATE(), 0),
    (N'Hương Da Thuộc (Leather)', 'huong-da-thuoc', N'Mạnh mẽ, phong trần và cá tính như áo khoác da và yên xe cổ điển.', '#4A3B32', 7, GETUTCDATE(), 0);
END
GO

-- 4. SEED FRAGRANCE NOTES (Các nốt hương chi tiết)
IF NOT EXISTS (SELECT 1 FROM FragranceNotes WHERE Slug = 'cam-bergamot')
BEGIN
    INSERT INTO FragranceNotes (Name, Slug, Description, CreatedAt, IsDeleted)
    VALUES
    (N'Cam Bergamot', 'cam-bergamot', N'Nốt hương cam chanh thanh mát, tươi sáng nổi tiếng của vùng Calabria, Ý.', GETUTCDATE(), 0),
    (N'Hạt Tiêu Tứ Xuyên', 'tieu-tu-xuyen', N'Cay nhẹ, ấm áp kích thích khứu giác ngay từ giây đầu tiên.', GETUTCDATE(), 0),
    (N'Dứa Nướng (Pineapple)', 'dua-nuong', N'Nốt ngọt trái cây nhiệt đới tươi mới pha chút vị khói đặc trưng.', GETUTCDATE(), 0),
    (N'Gỗ Tuyết Tùng (Cedarwood)', 'tuyet-tung', N'Hương gỗ khô ráo, nam tính, bền bỉ và tạo sự tin cậy.', GETUTCDATE(), 0),
    (N'Gỗ Đàn Hương (Sandalwood)', 'dan-huong', N'Hương gỗ béo ngậy, mịn màng và thanh tịnh tâm trí.', GETUTCDATE(), 0),
    (N'Trầm Hương (Oud)', 'tram-huong', N'Vua của các loại gỗ, huyền bí, quyền lực và đắt đỏ bậc nhất thế giới.', GETUTCDATE(), 0),
    (N'Vani Bourbon', 'vani-bourbon', N'Hương vani ngọt ấm, sâu sắc và lưu lại bền lâu trên da.', GETUTCDATE(), 0),
    (N'Long Diên Hương (Ambroxan)', 'long-dien-huong', N'Khoáng chất biển cả, tạo hiệu ứng tỏa hương xa cực kỳ quyến rũ.', GETUTCDATE(), 0),
    (N'Rượu Rum & Thuốc Lá', 'rum-thuoc-la', N'Nốt hương say đắm của một đêm nhạc Jazz cổ điển bên ly cocktail.', GETUTCDATE(), 0),
    (N'Hoa Hồng Damask', 'hoa-hong-damask', N'Nữ hoàng các loài hoa với hương thơm nồng nàn, quý phái tuyệt hảo.', GETUTCDATE(), 0),
    (N'Hoa Nhài Sambac', 'hoa-nhai-sambac', N'Thanh khiết, nữ tính và lan tỏa dịu dàng trong không gian đêm.', GETUTCDATE(), 0),
    (N'Hoa Oải Hương (Lavender)', 'oai-huong', N'Hương thảo mộc thư thái, sạch sẽ phong cách tiệm cắt tóc quý ông Pháp.', GETUTCDATE(), 0);
END
GO

-- 5. SEED PRODUCTS (Sản phẩm nước hoa kinh điển)
DECLARE @BrandDior INT = (SELECT TOP 1 Id FROM Brands WHERE Slug = 'dior');
DECLARE @BrandChanel INT = (SELECT TOP 1 Id FROM Brands WHERE Slug = 'chanel');
DECLARE @BrandCreed INT = (SELECT TOP 1 Id FROM Brands WHERE Slug = 'creed');
DECLARE @BrandTomFord INT = (SELECT TOP 1 Id FROM Brands WHERE Slug = 'tom-ford');
DECLARE @BrandLeLabo INT = (SELECT TOP 1 Id FROM Brands WHERE Slug = 'le-labo');
DECLARE @BrandMargiela INT = (SELECT TOP 1 Id FROM Brands WHERE Slug = 'maison-margiela');
DECLARE @BrandKilian INT = (SELECT TOP 1 Id FROM Brands WHERE Slug = 'kilian');

DECLARE @CatMen INT = (SELECT TOP 1 Id FROM Categories WHERE Slug = 'nuoc-hoa-nam');
DECLARE @CatWomen INT = (SELECT TOP 1 Id FROM Categories WHERE Slug = 'nuoc-hoa-nu');
DECLARE @CatUnisex INT = (SELECT TOP 1 Id FROM Categories WHERE Slug = 'nuoc-hoa-unisex');

DECLARE @FamWoody INT = (SELECT TOP 1 Id FROM FragranceFamilies WHERE Slug = 'huong-go-woody');
DECLARE @FamOriental INT = (SELECT TOP 1 Id FROM FragranceFamilies WHERE Slug = 'huong-phuong-dong');
DECLARE @FamFresh INT = (SELECT TOP 1 Id FROM FragranceFamilies WHERE Slug = 'huong-tuoi-mat');
DECLARE @FamGourmand INT = (SELECT TOP 1 Id FROM FragranceFamilies WHERE Slug = 'huong-gourmand');

IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = 'dior-sauvage-elixir')
BEGIN
    -- 1. Dior Sauvage Elixir
    INSERT INTO Products (Name, Slug, Sku, ShortDescription, FullDescription, BrandId, CategoryId, FragranceFamilyId, Gender, Concentration, Longevity, Sillage, ReleaseYear, SeasonRecommendation, BasePrice, MainImageUrl, IsFeatured, IsBestSeller, IsNewArrival, IsInStock, RatingAverage, RatingCount, ViewCount, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Dior Sauvage Elixir Parfum',
        'dior-sauvage-elixir',
        'DIOR-SE-001',
        N'Bản cô đặc đỉnh cao với hương gỗ cay nồng mãnh liệt và quyền lực phi thường.',
        N'Dior Sauvage Elixir tái hiện lại DNA kinh điển của dòng Sauvage nhưng nâng tầm lên một đẳng cấp hoàn toàn mới. Nồng độ tinh dầu cực cao kết hợp giữa gia vị ấm nồng, hoa oải hương tươi tắn và hương gỗ phong phú mang lại sức hút thôi miên đối phương.',
        @BrandDior, @CatMen, @FamOriental, 2, 4, N'Trên 12 giờ', N'Tỏa hương xa trên 2 mét', 2021, N'Thu, Đông, Đêm tiệc',
        3850000, 'https://images.unsplash.com/photo-1594035910387-fea47794261f?auto=format&fit=crop&w=800&q=80',
        1, 1, 0, 1, 4.9, 128, 1250,
        N'Dior Sauvage Elixir Parfum Chính Hãng | Nước Hoa Nam Đỉnh Cao',
        N'Mua nước hoa Dior Sauvage Elixir Parfum chính hãng. Hương gỗ cay nồng sang trọng, lưu hương trên 12h, cam kết 100% chuẩn Pháp.',
        GETUTCDATE(), 0
    );

    -- 2. Bleu de Chanel Parfum
    INSERT INTO Products (Name, Slug, Sku, ShortDescription, FullDescription, BrandId, CategoryId, FragranceFamilyId, Gender, Concentration, Longevity, Sillage, ReleaseYear, SeasonRecommendation, BasePrice, MainImageUrl, IsFeatured, IsBestSeller, IsNewArrival, IsInStock, RatingAverage, RatingCount, ViewCount, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Bleu de Chanel Parfum',
        'bleu-de-chanel-parfum',
        'CHANEL-BDC-002',
        N'Hương gỗ thơm quý tộc, biểu tượng của sự tự do và bản lĩnh phái mạnh.',
        N'Bleu de Chanel Parfum thể hiện sự tinh túy thuần khiết nhất của dòng Bleu. Mở đầu bằng sự tươi mát rực rỡ của cam chanh, nhanh chóng chuyển sang nốt hương gỗ đàn hương New Caledonia ấm áp và sâu lắng vô cùng sang trọng.',
        @BrandChanel, @CatMen, @FamWoody, 2, 4, N'10 - 12 giờ', N'Tỏa hương trong bán kính 1 - 2 mét', 2018, N'Bốn mùa, Mọi hoàn cảnh',
        3650000, 'https://images.unsplash.com/photo-1523293182086-7651a899d37f?auto=format&fit=crop&w=800&q=80',
        1, 1, 0, 1, 4.8, 95, 980,
        N'Bleu de Chanel Parfum Chính Hãng | Biểu Tượng Lịch Lãm',
        N'Nước hoa Bleu de Chanel Parfum chính hãng - nồng độ tinh dầu cao nhất dòng Bleu, sang trọng và chuẩn mực quý ông.',
        GETUTCDATE(), 0
    );

    -- 3. Creed Aventus
    INSERT INTO Products (Name, Slug, Sku, ShortDescription, FullDescription, BrandId, CategoryId, FragranceFamilyId, Gender, Concentration, Longevity, Sillage, ReleaseYear, SeasonRecommendation, BasePrice, MainImageUrl, IsFeatured, IsBestSeller, IsNewArrival, IsInStock, RatingAverage, RatingCount, ViewCount, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Creed Aventus Millesime EDP',
        'creed-aventus-edp',
        'CREED-AV-003',
        N'Ông hoàng nước hoa nam với nốt hương dứa nướng khói phong trần kiêu hãnh.',
        N'Lấy cảm hứng từ cuộc đời oanh liệt của Hoàng đế Napoleon, Creed Aventus là sự tôn vinh sức mạnh, tầm nhìn và thành công. Sự kết hợp độc nhất giữa quả dứa tươi, khói bạch dương và long diên hương tạo nên một mùi hương hoàng gia không thể trộn lẫn.',
        @BrandCreed, @CatMen, @FamFresh, 2, 3, N'8 - 10 giờ', N'Tỏa hương xa 2 mét', 2010, N'Xuân, Hạ, Thu, Hội nghị & Sự kiện',
        7800000, 'https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?auto=format&fit=crop&w=800&q=80',
        1, 1, 0, 1, 4.9, 210, 2450,
        N'Creed Aventus Millesime EDP Hoàng Gia Anh | Nước Hoa Niche Cao Cấp',
        N'Sở hữu ngay Creed Aventus EDP - Biểu tượng đế vương của phái mạnh với nốt dứa khói trứ danh. Hàng chính hãng đầy đủ tem hộp.',
        GETUTCDATE(), 0
    );

    -- 4. Tom Ford Tobacco Vanille
    INSERT INTO Products (Name, Slug, Sku, ShortDescription, FullDescription, BrandId, CategoryId, FragranceFamilyId, Gender, Concentration, Longevity, Sillage, ReleaseYear, SeasonRecommendation, BasePrice, MainImageUrl, IsFeatured, IsBestSeller, IsNewArrival, IsInStock, RatingAverage, RatingCount, ViewCount, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Tom Ford Tobacco Vanille EDP',
        'tom-ford-tobacco-vanille',
        'TF-TV-004',
        N'Hơi thở quý tộc Anh với lá thuốc lá thơm nồng quyện cùng vani ngọt ấm.',
        N'Tobacco Vanille mở ra không gian của một câu lạc bộ quý ông London cổ điển. Mùi lá thuốc lá hảo hạng hòa cùng vani béo ngậy, gừng cay và hoa quả khô tạo nên trải nghiệm ấm áp và ngây ngất tột độ trong những đêm đông lạnh giá.',
        @BrandTomFord, @CatUnisex, @FamGourmand, 1, 3, N'Trên 12 giờ', N'Tỏa hương xa trên 2 mét', 2007, N'Thu, Đông, Đêm tiệc xa hoa',
        6800000, 'https://images.unsplash.com/photo-1547887537-6158d64c35b3?auto=format&fit=crop&w=800&q=80',
        1, 0, 0, 1, 4.9, 74, 820,
        N'Tom Ford Tobacco Vanille EDP | Nước Hoa Niche Đẳng Cấp',
        N'Tom Ford Tobacco Vanille hương lá thuốc lá và vani nồng nàn. Mùi hương ấm áp xa xỉ bậc nhất cho mùa thu đông.',
        GETUTCDATE(), 0
    );

    -- 5. Maison Margiela Replica Jazz Club
    INSERT INTO Products (Name, Slug, Sku, ShortDescription, FullDescription, BrandId, CategoryId, FragranceFamilyId, Gender, Concentration, Longevity, Sillage, ReleaseYear, SeasonRecommendation, BasePrice, MainImageUrl, IsFeatured, IsBestSeller, IsNewArrival, IsInStock, RatingAverage, RatingCount, ViewCount, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Maison Margiela Replica Jazz Club EDT',
        'replica-jazz-club',
        'MM-JC-005',
        N'Giai điệu Jazz ấm cúng bên ly rượu rum sóng sánh và điếu xì gà thượng hạng.',
        N'Brooklyn, năm 2013 - Replica Jazz Club lưu giữ bầu không khí trầm lắng của một quán bar Jazz huyền thoại. Nốt hương rượu Rum nồng say, khói thuốc lá êm dịu và đậu tonka gợi cảm mang lại cảm giác thư thái nhưng đầy cuốn hút.',
        @BrandMargiela, @CatUnisex, @FamOriental, 1, 2, N'7 - 9 giờ', N'Tỏa hương thoang thoảng 1 mét', 2013, N'Thu, Đông, Hẹn hò lãng mạn',
        3300000, 'https://images.unsplash.com/photo-1615397349754-cfa2066a298e?auto=format&fit=crop&w=800&q=80',
        1, 0, 1, 1, 4.7, 88, 760,
        N'Replica Jazz Club Maison Margiela Chính Hãng | Giai Điệu Rượu Rum & Thuốc Lá',
        N'Trải nghiệm không gian Jazz Club với hương rượu Rum và nốt thuốc lá ấm cúng. Thiết kế lọ thủy tinh vintage đậm chất nghệ thuật.',
        GETUTCDATE(), 0
    );

    -- 6. Kilian Angels'' Share
    INSERT INTO Products (Name, Slug, Sku, ShortDescription, FullDescription, BrandId, CategoryId, FragranceFamilyId, Gender, Concentration, Longevity, Sillage, ReleaseYear, SeasonRecommendation, BasePrice, MainImageUrl, IsFeatured, IsBestSeller, IsNewArrival, IsInStock, RatingAverage, RatingCount, ViewCount, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Kilian Angels'' Share EDP',
        'kilian-angels-share',
        'KIL-AS-006',
        N'Mùi hương của rượu Cognac ủ thùng sồi, quế cay và kẹo praline tan chảy.',
        N'Được sáng tạo bởi Kilian Hennessy - hậu duệ đời thứ 8 của đế chế rượu cognac lừng danh Hennessy. Angels'' Share là phần rượu bốc hơi lên thiên đường trong quá trình ủ. Mùi hương như một ly rượu cognac quý tộc sóng sánh trong ly pha lê lấp lánh.',
        @BrandKilian, @CatUnisex, @FamGourmand, 1, 3, N'10 - 12 giờ', N'Tỏa hương xa 2 mét', 2020, N'Thu, Đông, Tiệc tối sang trọng',
        5900000, 'https://images.unsplash.com/photo-1595425970377-c9703cf48b6d?auto=format&fit=crop&w=800&q=80',
        1, 0, 1, 1, 4.9, 115, 1100,
        N'Kilian Angels'' Share EDP | Nước Hoa Rượu Cognac Xa Xỉ',
        N'Kilian Angels'' Share EDP chính hãng từ Pháp. Hương rượu cognac kết hợp gỗ sồi và quế ấm áp cho mùa lễ hội.',
        GETUTCDATE(), 0
    );
END
GO

-- 6. SEED PRODUCT VARIANTS (Dung tích & Biến thể)
DECLARE @P1 INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'dior-sauvage-elixir');
DECLARE @P2 INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'bleu-de-chanel-parfum');
DECLARE @P3 INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'creed-aventus-edp');
DECLARE @P4 INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'tom-ford-tobacco-vanille');
DECLARE @P5 INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'replica-jazz-club');
DECLARE @P6 INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'kilian-angels-share');

IF @P1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductVariants WHERE ProductId = @P1)
BEGIN
    INSERT INTO ProductVariants (ProductId, VolumeMl, VolumeLabel, VariantSku, OriginalPrice, SalePrice, StockQuantity, IsDefault, CreatedAt, IsDeleted)
    VALUES
    (@P1, 10, N'Chiết 10ml', 'DIOR-SE-10ML', 750000, 690000, 50, 0, GETUTCDATE(), 0),
    (@P1, 60, N'Fullbox 60ml', 'DIOR-SE-60ML', 4200000, 3850000, 20, 1, GETUTCDATE(), 0),
    (@P1, 100, N'Fullbox 100ml', 'DIOR-SE-100ML', 5500000, 4990000, 15, 0, GETUTCDATE(), 0);

    INSERT INTO ProductVariants (ProductId, VolumeMl, VolumeLabel, VariantSku, OriginalPrice, SalePrice, StockQuantity, IsDefault, CreatedAt, IsDeleted)
    VALUES
    (@P2, 10, N'Chiết 10ml', 'BDC-10ML', 520000, 480000, 60, 0, GETUTCDATE(), 0),
    (@P2, 50, N'Fullbox 50ml', 'BDC-50ML', 3300000, 2990000, 25, 0, GETUTCDATE(), 0),
    (@P2, 100, N'Fullbox 100ml', 'BDC-100ML', 3900000, 3650000, 30, 1, GETUTCDATE(), 0);

    INSERT INTO ProductVariants (ProductId, VolumeMl, VolumeLabel, VariantSku, OriginalPrice, SalePrice, StockQuantity, IsDefault, CreatedAt, IsDeleted)
    VALUES
    (@P3, 10, N'Chiết 10ml', 'AV-10ML', 950000, 890000, 40, 0, GETUTCDATE(), 0),
    (@P3, 100, N'Fullbox 100ml', 'AV-100ML', 8500000, 7800000, 12, 1, GETUTCDATE(), 0);

    INSERT INTO ProductVariants (ProductId, VolumeMl, VolumeLabel, VariantSku, OriginalPrice, SalePrice, StockQuantity, IsDefault, CreatedAt, IsDeleted)
    VALUES
    (@P4, 10, N'Chiết 10ml', 'TV-10ML', 820000, 750000, 35, 0, GETUTCDATE(), 0),
    (@P4, 50, N'Fullbox 50ml', 'TV-50ML', 7200000, 6800000, 10, 1, GETUTCDATE(), 0);

    INSERT INTO ProductVariants (ProductId, VolumeMl, VolumeLabel, VariantSku, OriginalPrice, SalePrice, StockQuantity, IsDefault, CreatedAt, IsDeleted)
    VALUES
    (@P5, 10, N'Chiết 10ml', 'JC-10ML', 450000, 390000, 45, 0, GETUTCDATE(), 0),
    (@P5, 100, N'Fullbox 100ml', 'JC-100ML', 3600000, 3300000, 18, 1, GETUTCDATE(), 0);

    INSERT INTO ProductVariants (ProductId, VolumeMl, VolumeLabel, VariantSku, OriginalPrice, SalePrice, StockQuantity, IsDefault, CreatedAt, IsDeleted)
    VALUES
    (@P6, 10, N'Chiết 10ml', 'AS-10ML', 780000, 720000, 30, 0, GETUTCDATE(), 0),
    (@P6, 50, N'Fullbox 50ml', 'AS-50ML', 6300000, 5900000, 14, 1, GETUTCDATE(), 0);
END
GO

-- 7. SEED PRODUCT FRAGRANCE NOTES (Liên kết 3 tầng hương chuẩn xác)
DECLARE @P1 INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'dior-sauvage-elixir');
DECLARE @N_Bergamot INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'cam-bergamot');
DECLARE @N_Pepper INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'tieu-tu-xuyen');
DECLARE @N_Lavender INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'oai-huong');
DECLARE @N_Cedar INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'tuyet-tung');
DECLARE @N_Sandal INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'dan-huong');
DECLARE @N_Ambroxan INT = (SELECT TOP 1 Id FROM FragranceNotes WHERE Slug = 'long-dien-huong');

IF @P1 IS NOT NULL AND @N_Pepper IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProductFragranceNotes WHERE ProductId = @P1)
BEGIN
    -- Dior Sauvage Elixir: Top = Tiêu, Nhục đậu khấu; Heart = Oải hương Pháp; Base = Gỗ đàn hương, Hổ phách
    INSERT INTO ProductFragranceNotes (ProductId, FragranceNoteId, NoteLevel, DisplayOrder)
    VALUES
    (@P1, @N_Pepper, 1, 1),    -- Hương đầu: Tiêu ấm
    (@P1, @N_Bergamot, 1, 2),  -- Hương đầu: Cam Bergamot
    (@P1, @N_Lavender, 2, 1),  -- Hương giữa: Oải hương
    (@P1, @N_Sandal, 3, 1),    -- Hương cuối: Đàn hương
    (@P1, @N_Cedar, 3, 2);     -- Hương cuối: Tuyết tùng
END
GO

-- 8. SEED VOUCHERS (Mã giảm giá ưu đãi)
IF NOT EXISTS (SELECT 1 FROM Vouchers WHERE Code = 'LUXE2026')
BEGIN
    INSERT INTO Vouchers (Code, Description, DiscountPercent, DiscountAmount, MinOrderAmount, MaxDiscountAmount, UsageLimit, UsedCount, ValidFrom, ValidTo, IsActive, CreatedAt, IsDeleted)
    VALUES
    ('LUXE2026', N'Ưu đãi đặc quyền giảm 10% cho thành viên mới', 10.00, NULL, 1500000, 500000, 200, 14, GETUTCDATE(), DATEADD(MONTH, 6, GETUTCDATE()), 1, GETUTCDATE(), 0),
    ('WELCOME', N'Tặng 100.000₫ cho đơn hàng đầu tiên từ 1.000.000₫', NULL, 100000, 1000000, 100000, 500, 42, GETUTCDATE(), DATEADD(MONTH, 3, GETUTCDATE()), 1, GETUTCDATE(), 0),
    ('FREESHIP', N'Miễn phí vận chuyển toàn quốc cho đơn hàng từ 2.000.000₫', NULL, 40000, 2000000, 40000, 1000, 89, GETUTCDATE(), DATEADD(MONTH, 12, GETUTCDATE()), 1, GETUTCDATE(), 0);
END
GO

-- 9. SEED BLOG POSTS (Cẩm nang kiến thức & SEO bài viết)
IF NOT EXISTS (SELECT 1 FROM BlogCategories WHERE Slug = 'kien-thuc-nuoc-hoa')
BEGIN
    INSERT INTO BlogCategories (Name, Slug, Description, CreatedAt, IsDeleted)
    VALUES
    (N'Kiến Thức Nước Hoa', 'kien-thuc-nuoc-hoa', N'Cẩm nang chuyên sâu về các tầng hương, nồng độ và văn hóa mùi hương.', GETUTCDATE(), 0),
    (N'Xu Hướng Mùi Hương', 'xu-huong-mui-huong', N'Cập nhật những chai nước hoa thịnh hành và bảng xếp hạng mùi hương.', GETUTCDATE(), 0);
END
GO

DECLARE @CatBlog1 INT = (SELECT TOP 1 Id FROM BlogCategories WHERE Slug = 'kien-thuc-nuoc-hoa');

IF @CatBlog1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = '3-tang-huong-trong-nuoc-hoa')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES
    (
        N'Kim Tự Tháp Mùi Hương: Giải Mã 3 Tầng Hương Trong Nước Hoa',
        '3-tang-huong-trong-nuoc-hoa',
        N'Tại sao một chai nước hoa lại có thể biến đổi kỳ diệu từ khi mới xịt cho đến cuối ngày? Cùng giải mã bí mật của Top, Heart và Base notes.',
        N'<p>Mỗi tác phẩm nước hoa cao cấp giống như một bản giao hưởng được chia thành ba chương hồi rõ rệt: Hương đầu (Top notes), Hương giữa (Heart notes) và Hương cuối (Base notes)...</p>',
        'https://images.unsplash.com/photo-1547887537-6158d64c35b3?auto=format&fit=crop&w=800&q=80',
        N'Chuyên Gia Mùi Hương', 1450, 1, @CatBlog1,
        N'Giải Mã 3 Tầng Hương Trong Nước Hoa | Cẩm Nang Perfume Luxe',
        N'Khám phá bí mật cấu trúc 3 tầng hương trong nước hoa cao cấp: Hương đầu, hương giữa và hương cuối hoạt động như thế nào trên da.',
        GETUTCDATE(), 0
    );
END
GO

PRINT N'>>> SEED DỮ LIỆU NƯỚC HOA HOÀN TẤT THÀNH CÔNG! <<<';
