-- =========================================================================================
-- PERFUME LUXE - SEED REMAINING REVIEWS
-- =========================================================================================
SET NOCOUNT ON;

DECLARE @ProdAventus INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'creed-aventus-edp');
DECLARE @ProdDiorSE INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'dior-sauvage-elixir');
DECLARE @ProdBDC INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'bleu-de-chanel-parfum');
DECLARE @P_Santal INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'le-labo-santal-33');
DECLARE @P_Libre INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'ysl-libre-edp');
DECLARE @P_No5 INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'chanel-no-5-edp');
DECLARE @P_CocoMad INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'chanel-coco-mademoiselle-edp');
DECLARE @P_LostCherry INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'tom-ford-lost-cherry');
DECLARE @P_LazySunday INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'replica-lazy-sunday-morning');

-- Reviews cho Creed Aventus
IF @ProdAventus IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reviews WHERE ProductId = @ProdAventus)
BEGIN
    INSERT INTO Reviews (ProductId, UserName, Rating, Comment, LongevityRating, IsApproved, CreatedAt, IsDeleted)
    VALUES
    (@ProdAventus, N'Hoàng Gia Bảo', 5, N'Mùi hương đỉnh của chóp! Mùi dứa nướng quyện khói rêu sồi cực kỳ nam tính và quyền lực. Đáng từng đồng bỏ ra.', N'Rất lâu (trên 10h)', 1, DATEADD(DAY, -18, GETUTCDATE()), 0),
    (@ProdAventus, N'Đặng Thanh Tùng', 5, N'Giao hàng hỏa tốc trong 2 tiếng tại Hà Nội, nhân viên tư vấn rất am hiểu nước hoa Niche. Sẽ ủng hộ shop dài lâu.', N'Tốt (8-10h)', 1, DATEADD(DAY, -5, GETUTCDATE()), 0);
END

-- Reviews cho Santal 33
IF @P_Santal IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reviews WHERE ProductId = @P_Santal)
BEGIN
    INSERT INTO Reviews (ProductId, UserName, Rating, Comment, LongevityRating, IsApproved, CreatedAt, IsDeleted)
    VALUES
    (@P_Santal, N'Đỗ Phương Thảo', 5, N'Santal 33 đúng là mùi hương độc bản! Ban đầu hơi ngái mùi thảo mộc nhưng sau 15 phút nốt đàn hương khô tỏa ra cực kỳ nghệ thuật và cuốn hút.', N'Rất lâu (trên 10h)', 1, DATEADD(DAY, -6, GETUTCDATE()), 0),
    (@P_Santal, N'Lâm Khánh Hà', 5, N'Mình dùng chai này ai đi qua cũng hỏi. Chai chiết 10ml cầm rất chắc tay và vòi xịt sương tơi đều.', N'Rất lâu (trên 10h)', 1, DATEADD(DAY, -3, GETUTCDATE()), 0);
END

-- Reviews cho YSL Libre
IF @P_Libre IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reviews WHERE ProductId = @P_Libre)
BEGIN
    INSERT INTO Reviews (ProductId, UserName, Rating, Comment, LongevityRating, IsApproved, CreatedAt, IsDeleted)
    VALUES
    (@P_Libre, N'Nguyễn Thùy Linh', 5, N'YSL Libre kiêu kỳ sang chảnh dã man! Mùi hoa oải hương mát kết hợp hoa cam ngọt ngào, xịt đi làm hay đi tiệc đều tôn thần thái.', N'Tốt (8-10h)', 1, DATEADD(DAY, -8, GETUTCDATE()), 0),
    (@P_Libre, N'Trần Bích Ngọc', 5, N'Mùi hương quyền lực của một nữ giám đốc hiện đại. Nước hoa thơm lâu và tỏa hương xa.', N'Tốt (8-10h)', 1, DATEADD(DAY, -4, GETUTCDATE()), 0);
END

-- Reviews cho Chanel No.5
IF @P_No5 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reviews WHERE ProductId = @P_No5)
BEGIN
    INSERT INTO Reviews (ProductId, UserName, Rating, Comment, LongevityRating, IsApproved, CreatedAt, IsDeleted)
    VALUES
    (@P_No5, N'Bà Thanh Hằng', 5, N'Chanel No.5 là tình yêu vượt thời gian của tôi. Mùi hương sang trọng cổ điển của phụ nữ Pháp quý tộc.', N'Rất lâu (trên 10h)', 1, DATEADD(DAY, -16, GETUTCDATE()), 0);
END

-- Reviews cho Chanel Coco Mademoiselle
IF @P_CocoMad IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reviews WHERE ProductId = @P_CocoMad)
BEGIN
    INSERT INTO Reviews (ProductId, UserName, Rating, Comment, LongevityRating, IsApproved, CreatedAt, IsDeleted)
    VALUES
    (@P_CocoMad, N'Thu Thảo Paris', 5, N'Mùi hương quyến rũ và thanh lịch nhất mà mình từng sở hữu. Rất thích hợp cho các buổi hẹn hò lãng mạn.', N'Tốt (8-10h)', 1, DATEADD(DAY, -10, GETUTCDATE()), 0);
END

-- Reviews cho Tom Ford Lost Cherry
IF @P_LostCherry IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reviews WHERE ProductId = @P_LostCherry)
BEGIN
    INSERT INTO Reviews (ProductId, UserName, Rating, Comment, LongevityRating, IsApproved, CreatedAt, IsDeleted)
    VALUES
    (@P_LostCherry, N'Hoàng Yến', 5, N'Mùi anh đào ngâm rượu ngọt lịm và quyến rũ kinh khủng! Đúng là siêu phẩm của Tom Ford, xứng đáng từng đồng.', N'Tốt (8-10h)', 1, DATEADD(DAY, -5, GETUTCDATE()), 0);
END

-- Reviews cho Replica Lazy Sunday Morning
IF @P_LazySunday IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reviews WHERE ProductId = @P_LazySunday)
BEGIN
    INSERT INTO Reviews (ProductId, UserName, Rating, Comment, LongevityRating, IsApproved, CreatedAt, IsDeleted)
    VALUES
    (@P_LazySunday, N'Minh Thư', 5, N'Mùi hương sạch sẽ tinh tươm như vừa giặt xong ga giường và phơi dưới nắng sớm. Rất thư giãn và dễ chịu.', N'Khá (6-8h)', 1, DATEADD(DAY, -2, GETUTCDATE()), 0);
END

PRINT N'>>> SEED REMAINING REVIEWS XONG! <<<';
